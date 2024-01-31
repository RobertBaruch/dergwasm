"""Generates various language APIs corresponding to the Resonite API."""

import os.path
import sys

from c.generate_api import Main as CGenerator
from micropython.usercmodule.resonite.generate_api import Main as MicropythonGenerator

if __name__ == "__main__":
    if not os.path.exists("resonite_api.json"):
        print(
            "resonite_api.json not found. Please run generate_api.py"
            " from the root of the repository."
        )
        sys.exit(1)

    CGenerator().main()
    MicropythonGenerator().main()
