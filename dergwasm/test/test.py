from dergwasm import resonite


def add_a_slot() -> None:
    root = resonite.get_root_slot()
    slot = resonite.new_slot("slot_name")
    root.add_slot(slot)
