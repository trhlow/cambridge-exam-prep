"""Builds the capstone report from the faculty template and noidung.md.

    python -I thesis/doc/report-src/build_report.py            (repo root; needs python-docx)
    powershell -File thesis/doc/report-src/update_fields.ps1   (Word fills the table of contents, lists and numbers)

Keeps the template's two cover pages (section 1) with their logo, fills the title and student, and replaces the rest
with the content of noidung.md. Faculty rules (0_MOT_SO_QUY_DINH_CHUNG_2026.doc): Times New Roman 13 pt, 1.5 lines,
6 pt before and after, margins 2/2/3/1.5 cm, page number bottom right, chapters and sections numbered 1, 1.1, 1.1.1
(the template's Heading 1-3 list "CHƯƠNG %1:"), IEEE references numbered in dictionary order. Tables are captioned
above, figures below, numbered per chapter with SEQ fields so Word builds the lists of tables and figures.

Markup of noidung.md: "# X" chapter, "#! X" unnumbered title in the table of contents, "## " / "### " sections,
"- " bullet, "|table| caption | widths" ... "|end|" (first row is the header), "!fig path | caption | width",
"[@key]" or "[@key, tr. 5]" citations (refs.py), *italic*, and the @ commands handled in build(). Tables and figures
after "#! PHỤ LỤC" are numbered PL.1, PL.2, … (a SEQ field restarted at that number, still in the lists of tables
and figures).
"""
import copy
import os
import re
import sys

import docx
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Pt

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(HERE)))
sys.path.insert(0, HERE)
from refs import REFS  # noqa: E402

TEMPLATE = os.path.join(ROOT, "thesis", "refs", "faculty-templates", "2_MAU_BAO_CAO_2026.docx")
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "thesis", "doc", "BaoCao_DoAnChuyenNganh_TranHoangLong.docx")

TITLE = "XÂY DỰNG WEBSITE HỖ TRỢ THI CHỨNG CHỈ TIẾNG ANH CAMBRIDGE"
STUDENT = {"Sinh viên:": "Trần Hoàng Long", "Lớp:": "DA23TTA", "MSSV:": "110123025", "GVHD:": "ThS. Nhan Minh Phúc"}
TEXT_WIDTH_TWIPS = 9355  # A4 21 cm - 3 cm - 1.5 cm


# ---------------------------------------------------------------- low-level XML helpers

def el(tag, **attrs):
    e = OxmlElement(tag)
    for k, v in attrs.items():
        e.set(qn(k), str(v))
    return e


def set_child(parent, tag, **attrs):
    for old in parent.findall(qn(tag)):
        parent.remove(old)
    child = el(tag, **attrs)
    parent.append(child)
    return child


def ppr(style_el):
    p = style_el.find(qn("w:pPr"))
    if p is None:
        p = el("w:pPr")
        style_el.append(p)
    return p


def rpr(style_el):
    r = style_el.find(qn("w:rPr"))
    if r is None:
        r = el("w:rPr")
        style_el.append(r)
    return r


def order_ppr(p):
    """Word refuses pPr children out of schema order; sort the ones this script touches."""
    order = ["w:pStyle", "w:keepNext", "w:keepLines", "w:pageBreakBefore", "w:numPr", "w:pBdr", "w:tabs", "w:spacing",
             "w:ind", "w:jc", "w:outlineLvl", "w:rPr"]
    rank = {qn(t): i for i, t in enumerate(order)}
    kids = list(p)
    for k in kids:
        p.remove(k)
    for k in sorted(kids, key=lambda k: rank.get(k.tag, 50)):
        p.append(k)


