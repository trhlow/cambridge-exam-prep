"""Part parsers for B1 Preliminary (for Schools) Listening, fed with made-up lines laid out like the Cambridge paper.

Positions copy the real layout (numbers at x≈50, context and question at x≈77, A/B/C letters bold at x≈77 with the
option text at x≈98; Part 3 sentences at x≈68 with the gap number '(14)' ending the first run of text); the wording
is invented.
"""

from converter.b1_listening import build_paper, convert_parts
from converter.model import Document, Line


def text(page, x, y, value, bold=False, size=11.0):
    return Line(page=page, x0=x, y0=y, x1=x + 5.5 * len(value), text=value, bold=bold, size=size)


def heading(page, part_no, first, last, *instructions):
    lines = [
        text(page, 284.6, 61.9, f"Part {part_no}", bold=True),
        text(page, 49.6, 86.7, f"Questions {first} – {last}", bold=True),
    ]
    lines += [text(page, 49.6, 112.5 + 12.6 * i, line) for i, line in enumerate(instructions)]
    lines.append(text(page, 295.3, 782.2, str(page + 2), size=11.2))
    return lines


def convert(lines, key=None):
    return convert_parts(Document(lines=tuple(lines), pictures=(), shapes=()), key or {})


def choice(page, y, number, context, question, options):
    lines = [text(page, 49.9, y, str(number), bold=True), text(page, 76.7, y, context), text(page, 76.7, y + 14.8, question)]
    for i, (letter, option) in enumerate(zip("ABC", options)):
        lines += [text(page, 76.7, y + 37 + 28.6 * i, letter, bold=True), text(page, 97.8, y + 37 + 28.6 * i, option)]
    return lines


# ---------------------------------------------------------------- Part 2: a line of context above each question

PART2 = (
    heading(0, 2, 8, 9, "For each question, choose the correct answer.")
    + choice(0, 151.3, 8, "You will hear two friends talking about a new café.", "What does the boy like about it?",
             ["the music", "the cakes", "the chairs"])
    + choice(0, 288.3, 9, "You will hear a girl talking about a school trip.", "How did she feel on the bus?",
             ["bored", "tired", "excited"])
)


def test_part2_shows_the_context_and_the_question_together():
    part = convert(PART2, {8: ["B"], 9: ["C"]}).paper_parts[2]
    questions = part["groups"][0]["questions"]

    assert part["groups"][0]["interactionType"] == "SingleChoice"
    assert questions[0]["promptHtml"] == "You will hear two friends talking about a new café.<br>What does the boy like about it?"
    assert [option["text"] for option in questions[1]["options"]] == ["bored", "tired", "excited"]
    assert questions[1]["answers"] == ["C"]


# ---------------------------------------------------------------- Part 3: sentences with a gap each

PART3 = heading(1, 3, 14, 16, "For each question, write the correct answer in the gap. Write one or two words or a number",
                "or a date or a time.") + [
    text(1, 49.6, 159.6, "You will hear a guide talking about a museum."),
    text(1, 193.6, 202.2, "THE CITY MUSEUM", bold=True, size=14.0),
    text(1, 68.2, 239.9, "The oldest thing in the museum is a (14)", bold=True),
    text(1, 353.5, 239.9, "from"),
    text(1, 68.2, 256.9, "the year 1200."),
    text(1, 68.2, 305.9, "Visitors may not take (15)", bold=True),
    text(1, 472.1, 305.9, "into the rooms."),
    text(1, 68.2, 356.4, "The café closes at (16)", bold=True),
    text(1, 479.3, 356.4, "."),
]
PART3_KEY = {14: ["boat", "the boat"], 15: ["bags"], 16: ["5 pm"]}


def test_part3_turns_each_sentence_into_a_typed_gap():
    part = convert(PART3, PART3_KEY).paper_parts[3]
    group = part["groups"][0]

    assert part["title"] == "THE CITY MUSEUM"
    assert group["interactionType"] == "InlineText"
    assert group["bodyHtml"] == (
        "<p>The oldest thing in the museum is a {{q:14}} from the year 1200.</p>"
        "<p>Visitors may not take {{q:15}} into the rooms.</p>"
        "<p>The café closes at {{q:16}}.</p>")
    assert [q["number"] for q in group["questions"]] == [14, 15, 16]
    assert group["questions"][0]["answers"] == ["boat", "the boat"]
    assert group["maxWordsPerAnswer"] == 2


def test_part3_moves_the_line_above_the_title_into_the_instructions():
    group = convert(PART3, PART3_KEY).paper_parts[3]["groups"][0]

    assert group["instructions"].endswith("You will hear a guide talking about a museum.")


def test_the_paper_gives_each_part_its_track_and_reports_missing_parts():
    conversion = convert(PART2 + PART3, {8: ["B"], 9: ["C"], **PART3_KEY})

    paper, warnings = build_paper(conversion, "B1 Listening", {2: "audio/part2.mp3"})

    assert paper["examType"] == "B1_PRELIM_FS" and paper["section"] == 3
    assert paper["parts"][0]["audio"] == "audio/part2.mp3"
    assert "Part 1 was not found in the PDF." in warnings
    assert "Part 3 has no audio track." in warnings
