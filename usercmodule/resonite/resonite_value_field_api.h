#ifndef __RESONITE_RESONITE_VALUE_FIELD_API_H__
#define __RESONITE_RESONITE_VALUE_FIELD_API_H__

#include <stdint.h>
#include <emscripten.h>

#include "resonite_api_types.h"

// Sets the value of a ValueField<Bool>.
//
// ProtoFlux equivalent: ValueFields/SetBoolValue
// FrooxEngine equivalent: ValueField<Bool>.Value.Value
extern void value_field__bool__set_value(resonite_component_refid_t value_field_id, int value);

// Gets the value of a ValueField<Bool>.
//
// ProtoFlux equivalent: ValueFields/SetBoolValue
// FrooxEngine equivalent: ValueField<Bool>.Value
extern int value_field__bool__get_value(resonite_component_refid_t value_field_id);

#endif // __RESONITE_RESONITE_VALUE_FIELD_API_H__
