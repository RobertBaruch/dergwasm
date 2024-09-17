#ifndef __RESONITE_RESONITE_API_TYPES_H__
#define __RESONITE_RESONITE_API_TYPES_H__

#include <stdint.h>
#include <emscripten.h>

// This file contains types used in the WASM API for Resonite.

typedef enum {
    RESONITE_ERROR_SUCCESS = 0,
    RESONITE_ERROR_NULL_ARGUMENT = 1,
    RESONITE_ERROR_INVALID_REF_ID = 2,
    RESONITE_ERROR_FAILED_PRECONDITION = 3,
} resonite_error_code_t;

extern const char* resonite_error_code_to_string(resonite_error_code_t code);

typedef int32_t resonite_error_t;
typedef int32_t resonite_type_t;
typedef uint64_t resonite_refid_t;
typedef uint64_t resonite_slot_refid_t;
typedef uint64_t resonite_user_refid_t;
typedef uint64_t resonite_user_root_refid_t;
typedef uint64_t resonite_component_refid_t;

typedef struct {
    void* ptr;
    int32_t len;
} resonite_buff_t;

#endif // __RESONITE_RESONITE_API_TYPES_H__
