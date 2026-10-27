"""Generate the ERDs in thesis/diagrams from the live MySQL schema.

Usage (repo root, MySQL container running):  python -I thesis/diagrams/tools/gen_erd.py
Reads MYSQL_USER / MYSQL_PASSWORD / MYSQL_DATABASE from docker/.env and queries information_schema inside the
examprep-mysql-1 container. Writes

    erd.drawio            overview: the four groups of tables and how they refer to each other
    erd-content.drawio    exam content (types, papers, parts, questions, answer keys, mixes, assets)
    erd-exams.drawio      exam sessions (exams, sections, candidates, staff assigned, messages)
    erd-delivery.drawio   delivery and monitoring (attempts, responses, marks, notes, actions, warnings)
    erd-accounts.drawio   staff accounts and settings (users, areas, approvals, centre settings)

then export each PNG with draw.io (see CLAUDE.md).

Only foreign keys inside a group are drawn; a key to a table of another group is written after the column
("→ exams"). Every line leaves a table on the side facing its target and turns once in its own vertical lane of the
corridor between two columns; the lanes of a corridor are ordered so that as few lines as possible cross (all
orders are tried), so no two lines share a segment.
"""
import itertools
import os
import subprocess
from collections import defaultdict
from xml.sax.saxutils import escape

TOOLS = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.dirname(TOOLS)
REPO = os.path.dirname(os.path.dirname(OUT))
CONTAINER = "examprep-mysql-1"

QUERIES = {
    "columns": "SELECT table_name, column_name, column_type, column_key, is_nullable FROM information_schema.columns "
               "WHERE table_schema = DATABASE() AND table_name <> '__EFMigrationsHistory' ORDER BY table_name, ordinal_position",
    "fks": "SELECT table_name, column_name, referenced_table_name FROM information_schema.key_column_usage "
           "WHERE table_schema = DATABASE() AND referenced_table_name IS NOT NULL ORDER BY table_name",
    "uniques": "SELECT DISTINCT table_name, column_name FROM information_schema.statistics "
               "WHERE table_schema = DATABASE() AND non_unique = 0 AND index_name <> 'PRIMARY'",
}

# group: (title, colour fill / stroke, columns of tables, side of the loop for keys inside one column)
GROUPS = {
    "content": ("Nội dung đề thi", ("#dae8fc", "#6c8ebf"), [
        ["exam_types", "section_templates"],
        ["test_papers", "paper_mixes"],
        ["paper_parts", "assets"],
        ["question_groups", "questions", "answer_keys"],
    ]),
    "exams": ("Kỳ thi và thí sinh", ("#e1d5e7", "#9673a6"), [
        ["exam_sections", "exam_invigilators", "exam_examiners"],
        ["exams"],
        ["candidates", "candidate_messages"],
    ]),
    "delivery": ("Làm bài, chấm điểm và giám sát", ("#d5e8d4", "#82b366"), [
        ["invigilator_actions", "security_events"],
        ["attempts"],
        ["responses", "annotations"],
        ["manual_marks"],
    ]),
    "accounts": ("Tài khoản nhân sự và cấu hình", ("#fff2cc", "#d6b656"), [
        ["users"],
        ["user_areas", "approval_requests", "site_settings"],
    ]),
}

ROW, HEAD, COL_GAP, ROW_GAP, KEY_W = 22, 28, 190, 46, 52
# Keys inside one column normally loop through the corridor on the right of the column (outer left for the first
# column); these loop on the left instead, so two loops of one column never overlap.
LOOP_LEFT = {("answer_keys", "question_id")}
FONT = "fontFamily=Times New Roman;fontSize=12;"
EDGE = ("rounded=0;html=1;startArrow=ERmany;startFill=0;endArrow=ERmandOne;endFill=0;startSize=10;endSize=10;"
        "strokeColor=#444444;")


def q(s):
    return escape(str(s), {'"': "&quot;", "\n": "&#xa;"})


def load_env() -> dict:
    env = {}
    with open(os.path.join(REPO, "docker", ".env"), encoding="utf-8") as f:
        for line in f:
            if "=" in line and not line.lstrip().startswith("#"):
                key, value = line.strip().split("=", 1)
                env[key] = value
    return env


def query(sql: str) -> list:
    env = load_env()
    result = subprocess.run(
        ["docker", "exec", "-e", f"MYSQL_PWD={env['MYSQL_PASSWORD']}", CONTAINER, "mysql", "-u", env["MYSQL_USER"],
         env["MYSQL_DATABASE"], "-N", "-B", "-e", sql],
        capture_output=True, text=True, encoding="utf-8", check=True)
    return [line.split("\t") for line in result.stdout.splitlines() if line]


