"""Builds the capstone report from the faculty template and noidung.md.

    python -I thesis/doc/report-src/build_report.py            (repo root; needs python-docx)
    powershell -File thesis/doc/report-src/update_fields.ps1   (Word fills the table of contents, lists and numbers)

Keeps the template's two cover pages (section 1) with their logo, fills the title and student, and replaces the rest
with the content of noidung.md. Faculty rules (0_MOT_SO_QUY_DINH_CHUNG_2026.doc): Times New Roman 13 pt, 1.5 lines,
6 pt before and after, margins 2/2/3/1.5 cm, page number bottom right, chapters and sections numbered 1, 1.1, 1.1.1
(the template's Heading 1-3 list "CHƯƠNG %1:"), IEEE references numbered in dictionary order. Tables are captioned
above, figures below, numbered per chapter with SEQ fields so Word builds the lists of tables and figures.

Markup of noidung.md: "# X" chapter, "#! X" unnumbered title in the table of contents, "## " / "### " sections,
"- " bullet, "|table| caption | widths" ... "|end|" (first row is the header; the widths only set the table width and
a starting point: table_widths() moves width between columns so no cell ends on a line of one word),
"!fig path | caption | width [| ngang]" (a figure from thesis/diagrams fills the text width or the page height,
whichever comes first, whatever the width says; "ngang" puts it alone on a landscape page),
"[@key]" or "[@key, tr. 5]" citations (refs.py), *italic*, and the @ commands handled in build(). Tables and figures
after "#! PHỤ LỤC" are numbered PL.1, PL.2, … (a SEQ field restarted at that number, still in the lists of tables
and figures).
"""
import copy
import os
import re
import sys

import docx
import pymupdf
from docx.enum.section import WD_ORIENT
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
FONTS = {bold: pymupdf.Font(fontfile=os.path.join(os.environ.get("WINDIR", r"C:\Windows"), "Fonts", name))
         for bold, name in ((False, "times.ttf"), (True, "timesbd.ttf"))}
TEXT_WIDTH_TWIPS = 9355  # A4 21 cm - 3 cm - 1.5 cm
TEXT_WIDTH_CM, TEXT_HEIGHT_CM = 16.5, 25.7  # portrait text area; landscape is 25.2 x 17
CAPTION_ROOM_CM = 2.5  # a caption of up to two lines under a figure that fills the page
CELL_PAD_CM = 0.38  # left and right cell margins of Table Grid (0.19 cm each)


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
    # the template's chapter titles ("CHƯƠNG 1: TỔNG QUAN") are centred bold 14 pt with no rule under them
    for old in p.findall(qn("w:pBdr")):
        p.remove(old)
    order_ppr(p)
    for name in ("Heading 1", "Heading 2"):
        for tag in ("w:sz", "w:szCs"):
            set_child(rpr(styles[name].element), tag, **{"w:val": 28})
    for name in ("Heading 2", "Heading 3"):
        p = ppr(styles[name].element)
        set_child(p, "w:spacing", **{"w:before": 240, "w:after": 120, "w:line": 360, "w:lineRule": "auto"})
        set_child(p, "w:jc", **{"w:val": "both"})
        order_ppr(p)
    # Heading 3 stays as in the template: bold 13 pt, not italic

    # Unnumbered titles: DANHMUC goes into the table of contents, TieuDe (copy) does not.
    dm = styles["DANHMUC"].element
    p = ppr(dm)
    set_child(p, "w:pageBreakBefore")
    set_child(p, "w:spacing", **{"w:before": 0, "w:after": 360, "w:line": 360, "w:lineRule": "auto"})
    set_child(p, "w:ind", **{"w:left": 0, "w:firstLine": 0})
    set_child(p, "w:jc", **{"w:val": "center"})
    set_child(p, "w:outlineLvl", **{"w:val": 0})
    order_ppr(p)
    # unnumbered titles in the template are bold 14 pt (LỜI CAM ĐOAN, MỤC LỤC, TÓM TẮT ĐỒ ÁN, ...)
    for tag in ("w:sz", "w:szCs"):
        set_child(rpr(dm), tag, **{"w:val": 28})
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

    # links (URLs in the references) are black like all text
    link = doc.styles["Hyperlink"].element
    set_child(rpr(link), "w:color", **{"w:val": "000000"})

    add_bullet_style(doc)


_WIDTHS = {}


def fix_widths(table, widths):
    """Fixed layout with the grid set: Word otherwise autofits the columns to their text and the widths chosen by
    table_widths() are lost."""
    table.autofit = False
    for col, w in zip(table.columns, widths):
        col.width = Cm(w)


def text_pt(text, bold=False):
    """Width in points of text in 13 pt Times New Roman, measured with the font file Word uses."""
    key = (text, bold)
    if key not in _WIDTHS:
        _WIDTHS[key] = FONTS[bold].text_length(text, fontsize=13)
    return _WIDTHS[key]


