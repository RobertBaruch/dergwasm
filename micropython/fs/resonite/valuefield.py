import resonitenative
from . import component


class ValueField(component.Component):
    def __init__(self, reference_id: int, typename: str):
        super().__init__(reference_id, typename)
