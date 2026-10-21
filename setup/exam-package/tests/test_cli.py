"""pdf2package.py as the staff portal runs it: a JSON report next to the draft, whatever the PDF holds."""

import json
import subprocess
import sys
from pathlib import Path

import pymupdf

SCRIPT = Path(__file__).resolve().parent.parent / "pdf2package.py"


def pdf(path: Path, *lines: str) -> Path:
    with pymupdf.open() as document:
        page = document.new_page()
        for index, line in enumerate(lines):
            page.insert_text((72, 72 + 20 * index), line, fontsize=11)
        document.save(path)
    return path


def test_writes_the_errors_to_the_report_when_the_pdf_is_not_a_cambridge_paper(tmp_path):
    questions = pdf(tmp_path / "q.pdf", "Key for Schools", "Reading and Writing")
    key = pdf(tmp_path / "k.pdf", "PART 1", "1", "A")
    report = tmp_path / "report.json"

    result = subprocess.run(
        [sys.executable, "-I", str(SCRIPT), str(questions), str(key), "--title", "Test", "-o", str(tmp_path / "out"), "--report", str(report)],
        capture_output=True, text=True, encoding="utf-8")

    assert result.returncode == 1
    data = json.loads(report.read_text(encoding="utf-8"))
    assert data["component"] == "a2-reading-writing"
    assert "Part 1 was not found in the PDF." in data["warnings"]


def test_a_listening_paper_is_recognised_from_its_cover(tmp_path):
    questions = pdf(tmp_path / "q.pdf", "Key for Schools", "Listening")
    key = pdf(tmp_path / "k.pdf", "Part 1", "1", "A")
    audio = tmp_path / "audio"
    audio.mkdir()
    report = tmp_path / "report.json"

    subprocess.run(
        [sys.executable, "-I", str(SCRIPT), str(questions), str(key), "--title", "Test", "--audio-dir", str(audio),
         "-o", str(tmp_path / "out"), "--report", str(report)],
        capture_output=True, text=True, encoding="utf-8")

    data = json.loads(report.read_text(encoding="utf-8"))
    assert data["component"] == "a2-listening"
    assert "Part 1 was not found in the PDF." in data["warnings"]
