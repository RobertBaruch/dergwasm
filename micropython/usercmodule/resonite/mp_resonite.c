
#include "py/obj.h"
#include "py/runtime.h"

#include "mp_resonite_api.h"

#define MODULE_NAME MP_QSTR_resonitenative
#define DEF_FUN(args, name) STATIC MP_DEFINE_CONST_FUN_OBJ_ ## args(resonite__ ## name ## _obj, resonite__ ## name)
#define DEF_FUNN(args, name) STATIC MP_DEFINE_CONST_FUN_OBJ_VAR_BETWEEN(resonite__ ## name ## _obj, args, args, resonite__ ## name)
#define DEF_ENTRY(name) { MP_ROM_QSTR(MP_QSTR_ ## name), MP_ROM_PTR(&resonite__ ## name ## _obj) }

DEF_FUN(0, slot__root_slot);
DEF_FUN(1, slot__get_parent);
DEF_FUN(1, slot__get_active_user);
DEF_FUN(1, slot__get_active_user_root);
DEF_FUN(2, slot__get_object_root);
DEF_FUN(1, slot__get_name);
DEF_FUN(2, slot__set_name);
DEF_FUN(1, slot__get_num_children);
DEF_FUN(2, slot__get_child);
DEF_FUN(1, slot__get_children);
DEF_FUNN(5, slot__find_child_by_name);
DEF_FUN(3, slot__find_child_by_tag);
DEF_FUN(2, slot__get_component);
DEF_FUN(1, slot__get_components);
DEF_FUN(1, component__get_type_name);
DEF_FUN(2, component__get_member);
DEF_FUN(1, value__get_int);
DEF_FUN(1, value__get_float);
DEF_FUN(1, value__get_double);
DEF_FUN(2, value__set_int);
DEF_FUN(2, value__set_float);
DEF_FUN(2, value__set_double);
STATIC const mp_rom_map_elem_t resonitenative_module_globals_table[] = {
    { MP_ROM_QSTR(MP_QSTR___name__), MP_ROM_QSTR(MODULE_NAME) },
    DEF_ENTRY(slot__root_slot),
    DEF_ENTRY(slot__get_parent),
    DEF_ENTRY(slot__get_active_user),
    DEF_ENTRY(slot__get_active_user_root),
    DEF_ENTRY(slot__get_object_root),
    DEF_ENTRY(slot__get_name),
    DEF_ENTRY(slot__set_name),
    DEF_ENTRY(slot__get_num_children),
    DEF_ENTRY(slot__get_child),
    DEF_ENTRY(slot__get_children),
    DEF_ENTRY(slot__find_child_by_name),
    DEF_ENTRY(slot__find_child_by_tag),
    DEF_ENTRY(slot__get_component),
    DEF_ENTRY(slot__get_components),
    DEF_ENTRY(component__get_type_name),
    DEF_ENTRY(component__get_member),
    DEF_ENTRY(value__get_int),
    DEF_ENTRY(value__get_float),
    DEF_ENTRY(value__get_double),
    DEF_ENTRY(value__set_int),
    DEF_ENTRY(value__set_float),
    DEF_ENTRY(value__set_double),
};

STATIC MP_DEFINE_CONST_DICT(resonitenative_module_globals, resonitenative_module_globals_table);

const mp_obj_module_t resonitenative_user_cmodule = {
    .base = { &mp_type_module },
    .globals = (mp_obj_dict_t*)&resonitenative_module_globals,
};

MP_REGISTER_MODULE(MODULE_NAME, resonitenative_user_cmodule);
