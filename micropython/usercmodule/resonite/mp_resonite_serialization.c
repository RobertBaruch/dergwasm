#include "mp_resonite_serialization.h"

#include <cstdint.h>
#include <string.h>

#include "py/obj.h"
#include "py/runtime.h"
#include "mp_resonite_utils.h"

static mp_obj_t deserialize_bool(uint8_t* data) {
    return mp_obj_new_bool(*(int*)data);
}

static mp_obj_t deserialize_bool2(uint8_t* data) {
    return mp_obj_new_tuple(2, (mp_obj_t[]) {
        mp_obj_new_bool(*(int*)data),
            mp_obj_new_bool(*(int*)(data + 4)),
    });
}

static mp_obj_t deserialize_bool3(uint8_t* data) {
    return mp_obj_new_tuple(3, (mp_obj_t[]) {
        mp_obj_new_bool(*(int*)data),
            mp_obj_new_bool(*(int*)(data + 4)),
            mp_obj_new_bool(*(int*)(data + 8)),
    });
}

static mp_obj_t deserialize_bool4(uint8_t* data) {
    return mp_obj_new_tuple(4, (mp_obj_t[]) {
        mp_obj_new_bool(*(int*)data),
            mp_obj_new_bool(*(int*)(data + 4)),
            mp_obj_new_bool(*(int*)(data + 8)),
            mp_obj_new_bool(*(int*)(data + 12)),
    });
}

static mp_obj_t_deserialize_int(uint8_t* data) {
    return mp_obj_new_int(*(int*)data);
}

static mp_obj_t_deserialize_int2(uint8_t* data) {
    return mp_obj_new_tuple(2, (mp_obj_t[]) {
        mp_obj_new_int(*(int*)data),
            mp_obj_new_int(*(int*)(data + 4)),
    });
}

static mp_obj_t_deserialize_int3(uint8_t* data) {
    return mp_obj_new_tuple(3, (mp_obj_t[]) {
        mp_obj_new_int(*(int*)data),
            mp_obj_new_int(*(int*)(data + 4)),
            mp_obj_new_int(*(int*)(data + 8)),
    });
}

static mp_obj_t_deserialize_int4(uint8_t* data) {
    return mp_obj_new_tuple(4, (mp_obj_t[]) {
        mp_obj_new_int(*(int*)data),
            mp_obj_new_int(*(int*)(data + 4)),
            mp_obj_new_int(*(int*)(data + 8)),
            mp_obj_new_int(*(int*)(data + 12)),
    });
}

mp_obj_t resonite_deserialize(uint8_t* data) {
    resonite_serialization_type_t type = (resonite_serialization_type_t) * (int*)data;
    switch (type) {
    case RESONITE_SERIALIZATION_TYPE_BOOL:
        return deserialize_bool(data + 4);
    case RESONITE_SERIALIZATION_TYPE_BOOL2:
        return deserialize_bool2(data + 4);
    case RESONITE_SERIALIZATION_TYPE_BOOL3:
        return deserialize_bool3(data + 4);
    case RESONITE_SERIALIZATION_TYPE_BOOL4:
        return deserialize_bool4(data + 4);
    case RESONITE_SERIALIZATION_TYPE_INT:
        return deserialize_int(data + 4);
    case RESONITE_SERIALIZATION_TYPE_INT2:
        return deserialize_int2(data + 4);
    case RESONITE_SERIALIZATION_TYPE_INT3:
        return deserialize_int3(data + 4);
    case RESONITE_SERIALIZATION_TYPE_INT4:
        return deserialize_int4(data + 4);
    default:
        return mp_const_none;
    }
}

mp_obj_t resonite_new_Slot(resonite_slot_refid_t reference_id) {
    resonite_Slot_obj_t* self = mp_obj_malloc(resonite_Slot_obj_t, &resonite_Slot_type);
    self->reference_id = reference_id;
    return MP_OBJ_FROM_PTR(self);
}

void resonite_Slot_print(const mp_print_t* print, mp_obj_t self_in, mp_print_kind_t kind) {
    resonite_Slot_obj_t* self = MP_OBJ_TO_PTR(self_in);
    mp_printf(print, "Slot(ID=%ul)", self->reference_id);
}

mp_obj_t resonite_Slot_make_new(const mp_obj_type_t* type, size_t n_args, size_t n_kw, const mp_obj_t* args) {
    mp_arg_check_num(n_args, n_kw, 1, 1, false);
    if (!mp_obj_is_type(args[0], &mp_type_int)) {
        mp_raise_ValueError(MP_ERROR_TEXT("Slot ID must be an int"));
    }
    return resonite_new_Slot(mp_obj_int_get_uint64_checked(args[0]));
}

