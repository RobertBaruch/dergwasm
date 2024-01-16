#include "mp_resonite_numerics.h"

#include <math.h>
#include <stdint.h>

#include "py/obj.h"
#include "py/runtime.h"
#include "py/mpz.h"
#include "py/objint.h"
#include "py/smallint.h"

// MicroPython uses the default object representation, representation A. See
// micropython/py/mpconfig.h. Also, the machine word is 32 bits for WASM.
// This means that a "small int" is a 31-bit signed integer.
//
// An int in MicroPython is either a small int or an MPZ (multiprecision integer).

void resonite_Int_print(const mp_print_t* print, mp_obj_t self_in, mp_print_kind_t kind) {
    resonite_Int_obj_t* self = MP_OBJ_TO_PTR(self_in);
    mp_printf(print, "%d", self->value);
}

void resonite_UInt_print(const mp_print_t* print, mp_obj_t self_in, mp_print_kind_t kind) {
    resonite_UInt_obj_t* self = MP_OBJ_TO_PTR(self_in);
    mp_printf(print, "%u", self->value);
}

void resonite_Long_print(const mp_print_t* print, mp_obj_t self_in, mp_print_kind_t kind) {
    resonite_Long_obj_t* self = MP_OBJ_TO_PTR(self_in);
    mp_printf(print, "%ld", self->value);
}

void resonite_ULong_print(const mp_print_t* print, mp_obj_t self_in, mp_print_kind_t kind) {
    resonite_ULong_obj_t* self = MP_OBJ_TO_PTR(self_in);
    mp_printf(print, "%lu", self->value);
}

mp_obj_t resonite_new_Int(int32_t val) {
    resonite_Int_obj_t* self = mp_obj_malloc(resonite_Int_obj_t, &resonite_Int_type);
    self->value = val;
    return MP_OBJ_FROM_PTR(self);
}

mp_obj_t resonite_new_UInt(uint32_t val) {
    resonite_UInt_obj_t* self = mp_obj_malloc(resonite_UInt_obj_t, &resonite_UInt_type);
    self->value = val;
    return MP_OBJ_FROM_PTR(self);
}

mp_obj_t resonite_new_Long(int64_t val) {
    resonite_Long_obj_t* self = mp_obj_malloc(resonite_Long_obj_t, &resonite_Long_type);
    self->value = val;
    return MP_OBJ_FROM_PTR(self);
}

mp_obj_t resonite_new_ULong(uint64_t val) {
    resonite_ULong_obj_t* self = mp_obj_malloc(resonite_ULong_obj_t, &resonite_ULong_type);
    self->value = val;
    return MP_OBJ_FROM_PTR(self);
}

// Accepts either no args, or an int.
mp_obj_t resonite_Int_make_new(const mp_obj_type_t* type, size_t n_args, size_t n_kw, const mp_obj_t* args) {
    mp_arg_check_num(n_args, n_kw, 0, 1, false);

    if (n_args == 0) {
        return resonite_new_Int(0);
    }
    else if (mp_obj_is_int(args[0])) {
        return resonite_new_Int(mp_obj_int_get_checked(args[0]));
    }
    mp_raise_msg_varg(&mp_type_TypeError,
        MP_ERROR_TEXT("can't convert %s to Int: only takes an int"), mp_obj_get_type_str(args[0]));
}

// Accepts either no args, or an int.
mp_obj_t resonite_UInt_make_new(const mp_obj_type_t* type, size_t n_args, size_t n_kw, const mp_obj_t* args) {
    mp_arg_check_num(n_args, n_kw, 0, 1, false);

    if (n_args == 0) {
        return resonite_new_UInt(0);
    }
    else if (mp_obj_is_int(args[0])) {
        return resonite_new_UInt(mp_obj_int_get_uint_checked(args[0]));
    }
    mp_raise_msg_varg(&mp_type_TypeError,
        MP_ERROR_TEXT("can't convert %s to UInt: only takes an int"), mp_obj_get_type_str(args[0]));
}