def words_of(text):
    return [w for w in re.split("[ \t\n\u200b]+", text.strip()) if w]  # no break at the no-break spaces of citations


def cell_lines(text, width_cm, bold=False):
    """Lines a cell takes and words on its last line, breaking between words as Word does."""
    avail = (width_cm - CELL_PAD_CM) / 2.54 * 72
    space = text_pt(" ", bold)
    lines, current, last = 0, None, 0
    for word in words_of(text):
        # Word may also break after a hyphen inside a word ("đầu-cuối")
        for k, part in enumerate(re.split(r"(?<=-)(?=.)", word)):
            w = text_pt(part, bold)
            gap = space if k == 0 else 0
            if current is not None and current + gap + w > avail:
                lines, current, last = lines + 1, w, 1
            else:
                current, last = (w if current is None else current + gap + w), (last + 1 if k == 0 else last)
    return lines + (current is not None), last


def place_breaks(text, width_cm, bold=False):
    """Turns the break marks (U+200B after "/") into a line break where Word would otherwise cut the path mid-word,
    and drops the others: no invisible character is left in the document."""
    if "\u200b" not in text:
        return text
    avail = (width_cm - CELL_PAD_CM) / 2.54 * 72
    space = text_pt(" ", bold)
    out, current = [], None
    for wi, word in enumerate(text.split(" ")):
        if wi:
            out.append(" ")
        for k, zpart in enumerate(word.split("\u200b")):
            for h, part in enumerate(re.split(r"(?<=-)(?=.)", zpart)):
                kind = "space" if k == h == 0 else ("mark" if h == 0 else "hyphen")
                w = text_pt(part, bold)
                gap = space if kind == "space" else 0
                if current is not None and current + gap + w > avail:
                    if kind == "mark":
                        out.append("\n")
                    current = w
                else:
                    current = w if current is None else current + gap + w
                out.append(part)
    return "".join(out)


def table_widths(rows, widths):
    """Same total width; chooses the column widths (1 mm steps) with the fewest cells whose last line is one word, then
    the lowest table. Never narrower than a column's longest word, nor than a header of one or two words."""
    total = sum(widths)
    plain = [[re.sub(r"\[@[^\]]+\]", lambda m: re.sub(r"@[\w-]+", "12", m.group()).replace(" ", " "),
                     c.replace("*", "")) for c in r] for r in rows]  # [@key, tr. 5] -> [12, tr. 5], unbreakable

    def floor_of(j):
        need = max(text_pt(w, i == 0) for i, r in enumerate(plain) for w in words_of(r[j]) or [""])
        if len(words_of(plain[0][j])) <= 2:
            need = max(need, text_pt(plain[0][j].strip(), True))
        return need / 72 * 2.54 + CELL_PAD_CM + 0.1

    floor = [floor_of(j) for j in range(len(widths))]

    def cost(ws):
        orphans = height = 0
        for r in plain:
            row = []
            for j, c in enumerate(r):
                bold = r is plain[0]
                n, last = cell_lines(c, ws[j], bold)
                # a line within a few points of the edge may break either way in Word: test a little narrower and wider
                if len(c.split()) >= 3:
                    orphans += any(k >= 2 and l == 1 for k, l in
                                   (cell_lines(c, ws[j] + d * (ws[j] - CELL_PAD_CM), bold) for d in (-0.02, 0, 0.01)))
                row.append(n)
            height += max(row)
        return orphans * 1000 + height

    # start from the given widths, scaled so every column keeps at least its floor
    extra = [max(w - f, 0.01) for w, f in zip(widths, floor)]
    room = max(total - sum(floor), 0)
    ws = [round(f + e * room / sum(extra), 1) for f, e in zip(floor, extra)]
    ws[-1] = round(total - sum(ws[:-1]), 1)
    best = start = cost(ws)
    n = len(ws)
    if n <= 3:
        # few columns: try every split in 1 mm steps
        steps = int(round(total * 10))
        lo = [int(f * 10 + 0.999) for f in floor]
        for a in range(lo[0], steps + 1):
            for b in (range(lo[1], steps - a + 1) if n == 3 else [steps - a]):
                trial = [a / 10, b / 10] + ([(steps - a - b) / 10] if n == 3 else [])
                if all(t >= f for t, f in zip(trial, floor)):
                    c = cost(trial)
                    if c < best:
                        best, ws = c, trial
    else:
        for _ in range(400):
            move = None
            for step in (0.1, 0.3, 0.6, 1.0, 2.0):
                for a in range(n):
                    for b in range(n):
                        if a == b or ws[a] - step < floor[a]:
                            continue
                        trial = ws[:]
                        trial[a] -= step
                        trial[b] += step
                        c = cost(trial)
                        if c < best:
                            best, move = c, trial
            if move is None:
                break
            ws = move
    if best >= 1000:
        print(f"  table {rows[0][0]!r}: {best // 1000} cells may end on one word (was {start // 1000})")
        for i, r in enumerate(plain):
            for j, c in enumerate(r):
                if len(c.split()) >= 3 and any(k >= 2 and l == 1 for k, l in (
                        cell_lines(c, ws[j] + d * (ws[j] - CELL_PAD_CM), i == 0) for d in (-0.02, 0, 0.01))):
                    print(f"    {ws[j]:.1f} cm: {c}")
    return ws


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
    lvl.append(el("w:lvlText", **{"w:val": "-"}))  # a short hyphen: "–" reads as a long dash
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
            p.runs[0].font.size = Pt(20)  # as the template; a long title wraps onto centred lines
        if text in STUDENT:
            r = p.add_run(" " + STUDENT[text])
            r.font.size, r.font.name = p.runs[0].font.size, "Times New Roman"


