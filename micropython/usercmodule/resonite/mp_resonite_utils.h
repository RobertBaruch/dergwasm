#ifndef __RESONITE_RESONITE_UTILS_H__
#define __RESONITE_RESONITE_UTILS_H__

#include <stdint.h>

#include "py/obj.h"
#include "py/runtime.h"
#include "resonite_api_types.h"

extern void mp_resonite_check_error(resonite_error_code_t err);
extern int64_t mp_obj_int_get_int64_checked(mp_const_obj_t o);
extern uint64_t mp_obj_int_get_uint64_checked(mp_const_obj_t o);
extern mp_obj_t mp_obj_new_null_terminated_str(char *str);

#endif // __RESONITE_RESONITE_UTILS_H__
