#ifndef __RESONITE_RESONITE_VALUE_FIELD_API_H__
#define __RESONITE_RESONITE_VALUE_FIELD_API_H__

#include <stdint.h>
#include <emscripten.h>

#include "resonite_api_types.h"

// ValueField-related functions that WASM can call.

// Allocates enough memory to hold the serialized value of the given ValueField, returns
// the pointer to the allocated memory. The size_t is set to the size of the memory
// allocated. The returned pointer must be freed after use.
uint8_t* value_field__get_value(resonite_component_refid_t id, size_t* len);

// Sets the value of the given ValueField to the given serialized value. The
// component reference must be that of a ValueField, and the type of the serialized
// value must match the type stored in the ValueField.
void value_field__set_value(resonite_component_refid_t id, uint8_t* value);

#endif // __RESONITE_RESONITE_COMPONENT_API_H__