cols = defaultdict(list)
for table, col, ctype, ckey, nullable in query(QUERIES["columns"]):
    cols[table].append((col, ctype, ckey, nullable == "YES"))
fks = {(t, c): ref for t, c, ref in query(QUERIES["fks"])}
uniques = {(t, c) for t, c in query(QUERIES["uniques"])}
group_of = {t: g for g, (_, _, columns) in GROUPS.items() for column in columns for t in column}
assert set(group_of) == set(cols), f"layout mismatch: {set(cols) ^ set(group_of)}"


def label(table, col, ctype, ckey, nullable):
    keys = []
    if ckey == "PRI":
        keys.append("PK")
    if (table, col) in fks:
        keys.append("FK")
    if (table, col) in uniques and ckey != "PRI":
        keys.append("UQ")
    text = f"{col} : {ctype}" + ("" if nullable or ckey == "PRI" else " NN")
    ref = fks.get((table, col))
    if ref and group_of[ref] != group_of[table]:
        text += f"  → {ref}"
    return ",".join(keys), text, ckey == "PRI"


def width(table):
    longest = max(len(label(table, *c)[1]) for c in cols[table])
    return max(230, int(round((KEY_W + 18 + longest * 6.4) / 10.0)) * 10)


class Doc:
    def __init__(self):
        self.cells, self.n = [], 1

    def add(self, xml):
        self.cells.append(xml)

    def nid(self, p="c"):
        self.n += 1
        return f"{p}{self.n}"

    def vertex(self, value, style, x, y, w, h, parent="1", cid=None, extra=""):
        cid = cid or self.nid()
        self.add(f'<mxCell id="{cid}" value="{q(value)}" style="{style}" vertex="1" parent="{parent}">'
                 f'<mxGeometry x="{x}" y="{y}" width="{w}" height="{h}" as="geometry">{extra}</mxGeometry></mxCell>')
        return cid

    def path(self, style, points, value=""):
        (sx, sy), (tx, ty) = points[0], points[-1]
        middle = "".join(f'<mxPoint x="{x}" y="{y}"/>' for x, y in points[1:-1])
        self.add(f'<mxCell id="{self.nid("e")}" value="{q(value)}" style="{style}" edge="1" parent="1">'
                 f'<mxGeometry relative="1" as="geometry"><mxPoint x="{sx}" y="{sy}" as="sourcePoint"/>'
                 f'<mxPoint x="{tx}" y="{ty}" as="targetPoint"/><Array as="points">{middle}</Array></mxGeometry></mxCell>')

    def save(self, name, title):
        with open(os.path.join(OUT, name), "w", encoding="utf-8") as f:
            f.write(f'<?xml version="1.0" encoding="UTF-8"?>\n<mxfile host="gen_erd.py"><diagram name="{q(title)}">'
                    '<mxGraphModel grid="0" page="0"><root><mxCell id="0"/><mxCell id="1" parent="0"/>'
                    + "".join(self.cells) + "</root></mxGraphModel></diagram></mxfile>\n")


def table_box(d, t, x, y, w, colours):
    fill, stroke = colours
    tid = d.vertex(t, "shape=table;startSize=28;container=1;collapsible=0;childLayout=tableLayout;fixedRows=1;rowLines=0;"
                   f"fontStyle=1;align=center;resizeLast=1;html=1;{FONT}fillColor={fill};strokeColor={stroke};",
                   x, y, w, HEAD + ROW * len(cols[t]), cid=f"t_{t}")
    for i, (c, ctype, ckey, nullable) in enumerate(cols[t]):
        keys, text, is_pk = label(t, c, ctype, ckey, nullable)
        rid = f"r_{t}_{c}"
        d.vertex("", "shape=tableRow;horizontal=0;startSize=0;swimlaneHead=0;swimlaneBody=0;fillColor=none;collapsible=0;"
                 "dropTarget=0;portConstraint=eastwest;top=0;left=0;right=0;bottom=0;html=1;",
                 0, HEAD + i * ROW, w, ROW, parent=tid, cid=rid)
        d.vertex(keys, "shape=partialRectangle;connectable=0;fillColor=none;top=0;left=0;bottom=0;right=0;fontStyle=1;"
                 f"overflow=hidden;whiteSpace=wrap;html=1;{FONT}fontSize=10;", 0, 0, KEY_W, ROW, parent=rid)
        style = "fontStyle=5;" if is_pk else ""
        colour = "fontColor=#555555;" if "→" in text else ""
        d.vertex(text, "shape=partialRectangle;connectable=0;fillColor=none;top=0;left=0;bottom=0;right=0;align=left;"
                 f"spacingLeft=6;overflow=hidden;whiteSpace=wrap;html=1;{FONT}{style}{colour}", KEY_W, 0, w - KEY_W, ROW,
                 parent=rid)


