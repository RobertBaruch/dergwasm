#ifndef __RESONITE_RESONITE_API_H__
#define __RESONITE_RESONITE_API_H__

// Reference IDs cannot be converted back to their objects unless they exist
// in the ResoniteEnv dictionaries.
typedef unsigned long resonite_slot_refid_t;
typedef unsigned long resonite_user_refid_t;
typedef unsigned long resonite_user_root_refid_t;
typedef unsigned long resonite_component_refid_t;
// A NUL-terminated UTF-8-encoded string. The "pointer" is the index into WASM's heap where
// the string has been malloced.
//
// TODO: Export malloc and free.
typedef unsigned int string_ptr_t;

//----------------------------------------------------------------------
// Slot functions.
//----------------------------------------------------------------------

// Returns the active user for the given slot.
//
// ProtoFlux equivalent: Users/GetActiveUser, Slots/GetActiveUser
// FrooxEngine equivalent: Slot.ActiveUser
extern resonite_user_refid_t slot__get_active_user(resonite_slot_refid_t slot_id);

// Returns the active user root for the given slot.
//
// ProtoFlux equivalent: Users/GetActiveUserRoot, Slots/GetActiveUserRoot
// FrooxEngine equivalent: Slot.ActiveUserRoot
extern resonite_user_root_refid_t slot__get_active_user_root(resonite_slot_refid_t slot_id);

// Returns the object root for the given slot.
//
// ProtoFlux equivalent: Slots/GetObjectRoot
// FrooxEngine equivalent: Slot.GetObjectRoot
extern resonite_slot_refid_t slot__get_object_root(resonite_slot_refid_t slot_id, int only_explicit);

// Returns the parent slot for the given slot.
//
// ProtoFlux equivalent: Slots/GetParentSlot
// FrooxEngine equivalent: Slot.Parent
extern resonite_slot_refid_t slot__get_parent(resonite_slot_refid_t slot_id);

//----------------------------------------------------------------------
// ValueField functions.
//----------------------------------------------------------------------

// Sets the value of a ValueField<Bool>.
//
// ProtoFlux equivalent: ValueFields/SetBoolValue
// FrooxEngine equivalent: ValueField<Bool>.Value.Value
external void value_field__bool__set_value(resonite_component_refid_t value_field_id, int value);

// Gets the value of a ValueField<Bool>.
//
// ProtoFlux equivalent: ValueFields/SetBoolValue
// FrooxEngine equivalent: ValueField<Bool>.Value
external int value_field__bool__get_value(resonite_component_refid_t value_field_id);

#endif // __RESONITE_RESONITE_API_H__
