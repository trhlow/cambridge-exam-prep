"""B1 Preliminary (for Schools) Reading: six parts, questions 1–32, laid out as on Inspera (thesis/refs/inspera-ui-b1.md).

Parts 1, 3, 5 and 6 look like their A2 counterparts (notices, an article, word choices, typed gaps) with four
options instead of three. Part 2 matches five people with eight texts on the facing page, and Part 4 removes five
sentences from an article: both become drag-and-drop on screen (DragDropMatch with long cards / gaps in the text).
"""

import re
from collections.abc import Callable
from pathlib import Path

from .answer_key import parse_answer_key
from .builders import Conversion, PartResult, choice_question, key_answers, make_group
from .model import Crop, Document, Line, Picture, read_document, read_text
from .parts import OPTION_LETTER, OPTION_WITH_TEXT, PartSource, is_question_number, parse_choice, question_regions, split_parts
from .text import GAP, escape, escape_attribute, gaps_before_capitals, gaps_to_slots, paragraphs, row_text, rows

EXAM_TYPE = "B1_PRELIM_FS"
SECTION = 1
PART_COUNT = 6
LAYOUTS = {1: "Single", 2: "Single", 3: "Split", 4: "Single", 5: "Single", 6: "Single"}

NOTICE_COLUMN_END = 290.0   # Part 1: notices left of this x, prompt and options right of it
PERSON_TEXT_START = 200.0   # Part 2: the people's descriptions start right of this x, photos and numbers left of it
CARD_COLUMN = 300.0         # Part 2: the second column of texts starts right of this x
BLOCK_GAP = 25.0            # Part 2: a vertical gap wider than this starts another person's description
TITLE_SIZE = 13.0
CROP_MARGIN = 4.0
PX_PER_POINT = 2.0
LINE_HEIGHT = 1.25          # a text line's box is about 1.25 × its font size high

BYLINE = re.compile(r"^by\s", re.IGNORECASE)
WORDS_PER_GAP = re.compile(r"Write (one|two|three|\d+) words? for each gap", re.IGNORECASE)
NUMBER_WORDS = {"one": 1, "two": 2, "three": 3}
PEOPLE_INTRO = re.compile(r"^The (.+?) below\b", re.IGNORECASE)
# The 2022 B1 Preliminary paper prints a removed sentence as "16  x xx." in a dotted font.
SENTENCE_GAP = re.compile(r"\b(\d{1,2})\s+x\s*xx\.?")
SPACE_BEFORE_STOP = re.compile(r"(\}\})\s+([.,;:!?])")


# ---------------------------------------------------------------------------------------------- shared builders

def _part(part_no: int, groups: list[dict], title: str | None = None, stimulus: str | None = None) -> dict:
    part: dict = {"partNo": part_no, "layout": LAYOUTS[part_no]}
    if title:
        part["title"] = title
    if stimulus:
        part["stimulusHtml"] = stimulus
    part["groups"] = groups
    return part


def _box(line: Line) -> tuple[float, float, float, float]:
    return line.x0, line.y0, line.x1, line.y0 + line.size * LINE_HEIGHT


def _crop(name: str, pictures: list[Picture], lines: list[Line] = ()) -> Crop:
    """Pictures and any printed text over or around them; one picture alone is saved from its own pixels."""
    boxes = [(p.x0, p.y0, p.x1, p.y1) for p in pictures] + [_box(line) for line in lines]
    page = pictures[0].page if pictures else lines[0].page
    return Crop(
        name=name,
        page=page,
        x0=max(0.0, min(b[0] for b in boxes) - CROP_MARGIN),
        y0=max(0.0, min(b[1] for b in boxes) - CROP_MARGIN),
        x1=max(b[2] for b in boxes) + CROP_MARGIN,
        y1=max(b[3] for b in boxes) + CROP_MARGIN,
        xref=pictures[0].xref if len(pictures) == 1 and not lines else 0,
    )


def _image(crop: Crop, alt: str) -> str:
    width = round((crop.x1 - crop.x0 - 2 * CROP_MARGIN) * PX_PER_POINT)
    return f"<img src='asset:{crop.name}' alt='{escape_attribute(alt)}' width='{width}'>"


def _title(lines: list[Line]) -> Line | None:
    return max((line for line in lines if line.size >= TITLE_SIZE), key=lambda line: line.size, default=None)


def _html_paragraphs(texts: list[str]) -> str:
    return "".join(f"<p>{escape(text)}</p>" for text in texts)


def _byline_and_rest(rows_: list[list[Line]]) -> tuple[str, list[list[Line]]]:
    """An 'by …, aged 13' line under the title is shown in italics, as on screen."""
    if rows_ and BYLINE.match(row_text(rows_[0])):
        return f"<p><em>{escape(row_text(rows_[0]))}</em></p>", rows_[1:]
    return "", rows_


