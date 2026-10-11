"""Part parsers for A2 Key (for Schools) Reading & Writing, fed with made-up lines laid out like the Cambridge paper.

Positions copy the real layout (A4 in points: numbers at x≈50, Part 1 options at x≈300, page number at y≈794);
the wording is invented so no exam content lives in the repository.
"""

from converter.a2_reading_writing import convert_parts
from converter.model import Document, Line, Picture


def text(page, x, y, value, bold=False, size=11.0):
    return Line(page=page, x0=x, y0=y, x1=x + 5.5 * len(value), text=value, bold=bold, size=size)


def heading(page, part_no, first, last, *instructions):
    numbers = f"Questions {first} – {last}" if first != last else f"Question {first}"
    lines = [
        text(page, 280, 62.7, f"Part {part_no}", bold=True),
        text(page, 49.6, 75.3, numbers, bold=True),
    ]
    lines += [text(page, 49.6, 100.7 + 12.6 * i, line, bold=line.startswith("Write")) for i, line in enumerate(instructions)]
    lines.append(text(page, 294.7, 793.9, str(page + 2), bold=True))
    return lines


def convert(lines, pictures=(), key=None):
    return convert_parts(Document(lines=tuple(lines), pictures=tuple(pictures)), key or {})


def only_group(part):
    assert len(part["groups"]) == 1
    return part["groups"][0]


# ---------------------------------------------------------------- Part 1: notices, one question each

PART1 = heading(0, 1, 1, 2, "For each question, choose the correct answer.") + [
    text(0, 49.7, 172.7, "1", bold=True),
    text(0, 299.4, 172.7, "The sign tells you to"),
    text(0, 299.4, 210.6, "A buy a ticket online.", bold=True),
    text(0, 299.4, 250.5, "B ask at the desk.", bold=True),
    text(0, 299.4, 290.3, "C wait outside the", bold=True),
    text(0, 316.4, 302.9, "museum."),
    text(0, 49.7, 368.1, "2", bold=True),
    text(0, 299.4, 380.8, "A Tom will be late.", bold=True),
    text(0, 299.4, 431.3, "B Tom has lost his", bold=True),
    text(0, 316.4, 443.9, "bag."),
    text(0, 299.4, 482.0, "C Tom is ill.", bold=True),
]
PART1_PICTURES = [Picture(0, 79.3, 207.2, 256.9, 282.3), Picture(0, 79.3, 368.8, 256.9, 518.3)]


def test_part1_reads_prompt_options_and_wrapped_option_text():
    result = convert(PART1, PART1_PICTURES, {1: ["C"], 2: ["B"]})

    part = result.paper_parts[1]
    group = only_group(part)
    assert part["layout"] == "Single"
    assert group["interactionType"] == "SingleChoice"
    assert group["instructions"] == "For each question, choose the correct answer."
    first, second = group["questions"]
    assert first["number"] == 1
    assert first["promptHtml"] == "The sign tells you to"
    assert first["options"] == [
        {"key": "A", "text": "buy a ticket online."},
        {"key": "B", "text": "ask at the desk."},
        {"key": "C", "text": "wait outside the museum."},
    ]
    assert first["answers"] == ["C"]
    assert "promptHtml" not in second
    assert second["options"][1] == {"key": "B", "text": "Tom has lost his bag."}


def test_part1_crops_each_notice_beside_its_question():
    result = convert(PART1, PART1_PICTURES, {1: ["C"], 2: ["B"]})

    questions = only_group(result.paper_parts[1])["questions"]
    assert questions[0]["stimulusHtml"] == "<img src='asset:images/p1-q1.png' alt='Question 1' width='355'>"
    assert questions[1]["stimulusHtml"] == "<img src='asset:images/p1-q2.png' alt='Question 2' width='355'>"
    crops = {crop.name: crop for crop in result.crops}
    assert crops["images/p1-q1.png"].page == 0
    assert crops["images/p1-q1.png"].x0 < 79.3 and crops["images/p1-q1.png"].y1 > 282.3


def test_a_choice_question_missing_from_the_key_is_reported():
    result = convert(PART1, PART1_PICTURES, {1: ["C"]})

    assert any("question 2" in warning and "answer key" in warning for warning in result.warnings)


# ---------------------------------------------------------------- Part 2: three people, questions in a grid

