#ifndef __RESONITE_RESONITE_SLOT_H__
#define __RESONITE_RESONITE_SLOT_H__

#include "py/obj.h"
#include "py/runtime.h"

// Wrapper for slot-related functions in C.

extern mp_obj_t resonite_Slot_root_slot();
extern mp_obj_t resonite_Slot_get_parent(mp_obj_t ref_id);
extern mp_obj_t resonite_Slot_get_object_root(mp_obj_t ref_id, mp_obj_t only_explicit);
extern mp_obj_t resonite_Slot_get_name(mp_obj_t ref_id);
extern mp_obj_t resonite_Slot_set_name(mp_obj_t ref_id, mp_obj_t name);
extern mp_obj_t resonite_Slot_children_count(mp_obj_t ref_id);
extern mp_obj_t resonite_Slot_get_child(mp_obj_t ref_id, mp_obj_t index);
extern mp_obj_t resonite_Slot_find_child_by_name(size_t n_args, const mp_obj_t *args);
extern mp_obj_t resonite_Slot_find_child_by_tag(mp_obj_t ref_id,
    mp_obj_t tag, mp_obj_t max_depth);
extern mp_obj_t resonite_Slot_get_active_user(mp_obj_t ref_id);
extern mp_obj_t resonite_Slot_get_active_user_root(mp_obj_t ref_id);
extern mp_obj_t resonite_Slot_get_component(mp_obj_t ref_id, mp_obj_t component_type_name);

#endif // __RESONITE_RESONITE_SLOT_H__
