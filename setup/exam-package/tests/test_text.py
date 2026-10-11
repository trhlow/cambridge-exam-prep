from converter.model import Line
from converter.text import gaps_before_capitals, gaps_to_slots, paragraphs, rows


def line(y, text, x=50.0, bold=False):
    return Line(page=0, x0=x, y0=y, x1=x + 6 * len(text), text=text, bold=bold)


def test_numbered_gaps_become_slots():
    assert gaps_to_slots("a long (19) ............... after two") == "a long {{q:19}} after two"


def test_gaps_written_with_ellipsis_characters_or_no_space_after():
    assert gaps_to_slots("(25) ………… red car") == "{{q:25}} red car"
    assert gaps_to_slots("We need (30) ............go home") == "We need {{q:30}} go home"


def test_the_example_gap_shows_its_answer():
    assert gaps_to_slots("Thanks (0) ........... your note", example="for") == "Thanks (0) <u>for</u> your note"


def test_text_is_html_escaped_but_slots_are_kept():
    assert gaps_to_slots("Tom & Jo <3 (7) ....") == "Tom &amp; Jo &lt;3 {{q:7}}"


def test_lines_on_the_same_baseline_form_one_row_ordered_left_to_right():
    grouped = rows([line(100.4, "B", x=200), line(100.0, "A", x=100), line(130, "next")])

    assert [[part.text for part in row] for row in grouped] == [["A", "B"], ["next"]]


def test_a_larger_vertical_gap_starts_a_new_paragraph():
    lines = [
        line(100, "First line of the"),
        line(119, "first paragraph."),
        line(150, "Second paragraph."),
    ]

    assert paragraphs(lines) == ["First line of the first paragraph.", "Second paragraph."]


def test_gaps_followed_by_a_capital_letter_are_listed_for_the_reviewer():
    # The dots of "(21) ......... The" may have swallowed a full stop; only a person can tell.
    assert gaps_before_capitals("at the shop (21) ......... The owner (22) ..... them") == [21]
