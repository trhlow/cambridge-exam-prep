"""What the converter sees of a PDF: text lines with positions, and where the pictures are.

Coordinates are PDF points from the top-left of the page (A4 is about 595 × 842).
"""

from dataclasses import dataclass
from pathlib import Path

BOLD_FLAG = 16


@dataclass(frozen=True)
class Line:
    page: int
    x0: float
    y0: float
    x1: float
    text: str
    bold: bool = False
    size: float = 11.0


@dataclass(frozen=True)
class Picture:
    page: int
    x0: float
    y0: float
    x1: float
    y1: float
    xref: int = 0  # the embedded image in the PDF, when known


@dataclass(frozen=True)
class Crop:
    """A PNG saved under `name` (a path inside the package, e.g. images/p1-q1.png).

    With an xref the embedded image is saved from its own pixels; otherwise the page rectangle is rendered.
    """

    name: str
    page: int
    x0: float
    y0: float
    x1: float
    y1: float
    xref: int = 0


@dataclass(frozen=True)
class Document:
    lines: tuple[Line, ...]
    pictures: tuple[Picture, ...]


def read_document(path: Path) -> Document:
    import pymupdf

    lines: list[Line] = []
    pictures: list[Picture] = []
    with pymupdf.open(path) as pdf:
        for page_no, page in enumerate(pdf):
            for block in page.get_text("dict")["blocks"]:
                for line in block.get("lines", []):
                    spans = [span for span in line["spans"] if span["text"].strip()]
                    text = " ".join("".join(span["text"] for span in line["spans"]).replace(" ", " ").split())
                    if not spans or not text:
                        continue
                    x0, y0, x1, _ = line["bbox"]
                    lines.append(Line(
                        page=page_no, x0=x0, y0=y0, x1=x1, text=text,
                        bold=any(span["flags"] & BOLD_FLAG for span in spans),
                        size=round(max(span["size"] for span in spans), 1),
                    ))
            seen = set()
            for image in page.get_images(full=True):
                for rect in page.get_image_rects(image[0]):
                    box = (round(rect.x0, 1), round(rect.y0, 1), round(rect.x1, 1), round(rect.y1, 1))
                    if box not in seen and rect.width > 20 and rect.height > 20:
                        seen.add(box)
                        pictures.append(Picture(page_no, *box, xref=image[0]))
    return Document(lines=tuple(lines), pictures=tuple(pictures))


def read_text(path: Path) -> str:
    import pymupdf

    with pymupdf.open(path) as pdf:
        return "\n".join(page.get_text() for page in pdf)
