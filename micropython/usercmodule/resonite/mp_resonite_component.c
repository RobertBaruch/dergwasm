#include "mp_resonite_component.h"

#include <string.h>

#include "py/obj.h"
#include "py/runtime.h"
#include "resonite_component_api.h"
#include "mp_resonite_utils.h"

mp_obj_t resonite_Component_get_type_name(mp_obj_t ref_id)
{
    char *name = component__get_type_name(
        mp_obj_int_get_uint64_checked(ref_id));
    return mp_obj_new_str(name, strlen(name));
}

mp_obj_t resonite_Component_get_field_value(mp_obj_t ref_id, mp_obj_t name)
{
    int len;
    uint8_t *data = component__get_field_value(
        mp_obj_int_get_uint64_checked(ref_id),
        mp_obj_str_get_str(name), &len);
    return data == NULL ? mp_const_none : mp_obj_new_bytes(data, len);
}

mp_obj_t resonite_Component_set_field_value(mp_obj_t ref_id, mp_obj_t name, mp_obj_t value)
{
    return component__set_field_value(
        mp_obj_int_get_uint64_checked(ref_id),
        mp_obj_str_get_str(name),
        (uint8_t *)mp_obj_str_get_data(value, NULL));
}
