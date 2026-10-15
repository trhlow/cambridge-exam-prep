"""Convert a Cambridge A2 Key (for Schools) question paper PDF and its answer key into a draft exam package folder.

Usage:
    python -I setup/exam-package/pdf2package.py <questions.pdf> <answer-key.pdf> --title "<paper title>" [-o <folder>]
    python -I setup/exam-package/pdf2package.py <listening.pdf> <answer-key.pdf> --audio-dir <folder> --title "…"

Reading & Writing or Listening is read from the cover page. Listening needs one audio track per part in
--audio-dir, named by part (P1_….mp3, part2.mp3, …).

The folder gets paper.json, images/ (pictures cropped from the PDF), audio/ for Listening, and REVIEW.md. Read
REVIEW.md, fix what it lists, then pack the folder with pack.py and import it. Keep the output under
setup/exam-package/sources/ (git-ignored): it is exam content and must never be committed.
"""

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from converter import a2_listening, a2_reading_writing  # noqa: E402


def is_listening(questions_pdf: Path) -> bool:
    import pymupdf

    with pymupdf.open(questions_pdf) as pdf:
        return any(line.strip() == "Listening" for line in pdf[0].get_text().splitlines())


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("questions", type=Path, help="question paper PDF")
    parser.add_argument("key", type=Path, help="answer key PDF")
    parser.add_argument("--title", required=True, help="paper title shown to staff, e.g. 'A2 Key for Schools – Test 1 – Reading & Writing'")
    parser.add_argument("--audio-dir", type=Path, help="Listening: folder with one audio track per part (P1_….mp3, part2.mp3, …)")
    parser.add_argument("-o", "--output", type=Path, help="output folder (default: package-reading-writing or package-listening next to the PDF)")
    parser.add_argument("--report", type=Path, help="also write the errors and checks as JSON (used by the staff portal's PDF import)")
    args = parser.parse_args()

    listening = is_listening(args.questions)
    if listening and (args.audio_dir is None or not args.audio_dir.is_dir()):
        parser.error("this is a Listening paper: give --audio-dir with one audio track per part")
    out_dir = args.output or args.questions.parent / ("package-listening" if listening else "package-reading-writing")
    try:
        if listening:
            report = a2_listening.convert_files(args.questions, args.key, args.audio_dir, out_dir, args.title)
        else:
            report = a2_reading_writing.convert_files(args.questions, args.key, out_dir, args.title)
        warnings, checks, crops = report.warnings, report.checks, len(report.crops)
    except ValueError as error:  # e.g. an answer key whose numbers do not pair with answers
        warnings, checks, crops = [str(error)], [], 0

    if args.report:
        args.report.write_text(json.dumps({
            "component": "listening" if listening else "reading-writing", "warnings": warnings, "checks": checks,
        }, ensure_ascii=False), encoding="utf-8")

    sys.stdout.reconfigure(encoding="utf-8")
    print(f"Draft written to {out_dir} ({crops} pictures). Read {out_dir / 'REVIEW.md'} before packing.")
    for warning in warnings:
        print(f"  ERROR  {warning}")
    for check in checks:
        print(f"  CHECK  {check}")
    return 1 if warnings else 0


if __name__ == "__main__":
    sys.exit(main())
