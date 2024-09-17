"""Generates various language APIs corresponding to the Resonite API."""

import subprocess

from c.generate_api import Main as CGenerator
from micropython.usercmodule.resonite.generate_api import Main as MicropythonGenerator

if __name__ == "__main__":
    subprocess.run(["dotnet", "run", "--project", "ExtractResoniteApi", "--", "resonite_api.json"])

    CGenerator().main()
    MicropythonGenerator().main()