def add_field(paragraph, instr, shown=""):
    """A complex field; Word fills it when the fields are updated."""
    def run_with(child):
        r = el("w:r")
        r.append(child)
        paragraph._p.append(r)
        return r
    run_with(el("w:fldChar", **{"w:fldCharType": "begin"}))
    t = el("w:instrText")
    t.set("{http://www.w3.org/XML/1998/namespace}space", "preserve")
    t.text = f" {instr} "
    run_with(t)
    run_with(el("w:fldChar", **{"w:fldCharType": "separate"}))
    shown_t = el("w:t")
    shown_t.set("{http://www.w3.org/XML/1998/namespace}space", "preserve")
    shown_t.text = shown
    run_with(shown_t)
    run_with(el("w:fldChar", **{"w:fldCharType": "end"}))


# ---------------------------------------------------------------- styles

def setup_styles(doc):
    styles = doc.styles
    normal = styles["Normal"].element
    set_child(rpr(normal), "w:rFonts", **{"w:ascii": "Times New Roman", "w:hAnsi": "Times New Roman",
                                         "w:cs": "Times New Roman", "w:eastAsia": "Times New Roman"})
    for tag in ("w:sz", "w:szCs"):
        set_child(rpr(normal), tag, **{"w:val": 26})

    body = styles["DoanVan"].element
    p = ppr(body)
    set_child(p, "w:spacing", **{"w:before": 120, "w:after": 120, "w:line": 360, "w:lineRule": "auto"})
    set_child(p, "w:ind", **{"w:firstLine": 720})
    set_child(p, "w:jc", **{"w:val": "both"})
    order_ppr(p)

    h1 = styles["Heading 1"].element
    p = ppr(h1)
    set_child(p, "w:pageBreakBefore")
    set_child(p, "w:spacing", **{"w:before": 0, "w:after": 360, "w:line": 360, "w:lineRule": "auto"})
    set_child(p, "w:ind", **{"w:left": 0, "w:firstLine": 0})
    order_ppr(p)
    for name in ("Heading 2", "Heading 3"):
        p = ppr(styles[name].element)
        set_child(p, "w:spacing", **{"w:before": 240, "w:after": 120, "w:line": 360, "w:lineRule": "auto"})
        set_child(p, "w:jc", **{"w:val": "both"})
        order_ppr(p)
    # 2.1.1 in bold italic, as usual for the third level
    set_child(rpr(styles["Heading 3"].element), "w:i")

    # Unnumbered titles: DANHMUC goes into the table of contents, TieuDe (copy) does not.
    dm = styles["DANHMUC"].element
    p = ppr(dm)
    set_child(p, "w:pageBreakBefore")
    set_child(p, "w:spacing", **{"w:before": 0, "w:after": 360, "w:line": 360, "w:lineRule": "auto"})
    set_child(p, "w:ind", **{"w:left": 0, "w:firstLine": 0})
    set_child(p, "w:jc", **{"w:val": "center"})
    set_child(p, "w:outlineLvl", **{"w:val": 0})
    order_ppr(p)
    set_child(rpr(dm), "w:sz", **{"w:val": 26})
    td = copy.deepcopy(dm)
    set_child(ppr(td), "w:outlineLvl", **{"w:val": 9})
    td.set(qn("w:styleId"), "TieuDe")
    td.find(qn("w:name")).set(qn("w:val"), "TieuDe")
    link = td.find(qn("w:link"))
    if link is not None:
        td.remove(link)
    dm.addnext(td)

    for name, before, after in (("Caption", 60, 240), ("Bảng", 240, 60)):
        p = ppr(styles[name].element)
        set_child(p, "w:spacing", **{"w:before": before, "w:after": after, "w:line": 360, "w:lineRule": "auto"})
        set_child(p, "w:ind", **{"w:firstLine": 0})
        set_child(p, "w:jc", **{"w:val": "center"})
        order_ppr(p)

    for name in ("toc 1", "toc 2", "toc 3", "table of figures"):
        p = ppr(styles[name].element)
        tabs = set_child(p, "w:tabs")
        tabs.append(el("w:tab", **{"w:val": "right", "w:leader": "dot", "w:pos": TEXT_WIDTH_TWIPS}))
        set_child(p, "w:spacing", **{"w:before": 60, "w:after": 60, "w:line": 312, "w:lineRule": "auto"})
        order_ppr(p)
    set_child(rpr(styles["table of figures"].element), "w:i", **{"w:val": 0})

    add_bullet_style(doc)


