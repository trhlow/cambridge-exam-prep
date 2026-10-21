"""A2 Key (for Schools) Listening: five parts, questions 1–25, one audio track per part (see FORMAT.md).

Part 1 asks a question over three pictures with A, B and C under them; the pictures may be photos, drawings (a
clock) or large printed text (a price), so each option is cut from the page between its letter and the question.
Part 2 is a form whose answers are typed into gaps, Parts 3 and 4 are A/B/C questions, and Part 5 matches people
to cards (drag and drop on screen).
"""

import re
import shutil
from collections.abc import Callable
from pathlib import Path

from .answer_key import parse_answer_key
from .builders import Conversion, PartResult, choice_question, key_answers, make_group, merge
from .model import Crop, Document, Line, Picture, Shape, read_document, read_text
from .parts import NUMBER_COLUMN, OPTION_LETTER, PartSource, parse_choice, question_regions, split_parts
from .text import escape, paragraphs, row_text, rows

EXAM_TYPE = "A2_KEY_FS"
SECTION = 2
PART_COUNT = 5

CROP_MARGIN = 4.0
LETTER_GAP = 1.0            # Part 1: the letters sit just under their pictures
OPTION_TEXT_SIZE = 16.0     # Part 1: printed options (a price) are set large; clock numbers and labels are not
WIDE_SHAPE = 300.0          # rules across the page are layout, not part of a picture
VALUE_COLUMN = 300.0        # Part 2: labels left of this x, the answers right of it, when no gap is numbered
COLUMN_MARGIN = 10.0        # Part 2: the answer column starts a little left of the "(6)" marks
CARD_COLUMN = 300.0         # Part 5: people left of this x, the cards right of it
TITLE_SIZE = 13.0
TITLE_LINE_SPAN = 30.0      # Part 2: a second title line starts within this distance of the first
TITLE_SIZE_SPREAD = 1.5     # Part 2: title lines may mix sizes a little (18 and 19 pt italic in A2 Key)

NUMBERED_PROMPT = re.compile(r"^(\d+)\s+(.+)$")
GAP_MARK = re.compile(r"^\((\d+)\)\s*(.*)$")
TIME = re.compile(r"^(\d{1,2})([.:])(\d{2})$")
TRANSFER_TIME = re.compile(r"^You now have \d+ minutes")


# ---------------------------------------------------------------------------------------------- Part 1

def _part1_pictures(source: PartSource, key: dict[int, list[str]], shapes: tuple[Shape, ...]) -> PartResult:
    questions, crops, warnings = [], [], []
    for region in question_regions(source.lines):
        number = region.question
        letters = sorted((line for line in region.lines if line.bold and OPTION_LETTER.match(line.text)), key=lambda l: l.x0)
        prompt = " ".join(line.text for line in region.lines if line not in letters and line.y0 <= region.number.y0 + 1)
        if len(letters) < 2:
            warnings.append(f"Part 1, question {number}: found {len(letters)} picture letter(s); expected A, B and C.")
            continue

        top = region.number.y0 + region.number.size
        bottom = min(letter.y0 for letter in letters) - LETTER_GAP
        centres = [(letter.x0 + letter.x1) / 2 for letter in letters]
        edges = [0.0] + [(a + b) / 2 for a, b in zip(centres, centres[1:])] + [float("inf")]
        boxes = _content_boxes(region.number.page, top, bottom, source, shapes, letters)

        options = []
        for index, letter in enumerate(letters):
            left, right = edges[index], edges[index + 1]
            inside = [_clip(box, left, right) for box in boxes if box[0] < right and box[2] > left]
            name = f"images/l-p1-q{number}-{letter.text.lower()}.png"
            option: dict = {"key": letter.text, "image": name}
            if not inside:
                warnings.append(f"Part 1, question {number}: nothing found above option {letter.text}.")
                inside = [(left, top, min(right, left + 160.0), bottom)]
            crops.append(Crop(
                name=name, page=region.number.page,
                x0=max(left, min(b[0] for b in inside) - CROP_MARGIN),
                y0=max(top, min(b[1] for b in inside) - CROP_MARGIN),
                x1=min(right, max(b[2] for b in inside) + CROP_MARGIN),
                y1=min(bottom, max(b[3] for b in inside) + CROP_MARGIN),
            ))
            printed = [line.text for line in source.lines
                       if line.page == region.number.page and line.size >= OPTION_TEXT_SIZE
                       and top <= line.y0 < bottom and left <= (line.x0 + line.x1) / 2 < right]
            if printed:
                option["text"] = " ".join(printed)
            options.append(option)

        question, problems = choice_question(1, number, prompt, options, key)
        questions.append(question)
        warnings += problems
    return PartResult({"partNo": 1, "layout": "Single", "groups": [make_group("ImageChoice", source, questions)]}, crops, warnings)


