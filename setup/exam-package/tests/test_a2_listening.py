"""Part parsers for A2 Key (for Schools) Listening, fed with made-up lines laid out like the Cambridge paper.

Positions copy the real layout (numbers at x≈50, Part 1 letters under three picture columns, the Part 2 form with
labels at x≈109 and answers at x≈321, the Part 5 cards at x≈312); the wording is invented.
"""

import pytest

from converter.a2_listening import build_paper, convert_parts, find_audio
from converter.model import Document, Line, Picture, Shape


def text(page, x, y, value, bold=False, size=11.0):
    return Line(page=page, x0=x, y0=y, x1=x + 5.5 * len(value), text=value, bold=bold, size=size)


def heading(page, part_no, first, last, *instructions):
    numbers = f"Questions {first} – {last}" if first != last else f"Question {first}"
    lines = [
        text(page, 284.6, 61.9, f"Part {part_no}", bold=True),
        text(page, 49.6, 86.7, numbers, bold=True),
    ]
    lines += [text(page, 49.6, 112.5 + 12.6 * i, line) for i, line in enumerate(instructions)]
    lines.append(text(page, 295.3, 782.2, str(page + 2), size=11.2))
    return lines


def convert(lines, pictures=(), shapes=(), key=None):
    return convert_parts(Document(lines=tuple(lines), pictures=tuple(pictures), shapes=tuple(shapes)), key or {})


def only_group(part):
    assert len(part["groups"]) == 1
    return part["groups"][0]


def letters(page, y, xs=(113.5, 279.0, 445.2)):
    return [text(page, x, y, key, bold=True) for key, x in zip("ABC", xs)]


# ---------------------------------------------------------------- Part 1: three pictures per question

PART1 = (
    heading(0, 1, 1, 3, "For each question, choose the correct picture.")
    + [text(0, 49.6, 150.2, "1", bold=True), text(0, 77.1, 150.2, "Which bag did Tom buy?")]
    + letters(0, 304.0)
    + [text(0, 49.6, 344.3, "2", bold=True), text(0, 77.1, 344.3, "How much is the ticket?")]
    + [text(0, x, 400.0, "£3.50", size=22.0) for x in (83.3, 248.5, 414.6)]
    + letters(0, 497.7)
    + [text(0, 45.5, 536.6, "3", bold=True), text(0, 73.0, 536.6, "Where will they meet?")]
    + letters(0, 692.8, (117.9, 294.1, 470.3))
)
PART1_PICTURES = [
    Picture(0, 44.7, 181.9, 190.5, 294.2),
    Picture(0, 210.6, 181.8, 356.4, 294.1),
    Picture(0, 376.7, 181.8, 522.4, 294.1),
    Picture(0, 44.4, 562.4, 550.7, 685.9),  # question 3: one picture holding all three
]
PART1_FRAMES = [Shape(0, 44.0, 376.0, 191.0, 489.0), Shape(0, 210.0, 376.0, 357.0, 489.0), Shape(0, 376.0, 376.0, 524.0, 489.0)]
PART1_KEY = {1: ["C"], 2: ["A"], 3: ["B"]}


def part1():
    return convert(PART1, PART1_PICTURES, PART1_FRAMES, PART1_KEY)


def test_part1_is_a_picture_choice_with_the_question_and_its_answer():
    group = only_group(part1().paper_parts[1])

    assert group["interactionType"] == "ImageChoice"
    assert group["instructions"] == "For each question, choose the correct picture."
    first = group["questions"][0]
    assert first["number"] == 1
    assert first["promptHtml"] == "Which bag did Tom buy?"
    assert first["answers"] == ["C"]
    assert first["options"] == [
        {"key": "A", "image": "images/l-p1-q1-a.png"},
        {"key": "B", "image": "images/l-p1-q1-b.png"},
        {"key": "C", "image": "images/l-p1-q1-c.png"},
    ]


