#include "py/obj.h"
#include "py/runtime.h"

#include "mp_resonite_slot.h"

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
    .globals = (mp_obj_dict_t*)&resonite_module_globals,
};

// Register the module to make it available in Python.
MP_REGISTER_MODULE(MP_QSTR_resonite, resonite_user_cmodule);
