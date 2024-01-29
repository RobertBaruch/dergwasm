import enum
import json
import sys
from typing import Tuple

HEADER_PREAMBLE = """
#ifndef MICROPY_INCLUDED_USERCMODULE_RESONITE_API_H
#define MICROPY_INCLUDED_USERCMODULE_RESONITE_API_H

#include "py/obj.h"
#include "py/runtime.h"

// Wrapper for slot-related functions in C.

"""

HEADER_POSTAMBLE = """
#endif // MICROPY_INCLUDED_USERCMODULE_RESONITE_API_H
"""

IMPL_PREAMBLE = """
#include "mp_resonite_api.h"

#include <string.h>

#include "py/obj.h"
#include "py/runtime.h"
#include "resonite_api.h"
#include "mp_resonite_utils.h"

"""


@enum.unique
class ValueType(enum.IntEnum):
    I32 = 0x7F
    I64 = 0x7E
    F32 = 0x7D
    F64 = 0x7C
    V128 = 0x7B
    FUNCREF = 0x70
    EXTERNREF = 0x6F


class GenericType:
    base_type: str
    type_params: list["GenericType"]

    def __init__(self, base_type: str, type_params: list["GenericType"] | None = None):
        self.base_type = base_type
        self.type_params = type_params if type_params is not None else []

    @staticmethod
    def parse_generic_type(s: str) -> "GenericType":
        # Helper function to split the string by commas, considering nested generics
        def split_type_params(s: str) -> list[str]:
            params: list[str] = []
            bracket_level = 0
            current = ""
            for char in s:
                if char == "," and bracket_level == 0:
                    params.append(current)
                    current = ""
                else:
                    if char == "<":
                        bracket_level += 1
                    elif char == ">":
                        bracket_level -= 1
                    current += char
            if current:
                params.append(current)
            return params

        # Base case: no type parameters
        if "<" not in s:
            return GenericType(s)

        # Recursive case: parse type parameters
        base_type, rest = s.split("<", 1)
        type_params_str = rest[:-1]  # Remove the closing '>'
        type_params = [
            GenericType.parse_generic_type(tp.strip())
            for tp in split_type_params(type_params_str)
        ]
        return GenericType(base_type, type_params)

    def __repr__(self) -> str:
        if not self.type_params:
            return self.base_type
        return f"{self.base_type}:({', '.join(map(str, self.type_params))})"


PY_TO_WASM: dict[ValueType, str] = {
    ValueType.I32: "mp_obj_get_int",
    ValueType.I64: "mp_obj_int_get_int64_checked",
    ValueType.F32: "mp_obj_get_float",
    ValueType.F64: "mp_obj_get_float",
}

WASM_TO_PY: dict[ValueType, str] = {
    ValueType.I32: "mp_obj_new_int_from_ll",
    ValueType.I64: "mp_obj_new_int_from_ll",
    ValueType.F32: "mp_obj_new_float",
    ValueType.F64: "mp_obj_new_float",
}


class Main:
    def __init__(self) -> None:
        pass

    @staticmethod
    def py_to_wasm(value_type: ValueType, generic_type: GenericType) -> str:
        generic_type_str = str(generic_type)
        if generic_type_str == "int":
            return "(int32_t)mp_obj_get_int"
        if generic_type_str == "uint":
            return "(uint32_t)mp_obj_get_int"
        if generic_type_str == "long":
            return "mp_obj_int_get_int64_checked"
        if generic_type_str == "ulong":
            return "mp_obj_int_get_uint64_checked"
        if generic_type_str == "float":
            return "(float)mp_obj_get_float"
        if generic_type_str == "double":
            return "(double)mp_obj_get_float"
        if generic_type_str.startswith("WasmRefID"):
            return "mp_obj_int_get_uint64_checked"
        if generic_type_str.startswith("Ptr"):
            return "(int32_t)mp_obj_get_int"
        return PY_TO_WASM[value_type]

    @staticmethod
    def wasm_to_py(value_type: ValueType, generic_type: GenericType) -> str:
        generic_type_str = str(generic_type)
        if generic_type_str in ["int", "uint", "long", "ulong"]:
            return "mp_obj_new_int_from_ll"
        if generic_type_str in ["float", "double"]:
            return "mp_obj_new_float"
        if generic_type_str.startswith("WasmRefID"):
            return "mp_obj_new_int_from_ll"
        if generic_type_str.startswith("Ptr"):
            return "mp_obj_new_int_from_ll"
        return WASM_TO_PY[value_type]

    def generate_header(self) -> None:
        with open("resonite_api.json", "r", encoding="UTF8") as f:
            data = json.load(f)

        with open(
            "micropython/usercmodule/resonite/mp_resonite_api.h", "w", encoding="UTF8"
        ) as f:
            f.write(HEADER_PREAMBLE)
            for item in data:
                f.write(f'extern mp_obj_t resonite__{item["Name"]}(')
                params = [f'mp_obj_t {param["Name"]}' for param in item["Parameters"]]
                f.write(", ".join(params))
                f.write(");\n")
            f.write(HEADER_POSTAMBLE)

        with open(
            "micropython/usercmodule/resonite/mp_resonite_api.c", "w", encoding="UTF8"
        ) as f:
            f.write(IMPL_PREAMBLE)
            for item in data:
                params = [f'mp_obj_t {param["Name"]}' for param in item["Parameters"]]
                f.write(f'mp_obj_t resonite__{item["Name"]}({", ".join(params)}) {{\n')

                call_params: list[str] = []
                for param in item["Parameters"]:
                    value_type = ValueType(param["Type"])
                    generic_type = GenericType.parse_generic_type(param["CSType"])
                    converter = self.py_to_wasm(value_type, generic_type)
                    call_params.append(f'\n    {converter}({param["Name"]})')
                    print(
                        f"param: {value_type}, generic: {generic_type} -> {converter}"
                    )
                call = f'{item["Name"]}({", ".join(call_params)})'

                if len(item["Returns"]) == 0:
                    f.write(f"  {call};\n")
                else:
                    ret_type = ValueType(item["Returns"][0]["Type"])
                    ret_generic_type = GenericType.parse_generic_type(
                        item["Returns"][0]["CSType"]
                    )
                    converter = self.wasm_to_py(ret_type, ret_generic_type)
                    print(
                        f"return: {ret_type}, generic: {ret_generic_type} -> {converter}"
                    )
                    f.write(f"  return {converter}({call});\n")

                f.write("}\n\n")

    def main(self) -> int:
        self.generate_header()
        return 0


if __name__ == "__main__":
    sys.exit(Main().main())
