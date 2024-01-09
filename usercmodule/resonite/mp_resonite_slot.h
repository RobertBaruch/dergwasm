#ifndef __RESONITE_RESONITE_SLOT_H__
#define __RESONITE_RESONITE_SLOT_H__

#include "py/obj.h"
#include "py/runtime.h"
#include "resonite_api_types.h"

// This structure represents Slot instance objects.
typedef struct _resonite_Slot_obj_t {
    // All objects start with the base.
    mp_obj_base_t base;
    // This is the IWorldElement.ReferenceID of the slot.
    resonite_slot_refid_t reference_id;
} resonite_Slot_obj_t;

extern const mp_obj_type_t resonite_Slot_type;

// Creates a Slot object from a reference ID.
extern mp_obj_t resonite_new_Slot(resonite_slot_refid_t reference_id);
// Creates a Slot object from a reference ID, or None if the reference ID is 0.
extern mp_obj_t resonite_new_Slot_or_none(resonite_slot_refid_t reference_id);

// This represents Slot.__init__.
extern mp_obj_t resonite_Slot_make_new(const mp_obj_type_t* type, size_t n_args, size_t n_kw, const mp_obj_t* args);
extern void resonite_Slot_print(const mp_print_t* print, mp_obj_t self_in, mp_print_kind_t kind);
extern mp_obj_t resonite_Slot_root_slot(mp_obj_t cls_in);
extern mp_obj_t resonite_Slot_get_parent(mp_obj_t self_in);
extern mp_obj_t resonite_Slot_get_object_root(size_t n_args, const mp_obj_t* pos_args, mp_map_t* kw_args);
extern mp_obj_t resonite_Slot_get_name(mp_obj_t self_in);
extern mp_obj_t resonite_Slot_set_name(mp_obj_t self_in, mp_obj_t name);
extern mp_obj_t resonite_Slot_children_count(mp_obj_t self_in);
extern mp_obj_t resonite_Slot_get_child(mp_obj_t self_in, mp_obj_t index);
extern mp_obj_t resonite_Slot_find_child_by_name(size_t n_args, const mp_obj_t* pos_args, mp_map_t* kw_args);
extern mp_obj_t resonite_Slot_find_child_by_tag(size_t n_args, const mp_obj_t* pos_args, mp_map_t* kw_args);

#endif // __RESONITE_RESONITE_SLOT_H__