def landscape_footers(doc):
    """Landscape sections get a footer of their own: the template's text, with its tab stops and the framed page number
    moved out to the wider page (the template places them at fixed positions for a portrait page)."""
    portrait = doc.sections[1]
    main = portrait.footer._element
    for sec in doc.sections:
        if sec.orientation != WD_ORIENT.LANDSCAPE:
            continue
        for ref in sec._sectPr.findall(qn("w:footerReference")):
            sec._sectPr.remove(ref)
        sec.footer.is_linked_to_previous = False  # a new, empty footer part for this section
        ftr = sec.footer._element
        for k in list(ftr):
            ftr.remove(k)
        for k in main:
            ftr.append(copy.deepcopy(k))
        widen = sec.page_width.twips - portrait.page_width.twips
        scale = (sec.page_width.twips - sec.left_margin.twips - sec.right_margin.twips) /                 (portrait.page_width.twips - portrait.left_margin.twips - portrait.right_margin.twips)
        for frame in ftr.iter(qn("w:framePr")):
            frame.set(qn("w:x"), str(int(frame.get(qn("w:x"))) + widen))
        for p in ftr.findall(qn("w:p")):
            if p.find(".//" + qn("w:tab")) is None or p.find(qn("w:pPr")) is None:
                continue
            tabs = set_child(p.find(qn("w:pPr")), "w:tabs")
            for kind, pos in (("center", 4320), ("right", 8640)):  # the Footer style's stops
                tabs.append(el("w:tab", **{"w:val": "clear", "w:pos": pos}))
                tabs.append(el("w:tab", **{"w:val": kind, "w:pos": int(pos * scale)}))
            order_ppr(p.find(qn("w:pPr")))