def _content_boxes(page: int, top: float, bottom: float, source: PartSource, shapes: tuple[Shape, ...],
                   letters: list[Line]) -> list[tuple[float, float, float, float]]:
    """Pictures, drawings and printed text between the question and its letters."""

    def within(y0: float, y1: float) -> bool:
        return y0 < bottom and y1 > top

    boxes = [(p.x0, p.y0, p.x1, p.y1) for p in source.pictures if p.page == page and within(p.y0, p.y1)]
    boxes += [(s.x0, s.y0, s.x1, s.y1) for s in shapes
              if s.page == page and within(s.y0, s.y1) and s.x1 - s.x0 < WIDE_SHAPE]
    boxes += [(l.x0, l.y0, l.x1, l.y0 + l.size) for l in source.lines
              if l.page == page and l not in letters and top <= l.y0 < bottom]
    return boxes


def _clip(box: tuple[float, float, float, float], left: float, right: float) -> tuple[float, float, float, float]:
    return max(box[0], left), box[1], min(box[2], right), box[3]


# ---------------------------------------------------------------------------------------------- Part 2

def _part2_form(source: PartSource, key: dict[int, list[str]], _shapes: tuple[Shape, ...]) -> PartResult:
    # The answers sit in the column of the "(6)" marks; papers place it differently (x 294 in A2 Key, 321 in
    # A2 Key for Schools), so the column is measured, not fixed.
    marks = [line.x0 for line in source.lines if GAP_MARK.match(line.text)]
    column = min(marks) - COLUMN_MARGIN if marks else VALUE_COLUMN
    candidates = [line for line in source.lines if line.size >= TITLE_SIZE and line.x0 < column and not GAP_MARK.match(line.text)]
    title_lines: list[Line] = []
    if candidates:
        # A title may run over two lines ("Jobs for students / with Sunshine Holidays"): same size, one under the other.
        first = max(candidates, key=lambda line: line.size)
        title_lines = sorted((line for line in candidates if abs(line.size - first.size) <= TITLE_SIZE_SPREAD
                              and line.page == first.page and abs(line.y0 - first.y0) <= TITLE_LINE_SPAN), key=lambda line: line.y0)
    intro: list[Line] = []
    form: list[tuple[str, str]] = []
    numbers: list[int] = []
    for row in rows(line for line in source.lines if line not in title_lines):
        labels = [line for line in row if line.x0 < column]
        values = [line for line in row if line.x0 >= column]
        if not values:
            intro.extend(row)
            continue
        cell, number = _form_value(values)
        form.append((row_text(labels), cell))
        if number is not None:
            numbers.append(number)

    html = "".join(f"<p>{escape(text)}</p>" for text in paragraphs(intro))
    html += "<table>" + "".join(f"<tr><td>{escape(label)}</td><td>{value}</td></tr>" for label, value in form) + "</table>"

    questions, warnings, checks = [], [], []
    for number in numbers:
        answers, problems = key_answers(2, number, key, None)
        times = _other_time_forms(answers)
        if times:
            checks.append(f"Part 2, question {number}: the key gives a time ({answers[0]}); {', '.join(times)} is accepted too.")
        questions.append({"number": number, "answers": answers + times})
        warnings += problems
    if source.first is not None and numbers != list(range(source.first, source.last + 1)):
        warnings.append(f"Part 2: found gaps {numbers}; the heading says questions {source.first}–{source.last}.")

    part: dict = {"partNo": 2, "layout": "Single"}
    if title_lines:
        part["title"] = " ".join(line.text for line in title_lines)
    part["groups"] = [make_group("InlineText", source, questions, bodyHtml=html)]
    return PartResult(part, [], warnings, checks)


