""" Resonite API for MicroPython.

This is just a stub file. It is not actually used, but serves as documentation
to the C module compiled into MicroPython. It also uses type hints, which
MicroPython does not support, but which are useful for documentation.
"""

from __future__ import annotations
from collections.abc import Iterator


# These types are necessary to mirror the Resonite types. Python's type system is
# more general.
#
# Type conversion between these numerics must be explicit. For example, you cannot
# multiply an Int and a UInt -- you would have to convert the Int to a UInt, or the
# UInt to an Int.
class Int:
	"""A 32-bit signed integer."""
	def __init__(self, val: int):
		"""
		Makes a new Int instance.

		Args:
		  val: The value.

        Raises:
  	        OverflowError: If the value cannot fit in a signed 32-bit int.
		"""

class UInt:
	"""A 32-bit unsigned integer."""
	def __init__(self, val: int):
		"""
		Makes a new UInt instance.

		Args:
		  val: The value.

        Raises:
  	        OverflowError: If the value is negative or cannot fit in an unsigned 32-bit int.
		"""

class Long:
	"""A 64-bit signed integer."""
	def __init__(self, val: int):
		"""
		Makes a new Long instance.

		Args:
		  val: The value.

        Raises:
  	        OverflowError: If the value cannot fit in a signed 64-bit int.
		"""

class ULong:
	"""A 64-bit unsigned integer."""
	def __init__(self, val: int):
		"""
		Makes a new ULong instance.

		Args:
		  val: The value.

        Raises:
  	        OverflowError: If the value is negative or cannot fit in an unsigned 64-bit int.
		"""

