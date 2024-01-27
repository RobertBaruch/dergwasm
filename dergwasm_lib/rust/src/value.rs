use std::marker::PhantomData;

use crate::{derg_result, DergResult, RefId};


#[link(wasm_import_module = "env")]
extern "C" {
    fn value__get_int(refId: RefId<Value<i32>>, val: &mut i32) -> i32;
    fn value__set_int(refId: RefId<Value<i32>>, val: &i32) -> i32;
}

#[repr(C)]
#[derive(Debug)]
pub struct Value<T>(RefId<Self>, PhantomData<T>);

impl<T> From<RefId<Self>> for Value<T> {
    fn from(value: RefId<Self>) -> Self {
        Self(value, Default::default())
    }
}

impl<T> Clone for Value<T> {
    fn clone(&self) -> Self {
        Self(self.0.clone(), self.1.clone())
    }
}

impl<T> Copy for Value<T> {}

impl Value<i32> {
    pub fn get(&self) -> DergResult<i32>  {
        let mut val = Default::default();
        derg_result(unsafe { value__get_int(self.0, &mut val) })?;
        Ok(val)
    }

    pub fn set(&mut self, val: i32) -> DergResult {
        derg_result(unsafe { value__set_int(self.0, &val.into()) })
    }
}