// An MPZ is stored as an array of n-bit "digits". In the WASM port, each digit is 16 bits.
// They are stored little-endian. For example, the value 0x1234567890 is stored as
// { 0x7890, 0x3456, 0x0012 }.
static bool mpz_as_int64_checked(const mpz_t* i, int64_t* value) {
    uint64_t val = 0;
    mpz_dig_t* d = i->dig + i->len;

    while (d-- > i->dig) {
        if (val > (0x7FFFFFFFFFFFFFFFULL >> MPZ_DIG_SIZE)) {
            // will overflow when shifted left by MPZ_DIG_SIZE.
            return false;
        }
        val = (val << MPZ_DIG_SIZE) | *d;
    }

    if (i->neg != 0) {
        val = -val;
    }

    *value = val;
    return true;
}

static bool mpz_as_uint64_checked(const mpz_t* i, uint64_t* value) {
    if (i->neg != 0) {
        // Can't represent negative numbers.
        return false;
    }

    uint64_t val = 0;
    mpz_dig_t* d = i->dig + i->len;

    while (d-- > i->dig) {
        if (val > (0x7FFFFFFFFFFFFFFFULL >> (MPZ_DIG_SIZE - 1))) {
            // will overflow when shifted left by MPZ_DIG_SIZE.
            return false;
        }
        val = (val << MPZ_DIG_SIZE) | *d;
    }

    *value = val;
    return true;
}

static int64_t mp_obj_int_get_int64_checked(mp_const_obj_t self_in) {
    if (mp_obj_is_small_int(self_in)) {
        return MP_OBJ_SMALL_INT_VALUE(self_in);
    }
    const mp_obj_int_t* self = MP_OBJ_TO_PTR(self_in);
    int64_t value;
    if (mpz_as_int64_checked(&self->mpz, &value)) {
        return value;
    }
    mp_raise_msg(&mp_type_OverflowError, MP_ERROR_TEXT("overflow converting Python int to long"));
}

static uint64_t mp_obj_int_get_uint64_checked(mp_const_obj_t self_in) {
    if (mp_obj_is_small_int(self_in)) {
        if (MP_OBJ_SMALL_INT_VALUE(self_in) > 0) {
            return MP_OBJ_SMALL_INT_VALUE(self_in);
        }
    }
    else {
        const mp_obj_int_t* self = MP_OBJ_TO_PTR(self_in);
        uint64_t value;
        if (mpz_as_uint64_checked(&self->mpz, &value)) {
            return value;
        }
    }
    mp_raise_msg(&mp_type_OverflowError, MP_ERROR_TEXT("overflow converting Python int to ulong"));
}

// Accepts either no args, or an int.
mp_obj_t resonite_Long_make_new(const mp_obj_type_t* type, size_t n_args, size_t n_kw, const mp_obj_t* args) {
    mp_arg_check_num(n_args, n_kw, 0, 1, false);

    if (n_args == 0) {
        return resonite_new_Long(0);
    }
    else if (mp_obj_is_int(args[0])) {
        return resonite_new_Long(mp_obj_int_get_int64_checked(args[0]));
    }
    mp_raise_msg_varg(&mp_type_TypeError,
        MP_ERROR_TEXT("can't convert %s to Long: only takes an int"), mp_obj_get_type_str(args[0]));
}

// Accepts either no args, or an int.
mp_obj_t resonite_ULong_make_new(const mp_obj_type_t* type, size_t n_args, size_t n_kw, const mp_obj_t* args) {
    mp_arg_check_num(n_args, n_kw, 0, 1, false);

    if (n_args == 0) {
        return resonite_new_ULong(0);
    }
    else if (mp_obj_is_int(args[0])) {
        return resonite_new_ULong(mp_obj_int_get_uint64_checked(args[0]));
    }
    mp_raise_msg_varg(&mp_type_TypeError,
        MP_ERROR_TEXT("can't convert %s to ULong: only takes an int"), mp_obj_get_type_str(args[0]));
}

