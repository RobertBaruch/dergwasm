#ifndef __RESONITE_RESONITE_API_H__
#define __RESONITE_RESONITE_API_H__

// A slot is represented by its ReferenceID, an unsigned 64-bit value.
typedef unsigned long resonite_slot_t;
// A user is represented by its ReferenceID, an unsigned 64-bit value.
typedef unsigned long resonite_user_t;
// A user root is represented by its ReferenceID, an unsigned 64-bit value.
typedef unsigned long resonite_user_root_t;
typedef unsigned long resonite_component_t;

//----------------------------------------------------------------------
// Slot functions.
//----------------------------------------------------------------------

// Returns the active user for the given slot.
//
// ProtoFlux equivalent: Users/GetActiveUser, Slots/GetActiveUser
// FrooxEngine equivalent: Slot.ActiveUser
extern resonite_user_t slot__get_active_user(resonite_slot_t slot);

// Returns the active user root for the given slot.
//
// ProtoFlux equivalent: Users/GetActiveUserRoot, Slots/GetActiveUserRoot
// FrooxEngine equivalent: Slot.ActiveUserRoot
extern resonite_user_root_t slot__get_active_user_root(resonite_slot_t slot);

// Returns the object root for the given slot.
//
// ProtoFlux equivalent: Slots/GetObjectRoot
// FrooxEngine equivalent: Slot.GetObjectRoot
extern resonite_slot_t slot__get_object_root(resonite_slot_t slot, int only_explicit);

// Returns the parent slot for the given slot.
//
// ProtoFlux equivalent: Slots/GetParentSlot
// FrooxEngine equivalent: Slot.Parent
extern resonite_slot_t slot__get_parent(resonite_slot_t slot);

//----------------------------------------------------------------------
// ValueField functions.
//----------------------------------------------------------------------

// Sets the value of a ValueField<Bool>.
//
// ProtoFlux equivalent: ValueFields/SetBoolValue
// FrooxEngine equivalent: ValueField<Bool>.Value.Value
external void value_field__bool__set_value(resonite_component_t value_field, int value);

// Gets the value of a ValueField<Bool>.
//
// ProtoFlux equivalent: ValueFields/SetBoolValue
// FrooxEngine equivalent: ValueField<Bool>.Value
external int value_field__bool__get_value(resonite_component_t value_field);

#endif // __RESONITE_RESONITE_API_H__