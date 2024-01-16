#include "py/obj.h"
#include "py/runtime.h"

#include "mp_resonite_slot.h"

#define MODULE_NAME MP_QSTR_resonitenative

STATIC MP_DEFINE_CONST_FUN_OBJ_0(resonite_Slot_root_slot_obj, resonite_Slot_root_slot);
STATIC MP_DEFINE_CONST_FUN_OBJ_1(resonite_Slot_get_parent_obj, resonite_Slot_get_parent);
STATIC MP_DEFINE_CONST_FUN_OBJ_2(resonite_Slot_get_object_root_obj, resonite_Slot_get_object_root);
STATIC MP_DEFINE_CONST_FUN_OBJ_1(resonite_Slot_get_name_obj, resonite_Slot_get_name);
STATIC MP_DEFINE_CONST_FUN_OBJ_2(resonite_Slot_set_name_obj, resonite_Slot_set_name);
STATIC MP_DEFINE_CONST_FUN_OBJ_1(resonite_Slot_children_count_obj, resonite_Slot_children_count);
STATIC MP_DEFINE_CONST_FUN_OBJ_2(resonite_Slot_get_child_obj, resonite_Slot_get_child);
STATIC MP_DEFINE_CONST_FUN_OBJ_VAR_BETWEEN(resonite_Slot_find_child_by_name_obj, 5, 5, resonite_Slot_find_child_by_name);
STATIC MP_DEFINE_CONST_FUN_OBJ_3(resonite_Slot_find_child_by_tag_obj, resonite_Slot_find_child_by_tag);

STATIC const mp_rom_map_elem_t resonitenative_module_globals_table[] = {
    { MP_ROM_QSTR(MP_QSTR___name__), MP_ROM_QSTR(MODULE_NAME) },
    { MP_ROM_QSTR(MP_QSTR_resonite_Slot_root_slot), MP_ROM_PTR(&resonite_Slot_root_slot_obj) },
    { MP_ROM_QSTR(MP_QSTR_resonite_Slot_get_parent),
        MP_ROM_PTR(&resonite_Slot_get_parent_obj) },
    { MP_ROM_QSTR(MP_QSTR_resonite_Slot_get_object_root),
        MP_ROM_PTR(&resonite_Slot_get_object_root_obj) },
    { MP_ROM_QSTR(MP_QSTR_resonite_Slot_get_name), MP_ROM_PTR(&resonite_Slot_get_name_obj) },
    { MP_ROM_QSTR(MP_QSTR_resonite_Slot_set_name), MP_ROM_PTR(&resonite_Slot_set_name_obj) },
    { MP_ROM_QSTR(MP_QSTR_resonite_Slot_children_count),
        MP_ROM_PTR(&resonite_Slot_children_count_obj) },
    { MP_ROM_QSTR(MP_QSTR_resonite_Slot_get_child), MP_ROM_PTR(&resonite_Slot_get_child_obj) },
    { MP_ROM_QSTR(MP_QSTR_resonite_Slot_find_child_by_name),
        MP_ROM_PTR(&resonite_Slot_find_child_by_name_obj) },
    { MP_ROM_QSTR(MP_QSTR_resonite_Slot_find_child_by_tag),
        MP_ROM_PTR(&resonite_Slot_find_child_by_tag_obj) },
};
STATIC MP_DEFINE_CONST_DICT(resonitenative_module_globals, resonitenative_module_globals_table);

const mp_obj_module_t resonitenative_user_cmodule = {
    .base = { &mp_type_module },
    .globals = (mp_obj_dict_t*)&resonitenative_module_globals,
};

MP_REGISTER_MODULE(MODULE_NAME, resonitenative_user_cmodule);
