
extern "C" {
    //fn malloc(size: usize) -> *mut u8;
    fn free(ptr: *mut u8);
}

#[repr(C)]
pub struct Extern<T> {
    ptr: *mut T,
}

impl<T> Extern<T> {
    pub fn is_null(&self) -> bool {
        self.ptr.is_null()
    }

    pub fn get(&self) -> Option<&T> {
        if self.is_null() {
            None
        } else {
            Some(unsafe { &*self.ptr })
        }
    }
}

impl Extern<std::ffi::c_char> {
    pub fn to_str(&self) -> Option<&str> {
        let data = self.get()?;
        Some(unsafe { std::ffi::CStr::from_ptr(data) }.to_str().unwrap())
    }
}

impl<T> Drop for Extern<T> {
    fn drop(&mut self) {
        if !self.ptr.is_null() {
            unsafe {
                free(self.ptr as *mut u8);
            }
        }
    }
}
