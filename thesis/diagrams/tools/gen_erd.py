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
import random
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


def in_group(t, c, ref):
    """Group ERDs draw a key only when both tables are in the same group."""
    return group_of[ref] == group_of[t]


def label(table, col, ctype, ckey, nullable, drawn=in_group):
    keys = []
    if ckey == "PRI":
        keys.append("PK")
    if (table, col) in fks:
        keys.append("FK")
    if (table, col) in uniques and ckey != "PRI":
        keys.append("UQ")
    text = f"{col} : {ctype}" + ("" if nullable or ckey == "PRI" else " NN")
    ref = fks.get((table, col))
    if ref and not drawn(table, col, ref):
        text += f"  → {ref}"
    return ",".join(keys), text, ckey == "PRI"


def width(table, drawn=in_group):
    longest = max(len(label(table, *c, drawn=drawn)[1]) for c in cols[table])
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


def table_box(d, t, x, y, w, colours, drawn=in_group):
    fill, stroke = colours
    tid = d.vertex(t, "shape=table;startSize=28;container=1;collapsible=0;childLayout=tableLayout;fixedRows=1;rowLines=0;"
                   f"fontStyle=1;align=center;resizeLast=1;html=1;{FONT}fillColor={fill};strokeColor={stroke};",
                   x, y, w, HEAD + ROW * len(cols[t]), cid=f"t_{t}")
    for i, (c, ctype, ckey, nullable) in enumerate(cols[t]):
        keys, text, is_pk = label(t, c, ctype, ckey, nullable, drawn=drawn)
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


def crossings(order, items):
    """Lines that cross when the items of one corridor take the lanes in this order (lane 0 nearest the left)."""
    lane = {id(e): i for i, e in enumerate(order)}
    count = 0
    for a, b in itertools.combinations(items, 2):
        for e, f in ((a, b), (b, a)):
            # e's horizontal stubs run from the corridor sides to e's lane; f's vertical segment sits at f's lane.
            low, high = sorted((f["y1"], f["y2"]))
            for side, y in ((e["side1"], e["y1"]), (e["side2"], e["y2"])):
                if side == "T":            # this end goes on up to a track above the tables: no stub here
                    continue
                between = lane[id(f)] < lane[id(e)] if side == "L" else lane[id(f)] > lane[id(e)]
                if between and low < y < high:
                    count += 1
    return count


def best_order(items):
    """Every order of the lanes for up to 7 lines; beyond that, swaps and moves from many random starts, keeping the
    order with the fewest crossings (deterministic: the random generator is seeded)."""
    if len(items) <= 7:
        return min(itertools.permutations(items), key=lambda o: crossings(o, items))
    rng = random.Random(7)
    best, best_cost = None, None
    for start in range(60):
        order = sorted(items, key=lambda e: min(e["y1"], e["y2"])) if start == 0 else rng.sample(items, len(items))
        cost = crossings(order, items)
        improved = True
        while improved:
            improved = False
            for i, j in itertools.permutations(range(len(order)), 2):
                trial = order[:]
                trial.insert(j, trial.pop(i))
                trial_cost = crossings(trial, items)
                if trial_cost < cost:
                    order, cost, improved = trial, trial_cost, True
        if best_cost is None or cost < best_cost:
            best, best_cost = order, cost
    return best


