#include <emscripten.h>

extern int this_is_to_be_defined_in_js(int arg0);

EMSCRIPTEN_KEEPALIVE int _this_is_so_that_function_must_be_in_js(int arg0) {
  return this_is_to_be_defined_in_js(arg0);
}

int main() {
}

