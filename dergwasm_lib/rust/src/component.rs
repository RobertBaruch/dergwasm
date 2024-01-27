use std::ffi::{c_char, CString};

use crate::{derg_result, ref_id::Unknown, value::Value, DergError, DergResult, RefId};

#[link(wasm_import_module = "env")]
extern "C" {
    fn component__get_member(
        component: RefId<Component>,
        name: *const c_char,
        resonite_type: &mut i32,
        field_ref_id: &mut RefId<Unknown>,
    ) -> i32;
}

#[repr(C)]
#[derive(Clone, Debug)]
pub struct Component(RefId<Self>);

impl From<RefId<Self>> for Component {
    fn from(value: RefId<Self>) -> Self {
        Self(value)
    }
}

pub enum Member {
    ValueInt(Value<i32>),
}

impl Component {
    pub fn member_by_name(&self, name: &str) -> DergResult<Member> {
        let name = CString::new(name).unwrap();
        let mut type_info: i32 = 0;
        let mut field_ref_id = RefId::<Unknown>::default();
        derg_result(unsafe {
            component__get_member(self.0, name.as_ptr(), &mut type_info, &mut field_ref_id)
        })?;
        match type_info {
            0x1 => Ok(Member::ValueInt(
                field_ref_id.reinterpret::<Value<i32>>().into(),
            )),
            _ => Err(DergError::Unknown),
        }
    }
}