def add_bullet_style(doc):
    numbering = doc.part.numbering_part.element
    ids = [int(a.get(qn("w:abstractNumId"))) for a in numbering.findall(qn("w:abstractNum"))]
    nums = [int(n.get(qn("w:numId"))) for n in numbering.findall(qn("w:num"))]
    abstract_id, num_id = max(ids) + 1, max(nums) + 1
    a = el("w:abstractNum", **{"w:abstractNumId": abstract_id})
    a.append(el("w:multiLevelType", **{"w:val": "singleLevel"}))
    lvl = el("w:lvl", **{"w:ilvl": 0})
    lvl.append(el("w:start", **{"w:val": 1}))
    lvl.append(el("w:numFmt", **{"w:val": "bullet"}))
    lvl.append(el("w:lvlText", **{"w:val": "–"}))
    lvl.append(el("w:lvlJc", **{"w:val": "left"}))
    lp = el("w:pPr")
    lp.append(el("w:ind", **{"w:left": 1077, "w:hanging": 357}))
    lvl.append(lp)
    lr = el("w:rPr")
    lr.append(el("w:rFonts", **{"w:ascii": "Times New Roman", "w:hAnsi": "Times New Roman"}))
    lvl.append(lr)
    a.append(lvl)
    first_num = numbering.find(qn("w:num"))
    first_num.addprevious(a)
    n = el("w:num", **{"w:numId": num_id})
    n.append(el("w:abstractNumId", **{"w:val": abstract_id}))
    numbering.append(n)

    styles = doc.styles.element
    s = copy.deepcopy(doc.styles["DoanVan"].element)
    s.set(qn("w:styleId"), "GachDau")
    s.find(qn("w:name")).set(qn("w:val"), "GachDau")
    link = s.find(qn("w:link"))
    if link is not None:
        s.remove(link)
    p = ppr(s)
    numpr = set_child(p, "w:numPr")
    numpr.append(el("w:ilvl", **{"w:val": 0}))
    numpr.append(el("w:numId", **{"w:val": num_id}))
    set_child(p, "w:ind", **{"w:left": 1077, "w:hanging": 357})
    set_child(p, "w:spacing", **{"w:before": 60, "w:after": 60, "w:line": 360, "w:lineRule": "auto"})
    order_ppr(p)
    styles.append(s)


# ---------------------------------------------------------------- the template

def fill_covers(doc):
    for p in doc.paragraphs[:67]:
        if "CƠ SỞ NGÀNH" in p.text:
            for r in p.runs:
                r.font.highlight_color = None
                r.text = r.text.replace("CƠ SỞ NGÀNH ", "").replace("(CHUYÊN NGÀNH)", "CHUYÊN NGÀNH")
    covers = doc.paragraphs[:67]
    for i, p in enumerate(covers):
        if p.text.strip().startswith("VĨNH LONG"):
            for blank in covers[i - 2:i]:
                if not blank.text.strip():
                    blank._p.getparent().remove(blank._p)
    for p in doc.paragraphs[:65]:
        text = p.text.strip()
        if text == "TÊN ĐỀ TÀI":
            p.runs[0].text = TITLE
            for r in p.runs[1:]:
                r.text = ""
            p.runs[0].font.size = Pt(18)
        if text in STUDENT:
            r = p.add_run(" " + STUDENT[text])
            r.font.size, r.font.name = p.runs[0].font.size, "Times New Roman"


