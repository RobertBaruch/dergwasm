// Contains all keepalives for resonite_slot_api.

// This file isn't needed when compiling MicroPython because MicroPython uses the functions.

#include "resonite_slot_api.h"

#include <stdint.h>
#include <emscripten.h>

EMSCRIPTEN_KEEPALIVE resonite_slot_refid_t _slot__root_slot() {
	return slot__root_slot();
}

EMSCRIPTEN_KEEPALIVE resonite_user_refid_t _slot__get_active_user(
	resonite_slot_refid_t slot_id) {
	return slot__get_active_user(slot_id);
}

EMSCRIPTEN_KEEPALIVE resonite_user_root_refid_t _slot__get_active_user_root(
	resonite_slot_refid_t slot_id) {
	return slot__get_active_user_root(slot_id);
}

EMSCRIPTEN_KEEPALIVE resonite_slot_refid_t _slot__get_object_root(
	resonite_slot_refid_t slot_id, int only_explicit) {
	return slot__get_object_root(slot_id, only_explicit);
}

EMSCRIPTEN_KEEPALIVE resonite_slot_refid_t _slot__get_parent(
    resonite_slot_refid_t slot_id) {
    return slot__get_parent(slot_id);
}

EMSCRIPTEN_KEEPALIVE char* _slot__get_name(resonite_slot_refid_t slot_id) {
	return slot__get_name(slot_id);
}

EMSCRIPTEN_KEEPALIVE void _slot__set_name(resonite_slot_refid_t slot_id, const char* name) {
	slot__set_name(slot_id, name);
}

EMSCRIPTEN_KEEPALIVE int _slot__get_num_children(resonite_slot_refid_t slot_id) {
	return slot__get_num_children(slot_id);
}

EMSCRIPTEN_KEEPALIVE resonite_slot_refid_t _slot__get_child(
	resonite_slot_refid_t slot_id, int index) {
	return slot__get_child(slot_id, index);
}

EMSCRIPTEN_KEEPALIVE resonite_slot_refid_t _slot__find_child_by_name(
	resonite_slot_refid_t slot_id, const char* name, int match_substring,
	int ignore_case, int max_depth) {
	return slot__find_child_by_name(slot_id, name, match_substring, ignore_case, max_depth);
}

EMSCRIPTEN_KEEPALIVE resonite_slot_refid_t _slot__find_child_by_tag(
	resonite_slot_refid_t slot_id, const char* tag, int max_depth) {
	return slot__find_child_by_tag(slot_id, tag, max_depth);
}

EMSCRIPTEN_KEEPALIVE resonite_component_refid_t _slot__get_component(
	resonite_slot_refid_t slot_id, const char* component_type_name) {
	return slot__get_component(slot_id, component_type_name);
}
