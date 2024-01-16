#include "py/obj.h"
#include "py/runtime.h"

#include "mp_resonite_component.h"
#include "mp_resonite_slot.h"

#define MODULE_NAME MP_QSTR_resonitenative
#define DEF_FUN(args, name) STATIC MP_DEFINE_CONST_FUN_OBJ_ ## args(name ## _obj, name)
#define DEF_ENTRY(name) { MP_ROM_QSTR(MP_QSTR_ ## name), MP_ROM_PTR(&name ## _obj) }

DEF_FUN(1, resonite_Component_get_type_name);

DEF_FUN(0, resonite_Slot_root_slot);
DEF_FUN(1, resonite_Slot_get_parent);
DEF_FUN(2, resonite_Slot_get_object_root);
DEF_FUN(1, resonite_Slot_get_name);
DEF_FUN(2, resonite_Slot_set_name);
DEF_FUN(1, resonite_Slot_children_count);
DEF_FUN(2, resonite_Slot_get_child);
DEF_FUN(3, resonite_Slot_find_child_by_tag);
DEF_FUN(1, resonite_Slot_get_active_user);
DEF_FUN(1, resonite_Slot_get_active_user_root);
STATIC MP_DEFINE_CONST_FUN_OBJ_VAR_BETWEEN(resonite_Slot_find_child_by_name_obj, 5, 5,
    resonite_Slot_find_child_by_name);
DEF_FUN(2, resonite_Slot_get_component);

STATIC const mp_rom_map_elem_t resonitenative_module_globals_table[] = {
    { MP_ROM_QSTR(MP_QSTR___name__), MP_ROM_QSTR(MODULE_NAME) },
    DEF_ENTRY(resonite_Component_get_type_name),
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
    { MP_ROM_QSTR(MP_QSTR_resonite_Slot_get_active_user),
        MP_ROM_PTR(&resonite_Slot_get_active_user_obj) },
    { MP_ROM_QSTR(MP_QSTR_resonite_Slot_get_active_user_root),
        MP_ROM_PTR(&resonite_Slot_get_active_user_root_obj) },
    { MP_ROM_QSTR(MP_QSTR_resonite_Slot_get_component),
        MP_ROM_PTR(&resonite_Slot_get_component_obj) },
};
STATIC MP_DEFINE_CONST_DICT(resonitenative_module_globals, resonitenative_module_globals_table);

const mp_obj_module_t resonitenative_user_cmodule = {
    .base = { &mp_type_module },
    .globals = (mp_obj_dict_t*)&resonitenative_module_globals,
};

MP_REGISTER_MODULE(MODULE_NAME, resonitenative_user_cmodule);
