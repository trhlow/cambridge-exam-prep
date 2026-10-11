"""A2 Key (for Schools) Reading & Writing: seven parts, questions 1–32, laid out as on Inspera (see FORMAT.md).

Each part has its own reader because each looks different on paper: notices beside options (Part 1), three people
and a grid of A/B/C columns (Part 2), an article (Part 3), a gapped text with word choices (Part 4), an email with
typed gaps (Part 5), and two writing tasks (Parts 6 and 7).
"""

import re
from collections.abc import Callable
from dataclasses import dataclass, field
from pathlib import Path

from .answer_key import parse_answer_key
from .model import Crop, Document, Line, Picture, read_document, read_text
from .parts import (
    OPTION_LETTER, ROW_SLACK, PartSource, Region, is_question_number, parse_choice, question_regions, split_parts,
)
from .text import GAP, escape, escape_attribute, gaps_before_capitals, gaps_to_slots, paragraphs, row_text, rows

EXAM_TYPE = "A2_KEY_FS"
SECTION = 1
PART_COUNT = 7
LAYOUTS = {1: "Single", 2: "Split", 3: "Split", 4: "Single", 5: "Single", 6: "Split", 7: "Single"}
ESSAY_POINTS = 15

NOTICE_COLUMN_END = 290.0   # Part 1: notices left of this x, prompt and options right of it
NAME_GRID_START = 330.0     # Part 2: the A/B/C answer columns under the names start right of this x
PERSON_COLUMN = 120.0       # Part 2: names and photos left of this x, texts right of it
TITLE_SIZE = 13.0           # titles are set larger than the 11 pt body text
CROP_MARGIN = 4.0
PX_PER_POINT = 2.0          # on screen a picture is twice its printed size in points: notices ≈ 355 px, photos ≈ 165 px
BULLET = "•"

WORDS_PER_GAP = re.compile(r"Write (one|two|three|\d+) words? for each gap", re.IGNORECASE)
MIN_WORDS = re.compile(r"Write (\d+) words or more")
BYLINE = re.compile(r"^by\s", re.IGNORECASE)
NUMBER_WORDS = {"one": 1, "two": 2, "three": 3}


@dataclass(frozen=True)
class PartResult:
    part: dict
    crops: list[Crop] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)
    checks: list[str] = field(default_factory=list)


@dataclass(frozen=True)
class Conversion:
    paper_parts: dict[int, dict]
    crops: list[Crop]
    warnings: list[str]
    checks: list[str]


# ---------------------------------------------------------------------------------------------- shared builders

def _part(part_no: int, groups: list[dict], title: str | None = None, stimulus: str | None = None) -> dict:
    part: dict = {"partNo": part_no, "layout": LAYOUTS[part_no]}
    if title:
        part["title"] = title
    if stimulus:
        part["stimulusHtml"] = stimulus
    part["groups"] = groups
    return part


def _group(interaction: str, source: PartSource, questions: list[dict], **extra) -> dict:
    group: dict = {"interactionType": interaction}
    if source.instructions:
        group["instructions"] = source.instructions
    group.update(extra)
    group["questions"] = questions
    return group


def _answers(part_no: int, number: int, key: dict[int, list[str]], options: list[dict] | None) -> tuple[list[str], list[str]]:
    answers = key.get(number, [])
    if not answers:
        return [], [f"Part {part_no}, question {number}: no answer in the answer key."]
    if options is not None:
        keys = [option["key"] for option in options]
        if len(answers) != 1 or answers[0] not in keys:
            return answers, [f"Part {part_no}, question {number}: answer {' / '.join(answers)} is not one of the options ({', '.join(keys)})."]
    return answers, []


def _choice_question(part_no: int, number: int, prompt: str, options: list[dict], key: dict[int, list[str]],
                     stimulus: str | None = None) -> tuple[dict, list[str]]:
    question: dict = {"number": number}
    if prompt:
        question["promptHtml"] = escape(prompt)
    if stimulus:
        question["stimulusHtml"] = stimulus
    question["options"] = options
    question["answers"], warnings = _answers(part_no, number, key, options)
    if len(options) < 2:
        warnings.append(f"Part {part_no}, question {number}: found {len(options)} option(s); expected A, B and C.")
    return question, warnings


def _crop(name: str, pictures: list[Picture]) -> Crop:
    return Crop(
        name=name,
        page=pictures[0].page,
        x0=max(0.0, min(p.x0 for p in pictures) - CROP_MARGIN),
        y0=max(0.0, min(p.y0 for p in pictures) - CROP_MARGIN),
        x1=max(p.x1 for p in pictures) + CROP_MARGIN,
        y1=max(p.y1 for p in pictures) + CROP_MARGIN,
        xref=pictures[0].xref if len(pictures) == 1 else 0,
    )