def fill_footer(doc):
    footer = doc.sections[1].footer
    for p in footer.paragraphs:
        if "GVHD" in p.text:
            runs = p.runs
            runs[0].text = "GVHD: ThS. Nhan Minh Phúc"
            runs[-1].text = "SVTH: Trần Hoàng Long"
    sec = doc.sections[1]
    sec.left_margin, sec.right_margin = Cm(3), Cm(1.5)
    sec.top_margin, sec.bottom_margin = Cm(2), Cm(2)


def clear_body(doc):
    body = doc.element.body
    kids = list(body)
    # the covers end with the paragraph that carries the first section break; the last child is the final sectPr
    end = next(i for i, k in enumerate(kids) if k.tag == qn("w:p") and k.find(".//" + qn("w:sectPr")) is not None)
    for k in kids[end + 1:-1]:
        body.remove(k)


# ---------------------------------------------------------------- content

class Builder:
    def __init__(self, doc):
        self.doc = doc
        self.body = doc.element.body
        self.sect = self.body[-1]
        self.chapter = 0
        self.tables = 0
        self.figures = 0
        self.cited = []
        self.plain_sections = None  # counter while inside an unnumbered title
        self.appendix = None  # {"Bảng": n, "Hình": n} once inside PHỤ LỤC

    # paragraphs are inserted before the final sectPr
    def para(self, style=None, align=None):
        p = self.doc.add_paragraph()
        self.sect.addprevious(p._p)
        if style:
            p.style = self.doc.styles[style]
        if align is not None:
            p.alignment = align
        return p

    def cite(self, m):
        parts = []
        for one in m.group(1).split(";"):
            one = one.strip().lstrip("@")
            key, _, loc = one.partition(",")
            key = key.strip()
            if key not in REFS:
                raise SystemExit(f"unknown reference {key}")
            if key not in self.cited:
                self.cited.append(key)
            parts.append(f"{key}" + (f", {loc.strip()}" if loc else ""))
        return "[" + "; ".join(parts) + "]"

    def runs(self, p, text, italic=False):
        text = re.sub(r"\[(@[^\]]+)\]", self.cite, text)
        for chunk in re.split(r"(\*\*[^*]+\*\*|\*[^*]+\*)", text):
            if not chunk:
                continue
            r = p.add_run()
            if chunk.startswith("**"):
                r.bold, chunk = True, chunk[2:-2]
            elif chunk.startswith("*"):
                r.italic, chunk = True, chunk[1:-1]
            if italic:
                r.italic = True
            r.text = chunk

    def text(self, line, style="DoanVan"):
        self.runs(self.para(style), line)

    def chapter_title(self, title):
        self.chapter += 1
        self.plain_sections = None
        self.para("Heading 1").add_run(title)

    def unnumbered(self, title, in_toc=True):
        self.plain_sections = 0
        if title == "PHỤ LỤC":
            self.appendix = {"Bảng": 0, "Hình": 0}
        self.para("DANHMUC" if in_toc else "TieuDe").add_run(title)

    def section(self, title, level):
        p = self.para(f"Heading {level}")
        if self.plain_sections is not None and level == 2:
            self.plain_sections += 1
            numpr = p._p.get_or_add_pPr()
            n = set_child(numpr, "w:numPr")
            n.append(el("w:ilvl", **{"w:val": 0}))
            n.append(el("w:numId", **{"w:val": 0}))
            order_ppr(numpr)
            title = f"{self.plain_sections}. {title}"
        p.add_run(title)

    def caption(self, kind, text):
        p = self.para("Bảng" if kind == "Bảng" else "Caption")
        if self.appendix is not None:
            self.appendix[kind] += 1
            n = self.appendix[kind]
            p.add_run(f"{kind} PL.")
            add_field(p, f"SEQ {kind} \\r {n} \\* ARABIC", str(n))
        else:
            n = self.tables if kind == "Bảng" else self.figures
            p.add_run(f"{kind} {self.chapter}.")
            add_field(p, f"SEQ {kind} \\* ARABIC \\s 1", str(n))
        self.runs(p, ". " + text)
        return p

    def table(self, caption, widths, rows):
        self.tables += 1
        self.caption("Bảng", caption)
        t = self.doc.add_table(rows=len(rows), cols=len(rows[0]))
        self.sect.addprevious(t._tbl)
        t.style = self.doc.styles["Table Grid"]
        t.alignment = 1
        tblpr = t._tbl.tblPr
        set_child(tblpr, "w:tblW", **{"w:w": int(sum(widths) * 567), "w:type": "dxa"})
        for i, row in enumerate(rows):
            tr = t.rows[i]
            if i == 0:
                trpr = tr._tr.get_or_add_trPr()
                trpr.append(el("w:tblHeader"))
            cant = tr._tr.get_or_add_trPr()
            cant.append(el("w:cantSplit"))
            for j, value in enumerate(row):
                cell = tr.cells[j]
                cell.width = Cm(widths[j])
                p = cell.paragraphs[0]
                pf = p.paragraph_format
                pf.space_before, pf.space_after, pf.line_spacing = Pt(3), Pt(3), 1.15
                p.alignment = WD_ALIGN_PARAGRAPH.CENTER if (i == 0 or len(value) < 18) else WD_ALIGN_PARAGRAPH.LEFT
                self.runs(p, value)
                for r in p.runs:
                    r.font.name, r.font.size = "Times New Roman", Pt(13)
                    if i == 0:
                        r.bold = True
        # a little air after the table
        self.para("DoanVan").paragraph_format.space_after = Pt(0)

    def figure(self, path, caption, width):
        self.figures += 1
        p = self.para(align=WD_ALIGN_PARAGRAPH.CENTER)
        p.paragraph_format.keep_with_next = True
        p.paragraph_format.space_before = Pt(6)
        p.add_run().add_picture(os.path.normpath(os.path.join(HERE, path)), width=Cm(width))
        self.caption("Hình", caption)

    def sign(self, lines):
        first = True
        for line in lines.split("|"):
            p = self.para(align=WD_ALIGN_PARAGRAPH.CENTER)
            pf = p.paragraph_format
            pf.left_indent = Cm(8)
            pf.space_before = Pt(12) if first else Pt(0)
            pf.line_spacing = 1.5
            r = p.add_run(line)
            r.italic = line.startswith("Vĩnh Long") or line.startswith("(")
            r.bold = line in ("Sinh viên thực hiện", "Trần Hoàng Long", "Giảng viên hướng dẫn", "Giảng viên chấm")
            if line == "":
                for _ in range(2):
                    self.para().paragraph_format.left_indent = Cm(8)
            first = False

    def comment_page(self, title, signer):
        self.unnumbered(title, in_toc=False)
        for _ in range(16):
            p = self.para()
            p.paragraph_format.line_spacing = 1.5
            p.add_run("." * 132)
        self.sign(f"Vĩnh Long, ngày … tháng … năm 2026|{signer}|(Ký và ghi rõ họ tên)")

    def toc(self, title, instr):
        self.unnumbered(title, in_toc=title != "MỤC LỤC")
        add_field(self.para("DoanVan"), instr, "Nhấn F9 trong Word để cập nhật.")

    def abbrev(self, lines):
        rows = [["Chữ viết tắt", "Ý nghĩa"]] + [l.split("|", 1) for l in lines]
        t = self.doc.add_table(rows=len(rows), cols=2)
        self.sect.addprevious(t._tbl)
        t.style = self.doc.styles["Table Grid"]
        t.alignment = 1
        for i, row in enumerate(rows):
            for j, value in enumerate(row):
                cell = t.rows[i].cells[j]
                cell.width = Cm(3.5 if j == 0 else 13)
                p = cell.paragraphs[0]
                p.paragraph_format.space_before = p.paragraph_format.space_after = Pt(3)
                p.paragraph_format.line_spacing = 1.15
                r = p.add_run(value)
                r.font.name, r.font.size = "Times New Roman", Pt(13)
                r.bold = i == 0 or j == 0
                if i == 0:
                    p.alignment = WD_ALIGN_PARAGRAPH.CENTER

    def references(self, title):
        self.unnumbered(title)
        order = sorted(REFS, key=lambda k: REFS[k][0].casefold())
        unused = [k for k in order if k not in self.cited]
        if unused:
            print("not cited, left out:", ", ".join(unused))
        numbers = {k: i for i, k in enumerate([k for k in order if k in self.cited], start=1)}
        for key, n in numbers.items():
            p = self.para("DoanVan")
            pf = p.paragraph_format
            pf.first_line_indent = Cm(-1.0)
            pf.left_indent = Cm(1.0)
            pf.alignment = WD_ALIGN_PARAGRAPH.LEFT
            p.add_run(f"[{n}]\t")
            for chunk in re.split(r"(\*[^*]+\*)", REFS[key][1]):
                if chunk:
                    r = p.add_run(chunk.strip("*") if chunk.startswith("*") else chunk)
                    r.italic = chunk.startswith("*")
        return numbers

    def todo(self, text):
        p = self.para("DoanVan")
        r = p.add_run(text)
        r.italic = True


