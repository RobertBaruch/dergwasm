#ifndef __RESONITE_RESONITE_COMPONENT_H__
#define __RESONITE_RESONITE_COMPONENT_H__

#include "py/obj.h"
#include "py/runtime.h"

// Wrapper for component-related functions in C.

extern mp_obj_t resonite_Component_get_type_name(mp_obj_t ref_id);

extern mp_obj_t resonite_Component_get_field_value(mp_obj_t ref_id, mp_obj_t name);

extern mp_obj_t resonite_Component_set_field_value(mp_obj_t ref_id, mp_obj_t name, mp_obj_t value);

#endif // __RESONITE_RESONITE_COMPONENT_H__