def draw_erd(title, filename, columns, colour_of, drawn, loop_left=frozenset(), note=""):
    """Tables in columns; every drawn key is one line. A key between the same or neighbouring columns turns once in a
    lane of the corridor between them; a key that skips columns climbs in a lane next to its table, runs along its
    own track above all tables and comes down in a lane next to the other table. No two lines share a lane or a
    track, and the lanes of each corridor are ordered for the fewest crossings."""
    keys = [((t, c), ref) for (t, c), ref in sorted(fks.items()) if t in colour_of and ref in colour_of and drawn(t, c, ref)]
    column_of = {t: i for i, column in enumerate(columns) for t in column}
    long_keys = [k for k in keys if abs(column_of[k[0][0]] - column_of[k[1]]) > 1]

    d = Doc()
    d.vertex(title, f"text;html=1;align=left;verticalAlign=middle;{FONT}fontSize=16;fontStyle=1;", 40, 0, 900, 30)
    top = 60 + 16 * len(long_keys)

    pos, col_x, col_w = {}, [], []
    x = 100               # room for loops on the far left
    for column in columns:
        w = max(width(t, drawn) for t in column)
        y = top
        for t in column:
            pos[t] = (x, y, w, len(col_x))
            table_box(d, t, x, y, w, colour_of[t], drawn)
            y += HEAD + ROW * len(cols[t]) + ROW_GAP
        col_x.append(x)
        col_w.append(w)
        x += w + COL_GAP

    def row_y(t, c):
        return pos[t][1] + HEAD + ROW * [n for n, *_ in cols[t]].index(c) + ROW / 2

    # A long key runs on a track above the tables when its rows are in the upper half, below them otherwise, so its
    # lanes cross as few rows as possible.
    table_bottom = max(p[1] + HEAD + ROW * len(cols[t]) for t, p in pos.items())
    middle = (top + table_bottom) / 2
    above, below = [], []
    for (t, c), ref in long_keys:
        pk = next(name for name, _, k, _ in cols[ref] if k == "PRI")
        (above if (row_y(t, c) + row_y(ref, pk)) / 2 < middle else below).append(((t, c), ref))

    def track_y(key):
        if key in above:
            return top - 24 - 16 * above.index(key)
        return table_bottom + 24 + 16 * below.index(key)

    corridors = defaultdict(list)       # corridor i lies left of column i; its items are vertical lanes
    lines = []
    for (t, c), ref in keys:
        pk = next(name for name, _, k, _ in cols[ref] if k == "PRI")
        ci, cr = column_of[t], column_of[ref]
        if abs(ci - cr) > 1:
            track = track_y(((t, c), ref))
            near_child = ci + 1 if cr > ci else ci          # corridor next to the child, on the parent's side
            near_parent = cr if cr > ci else cr + 1
            a = {"y1": row_y(t, c), "side1": "L" if near_child - 1 == ci else "R", "y2": track, "side2": "T", "end": (t, c)}
            b = {"y1": track, "side1": "T", "y2": row_y(ref, pk), "side2": "L" if near_parent - 1 == cr else "R",
                 "end2": (ref, pk)}
            corridors[near_child].append(a)
            corridors[near_parent].append(b)
            lines.append(("long", a, b, near_child, near_parent))
            continue
        if ci != cr:
            corridor = max(ci, cr)
        elif (t, c) in loop_left:
            corridor = ci
        elif ci == 0 and len(columns) > 1:
            corridor = 0
        else:
            corridor = ci + 1
        left_col = corridor - 1
        e = {"y1": row_y(t, c), "y2": row_y(ref, pk), "side1": "L" if ci == left_col else "R",
             "side2": "L" if cr == left_col else "R", "end": (t, c), "end2": (ref, pk)}
        corridors[corridor].append(e)
        lines.append(("short", e, None, corridor, corridor))

    # heights touched by several lines in one corridor: spread the ends apart so the lines run side by side
    ends = defaultdict(list)
    for corridor, items in corridors.items():
        for e in items:
            # keyed by height, not by row: rows of two tables on either side of a corridor may line up too
            if e["side1"] != "T":
                ends[(corridor, round(e["y1"]))].append((e, "y1", e["y2"]))
            if e["side2"] != "T":
                ends[(corridor, round(e["y2"]))].append((e, "y2", e["y1"]))
    for items in ends.values():
        if len(items) > 1:
            items.sort(key=lambda item: item[2])
            for i, (e, field, _) in enumerate(items):
                e[field] += 7 * i - 7 * (len(items) - 1) / 2

    bounds = {}
    for corridor, items in corridors.items():
        if corridor == 0:
            right_edge = col_x[0]
            left_edge = right_edge - 16 * (len(items) + 1)
        elif corridor == len(columns):
            left_edge = col_x[-1] + col_w[-1]
            right_edge = left_edge + 16 * (len(items) + 1)
        else:
            left_edge, right_edge = col_x[corridor - 1] + col_w[corridor - 1], col_x[corridor]
        bounds[corridor] = (left_edge, right_edge)
        step = (right_edge - left_edge) / (len(items) + 1)
        for i, e in enumerate(best_order(items)):
            e["lane"] = left_edge + step * (i + 1)

    def side_x(corridor, side):
        left_edge, right_edge = bounds[corridor]
        return left_edge if side == "L" else right_edge

    for kind, a, b, ca, cb in lines:
        if kind == "short":
            d.path(EDGE, [(side_x(ca, a["side1"]), a["y1"]), (a["lane"], a["y1"]), (a["lane"], a["y2"]),
                          (side_x(ca, a["side2"]), a["y2"])])
        else:
            d.path(EDGE, [(side_x(ca, a["side1"]), a["y1"]), (a["lane"], a["y1"]), (a["lane"], a["y2"]),
                          (b["lane"], b["y1"]), (b["lane"], b["y2"]), (side_x(cb, b["side2"]), b["y2"])])

    bottom = table_bottom + 16 * len(below) + (16 if below else 0)
    d.vertex("PK khoá chính · FK khoá ngoại · UQ duy nhất · NN bắt buộc (NOT NULL) · chân gà: nhiều, hai vạch: đúng một" + note,
             f"text;html=1;align=left;verticalAlign=middle;{FONT}fontColor=#444444;", 40, bottom + 30, 1600, 24)
    d.save(filename, title)
    return len(keys)


