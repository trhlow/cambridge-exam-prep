"""Builds setup/HuongDan_PhongMay.docx from setup/HUONG_DAN_PHONG_MAY.md.

The Word guide for non-technical readers (HuongDan_CaiDat_ChayThu.docx) is the template: its cover, styles, header,
table of contents and block shapes (grey command box, coloured notes, blue-headed tables, "Bước n." lists) are
copied, so both guides look alike. Run from the repository root:

    python -I setup/build_lab_guide.py
    powershell -ExecutionPolicy Bypass -File thesis/doc/report-src/update_fields.ps1 -Doc setup/HuongDan_PhongMay.docx

The second command lets Word fill the table of contents and page numbers (Word must not have the file open).
"""
import copy
import os
import re

import docx
from docx.oxml.ns import qn

HERE = os.path.dirname(os.path.abspath(__file__))
TEMPLATE = os.path.join(HERE, "HuongDan_CaiDat_ChayThu.docx")
SOURCE = os.path.join(HERE, "HUONG_DAN_PHONG_MAY.md")
OUT = os.path.join(HERE, "HuongDan_PhongMay.docx")

COVER = {
    "HƯỚNG DẪN CÀI ĐẶT VÀ CHẠY THỬ": "HƯỚNG DẪN CHẠY THỬ TRÊN PHÒNG MÁY",
    "Dành cho người không có chuyên môn công nghệ thông tin": "Một máy chủ và nhiều máy con trong mạng LAN",
}
HEADER = "Hướng dẫn chạy thử trên phòng máy – hệ thống thi Victory"
TABLE_WIDTH = 9071
INLINE = re.compile(r"(\*\*[^*]+\*\*|\*[^*]+\*|`[^`]+`)")


def text_of(el):
    return "".join(t.text or "" for t in el.iter(qn("w:t")))


def runs(p, text):
    """Adds the text to the paragraph, turning **bold**, *italic* and `code` into Word formatting."""
    for part in INLINE.split(text):
        if not part:
            continue
        r = p.makeelement(qn("w:r"), {})
        props = None
        if part.startswith("**"):
            part, props = part[2:-2], ["w:b", "w:bCs"]
        elif part.startswith("`"):
            part, props = part[1:-1], "code"
        elif part.startswith("*"):
            part, props = part[1:-1], ["w:i", "w:iCs"]
        if props:
            rpr = r.makeelement(qn("w:rPr"), {})
            if props == "code":
                fonts = rpr.makeelement(qn("w:rFonts"), {qn(f"w:{a}"): "Consolas" for a in ("ascii", "eastAsia", "hAnsi", "cs")})
                rpr.append(fonts)
                for tag in ("w:sz", "w:szCs"):
                    rpr.append(rpr.makeelement(qn(tag), {qn("w:val"): "22"}))
            else:
                for tag in props:
                    rpr.append(rpr.makeelement(qn(tag), {}))
            r.append(rpr)
        t = r.makeelement(qn("w:t"), {qn("xml:space"): "preserve"})
        t.text = part
        r.append(t)
        p.append(r)
    return p


def emptied(proto):
    """A copy of the paragraph with its formatting and no text."""
    p = copy.deepcopy(proto)
    for child in list(p):
        if child.tag != qn("w:pPr"):
            p.remove(child)
    return p


