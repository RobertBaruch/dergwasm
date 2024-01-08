// Contains all keepalives for resonite_slot_api.

// This file isn't needed when compiling MicroPython because MicroPython uses the functions.

#include "resonite_slot_api.h"

#include <stdint.h>
#include <emscripten.h>

EMSCRIPTEN_KEEPALIVE resonite_slot_refid_t _slot__root_slot() {
	return slot__root_slot();
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
