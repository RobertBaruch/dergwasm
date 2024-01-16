import resonitenative
from . import component
from . import user
from . import userroot


class SlotChildrenIterable:
    slot: "Slot"
    index: int

    def __init__(self, slot):
        self.slot = slot
        self.index = 0

    def __iter__(self):
        return self

    def __next__(self):
        if self.index >= self.slot.children_count():
            raise StopIteration
        child = self.slot.get_child(self.index)
        self.index += 1
        return child


class Slot:
    reference_id: int

    def __init__(self, reference_id: int):
        self.reference_id = reference_id

    def __str__(self):
        return f"Slot<ID={self.reference_id:X}>"

    @classmethod
    def root_slot(cls) -> "Slot":
        return Slot(resonitenative.resonite_Slot_root_slot())

    def get_parent(self) -> "Slot":
        return Slot(resonitenative.resonite_Slot_get_parent(self.reference_id))

    def get_object_root(self, only_explicit: bool = False) -> "Slot":
        return Slot(resonitenative.resonite_Slot_get_object_root(
            self.reference_id, only_explicit))

    def get_name(self) -> str:
        return resonitenative.resonite_Slot_get_name(self.reference_id)

    def set_name(self, name: str) -> None:
        resonitenative.resonite_Slot_set_name(self.reference_id, name)

    def children_count(self) -> int:
        return resonitenative.resonite_Slot_children_count(self.reference_id)

    def get_child(self, index: int) -> "Slot" | None:
        child_id = resonitenative.resonite_Slot_get_child(self.reference_id, index)
        if child_id == 0:
            return None
        return Slot(child_id)

    def find_child_by_name(self, name: str,
                           match_substring: bool = True,
                           ignore_case: bool = False,
                           max_depth: int = -1) -> "Slot" | None:
        child_id = resonitenative.resonite_Slot_find_child_by_name(
            self.reference_id, name, match_substring, ignore_case, max_depth)
        if child_id == 0:
            return None
        return Slot(child_id)

    def find_child_by_tag(self, tag: str, max_depth: int = -1) -> "Slot" | None:
        child_id = resonitenative.resonite_Slot_find_child_by_tag(
            self.reference_id, tag, max_depth)
        if child_id == 0:
            return None
        return Slot(child_id)

    def children(self) -> SlotChildrenIterable:
        return SlotChildrenIterable(self)

    def get_active_user(self) -> "user.User":
        return user.User(
            resonitenative.resonite_Slot_get_active_user(self.reference_id))

    def get_active_user_root(self) -> "userroot.UserRoot":
        return userroot.UserRoot(
            resonitenative.resonite_Slot_get_active_user_root(self.reference_id))

    def get_component(self, component_type_name: str) -> "component.Component" | None:
        component_id = resonitenative.resonite_Slot_get_component(
            self.reference_id, component_type_name)
        if component_id == 0:
            return None
        return component.Component.make_new(component_id)
