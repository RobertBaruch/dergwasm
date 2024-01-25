#include "mp_resonite_numerics.h"

#include "py/obj.h"
#include "py/runtime.h"

STATIC const mp_rom_map_elem_t resonite_Int_locals_dict_table[] = {
};
STATIC MP_DEFINE_CONST_DICT(resonite_Int_locals_dict, resonite_Int_locals_dict_table);

MP_DEFINE_CONST_OBJ_TYPE(
    resonite_Int_type,
    MP_QSTR_Int,
    MP_TYPE_FLAG_NONE,
    make_new, resonite_Int_make_new,
    print, resonite_Int_print,
    unary_op, resonite_Int_unary_op,
    binary_op, resonite_Int_binary_op,
    locals_dict, &resonite_Int_locals_dict
);

STATIC const mp_rom_map_elem_t resonite_UInt_locals_dict_table[] = {
};
STATIC MP_DEFINE_CONST_DICT(resonite_UInt_locals_dict, resonite_UInt_locals_dict_table);

MP_DEFINE_CONST_OBJ_TYPE(
    resonite_UInt_type,
    MP_QSTR_UInt,
    MP_TYPE_FLAG_NONE,
    make_new, resonite_UInt_make_new,
    print, resonite_UInt_print,
    unary_op, resonite_UInt_unary_op,
    locals_dict, &resonite_UInt_locals_dict
);

STATIC const mp_rom_map_elem_t resonite_Long_locals_dict_table[] = {
};
STATIC MP_DEFINE_CONST_DICT(resonite_Long_locals_dict, resonite_Long_locals_dict_table);

MP_DEFINE_CONST_OBJ_TYPE(
    resonite_Long_type,
    MP_QSTR_Long,
    MP_TYPE_FLAG_NONE,
    make_new, resonite_Long_make_new,
    print, resonite_Long_print,
    unary_op, resonite_Long_unary_op,
    locals_dict, &resonite_Long_locals_dict
);

STATIC const mp_rom_map_elem_t resonite_ULong_locals_dict_table[] = {
};
STATIC MP_DEFINE_CONST_DICT(resonite_ULong_locals_dict, resonite_ULong_locals_dict_table);

MP_DEFINE_CONST_OBJ_TYPE(
    resonite_ULong_type,
    MP_QSTR_ULong,
    MP_TYPE_FLAG_NONE,
    make_new, resonite_ULong_make_new,
    print, resonite_ULong_print,
    unary_op, resonite_ULong_unary_op,
    locals_dict, &resonite_ULong_locals_dict
);