def _image(crop: Crop, alt: str) -> str:
    width = round((crop.x1 - crop.x0 - 2 * CROP_MARGIN) * PX_PER_POINT)
    return f"<img src='asset:{crop.name}' alt='{escape_attribute(alt)}' width='{width}'>"


def _title(lines: list[Line]) -> Line | None:
    return max((line for line in lines if line.size >= TITLE_SIZE), key=lambda line: line.size, default=None)


def _html_paragraphs(texts: list[str]) -> str:
    return "".join(f"<p>{escape(text)}</p>" for text in texts)


def _question_pages(regions: list[Region]) -> set[int]:
    return {region.number.page for region in regions}


# ---------------------------------------------------------------------------------------------- the seven parts

def _part1_notices(source: PartSource, key: dict[int, list[str]]) -> PartResult:
    questions, crops, warnings = [], [], []
    for region in question_regions(source.lines):
        number = region.question
        prompt, options = parse_choice([line for line in region.lines if line.x0 >= NOTICE_COLUMN_END])
        notice = [p for p in source.pictures if region.holds(p) and (p.x0 + p.x1) / 2 < NOTICE_COLUMN_END]
        stimulus = None
        if notice:
            crop = _crop(f"images/p1-q{number}.png", notice)
            crops.append(crop)
            stimulus = _image(crop, f"Question {number}")
        else:
            warnings.append(f"Part 1, question {number}: no notice picture found beside the question.")
        question, problems = _choice_question(1, number, prompt, options, key, stimulus)
        questions.append(question)
        warnings += problems
    return PartResult(_part(1, [_group("SingleChoice", source, questions)]), crops, warnings)


def _part2_people(source: PartSource, key: dict[int, list[str]]) -> PartResult:
    regions = question_regions(source.lines)
    question_pages = _question_pages(regions)
    first_top = min((region.top for region in regions), default=float("inf"))
    names = sorted(
        (line for line in source.lines
         if line.page in question_pages and line.bold and line.y0 < first_top and line.x0 >= NAME_GRID_START),
        key=lambda line: line.x0,
    )
    questions, warnings = [], []
    for region in regions:
        prompt = " ".join(row_text(row) for row in rows(line for line in region.lines if line.x0 < NAME_GRID_START))
        letters = sorted(
            (line for line in region.lines
             if line.x0 >= NAME_GRID_START and line.bold and OPTION_LETTER.match(line.text)),
            key=lambda line: line.x0,
        )
        options = (
            [{"key": letter.text, "text": _nearest(names, letter).text} for letter in letters] if letters and names
            else [{"key": "ABCDEFGH"[index], "text": name.text} for index, name in enumerate(names)]
        )
        question, problems = _choice_question(2, region.question, prompt, options, key)
        questions.append(question)
        warnings += problems

    title, stimulus, crops, text_warnings = _people_texts(
        [line for line in source.lines if line.page not in question_pages],
        [picture for picture in source.pictures if picture.page not in question_pages],
    )
    return PartResult(_part(2, [_group("SingleChoice", source, questions)], title, stimulus), crops, warnings + text_warnings)


def _nearest(names: list[Line], letter: Line) -> Line:
    centre = (letter.x0 + letter.x1) / 2
    return min(names, key=lambda name: abs((name.x0 + name.x1) / 2 - centre))


def _people_texts(lines: list[Line], pictures: list[Picture]) -> tuple[str | None, str, list[Crop], list[str]]:
    """Name (and photo) on the left, their text on the right, one person after another."""
    title_line = _title(lines)
    people = sorted(
        (line for line in lines if line.bold and line.x0 < PERSON_COLUMN and line != title_line),
        key=lambda line: (line.page, line.y0),
    )
    body = [line for line in lines if line.x0 >= PERSON_COLUMN and line != title_line]
    title = title_line.text if title_line else None
    if not people:
        return title, _html_paragraphs(paragraphs(body)), [], ["Part 2: no names found beside the texts; check the text."]

    html, crops = [], []
    for index, person in enumerate(people):
        following = people[index + 1] if index + 1 < len(people) else None

        def belongs(page: int, y: float) -> bool:
            after = (page, y) >= (person.page, person.y0 - ROW_SLACK)
            before = following is None or (page, y) < (following.page, following.y0 - ROW_SLACK)
            return after and before

        html.append(f"<h3>{escape(person.text)}</h3>")
        photo = [p for p in pictures if p.x0 < PERSON_COLUMN and belongs(p.page, p.y0)]
        if photo:
            crop = _crop(f"images/p2-{index + 1}.png", photo)
            crops.append(crop)
            html.append(f"<p>{_image(crop, person.text)}</p>")
        html.append(_html_paragraphs(paragraphs(line for line in body if belongs(line.page, line.y0))))
    return title, "".join(html), crops, []


