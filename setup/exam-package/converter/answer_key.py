"""Cambridge mark schemes: 'PART n' (Reading & Writing adds a 'KEY' line), then question number and answer on
alternating lines.

A key ends at the next 'PART n' or at the first line where a question number should be but is not (the page
header repeated at the top of the next page).
"""

import re

PART_HEADING = re.compile(r"^PART\s+\d+$", re.IGNORECASE)


def parse_answer_key(text: str) -> dict[int, list[str]]:
    """Question number → accepted answers ('your / the' gives two)."""
    answers: dict[int, list[str]] = {}
    in_key = False
    number: int | None = None

    for raw in text.splitlines():
        line = raw.strip()
        if not line:
            continue
        if PART_HEADING.match(line) or (in_key and number is None and line.upper() == "KEY"):
            if number is not None:
                raise ValueError(f"Answer key: question {number} has no answer.")
            in_key = True
        elif not in_key:
            continue
        elif number is None:
            if line.isdigit():
                number = int(line)
            else:
                in_key = False
        else:
            answers[number] = [part.strip() for part in line.split("/") if part.strip()]
            number = None

    if number is not None:
        raise ValueError(f"Answer key: question {number} has no answer.")
    return answers
