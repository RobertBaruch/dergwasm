#ifndef __RESONITE_RESONITE_COMPONENT_API_H__
#define __RESONITE_RESONITE_COMPONENT_API_H__

#include <stdint.h>
#include <emscripten.h>

#include "resonite_api_types.h"

// Component-related functions that WASM can call.

// Returns the fully-qualified type name for the given component. The returned name
// must be freed when done with it. Although the return type is not const, changes
// to the returned string do nothing.
extern char* component__get_type_name(resonite_component_refid_t id);

#endif // __RESONITE_RESONITE_COMPONENT_API_H__
