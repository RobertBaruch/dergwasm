#[link(wasm_import_module = "env")]
extern "C" {
    pub fn mp_js_write(ptr: *const u8, len: i32);
}

// Note: This is going to go away once stdout is fixed.
#[derive(Default, Debug, Clone)]
pub struct Console;

impl std::fmt::Write for Console {
    fn write_str(&mut self, s: &str) -> std::fmt::Result {
        unsafe { mp_js_write(s.as_ptr(), s.len() as i32) };
        Ok(())
    }
}