def _tidy(html: str) -> str:
    """'a hot and wet {{q:21}} .' → 'a hot and wet {{q:21}}.': the paper leaves a space between the dots and the stop."""
    return SPACE_BEFORE_STOP.sub(r"\1\2", html)


def _capital_checks(part_no: int, texts: list[str]) -> list[str]:
    return [
        f"Part {part_no}, question {number}: a capital letter follows the gap; check that a full stop was not lost in the dots."
        for text in texts for number in gaps_before_capitals(text)
    ]


def _letter_options(lines: list[Line]) -> list[dict]:
    """Options that each start with a bold letter, alone ('A' + text beside it) or joined ('A text')."""
    options: list[dict] = []
    for row in rows(lines):
        for line in row:
            alone = line.bold and OPTION_LETTER.match(line.text)
            joined = line.bold and OPTION_WITH_TEXT.match(line.text)
            if alone:
                options.append({"key": line.text, "text": ""})
            elif joined:
                options.append({"key": joined[1], "text": joined[2]})
            elif options:
                options[-1]["text"] = f"{options[-1]['text']} {line.text}".strip()
    return options


def _grid_choice(lines: list[Line]) -> tuple[str, list[dict]] | None:
    """Options set out two by two (B1 Reading question 15: A B / C D), each letter above its own text."""
    letters = [line for line in lines if line.bold and OPTION_LETTER.match(line.text)]
    if not any(sum(1 for other in letters if abs(other.y0 - letter.y0) < 3) > 1 for letter in letters):
        return None
    top = min(letter.y0 for letter in letters)
    prompt = row_text([line for line in lines if line.y0 < top - 3])
    options = []
    for letter in sorted(letters, key=lambda item: item.text):
        right = min((other.x0 for other in letters if other.x0 > letter.x0 + 20), default=float("inf"))
        below = min((other.y0 for other in letters if abs(other.x0 - letter.x0) < 20 and other.y0 > letter.y0 + 3), default=float("inf"))
        text = [line for line in lines if line not in letters and letter.x0 - 5 <= line.x0 < right and letter.y0 - 3 <= line.y0 < below - 3]
        options.append({"key": letter.text, "text": " ".join(paragraphs(text))})
    return prompt, options


# ---------------------------------------------------------------------------------------------- the six parts

def _part1_notices(source: PartSource, key: dict[int, list[str]]) -> PartResult:
    """Each question: a notice, message or label on the left (a picture, printed text, or both), options on the right."""
    questions, crops, warnings = [], [], []
    for region in question_regions(source.lines):
        number = region.question
        prompt, options = parse_choice([line for line in region.lines if line.x0 >= NOTICE_COLUMN_END])
        pictures = [p for p in source.pictures if region.holds(p) and (p.x0 + p.x1) / 2 < NOTICE_COLUMN_END]
        printed = [line for line in region.lines if line.x1 < NOTICE_COLUMN_END]
        stimulus = None
        if pictures or printed:
            crop = _crop(f"images/r-p1-q{number}.png", pictures, printed)
            crops.append(crop)
            stimulus = _image(crop, f"Question {number}")
        else:
            warnings.append(f"Part 1, question {number}: no notice found beside the question.")
        question, problems = choice_question(1, number, prompt, options, key, stimulus)
        questions.append(question)
        warnings += problems
    return PartResult(_part(1, [make_group("SingleChoice", source, questions)]), crops, warnings)


