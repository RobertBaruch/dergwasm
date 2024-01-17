import resonitenative
from . import valuefield


class Component:
    reference_id: int
    typename: str

    def __init__(self, reference_id: int, typename: str = ""):
        self.reference_id = reference_id
        if typename:
            self.typename = typename
        else:
            self.typename = resonitenative.resonite_Component_get_type_name(reference_id)

    def __str__(self):
        return f"Component<ID={self.reference_id:X}>({self.typename})"

    @classmethod
    def make_new(cls, reference_id: int) -> "Component":
        typename = resonitenative.resonite_Component_get_type_name(reference_id)
        if typename.startswith("ValueField"):
            return valuefield.ValueField(reference_id, typename)
        return Component(reference_id, typename)