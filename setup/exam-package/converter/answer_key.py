"""Cambridge mark schemes: 'PART n' (Reading & Writing adds a 'KEY' line), then question number and answer on
alternating lines.

A key ends at the next 'PART n' or at the first line where a question number should be but is not (the page
header repeated at the top of the next page). B1 Listening Part 3 adds an 'ACCEPTABLE MIS-SPELLING(S)' column: the
words after an answer, before the next number, are misspellings that also score.

Answers may mark optional words in brackets: '(a/an/the) (fantastic) waterfall(s)' accepts 'waterfall',
'the waterfalls', 'a fantastic waterfall' … — every combination becomes an accepted answer.
"""

import itertools
import re

PART_HEADING = re.compile(r"^PART\s+\d+$", re.IGNORECASE)
MISSPELLING_HEADING = re.compile(r"^ACCEPTABLE MIS-?SPELLINGS?$", re.IGNORECASE)
OPTIONAL = re.compile(r"\(([^()]*)\)")
MAX_VARIANTS = 64
FUSE = "\u0000"  # marks an optional word to join to the next one
# Words that stay separate: '(the) river' never means 'theriver'.
SEPARATE = {"a", "an", "the", "some", "my", "your", "his", "her", "its", "our", "their", "to", "of", "in", "on", "at"}


def expand_optional(answer: str) -> list[str]:
    """'(a/an/the) shark(s)' → ['shark', 'sharks', 'a shark', …]: bracketed words may be left out or replaced by
    any of their '/' alternatives. A bracket stuck to a word ('shark(s)') is an optional ending."""
    pieces = OPTIONAL.split(answer)
    if len(pieces) == 1:
        return [answer.strip()]
    literals, choices = pieces[0::2], pieces[1::2]
    options = []
    for group, after in zip(choices, literals[1:]):
        words = [choice.strip() for choice in group.split("/") if choice.strip()]
        # '(news) paper' also means 'newspaper': a single optional word before another may be joined to it.
        fused = [word + FUSE for word in words] if len(words) == 1 and after.startswith(" ") and words[0].lower() not in SEPARATE else []
        options.append([""] + words + fused)
    variants: list[str] = []
    for picked in itertools.product(*options):
        text = literals[0] + "".join(p + lit for p, lit in zip(picked, literals[1:]))
        text = " ".join(text.replace(FUSE + " ", "").replace(FUSE, "").split())
        if text and text not in variants:
            variants.append(text)
        if len(variants) >= MAX_VARIANTS:
            break
    # The plainest form first: what the examiner wrote without its optional words.
    return sorted(variants, key=len)


def parse_answer_key(text: str) -> dict[int, list[str]]:
    """Question number → accepted answers ('your / the' gives two)."""
    answers: dict[int, list[str]] = {}
    in_key = False
    misspellings = False
    number: int | None = None
    last: int | None = None

    for raw in text.splitlines():
        line = raw.strip()
        if not line:
            continue
        if PART_HEADING.match(line) or (in_key and number is None and line.upper() == "KEY"):
            if number is not None:
                raise ValueError(f"Answer key: question {number} has no answer.")
            in_key = True
            misspellings = False
        elif not in_key:
            continue
        elif MISSPELLING_HEADING.match(line):
            misspellings = True
        elif number is None:
            if line.isdigit():
                number = int(line)
            elif misspellings and last is not None and line == line.lower() and any(c.isalpha() for c in line):
                answers[last] += [word for word in expand_optional(line) if word not in answers[last]]
            else:
                in_key = False
        else:
            accepted: list[str] = []
            for part in line.split(" / ") if "(" in line else line.split("/"):
                for variant in expand_optional(part.strip()) if part.strip() else []:
                    if variant not in accepted:
                        accepted.append(variant)
            answers[number] = accepted
            last, number = number, None

    if number is not None:
        raise ValueError(f"Answer key: question {number} has no answer.")
    return answers