def test_part1_crops_each_picture_above_its_letter():
    crops = {crop.name: crop for crop in part1().crops}

    a = crops["images/l-p1-q1-a.png"]
    assert (a.page, a.x0, a.y0, a.x1, a.y1) == (0, 40.7, 177.9, 194.5, 298.2)
    c = crops["images/l-p1-q1-c.png"]
    assert (c.x0, c.x1) == (372.7, 526.4)


def test_part1_splits_a_shared_picture_halfway_between_the_letters():
    crops = {crop.name: crop for crop in part1().crops}

    a, b, c = (crops[f"images/l-p1-q3-{key}.png"] for key in "abc")
    assert a.x0 == 40.4 and a.x1 == b.x0 == pytest.approx((117.9 + 294.1) / 2 + 2.75)
    assert b.x1 == c.x0 == pytest.approx((294.1 + 470.3) / 2 + 2.75)
    assert c.x1 == 554.7
    assert (a.y0, a.y1) == (558.4, 689.9)


def test_part1_keeps_drawn_options_and_their_printed_text():
    result = part1()
    question = only_group(result.paper_parts[1])["questions"][1]
    crops = {crop.name: crop for crop in result.crops}

    assert [option.get("text") for option in question["options"]] == ["£3.50", "£3.50", "£3.50"]
    a = crops["images/l-p1-q2-a.png"]
    assert (a.x0, a.y0, a.x1, a.y1) == (40.0, 372.0, 195.0, 493.0)
    assert result.warnings == []


# ---------------------------------------------------------------- Part 2: a form with typed gaps

PART2 = heading(
    1, 2, 6, 8,
    "For each question, write the correct answer in the gap. Write one word or a number or a date or a",
    "time.",
) + [
    text(1, 49.7, 165.5, "You will hear a girl talking about a cooking class."),
    text(1, 213.3, 220.3, "Cooking Class", size=18.0),
    text(1, 108.9, 260.9, "Day:"),
    text(1, 351.4, 261.1, "Tuesday", size=17.1),
    text(1, 108.9, 303.8, "Teacher's name:"),
    text(1, 321.4, 303.8, "(6) Mr", bold=True, size=16.3),
    text(1, 108.9, 350.1, "Start time:"),
    text(1, 321.4, 351.1, "(7)", bold=True),
    text(1, 422.4, 350.5, "p.m.", size=16.0),
    text(1, 108.9, 396.7, "Bring:"),
    text(1, 321.4, 397.6, "(8)", bold=True),
]
PART2_KEY = {6: ["Lopez"], 7: ["4.15"], 8: ["apron"]}


def test_part2_reads_the_form_into_a_table_with_a_slot_per_gap():
    result = convert(PART2, key=PART2_KEY)
    part = result.paper_parts[2]
    group = only_group(part)

    assert part["title"] == "Cooking Class"
    assert group["interactionType"] == "InlineText"
    assert group["instructions"] == (
        "For each question, write the correct answer in the gap. Write one word or a number or a date or a time."
    )
    assert group["bodyHtml"] == (
        "<p>You will hear a girl talking about a cooking class.</p><table>"
        "<tr><td>Day:</td><td>Tuesday</td></tr>"
        "<tr><td>Teacher's name:</td><td>Mr {{q:6}}</td></tr>"
        "<tr><td>Start time:</td><td>{{q:7}} p.m.</td></tr>"
        "<tr><td>Bring:</td><td>{{q:8}}</td></tr>"
        "</table>"
    )
    assert [q["number"] for q in group["questions"]] == [6, 7, 8]
    assert result.warnings == []


