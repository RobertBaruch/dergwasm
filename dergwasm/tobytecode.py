import dis

from dergwasm.test import test


def run():
    dis.dis(test.add_a_slot)


if __name__ == "__main__":
    run()
