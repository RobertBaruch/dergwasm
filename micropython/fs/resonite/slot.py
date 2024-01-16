import resonitenative


class Slot:
    reference_id: int

    def __init__(self, reference_id: int):
        self.reference_id = reference_id

    def __str__(self):
        return "Slot<ID=%d>" % self.reference_id

    @classmethod
    def root_slot(cls):
        return Slot(resonitenative.resonite_Slot_root_slot())

    def get_parent(self):
        return Slot(resonitenative.resonite_Slot_get_parent(self.reference_id))

    def get_object_root(self, only_explicit: bool = False):
        return Slot(resonitenative.resonite_Slot_get_object_root(
            self.reference_id, only_explicit))

    def get_name(self):
        return resonitenative.resonite_Slot_get_name(self.reference_id)

    def set_name(self, name: str):
        resonitenative.resonite_Slot_set_name(self.reference_id, name)

    def children_count(self):
        return resonitenative.resonite_Slot_children_count(self.reference_id)

    def get_child(self, index: int):
        return Slot(resonitenative.resonite_Slot_get_child(self.reference_id, index))

    def find_child_by_name(self, name: str,
                           match_substring: bool = True,
                           ignore_case: bool = False,
                           max_depth: int = -1):
        child_id = resonitenative.resonite_Slot_find_child_by_name(
            self.reference_id, name, match_substring, ignore_case, max_depth)
        if child_id == 0:
            return None
        return Slot(child_id)

    def find_child_by_tag(self, tag: str, max_depth: int = -1):
        child_id = resonitenative.resonite_Slot_find_child_by_tag(
            self.reference_id, tag, max_depth)
        if child_id == 0:
            return None
        return Slot(child_id)
