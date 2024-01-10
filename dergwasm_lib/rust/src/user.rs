use super::RefId;

#[repr(C)]
#[derive(Clone, Debug)]
pub struct User(RefId<Self>);

impl From<RefId<Self>> for User {
    fn from(value: RefId<Self>) -> Self {
        Self(value)
    }
}
