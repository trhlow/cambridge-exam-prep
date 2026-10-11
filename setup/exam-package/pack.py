"""Pack an exam-package folder (paper.json + images/ + audio/) into a .zip for POST /api/admin/papers/import.

Usage:
    python -I setup/exam-package/pack.py <folder> [--placeholders] [-o out.zip]

--placeholders creates grey PNG pictures and one-second silent audio for referenced files that are
missing, so the synthetic samples can be imported for a try-out. Never use it for a real exam.
"""

import argparse
import json
import re
import struct
import sys
import zipfile
import zlib
from pathlib import Path

ASSET_REFERENCE = re.compile(r"""asset:([^"'\s>]+)""")


def referenced_files(node, found):
    """Collect every path the paper refers to: asset: links in HTML, "image" and "audio" fields."""
    if isinstance(node, dict):
        for key, value in node.items():
            if key in ("image", "audio") and isinstance(value, str):
                found.add(value)
            else:
                referenced_files(value, found)
    elif isinstance(node, list):
        for item in node:
            referenced_files(item, found)
    elif isinstance(node, str):
        found.update(ASSET_REFERENCE.findall(node))
    return found


def grey_png(width=320, height=200):
    def chunk(kind, data):
        body = kind + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)

    raw = b"".join(b"\x00" + b"\xcc" * width for _ in range(height))
    header = struct.pack(">IIBBBBB", width, height, 8, 0, 0, 0, 0)
    return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", header) + chunk(b"IDAT", zlib.compress(raw)) + chunk(b"IEND", b"")


def silent_wav(seconds=1, rate=8000):
    samples = b"\x00\x00" * rate * seconds
    return (b"RIFF" + struct.pack("<I", 36 + len(samples)) + b"WAVEfmt "
            + struct.pack("<IHHIIHH", 16, 1, 1, rate, rate * 2, 2, 16)
            + b"data" + struct.pack("<I", len(samples)) + samples)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("folder", type=Path)
    parser.add_argument("--placeholders", action="store_true")
    parser.add_argument("-o", "--output", type=Path)
    args = parser.parse_args()

    paper_path = args.folder / "paper.json"
    paper = json.loads(paper_path.read_text(encoding="utf-8"))
    output = args.output or Path(f"{args.folder.resolve().name}.zip")

    missing = []
    with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as package:
        package.write(paper_path, "paper.json")
        for name in sorted(referenced_files(paper, set())):
            source = args.folder / name
            if source.is_file():
                package.write(source, name)
            elif args.placeholders:
                package.writestr(name, silent_wav() if name.startswith("audio/") else grey_png())
            else:
                missing.append(name)

    if missing:
        output.unlink()
        sys.exit("Missing files (add them or use --placeholders):\n  " + "\n  ".join(missing))
    print(f"Wrote {output}")


if __name__ == "__main__":
    main()