def test_part2_finds_the_answer_column_where_the_gap_numbers_are():
    # A2 Key (not for Schools) prints the answers at x≈294 and a two-line heading; the column follows the "(6)" marks.
    lines = heading(
        1, 2, 6, 7,
        "For each question, write the correct answer in the gap. Write one word or a number or a date or a",
        "time.",
    ) + [
        text(1, 49.7, 165.5, "You will hear a man talking about a bike race."),
        text(1, 229.0, 220.8, "Bike Race", size=18.0),
        text(1, 203.0, 240.0, "in the City", size=18.96),
        text(1, 110.0, 283.0, "Place:"),
        text(1, 291.0, 283.0, "City park", size=16.0),
        text(1, 110.0, 330.0, "Date:"),
        text(1, 294.0, 330.0, "(6) 15th June –", bold=True, size=16.0),
        text(1, 110.0, 384.0, "Riders must be:"),
        text(1, 294.0, 385.0, "(7)", bold=True),
        text(1, 419.0, 383.0, "years old", size=16.0),
    ]
    result = convert(lines, key={6: ["17th"], 7: ["12"]})
    group = only_group(result.paper_parts[2])

    assert result.paper_parts[2]["title"] == "Bike Race in the City"
    assert group["bodyHtml"] == (
        "<p>You will hear a man talking about a bike race.</p><table>"
        "<tr><td>Place:</td><td>City park</td></tr>"
        "<tr><td>Date:</td><td>15th June – {{q:6}}</td></tr>"
        "<tr><td>Riders must be:</td><td>{{q:7}} years old</td></tr>"
        "</table>"
    )
    assert result.warnings == []


def test_part2_also_accepts_a_time_written_with_a_colon_and_says_so():
    result = convert(PART2, key=PART2_KEY)
    questions = only_group(result.paper_parts[2])["questions"]

    assert questions[1]["answers"] == ["4.15", "4:15"]
    assert questions[0]["answers"] == ["Lopez"]
    assert any("question 7" in check and "4:15" in check for check in result.checks)


# ---------------------------------------------------------------- Parts 3 and 4: A/B/C answers

PART3 = heading(2, 3, 11, 12, "For each question, choose the correct answer.") + [
    text(2, 49.6, 159.0, "You will hear Ben talking to his friend about a museum."),
    text(2, 46.6, 185.7, "11 Ben went to the museum on", bold=True),
    text(2, 72.9, 208.6, "A", bold=True), text(2, 99.0, 208.6, "Monday."),
    text(2, 72.9, 231.5, "B", bold=True), text(2, 99.0, 231.5, "Wednesday."),
    text(2, 72.9, 254.4, "C", bold=True), text(2, 99.0, 254.4, "Saturday."),
    text(2, 46.7, 296.7, "12 A guide told them about", bold=True),
    text(2, 72.9, 319.6, "A", bold=True), text(2, 99.0, 319.6, "old trains."),
    text(2, 72.9, 342.6, "B", bold=True), text(2, 99.0, 342.6, "famous paintings."),
    text(2, 72.9, 365.6, "C", bold=True), text(2, 99.0, 365.6, "dinosaurs."),
]


def test_part3_puts_the_situation_in_the_instructions_and_splits_number_from_prompt():
    result = convert(PART3, key={11: ["B"], 12: ["C"]})
    group = only_group(result.paper_parts[3])

    assert group["interactionType"] == "SingleChoice"
    assert group["instructions"] == (
        "For each question, choose the correct answer. You will hear Ben talking to his friend about a museum."
    )
    first, second = group["questions"]
    assert first["promptHtml"] == "Ben went to the museum on"
    assert first["options"] == [
        {"key": "A", "text": "Monday."}, {"key": "B", "text": "Wednesday."}, {"key": "C", "text": "Saturday."},
    ]
    assert second["promptHtml"] == "A guide told them about"
    assert [o["key"] for o in second["options"]] == ["A", "B", "C"]
    assert result.warnings == []


PART4 = heading(3, 4, 16, 16, "For each question, choose the correct answer.") + [
    text(3, 49.6, 145.7, "16", bold=True),
    text(3, 76.1, 145.7, "You will hear a man talking to his son."),
    text(3, 76.5, 158.3, "Where are they going?"),
    text(3, 76.5, 181.3, "A", bold=True), text(3, 104.1, 181.3, "to the park"),
    text(3, 76.5, 204.2, "B", bold=True), text(3, 104.1, 204.2, "to a shop"),
    text(3, 76.5, 227.3, "C", bold=True), text(3, 104.1, 227.3, "to the station"),
]


