#include "mp_resonite_component.h"

#include <string.h>

#include "py/obj.h"
#include "py/runtime.h"
#include "resonite_component_api.h"
#include "mp_resonite_utils.h"

mp_obj_t resonite_Component_get_type_name(mp_obj_t ref_id) {
    char* name = component__get_type_name(
        mp_obj_int_get_uint64_checked(ref_id));
    return mp_obj_new_str(name, strlen(name));
}
