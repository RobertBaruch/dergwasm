use super::RefId;

#[repr(C)]
#[derive(Clone, Debug)]
pub struct UserRoot(RefId<Self>);

impl From<RefId<Self>> for UserRoot {
    fn from(value: RefId<Self>) -> Self {
        Self(value)
    }
}
