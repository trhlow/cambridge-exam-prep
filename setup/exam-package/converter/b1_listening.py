"""B1 Preliminary (for Schools) Listening: four parts, questions 1–25, one audio track per part.

Part 1 (seven questions with three pictures each) and Part 4 (an interview, A/B/C) read like A2 Listening Parts 1
and 3. Part 2 gives each question its own line of context ("You will hear two friends talking about …") above the
question. Part 3 is a page of sentences with one gap each, written with one or two words, a number, a date or a time.
"""

import re
import shutil
from collections.abc import Callable
from pathlib import Path

from . import a2_listening
from .answer_key import parse_answer_key
from .builders import Conversion, PartResult, choice_question, key_answers, make_group, merge
from .model import Document, Line, Shape, read_document, read_text
from .parts import OPTION_LETTER, OPTION_WITH_TEXT, PartSource, parse_choice, question_regions, split_parts
from .text import escape, paragraphs, row_text, rows

EXAM_TYPE = "B1_PRELIM_FS"
SECTION = 3
PART_COUNT = 4
TITLE_SIZE = 13.0
WORDS_PER_GAP = re.compile(r"Write (one|two|one or two|\d+) words?", re.IGNORECASE)
SENTENCE_GAP = re.compile(r"\((\d+)\)\s*$")
SPACE_BEFORE_STOP = re.compile(r"(\}\})\s+([.,;:!?])")


def _part2_context_choice(source: PartSource, key: dict[int, list[str]], _shapes: tuple[Shape, ...]) -> PartResult:
    """Each question: a line of context and the question (both shown in bold on screen), then A/B/C."""
    lines = a2_listening._split_numbered_prompts(source.lines)
    questions, warnings = [], []
    for region in question_regions(lines):
        _, options = parse_choice(list(region.lines))
        prompt_rows = []
        for row in rows(region.lines):
            if row[0].bold and (OPTION_LETTER.match(row[0].text) or OPTION_WITH_TEXT.match(row[0].text)):
                break
            prompt_rows.append(row_text(row))
        question, problems = choice_question(2, region.question, "", options, key)
        question = {"number": question["number"], "promptHtml": "<br>".join(escape(text) for text in prompt_rows), **{k: v for k, v in question.items() if k != "number"}}
        questions.append(question)
        warnings += problems
    group = make_group("SingleChoice", source, questions)
    return PartResult({"partNo": 2, "layout": "Single", "groups": [group]}, [], warnings)


def _part3_sentences(source: PartSource, key: dict[int, list[str]], _shapes: tuple[Shape, ...]) -> PartResult:
    """A title and sentences, each with a gap '(14)' followed by a drawn line; the rest of the sentence follows it."""
    title_line = max((line for line in source.lines if line.size >= TITLE_SIZE), key=lambda line: line.size, default=None)
    intro = [line for line in source.lines if title_line and (line.page, line.y0) < (title_line.page, title_line.y0)]
    body_lines = [line for line in source.lines if line is not title_line and line not in intro]

    numbers: list[int] = []
    texts: list[str] = []
    previous: Line | None = None
    for row in rows(body_lines):
        words = []
        for line in row:
            match = SENTENCE_GAP.search(line.text)
            if match:
                numbers.append(int(match[1]))
                words.append(f"{escape(line.text[:match.start()].strip())} {{{{q:{match[1]}}}}}".strip())
            else:
                words.append(escape(line.text))
        sentence = " ".join(words)
        if previous is None or row[0].y0 - previous.y0 > 2.2 * max(row[0].size, previous.size):
            texts.append(sentence)
        else:
            texts[-1] = f"{texts[-1]} {sentence}"
        previous = row[0]
    body = SPACE_BEFORE_STOP.sub(r"\1\2", "".join(f"<p>{text}</p>" for text in texts))

    questions, warnings = [], []
    for number in numbers:
        answers, problems = key_answers(3, number, key, None)
        questions.append({"number": number, "answers": answers})
        warnings += problems
    if source.first is not None and numbers != list(range(source.first, source.last + 1)):
        warnings.append(f"Part 3: found gaps {numbers}; the heading says questions {source.first}–{source.last}.")

    instructions = " ".join([source.instructions] + paragraphs(intro)).strip()
    extra: dict = {"bodyHtml": body}
    words = WORDS_PER_GAP.search(instructions)
    if words:
        extra["maxWordsPerAnswer"] = {"one": 1, "two": 2, "one or two": 2}.get(words[1].lower()) or int(words[1])
    checks = [f"Part 3: answers accept the key's optional words and listed misspellings ({sum(len(q['answers']) for q in questions)} forms in all)."]
    group = make_group("InlineText", source, questions, instructions, **extra)
    part = {"partNo": 3, "layout": "Single", **({"title": title_line.text} if title_line else {}), "groups": [group]}
    return PartResult(part, [], warnings, checks)


PART_READERS: dict[int, Callable[[PartSource, dict[int, list[str]], tuple[Shape, ...]], PartResult]] = {
    1: a2_listening._part1_pictures,
    2: _part2_context_choice,
    3: _part3_sentences,
    4: a2_listening._choice_part,
}


def convert_parts(document: Document, key: dict[int, list[str]]) -> Conversion:
    results: dict[int, PartResult] = {}
    extra: list[str] = []
    for part_no, source in split_parts(document).items():
        reader = PART_READERS.get(part_no)
        if reader is None:
            extra.append(f"Part {part_no} is not part of B1 Listening; it was left out.")
            continue
        results[part_no] = reader(source, key, document.shapes)

    conversion = merge(results, extra)
    numbers = {q["number"] for part in conversion.paper_parts.values() for group in part["groups"] for q in group["questions"]}
    conversion.warnings.extend(f"The answer key has question {n}, which is not in the paper." for n in sorted(set(key) - numbers))
    return conversion


def build_paper(conversion: Conversion, title: str, audio: dict[int, str]) -> tuple[dict, list[str]]:
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


def convert_files(questions_pdf: Path, key_pdf: Path, audio_dir: Path, out_dir: Path, title: str) -> Conversion:
    from .output import write_package

    conversion = convert_parts(read_document(questions_pdf), parse_answer_key(read_text(key_pdf)))
    tracks = {n: path for n, path in a2_listening.find_audio(audio_dir).items() if 1 <= n <= PART_COUNT}
    names = {n: f"audio/part{n}{path.suffix.lower()}" for n, path in tracks.items()}
    paper, paper_warnings = build_paper(conversion, title, names)
    report = Conversion(conversion.paper_parts, conversion.crops, conversion.warnings + paper_warnings, conversion.checks)
    write_package(out_dir, paper, report, questions_pdf, key_pdf)
    (out_dir / "audio").mkdir(exist_ok=True)
    for n, path in tracks.items():
        shutil.copyfile(path, out_dir / names[n])
    return report