mp_obj_t resonite_Int_unary_op(mp_unary_op_t op, mp_obj_t self_in) {
    resonite_Int_obj_t* self = MP_OBJ_TO_PTR(self_in);
    switch (op) {
    case MP_UNARY_OP_BOOL:
        return mp_obj_new_bool(self->value != 0);
    case MP_UNARY_OP_HASH:
        return MP_OBJ_NEW_SMALL_INT(self->value >> 1);
    case MP_UNARY_OP_POSITIVE:
        return self_in;
    case MP_UNARY_OP_NEGATIVE:
        return resonite_new_Int(-self->value);
    case MP_UNARY_OP_INVERT:
        return resonite_new_Int(~self->value);
    case MP_UNARY_OP_ABS:
        return resonite_new_Int(self->value < 0 ? -self->value : self->value);
    case MP_UNARY_OP_INT_MAYBE: {
        if (MP_SMALL_INT_FITS(self->value)) {
            return MP_OBJ_NEW_SMALL_INT(self->value);
        }
        else {
            mp_obj_int_t* o = mp_obj_int_new_mpz();
            mpz_set_from_ll(&o->mpz, self->value, true);
            return MP_OBJ_FROM_PTR(o);
        }
    }
    default:
        return MP_OBJ_NULL; // op not supported
    }
}

mp_obj_t resonite_UInt_unary_op(mp_unary_op_t op, mp_obj_t self_in) {
    resonite_UInt_obj_t* self = MP_OBJ_TO_PTR(self_in);
    switch (op) {
    case MP_UNARY_OP_BOOL:
        return mp_obj_new_bool(self->value != 0);
    case MP_UNARY_OP_HASH:
        return MP_OBJ_NEW_SMALL_INT(self->value >> 1);
    case MP_UNARY_OP_POSITIVE:
        return self_in;
    case MP_UNARY_OP_NEGATIVE:
        return resonite_new_Int(-self->value);
    case MP_UNARY_OP_INVERT:
        return resonite_new_UInt(~self->value);
    case MP_UNARY_OP_ABS:
        return self_in;
    case MP_UNARY_OP_INT_MAYBE: {
        if (MP_SMALL_INT_FITS(self->value)) {
            return MP_OBJ_NEW_SMALL_INT(self->value);
        }
        else {
            mp_obj_int_t* o = mp_obj_int_new_mpz();
            mpz_set_from_ll(&o->mpz, self->value, false);
            return MP_OBJ_FROM_PTR(o);
        }
    }
    default:
        return MP_OBJ_NULL; // op not supported
    }
}

mp_obj_t resonite_Long_unary_op(mp_unary_op_t op, mp_obj_t self_in) {
    resonite_Long_obj_t* self = MP_OBJ_TO_PTR(self_in);
    switch (op) {
    case MP_UNARY_OP_BOOL:
        return mp_obj_new_bool(self->value != 0);
    case MP_UNARY_OP_HASH:
        return MP_OBJ_NEW_SMALL_INT((self->value >> 1) & MP_SMALL_INT_POSITIVE_MASK);
    case MP_UNARY_OP_POSITIVE:
        return self_in;
    case MP_UNARY_OP_NEGATIVE:
        return resonite_new_Long(-self->value);
    case MP_UNARY_OP_INVERT:
        return resonite_new_Long(~self->value);
    case MP_UNARY_OP_ABS:
        return resonite_new_Long(self->value < 0 ? -self->value : self->value);
    case MP_UNARY_OP_INT_MAYBE: {
        if (MP_SMALL_INT_FITS(self->value)) {
            return MP_OBJ_NEW_SMALL_INT(self->value);
        }
        else {
            mp_obj_int_t* o = mp_obj_int_new_mpz();
            mpz_set_from_ll(&o->mpz, self->value, true);
            return MP_OBJ_FROM_PTR(o);
        }
    }
    default:
        return MP_OBJ_NULL; // op not supported
    }
}

