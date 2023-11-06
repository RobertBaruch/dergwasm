from dergwasm import resonite


def add_a_slot() -> None:
    root = resonite.GetRootSlot()
    slot = resonite.NewSlot("slot_name")
    root.AddSlot(slot)
