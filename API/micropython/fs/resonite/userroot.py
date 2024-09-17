import resonitenative


class UserRoot:
    reference_id: int

    def __init__(self, reference_id: int):
        self.reference_id = reference_id

    def __str__(self):
        return f"UserRoot<ID={self.reference_id:X}>"

    @staticmethod
    def make_new(reference_id: int) -> "UserRoot" | None:
        if reference_id == 0:
            return None
        return UserRoot(reference_id)