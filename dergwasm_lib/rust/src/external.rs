use std::ptr::NonNull;

extern "C" {
    //fn malloc(size: usize) -> *mut u8;
    fn free(ptr: *mut u8);
}

#[repr(C)]
pub struct Extern<T> {
    ptr: Option<NonNull<T>>,
}

impl<T> Extern<T> {
    pub fn get(&self) -> Option<&T> {
        let ptr = self.ptr?;
        Some(unsafe { ptr.as_ref() })
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
        if let Some(ptr) = self.ptr {
            unsafe {
                free(ptr.as_ptr() as *mut u8);
            }
        }
    }
}
