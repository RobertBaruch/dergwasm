#ifndef __RESONITE_RESONITE_UTILS_H__
#define __RESONITE_RESONITE_UTILS_H__

#include <stdint.h>

#include "py/obj.h"
#include "py/runtime.h"
#include "resonite_api_types.h"

extern int64_t mp_obj_int_get_int64_checked(mp_const_obj_t o);
extern uint64_t mp_obj_int_get_uint64_checked(mp_const_obj_t o);

#endif // __RESONITE_RESONITE_UTILS_H__