def _part2_people_and_texts(source: PartSource, key: dict[int, list[str]]) -> PartResult:
    """Five people (photo, number, description) on the first page; eight lettered texts on the facing page."""
    pages = source.pages
    if len(pages) < 2:
        return PartResult(_part(2, []), [], ["Part 2: expected the people and the texts on two pages."])
    people_page, texts_page = pages[0], pages[1]
    numbers = sorted((line for line in source.lines if line.page == people_page and is_question_number(line)), key=lambda line: line.y0)
    described = [line for line in source.lines if line.page == people_page and line.x0 >= PERSON_TEXT_START]
    top = min((line.y0 for line in described), default=0.0)
    intro = [line for line in source.lines if line.page == people_page and line.x0 < PERSON_TEXT_START and line.y0 < top - 10 and not is_question_number(line)]

    # Descriptions are blocks of lines; each block belongs to the number closest to its middle.
    blocks: list[list[Line]] = []
    for row in rows(described):
        if blocks and row[0].y0 - blocks[-1][-1].y0 <= BLOCK_GAP:
            blocks[-1].extend(row)
        else:
            blocks.append(list(row))
    middle = lambda items: (min(i.y0 for i in items) + max(i.y0 for i in items)) / 2  # noqa: E731
    photos = [p for p in source.pictures if p.page == people_page and p.x1 <= PERSON_TEXT_START + 10]

    questions, crops, warnings = [], [], []
    for number in numbers:
        block = min(blocks, key=lambda b: abs(middle(b) - number.y0), default=[])
        photo = min(photos, key=lambda p: abs((p.y0 + p.y1) / 2 - number.y0), default=None)
        question: dict = {"number": int(number.text)}
        if photo:
            crop = _crop(f"images/r-p2-q{number.text}.png", [photo])
            crops.append(crop)
            question["stimulusHtml"] = _image(crop, f"Question {number.text}")
        question["promptHtml"] = _html_paragraphs([" ".join(paragraphs(block))]) if block else ""
        if not block:
            warnings.append(f"Part 2, question {number.text}: no description found beside the number.")
        question["answers"], problems = key_answers(2, int(number.text), key, None)
        warnings += problems
        questions.append(question)

    cards, pool_heading, card_warnings = _cards([line for line in source.lines if line.page == texts_page])
    warnings += card_warnings
    keys = [card["key"] for card in cards]
    for question in questions:
        if question["answers"] and question["answers"][0] not in keys:
            warnings.append(f"Part 2, question {question['number']}: answer {question['answers'][0]} is not one of the texts ({', '.join(keys)}).")

    intro_text = " ".join(paragraphs(intro)).replace("On the opposite page there are", "On the right there are")
    instructions = " ".join(text for text in (intro_text, source.instructions) if text)
    match = PEOPLE_INTRO.match(intro_text)
    group = make_group(
        "DragDropMatch", source, questions, instructions,
        itemsHeading=match[1].title() if match else "People",
        **({"poolHeading": pool_heading} if pool_heading else {}),
        optionPool=cards,
    )
    return PartResult(_part(2, [group]), crops, warnings)


def _cards(lines: list[Line]) -> tuple[list[dict], str | None, list[str]]:
    """Lettered texts in two columns: a bold letter, a bold name beside it, then the text under the name."""
    heading = _title(lines)
    letters = [line for line in lines if line.bold and OPTION_LETTER.match(line.text)]
    cards = []
    for letter in sorted(letters, key=lambda item: item.text):
        column = [line for line in lines if (line.x0 >= CARD_COLUMN) == (letter.x0 >= CARD_COLUMN) and line not in letters and line != heading]
        below = min((other.y0 for other in letters if (other.x0 >= CARD_COLUMN) == (letter.x0 >= CARD_COLUMN) and other.y0 > letter.y0 + 5), default=float("inf"))
        own = sorted((line for line in column if letter.y0 - 5 <= line.y0 < below - 5), key=lambda line: line.y0)
        name = next((line for line in own if line.bold and abs(line.y0 - letter.y0) < 5), None)
        text = [line for line in own if line is not name]
        card = {"key": letter.text}
        if name:
            card["title"] = name.text
        card["text"] = " ".join(paragraphs(text))
        cards.append(card)
    warnings = [] if len(cards) >= 5 else [f"Part 2: found {len(cards)} lettered texts; expected eight (A–H)."]
    return cards, heading.text if heading else None, warnings


def _part3_article(source: PartSource, key: dict[int, list[str]]) -> PartResult:
    regions = question_regions(source.lines)
    question_pages = {region.number.page for region in regions}
    questions, warnings = [], []
    for region in regions:
        prompt, options = _grid_choice(list(region.lines)) or parse_choice(list(region.lines))
        question, problems = choice_question(3, region.question, prompt, options, key)
        questions.append(question)
        warnings += problems

    text_lines = [line for line in source.lines if line.page not in question_pages]
    title_line = _title(text_lines)
    byline, remaining = _byline_and_rest(rows(line for line in text_lines if line != title_line))
    html = byline + _html_paragraphs(paragraphs(line for row in remaining for line in row))
    title = title_line.text if title_line else None
    return PartResult(_part(3, [make_group("SingleChoice", source, questions)], title, html), [], warnings)