PART2 = heading(0, 2, 7, 8, "For each question, choose the correct answer.") + [
    text(0, 349.5, 148.7, "Ann", bold=True),
    text(0, 416.7, 148.7, "Ben", bold=True),
    text(0, 481.5, 148.7, "Cat", bold=True),
    text(0, 56.5, 177.2, "7", bold=True),
    text(0, 84.9, 176.7, "Who has a dog that"),
    text(0, 84.9, 189.9, "likes swimming?"),
    text(0, 357.0, 177.0, "A", bold=True),
    text(0, 426.0, 177.0, "B", bold=True),
    text(0, 495.0, 177.0, "C", bold=True),
    text(0, 56.5, 232.0, "8", bold=True),
    text(0, 84.9, 231.4, "Who plays the drums?"),
    text(0, 357.0, 231.7, "A", bold=True),
    text(0, 426.0, 231.7, "B", bold=True),
    text(0, 495.0, 231.7, "C", bold=True),
    text(1, 294.7, 793.9, "5", bold=True),
    text(1, 197.3, 138.0, "Three friends", bold=True, size=14.0),
    text(1, 56.8, 169.3, "Ann", bold=True),
    text(1, 140.1, 188.7, "My dog loves the river and"),
    text(1, 140.1, 208.6, "swims every day."),
    text(1, 56.8, 308.6, "Ben", bold=True),
    text(1, 140.1, 327.9, "I play the guitar."),
    text(1, 56.8, 435.5, "Cat", bold=True),
    text(1, 140.1, 454.9, "I play the drums & sing."),
]
PART2_PICTURES = [
    Picture(1, 49.6, 189.2, 131.9, 309.8, xref=41),
    Picture(1, 49.6, 329.2, 131.9, 437.7, xref=42),
    Picture(1, 49.6, 457.0, 131.9, 577.4, xref=43),
]


def test_part2_options_are_the_names_above_the_answer_columns():
    result = convert(PART2, PART2_PICTURES, {7: ["A"], 8: ["C"]})

    part = result.paper_parts[2]
    questions = only_group(part)["questions"]
    assert part["layout"] == "Split"
    assert questions[0]["promptHtml"] == "Who has a dog that likes swimming?"
    assert questions[0]["options"] == [
        {"key": "A", "text": "Ann"},
        {"key": "B", "text": "Ben"},
        {"key": "C", "text": "Cat"},
    ]
    assert questions[1]["answers"] == ["C"]


def test_part2_text_has_each_person_with_photo_and_paragraph():
    result = convert(PART2, PART2_PICTURES, {7: ["A"], 8: ["C"]})

    part = result.paper_parts[2]
    assert part["title"] == "Three friends"
    assert part["stimulusHtml"] == (
        "<h3>Ann</h3><p><img src='asset:images/p2-1.png' alt='Ann' width='165'></p><p>My dog loves the river and swims every day.</p>"
        "<h3>Ben</h3><p><img src='asset:images/p2-2.png' alt='Ben' width='165'></p><p>I play the guitar.</p>"
        "<h3>Cat</h3><p><img src='asset:images/p2-3.png' alt='Cat' width='165'></p><p>I play the drums &amp; sing.</p>"
    )
    assert [crop.name for crop in result.crops] == ["images/p2-1.png", "images/p2-2.png", "images/p2-3.png"]


def test_a_crop_of_one_embedded_picture_keeps_its_xref_so_the_original_pixels_are_saved():
    # The name below a photo starts a point above the photo's bottom edge; rendering the page area would cut it in.
    result = convert(PART2, PART2_PICTURES, {7: ["A"], 8: ["C"]})

    assert [crop.xref for crop in result.crops] == [41, 42, 43]


# ---------------------------------------------------------------- Part 3: long text, options on their own rows

PART3 = heading(0, 3, 14, 15, "For each question, choose the correct answer.") + [
    text(0, 212.6, 160.8, "My first camping trip", bold=True, size=14.0),
    text(0, 238.9, 182.6, "By Sam Lee, age 12"),
    text(0, 49.4, 209.7, "Last summer I went camping with my"),
    text(0, 49.4, 228.7, "cousins for the first time."),
    text(0, 49.4, 260.4, "It rained every night."),
    text(1, 294.7, 793.9, "7", bold=True),
    text(1, 49.4, 294.4, "14", bold=True),
    text(1, 84.9, 294.4, "How did Sam feel before the trip?"),
    text(1, 84.9, 318.0, "A", bold=True),
    text(1, 106.0, 318.0, "excited about sleeping outside"),
    text(1, 84.9, 342.0, "B", bold=True),
    text(1, 106.0, 342.0, "afraid of the dark"),
    text(1, 84.9, 366.0, "C", bold=True),
    text(1, 106.0, 366.0, "bored"),
    text(1, 49.4, 407.0, "15", bold=True),
    text(1, 84.9, 407.0, "What was the weather like?"),
    text(1, 84.9, 431.0, "A", bold=True),
    text(1, 106.0, 431.0, "sunny"),
    text(1, 84.9, 455.0, "B", bold=True),
    text(1, 106.0, 455.0, "wet"),
    text(1, 84.9, 479.0, "C", bold=True),
    text(1, 106.0, 479.0, "windy"),
]