def crossings(order, edges):
    """Lines that cross when the edges of one corridor take the lanes in this order (lane 0 nearest the left)."""
    lane = {id(e): i for i, e in enumerate(order)}
    count = 0
    for a, b in itertools.combinations(edges, 2):
        for e, f in ((a, b), (b, a)):
            # e's horizontal stubs run from the corridor sides to e's lane; f's vertical segment sits at f's lane.
            low, high = sorted((f["y1"], f["y2"]))
            for side, y in ((e["side1"], e["y1"]), (e["side2"], e["y2"])):
                between = lane[id(f)] < lane[id(e)] if side == "L" else lane[id(f)] > lane[id(e)]
                if between and low < y < high:
                    count += 1
    return count


def group_diagram(key):
    title, colours, columns = GROUPS[key]
    d = Doc()
    d.vertex(f"ERD: {title}", f"text;html=1;align=left;verticalAlign=middle;{FONT}fontSize=16;fontStyle=1;", 40, 0, 600, 30)

    # place the tables: columns left to right, tables top-down
    pos, col_x, col_w = {}, [], []
    x = 100               # room for loops on the far left
    for column in columns:
        w = max(width(t) for t in column)
        y = 50
        for t in column:
            pos[t] = (x, y, w, len(col_x))
            table_box(d, t, x, y, w, colours)
            y += HEAD + ROW * len(cols[t]) + ROW_GAP
        col_x.append(x)
        col_w.append(w)
        x += w + COL_GAP

    def row_y(t, c):
        return pos[t][1] + HEAD + ROW * [n for n, *_ in cols[t]].index(c) + ROW / 2

    # every foreign key inside the group, assigned to a corridor (0 = far left, i = between column i-1 and i)
    corridors = defaultdict(list)
    for (t, c), ref in sorted(fks.items()):
        if group_of.get(ref) != key or group_of[t] != key:
            continue
        pk = next(n for n, _, k, _ in cols[ref] if k == "PRI")
        ci, cr = pos[t][3], pos[ref][3]
        assert abs(ci - cr) <= 1, f"{t}.{c} -> {ref}: tables must sit in the same or neighbouring columns"
        if ci != cr:
            corridor = max(ci, cr)
        elif (t, c) in LOOP_LEFT:
            corridor = ci
        elif ci == 0 and len(columns) > 1:
            corridor = 0                 # first column: loop on the outer left
        else:
            corridor = ci + 1            # otherwise: loop through the corridor on its right
        left_col = corridor - 1          # the column on the corridor's left; "L" = the line comes from that side
        e = {"t": t, "c": c, "ref": ref, "pk": pk, "y1": row_y(t, c), "y2": row_y(ref, pk),
             "side1": "L" if ci == left_col else "R", "side2": "L" if cr == left_col else "R"}
        e["x1"], e["x2"] = ci, cr
        corridors[corridor].append(e)

    # Lines that leave or reach the same row into the same corridor are spread a few pixels apart, ordered by where
    # their other end lies, so they run side by side instead of on top of each other.
    ends = defaultdict(list)
    for corridor, edges in corridors.items():
        for e in edges:
            ends[(e["t"], e["c"], corridor)].append((e, "y1", e["y2"]))
            ends[(e["ref"], e["pk"], corridor)].append((e, "y2", e["y1"]))
    for items in ends.values():
        if len(items) > 1:
            items.sort(key=lambda item: item[2])
            for i, (e, field, _) in enumerate(items):
                e[field] += 7 * i - 7 * (len(items) - 1) / 2

    for corridor, edges in corridors.items():
        best = min(itertools.permutations(edges), key=lambda o: crossings(o, edges)) if len(edges) <= 7 else edges
        if corridor == 0:                                  # outer left: lanes step leftwards from the tables
            right_edge = col_x[0]
            left_edge = right_edge - 16 * (len(edges) + 1)
        elif corridor == len(columns):                     # outer right
            left_edge = col_x[-1] + col_w[-1]
            right_edge = left_edge + 16 * (len(edges) + 1)
        else:
            left_edge, right_edge = col_x[corridor - 1] + col_w[corridor - 1], col_x[corridor]
        step = (right_edge - left_edge) / (len(edges) + 1)
        for i, e in enumerate(best):
            lane = left_edge + step * (i + 1)
            x1 = left_edge if e["side1"] == "L" else right_edge
            x2 = left_edge if e["side2"] == "L" else right_edge
            d.path(EDGE, [(x1, e["y1"]), (lane, e["y1"]), (lane, e["y2"]), (x2, e["y2"])])

    bottom = max(p[1] + HEAD + ROW * len(cols[t]) for t, p in pos.items())
    d.vertex("PK khoá chính · FK khoá ngoại · UQ duy nhất · NN bắt buộc (NOT NULL) · “→ bảng”: khoá ngoại sang nhóm khác "
             "· chân gà: nhiều, hai vạch: đúng một",
             f"text;html=1;align=left;verticalAlign=middle;{FONT}fontColor=#444444;", 40, bottom + 30, 1000, 24)
    d.save(f"erd-{key}.drawio", f"ERD {title}")
    return len(cols), sum(len(v) for k, v in corridors.items())


