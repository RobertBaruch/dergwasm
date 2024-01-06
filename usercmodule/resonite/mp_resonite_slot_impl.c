#include "mp_resonite_slot.h"

#include <string.h>

#include "py/obj.h"
#include "py/runtime.h"
#include "resonite_api.h"

mp_obj_t resonite_create_slot(resonite_slot_refid_t reference_id) {
    resonite_Slot_obj_t *self = mp_obj_malloc(resonite_Slot_obj_t, &resonite_Slot_type);
    self->reference_id = reference_id;
    return MP_OBJ_FROM_PTR(self);
}

void resonite_Slot_print(const mp_print_t *print, mp_obj_t self_in, mp_print_kind_t kind) {
	resonite_Slot_obj_t *self = MP_OBJ_TO_PTR(self_in);
	mp_printf(print, "Slot(ID=%ul)", self->reference_id);
}

mp_obj_t resonite_Slot_make_new(const mp_obj_type_t *type, size_t n_args, size_t n_kw, const mp_obj_t *args) {
    resonite_slot_refid_t reference_id = 0;
    // args[0] is reference_id_lo
    reference_id = (uint64_t)mp_obj_get_int(args[0]); // mp_int_t is an int under the WASM port.
    // args[1] is reference_id_hi
    reference_id |= (uint64_t)mp_obj_get_int(args[1]) << 32; // mp_int_t is an int under the WASM port.
    return resonite_create_slot(reference_id);
}

mp_obj_t resonite_Slot_root_slot(mp_obj_t cls_in) {
    resonite_slot_refid_t id;
    slot__root_slot(&id);
    return resonite_create_slot(id);
}

mp_obj_t resonite_Slot_get_parent(mp_obj_t self_in) {
    resonite_Slot_obj_t *self = MP_OBJ_TO_PTR(self_in);
    resonite_slot_refid_t id;
    slot__get_parent(self->reference_id, &id);
    return resonite_create_slot(id);
}

mp_obj_t resonite_Slot_get_object_root(size_t n_args, const mp_obj_t *pos_args, mp_map_t *kw_args) {
    static const mp_arg_t allowed_args[] = {
		{ MP_QSTR_only_explicit, MP_ARG_BOOL, {.u_bool = false} },
	};
    resonite_Slot_obj_t *self = MP_OBJ_TO_PTR(pos_args[0]);

    mp_arg_val_t args[MP_ARRAY_SIZE(allowed_args)];
    mp_arg_parse_all(n_args - 1, pos_args + 1, kw_args, MP_ARRAY_SIZE(allowed_args), allowed_args, args);
    int only_explicit = args[0].u_bool ? 1 : 0;

    resonite_slot_refid_t id;
    slot__get_object_root(self->reference_id, only_explicit, &id);
    return resonite_create_slot(id);
}

mp_obj_t resonite_Slot_get_name(mp_obj_t self_in) {
	resonite_Slot_obj_t *self = MP_OBJ_TO_PTR(self_in);
	char* name = slot__get_name(self->reference_id);
    return mp_obj_new_str(name, strlen(name));
}