def test_part3_text_has_title_byline_and_paragraphs():
    part = convert(PART3, key={14: ["A"], 15: ["B"]}).paper_parts[3]

    assert part["layout"] == "Split"
    assert part["title"] == "My first camping trip"
    assert part["stimulusHtml"] == (
        "<p><em>By Sam Lee, age 12</em></p>"
        "<p>Last summer I went camping with my cousins for the first time.</p>"
        "<p>It rained every night.</p>"
    )


def test_part3_questions_have_options_on_separate_rows():
    questions = only_group(convert(PART3, key={14: ["A"], 15: ["B"]}).paper_parts[3])["questions"]

    assert [q["number"] for q in questions] == [14, 15]
    assert questions[0]["promptHtml"] == "How did Sam feel before the trip?"
    assert questions[0]["options"][0] == {"key": "A", "text": "excited about sleeping outside"}
    assert questions[1]["options"][2] == {"key": "C", "text": "windy"}


# ---------------------------------------------------------------- Part 4: gapped text with word choices

PART4 = [
    text(0, 277.0, 62.6, "Part 4", bold=True),
    text(0, 291.6, 793.9, "8", bold=True),
    text(0, 241.1, 150.8, "The town zoo", bold=True, size=14.0),
    text(0, 49.6, 179.5, "The zoo opened ten years ago and has a (19) ......... of animals.", bold=True),
    text(0, 49.6, 198.5, "Visitors can (20) ................ the lions every day."),
    text(0, 49.6, 230.2, "It is open all year."),
    text(0, 49.7, 74.4, "Questions 19 – 20", bold=True),
    text(0, 49.7, 99.6, "For each question, choose the correct answer."),
    text(0, 49.9, 420.9, "19", bold=True),
    text(0, 88.0, 420.9, "A", bold=True),
    text(0, 114.3, 420.9, "lot"),
    text(0, 218.1, 420.9, "B", bold=True),
    text(0, 244.6, 420.9, "many"),
    text(0, 348.7, 420.9, "C", bold=True),
    text(0, 377.6, 420.9, "much"),
    text(0, 49.9, 465.3, "20", bold=True),
    text(0, 88.0, 465.3, "A", bold=True),
    text(0, 114.3, 465.3, "feed"),
    text(0, 218.1, 465.3, "B", bold=True),
    text(0, 244.6, 465.3, "eat"),
    text(0, 348.7, 465.3, "C", bold=True),
    text(0, 377.6, 465.3, "give"),
]


def test_part4_turns_numbered_gaps_into_slots_and_rows_into_word_choices():
    part = convert(PART4, key={19: ["A"], 20: ["A"]}).paper_parts[4]
    group = only_group(part)

    assert part["layout"] == "Single"
    assert part["title"] == "The town zoo"
    assert group["interactionType"] == "InlineChoice"
    assert group["bodyHtml"] == (
        "<p>The zoo opened ten years ago and has a {{q:19}} of animals. Visitors can {{q:20}} the lions every day.</p>"
        "<p>It is open all year.</p>"
    )
    assert group["questions"][0] == {
        "number": 19,
        "options": [{"key": "A", "text": "lot"}, {"key": "B", "text": "many"}, {"key": "C", "text": "much"}],
        "answers": ["A"],
    }


# ---------------------------------------------------------------- Part 5: email with typed gaps

PART5 = [
    text(0, 277.0, 62.7, "Part 5", bold=True),
    text(0, 291.6, 793.9, "9", bold=True),
    text(0, 481.2, 793.9, "Turn over ►", bold=True),
    text(0, 63.7, 194.5, "From:", size=10.0),
    text(0, 63.7, 217.6, "To:", size=10.0),
    text(0, 49.6, 251.3, "Hi! Thanks (0) ........... your letter. I (25) ............ happy.", bold=True),
    text(0, 49.6, 284.4, "See you (26) ............Monday.", bold=True),
    text(0, 127.6, 217.4, "Kim", size=10.0),
    text(0, 127.6, 194.5, "Lou", size=10.0),
    text(0, 49.7, 74.4, "Questions 25 – 26", bold=True),
    text(0, 49.7, 99.6, "For each question, write the correct answer."),
    text(0, 49.7, 112.3, "Write one word for each gap.", bold=True),
    text(0, 49.7, 143.0, "Example:", bold=True),
    text(0, 219.4, 142.0, "for", size=16.0),
    text(0, 133.3, 143.1, "0", bold=True),
]