def overview():
    """The four groups with their tables, and one arrow per pair of groups that refer to each other: content on the
    left, sessions and delivery on the right, accounts as a band underneath, so every arrow is straight or turns once
    outside the boxes and none crosses another."""
    d = Doc()
    d.vertex("Lược đồ cơ sở dữ liệu: 25 bảng trong 4 nhóm", f"text;html=1;align=left;verticalAlign=middle;{FONT}fontSize=16;fontStyle=1;", 40, 0, 700, 30)
    LINE, W = 26, 380

    def box(key, x, y, w=W, across=False):
        title, (fill, stroke), columns = GROUPS[key]
        tables = [t for col in columns for t in col]
        h = 44 + LINE * (1 if across else len(tables))
        gid = d.vertex(title, f"swimlane;html=1;startSize=34;{FONT}fontSize=15;fontStyle=1;fillColor={fill};strokeColor={stroke};rounded=1;",
                       x, y, w, h + 6)
        if across:
            d.vertex("   ·   ".join(f"{t} ({len(cols[t])} cột)" for t in tables), f"text;html=1;align=center;verticalAlign=middle;{FONT}fontSize=14;",
                     0, 38, w, LINE, parent=gid)
        else:
            for i, t in enumerate(tables):
                d.vertex(f"{t}  ({len(cols[t])} cột)", f"text;html=1;align=left;verticalAlign=middle;{FONT}fontSize=14;spacingLeft=12;",
                         0, 38 + LINE * i, w, LINE, parent=gid)
        return x, y, w, h + 6

    content = box("content", 40, 60)
    exams = box("exams", 700, 60)
    delivery = box("delivery", 700, exams[1] + exams[3] + 70)
    band_y = max(content[1] + content[3], delivery[1] + delivery[3]) + 90
    accounts = box("accounts", 40, band_y, w=1040, across=True)

    links = defaultdict(set)
    for (t, c), ref in fks.items():
        a, b = group_of[t], group_of[ref]
        if a != b:
            links[(a, b)].add(f"{t}.{c}")
    style = (f"endArrow=block;endFill=1;html=1;rounded=0;strokeColor=#444444;{FONT}fontSize=12;labelBackgroundColor=#ffffff;")
    cx, cy, cw, ch = content
    ex, ey, ew, eh = exams
    dx, dy, dw, dh = delivery
    ax, ay, aw, ah = accounts
    routes = {
        ("exams", "content"): [(ex, ey + 70), (cx + cw, ey + 70)],
        ("delivery", "content"): [(dx, dy + 60), (cx + cw + 90, dy + 60), (cx + cw + 90, cy + ch - 40), (cx + cw, cy + ch - 40)],
        ("delivery", "exams"): [(dx + dw / 2, dy), (dx + dw / 2, ey + eh)],
        ("content", "accounts"): [(cx + cw * 0.65, cy + ch), (cx + cw * 0.65, ay)],
        ("accounts", "content"): [(cx + cw * 0.3, ay), (cx + cw * 0.3, cy + ch)],
        ("delivery", "accounts"): [(dx + dw / 2, dy + dh), (dx + dw / 2, ay)],
        ("exams", "accounts"): [(ex + ew, ey + 60), (ex + ew + 50, ey + 60), (ex + ew + 50, ay + ah / 2), (ax + aw, ay + ah / 2)],
    }
    for pair, columns_ in sorted(links.items()):
        d.path(style, routes[pair], f"{len(columns_)} khoá ngoại")
    d.vertex("Mũi tên: nhóm chứa khoá ngoại → nhóm được tham chiếu (số khoá ngoại). Chi tiết từng nhóm ở các sơ đồ erd-*.",
             f"text;html=1;align=left;verticalAlign=middle;{FONT}fontColor=#444444;", 40, ay + ah + 20, 1000, 24)
    d.save("erd.drawio", "ERD tổng quan")
    return links


if __name__ == "__main__":
    for key in GROUPS:
        group_diagram(key)
        print("wrote", f"erd-{key}.drawio")
    links = overview()
    print("wrote erd.drawio;", {f"{a}->{b}": len(v) for (a, b), v in links.items()})
