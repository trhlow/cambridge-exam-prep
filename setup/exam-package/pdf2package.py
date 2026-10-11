"""Convert a Cambridge A2 Key (for Schools) Reading & Writing PDF and its answer key into a draft exam package folder.

Usage:
    python -I setup/exam-package/pdf2package.py <questions.pdf> <answer-key.pdf> --title "<paper title>" [-o <folder>]

The folder gets paper.json, images/ (notices, photos and pictures cropped from the PDF) and REVIEW.md. Read
REVIEW.md, fix what it lists, then pack the folder with pack.py and import it. Keep the output under
setup/exam-package/sources/ (git-ignored): it is exam content and must never be committed.
"""

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from converter.a2_reading_writing import convert_files  # noqa: E402


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("questions", type=Path, help="question paper PDF")
    parser.add_argument("key", type=Path, help="answer key PDF")
    parser.add_argument("--title", required=True, help="paper title shown to staff, e.g. 'A2 Key for Schools – Test 1 – Reading & Writing'")
    parser.add_argument("-o", "--output", type=Path, help="output folder (default: package-reading-writing next to the PDF)")
    args = parser.parse_args()

    out_dir = args.output or args.questions.parent / "package-reading-writing"
    report = convert_files(args.questions, args.key, out_dir, args.title)

    sys.stdout.reconfigure(encoding="utf-8")
    print(f"Draft written to {out_dir} ({len(report.crops)} pictures). Read {out_dir / 'REVIEW.md'} before packing.")
    for warning in report.warnings:
        print(f"  ERROR  {warning}")
    for check in report.checks:
        print(f"  CHECK  {check}")
    return 1 if report.warnings else 0


if __name__ == "__main__":
    sys.exit(main())