def _part4_sentences(source: PartSource, key: dict[int, list[str]]) -> PartResult:
    """An article with five sentences taken out (gaps 16–20) and eight sentences A–H on the next page."""
    first, last = source.first or 16, source.last or 20
    gap_numbers = set(range(first, last + 1))
    lettered = [line.page for line in source.lines if line.bold and (OPTION_LETTER.match(line.text) or OPTION_WITH_TEXT.match(line.text))]
    letters_page = max(set(lettered), key=lettered.count) if lettered else None
    text_lines = [line for line in source.lines if line.page != letters_page]
    title_line = _title(text_lines)
    # "Five sentences have been removed from the text below." stands above the title: it is an instruction.
    above = [line for line in text_lines if title_line and line.page == title_line.page and line.y0 < title_line.y0]
    instructions = " ".join(text for text in (" ".join(paragraphs(above)), source.instructions) if text)
    byline, remaining = _byline_and_rest(rows(line for line in text_lines if line != title_line and line not in above))

    texts: list[str] = []
    previous: Line | None = None
    for row in remaining:
        words = []
        for line in row:
            if line.bold and line.text.isdigit() and int(line.text) in gap_numbers:
                words.append(f"{{{{q:{line.text}}}}}")  # the gap is a picture, its number printed over it
            else:
                text = escape(line.text)
                words.append(SENTENCE_GAP.sub(lambda m: f"{{{{q:{m[1]}}}}}" if int(m[1]) in gap_numbers else m[0], text))
        sentence = " ".join(words)
        new_paragraph = previous is None or row[0].y0 - previous.y0 > 2.2 * max(row[0].size, previous.size)
        if new_paragraph:
            texts.append(sentence)
        else:
            texts[-1] = f"{texts[-1]} {sentence}"
        previous = row[0]
    body = _tidy(byline + "".join(f"<p>{text}</p>" for text in texts))

    options = _letter_options([line for line in source.lines if line.page == letters_page]) if letters_page is not None else []
    questions, warnings = [], []
    for number in sorted(gap_numbers):
        answers, problems = key_answers(4, number, key, options)
        questions.append({"number": number, "answers": answers})
        warnings += problems
        if f"{{{{q:{number}}}}}" not in body:
            warnings.append(f"Part 4, question {number}: gap {number} not found in the text.")
    if len(options) < 6:
        warnings.append(f"Part 4: found {len(options)} sentences; expected eight (A–H).")
    group = make_group("DragDropMatch", source, questions, instructions, bodyHtml=body, optionPool=options)
    title = title_line.text if title_line else None
    return PartResult(_part(4, [group], title), [], warnings)


def _part5_word_choice(source: PartSource, key: dict[int, list[str]]) -> PartResult:
    regions = question_regions(source.lines)
    title_line = _title(list(source.lines))
    start = min(((region.number.page, region.top) for region in regions), default=(10**6, 0.0))
    passage = [line for line in source.lines if line != title_line and not is_question_number(line) and (line.page, line.y0) < start]
    texts = paragraphs(passage)
    body = _tidy("".join(f"<p>{gaps_to_slots(text)}</p>" for text in texts))

    questions, warnings = [], []
    for region in regions:
        options = _letter_options(list(region.lines))
        number = region.question
        answers, problems = key_answers(5, number, key, options)
        questions.append({"number": number, "options": options, "answers": answers})
        warnings += problems
        if f"{{{{q:{number}}}}}" not in body:
            warnings.append(f"Part 5, question {number}: gap ({number}) not found in the text.")
    title = title_line.text if title_line else None
    return PartResult(_part(5, [make_group("InlineChoice", source, questions, bodyHtml=body)], title), [], warnings, _capital_checks(5, texts))


def _part6_typed_gaps(source: PartSource, key: dict[int, list[str]]) -> PartResult:
    title_line = _title(list(source.lines))
    byline, remaining = _byline_and_rest(rows(line for line in source.lines if line != title_line))
    texts = paragraphs(line for row in remaining for line in row)
    body = _tidy(byline + "".join(f"<p>{gaps_to_slots(text)}</p>" for text in texts))
    numbers = sorted({int(n) for text in texts for n in GAP.findall(text) if int(n) > 0})

    questions, warnings = [], []
    for number in numbers:
        answers, problems = key_answers(6, number, key, None)
        questions.append({"number": number, "answers": answers})
        warnings += problems
    if source.first is not None and numbers != list(range(source.first, source.last + 1)):
        warnings.append(f"Part 6: found gaps {numbers}; the heading says questions {source.first}–{source.last}.")
    extra: dict = {"bodyHtml": body}
    words = WORDS_PER_GAP.search(source.instructions)
    if words:
        extra["maxWordsPerAnswer"] = NUMBER_WORDS.get(words[1].lower()) or int(words[1])
    title = title_line.text if title_line else None
    return PartResult(_part(6, [make_group("InlineText", source, questions, **extra)], title), [], warnings, _capital_checks(6, texts))


PART_READERS: dict[int, Callable[[PartSource, dict[int, list[str]]], PartResult]] = {
    1: _part1_notices,
    2: _part2_people_and_texts,
    3: _part3_article,
    4: _part4_sentences,
    5: _part5_word_choice,
    6: _part6_typed_gaps,
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
            warnings.append(f"Part {part_no} is not part of B1 Reading; it was left out.")
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