def _part3_article(source: PartSource, key: dict[int, list[str]]) -> PartResult:
    regions = question_regions(source.lines)
    question_pages = _question_pages(regions)
    questions, warnings = [], []
    for region in regions:
        prompt, options = parse_choice(list(region.lines))
        question, problems = _choice_question(3, region.question, prompt, options, key)
        questions.append(question)
        warnings += problems

    text_lines = [line for line in source.lines if line.page not in question_pages]
    title_line = _title(text_lines)
    remaining = rows(line for line in text_lines if line != title_line)
    html = []
    if remaining and BYLINE.match(row_text(remaining[0])):
        html.append(f"<p><em>{escape(row_text(remaining[0]))}</em></p>")
        remaining = remaining[1:]
    html.append(_html_paragraphs(paragraphs(line for row in remaining for line in row)))
    title = title_line.text if title_line else None
    return PartResult(_part(3, [_group("SingleChoice", source, questions)], title, "".join(html)), [], warnings)


def _part4_word_choice(source: PartSource, key: dict[int, list[str]]) -> PartResult:
    regions = question_regions(source.lines)
    title_line = _title(list(source.lines))
    start = min(((region.number.page, region.top) for region in regions), default=(10**6, 0.0))
    passage = [
        line for line in source.lines
        if line != title_line and not is_question_number(line) and (line.page, line.y0) < start
    ]
    texts = paragraphs(passage)
    body = "".join(f"<p>{gaps_to_slots(text)}</p>" for text in texts)
    checks = _capital_checks(4, texts)

    questions, warnings = [], []
    for region in regions:
        options = _options_in_a_row(region.lines)
        number = region.question
        answers, problems = _answers(4, number, key, options)
        questions.append({"number": number, "options": options, "answers": answers})
        warnings += problems
        if f"{{{{q:{number}}}}}" not in body:
            warnings.append(f"Part 4, question {number}: gap ({number}) not found in the text.")
    group = _group("InlineChoice", source, questions, bodyHtml=body)
    title = title_line.text if title_line else None
    return PartResult(_part(4, [group], title), [], warnings, checks)


def _capital_checks(part_no: int, texts: list[str]) -> list[str]:
    return [
        f"Part {part_no}, question {number}: a capital letter follows the gap; check that a full stop was not lost in the dots."
        for text in texts for number in gaps_before_capitals(text)
    ]


