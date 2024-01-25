#include "mp_resonite_slot.h"

#include <string.h>

#include "py/obj.h"
#include "py/runtime.h"
#include "resonite_slot_api.h"
#include "mp_resonite_utils.h"

mp_obj_t resonite_Slot_root_slot() {
    return mp_obj_new_int_from_ll(slot__root_slot());
}

mp_obj_t resonite_Slot_get_parent(mp_obj_t ref_id) {
    return mp_obj_new_int_from_ll(slot__get_parent(
        mp_obj_int_get_uint64_checked(ref_id)));
}

mp_obj_t resonite_Slot_get_object_root(mp_obj_t ref_id, mp_obj_t only_explicit) {
    return mp_obj_new_int_from_ll(slot__get_object_root(
        mp_obj_int_get_uint64_checked(ref_id),
        mp_obj_is_true(only_explicit) ? 1 : 0));
}

mp_obj_t resonite_Slot_get_name(mp_obj_t ref_id) {
    char* name = slot__get_name(
        mp_obj_int_get_uint64_checked(ref_id));
    return mp_obj_new_str(name, strlen(name));
}

mp_obj_t resonite_Slot_set_name(mp_obj_t ref_id, mp_obj_t name) {
    slot__set_name(
        mp_obj_int_get_uint64_checked(ref_id),
        mp_obj_str_get_str(name));
    return mp_const_none;
}

mp_obj_t resonite_Slot_children_count(mp_obj_t ref_id) {
    return mp_obj_new_int(slot__get_num_children(
        mp_obj_int_get_uint64_checked(ref_id)));
}

mp_obj_t resonite_Slot_get_child(mp_obj_t ref_id, mp_obj_t index) {
    return mp_obj_new_int_from_ll(slot__get_child(
        mp_obj_int_get_uint64_checked(ref_id),
        mp_obj_get_int(index)));
}

mp_obj_t resonite_Slot_find_child_by_name(size_t n_args, const mp_obj_t *args) {
    return mp_obj_new_int_from_ll(slot__find_child_by_name(
        mp_obj_int_get_uint64_checked(args[0]), // ref_id
        mp_obj_str_get_str(args[1]), // name
        mp_obj_is_true(args[2]) ? 1 : 0, // match_substring
        mp_obj_is_true(args[3]) ? 1 : 0, // ignore_case
        mp_obj_get_int(args[4]))); // max_depth
}

mp_obj_t resonite_Slot_find_child_by_tag(
        mp_obj_t ref_id, mp_obj_t tag, mp_obj_t max_depth) {
    return mp_obj_new_int_from_ll(slot__find_child_by_tag(
        mp_obj_int_get_uint64_checked(ref_id),
        mp_obj_str_get_str(tag),
        mp_obj_get_int(max_depth)));
}

mp_obj_t resonite_Slot_get_active_user(mp_obj_t ref_id) {
    return mp_obj_new_int_from_ll(slot__get_active_user(
        mp_obj_int_get_uint64_checked(ref_id)));
}

mp_obj_t resonite_Slot_get_active_user_root(mp_obj_t ref_id) {
    return mp_obj_new_int_from_ll(slot__get_active_user_root(
        mp_obj_int_get_uint64_checked(ref_id)));
}

mp_obj_t resonite_Slot_get_component(mp_obj_t ref_id, mp_obj_t component_type_name) {
    return mp_obj_new_int_from_ll(slot__get_component(
        mp_obj_int_get_uint64_checked(ref_id),
        mp_obj_str_get_str(component_type_name)));
}
