import resonitenative


class User:
    reference_id: int

    def __init__(self, reference_id: int):
        self.reference_id = reference_id

    def __str__(self):
        return f"User<ID={self.reference_id:X}>"
