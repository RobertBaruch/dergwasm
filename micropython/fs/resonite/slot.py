import resonitenative


class Slot:
    reference_id: int

    def __init__(self, reference_id: int):
        self.reference_id = reference_id

    def __str__(self):
        return f"Slot<ID={self.reference_id:X}>"

    @staticmethod
    def make_new(reference_id: int) -> "Slot" | None:
        if reference_id == 0:
            return None
        return Slot(reference_id)

    @staticmethod
    def root_slot() -> "Slot":
        rets = resonitenative.slot__root_slot()
        return Slot(rets[0])

    def get_parent(self) -> "Slot" | None:
        rets = resonitenative.slot__get_parent(self.reference_id)
        return Slot.make_new(rets[0])

    def get_object_root(self, only_explicit: bool = False) -> "Slot" | None:
        rets = resonitenative.slot__get_object_root(
            self.reference_id, only_explicit
        )
        return Slot.make_new(rets[0])

    def get_name(self) -> str | None:
        rets = resonitenative.slot__get_name(self.reference_id)
        return rets[0]

    def set_name(self, name: str) -> None:
        resonitenative.slot__set_name(self.reference_id, name)

    def children_count(self) -> int:
        rets = resonitenative.slot__get_num_children(self.reference_id)
        return rets[0]

    def get_child(self, index: int) -> "Slot" | None:
        rets = resonitenative.slot__get_child(self.reference_id, index)
        return Slot.make_new(rets[0])

    def get_children(self) -> list["Slot"]:
        rets = resonitenative.slot__get_children(self.reference_id)
        return [Slot(ret) for ret in rets[0]]

    def find_child_by_name(
        self,
        name: str,
        match_substring: bool = True,
        ignore_case: bool = False,
        max_depth: int = -1,
    ) -> "Slot" | None:
        rets = resonitenative.slot__find_child_by_name(
            self.reference_id, name, match_substring, ignore_case, max_depth
        )
        return Slot.make_new(rets[0])

    def find_child_by_tag(self, tag: str, max_depth: int = -1) -> "Slot" | None:
        rets = resonitenative.slot__find_child_by_tag(
            self.reference_id, tag, max_depth
        )
        return Slot.make_new(rets[0])

    def slot__get_active_user(self) -> "User" | None:
        rets = resonitenative.slot__get_active_user(self.reference_id)
        return User.make_new(rets[0])

    def get_active_user_root(self) -> "UserRoot" | None:
        rets = resonitenative.slot__get_active_user_root(self.reference_id)
        return UserRoot.make_new(rets[0])

    def get_component(self, component_type_name: str) -> "Component" | None:
        rets = resonitenative.slot__get_component(
            self.reference_id, component_type_name
        )
        return Component.make_new(rets[0])

    def get_components(self) -> list["Component"]:
        rets = resonitenative.slot__get_components(self.reference_id)
        return [Component(ret) for ret in rets[0]]


from resonite.component import Component
from resonite.user import User
from resonite.userroot import UserRoot
