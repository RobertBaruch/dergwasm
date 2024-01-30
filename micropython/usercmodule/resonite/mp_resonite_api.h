
#ifndef MICROPY_INCLUDED_USERCMODULE_RESONITE_API_H
#define MICROPY_INCLUDED_USERCMODULE_RESONITE_API_H

#include "py/obj.h"
#include "py/runtime.h"

// Wrapper for slot-related functions in C.

extern mp_obj_t resonite__slot__root_slot();
extern mp_obj_t resonite__slot__get_parent(mp_obj_t slot);
extern mp_obj_t resonite__slot__get_active_user(mp_obj_t slot);
extern mp_obj_t resonite__slot__get_active_user_root(mp_obj_t slot);
extern mp_obj_t resonite__slot__get_object_root(mp_obj_t slot, mp_obj_t only_explicit);
extern mp_obj_t resonite__slot__get_name(mp_obj_t slot);
extern mp_obj_t resonite__slot__set_name(mp_obj_t slot, mp_obj_t name);
extern mp_obj_t resonite__slot__get_num_children(mp_obj_t slot);
extern mp_obj_t resonite__slot__get_child(mp_obj_t slot, mp_obj_t index);
extern mp_obj_t resonite__slot__find_child_by_name(mp_obj_t slot, mp_obj_t namePtr, mp_obj_t match_substring, mp_obj_t ignore_case, mp_obj_t max_depth);
extern mp_obj_t resonite__slot__find_child_by_tag(mp_obj_t slot, mp_obj_t tagPtr, mp_obj_t max_depth);
extern mp_obj_t resonite__slot__get_component(mp_obj_t slot, mp_obj_t typeNamePtr);
extern mp_obj_t resonite__component__get_type_name(mp_obj_t component_id);
extern mp_obj_t resonite__component__get_member(mp_obj_t componentRefId, mp_obj_t namePtr, mp_obj_t outTypePtr, mp_obj_t outRefIdPtr);
extern mp_obj_t resonite__value__get_int(mp_obj_t refId, mp_obj_t outPtr);
extern mp_obj_t resonite__value__get_float(mp_obj_t refId, mp_obj_t outPtr);
extern mp_obj_t resonite__value__get_double(mp_obj_t refId, mp_obj_t outPtr);
extern mp_obj_t resonite__value__set_int(mp_obj_t refId, mp_obj_t inPtr);
extern mp_obj_t resonite__value__set_float(mp_obj_t refId, mp_obj_t inPtr);
extern mp_obj_t resonite__value__set_double(mp_obj_t refId, mp_obj_t inPtr);

#endif // MICROPY_INCLUDED_USERCMODULE_RESONITE_API_H