mp_obj_t resonite_ULong_unary_op(mp_unary_op_t op, mp_obj_t self_in) {
    resonite_ULong_obj_t* self = MP_OBJ_TO_PTR(self_in);
    switch (op) {
    case MP_UNARY_OP_BOOL:
        return mp_obj_new_bool(self->value != 0);
    case MP_UNARY_OP_HASH:
        return MP_OBJ_NEW_SMALL_INT((self->value >> 1) & MP_SMALL_INT_POSITIVE_MASK);
    case MP_UNARY_OP_POSITIVE:
        return self_in;
    case MP_UNARY_OP_NEGATIVE:
        return resonite_new_Long(-self->value);
    case MP_UNARY_OP_INVERT:
        return resonite_new_ULong(~self->value);
    case MP_UNARY_OP_ABS:
        return self_in;
    case MP_UNARY_OP_INT_MAYBE: {
        if (MP_SMALL_INT_FITS(self->value)) {
            return MP_OBJ_NEW_SMALL_INT(self->value);
        }
        else {
            mp_obj_int_t* o = mp_obj_int_new_mpz();
            mpz_set_from_ll(&o->mpz, self->value, false);
            return MP_OBJ_FROM_PTR(o);
        }
    }
    default:
        return MP_OBJ_NULL; // op not supported
    }
}

mp_obj_t resonite_Int_binary_op(mp_binary_op_t op, mp_obj_t lhs_in, mp_obj_t rhs_in) {
    if (mp_obj_get_type(lhs_in) != &resonite_Int_type) {
        mp_raise_msg_varg(&mp_type_TypeError,
            MP_ERROR_TEXT("unsupported lvalue type for Int %s: '%s'"), mp_binary_op_method_name[op], mp_obj_get_type_str(lhs_in));
    }
    resonite_Int_obj_t* lhs = MP_OBJ_TO_PTR(lhs_in);
    int32_t rhs_value;

    if (mp_obj_get_type(rhs_in) == &resonite_Int_type) {
        resonite_Int_obj_t* rhs = MP_OBJ_TO_PTR(rhs_in);
        rhs_value = rhs->value;
    }
    else if (mp_obj_get_type(rhs_in) == &mp_type_int) {
        rhs_value = mp_obj_int_get_checked(rhs_in);
    }
    else {
        mp_raise_msg_varg(&mp_type_TypeError,
            MP_ERROR_TEXT("unsupported rvalue type for Int %s: '%s'"), mp_binary_op_method_name[op], mp_obj_get_type_str(rhs_in));
    }

    switch (op) {
    case MP_BINARY_OP_LESS:
        return mp_obj_new_bool(lhs->value < rhs_value);
    case MP_BINARY_OP_MORE:
        return mp_obj_new_bool(lhs->value > rhs_value);
    case MP_BINARY_OP_EQUAL:
        return mp_obj_new_bool(lhs->value == rhs_value);
    case MP_BINARY_OP_LESS_EQUAL:
        return mp_obj_new_bool(lhs->value <= rhs_value);
    case MP_BINARY_OP_MORE_EQUAL:
        return mp_obj_new_bool(lhs->value >= rhs_value);
    case MP_BINARY_OP_NOT_EQUAL:
        return mp_obj_new_bool(lhs->value != rhs_value);

    case MP_BINARY_OP_OR:
        return resonite_new_Int(lhs->value | rhs_value);
    case MP_BINARY_OP_INPLACE_OR:
        lhs->value |= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_XOR:
        return resonite_new_Int(lhs->value ^ rhs_value);
    case MP_BINARY_OP_INPLACE_XOR:
        lhs->value ^= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_AND:
        return resonite_new_Int(lhs->value & rhs_value);
    case MP_BINARY_OP_INPLACE_AND:
        lhs->value &= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_LSHIFT:
        return resonite_new_Int(lhs->value << rhs_value);
    case MP_BINARY_OP_INPLACE_LSHIFT:
        lhs->value <<= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_RSHIFT:
        return resonite_new_Int(lhs->value >> rhs_value);
    case MP_BINARY_OP_INPLACE_RSHIFT:
        lhs->value >>= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_ADD:
        return resonite_new_Int(lhs->value + rhs_value);
    case MP_BINARY_OP_INPLACE_ADD:
        lhs->value += rhs_value;
        return lhs_in;

    case MP_BINARY_OP_SUBTRACT:
        return resonite_new_Int(lhs->value - rhs_value);
    case MP_BINARY_OP_INPLACE_SUBTRACT:
        lhs->value -= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_MULTIPLY:
        return resonite_new_Int(lhs->value * rhs_value);
    case MP_BINARY_OP_INPLACE_MULTIPLY:
        lhs->value *= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_TRUE_DIVIDE:
    case MP_BINARY_OP_FLOOR_DIVIDE:
        if (rhs_value == 0) {
            mp_raise_msg(&mp_type_ZeroDivisionError, MP_ERROR_TEXT("division by zero"));
        }
        return resonite_new_Int(lhs->value / rhs_value);
    case MP_BINARY_OP_INPLACE_TRUE_DIVIDE:
    case MP_BINARY_OP_INPLACE_FLOOR_DIVIDE: {
        if (rhs_value == 0) {
            mp_raise_msg(&mp_type_ZeroDivisionError, MP_ERROR_TEXT("division by zero"));
        }
        lhs->value /= rhs_value;
        return lhs_in;
    }

    case MP_BINARY_OP_MODULO:
        if (rhs_value == 0) {
            mp_raise_msg(&mp_type_ZeroDivisionError, MP_ERROR_TEXT("division by zero"));
        }
        return resonite_new_Int(lhs->value % rhs_value);
    case MP_BINARY_OP_INPLACE_MODULO: {
        if (rhs_value == 0) {
            mp_raise_msg(&mp_type_ZeroDivisionError, MP_ERROR_TEXT("division by zero"));
        }
        lhs->value %= rhs_value;
        return lhs_in;
    }

    case MP_BINARY_OP_POWER:
        return resonite_new_Int((int32_t)pow(lhs->value, rhs_value));
    case MP_BINARY_OP_INPLACE_POWER: {
        lhs->value = (int32_t)pow(lhs->value, rhs_value);
        return lhs_in;
    }

    default:
        return MP_OBJ_NULL;
    }
}