def group_diagram(key):
    title, colours, columns = GROUPS[key]
    tables = {t: colours for column in columns for t in column}
    return draw_erd(f"ERD: {title}", f"erd-{key}.drawio", columns, tables, in_group, LOOP_LEFT,
                    " · “→ bảng”: khoá ngoại sang nhóm khác (xem ERD toàn bộ)")


# The whole schema on one page: the columns follow the flow staff → session → delivery → content, so most keys join
# neighbouring columns. Only the audit keys to users (who created, uploaded, decided, marked, sent, acted) are written
# as "→ users" instead of drawn; the room's staff (exam_invigilators, exam_examiners) and user_areas are drawn.
FULL_COLUMNS = [
    ["users", "user_areas", "approval_requests", "site_settings"],
    ["exam_invigilators", "exam_examiners", "candidate_messages"],
    ["exams", "candidates", "invigilator_actions"],
    ["exam_types", "exam_sections", "attempts", "security_events"],
    ["section_templates", "test_papers", "paper_mixes"],
    ["paper_parts", "assets", "responses", "annotations"],
    ["question_groups", "questions", "answer_keys", "manual_marks"],
]
AUDIT_TO_USERS = {"created_by_user_id", "uploaded_by_user_id", "updated_by_user_id", "sent_by_user_id",
                  "resolved_by_user_id", "marker_user_id", "requested_by_user_id", "decided_by_user_id"}
ACTIONS_USER = {("invigilator_actions", "user_id")}


def all_but_audit(t, c, ref):
    return not (ref == "users" and (c in AUDIT_TO_USERS or (t, c) in ACTIONS_USER))


def full_diagram():
    assert {t for col in FULL_COLUMNS for t in col} == set(cols)
    colour_of = {t: GROUPS[group_of[t]][1] for t in cols}
    return draw_erd("ERD toàn bộ: 25 bảng của hệ thống thi", "erd-full.drawio", FULL_COLUMNS, colour_of, all_but_audit,
                    {("answer_keys", "question_id"), ("security_events", "attempt_id")},
                    " · “→ users”: khoá ngoại ghi người tạo, duyệt, chấm, gửi, thao tác (không vẽ đường) · màu theo nhóm như ERD tổng quan")


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
    print("wrote erd-full.drawio;", full_diagram(), "lines")
    for key in GROUPS:
        group_diagram(key)
        print("wrote", f"erd-{key}.drawio")
    links = overview()
    print("wrote erd.drawio;", {f"{a}->{b}": len(v) for (a, b), v in links.items()})
