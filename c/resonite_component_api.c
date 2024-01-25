// Contains all keepalives for resonite_component_api.

// This file isn't needed when compiling MicroPython because MicroPython uses the functions.

#include "resonite_component_api.h"

#include <stdint.h>
#include <emscripten.h>

EMSCRIPTEN_KEEPALIVE char* _component__get_type_name(resonite_component_refid_t id) {
	return component__get_type_name(id);
}
