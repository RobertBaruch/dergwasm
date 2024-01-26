use std::marker::PhantomData;

#[repr(C)]
pub struct RefId<T>(u64, PhantomData<T>);

#[repr(C)]
pub struct Unknown;

impl<T> std::fmt::Debug for RefId<T> {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> Result<(), std::fmt::Error> {
        f.debug_tuple("RefId")
            .field(&format_args!("{:X}", self.0))
            .field(&self.1)
            .finish()
    }
}

impl<T> Default for RefId<T> {
    fn default() -> Self {
        Self(Default::default(), Default::default())
    }
}

impl<T> Clone for RefId<T> {
    fn clone(&self) -> Self {
        Self(self.0.clone(), self.1.clone())
    }
}

impl<T> Copy for RefId<T> {}

impl<T> RefId<T> {
    pub fn valid(&self) -> bool {
        self.0 != 0
    }
}

impl<T> RefId<T>
where
    T: From<RefId<T>>,
{
    pub fn filter_invalid(self) -> Option<T> {
        if self.valid() {
            Some(self.into())
        } else {
            None
        }
    }
}

impl RefId<Unknown> {
    pub fn reinterpret<T>(self) -> RefId<T> {
        RefId::<T>(self.0, Default::default())
    }
}
