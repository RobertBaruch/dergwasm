
#include "resonite_api.h"

#include <stdbool.h>
#include <stdint.h>
#include <emscripten.h>

// C API corresponding to the Resonite API.
// Contains all keepalives for resonite_api.
// This file isn't needed when compiling MicroPython because MicroPython
//   uses the functions.
// Autogenerated by generate_api.py. DO NOT EDIT.

EMSCRIPTEN_KEEPALIVE resonite_refid_t _slot__root_slot() {
    return slot__root_slot();
}
EMSCRIPTEN_KEEPALIVE resonite_refid_t _slot__get_parent(
    resonite_refid_t slot) {
    return slot__get_parent(slot);
}
EMSCRIPTEN_KEEPALIVE resonite_refid_t _slot__get_active_user(
    resonite_refid_t slot) {
    return slot__get_active_user(slot);
}
EMSCRIPTEN_KEEPALIVE resonite_refid_t _slot__get_active_user_root(
    resonite_refid_t slot) {
    return slot__get_active_user_root(slot);
}
EMSCRIPTEN_KEEPALIVE resonite_refid_t _slot__get_object_root(
    resonite_refid_t slot, 
    bool only_explicit) {
    return slot__get_object_root(slot, only_explicit);
}
EMSCRIPTEN_KEEPALIVE char * _slot__get_name(
    resonite_refid_t slot) {
    return slot__get_name(slot);
}
EMSCRIPTEN_KEEPALIVE void _slot__set_name(
    resonite_refid_t slot, 
    char * name) {
    slot__set_name(slot, name);
}
EMSCRIPTEN_KEEPALIVE int32_t _slot__get_num_children(
    resonite_refid_t slot) {
    return slot__get_num_children(slot);
}
EMSCRIPTEN_KEEPALIVE resonite_refid_t _slot__get_child(
    resonite_refid_t slot, 
    int32_t index) {
    return slot__get_child(slot, index);
}
EMSCRIPTEN_KEEPALIVE resonite_refid_t _slot__find_child_by_name(
    resonite_refid_t slot, 
    char * namePtr, 
    bool match_substring, 
    bool ignore_case, 
    int32_t max_depth) {
    return slot__find_child_by_name(slot, namePtr, match_substring, ignore_case, max_depth);
}
EMSCRIPTEN_KEEPALIVE resonite_refid_t _slot__find_child_by_tag(
    resonite_refid_t slot, 
    char * tagPtr, 
    int32_t max_depth) {
    return slot__find_child_by_tag(slot, tagPtr, max_depth);
}
EMSCRIPTEN_KEEPALIVE resonite_refid_t _slot__get_component(
    resonite_refid_t slot, 
    char * typeNamePtr) {
    return slot__get_component(slot, typeNamePtr);
}
EMSCRIPTEN_KEEPALIVE char * _component__get_type_name(
    resonite_refid_t component_id) {
    return component__get_type_name(component_id);
}
EMSCRIPTEN_KEEPALIVE int32_t _component__get_member(
    resonite_refid_t componentRefId, 
    char * namePtr, 
    void * outTypePtr, 
    void * outRefIdPtr) {
    return component__get_member(componentRefId, namePtr, outTypePtr, outRefIdPtr);
}
EMSCRIPTEN_KEEPALIVE int32_t _value__get_int(
    resonite_refid_t refId, 
    void * outPtr) {
    return value__get_int(refId, outPtr);
}
EMSCRIPTEN_KEEPALIVE int32_t _value__get_float(
    resonite_refid_t refId, 
    void * outPtr) {
    return value__get_float(refId, outPtr);
}
EMSCRIPTEN_KEEPALIVE int32_t _value__get_double(
    resonite_refid_t refId, 
    void * outPtr) {
    return value__get_double(refId, outPtr);
}
EMSCRIPTEN_KEEPALIVE int32_t _value__set_int(
    resonite_refid_t refId, 
    void * inPtr) {
    return value__set_int(refId, inPtr);
}
EMSCRIPTEN_KEEPALIVE int32_t _value__set_float(
    resonite_refid_t refId, 
    void * inPtr) {
    return value__set_float(refId, inPtr);
}
EMSCRIPTEN_KEEPALIVE int32_t _value__set_double(
    resonite_refid_t refId, 
    void * inPtr) {
    return value__set_double(refId, inPtr);
}