mp_obj_t resonite_Slot_root_slot(mp_obj_t cls_in) {
    return resonite_new_Slot_or_none(slot__root_slot());
}

mp_obj_t resonite_Slot_get_parent(mp_obj_t self_in) {
    resonite_Slot_obj_t* self = MP_OBJ_TO_PTR(self_in);
    return resonite_new_Slot_or_none(slot__get_parent(self->reference_id));
}

mp_obj_t resonite_Slot_get_object_root(size_t n_args, const mp_obj_t* pos_args, mp_map_t* kw_args) {
    static const mp_arg_t allowed_args[] = {
        { MP_QSTR_only_explicit, MP_ARG_BOOL, {.u_bool = false} },
    };
    resonite_Slot_obj_t* self = MP_OBJ_TO_PTR(pos_args[0]);

    mp_arg_val_t args[MP_ARRAY_SIZE(allowed_args)];
    mp_arg_parse_all(n_args - 1, pos_args + 1, kw_args, MP_ARRAY_SIZE(allowed_args), allowed_args, args);
    int only_explicit = args[0].u_bool ? 1 : 0;

    return resonite_new_Slot_or_none(slot__get_object_root(self->reference_id, only_explicit));
}

mp_obj_t resonite_Slot_get_name(mp_obj_t self_in) {
    resonite_Slot_obj_t* self = MP_OBJ_TO_PTR(self_in);
    char* name = slot__get_name(self->reference_id);
    return mp_obj_new_str(name, strlen(name));
}

mp_obj_t resonite_Slot_set_name(mp_obj_t self_in, mp_obj_t name) {
    resonite_Slot_obj_t* self = MP_OBJ_TO_PTR(self_in);
    slot__set_name(self->reference_id, mp_obj_str_get_str(name));
    return mp_const_none;
}

mp_obj_t resonite_Slot_children_count(mp_obj_t self_in) {
    resonite_Slot_obj_t* self = MP_OBJ_TO_PTR(self_in);
    return mp_obj_new_int(slot__get_num_children(self->reference_id));
}

mp_obj_t resonite_Slot_get_child(mp_obj_t self_in, mp_obj_t index) {
    resonite_Slot_obj_t* self = MP_OBJ_TO_PTR(self_in);
    return resonite_new_Slot_or_none(slot__get_child(self->reference_id, mp_obj_get_int(index)));
}

mp_obj_t resonite_Slot_find_child_by_name(size_t n_args, const mp_obj_t* pos_args, mp_map_t* kw_args) {
    static const mp_arg_t allowed_args[] = {
        { MP_QSTR_name, MP_ARG_REQUIRED | MP_ARG_OBJ, },
        { MP_QSTR_match_substring, MP_ARG_BOOL, {.u_bool = true} },
        { MP_QSTR_ignore_case, MP_ARG_BOOL, {.u_bool = false} },
        { MP_QSTR_max_depth, MP_ARG_INT, {.u_int = -1} },
    };
    resonite_Slot_obj_t* self = MP_OBJ_TO_PTR(pos_args[0]);

    mp_arg_val_t args[MP_ARRAY_SIZE(allowed_args)];
    mp_arg_parse_all(n_args - 1, pos_args + 1, kw_args, MP_ARRAY_SIZE(allowed_args), allowed_args, args);
    const char* name = mp_obj_str_get_str(args[0].u_obj);
    int match_substring = args[1].u_bool ? 1 : 0;
    int ignore_case = args[2].u_bool ? 1 : 0;
    int max_depth = args[3].u_int;
    return resonite_new_Slot_or_none(slot__find_child_by_name(self->reference_id, name, match_substring, ignore_case, max_depth));
}

mp_obj_t resonite_Slot_find_child_by_tag(size_t n_args, const mp_obj_t* pos_args, mp_map_t* kw_args) {
    static const mp_arg_t allowed_args[] = {
        { MP_QSTR_tag, MP_ARG_REQUIRED | MP_ARG_OBJ, },
        { MP_QSTR_max_depth, MP_ARG_INT, {.u_int = -1} },
    };
    resonite_Slot_obj_t* self = MP_OBJ_TO_PTR(pos_args[0]);

    mp_arg_val_t args[MP_ARRAY_SIZE(allowed_args)];
    mp_arg_parse_all(n_args - 1, pos_args + 1, kw_args, MP_ARRAY_SIZE(allowed_args), allowed_args, args);
    const char* tag = mp_obj_str_get_str(args[0].u_obj);
    int max_depth = args[1].u_int;
    return resonite_new_Slot_or_none(slot__find_child_by_tag(self->reference_id, tag, max_depth));
}
