import pytest

from converter.answer_key import expand_optional, parse_answer_key

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


def test_reads_a_key_without_key_lines_as_in_the_listening_mark_scheme():
    text = "KEY for Schools Listening Sample Paper Answer key\nPart 1\n\n1\nC\n2\nA\n\nPart 2\n\n6\nfairfield\n7\n7.30\n"

    assert parse_answer_key(text) == {1: ["C"], 2: ["A"], 6: ["fairfield"], 7: ["7.30"]}


def test_a_page_header_after_a_key_ends_that_key():
    text = "PART 1\nKEY\n1\nB\n2\nC\nEXAM\nPAPER\nREAD/WRITE\nPART 2\nKEY\n3\nA\n"

    assert parse_answer_key(text) == {1: ["B"], 2: ["C"], 3: ["A"]}


def test_bracketed_words_may_be_left_out_or_swapped_for_an_alternative():
    variants = expand_optional("(a/the) (big) lake(s)")

    assert variants[0] == "lake"
    assert {"lakes", "a lake", "the big lakes", "big lake"} <= set(variants)
    assert "a/the lake" not in variants


def test_a_single_optional_word_before_another_may_also_be_joined_to_it():
    assert expand_optional("(sun) glasses") == ["glasses", "sunglasses", "sun glasses"]


def test_an_answer_without_brackets_is_kept_as_it_is():
    assert expand_optional("7.30") == ["7.30"]


def test_listening_misspellings_after_an_answer_also_score():
    text = "Part 3\n14\n(the) river\nACCEPTABLE MIS-SPELLINGS\nriverr\n15\nboat\nPart 4\n20\nB\n"

    key = parse_answer_key(text)

    assert key[14] == ["river", "the river", "riverr"]
    assert key[15] == ["boat"]
    assert key[20] == ["B"]