mp_obj_t resonite_UInt_binary_op(mp_binary_op_t op, mp_obj_t lhs_in, mp_obj_t rhs_in) {
    if (mp_obj_get_type(lhs_in) != &resonite_UInt_type) {
        mp_raise_msg_varg(&mp_type_TypeError,
            MP_ERROR_TEXT("unsupported lvalue type for UInt %s: '%s'"), mp_binary_op_method_name[op], mp_obj_get_type_str(lhs_in));
    }
    resonite_UInt_obj_t* lhs = MP_OBJ_TO_PTR(lhs_in);
    uint32_t rhs_value;

    if (mp_obj_get_type(rhs_in) == &resonite_UInt_type) {
        resonite_UInt_obj_t* rhs = MP_OBJ_TO_PTR(rhs_in);
        rhs_value = rhs->value;
    }
    else if (mp_obj_get_type(rhs_in) == &mp_type_int) {
        rhs_value = mp_obj_int_get_uint_checked(rhs_in);
    }
    else {
        mp_raise_msg_varg(&mp_type_TypeError,
            MP_ERROR_TEXT("unsupported rvalue type for UInt %s: '%s'"), mp_binary_op_method_name[op], mp_obj_get_type_str(rhs_in));
    }

    switch (op) {
    case MP_BINARY_OP_LESS:
        return mp_obj_new_bool(lhs->value < rhs_value);
    case MP_BINARY_OP_MORE:
        return mp_obj_new_bool(lhs->value > rhs_value);
    case MP_BINARY_OP_EQUAL:
        return mp_obj_new_bool(lhs->value == rhs_value);
    case MP_BINARY_OP_LESS_EQUAL:
        return mp_obj_new_bool(lhs->value <= rhs_value);
    case MP_BINARY_OP_MORE_EQUAL:
        return mp_obj_new_bool(lhs->value >= rhs_value);
    case MP_BINARY_OP_NOT_EQUAL:
        return mp_obj_new_bool(lhs->value != rhs_value);

    case MP_BINARY_OP_OR:
        return resonite_new_UInt(lhs->value | rhs_value);
    case MP_BINARY_OP_INPLACE_OR:
        lhs->value |= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_XOR:
        return resonite_new_UInt(lhs->value ^ rhs_value);
    case MP_BINARY_OP_INPLACE_XOR:
        lhs->value ^= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_AND:
        return resonite_new_UInt(lhs->value & rhs_value);
    case MP_BINARY_OP_INPLACE_AND:
        lhs->value &= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_LSHIFT:
        return resonite_new_UInt(lhs->value << rhs_value);
    case MP_BINARY_OP_INPLACE_LSHIFT:
        lhs->value <<= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_RSHIFT:
        return resonite_new_UInt(lhs->value >> rhs_value);
    case MP_BINARY_OP_INPLACE_RSHIFT:
        lhs->value >>= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_ADD:
        return resonite_new_UInt(lhs->value + rhs_value);
    case MP_BINARY_OP_INPLACE_ADD:
        lhs->value += rhs_value;
        return lhs_in;

    case MP_BINARY_OP_SUBTRACT:
        return resonite_new_UInt(lhs->value - rhs_value);
    case MP_BINARY_OP_INPLACE_SUBTRACT:
        lhs->value -= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_MULTIPLY:
        return resonite_new_UInt(lhs->value * rhs_value);
    case MP_BINARY_OP_INPLACE_MULTIPLY:
        lhs->value *= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_TRUE_DIVIDE:
    case MP_BINARY_OP_FLOOR_DIVIDE:
        if (rhs_value == 0) {
            mp_raise_msg(&mp_type_ZeroDivisionError, MP_ERROR_TEXT("division by zero"));
        }
        return resonite_new_UInt(lhs->value / rhs_value);
    case MP_BINARY_OP_INPLACE_TRUE_DIVIDE:
    case MP_BINARY_OP_INPLACE_FLOOR_DIVIDE: {
        if (rhs_value == 0) {
            mp_raise_msg(&mp_type_ZeroDivisionError, MP_ERROR_TEXT("division by zero"));
        }
        lhs->value /= rhs_value;
        return lhs_in;
    }

    case MP_BINARY_OP_MODULO:
        if (rhs_value == 0) {
            mp_raise_msg(&mp_type_ZeroDivisionError, MP_ERROR_TEXT("division by zero"));
        }
        return resonite_new_UInt(lhs->value % rhs_value);
    case MP_BINARY_OP_INPLACE_MODULO: {
        if (rhs_value == 0) {
            mp_raise_msg(&mp_type_ZeroDivisionError, MP_ERROR_TEXT("division by zero"));
        }
        lhs->value %= rhs_value;
        return lhs_in;
    }

    case MP_BINARY_OP_POWER:
        return resonite_new_UInt((uint32_t)pow(lhs->value, rhs_value));
    case MP_BINARY_OP_INPLACE_POWER: {
        lhs->value = (uint32_t)pow(lhs->value, rhs_value);
        return lhs_in;
    }

    default:
        return MP_OBJ_NULL;
    }
}

