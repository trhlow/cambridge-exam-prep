"""Writes the draft package folder: paper.json, the cropped pictures and REVIEW.md for the person who checks it."""

import json
import re
from datetime import datetime
from pathlib import Path

ZOOM = 3.0  # 216 dpi: notices and photos stay sharp on a 1080p screen
TAG = re.compile(r"<[^>]+>")


def write_package(out_dir: Path, paper: dict, report, questions_pdf: Path, key_pdf: Path) -> None:
    import pymupdf

    (out_dir / "images").mkdir(parents=True, exist_ok=True)
    (out_dir / "paper.json").write_text(json.dumps(paper, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    with pymupdf.open(questions_pdf) as pdf:
        for crop in report.crops:
            _save_crop(pymupdf, pdf, crop, out_dir / crop.name)
    (out_dir / "REVIEW.md").write_text(review(paper, report, questions_pdf, key_pdf, out_dir), encoding="utf-8")


def _save_crop(pymupdf, pdf, crop, target: Path) -> None:
    """The embedded image's own pixels when there is one (nothing around it gets in), else the rendered rectangle."""
    if crop.xref and not pdf.extract_image(crop.xref).get("smask"):
        pixmap = pymupdf.Pixmap(pdf, crop.xref)
        if pixmap.n - pixmap.alpha >= 4:  # CMYK → RGB, PNG cannot hold CMYK
            pixmap = pymupdf.Pixmap(pymupdf.csRGB, pixmap)
        pixmap.save(target)
        return
    clip = pymupdf.Rect(crop.x0, crop.y0, crop.x1, crop.y1) & pdf[crop.page].rect
    pdf[crop.page].get_pixmap(clip=clip, matrix=pymupdf.Matrix(ZOOM, ZOOM)).save(target)


def _cell(text: str) -> str:
    return " ".join(TAG.sub(" ", text).split()).replace("|", "\\|")


def review(paper: dict, report, questions_pdf: Path, key_pdf: Path, out_dir: Path) -> str:
    rows = []
    for part in paper["parts"]:
        for group in part["groups"]:
            for q in group["questions"]:
                options = "; ".join(f"{o['key']} {o['text']}" for o in q.get("options", []))
                answers = " / ".join(q.get("answers", [])) or ("(giáo viên chấm)" if group["interactionType"] == "Essay" else "**THIẾU**")
                prompt = q.get("promptHtml") or ("có hình" if q.get("stimulusHtml") else "")
                rows.append(f"| {q['number']} | {part['partNo']} | {group['interactionType']} | {_cell(prompt)} | {_cell(options)} | {_cell(answers)} |")

    def bullets(items: list[str], empty: str) -> str:
        return "\n".join(f"- {item}" for item in items) if items else f"- {empty}"

    return f"""# Bản nháp cần duyệt: {paper['title']}

- Đề: `{questions_pdf.name}`
- Đáp án: `{key_pdf.name}`
- Tạo lúc {datetime.now():%d/%m/%Y %H:%M}: {len(rows)} câu, {len(paper['parts'])} part, {len(report.crops)} hình trong `images/`

## Lỗi phải sửa trước khi import

{bullets(report.warnings, "Không có.")}

## Cần xem lại

{bullets(report.checks, "Không có mục riêng.")}
- Mở từng hình trong `images/` và so với PDF: cắt đủ, không lẹm chữ.
- Đọc lướt `paper.json`: chữ trong đoạn văn đúng thứ tự, không dính dòng tiêu đề hay số trang.

## Từng câu

| Câu | Part | Kiểu | Câu hỏi | Phương án | Đáp án |
|---|---|---|---|---|---|
{chr(10).join(rows)}

## Sau khi duyệt

```bash
python -I setup/exam-package/pack.py "{out_dir.as_posix()}" -o "{out_dir.as_posix()}.zip"
```

Rồi import file zip qua `POST /api/admin/papers/import` (xem `setup/exam-package/FORMAT.md`). Đề vào hệ thống ở trạng thái Draft.
"""