class Builder:
    def __init__(self):
        self.doc = docx.Document(TEMPLATE)
        body = self.doc.element.body
        self.els = list(body.iterchildren())
        find = lambda pred: next(e for e in self.els if pred(e))
        style = lambda e: (e.find(qn("w:pPr") + "/" + qn("w:pStyle")) is not None and e.find(qn("w:pPr") + "/" + qn("w:pStyle")).get(qn("w:val")))
        fill = lambda e, f: e.tag == qn("w:tbl") and f'w:fill="{f}"' in e.xml
        num = lambda e: e.find(qn("w:pPr") + "/" + qn("w:numPr") + "/" + qn("w:numId"))
        self.h1 = find(lambda e: style(e) == "Heading1")
        self.h2 = find(lambda e: style(e) == "Heading2")
        first_h1 = self.els.index(self.h1)
        self.normal = self.els[first_h1 + 2]
        self.spacer = find(lambda e: e.tag == qn("w:p") and not text_of(e) and self.els.index(e) > first_h1)
        self.bullet = find(lambda e: e.tag == qn("w:p") and num(e) is not None and num(e).get(qn("w:val")) == "2")
        self.step = find(lambda e: e.tag == qn("w:p") and num(e) is not None and num(e).get(qn("w:val")) == "3")
        self.note = find(lambda e: fill(e, "FFF4E0"))
        self.code = find(lambda e: fill(e, "F2F2F2"))
        self.table = find(lambda e: fill(e, "D9E2F3"))
        self.numbering = self.doc.part.numbering_part.element
        for el in self.els[first_h1:]:
            if el.tag != qn("w:sectPr"):
                body.remove(el)
        self.body = body
        self.sect = body.find(qn("w:sectPr"))

    def add(self, el):
        self.sect.addprevious(el)

    def heading(self, level, text):
        self.add(runs(emptied(self.h1 if level == 1 else self.h2), text))

    def paragraph(self, text):
        self.add(runs(emptied(self.normal), text))

    def bullet_item(self, text):
        self.add(runs(emptied(self.bullet), text))

    def new_list(self):
        """A fresh "Bước 1." list: a new w:num on the template's numbered list, restarting at 1."""
        ids = [int(n.get(qn("w:numId"))) for n in self.numbering.findall(qn("w:num"))]
        abstract = self.step.find(qn("w:pPr") + "/" + qn("w:numPr") + "/" + qn("w:numId")).get(qn("w:val"))
        abstract_id = next(n for n in self.numbering.findall(qn("w:num")) if n.get(qn("w:numId")) == abstract).find(qn("w:abstractNumId")).get(qn("w:val"))
        num = self.numbering.makeelement(qn("w:num"), {qn("w:numId"): str(max(ids) + 1)})
        num.append(num.makeelement(qn("w:abstractNumId"), {qn("w:val"): abstract_id}))
        override = num.makeelement(qn("w:lvlOverride"), {qn("w:ilvl"): "0"})
        override.append(override.makeelement(qn("w:startOverride"), {qn("w:val"): "1"}))
        num.append(override)
        self.numbering.append(num)
        self.list_id = str(max(ids) + 1)

    def step_item(self, text):
        p = runs(emptied(self.step), text)
        p.find(qn("w:pPr") + "/" + qn("w:numPr") + "/" + qn("w:numId")).set(qn("w:val"), self.list_id)
        self.add(p)

    def box(self, proto, lines, title=None):
        """A one-cell box: the grey command box (lines kept as they are) or a coloured note with a bold title."""
        tbl = copy.deepcopy(proto)
        cell = tbl.find(".//" + qn("w:tc"))
        paras = cell.findall(qn("w:p"))
        for p in paras:
            cell.remove(p)
        if title is not None:
            cell.append(runs(emptied(paras[0]), f"**{title}**"))
            for line in lines:
                cell.append(runs(emptied(paras[-1]), line))
        else:
            run = paras[0].find(qn("w:r"))  # carries the monospace font of the command box
            for line in lines:
                p = emptied(paras[0])
                r = copy.deepcopy(run) if run is not None else p.makeelement(qn("w:r"), {})
                p.append(r)
                for t in r.findall(qn("w:t")):
                    r.remove(t)
                t = r.makeelement(qn("w:t"), {qn("xml:space"): "preserve"})
                t.text = line
                r.append(t)
                cell.append(p)
        self.add(tbl)
        self.add(copy.deepcopy(self.spacer))

    def data_table(self, rows):
        tbl = copy.deepcopy(self.table)
        proto_rows = tbl.findall(qn("w:tr"))
        head_row, body_row = proto_rows[0], proto_rows[1]
        for r in proto_rows:
            tbl.remove(r)
        columns = len(rows[0])
        longest = [max(len(re.sub(r"[*`]", "", row[c])) for row in rows) for c in range(columns)]
        weights = [max(n, 2) ** 0.5 for n in longest]  # short columns still get room for a few words
        widths = [int(TABLE_WIDTH * w / sum(weights)) for w in weights]
        widths[-1] += TABLE_WIDTH - sum(widths)
        grid = tbl.find(qn("w:tblGrid"))
        for g in list(grid):
            grid.remove(g)
        for w in widths:
            grid.append(grid.makeelement(qn("w:gridCol"), {qn("w:w"): str(w)}))
        for i, row in enumerate(rows):
            tr = copy.deepcopy(head_row if i == 0 else body_row)
            cells = tr.findall(qn("w:tc"))
            proto_cell = cells[0]
            for c in cells:
                tr.remove(c)
            for text, width in zip(row, widths):
                tc = copy.deepcopy(proto_cell)
                tc.find(qn("w:tcPr") + "/" + qn("w:tcW")).set(qn("w:w"), str(width))
                p = tc.find(qn("w:p"))
                bold = i == 0 and p.find(".//" + qn("w:b")) is not None
                for extra in tc.findall(qn("w:p"))[1:]:
                    tc.remove(extra)
                tc.replace(p, runs(emptied(p), f"**{text}**" if bold and text else text))
                tr.append(tc)
            tbl.append(tr)
        self.add(tbl)
        self.add(copy.deepcopy(self.spacer))

    def cover_and_header(self, title):
        for p in self.doc.paragraphs[:12]:
            new = COVER.get(p.text.strip())
            if new:
                for r in p.runs[1:]:
                    r._r.getparent().remove(r._r)
                p.runs[0].text = new
        for section in self.doc.sections:
            for p in section.header.paragraphs:
                if p.text.strip():
                    for r in p.runs[1:]:
                        r._r.getparent().remove(r._r)
                    p.runs[0].text = HEADER
        self.doc.core_properties.title = title


