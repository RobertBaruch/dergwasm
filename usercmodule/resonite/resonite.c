#include "py/obj.h"
#include "py/runtime.h"
#include "resonite_api.h"

// See https://micropython-usermod.readthedocs.io/
// and https://docs.micropython.org/en/latest/develop/cmodules.html

// Foreward declarations.
mp_obj_t resonite_create_slot(resonite_slot_refid_t reference_id);
const mp_obj_type_t resonite_Slot_type;

// This structure represents Slot instance objects.
typedef struct _resonite_Slot_obj_t {
    // All objects start with the base.
    mp_obj_base_t base;
    // This is the IWorldElement.ReferenceID of the slot.
    resonite_slot_refid_t reference_id;
} resonite_Slot_obj_t;

// This represents Slot.__init__.
STATIC mp_obj_t resonite_Slot_make_new(const mp_obj_type_t *type, size_t n_args, size_t n_kw, const mp_obj_t *args) {
    resonite_slot_refid_t reference_id = 0;
    // args[0] is reference_id_lo
    reference_id = (uint64_t)mp_obj_get_int(args[0]); // mp_int_t is an int under the WASM port.
    // args[1] is reference_id_hi
    reference_id |= (uint64_t)mp_obj_get_int(args[1]) << 32; // mp_int_t is an int under the WASM port.
    return resonite_create_slot(reference_id);
}

STATIC mp_obj_t resonite_Slot_get_parent(mp_obj_t self_in) {
    resonite_Slot_obj_t *self = MP_OBJ_TO_PTR(self_in);
    resonite_slot_refid_t parent_id = slot__get_parent(self->reference_id);
    return resonite_create_slot(parent_id);
}
STATIC MP_DEFINE_CONST_FUN_OBJ_1(resonite_Slot_get_parent_obj, resonite_Slot_get_parent);


// This collects all methods and other static class attributes of Slot.
// The table structure is similar to the module table, as detailed below.
STATIC const mp_rom_map_elem_t resonite_Slot_locals_dict_table[] = {
    { MP_ROM_QSTR(MP_QSTR_get_parent), MP_ROM_PTR(&resonite_Slot_get_parent_obj) },
};
// Create a const dict named resonite_Slot_locals_dict, with content
// resonite_Slot_locals_dict_table.
STATIC MP_DEFINE_CONST_DICT(resonite_Slot_locals_dict, resonite_Slot_locals_dict_table);

// This defines the mp_obj_type_t for the Slot object, called resonite_Slot_type.
MP_DEFINE_CONST_OBJ_TYPE(
    resonite_Slot_type,
    MP_QSTR_Slot,
    MP_TYPE_FLAG_NONE,
    make_new, resonite_Slot_make_new,
    locals_dict, &resonite_Slot_locals_dict
);

// Utility function to create a Slot object from a reference ID.
mp_obj_t resonite_create_slot(resonite_slot_refid_t reference_id) {
    resonite_Slot_obj_t *self = mp_obj_malloc(resonite_Slot_obj_t, &resonite_Slot_type);
    self->reference_id = reference_id;
    return MP_OBJ_FROM_PTR(self);
}

// Define all attributes of the module.
// Table entries are key/value pairs of the attribute name (a string)
// and the MicroPython object reference.
// All identifiers and strings are written as MP_QSTR_xxx and will be
// optimized to word-sized integers by the build system (interned strings).
STATIC const mp_rom_map_elem_t resonite_module_globals_table[] = {
    { MP_ROM_QSTR(MP_QSTR___name__), MP_ROM_QSTR(MP_QSTR_resonite) },
    { MP_ROM_QSTR(MP_QSTR_Slot),     MP_ROM_PTR(&resonite_Slot_type) },
};
// Create a const dict named resonite_module_globals, with content resonite_module_globals_table.
STATIC MP_DEFINE_CONST_DICT(resonite_module_globals, resonite_module_globals_table);


// Define module object.
const mp_obj_module_t resonite_user_cmodule = {
    .base = { &mp_type_module },
    .globals = (mp_obj_dict_t *)&resonite_module_globals,
};

// Register the module to make it available in Python.
MP_REGISTER_MODULE(MP_QSTR_resonite, resonite_user_cmodule);
