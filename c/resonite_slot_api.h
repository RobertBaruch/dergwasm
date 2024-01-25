#ifndef __RESONITE_RESONITE_SLOT_API_H__
#define __RESONITE_RESONITE_SLOT_API_H__

#include <stdint.h>
#include <emscripten.h>

#include "resonite_api_types.h"

// Slot-related functions that WASM can call.

extern resonite_slot_refid_t slot__root_slot();

// Returns the active user for the given slot.
//
// ProtoFlux equivalent: Users/GetActiveUser, Slots/GetActiveUser
// FrooxEngine equivalent: Slot.ActiveUser
extern resonite_user_refid_t slot__get_active_user(resonite_slot_refid_t slot_id);

// Returns the active user root for the given slot.
//
// ProtoFlux equivalent: Users/GetActiveUserRoot, Slots/GetActiveUserRoot
// FrooxEngine equivalent: Slot.ActiveUserRoot
extern resonite_user_root_refid_t slot__get_active_user_root(
    resonite_slot_refid_t slot_id);

// Returns the object root for the given slot.
//
// ProtoFlux equivalent: Slots/GetObjectRoot
// FrooxEngine equivalent: Slot.GetObjectRoot
extern resonite_slot_refid_t slot__get_object_root(
    resonite_slot_refid_t slot_id, int only_explicit);

// Returns the parent slot for the given slot.
//
// ProtoFlux equivalent: Slots/GetParentSlot
// FrooxEngine equivalent: Slot.Parent
extern resonite_slot_refid_t slot__get_parent(resonite_slot_refid_t slot_id);

// Returns the name for the given slot. The returned name must be freed when
// done with it. Although the return type is not const, changes to the returned
// string will not be reflected in the slot; use slot__set_name() for that.
extern char* slot__get_name(resonite_slot_refid_t slot_id);

// Sets the name for the given slot.
extern void slot__set_name(resonite_slot_refid_t slot_id, const char* name);

extern int slot__get_num_children(resonite_slot_refid_t slot_id);

extern resonite_slot_refid_t slot__get_child(resonite_slot_refid_t slot_id, int index);

extern resonite_slot_refid_t slot__find_child_by_name(resonite_slot_refid_t slot_id,
    const char* name, int match_substring, int ignore_case, int max_depth);

extern resonite_slot_refid_t slot__find_child_by_tag(resonite_slot_refid_t slot_id,
    const char* tag, int max_depth);

extern resonite_component_refid_t slot__get_component(resonite_slot_refid_t slot_id,
    const char* component_type_name);

#endif // __RESONITE_RESONITE_SLOT_API_H__