def _options_in_a_row(lines: tuple[Line, ...]) -> list[dict]:
    """'A lot  B many  C much': each bold letter starts an option; the words after it are its text."""
    options: list[dict] = []
    for line in sorted(lines, key=lambda item: (item.y0 // 10, item.x0)):
        if line.bold and OPTION_LETTER.match(line.text):
            options.append({"key": line.text, "text": ""})
        elif options:
            options[-1]["text"] = f"{options[-1]['text']} {line.text}".strip()
    return options


def _part5_typed_gaps(source: PartSource, key: dict[int, list[str]]) -> PartResult:
    header: dict[str, str] = {}
    example = None
    body_lines: list[Line] = []
    for row in rows(source.lines):
        label = row[0].text
        if label in ("From:", "To:"):
            header[label] = " ".join(line.text for line in row[1:])
        elif label.startswith("Example"):
            example = " ".join(line.text for line in row[1:] if not line.text.isdigit()) or None
        else:
            body_lines.extend(row)

    texts = paragraphs(body_lines)
    html = []
    if header:
        html.append("<p>" + "<br>".join(f"<strong>{label}</strong> {escape(value)}" for label, value in header.items()) + "</p>")
    html += [f"<p>{gaps_to_slots(text, example)}</p>" for text in texts]
    numbers = sorted({int(n) for text in texts for n in GAP.findall(text) if int(n) > 0})

    questions, warnings = [], []
    for number in numbers:
        answers, problems = _answers(5, number, key, None)
        questions.append({"number": number, "answers": answers})
        warnings += problems
    if source.first is not None and numbers != list(range(source.first, source.last + 1)):
        warnings.append(f"Part 5: found gaps {numbers}; the heading says questions {source.first}–{source.last}.")
    extra = {}
    words = WORDS_PER_GAP.search(source.instructions)
    if words:
        extra["maxWordsPerAnswer"] = NUMBER_WORDS.get(words[1].lower()) or int(words[1])
    extra["bodyHtml"] = "".join(html)
    return PartResult(_part(5, [_group("InlineText", source, questions, **extra)]), [], warnings, _capital_checks(5, texts))


def _writing_task(source: PartSource, key: dict[int, list[str]]) -> PartResult:
    part_no = source.part_no
    html: list[str] = []
    bullets: list[str] = []
    for row in rows(source.lines):
        if row[0].text.startswith(BULLET):
            bullets.append(row_text(row).lstrip(BULLET).strip())
            continue
        if bullets:
            html.append("<ul>" + "".join(f"<li>{escape(item)}</li>" for item in bullets) + "</ul>")
            bullets = []
        html.append(f"<p>{escape(row_text(row))}</p>")
    if bullets:
        html.append("<ul>" + "".join(f"<li>{escape(item)}</li>" for item in bullets) + "</ul>")

    crops = [
        _crop(f"images/p{part_no}-{index + 1}.png", [picture])
        for index, picture in enumerate(sorted(source.pictures, key=lambda p: (p.page, p.x0, p.y0)))
    ]
    if crops:
        html.append("<p>" + " ".join(_image(crop, f"Picture {i + 1}") for i, crop in enumerate(crops)) + "</p>")

    warnings = []
    minimum = MIN_WORDS.search(source.instructions)
    if not minimum:
        warnings.append(f"Part {part_no}: no 'Write N words or more' line found.")
    if source.first is None:
        warnings.append(f"Part {part_no}: no 'Question n' heading found.")
    question = {"number": source.first or 0, "points": ESSAY_POINTS, "minWords": int(minimum[1]) if minimum else 0}
    return PartResult(_part(part_no, [_group("Essay", source, [question])], stimulus="".join(html)), crops, warnings)


PART_READERS: dict[int, Callable[[PartSource, dict[int, list[str]]], PartResult]] = {
    1: _part1_notices,
    2: _part2_people,
    3: _part3_article,
    4: _part4_word_choice,
    5: _part5_typed_gaps,
    6: _writing_task,
    7: _writing_task,
}


# ---------------------------------------------------------------------------------------------- whole paper

def convert_parts(document: Document, key: dict[int, list[str]]) -> Conversion:
    parts: dict[int, dict] = {}
    crops: list[Crop] = []
    warnings: list[str] = []
    checks: list[str] = []
    for part_no, source in split_parts(document).items():
        reader = PART_READERS.get(part_no)
        if reader is None:
            warnings.append(f"Part {part_no} is not part of A2 Reading & Writing; it was left out.")
            continue
        result = reader(source, key)
        parts[part_no] = result.part
        crops += result.crops
        warnings += result.warnings
        checks += result.checks

    numbers = {q["number"] for part in parts.values() for group in part["groups"] for q in group["questions"]}
    warnings += [f"The answer key has question {n}, which is not in the paper." for n in sorted(set(key) - numbers)]
    return Conversion(parts, crops, warnings, checks)


def build_paper(conversion: Conversion, title: str) -> tuple[dict, list[str]]:
    warnings = [f"Part {n} was not found in the PDF." for n in range(1, PART_COUNT + 1) if n not in conversion.paper_parts]
    parts = [conversion.paper_parts[n] for n in sorted(conversion.paper_parts)]
    numbers = [q["number"] for part in parts for group in part["groups"] for q in group["questions"]]
    if numbers != list(range(1, len(numbers) + 1)):
        warnings.append(f"Question numbers must run 1, 2, 3… without gaps; found {numbers}.")
    paper = {"formatVersion": 1, "examType": EXAM_TYPE, "section": SECTION, "title": title, "parts": parts}
    return paper, warnings


def convert_files(questions_pdf: Path, key_pdf: Path, out_dir: Path, title: str) -> Conversion:
    from .output import write_package

    conversion = convert_parts(read_document(questions_pdf), parse_answer_key(read_text(key_pdf)))
    paper, paper_warnings = build_paper(conversion, title)
    report = Conversion(conversion.paper_parts, conversion.crops, conversion.warnings + paper_warnings, conversion.checks)
    write_package(out_dir, paper, report, questions_pdf, key_pdf)
    return report
