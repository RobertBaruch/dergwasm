#include "mp_resonite_slot.h"

#include <string.h>

#include "py/obj.h"
#include "py/runtime.h"

STATIC MP_DEFINE_CONST_FUN_OBJ_1(resonite_Slot_root_slot_fun_obj, resonite_Slot_root_slot);
STATIC MP_DEFINE_CONST_CLASSMETHOD_OBJ(resonite_Slot_root_slot_obj, MP_ROM_PTR(&resonite_Slot_root_slot_fun_obj));

STATIC MP_DEFINE_CONST_FUN_OBJ_1(resonite_Slot_get_parent_obj, resonite_Slot_get_parent);

STATIC MP_DEFINE_CONST_FUN_OBJ_KW(resonite_Slot_get_object_root_obj, 1, resonite_Slot_get_object_root);

STATIC MP_DEFINE_CONST_FUN_OBJ_1(resonite_Slot_get_name_obj, resonite_Slot_get_name);

// This collects all methods and other static class attributes of Slot.
// The table structure is similar to the module table, as detailed below.
STATIC const mp_rom_map_elem_t resonite_Slot_locals_dict_table[] = {
    { MP_ROM_QSTR(MP_QSTR_root_slot), MP_ROM_PTR(&resonite_Slot_root_slot_obj) },
    { MP_ROM_QSTR(MP_QSTR_get_parent), MP_ROM_PTR(&resonite_Slot_get_parent_obj) },
    { MP_ROM_QSTR(MP_QSTR_get_object_root), MP_ROM_PTR(&resonite_Slot_get_object_root_obj) },
    { MP_ROM_QSTR(MP_QSTR_get_name), MP_ROM_PTR(&resonite_Slot_get_name_obj) },
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
    print, resonite_Slot_print,
    locals_dict, &resonite_Slot_locals_dict
);

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
