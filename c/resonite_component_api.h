#ifndef __RESONITE_RESONITE_COMPONENT_API_H__
#define __RESONITE_RESONITE_COMPONENT_API_H__

#include <stdint.h>

#include "resonite_api_types.h"

// Component-related functions that WASM can call.

// Returns the fully-qualified type name for the given component.
//
// Although the return type is not const, changes to the returned string do nothing.
//
// The caller is responsible for freeing the memory pointed to by the returned string.
extern char *component__get_type_name(resonite_component_refid_t id);

// Gets the value of a field on a component.
//
// The value is serialized into memory, and a pointer to the serialized value is
// returned. If the len pointer is not NULL, the length of the serialized value is
// written to it.
//
// Returns NULL on failure.
//
// The caller is responsible for freeing the memory pointed to by data.
extern uint8_t *component__get_field_value(resonite_component_refid_t component_id,
                                           const char *name, int *len);

// Sets the value of a field on a component.
//
// The value to set must have been serialized in the buffer pointed to by data.
//
// Returns 0 on success, or -1 if the field doesn't exist, couldn't be set, or the
// value couldn't be deserialized.
extern int component__set_field_value(resonite_component_refid_t component_id,
                                      const char *name, uint8_t *data);

#endif // __RESONITE_RESONITE_COMPONENT_API_H__
