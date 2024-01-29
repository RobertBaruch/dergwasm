
#include "mp_resonite_api.h"

#include <string.h>

#include "py/obj.h"
#include "py/runtime.h"
#include "resonite_api.h"
#include "mp_resonite_utils.h"

mp_obj_t resonite__slot__root_slot() {
  return mp_obj_new_int_from_ll(slot__root_slot());
}

mp_obj_t resonite__slot__get_parent(mp_obj_t slot) {
  return mp_obj_new_int_from_ll(slot__get_parent(
    mp_obj_int_get_uint64_checked(slot)));
}

mp_obj_t resonite__slot__get_active_user(mp_obj_t slot) {
  return mp_obj_new_int_from_ll(slot__get_active_user(
    mp_obj_int_get_uint64_checked(slot)));
}

mp_obj_t resonite__slot__get_active_user_root(mp_obj_t slot) {
  return mp_obj_new_int_from_ll(slot__get_active_user_root(
    mp_obj_int_get_uint64_checked(slot)));
}

mp_obj_t resonite__slot__get_object_root(mp_obj_t slot, mp_obj_t only_explicit) {
  return mp_obj_new_int_from_ll(slot__get_object_root(
    mp_obj_int_get_uint64_checked(slot), 
    (int32_t)mp_obj_get_int(only_explicit)));
}

mp_obj_t resonite__slot__get_name(mp_obj_t slot) {
  return mp_obj_new_int_from_ll(slot__get_name(
    mp_obj_int_get_uint64_checked(slot)));
}

mp_obj_t resonite__slot__set_name(mp_obj_t slot, mp_obj_t ptr) {
  slot__set_name(
    mp_obj_int_get_uint64_checked(slot), 
    (int32_t)mp_obj_get_int(ptr));
}

mp_obj_t resonite__slot__get_num_children(mp_obj_t slot) {
  return mp_obj_new_int_from_ll(slot__get_num_children(
    mp_obj_int_get_uint64_checked(slot)));
}

mp_obj_t resonite__slot__get_child(mp_obj_t slot, mp_obj_t index) {
  return mp_obj_new_int_from_ll(slot__get_child(
    mp_obj_int_get_uint64_checked(slot), 
    (int32_t)mp_obj_get_int(index)));
}

mp_obj_t resonite__slot__find_child_by_name(mp_obj_t slot, mp_obj_t namePtr, mp_obj_t match_substring, mp_obj_t ignore_case, mp_obj_t max_depth) {
  return mp_obj_new_int_from_ll(slot__find_child_by_name(
    mp_obj_int_get_uint64_checked(slot), 
    (int32_t)mp_obj_get_int(namePtr), 
    (int32_t)mp_obj_get_int(match_substring), 
    (int32_t)mp_obj_get_int(ignore_case), 
    (int32_t)mp_obj_get_int(max_depth)));
}

mp_obj_t resonite__slot__find_child_by_tag(mp_obj_t slot, mp_obj_t tagPtr, mp_obj_t max_depth) {
  return mp_obj_new_int_from_ll(slot__find_child_by_tag(
    mp_obj_int_get_uint64_checked(slot), 
    (int32_t)mp_obj_get_int(tagPtr), 
    (int32_t)mp_obj_get_int(max_depth)));
}

mp_obj_t resonite__slot__get_component(mp_obj_t slot, mp_obj_t typeNamePtr) {
  return mp_obj_new_int_from_ll(slot__get_component(
    mp_obj_int_get_uint64_checked(slot), 
    (int32_t)mp_obj_get_int(typeNamePtr)));
}

mp_obj_t resonite__component__get_type_name(mp_obj_t component_id) {
  return mp_obj_new_int_from_ll(component__get_type_name(
    mp_obj_int_get_uint64_checked(component_id)));
}

mp_obj_t resonite__component__get_member(mp_obj_t componentRefId, mp_obj_t namePtr, mp_obj_t outTypePtr, mp_obj_t outRefIdPtr) {
  return mp_obj_new_int_from_ll(component__get_member(
    mp_obj_int_get_uint64_checked(componentRefId), 
    (int32_t)mp_obj_get_int(namePtr), 
    (int32_t)mp_obj_get_int(outTypePtr), 
    (int32_t)mp_obj_get_int(outRefIdPtr)));
}

mp_obj_t resonite__value__get_int(mp_obj_t refId, mp_obj_t outPtr) {
  return mp_obj_new_int_from_ll(value__get_int(
    mp_obj_int_get_uint64_checked(refId), 
    (int32_t)mp_obj_get_int(outPtr)));
}

mp_obj_t resonite__value__get_float(mp_obj_t refId, mp_obj_t outPtr) {
  return mp_obj_new_int_from_ll(value__get_float(
    mp_obj_int_get_uint64_checked(refId), 
    (int32_t)mp_obj_get_int(outPtr)));
}

mp_obj_t resonite__value__get_double(mp_obj_t refId, mp_obj_t outPtr) {
  return mp_obj_new_int_from_ll(value__get_double(
    mp_obj_int_get_uint64_checked(refId), 
    (int32_t)mp_obj_get_int(outPtr)));
}

mp_obj_t resonite__value__set_int(mp_obj_t refId, mp_obj_t inPtr) {
  return mp_obj_new_int_from_ll(value__set_int(
    mp_obj_int_get_uint64_checked(refId), 
    (int32_t)mp_obj_get_int(inPtr)));
}

mp_obj_t resonite__value__set_float(mp_obj_t refId, mp_obj_t inPtr) {
  return mp_obj_new_int_from_ll(value__set_float(
    mp_obj_int_get_uint64_checked(refId), 
    (int32_t)mp_obj_get_int(inPtr)));
}

mp_obj_t resonite__value__set_double(mp_obj_t refId, mp_obj_t inPtr) {
  return mp_obj_new_int_from_ll(value__set_double(
    mp_obj_int_get_uint64_checked(refId), 
    (int32_t)mp_obj_get_int(inPtr)));
}