def test_part5_reads_sender_example_and_one_word_gaps():
    part = convert(PART5, key={25: ["am", "'m"], 26: ["on"]}).paper_parts[5]
    group = only_group(part)

    assert group["interactionType"] == "InlineText"
    assert group["instructions"] == "For each question, write the correct answer. Write one word for each gap."
    assert group["maxWordsPerAnswer"] == 1
    assert group["bodyHtml"] == (
        "<p><strong>From:</strong> Lou<br><strong>To:</strong> Kim</p>"
        "<p>Hi! Thanks (0) <u>for</u> your letter. I {{q:25}} happy.</p>"
        "<p>See you {{q:26}} Monday.</p>"
    )
    assert group["questions"] == [{"number": 25, "answers": ["am", "'m"]}, {"number": 26, "answers": ["on"]}]


def test_part5_lists_gaps_before_a_capital_letter_for_the_reviewer():
    checks = convert(PART5, key={25: ["am"], 26: ["on"]}).checks

    assert len(checks) == 1 and "question 26" in checks[0]


# ---------------------------------------------------------------- Parts 6 and 7: writing

PART6 = heading(0, 6, 31, 31) + [
    text(0, 49.7, 112.2, "Your friend Max is coming to visit."),
    text(0, 49.7, 124.9, "Write a note to Max."),
    text(0, 49.7, 150.1, "Say:"),
    text(0, 49.6, 175.3, "•", size=10.9),
    text(0, 67.7, 176.4, "when you will meet"),
    text(0, 49.6, 201.1, "•", size=10.9),
    text(0, 67.7, 202.2, "what you will do."),
    text(0, 49.7, 266.1, "Write 25 words or more.", bold=True),
    text(0, 49.7, 304.1, "Write the note on your answer sheet.", bold=True),
]

PART7 = heading(0, 7, 32, 32) + [
    text(0, 49.7, 112.2, "Look at the three pictures."),
    text(0, 49.7, 124.9, "Write the story shown in the pictures."),
    text(0, 49.7, 137.5, "Write 35 words or more.", bold=True),
    text(0, 49.7, 323.4, "Write the story on your answer sheet.", bold=True),
]
PART7_PICTURES = [
    Picture(0, 390.4, 173.4, 550.0, 296.3),
    Picture(0, 49.6, 173.5, 209.1, 296.1),
    Picture(0, 220.0, 173.5, 379.6, 296.1),
]


def test_part6_is_an_essay_with_the_task_and_bullets_beside_it():
    part = convert(PART6).paper_parts[6]
    group = only_group(part)

    assert part["layout"] == "Split"
    assert part["stimulusHtml"] == (
        "<p>Your friend Max is coming to visit.</p><p>Write a note to Max.</p><p>Say:</p>"
        "<ul><li>when you will meet</li><li>what you will do.</li></ul>"
    )
    assert group["interactionType"] == "Essay"
    assert group["instructions"] == "Write 25 words or more."
    assert group["questions"] == [{"number": 31, "points": 15, "minWords": 25}]


def test_part7_shows_the_three_pictures_left_to_right():
    result = convert(PART7, PART7_PICTURES)
    part = result.paper_parts[7]

    assert part["layout"] == "Single"
    assert part["stimulusHtml"] == (
        "<p>Look at the three pictures.</p><p>Write the story shown in the pictures.</p>"
        "<p><img src='asset:images/p7-1.png' alt='Picture 1' width='319'> <img src='asset:images/p7-2.png' alt='Picture 2' width='319'> "
        "<img src='asset:images/p7-3.png' alt='Picture 3' width='319'></p>"
    )
    assert only_group(part)["questions"] == [{"number": 32, "points": 15, "minWords": 35}]
    assert [crop.x0 < 60 for crop in result.crops] == [True, False, False]


def test_a_blank_page_at_the_end_is_not_part_of_the_task():
    blank = [text(1, 260.0, 400.0, "BLANK PAGE"), text(1, 291.6, 793.9, "12", bold=True)]

    part = convert(PART7 + blank, PART7_PICTURES).paper_parts[7]

    assert "BLANK" not in part["stimulusHtml"]


def test_answers_in_the_key_for_unknown_questions_are_reported():
    result = convert(PART6, key={31: ["x"], 99: ["A"]})

    assert any("99" in warning for warning in result.warnings)


def test_names_with_apostrophes_stay_inside_the_alt_attribute():
    lines = [line if line.text != "Cat" else text(line.page, line.x0, line.y0, "Cat O'Neil' onerror='x", bold=True)
             for line in PART2]

    stimulus = convert(lines, PART2_PICTURES, {7: ["A"], 8: ["C"]}).paper_parts[2]["stimulusHtml"]

    assert "alt='Cat O&#x27;Neil&#x27; onerror=&#x27;x' width='165'" in stimulus
