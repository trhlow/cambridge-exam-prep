"""End to end on the official A2 Key for Schools 2022 sample, when it is present.

The PDFs live in the git-ignored sources folder, so on other machines these tests are skipped. They only assert
structure, never exam wording.
"""

import json
from pathlib import Path

import pytest

from converter.a2_reading_writing import convert_files

SOURCE = Path(__file__).resolve().parent.parent / "sources" / "a2-key-fs-2022-sample"
QUESTIONS = SOURCE / "A2 Key for Schools 2022 sample Reading and Writing - questions v2.pdf"
KEY = SOURCE / "A2 Key for Schools 2022 sample Reading and Writing - answer key.pdf"

pytestmark = pytest.mark.skipif(not QUESTIONS.is_file() or not KEY.is_file(), reason="official sample not present")


@pytest.fixture(scope="module")
def converted(tmp_path_factory):
    out = tmp_path_factory.mktemp("a2-rw")
    report = convert_files(QUESTIONS, KEY, out, title="A2 Key for Schools 2022 sample – Reading & Writing")
    paper = json.loads((out / "paper.json").read_text(encoding="utf-8"))
    return out, paper, report


def questions(paper):
    return [q for part in paper["parts"] for group in part["groups"] for q in group["questions"]]


def test_the_paper_has_seven_parts_and_questions_1_to_32(converted):
    _, paper, _ = converted

    assert paper["examType"] == "A2_KEY_FS"
    assert paper["section"] == 1
    assert [part["partNo"] for part in paper["parts"]] == [1, 2, 3, 4, 5, 6, 7]
    assert [q["number"] for q in questions(paper)] == list(range(1, 33))


def test_every_reading_question_has_three_options_and_a_key(converted):
    _, paper, report = converted

    for q in questions(paper)[:24]:
        assert [o["key"] for o in q["options"]] == ["A", "B", "C"], q["number"]
        assert all(o["text"] for o in q["options"]), q["number"]
        assert len(q["answers"]) == 1 and q["answers"][0] in "ABC", q["number"]
    for q in questions(paper)[24:30]:
        assert q["answers"], q["number"]
    assert report.warnings == []


def test_gapped_texts_have_a_slot_for_every_question(converted):
    _, paper, _ = converted

    for part_no, numbers in ((4, range(19, 25)), (5, range(25, 31))):
        body = paper["parts"][part_no - 1]["groups"][0]["bodyHtml"]
        assert all(f"{{{{q:{n}}}}}" in body for n in numbers)
        assert "...." not in body and "…" not in body


def test_pictures_are_cropped_into_the_package(converted):
    out, paper, _ = converted

    referenced = {name for name in json.dumps(paper).split("asset:")[1:]}
    images = sorted(p.name for p in (out / "images").iterdir())
    assert len(referenced) == len(images) == 6 + 3 + 3
    assert all((out / "images" / name).stat().st_size > 2000 for name in images)


def test_writing_tasks_keep_their_word_limits(converted):
    _, paper, _ = converted

    assert questions(paper)[30:] == [
        {"number": 31, "points": 15, "minWords": 25},
        {"number": 32, "points": 15, "minWords": 35},
    ]


def test_a_review_checklist_is_written_next_to_the_paper(converted):
    out, _, _ = converted

    review = (out / "REVIEW.md").read_text(encoding="utf-8")
    assert "| 32 |" in review
