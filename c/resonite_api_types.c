#include "resonite_api_types.h"

const char* resonite_error_code_to_string(resonite_error_code_t code) {
    switch (code) {
        case RESONITE_ERROR_SUCCESS:
            return "Success";
        case RESONITE_ERROR_NULL_ARGUMENT:
            return "Null argument";
        case RESONITE_ERROR_INVALID_REF_ID:
            return "Invalid reference ID";
        case RESONITE_ERROR_FAILED_PRECONDITION:
            return "Failed precondition";
        default:
            return "Unknown error code";
    }
}
