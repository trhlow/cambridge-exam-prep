"""B1 Preliminary (for Schools) Writing: Part 1 (question 1, an email to answer, compulsory) and Part 2 (questions 2
and 3, an article or a story: the candidate chooses one). There is no answer key: examiners mark both tasks.

On paper each question is a heading ("Question 1"), lines of instructions at the left margin, and a figure — the
email with handwritten notes linked to it, or a framed notice. The figure is saved as one picture, as Inspera shows
it; the margin lines stay text, so the candidate can highlight them.
"""

import re
from pathlib import Path

from .builders import Conversion
from .model import Crop, Document, Line, Shape, read_document
from .parts import split_parts
from .text import escape, escape_attribute, row_text, rows

EXAM_TYPE = "B1_PRELIM_FS"
SECTION = 2
PART_COUNT = 2
POINTS = 20                 # four criteria marked 0–5
MARGIN_X = 52.0             # the instruction lines start at x ≈ 49.7; a figure's text starts further right
FIGURE_MARGIN = 8.0
PAGE_LEFT, PAGE_RIGHT = 40.0, 555.0
PX_PER_POINT = 1.25        # the figure spans the page; 1.25 px/pt fits the left pane
LINE_HEIGHT = 1.25
STORY_SIZE = 13.0           # the story's first sentence is set larger than the 11 pt text
FOOTER_ZONE = 770.0

QUESTION_HEADING = re.compile(r"^Question (\d+)$")
PART_MARK = re.compile(r"^Part (\d+)$")
ABOUT_WORDS = re.compile(r"about (\d+) words")


def _lines_on(document: Document, page: int) -> list[Line]:
    return [
        line for line in document.lines
        if line.page == page and not (line.y0 > FOOTER_ZONE and line.text.isdigit()) and line.text.upper() != "BLANK PAGE"
    ]


def _figure_crop(name: str, page: int, lines: list[Line], pictures, shapes: list[Shape]) -> Crop:
    top = min([line.y0 for line in lines] + [p.y0 for p in pictures])
    bottom = max([line.y0 + line.size * LINE_HEIGHT for line in lines] + [p.y1 for p in pictures])
    # Frames and the lines linking notes to the email are drawn: take the drawings that reach into the figure.
    for shape in shapes:
        if shape.page == page and shape.y1 > top - 30 and shape.y0 < bottom + 30 and shape.y1 - shape.y0 < 700:
            top, bottom = min(top, shape.y0), max(bottom, shape.y1)
    return Crop(name=name, page=page, x0=PAGE_LEFT, y0=max(0.0, top - FIGURE_MARGIN), x1=PAGE_RIGHT, y1=bottom + FIGURE_MARGIN)


def _question(number: int, lines: list[Line], document: Document, page: int) -> tuple[dict, list[Crop]]:
    """Margin rows become paragraphs (bold rows bold, the story's first sentence emphasised); the rest is the figure,
    saved as one picture and shown where its first row stands."""
    html: list[str] = []
    figure: list[Line] = []
    figure_at: int | None = None
    for row in rows(lines):
        if row[0].x0 < MARGIN_X:
            text = escape(row_text(row))
            if row[0].size >= STORY_SIZE:
                html.append(f"<p><strong><em>{text}</em></strong></p>")
            elif all(line.bold for line in row):
                html.append(f"<p><strong>{text}</strong></p>")
            else:
                html.append(f"<p>{text}</p>")
        else:
            if figure_at is None:
                figure_at = len(html)
            figure.extend(row)

    top, bottom = min(line.y0 for line in lines), max(line.y0 for line in lines)
    pictures = [p for p in document.pictures if p.page == page and top - 20 <= p.y0 <= bottom + 20]
    crops: list[Crop] = []
    if figure or pictures:
        crop = _figure_crop(f"images/w-q{number}.png", page, figure, pictures, list(document.shapes))
        crops.append(crop)
        width = round((crop.x1 - crop.x0) * PX_PER_POINT)
        html.insert(figure_at if figure_at is not None else len(html), f"<p><img src='asset:{crop.name}' alt='Question {number}' width='{width}'></p>")
    return {"number": number, "stimulusHtml": "".join(html), "points": POINTS}, crops


def convert_parts(document: Document) -> Conversion:
    parts: dict[int, dict] = {}
    crops: list[Crop] = []
    warnings: list[str] = []
    checks: list[str] = []
    for part_no, source in split_parts(document).items():
        if part_no > PART_COUNT:
            warnings.append(f"Part {part_no} is not part of B1 Writing; it was left out.")
            continue
        page_lines = [line for page in source.pages for line in _lines_on(document, page)]
        headings = sorted((line for line in page_lines if line.bold and QUESTION_HEADING.match(line.text)), key=lambda line: (line.page, line.y0))
        if not headings:
            warnings.append(f"Part {part_no}: no 'Question n' heading found.")
            continue
        first = headings[0]
        above = [line for line in page_lines if (line.page, line.y0) < (first.page, first.y0) and not PART_MARK.match(line.text)]
        instructions = " ".join(row_text(row) for row in rows(above)).replace(" on the answer sheet", "")
        minimum = ABOUT_WORDS.search(instructions)
        if not minimum:
            warnings.append(f"Part {part_no}: no 'about N words' found in the instructions.")

        questions = []
        for index, heading in enumerate(headings):
            following = headings[index + 1] if index + 1 < len(headings) else None
            own = [
                line for line in page_lines
                if line is not heading and (line.page, line.y0) > (heading.page, heading.y0)
                and (following is None or (line.page, line.y0) < (following.page, following.y0))
            ]
            question, question_crops = _question(int(QUESTION_HEADING.match(heading.text)[1]), own, document, heading.page)
            question["minWords"] = int(minimum[1]) if minimum else 0
            questions.append(question)
            crops += question_crops

        group = {"interactionType": "Essay", "instructions": instructions, "questions": questions}
        if part_no == 2 and len(questions) > 1:
            # "Choose one of these questions": the candidate says which on screen and only that one is marked.
            group = {"interactionType": "Essay", "instructions": instructions, "chooseCount": 1, "questions": questions}
            checks.append("Part 2: candidates choose one of the questions (chooseCount 1); each question has its own page.")
        parts[part_no] = {"partNo": part_no, "layout": "Split", **({"questionPerPage": True} if part_no == 2 else {}), "groups": [group]}

    checks.append("B1 asks for about 100 words: minWords is that number; the screen only counts words, as Inspera does.")
    return Conversion(parts, crops, warnings, checks)


def build_paper(conversion: Conversion, title: str) -> tuple[dict, list[str]]:
    warnings = [f"Part {n} was not found in the PDF." for n in range(1, PART_COUNT + 1) if n not in conversion.paper_parts]
    parts = [conversion.paper_parts[n] for n in sorted(conversion.paper_parts)]
    numbers = [q["number"] for part in parts for group in part["groups"] for q in group["questions"]]
    if numbers != list(range(1, len(numbers) + 1)):
        warnings.append(f"Question numbers must run 1, 2, 3… without gaps; found {numbers}.")
    paper = {"formatVersion": 1, "examType": EXAM_TYPE, "section": SECTION, "title": title, "parts": parts}
    return paper, warnings


def convert_files(questions_pdf: Path, out_dir: Path, title: str) -> Conversion:
    from .output import write_package

    conversion = convert_parts(read_document(questions_pdf))
    paper, paper_warnings = build_paper(conversion, title)
    report = Conversion(conversion.paper_parts, conversion.crops, conversion.warnings + paper_warnings, conversion.checks)
    write_package(out_dir, paper, report, questions_pdf, None)
    return report
