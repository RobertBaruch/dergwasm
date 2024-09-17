#include "mp_resonite_utils.h"

#include <math.h>
#include <stdint.h>
#include <string.h>

#include "py/obj.h"
#include "py/runtime.h"
#include "py/mpz.h"
#include "py/objint.h"
#include "py/smallint.h"

// An MPZ is stored as an array of n-bit "digits". In the WASM port, each digit is 16 bits.
// They are stored little-endian. For example, the value 0x1234567890 is stored as
// { 0x7890, 0x3456, 0x0012 }.
static bool mpz_as_int64_checked(const mpz_t *i, int64_t *value)
{
    uint64_t val = 0;
    mpz_dig_t *d = i->dig + i->len;

    while (d-- > i->dig)
    {
        if (val > (0x7FFFFFFFFFFFFFFFULL >> MPZ_DIG_SIZE))
        {
            // will overflow when shifted left by MPZ_DIG_SIZE.
            return false;
        }
        val = (val << MPZ_DIG_SIZE) | *d;
    }

    if (i->neg != 0)
    {
        val = -val;
    }

    *value = val;
    return true;
}

static bool mpz_as_uint64_checked(const mpz_t *i, uint64_t *value)
{
    if (i->neg != 0)
    {
        // Can't represent negative numbers.
        return false;
    }

    uint64_t val = 0;
    mpz_dig_t *d = i->dig + i->len;

    while (d-- > i->dig)
    {
        if (val > (0x7FFFFFFFFFFFFFFFULL >> (MPZ_DIG_SIZE - 1)))
        {
            // will overflow when shifted left by MPZ_DIG_SIZE.
            return false;
        }
        val = (val << MPZ_DIG_SIZE) | *d;
    }

    *value = val;
    return true;
}

int64_t mp_obj_int_get_int64_checked(mp_const_obj_t self_in)
{
    if (mp_obj_is_small_int(self_in))
    {
        return MP_OBJ_SMALL_INT_VALUE(self_in);
    }
    const mp_obj_int_t *self = MP_OBJ_TO_PTR(self_in);
    int64_t value;
    if (mpz_as_int64_checked(&self->mpz, &value))
    {
        return value;
    }
    mp_raise_msg(&mp_type_OverflowError, MP_ERROR_TEXT(
                                             "overflow converting Python int to 64-bit signed int"));
}

uint64_t mp_obj_int_get_uint64_checked(mp_const_obj_t self_in)
{
    if (mp_obj_is_small_int(self_in))
    {
        if (MP_OBJ_SMALL_INT_VALUE(self_in) > 0)
        {
            return MP_OBJ_SMALL_INT_VALUE(self_in);
        }
    }
    else
    {
        const mp_obj_int_t *self = MP_OBJ_TO_PTR(self_in);
        uint64_t value;
        if (mpz_as_uint64_checked(&self->mpz, &value))
        {
            return value;
        }
    }
    mp_raise_msg(&mp_type_OverflowError, MP_ERROR_TEXT(
                                             "overflow converting Python int to 64-bit unsigned int"));
}

mp_obj_t mp_obj_new_null_terminated_str(char *str)
{
    return mp_obj_new_str(str, strlen(str));
}

void mp_resonite_check_error(resonite_error_code_t err)
{
    if (err != RESONITE_ERROR_SUCCESS)
    {
        mp_raise_msg_varg(&mp_type_ValueError, MP_ERROR_TEXT("Resonite API error: %s"),
            resonite_error_code_to_string(err));
    }
}