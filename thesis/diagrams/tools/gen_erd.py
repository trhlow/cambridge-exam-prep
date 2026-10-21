"""Generate thesis/diagrams/erd.drawio from the live MySQL schema.

Usage (repo root, MySQL container running):  python -I thesis/diagrams/tools/gen_erd.py
Reads MYSQL_USER / MYSQL_PASSWORD / MYSQL_DATABASE from docker/.env and queries information_schema
inside the examprep-mysql-1 container. Then export the PNG with draw.io (see CLAUDE.md).
"""
import os
import subprocess
from collections import defaultdict
from xml.sax.saxutils import escape

TOOLS = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.dirname(TOOLS)
REPO = os.path.dirname(os.path.dirname(OUT))
CONTAINER = "examprep-mysql-1"

QUERIES = {
    "columns": "SELECT table_name, column_name, column_type, column_key, ordinal_position FROM information_schema.columns "
               "WHERE table_schema = DATABASE() AND table_name <> '__EFMigrationsHistory' ORDER BY table_name, ordinal_position",
    "fks": "SELECT table_name, column_name, referenced_table_name FROM information_schema.key_column_usage "
           "WHERE table_schema = DATABASE() AND referenced_table_name IS NOT NULL ORDER BY table_name",
    "uniques": "SELECT DISTINCT table_name, column_name FROM information_schema.statistics "
               "WHERE table_schema = DATABASE() AND non_unique = 0 AND index_name <> 'PRIMARY'",
}

# Audit foreign keys to users are written in the column text instead of drawn, to keep the diagram readable.
AUDIT_TO_USERS = {"created_by_user_id", "uploaded_by_user_id", "updated_by_user_id", "sent_by_user_id",
                  "resolved_by_user_id", "marker_user_id", "user_id", "requested_by_user_id", "decided_by_user_id"}
DRAW_USER_EDGES = {("exam_invigilators", "user_id"), ("exam_examiners", "user_id"), ("user_areas", "user_id")}

COLUMNS = [  # domain columns, left to right; tables stacked top-down
    ("staff", ["users", "user_areas", "approval_requests", "site_settings"]),
    ("template", ["exam_types", "section_templates", "test_papers", "assets"]),
    ("content", ["paper_parts", "question_groups", "questions", "answer_keys", "paper_mixes"]),
    ("exam", ["exams", "exam_sections", "exam_invigilators", "exam_examiners", "candidates", "candidate_messages"]),
    ("delivery", ["attempts", "responses", "manual_marks", "annotations"]),
    ("monitor", ["invigilator_actions", "security_events"]),
]
DOMAIN_OVERRIDE = {"assets": "staff"}
COLORS = {"staff": ("#fff2cc", "#d6b656"), "template": ("#f5f5f5", "#666666"), "content": ("#dae8fc", "#6c8ebf"),
          "exam": ("#e1d5e7", "#9673a6"), "delivery": ("#d5e8d4", "#82b366"), "monitor": ("#f8cecc", "#b85450")}
LEGEND = [("staff", "Tài khoản, cấu hình, tệp"), ("template", "Loại bài thi"), ("content", "Nội dung đề"),
          ("exam", "Kỳ thi, thí sinh"), ("delivery", "Làm bài, chấm điểm"), ("monitor", "Giám sát")]

ROW, HEAD, COL_GAP, ROW_GAP, KEY_W = 22, 28, 150, 50, 52
FONT = "fontFamily=Times New Roman;fontSize=12;"


def q(s):
    return escape(s, {'"': "&quot;"})


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
    return [line.split("	") for line in result.stdout.splitlines() if line]


cols = defaultdict(list)
for table, col, ctype, ckey, _ in query(QUERIES["columns"]):
    cols[table].append((col, ctype, ckey))
fks = {(t, c): ref for t, c, ref in query(QUERIES["fks"])}
uniques = {(t, c) for t, c in query(QUERIES["uniques"])}

placed = [t for _, ts in COLUMNS for t in ts]
assert set(placed) == set(cols), f"layout mismatch: {set(cols) ^ set(placed)}"


def label(table, col, ctype, ckey):
    keys = []
    if ckey == "PRI":
        keys.append("PK")
    if (table, col) in fks:
        keys.append("FK")
    if (table, col) in uniques and ckey != "PRI":
        keys.append("UQ")
    text = f"{col} : {ctype}"
    ref = fks.get((table, col))
    if ref == "users" and col in AUDIT_TO_USERS and (table, col) not in DRAW_USER_EDGES:
        text += "  → users"
    return ",".join(keys), text, ckey == "PRI"


def width(table):
    longest = max(len(label(table, *c)[1]) for c in cols[table])
    return max(220, int(round((KEY_W + 16 + longest * 6.6) / 10.0)) * 10)


cells, n = [], [1]


def nid(p="c"):
    n[0] += 1
    return f"{p}{n[0]}"


def vertex(value, style, x, y, w, h, parent="1", cid=None, extra=""):
    cid = cid or nid()
    cells.append(f'<mxCell id="{cid}" value="{q(value)}" style="{style}" vertex="1" parent="{parent}">'
                 f'<mxGeometry x="{x}" y="{y}" width="{w}" height="{h}" as="geometry">{extra}</mxGeometry></mxCell>')
    return cid


layout, col_of, col_span, x = {}, {}, [], 40
for ci, (domain, tables) in enumerate(COLUMNS):
    w = max(width(t) for t in tables)
    col_span.append((x, x + w))
    y = 40
    for t in tables:
        layout[t] = (x, y, w)
        col_of[t] = ci
        y += HEAD + ROW * len(cols[t]) + ROW_GAP
    x += w + COL_GAP