def test_part4_joins_the_situation_and_the_question_into_the_prompt():
    result = convert(PART4, key={16: ["A"]})
    question = only_group(result.paper_parts[4])["questions"][0]

    assert question["promptHtml"] == "You will hear a man talking to his son. Where are they going?"
    assert question["options"][2] == {"key": "C", "text": "to the station"}
    assert question["answers"] == ["A"]


# ---------------------------------------------------------------- Part 5: drag the cards to the people

PART5 = heading(4, 5, 21, 22, "For each question, choose the correct answer.") + [
    text(4, 49.7, 150.6, "You will hear Mia talking to her dad about a party."),
    text(4, 49.7, 163.8, "What will each person bring?"),
    text(4, 49.7, 176.7, "Example:", bold=True),
    text(4, 49.7, 210.5, "0", bold=True), text(4, 80.1, 210.5, "Mia"), text(4, 163.1, 210.5, "B", bold=True),
    text(4, 49.7, 262.3, "People", bold=True), text(4, 312.1, 262.3, "Bring", bold=True),
    text(4, 49.7, 300.2, "21", bold=True), text(4, 80.1, 300.2, "Sam"),
    text(4, 312.1, 300.2, "A", bold=True), text(4, 349.3, 300.2, "cake"),
    text(4, 312.7, 335.9, "B", bold=True), text(4, 348.7, 335.9, "cups"),
    text(4, 49.7, 354.1, "22", bold=True), text(4, 80.1, 354.1, "Lily"),
    text(4, 312.7, 372.1, "C", bold=True), text(4, 348.7, 372.1, "games"),
    text(4, 49.7, 681.1, "You now have 6 minutes to write your answers on the answer sheet.", bold=True, size=14.0),
]


def test_part5_builds_the_card_pool_and_one_row_per_person():
    result = convert(PART5, key={21: ["C"], 22: ["A"]})
    group = only_group(result.paper_parts[5])

    assert group["interactionType"] == "DragDropMatch"
    assert group["optionPool"] == [
        {"key": "A", "text": "cake"}, {"key": "B", "text": "cups"}, {"key": "C", "text": "games"},
    ]
    assert group["questions"] == [
        {"number": 21, "promptHtml": "Sam", "answers": ["C"]},
        {"number": 22, "promptHtml": "Lily", "answers": ["A"]},
    ]
    assert group["instructions"] == (
        "For each question, choose the correct answer. You will hear Mia talking to her dad about a party. "
        "What will each person bring? Example: Mia – cups."
    )
    assert result.warnings == []


def test_part5_reports_an_answer_that_is_not_a_card():
    result = convert(PART5, key={21: ["H"], 22: ["A"]})

    assert any("question 21" in warning and "H" in warning for warning in result.warnings)


# ---------------------------------------------------------------- whole paper

def test_the_paper_is_section_2_with_an_audio_track_per_part():
    result = convert(PART1 + PART2 + PART3 + PART4 + PART5, PART1_PICTURES, PART1_FRAMES)

    paper, warnings = build_paper(result, "Listening test", {1: "audio/part1.mp3", 2: "audio/part2.mp3"})

    assert (paper["examType"], paper["section"], paper["title"]) == ("A2_KEY_FS", 2, "Listening test")
    assert [part["partNo"] for part in paper["parts"]] == [1, 2, 3, 4, 5]
    assert paper["parts"][0]["audio"] == "audio/part1.mp3"
    assert "audio" not in paper["parts"][2]
    assert "Part 3 has no audio track." in warnings
    assert "Part 5 has no audio track." in warnings


def test_finds_the_audio_track_of_each_part_by_its_name(tmp_path):
    for name in ("P1_sample.mp3", "part2.mp3", "Part 3 - sample.m4a", "p4.wav", "P5_sample.mp3", "sample.mp3", "P1_notes.txt"):
        (tmp_path / name).write_bytes(b"")

    tracks = find_audio(tmp_path)

    assert {n: path.name for n, path in tracks.items()} == {
        1: "P1_sample.mp3", 2: "part2.mp3", 3: "Part 3 - sample.m4a", 4: "p4.wav", 5: "P5_sample.mp3",
    }