class Slot:
    """A Slot."""
    def __init__(self, reference_id: int):
        """
        Makes a new Slot instance.

        Args:
          reference_id: The Slot's ReferenceID.
        """

    @classmethod
    def root_slot(cls) -> Slot;
		"""Returns the root slot.

		ProtoFlux equivalent: Slots/RootSlot
		FrooxEngine equivalent: World.RootSlot
		"""

    def get_active_user(self) -> User:
        """Returns the active user for this slot.

        ProtoFlux equivalent: Users/GetActiveUser, Slots/GetActiveUser
        FrooxEngine equivalent: Slot.ActiveUser
        """

    def get_active_user_root(self) -> UserRoot:
        """Returns the active user root for this slot.

        ProtoFlux equivalent: Users/User Root/GetActiveUserRoot, Slots/GetActiveUserRoot
        FrooxEngine equivalent: Slot.ActiveUserRoot
        """

    def get_object_root(self, only_explicit: bool = False) -> Slot:
        """Returns the object root for this slot.

        ProtoFlux equivalent: Slots/GetObjectRoot
        FrooxEngine equivalent: Slot.GetObjectRoot
        """

    def get_parent(self) -> Slot:
        """Returns the parent of this slot.

        ProtoFlux equivalent: Slots/GetParentSlot
        FrooxEngine equivalent: Slot.Parent
        """

    def is_active(self) -> bool:
        """Returns whether this slot is active.

        ProtoFlux equivalent: Slots/Info/GetSlotActive
        FrooxEngine equivalent: Slot.IsActive
        """

    def is_active_self(self) -> bool:
        """Returns whether this slot is active self.

        ProtoFlux equivalent: Slots/Info/GetSlotActiveSelf
        FrooxEngine equivalent: Slot.ActiveSelf
        """

    def get_name(self) -> str:
        """Returns the name of this slot.

        ProtoFlux equivalent: Slots/Info/GetSlotName
        FrooxEngine equivalent: Slot.Name
        """

    def set_name(self, name: str) -> None:
		"""Sets the name of this slot.

		ProtoFlux equivalent: Slots/Info/SetSlotName
		FrooxEngine equivalent: Slot.Name
		"""

    def children(self) -> Iterator[Slot]:
        """Returns an iterator over the children of this slot.

        Warning about added and deleted children during iteration.
        Warning about storing iterators across MicroPython calls.

        ProtoFlux equivalent: None
        FrooxEngine equivalent: Slot.Children
        """

    def children_count(self) -> int:
        """Returns the number of children of this slot.

        ProtoFlux equivalent: Slots/ChildrenCount
        FrooxEngine equivalent: Slot.ChildrenCount
        """

    def get_child(self, index: int) -> Slot | None:
        """Returns the child of this slot at the given index.

        ProtoFlux equivalent: Slots/GetChild
        FrooxEngine equivalent: Slot.operator[]
        """

    def destroy(self,
                send_destroying_event: bool = True) -> bool:
        """Destroys this slot without preserving assets.

        Args:
            send_destroying_event: Whether to send the destroying event.

        Returns:
            False if the slot is already removed or is the root slot, True otherwise.

        ProtoFlux equivalent: Slots/DestroySlot
        FrooxEngine equivalent: Slot.SendDestroyingEvent, Slot.Destroy
        """

    def destroy_preserving_assets(self,
                                  send_destroying_event: bool = True) -> bool:
        """Destroys this slot, preserving assets.

        Args:
            send_destroying_event: Whether to send the destroying event.

        Returns:
            False if the slot is already removed or is the root slot, True otherwise.

        ProtoFlux equivalent: Slots/DestroySlot
        FrooxEngine equivalent: Slot.SendDestroyingEvent, Slot.DestroyPreservingAssets
        """

    def destroy_children(self,
                         preserve_assets: bool = False,
                         send_destroying_event: bool = True) -> bool:
        """Destroys all children of this slot.

        Args:
            preserve_assets: Whether to preserve the assets of this slot.
            send_destroying_event: Whether to send the destroying event.

        Returns:
            False if this slot is already removed or is the root slot, True otherwise.

        ProtoFlux equivalent: Slots/DestroySlotChildren
        FrooxEngine equivalent: Slot.SendDestroyingEvent, Slot.DestroyChildren
        """

    def duplicate(self) -> bool:
        """Duplicates this slot.

        Args:
            None.

        Returns:
            False if this slot is already removed or is the root slot, True otherwise.

        ProtoFlux equivalent: Slots/DuplicateSlot
        FrooxEngine equivalent: Slot.Duplicate
        """

    def find_child_by_name(self, name: str,
                           match_substring: bool = True,
                           ignore_case: bool = False,
                           max_depth: int = -1) -> Slot | None:
        """Returns the child of this slot with the given name.

        ProtoFlux equivalent: Slots/Searching/FindChildByName
        FrooxEngine equivalent: Slot.FindChild
        """

    def find_child_by_tag(self, tag: str, max_depth: int = -1) -> Slot | None:
        """Returns a child of this slot with the given tag.

        ProtoFlux equivalent: Slots/Searching/FindChildByTag
        FrooxEngine equivalent: Slot.FindChild
        """

    def find_parent_by_name(self, name: str,
                            match_substring: bool = True,
                            ignore_case: bool = False,
                            max_depth: int = -1) -> Slot | None:
        """Returns the parent of this slot with the given name.

        ProtoFlux equivalent: Slots/Searching/FindParentByName
        FrooxEngine equivalent: Slot.FindParent
        """

    def find_parent_by_tag(self, tag: str, max_depth: int = -1) -> Slot | None:
        """Returns a parent of this slot with the given tag.

        ProtoFlux equivalent: Slots/Searching/FindParentByTag
        FrooxEngine equivalent: Slot.FindParent
        """

    def get_components(self) -> Iterator[Component]:
		"""Returns an iterator over the components of this slot.

		Warning about added and deleted components during iteration.
		Warning about storing iterators across MicroPython calls.

		ProtoFlux equivalent: None
		FrooxEngine equivalent: Slot.Components
		"""

class Component:
    """Base class for Components."""
    def __init__(self, reference_id: int):
        """
        Makes a new Component instance.

        Args:
          reference_id: The Component's ReferenceID.
        """

class ValueField[T](Component):
	"""A ValueField storing a value of type T."""
	def __init__(self, reference_id: int, value_type: type[T]):
		"""
		Makes a new ValueField instance holding a type of value_type.
        We must pass the type of the value as a type hint, because
        while the Python runtime may not care, C# does.

		Args:
		  reference_id: The ValueField's ReferenceID.
          value_type: The type of the value.
		"""

    def get_value_type(self) -> type:
		"""Returns the type of the value stored in this ValueField."""

	def get_value(self) -> T:
		"""Returns the value of this ValueField.

		ProtoFlux equivalent: ValueFields/GetValue
		FrooxEngine equivalent: ValueField.Value
		"""

	def set_value(self, value: T) -> None:
		"""Sets the value of this ValueField.

		ProtoFlux equivalent: ValueFields/SetValue
		FrooxEngine equivalent: ValueField.Value
		"""