def _form_value(values: list[Line]) -> tuple[str, int | None]:
    """'(6) Mrs' + blank → 'Mrs {{q:6}}'; '(8)' + blank + 'a.m.' → '{{q:8}} a.m.': words printed with the number
    come before the answer line, words further right come after it."""
    before, after, number = [], [], None
    for line in sorted(values, key=lambda item: item.x0):
        mark = GAP_MARK.match(line.text)
        if mark and number is None:
            number = int(mark[1])
            if mark[2]:
                before.append(mark[2])
        elif number is None:
            before.append(line.text)
        else:
            after.append(line.text)
    if number is None:
        return escape(" ".join(before)), None
    words = [escape(" ".join(before))] if before else []
    words.append(f"{{{{q:{number}}}}}")
    if after:
        words.append(escape(" ".join(after)))
    return " ".join(words), number


def _other_time_forms(answers: list[str]) -> list[str]:
    """'7.30' is also '7:30' (and the other way round): both are how a time is written."""
    extra = []
    for answer in answers:
        match = TIME.match(answer)
        if match:
            other = f"{match[1]}{':' if match[2] == '.' else '.'}{match[3]}"
            if other not in answers and other not in extra:
                extra.append(other)
    return extra


# ---------------------------------------------------------------------------------------------- Parts 3 and 4

def _choice_part(source: PartSource, key: dict[int, list[str]], _shapes: tuple[Shape, ...]) -> PartResult:
    lines = _split_numbered_prompts(source.lines)
    regions = question_regions(lines)
    first = min(((region.number.page, region.top) for region in regions), default=(10**6, 0.0))
    intro = [line for line in lines if (line.page, line.y0) < first]

    questions, warnings = [], []
    for region in regions:
        prompt, options = parse_choice(list(region.lines))
        question, problems = choice_question(source.part_no, region.question, prompt, options, key)
        questions.append(question)
        warnings += problems
    instructions = " ".join([source.instructions] + paragraphs(intro)).strip()
    group = make_group("SingleChoice", source, questions, instructions)
    return PartResult({"partNo": source.part_no, "layout": "Single", "groups": [group]}, [], warnings)


def _split_numbered_prompts(lines: tuple[Line, ...]) -> tuple[Line, ...]:
    """'11 Annie saw a film at' (number and prompt on one bold line) → the number, and the prompt as plain text so
    a prompt that starts with 'A ' is not read as option A."""
    result = []
    for line in lines:
        match = NUMBERED_PROMPT.match(line.text) if line.bold and line.x0 < NUMBER_COLUMN else None
        if match:
            width = 5.5 * len(match[1])
            result.append(Line(line.page, line.x0, line.y0, line.x0 + width, match[1], bold=True, size=line.size))
            result.append(Line(line.page, line.x0 + width + 5.5, line.y0, line.x1, match[2], bold=False, size=line.size))
        else:
            result.append(line)
    return tuple(result)


# ---------------------------------------------------------------------------------------------- Part 5

def _part5_cards(source: PartSource, key: dict[int, list[str]], _shapes: tuple[Shape, ...]) -> PartResult:
    lines = [line for line in source.lines if not TRANSFER_TIME.match(line.text)]
    numbered = [line for line in lines if line.bold and line.x0 < NUMBER_COLUMN and line.text.isdigit()]
    first = min(((line.page, line.y0) for line in numbered), default=(10**6, 0.0))
    intro = [line for line in lines if (line.page, line.y0) < first and not line.bold]

    pool: list[dict] = []
    for row in rows(line for line in lines if line.x0 >= CARD_COLUMN):
        if row[0].bold and OPTION_LETTER.match(row[0].text):
            pool.append({"key": row[0].text, "text": row_text(row[1:])})

    questions, warnings, example = [], [], None
    for number_line in sorted(numbered, key=lambda line: (line.page, line.y0)):
        same_row = sorted((line for line in lines if line.page == number_line.page and line is not number_line
                           and abs(line.y0 - number_line.y0) <= 3.0 and line.x0 < CARD_COLUMN), key=lambda l: l.x0)
        name = " ".join(line.text for line in same_row if not (line.bold and OPTION_LETTER.match(line.text)))
        number = int(number_line.text)
        if number == 0:
            letter = next((line.text for line in same_row if line.bold and OPTION_LETTER.match(line.text)), None)
            card = next((option["text"] for option in pool if option["key"] == letter), None)
            example = f"Example: {name} – {card}." if card else None
            continue
        answers, problems = key_answers(5, number, key, pool)
        questions.append({"number": number, "promptHtml": escape(name), "answers": answers})
        warnings += problems
    if len(pool) < 2:
        warnings.append(f"Part 5: found {len(pool)} card(s) beside the names; expected A–H.")

    instructions = " ".join([source.instructions] + paragraphs(intro) + ([example] if example else [])).strip()
    group = make_group("DragDropMatch", source, questions, instructions, optionPool=pool)
    return PartResult({"partNo": 5, "layout": "Single", "groups": [group]}, [], warnings)