def build():
    doc = docx.Document(TEMPLATE)
    setup_styles(doc)
    fill_covers(doc)
    fill_footer(doc)
    clear_body(doc)
    b = Builder(doc)

    lines = open(os.path.join(HERE, "noidung.md"), encoding="utf-8").read().splitlines()
    i = 0
    refs_numbers = None
    while i < len(lines):
        line = lines[i].rstrip()
        i += 1
        if not line:
            continue
        if line.startswith("#! "):
            b.unnumbered(line[3:])
        elif line.startswith("### "):
            b.section(line[4:], 3)
        elif line.startswith("## "):
            b.section(line[3:], 2)
        elif line.startswith("# "):
            b.chapter_title(line[2:])
        elif line.startswith("- "):
            b.text(line[2:], "GachDau")
        elif line.startswith("|table|"):
            caption, widths = [s.strip() for s in line[len("|table|"):].rsplit("|", 1)]
            rows = []
            while lines[i].strip() != "|end|":
                rows.append([c.strip() for c in lines[i].split("|")])
                i += 1
            i += 1
            b.table(caption, [float(w) for w in widths.split(",")], rows)
        elif line.startswith("!fig "):
            path, caption, width = [s.strip() for s in line[5:].split(" | ")]
            b.figure(path, caption, float(width))
        elif line.startswith("@sign "):
            b.sign(line[6:])
        elif line.startswith("@nhanxet "):
            title, signer = line[9:].split("|")
            b.comment_page(title, signer)
        elif line.startswith("@toc "):
            b.toc(line[5:], 'TOC \\o "1-3" \\h \\z \\u')
        elif line.startswith("@lot "):
            b.toc(line[5:], 'TOC \\h \\z \\c "Bảng"')
        elif line.startswith("@lof "):
            b.toc(line[5:], 'TOC \\h \\z \\c "Hình"')
        elif line == "@abbrev":
            block = []
            while lines[i].strip() != "@end":
                block.append(lines[i].strip())
                i += 1
            i += 1
            b.abbrev(block)
        elif line.startswith("@refs "):
            refs_numbers = b.references(line[6:])
        elif line.startswith("@todo "):
            b.todo(line[6:])
        else:
            b.text(line)

    # citation keys -> numbers (the list is only complete at the end)
    for t in doc.element.body.iter(qn("w:t")):
        if t.text and "" in t.text:
            t.text = re.sub(r"([^]+)", lambda m: str(refs_numbers[m.group(1)]), t.text)

    doc.core_properties.title = TITLE.capitalize()
    doc.core_properties.author = "Trần Hoàng Long"
    doc.save(OUT)
    print("wrote", OUT, f"({b.tables} tables, {b.figures} figures, {len(refs_numbers)} references)")


if __name__ == "__main__":
    build()