mp_obj_t resonite_Long_binary_op(mp_binary_op_t op, mp_obj_t lhs_in, mp_obj_t rhs_in) {
    if (mp_obj_get_type(lhs_in) != &resonite_Long_type) {
        mp_raise_msg_varg(&mp_type_TypeError,
            MP_ERROR_TEXT("unsupported lvalue type for Long %s: '%s'"), mp_binary_op_method_name[op], mp_obj_get_type_str(lhs_in));
    }
    resonite_Long_obj_t* lhs = MP_OBJ_TO_PTR(lhs_in);
    int64_t rhs_value;

    if (mp_obj_get_type(rhs_in) == &resonite_Long_type) {
        resonite_Long_obj_t* rhs = MP_OBJ_TO_PTR(rhs_in);
        rhs_value = rhs->value;
    }
    else if (mp_obj_get_type(rhs_in) == &mp_type_int) {
        rhs_value = mp_obj_int_get_int64_checked(rhs_in);
    }
    else {
        mp_raise_msg_varg(&mp_type_TypeError,
            MP_ERROR_TEXT("unsupported rvalue type for Long %s: '%s'"), mp_binary_op_method_name[op], mp_obj_get_type_str(rhs_in));
    }

    switch (op) {
    case MP_BINARY_OP_LESS:
        return mp_obj_new_bool(lhs->value < rhs_value);
    case MP_BINARY_OP_MORE:
        return mp_obj_new_bool(lhs->value > rhs_value);
    case MP_BINARY_OP_EQUAL:
        return mp_obj_new_bool(lhs->value == rhs_value);
    case MP_BINARY_OP_LESS_EQUAL:
        return mp_obj_new_bool(lhs->value <= rhs_value);
    case MP_BINARY_OP_MORE_EQUAL:
        return mp_obj_new_bool(lhs->value >= rhs_value);
    case MP_BINARY_OP_NOT_EQUAL:
        return mp_obj_new_bool(lhs->value != rhs_value);

    case MP_BINARY_OP_OR:
        return resonite_new_Int(lhs->value | rhs_value);
    case MP_BINARY_OP_INPLACE_OR:
        lhs->value |= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_XOR:
        return resonite_new_Int(lhs->value ^ rhs_value);
    case MP_BINARY_OP_INPLACE_XOR:
        lhs->value ^= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_AND:
        return resonite_new_Int(lhs->value & rhs_value);
    case MP_BINARY_OP_INPLACE_AND:
        lhs->value &= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_LSHIFT:
        return resonite_new_Int(lhs->value << rhs_value);
    case MP_BINARY_OP_INPLACE_LSHIFT:
        lhs->value <<= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_RSHIFT:
        return resonite_new_Int(lhs->value >> rhs_value);
    case MP_BINARY_OP_INPLACE_RSHIFT:
        lhs->value >>= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_ADD:
        return resonite_new_Int(lhs->value + rhs_value);
    case MP_BINARY_OP_INPLACE_ADD:
        lhs->value += rhs_value;
        return lhs_in;

    case MP_BINARY_OP_SUBTRACT:
        return resonite_new_Int(lhs->value - rhs_value);
    case MP_BINARY_OP_INPLACE_SUBTRACT:
        lhs->value -= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_MULTIPLY:
        return resonite_new_Int(lhs->value * rhs_value);
    case MP_BINARY_OP_INPLACE_MULTIPLY:
        lhs->value *= rhs_value;
        return lhs_in;

    case MP_BINARY_OP_TRUE_DIVIDE:
    case MP_BINARY_OP_FLOOR_DIVIDE:
        if (rhs_value == 0) {
            mp_raise_msg(&mp_type_ZeroDivisionError, MP_ERROR_TEXT("division by zero"));
        }
        return resonite_new_Int(lhs->value / rhs_value);
    case MP_BINARY_OP_INPLACE_TRUE_DIVIDE:
    case MP_BINARY_OP_INPLACE_FLOOR_DIVIDE: {
        if (rhs_value == 0) {
            mp_raise_msg(&mp_type_ZeroDivisionError, MP_ERROR_TEXT("division by zero"));
        }
        lhs->value /= rhs_value;
        return lhs_in;
    }

    case MP_BINARY_OP_MODULO:
        if (rhs_value == 0) {
            mp_raise_msg(&mp_type_ZeroDivisionError, MP_ERROR_TEXT("division by zero"));
        }
        return resonite_new_Int(lhs->value % rhs_value);
    case MP_BINARY_OP_INPLACE_MODULO: {
        if (rhs_value == 0) {
            mp_raise_msg(&mp_type_ZeroDivisionError, MP_ERROR_TEXT("division by zero"));
        }
        lhs->value %= rhs_value;
        return lhs_in;
    }

    case MP_BINARY_OP_POWER:
        return resonite_new_Int((int32_t)pow(lhs->value, rhs_value));
    case MP_BINARY_OP_INPLACE_POWER: {
        lhs->value = (int32_t)pow(lhs->value, rhs_value);
        return lhs_in;
    }

    default:
        return MP_OBJ_NULL;
    }
}