PART_READERS: dict[int, Callable[[PartSource, dict[int, list[str]], tuple[Shape, ...]], PartResult]] = {
    1: _part1_pictures,
    2: _part2_form,
    3: _choice_part,
    4: _choice_part,
    5: _part5_cards,
}


# ---------------------------------------------------------------------------------------------- whole paper

def convert_parts(document: Document, key: dict[int, list[str]]) -> Conversion:
    results: dict[int, PartResult] = {}
    extra: list[str] = []
    for part_no, source in split_parts(document).items():
        reader = PART_READERS.get(part_no)
        if reader is None:
            extra.append(f"Part {part_no} is not part of A2 Listening; it was left out.")
            continue
        results[part_no] = reader(source, key, document.shapes)

    conversion = merge(results, extra)
    numbers = {q["number"] for part in conversion.paper_parts.values() for group in part["groups"] for q in group["questions"]}
    conversion.warnings.extend(f"The answer key has question {n}, which is not in the paper." for n in sorted(set(key) - numbers))
    return conversion


def build_paper(conversion: Conversion, title: str, audio: dict[int, str]) -> tuple[dict, list[str]]:
    """`audio` maps a part number to its track inside the package, e.g. {1: 'audio/part1.mp3'}."""
    warnings = [f"Part {n} was not found in the PDF." for n in range(1, PART_COUNT + 1) if n not in conversion.paper_parts]
    parts = []
    for part_no in sorted(conversion.paper_parts):
        part = dict(conversion.paper_parts[part_no])
        if part_no in audio:
            part = {"partNo": part["partNo"], "layout": part["layout"], "audio": audio[part_no],
                    **{k: v for k, v in part.items() if k not in ("partNo", "layout")}}
        else:
            warnings.append(f"Part {part_no} has no audio track.")
        parts.append(part)
    numbers = [q["number"] for part in parts for group in part["groups"] for q in group["questions"]]
    if numbers != list(range(1, len(numbers) + 1)):
        warnings.append(f"Question numbers must run 1, 2, 3… without gaps; found {numbers}.")
    paper = {"formatVersion": 1, "examType": EXAM_TYPE, "section": SECTION, "title": title, "parts": parts}
    return paper, warnings


AUDIO_NAME = re.compile(r"^(?:part|p)[\s_-]*(\d+)(?!\d)", re.IGNORECASE)


def find_audio(folder: Path) -> dict[int, Path]:
    """Tracks named by part: 'P1_….mp3', 'part2.mp3', 'Part 3 - ….m4a'."""
    tracks: dict[int, Path] = {}
    for path in sorted(folder.iterdir()):
        match = AUDIO_NAME.match(path.name)
        if match and path.suffix.lower() in (".mp3", ".m4a", ".ogg", ".wav"):
            tracks.setdefault(int(match[1]), path)
    return tracks


def convert_files(questions_pdf: Path, key_pdf: Path, audio_dir: Path, out_dir: Path, title: str) -> Conversion:
    from .output import write_package

    conversion = convert_parts(read_document(questions_pdf), parse_answer_key(read_text(key_pdf)))
    tracks = {n: path for n, path in find_audio(audio_dir).items() if 1 <= n <= PART_COUNT}
    names = {n: f"audio/part{n}{path.suffix.lower()}" for n, path in tracks.items()}
    paper, paper_warnings = build_paper(conversion, title, names)
    report = Conversion(conversion.paper_parts, conversion.crops, conversion.warnings + paper_warnings, conversion.checks)
    write_package(out_dir, paper, report, questions_pdf, key_pdf)
    (out_dir / "audio").mkdir(exist_ok=True)
    for n, path in tracks.items():
        shutil.copyfile(path, out_dir / names[n])
    return report
