"""Splitting a question paper into parts and recognising the shared pieces: question numbers, A/B/C options.

Cambridge papers put "Part n" at the top of the first page of each part, then "Questions a – b", the instructions
("For each question, …", "Write one word for each gap.") and the content. Page numbers and "Turn over ►" are dropped.
"""

import re
from dataclasses import dataclass

from .model import Document, Line, Picture
from .text import row_text, rows

PART_MARK = re.compile(r"^Part (\d+)$")
QUESTIONS_HEADING = re.compile(r"^Questions? (\d+)(?:\s*[–-]\s*(\d+))?$")
INSTRUCTION_START = re.compile(r"^For each question\b")
WORD_LIMIT = re.compile(r"^Write (\d+) words or more\.?$")
OPTION_LETTER = re.compile(r"^([A-H])$")
OPTION_WITH_TEXT = re.compile(r"^([A-H])\s+(.+)$")

HEADER_ZONE = 100.0       # "Part n" sits above this y
FOOTER_ZONE = 770.0       # page number and "Turn over" sit below this y
NUMBER_COLUMN = 80.0      # question numbers start left of this x
FOLLOW_ON_GAP = 20.0      # an instruction line continues the one above when closer than this
ROW_SLACK = 4.0           # a question's own row may sit a few points above its number
PICTURE_SLACK = 10.0      # a picture may start a little above its question number


@dataclass(frozen=True)
class PartSource:
    part_no: int
    lines: tuple[Line, ...]
    pictures: tuple[Picture, ...]
    instructions: str
    first: int | None
    last: int | None

    @property
    def pages(self) -> list[int]:
        return sorted({line.page for line in self.lines} | {picture.page for picture in self.pictures})


def is_question_number(line: Line) -> bool:
    return line.bold and line.text.isdigit() and line.x0 < NUMBER_COLUMN and int(line.text) > 0


def _is_page_chrome(line: Line) -> bool:
    footer = line.y0 > FOOTER_ZONE and (line.text.isdigit() or line.text.startswith("Turn over"))
    return footer or line.text.upper() == "BLANK PAGE"


def split_parts(document: Document) -> dict[int, PartSource]:
    marks = {
        line.page: int(match[1])
        for line in document.lines
        if line.bold and line.y0 < HEADER_ZONE and (match := PART_MARK.match(line.text))
    }
    part_of_page: dict[int, int] = {}
    current = None
    last_page = max([line.page for line in document.lines] + [p.page for p in document.pictures], default=-1)
    for page in range(last_page + 1):
        current = marks.get(page, current)
        if current is not None:
            part_of_page[page] = current

    parts: dict[int, PartSource] = {}
    for part_no in sorted(set(part_of_page.values())):
        pages = {page for page, number in part_of_page.items() if number == part_no}
        lines = [line for line in document.lines if line.page in pages and not _is_page_chrome(line)]
        parts[part_no] = _strip_headings(
            part_no, lines, tuple(p for p in document.pictures if p.page in pages))
    return parts


def _strip_headings(part_no: int, lines: list[Line], pictures: tuple[Picture, ...]) -> PartSource:
    content: list[Line] = []
    instructions: list[Line] = []
    first = last = None
    for line in sorted(lines, key=lambda item: (item.page, item.y0, item.x0)):
        heading = QUESTIONS_HEADING.match(line.text)
        follows_instruction = (
            instructions and line.text.startswith("Write") and line.page == instructions[-1].page
            and 0 < line.y0 - instructions[-1].y0 < FOLLOW_ON_GAP
        )
        if line.bold and PART_MARK.match(line.text) and line.y0 < HEADER_ZONE:
            continue
        if line.bold and heading:
            first = int(heading[1])
            last = int(heading[2] or heading[1])
        elif INSTRUCTION_START.match(line.text) or follows_instruction or WORD_LIMIT.match(line.text):
            instructions.append(line)
        elif "on your answer sheet" not in line.text:
            content.append(line)
    return PartSource(
        part_no=part_no,
        lines=tuple(content),
        pictures=pictures,
        instructions=" ".join(line.text for line in instructions),
        first=first,
        last=last,
    )


@dataclass(frozen=True)
class Region:
    """A question number, the other lines from its row down to the next number on the page, and those bounds."""

    number: Line
    lines: tuple[Line, ...]
    top: float
    bottom: float

    @property
    def question(self) -> int:
        return int(self.number.text)

    def holds(self, picture: Picture) -> bool:
        return picture.page == self.number.page and self.top - PICTURE_SLACK <= picture.y0 < self.bottom


def question_regions(lines: tuple[Line, ...]) -> list[Region]:
    numbers = sorted((line for line in lines if is_question_number(line)), key=lambda item: (item.page, item.y0))
    regions = []
    for index, number in enumerate(numbers):
        following = numbers[index + 1] if index + 1 < len(numbers) else None
        top = number.y0 - ROW_SLACK
        bottom = following.y0 - ROW_SLACK if following and following.page == number.page else float("inf")
        region = tuple(
            line for line in lines
            if line.page == number.page and top <= line.y0 < bottom and not is_question_number(line)
        )
        regions.append(Region(number, region, top, bottom))
    return regions


def parse_choice(lines: list[Line]) -> tuple[str, list[dict]]:
    """Prompt rows, then options that start with a bold letter ('A text' or 'A' with the text beside it)."""
    prompt: list[str] = []
    options: list[dict] = []
    for row in rows(lines):
        first = row[0]
        alone = OPTION_LETTER.match(first.text)
        joined = OPTION_WITH_TEXT.match(first.text)
        if first.bold and (alone or joined):
            key = (alone or joined)[1]
            words = ([joined[2]] if joined else []) + [line.text for line in row[1:]]
            options.append({"key": key, "text": " ".join(words)})
        elif options:
            options[-1]["text"] = f"{options[-1]['text']} {row_text(row)}".strip()
        else:
            prompt.append(row_text(row))
    return " ".join(prompt), options