def fill_footer(doc):
    footer = doc.sections[1].footer
    for p in footer.paragraphs:
        if "GVHD" in p.text:
            runs = p.runs
            runs[0].text = "GVHD: ThS. Nhan Minh Phúc"
            runs[-1].text = "\tSVTH: Trần Hoàng Long"  # two tabs as in the template: SVTH sits at the right tab
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
        return ("[" + "; ".join(parts) + "]").replace(" ", " ")  # a citation never breaks across lines

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
        # long paths may wrap after "/" (marked here, a line break in the document) instead of being cut mid-word
        rows = [[re.sub(r"\S{21,}", lambda m: m.group().replace("/", "/\u200b"), c) for c in r] for r in rows]
        widths = table_widths(rows, widths)
        # a column is centred only when all its cells are short (numbers, codes), never half and half
        short = [all(len(r[j]) < 18 for r in rows[1:]) for j in range(len(rows[0]))]
        self.tables += 1
        self.caption("Bảng", caption)
        t = self.doc.add_table(rows=len(rows), cols=len(rows[0]))
        self.sect.addprevious(t._tbl)
        t.style = self.doc.styles["Table Grid"]
        t.alignment = 1
        tblpr = t._tbl.tblPr
        set_child(tblpr, "w:tblW", **{"w:w": int(sum(widths) * 567), "w:type": "dxa"})
        fix_widths(t, widths)
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
                p.alignment = WD_ALIGN_PARAGRAPH.CENTER if (i == 0 or short[j]) else WD_ALIGN_PARAGRAPH.LEFT
                self.runs(p, place_breaks(value, widths[j], i == 0))
                for r in p.runs:
                    r.font.name, r.font.size = "Times New Roman", Pt(13)
                    if i == 0:
                        r.bold = True
        # a little air after the table
        self.para("DoanVan").paragraph_format.space_after = Pt(0)

    def section_break(self, landscape):
        """Ends the section running so far here (its orientation is the opposite of the one starting) and starts a
        landscape or portrait one. The first break keeps the body's page-number restart; later sections continue it."""
        sect = copy.deepcopy(self.sect)
        restart = self.sect.find(qn("w:pgNumType"))
        if restart is not None and restart.get(qn("w:start")) is not None:
            del restart.attrib[qn("w:start")]  # only the first body section restarts at 1, and it keeps its copy
        elif sect.find(qn("w:pgNumType")) is not None:
            sect.find(qn("w:pgNumType")).attrib.pop(qn("w:start"), None)
        if not landscape:  # the section that ends is the landscape one
            size = sect.find(qn("w:pgSz"))
            w, h = size.get(qn("w:w")), size.get(qn("w:h"))
            size.set(qn("w:w"), h)
            size.set(qn("w:h"), w)
            size.set(qn("w:orient"), "landscape")
        p = self.para()
        pf = p.paragraph_format
        pf.space_before = pf.space_after = Pt(0)
        p._p.get_or_add_pPr().append(sect)

    def figure(self, path, caption, width, landscape=False):
        self.figures += 1
        full = os.path.normpath(os.path.join(HERE, path))
        if landscape:
            self.section_break(landscape=True)
        if landscape or "diagrams" in path:
            from docx.image.image import Image
            img = Image.from_file(full)
            text_w, text_h = (25.2, 17.0) if landscape else (TEXT_WIDTH_CM, TEXT_HEIGHT_CM)
            width = min(text_w, (text_h - CAPTION_ROOM_CM) * img.px_width / img.px_height)
        p = self.para(align=WD_ALIGN_PARAGRAPH.CENTER)
        p.paragraph_format.keep_with_next = True
        p.paragraph_format.space_before = Pt(6)
        p.add_run().add_picture(full, width=Cm(width))
        self.caption("Hình", caption)
        if landscape:
            self.section_break(landscape=False)

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
        """As the template: the centred title, two blank lines, 33 dotted writing lines (a tab with a dot leader to the
        right margin), a blank line, then the signer in bold and "(Ký và ghi rõ họ tên)" on the right."""
        self.unnumbered(title, in_toc=False)
        for n in range(36):
            p = self.para(align=WD_ALIGN_PARAGRAPH.JUSTIFY)
            pf = p.paragraph_format
            pf.space_before, pf.space_after, pf.line_spacing, pf.first_line_indent = Pt(0), Pt(0), Pt(16), Cm(0)  # single line of 14 pt, as the template
            if 2 <= n < 35:
                tabs = set_child(ppr(p._p), "w:tabs")
                tabs.append(el("w:tab", **{"w:val": "right", "w:leader": "dot", "w:pos": TEXT_WIDTH_TWIPS}))
                order_ppr(p._p.pPr)
                p.add_run("\t")
        self.para(align=WD_ALIGN_PARAGRAPH.RIGHT).add_run(signer).bold = True
        self.para(align=WD_ALIGN_PARAGRAPH.RIGHT).add_run("(Ký và ghi rõ họ tên)")

    def toc(self, title, instr):
        self.unnumbered(title, in_toc=title != "MỤC LỤC")
        add_field(self.para("DoanVan"), instr, "Nhấn F9 trong Word để cập nhật.")

    def abbrev(self, lines):
        rows = [["Chữ viết tắt", "Ý nghĩa"]] + [l.split("|", 1) for l in lines]
        widths = table_widths(rows, [3.5, 13])
        t = self.doc.add_table(rows=len(rows), cols=2)
        self.sect.addprevious(t._tbl)
        t.style = self.doc.styles["Table Grid"]
        t.alignment = 1
        fix_widths(t, widths)
        for i, row in enumerate(rows):
            for j, value in enumerate(row):
                cell = t.rows[i].cells[j]
                cell.width = Cm(widths[j])
                p = cell.paragraphs[0]
                p.paragraph_format.space_before = p.paragraph_format.space_after = Pt(3)
                p.paragraph_format.line_spacing = 1.15
                r = p.add_run(value)
                r.font.name, r.font.size = "Times New Roman", Pt(13)
                r.bold = i == 0
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
    sys.stdout.reconfigure(encoding="utf-8")
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
            path, caption, width, *flags = [s.strip() for s in line[5:].split(" | ")]
            b.figure(path, caption, float(width), landscape="ngang" in flags)
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

    landscape_footers(doc)

    # all text black (the template leaves a few coloured runs)
    for part in [doc.element.body] + [s.footer._element for s in doc.sections]:
        for color in part.iter(qn("w:color")):
            for attr in ("w:themeColor", "w:themeShade", "w:themeTint"):
                color.attrib.pop(qn(attr), None)
            color.set(qn("w:val"), "000000")

    doc.core_properties.title = TITLE.capitalize()
    doc.core_properties.author = "Trần Hoàng Long"
    doc.save(OUT)
    print("wrote", OUT, f"({b.tables} tables, {b.figures} figures, {len(refs_numbers)} references)")


if __name__ == "__main__":
    build()