rows = {}
col_x = []
for domain, tables in COLUMNS:
    for t in tables:
        fill, stroke = COLORS[DOMAIN_OVERRIDE.get(t, domain)]
        tx, ty, tw = layout[t]
        tid = vertex(t, "shape=table;startSize=28;container=1;collapsible=0;childLayout=tableLayout;fixedRows=1;rowLines=0;"
                     f"fontStyle=1;align=center;resizeLast=1;html=1;{FONT}fillColor={fill};strokeColor={stroke};",
                     tx, ty, tw, HEAD + ROW * len(cols[t]), cid=f"t_{t}")
        for i, (c, ctype, ckey) in enumerate(cols[t]):
            keys, text, is_pk = label(t, c, ctype, ckey)
            rid = f"r_{t}_{c}"
            rows[(t, c)] = rid
            vertex("", "shape=tableRow;horizontal=0;startSize=0;swimlaneHead=0;swimlaneBody=0;fillColor=none;collapsible=0;"
                   "dropTarget=0;points=[[0,0.5],[1,0.5]];portConstraint=eastwest;top=0;left=0;right=0;bottom=0;html=1;",
                   0, HEAD + i * ROW, tw, ROW, parent=tid, cid=rid)
            vertex(keys, "shape=partialRectangle;connectable=0;fillColor=none;top=0;left=0;bottom=0;right=0;fontStyle=1;"
                   f"overflow=hidden;whiteSpace=wrap;html=1;{FONT}fontSize=10;", 0, 0, KEY_W, ROW, parent=rid,
                   extra=f'<mxRectangle width="{KEY_W}" height="{ROW}" as="alternateBounds"/>')
            style = "fontStyle=5;" if is_pk else ""
            vertex(text, "shape=partialRectangle;connectable=0;fillColor=none;top=0;left=0;bottom=0;right=0;align=left;"
                   f"spacingLeft=6;overflow=hidden;whiteSpace=wrap;html=1;{FONT}{style}", KEY_W, 0, tw - KEY_W, ROW,
                   parent=rid, extra=f'<mxRectangle width="{tw - KEY_W}" height="{ROW}" as="alternateBounds"/>')

edge_style = ("edgeStyle=orthogonalEdgeStyle;rounded=1;orthogonalLoop=1;jettySize=auto;html=1;startArrow=ERmany;"
              "startFill=0;endArrow=ERmandOne;endFill=0;startSize=9;endSize=9;strokeColor=#555555;")
edges = 0
long_edges = 0
for (t, c), ref in sorted(fks.items()):
    if ref == "users" and c in AUDIT_TO_USERS and (t, c) not in DRAW_USER_EDGES:
        continue
    pk = next(col for col, _, k in cols[ref] if k == "PRI")
    a, b = col_of[t], col_of[ref]
    ex, en = (1, 0) if a < b else (0, 1) if a > b else (0, 0)
    points = ""
    if abs(a - b) >= 2:
        # Long edge: leave through the corridor next to the child, run along a lane above all tables,
        # and come down the corridor next to the parent, so it never crosses a table body.
        lane_y = 10 - long_edges * 10
        shift = (long_edges % 5) * 12 - 24
        cx_child = (col_span[a][0] - COL_GAP // 2 if a > b else col_span[a][1] + COL_GAP // 2) + shift
        cx_parent = (col_span[b][1] + COL_GAP // 2 if a > b else col_span[b][0] - COL_GAP // 2) + shift
        points = (f'<Array as="points"><mxPoint x="{cx_child}" y="{lane_y}"/>'
                  f'<mxPoint x="{cx_parent}" y="{lane_y}"/></Array>')
        long_edges += 1
    cells.append(f'<mxCell id="{nid("e")}" style="{edge_style}exitX={ex};exitY=0.5;exitDx=0;exitDy=0;entryX={en};entryY=0.5;'
                 f'entryDx=0;entryDy=0;" edge="1" parent="1" source="{rows[(t, c)]}" target="{rows[(ref, pk)]}">'
                 f'<mxGeometry relative="1" as="geometry">{points}</mxGeometry></mxCell>')
    edges += 1

# legend under the first column
bottom = max(y0 + HEAD + ROW * len(cols[t]) for t, (x0, y0, w0) in layout.items())
leg_y = bottom + 60
leg = vertex("Chú thích — PK khóa chính · FK khóa ngoại · UQ duy nhất · “→ users”: khóa ngoại tới users (không vẽ đường)",
             f"rounded=0;whiteSpace=wrap;html=1;fillColor=none;strokeColor=#666666;verticalAlign=top;align=left;spacingLeft=8;{FONT}fontStyle=1;",
             40, leg_y, 1180, 64, cid="legend")
lx = 16
for domain, text in LEGEND:
    fill, stroke = COLORS[domain]
    vertex("", f"rounded=0;html=1;fillColor={fill};strokeColor={stroke};", lx, 32, 28, 16, parent=leg)
    vertex(text, f"text;html=1;align=left;verticalAlign=middle;{FONT}", lx + 34, 28, 160, 24, parent=leg)
    lx += 196

with open(os.path.join(OUT, "erd.drawio"), "w", encoding="utf-8") as f:
    f.write('<?xml version="1.0" encoding="UTF-8"?>\n<mxfile host="drawio" version="31.7.0"><diagram name="ERD">'
            '<mxGraphModel grid="1" gridSize="10" page="0"><root><mxCell id="0"/><mxCell id="1" parent="0"/>'
            + "".join(cells) + "</root></mxGraphModel></diagram></mxfile>\n")
print(f"{len(cols)} tables, {sum(len(v) for v in cols.values())} columns, {edges} relationship lines")
