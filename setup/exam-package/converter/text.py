"""Turning positioned lines into rows, paragraphs and HTML."""

import html
import re
from collections.abc import Iterable

from .model import Line

ROW_TOLERANCE = 3.0
# Lines in a paragraph are about 1.7 × the font size apart; a blank line between paragraphs makes it about 2.9 ×.
PARAGRAPH_GAP = 2.2

GAP = re.compile(r"\((\d+)\)\s*[.…]{2,}")
SLOT_BEFORE_WORD = re.compile(r"(\{\{q:\d+\}\})(?=\w)")
GAP_BEFORE_CAPITAL = re.compile(r"\((\d+)\)\s*[.…]{2,}\s*[A-Z]")


def escape(text: str) -> str:
    return html.escape(text, quote=False)


def escape_attribute(text: str) -> str:
    """For attribute values (quoted with ' in the generated HTML): quotes are escaped too."""
    return html.escape(text, quote=True)


def gaps_to_slots(text: str, example: str | None = None) -> str:
    """'a long (19) ....... after' → 'a long {{q:19}} after'. Gap 0 is the worked example and shows its answer."""

    def slot(match: re.Match) -> str:
        number = int(match[1])
        if number == 0:
            return f"(0) <u>{escape(example)}</u>" if example else "(0) ……"
        return f"{{{{q:{number}}}}}"

    return SLOT_BEFORE_WORD.sub(r"\1 ", GAP.sub(slot, escape(text)))


def gaps_before_capitals(text: str) -> list[int]:
    """Gap numbers directly followed by a capital letter: the dots may have swallowed a full stop."""
    return [int(number) for number in GAP_BEFORE_CAPITAL.findall(text)]


def rows(lines: Iterable[Line]) -> list[list[Line]]:
    """Lines on the same baseline (within a few points) as one row, top to bottom, each row left to right."""
    grouped: list[list[Line]] = []
    for line in sorted(lines, key=lambda item: (item.page, item.y0, item.x0)):
        row = grouped[-1] if grouped else None
        if row and row[0].page == line.page and abs(line.y0 - row[0].y0) <= ROW_TOLERANCE:
            row.append(line)
        else:
            grouped.append([line])
    return [sorted(row, key=lambda item: item.x0) for row in grouped]


def row_text(row: Iterable[Line]) -> str:
    return " ".join(line.text for line in row)


def paragraphs(lines: Iterable[Line]) -> list[str]:
    """Joins rows into paragraphs; a vertical gap wider than a normal line break starts a new one."""
    result: list[list[str]] = []
    previous: Line | None = None
    for row in rows(lines):
        first = row[0]
        new_paragraph = (
            previous is None
            or first.page != previous.page
            or first.y0 - previous.y0 > PARAGRAPH_GAP * max(first.size, previous.size)
        )
        if new_paragraph:
            result.append([])
        result[-1].append(row_text(row))
        previous = first
    return [" ".join(words) for words in result]
