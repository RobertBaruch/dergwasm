#ifndef __RESONITE_RESONITE_NUMERICS_H__
#define __RESONITE_RESONITE_NUMERICS_H__

#include <stdint.h>

#include "py/obj.h"
#include "py/runtime.h"
#include "resonite_api_types.h"

// Numerics based on Resonite types.
// An exception will be raised if you attempt to create a numeric object with
// a value that is out of range for the underlying type.

typedef struct _resonite_Int_obj_t {
    mp_obj_base_t base;
    int32_t value;
} resonite_Int_obj_t;

extern const mp_obj_type_t resonite_Int_type;

typedef struct _resonite_UInt_obj_t {
    mp_obj_base_t base;
    uint32_t value;
} resonite_UInt_obj_t;

extern const mp_obj_type_t resonite_UInt_type;

typedef struct _resonite_Long_obj_t {
    mp_obj_base_t base;
    int64_t value;
} resonite_Long_obj_t;

extern const mp_obj_type_t resonite_Long_type;

typedef struct _resonite_ULong_obj_t {
    mp_obj_base_t base;
    uint64_t value;
} resonite_ULong_obj_t;

extern const mp_obj_type_t resonite_ULong_type;

extern mp_obj_t resonite_new_Int(int32_t val);
extern mp_obj_t resonite_new_UInt(uint32_t val);
extern mp_obj_t resonite_new_Long(int64_t val);
extern mp_obj_t resonite_new_ULong(uint64_t val);
extern mp_obj_t resonite_Int_make_new(const mp_obj_type_t* type, size_t n_args, size_t n_kw, const mp_obj_t* args);
extern mp_obj_t resonite_UInt_make_new(const mp_obj_type_t* type, size_t n_args, size_t n_kw, const mp_obj_t* args);
extern mp_obj_t resonite_Long_make_new(const mp_obj_type_t* type, size_t n_args, size_t n_kw, const mp_obj_t* args);
extern mp_obj_t resonite_ULong_make_new(const mp_obj_type_t* type, size_t n_args, size_t n_kw, const mp_obj_t* args);
extern void resonite_Int_print(const mp_print_t* print, mp_obj_t self_in, mp_print_kind_t kind);
extern void resonite_UInt_print(const mp_print_t* print, mp_obj_t self_in, mp_print_kind_t kind);
extern void resonite_Long_print(const mp_print_t* print, mp_obj_t self_in, mp_print_kind_t kind);
extern void resonite_ULong_print(const mp_print_t* print, mp_obj_t self_in, mp_print_kind_t kind);
extern mp_obj_t resonite_Int_unary_op(mp_unary_op_t op, mp_obj_t self_in);
extern mp_obj_t resonite_UInt_unary_op(mp_unary_op_t op, mp_obj_t self_in);
extern mp_obj_t resonite_Long_unary_op(mp_unary_op_t op, mp_obj_t self_in);
extern mp_obj_t resonite_ULong_unary_op(mp_unary_op_t op, mp_obj_t self_in);
extern mp_obj_t resonite_Int_binary_op(mp_binary_op_t op, mp_obj_t lhs_in, mp_obj_t rhs_in);
extern mp_obj_t resonite_UInt_binary_op(mp_binary_op_t op, mp_obj_t lhs_in, mp_obj_t rhs_in);
extern mp_obj_t resonite_Long_binary_op(mp_binary_op_t op, mp_obj_t lhs_in, mp_obj_t rhs_in);
extern mp_obj_t resonite_ULong_binary_op(mp_binary_op_t op, mp_obj_t lhs_in, mp_obj_t rhs_in);

#endif // __RESONITE_RESONITE_NUMERICS_H__
