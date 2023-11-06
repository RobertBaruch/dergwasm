"""The stack."""

from typing import Type, TypeVar, cast

from dergwasm.interpreter import values


T = TypeVar("T")


class Stack:
    """A stack of StackValues."""

    data: list[values.StackValue]

    def __init__(self) -> None:
        self.data = []

    def push(self, value: values.StackValue) -> None:
        self.data.append(value)

    def pop(self) -> values.StackValue:
        return self.data.pop()

    def depth(self) -> int:
        return len(self.data)

    def get_topmost_value_of_type(self, value_type: Type[T]) -> T:
        return self.get_nth_value_of_type(0, value_type)

    def get_nth_value_of_type(self, n: int, value_type: Type[T]) -> T:
        """Gets the n-th value of the given type from the top of the stack (0-based)."""
        for i in range(len(self.data) - 1, -1, -1):
            if isinstance(self.data[i], value_type):
                if n == 0:
                    return cast(T, self.data[i])
                n -= 1
        raise ValueError(
            f"Could not find the {n}-th value of type {value_type.__name__} on the stack"
        )
