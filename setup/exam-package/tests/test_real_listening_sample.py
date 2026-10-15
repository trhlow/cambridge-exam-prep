"""End to end on the official A2 Key for Schools 2022 Listening sample, when it is present.

The PDFs and the five part tracks live in the git-ignored sources folder, so on other machines these tests are
skipped. They only assert structure, never exam wording.
"""

import json
from pathlib import Path

import pytest

from converter.a2_listening import convert_files

SOURCE = Path(__file__).resolve().parent.parent / "sources" / "a2-key-fs-2022-sample"
QUESTIONS = SOURCE / "A2 Key for Schools 2022 sample tests Listening - question paper.pdf"
KEY = SOURCE / "A2 Key for Schools 2022 sample tests Listening - answer key.pdf"
AUDIO = SOURCE / "audio-parts"

pytestmark = pytest.mark.skipif(
    not QUESTIONS.is_file() or not KEY.is_file() or not AUDIO.is_dir(), reason="official Listening sample not present")


@pytest.fixture(scope="module")
def converted(tmp_path_factory):
    out = tmp_path_factory.mktemp("a2-listening")
    report = convert_files(QUESTIONS, KEY, AUDIO, out, title="A2 Key for Schools 2022 sample – Listening")
    paper = json.loads((out / "paper.json").read_text(encoding="utf-8"))
    return out, paper, report


def groups(paper):
    return [part["groups"][0] for part in paper["parts"]]


def test_the_paper_has_five_parts_questions_1_to_25_and_no_errors(converted):
    _, paper, report = converted

    assert (paper["examType"], paper["section"]) == ("A2_KEY_FS", 2)
    assert [g["interactionType"] for g in groups(paper)] == ["ImageChoice", "InlineText", "SingleChoice", "SingleChoice", "DragDropMatch"]
    assert [q["number"] for g in groups(paper) for q in g["questions"]] == list(range(1, 26))
    assert report.warnings == []


def test_every_part_has_its_own_track_copied_into_the_package(converted):
    out, paper, _ = converted

    assert [part["audio"] for part in paper["parts"]] == [f"audio/part{n}.mp3" for n in range(1, 6)]
    assert all((out / part["audio"]).stat().st_size > 100_000 for part in paper["parts"])


def test_part1_has_three_cropped_pictures_per_question(converted):
    out, paper, _ = converted

    for question in groups(paper)[0]["questions"]:
        assert [o["key"] for o in question["options"]] == ["A", "B", "C"], question["number"]
        assert all((out / o["image"]).stat().st_size > 1000 for o in question["options"]), question["number"]


def test_choice_and_card_answers_are_in_their_options(converted):
    _, paper, _ = converted

    for group in groups(paper)[2:4]:
        for question in group["questions"]:
            assert [o["key"] for o in question["options"]] == ["A", "B", "C"], question["number"]
    pool = [card["key"] for card in groups(paper)[4]["optionPool"]]
    assert pool == list("ABCDEFGH")
    assert all(q["answers"][0] in pool and q["promptHtml"] for q in groups(paper)[4]["questions"])


def test_the_form_has_a_slot_for_every_gap(converted):
    _, paper, _ = converted

    body = groups(paper)[1]["bodyHtml"]
    assert all(f"{{{{q:{n}}}}}" in body for n in range(6, 11))
    assert body.count("<tr>") == 6
