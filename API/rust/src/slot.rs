use std::ffi::{c_char, CString};

use super::*;

#[link(wasm_import_module = "env")]
extern "C" {
    fn slot__root_slot() -> RefId<Slot>;
    fn slot__get_parent(slot: RefId<Slot>) -> RefId<Slot>;
    fn slot__get_active_user(slot: RefId<Slot>) -> RefId<User>;
    fn slot__get_active_user_root(slot: RefId<Slot>) -> RefId<UserRoot>;
    fn slot__get_object_root(slot: RefId<Slot>, only_explicit: i32) -> RefId<Slot>;

    fn slot__get_name(slot: RefId<Slot>) -> Extern<c_char>;
    fn slot__set_name(slot: RefId<Slot>, str: *const c_char);

    fn slot__get_num_children(slot: RefId<Slot>) -> i32;
    fn slot__get_child(slot: RefId<Slot>, index: u32) -> RefId<Slot>;
    pub fn slot__find_child_by_name(
        slot: RefId<Slot>,
        name: *const c_char,
        match_substring: u32,
        ignore_case: u32,
        max_depth: u32,
    ) -> RefId<Slot>;
    pub fn slot__find_child_by_tag(
        slot: RefId<Slot>,
        tag: *const c_char,
        max_depth: u32,
    ) -> RefId<Slot>;
}

#[repr(C)]
#[derive(Clone, Debug)]
pub struct Slot(RefId<Self>);

impl From<RefId<Self>> for Slot {
    fn from(value: RefId<Self>) -> Self {
        Self(value)
    }
}

impl Slot {
    pub fn root_slot() -> Slot {
        unsafe { slot__root_slot() }.filter_invalid().unwrap()
    }

    pub fn parent(&self) -> Option<Slot> {
        unsafe { slot__get_parent(self.0) }.filter_invalid()
    }

    pub fn active_user(&self) -> Option<User> {
        unsafe { slot__get_active_user(self.0) }.filter_invalid()
    }

    pub fn active_user_root(&self) -> Option<UserRoot> {
        unsafe { slot__get_active_user_root(self.0) }.filter_invalid()
    }

    pub fn object_root(&self, only_explicit: bool) -> Option<Slot> {
        unsafe { slot__get_object_root(self.0, if only_explicit { 1 } else { 0 }) }
            .filter_invalid()
    }

    pub fn name(&self) -> Option<String> {
        let str = unsafe { slot__get_name(self.0) };
        Some(str.to_str()?.to_string())
    }
    pub fn set_name(&self, name: &str) {
        let name = CString::new(name).unwrap();
        unsafe { slot__set_name(self.0, name.as_ptr()) };
    }

    pub fn children(&self) -> SlotChildren {
        SlotChildren(self)
    }
}

#[repr(C)]
#[derive(Clone, Debug)]
pub struct SlotChildren<'a>(&'a Slot);

impl<'a> SlotChildren<'a> {
    pub fn get(&self, i: usize) -> Option<Slot> {
        unsafe { slot__get_child(self.0 .0, i as u32) }.filter_invalid()
    }

    pub fn len(&self) -> usize {
        unsafe { slot__get_num_children(self.0 .0) as usize }
    }

    pub fn find_by_name(
        &self,
        name: &str,
        match_substring: bool,
        ignore_case: bool,
        max_depth: usize,
    ) -> Option<Slot> {
        let name = CString::new(name).unwrap();
        unsafe {
            slot__find_child_by_name(
                self.0 .0,
                name.as_ptr(),
                match_substring.into(),
                ignore_case.into(),
                max_depth as u32,
            )
        }
        .filter_invalid()
    }

    pub fn find_by_tag(&self, tag: &str, max_depth: bool) -> Option<Slot> {
        let tag = CString::new(tag).unwrap();
        unsafe { slot__find_child_by_tag(self.0 .0, tag.as_ptr(), max_depth.into()) }
            .filter_invalid()
    }

    pub fn iter(&self) -> SlotChildrenIterator<'a> {
        SlotChildrenIterator {
            slot: self.clone(),
            index: 0,
        }
    }
}

pub struct SlotChildrenIterator<'a> {
    slot: SlotChildren<'a>,
    index: u32,
}

impl<'a> Iterator for SlotChildrenIterator<'a> {
    type Item = Slot;

    fn next(&mut self) -> Option<Self::Item> {
        let index = self.index;
        self.index += 1;
        unsafe { slot__get_child(self.slot.0 .0, index) }.filter_invalid()
    }
}