def cells(line):
    return [c.strip() for c in line.strip().strip("|").split("|")]


def build():
    b = Builder()
    lines = open(SOURCE, encoding="utf-8").read().splitlines()
    i = 0
    in_list = False
    while i < len(lines):
        line = lines[i]
        stripped = line.strip()
        indented = line.startswith("   ")
        if not stripped:
            i += 1
            continue
        if line.startswith("# "):
            b.cover_and_header(line[2:].strip())
        elif line.startswith("## "):
            in_list = False
            b.heading(1, re.sub(r"^(\d+)\. ", r"Phần \1. ", line[3:].strip()))
        elif line.startswith("### "):
            in_list = False
            b.heading(2, line[4:].strip())
        elif stripped.startswith("```"):
            block = []
            i += 1
            while not lines[i].strip().startswith("```"):
                block.append(lines[i][3:] if indented and lines[i].startswith("   ") else lines[i])
                i += 1
            b.box(b.code, block)
        elif stripped.startswith(">"):
            quote = []
            while i < len(lines) and lines[i].strip().startswith(">"):
                quote.append(lines[i].strip()[1:].strip())
                i += 1
            b.box(b.note, [" ".join(q for q in quote if q)], title="Lưu ý")
            continue
        elif stripped.startswith("|"):
            rows = []
            while i < len(lines) and lines[i].strip().startswith("|"):
                if not re.fullmatch(r"\|[\s\-:|]+\|", lines[i].strip()):
                    rows.append(cells(lines[i]))
                i += 1
            b.data_table(rows)
            continue
        elif re.match(r"\d+\. ", stripped) and not indented:
            if not in_list:
                b.new_list()
                in_list = True
            b.step_item(re.sub(r"^\d+\. ", "", stripped))
        elif stripped.startswith("- "):
            b.bullet_item(stripped[2:])
        else:
            if not indented:
                in_list = False
            b.paragraph(stripped)
        i += 1
    b.doc.save(OUT)
    print(f"wrote {OUT}")


if __name__ == "__main__":
    build()
