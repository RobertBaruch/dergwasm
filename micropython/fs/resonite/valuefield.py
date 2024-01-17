import resonitenative
from resonite.component import Component


class ValueField(Component):
    def __init__(self, reference_id: int, typename: str):
        super().__init__(reference_id, typename)
