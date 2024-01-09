#include "mp_resonite_slot.h"

#include <string.h>

#include "py/obj.h"
#include "py/runtime.h"

STATIC MP_DEFINE_CONST_FUN_OBJ_1(resonite_Slot_root_slot_fun_obj, resonite_Slot_root_slot);
STATIC MP_DEFINE_CONST_CLASSMETHOD_OBJ(resonite_Slot_root_slot_obj, MP_ROM_PTR(&resonite_Slot_root_slot_fun_obj));
STATIC MP_DEFINE_CONST_FUN_OBJ_1(resonite_Slot_get_parent_obj, resonite_Slot_get_parent);
STATIC MP_DEFINE_CONST_FUN_OBJ_KW(resonite_Slot_get_object_root_obj, 1, resonite_Slot_get_object_root);
STATIC MP_DEFINE_CONST_FUN_OBJ_1(resonite_Slot_get_name_obj, resonite_Slot_get_name);
STATIC MP_DEFINE_CONST_FUN_OBJ_2(resonite_Slot_set_name_obj, resonite_Slot_set_name);
STATIC MP_DEFINE_CONST_FUN_OBJ_1(resonite_Slot_children_count_obj, resonite_Slot_children_count);
STATIC MP_DEFINE_CONST_FUN_OBJ_2(resonite_Slot_get_child_obj, resonite_Slot_get_child);
STATIC MP_DEFINE_CONST_FUN_OBJ_KW(resonite_Slot_find_child_by_name_obj, 2, resonite_Slot_find_child_by_name);
STATIC MP_DEFINE_CONST_FUN_OBJ_KW(resonite_Slot_find_child_by_tag_obj, 2, resonite_Slot_find_child_by_tag);

// This collects all methods and other static class attributes of Slot.
// The table structure is similar to the module table, as detailed below.
STATIC const mp_rom_map_elem_t resonite_Slot_locals_dict_table[] = {
    { MP_ROM_QSTR(MP_QSTR_root_slot), MP_ROM_PTR(&resonite_Slot_root_slot_obj) },
    { MP_ROM_QSTR(MP_QSTR_get_parent), MP_ROM_PTR(&resonite_Slot_get_parent_obj) },
    { MP_ROM_QSTR(MP_QSTR_get_object_root), MP_ROM_PTR(&resonite_Slot_get_object_root_obj) },
    { MP_ROM_QSTR(MP_QSTR_get_name), MP_ROM_PTR(&resonite_Slot_get_name_obj) },
    { MP_ROM_QSTR(MP_QSTR_set_name), MP_ROM_PTR(&resonite_Slot_set_name_obj) },
    { MP_ROM_QSTR(MP_QSTR_children_count), MP_ROM_PTR(&resonite_Slot_children_count_obj) },
    { MP_ROM_QSTR(MP_QSTR_get_child), MP_ROM_PTR(&resonite_Slot_get_child_obj) },
    { MP_ROM_QSTR(MP_QSTR_find_child_by_name), MP_ROM_PTR(&resonite_Slot_find_child_by_name_obj) },
    { MP_ROM_QSTR(MP_QSTR_find_child_by_tag), MP_ROM_PTR(&resonite_Slot_find_child_by_tag_obj) },
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
