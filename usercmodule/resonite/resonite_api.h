#ifndef __RESONITE_RESONITE_API_H__
#define __RESONITE_RESONITE_API_H__

#include <stdint.h>
#include <emscripten.h>

// This file contains the WASM API for Resonite.

typedef uint64_t resonite_slot_refid_t;
typedef uint64_t resonite_user_refid_t;
typedef uint64_t resonite_user_root_refid_t;
typedef uint64_t resonite_component_refid_t;

//----------------------------------------------------------------------
// Slot functions.
//----------------------------------------------------------------------

extern void slot__root_slot(resonite_slot_refid_t* slot_id);
EMSCRIPTEN_KEEPALIVE void _slot__root_slot(resonite_slot_refid_t* slot_id) {
	slot__root_slot(slot_id);
}

// Returns the active user for the given slot.
//
// ProtoFlux equivalent: Users/GetActiveUser, Slots/GetActiveUser
// FrooxEngine equivalent: Slot.ActiveUser
extern void slot__get_active_user(resonite_slot_refid_t slot_id, resonite_user_refid_t* user_id);

// Returns the active user root for the given slot.
//
// ProtoFlux equivalent: Users/GetActiveUserRoot, Slots/GetActiveUserRoot
// FrooxEngine equivalent: Slot.ActiveUserRoot
extern void slot__get_active_user_root(
    resonite_slot_refid_t slot_id,
    resonite_user_root_refid_t* user_root_id);

// Returns the object root for the given slot.
//
// ProtoFlux equivalent: Slots/GetObjectRoot
// FrooxEngine equivalent: Slot.GetObjectRoot
extern void slot__get_object_root(
    resonite_slot_refid_t slot_id, int only_explicit, resonite_slot_refid_t* object_root_id);
EMSCRIPTEN_KEEPALIVE void _slot__get_object_root(
	resonite_slot_refid_t slot_id, int only_explicit, resonite_slot_refid_t* object_root_id) {
	slot__get_object_root(slot_id, only_explicit, object_root_id);
}

// Returns the parent slot for the given slot.
//
// ProtoFlux equivalent: Slots/GetParentSlot
// FrooxEngine equivalent: Slot.Parent
extern void slot__get_parent(
    resonite_slot_refid_t slot_id, resonite_slot_refid_t* parent_slot_id);
EMSCRIPTEN_KEEPALIVE void _slot__get_parent(
    resonite_slot_refid_t slot_id, resonite_slot_refid_t* parent_slot_id) {
    slot__get_parent(slot_id, parent_slot_id);
}

// Returns the name for the given slot. The returned name must be freed when
// done with it.
extern char* slot__get_name(resonite_slot_refid_t slot_id);
EMSCRIPTEN_KEEPALIVE char* _slot__get_name(resonite_slot_refid_t slot_id) {
	return slot__get_name(slot_id);
}

//----------------------------------------------------------------------
// ValueField functions.
//----------------------------------------------------------------------

// Sets the value of a ValueField<Bool>.
//
// ProtoFlux equivalent: ValueFields/SetBoolValue
// FrooxEngine equivalent: ValueField<Bool>.Value.Value
extern void value_field__bool__set_value(resonite_component_refid_t value_field_id, int value);

// Gets the value of a ValueField<Bool>.
//
// ProtoFlux equivalent: ValueFields/SetBoolValue
// FrooxEngine equivalent: ValueField<Bool>.Value
extern int value_field__bool__get_value(resonite_component_refid_t value_field_id);

#endif // __RESONITE_RESONITE_API_H__
