"""Convert a Cambridge A2 Key or B1 Preliminary (for Schools) question paper PDF and its answer key into a draft exam
package folder.

Usage:
    python -I setup/exam-package/pdf2package.py <questions.pdf> <answer-key.pdf> --title "<paper title>" [-o <folder>]
    python -I setup/exam-package/pdf2package.py <listening.pdf> <answer-key.pdf> --audio-dir <folder> --title "…"

The level (A2 Key or B1 Preliminary) and the component (A2 Reading & Writing / Listening; B1 Reading, Writing or
Listening) are read from the cover page. B1 Writing has no answer key. Listening needs one audio track per part in
--audio-dir, named by part (P1_….mp3, part2.mp3, …); split_listening.py cuts a whole recording into them.

The folder gets paper.json, images/ (pictures cropped from the PDF), audio/ for Listening, and REVIEW.md. Read
REVIEW.md, fix what it lists, then pack the folder with pack.py and import it. Keep the output under
setup/exam-package/sources/ (git-ignored): it is exam content and must never be committed.
"""

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from converter import a2_listening, a2_reading_writing, b1_listening, b1_reading, b1_writing  # noqa: E402


def cover(questions_pdf: Path) -> list[str]:
    import pymupdf
    with pymupdf.open(questions_pdf) as pdf:
        return [line.strip() for line in pdf[0].get_text().splitlines()]


def is_b1(questions_pdf: Path) -> bool:
    return any("PRELIMINARY" in line.upper() for line in cover(questions_pdf))


def is_listening(questions_pdf: Path) -> bool:
    import pymupdf

    with pymupdf.open(questions_pdf) as pdf:
        return any(line.strip() == "Listening" for line in pdf[0].get_text().splitlines())


def component(questions_pdf: Path) -> str:
    """What the cover page says: 'a2-reading-writing', 'a2-listening', 'b1-reading', 'b1-writing' or 'b1-listening'."""
    lines = cover(questions_pdf)
    if not is_b1(questions_pdf):
        return "a2-listening" if "Listening" in lines else "a2-reading-writing"
    for name in ("Listening", "Writing", "Reading"):
        if name in lines:
            return f"b1-{name.lower()}"
    return "b1-reading"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("questions", type=Path, help="question paper PDF")
    parser.add_argument("key", type=Path, nargs="?", help="answer key PDF (not needed for B1 Writing)")
    parser.add_argument("--title", required=True, help="paper title shown to staff, e.g. 'A2 Key for Schools – Test 1 – Reading & Writing'")
    parser.add_argument("--audio-dir", type=Path, help="Listening: folder with one audio track per part (P1_….mp3, part2.mp3, …)")
    parser.add_argument("-o", "--output", type=Path, help="output folder (default: package-<component> next to the PDF)")
    parser.add_argument("--report", type=Path, help="also write the errors and checks as JSON (used by the staff portal's PDF import)")
    args = parser.parse_args()

    kind = component(args.questions)
    listening = kind.endswith("listening")
    missing = None
    if listening and (args.audio_dir is None or not args.audio_dir.is_dir()):
        missing = "This is a Listening paper: give the folder with one audio track per part (--audio-dir)."
    if kind != "b1-writing" and args.key is None:
        missing = "This paper needs its answer key PDF."
    folder = {"a2-reading-writing": "package-reading-writing", "a2-listening": "package-listening"}.get(kind, f"package-{kind[3:]}")
    out_dir = args.output or args.questions.parent / folder

    converters = {
        "a2-reading-writing": lambda: a2_reading_writing.convert_files(args.questions, args.key, out_dir, args.title),
        "a2-listening": lambda: a2_listening.convert_files(args.questions, args.key, args.audio_dir, out_dir, args.title),
        "b1-reading": lambda: b1_reading.convert_files(args.questions, args.key, out_dir, args.title),
        "b1-writing": lambda: b1_writing.convert_files(args.questions, out_dir, args.title),
        "b1-listening": lambda: b1_listening.convert_files(args.questions, args.key, args.audio_dir, out_dir, args.title),
    }
    try:
        if missing:
            raise ValueError(missing)
        report = converters[kind]()
        warnings, checks, crops = report.warnings, report.checks, len(report.crops)
    except ValueError as error:  # a missing file, or an answer key whose numbers do not pair with answers
        warnings, checks, crops = [str(error)], [], 0

    if args.report:
        args.report.write_text(json.dumps({"component": kind, "warnings": warnings, "checks": checks}, ensure_ascii=False), encoding="utf-8")

    sys.stdout.reconfigure(encoding="utf-8")
    print(f"Draft written to {out_dir} ({crops} pictures). Read {out_dir / 'REVIEW.md'} before packing.")
    for warning in warnings:
        print(f"  ERROR  {warning}")
    for check in checks:
        print(f"  CHECK  {check}")
    return 1 if warnings else 0


if __name__ == "__main__":
    sys.exit(main())
