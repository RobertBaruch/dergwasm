mod component;
mod external;
mod ref_id;
mod slot;
mod user;
mod user_root;
mod value;

use std::error::Error;

use external::Extern;

pub use component::Component;
pub use ref_id::RefId;
pub use slot::{Slot, SlotChildren, SlotChildrenIterator};
pub use user::User;
pub use user_root::UserRoot;
pub use value::Value;

pub type DergResult<T = ()> = Result<T, DergError>;

pub fn derg_result(value: i32) -> DergResult {
    DergError::from(value).to_result()
}

#[derive(Clone, Debug)]
pub enum DergError {
    Unknown,
    Success,
    NullOrNotFound,
}

impl std::fmt::Display for DergError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{:#?}", self)
    }
}

impl Error for DergError {}

impl From<i32> for DergError {
    fn from(value: i32) -> Self {
        match value {
            0 => DergError::Success,
            -1 => DergError::NullOrNotFound,
            _ => DergError::Unknown,
        }
    }
}

impl DergError {
    pub fn to_result(&self) -> DergResult {
        match self {
            DergError::Success => Ok(()),
            _ => Err(self.clone()),
        }
    }
}
