// Contains all keepalives for resonite_component_api.

// This file isn't needed when compiling MicroPython because MicroPython uses the functions.

#include "resonite_component_api.h"

#include <stdint.h>
#include <emscripten.h>

EMSCRIPTEN_KEEPALIVE char *_component__get_type_name(resonite_component_refid_t id)
{
	return component__get_type_name(id);
}

EMSCRIPTEN_KEEPALIVE uint8_t *_component__get_field_value(resonite_component_refid_t component_id,
														  const char *name, int *len)
{
	return component__get_field_value(component_id, name, data);
}

EMSCRIPTEN_KEEPALIVE int _component__set_field_value(resonite_component_refid_t component_id,
													 const char *name, uint8_t *data)
{
	return component__set_field_value(component_id, name, data);
}
