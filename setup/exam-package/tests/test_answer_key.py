import pytest

from converter.answer_key import parse_answer_key

# Same shape as a Cambridge mark scheme after text extraction: a header, then "PART n / KEY" and number/answer pairs.
KEY_TEXT = """EXAM
PAPER
LEVEL
COMPONENT
CODE/01
V1

PART 1
KEY
1
B
2
C

PART 5
KEY
25
his / her
26
Are
27
14
"""


def test_reads_every_answer_by_question_number():
    key = parse_answer_key(KEY_TEXT)

    assert key[1] == ["B"]
    assert key[2] == ["C"]
    assert key[26] == ["Are"]


def test_splits_accepted_alternatives_on_slashes():
    assert parse_answer_key(KEY_TEXT)[25] == ["his", "her"]


def test_keeps_answers_that_are_numbers():
    assert parse_answer_key(KEY_TEXT)[27] == ["14"]


def test_ignores_the_header_before_the_first_part():
    assert set(parse_answer_key(KEY_TEXT)) == {1, 2, 25, 26, 27}


def test_rejects_a_key_whose_numbers_do_not_pair_with_answers():
    with pytest.raises(ValueError, match="question 3"):
        parse_answer_key("PART 1\nKEY\n1\nA\n2\nB\n3\n")


def test_a_page_header_after_a_key_ends_that_key():
    text = "PART 1\nKEY\n1\nB\n2\nC\nEXAM\nPAPER\nREAD/WRITE\nPART 2\nKEY\n3\nA\n"

    assert parse_answer_key(text) == {1: ["B"], 2: ["C"], 3: ["A"]}
