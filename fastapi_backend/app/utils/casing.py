from __future__ import annotations

import re


def to_camel(value: str) -> str:
    parts = re.split(r"_+", value)
    if not parts:
        return value

    head = parts[0]
    tail = "".join(part.capitalize() for part in parts[1:])
    return f"{head}{tail}"
