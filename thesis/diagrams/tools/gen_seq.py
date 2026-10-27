"""Generate the UML sequence diagrams thesis/diagrams/seq-*.drawio from tools/seq/*.json.

    python -I thesis/diagrams/tools/gen_seq.py        (repo root), then export each to PNG (see CLAUDE.md)

JSON: {"title", "participants": [{"id", "label", "actor"?}], "messages": [...]} where a message is
{"from", "to", "label"} (synchronous call), with "async": true (dashed, open arrow) or "return": true (grey reply), a
self call when from == to, or {"note": text, "over": participant}. Every coordinate is computed: participants are
evenly spaced, each message takes its own row, activation bars open on a call and close on the reply. Labels sit
above their arrow on a white background, a self call's label to the right of its loop, and notes grow with their
text, so nothing is drawn over anything else.
"""
import json
import os
from xml.sax.saxutils import escape

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.dirname(HERE)

FONT = "fontFamily=Times New Roman;fontSize=15;"
SPACING, LEFT = 340, 130          # lifeline centres; a lane fits the longest label
HEAD_W, HEAD_H = 170, 56
FIRST_ROW, ROW, SELF_ROW = HEAD_H + 50, 58, 84
BAR_W = 12
NOTE_W, NOTE_LINE = SPACING - 60, 21   # a note stays inside its lane

HEAD = f"rounded=0;whiteSpace=wrap;html=1;{FONT}"
ACTOR = f"shape=umlActor;verticalLabelPosition=bottom;verticalAlign=top;html=1;outlineConnect=0;{FONT}"
LIFE = "endArrow=none;dashed=1;html=1;strokeColor=#555555;"
BAR = "rounded=0;whiteSpace=wrap;html=1;fillColor=#ffffff;strokeColor=#333333;"
LABEL = f"{FONT}labelBackgroundColor=#ffffff;verticalAlign=bottom;"
SYNC = "endArrow=block;endFill=1;html=1;rounded=0;strokeColor=#222222;" + LABEL
ASYNC = "endArrow=open;dashed=1;html=1;rounded=0;strokeColor=#222222;" + LABEL
REPLY = "endArrow=open;dashed=1;html=1;rounded=0;strokeColor=#888888;fontColor=#666666;" + LABEL
TEXT = f"text;html=1;whiteSpace=wrap;align=left;verticalAlign=middle;{FONT}"
NOTE = f"shape=note;whiteSpace=wrap;html=1;size=14;fillColor=#fff2cc;strokeColor=#d6b656;align=left;spacingLeft=8;spacingRight=8;{FONT}"


def q(s):
    return escape(str(s), {'"': "&quot;", "\n": "&#xa;"})


class Doc:
    def __init__(self):
        self.cells, self.n = [], 1

    def vertex(self, value, style, x, y, w, h):
        self.n += 1
        self.cells.append(f'<mxCell id="v{self.n}" value="{q(value)}" style="{style}" vertex="1" parent="1">'
                          f'<mxGeometry x="{x}" y="{y}" width="{w}" height="{h}" as="geometry"/></mxCell>')

    def line(self, value, style, points):
        """An edge between free points (no source or target cell), so its geometry is exactly as computed."""
        self.n += 1
        (sx, sy), (tx, ty) = points[0], points[-1]
        middle = "".join(f'<mxPoint x="{x}" y="{y}"/>' for x, y in points[1:-1])
        array = f'<Array as="points">{middle}</Array>' if middle else ""
        self.cells.append(f'<mxCell id="e{self.n}" value="{q(value)}" style="{style}" edge="1" parent="1">'
                          f'<mxGeometry relative="1" as="geometry"><mxPoint x="{sx}" y="{sy}" as="sourcePoint"/>'
                          f'<mxPoint x="{tx}" y="{ty}" as="targetPoint"/>{array}</mxGeometry></mxCell>')

    def save(self, name, title):
        body = "".join(self.cells)
        xml = (f'<mxfile host="gen_seq.py"><diagram name="{q(title)}"><mxGraphModel grid="0" page="0"><root>'
               f'<mxCell id="0"/><mxCell id="1" parent="0"/>{body}</root></mxGraphModel></diagram></mxfile>')
        with open(os.path.join(OUT, name), "w", encoding="utf-8") as f:
            f.write(xml)


def note_height(text):
    chars_per_line = NOTE_W // 8
    lines = sum(max(1, -(-len(part) // chars_per_line)) for part in text.split("\n"))
    return lines * NOTE_LINE + 18


def build(spec):
    parts = spec["participants"]
    x_of = {p["id"]: LEFT + i * SPACING for i, p in enumerate(parts)}
    actors = {p["id"] for p in parts if p.get("actor")}

    # First pass: the y of every row, and the activation bars (open on a call, close on the reply).
    y = FIRST_ROW
    rows, bars, open_bars = [], [], {}
    for m in spec["messages"]:
        if "note" in m:
            h = note_height(m["note"])
            rows.append((m, y, h))
            y += h + 26
            continue
        is_self = m["from"] == m["to"]
        rows.append((m, y, 0))
        if m.get("return"):
            start = open_bars.pop(m["from"], None)
            if start is not None:
                bars.append((m["from"], start, y))
        elif m["to"] not in open_bars:
            open_bars[m["to"]] = y
        if m["from"] not in open_bars and not m.get("return"):
            open_bars[m["from"]] = y
        y += SELF_ROW if is_self else ROW
    bottom = y + 20
    bars += [(pid, start, bottom - 10) for pid, start in open_bars.items()]
    bars = [bar for bar in bars if bar[0] not in actors]   # people have no activation bar

    d = Doc()
    for p in parts:
        x = x_of[p["id"]]
        if p.get("actor"):
            d.vertex(p["label"], ACTOR, x - 15, 4, 30, HEAD_H - 24)
        else:
            d.vertex(p["label"], HEAD, x - HEAD_W / 2, 0, HEAD_W, HEAD_H)
        d.line("", LIFE, [(x, HEAD_H + (16 if p.get("actor") else 0)), (x, bottom)])
    for pid, start, end in bars:
        d.vertex("", BAR, x_of[pid] - BAR_W / 2, start - 6, BAR_W, max(end - start + 12, 24))

    def edge_x(pid, toward):
        if pid in actors:
            return x_of[pid]
        return x_of[pid] + (BAR_W / 2 if toward > x_of[pid] else -BAR_W / 2)

    for m, y, h in rows:
        if "note" in m:
            x = x_of[m["over"]] + 22
            d.vertex(m["note"], NOTE, x, y - 10, NOTE_W, h)
            continue
        if m["from"] == m["to"]:
            x = x_of[m["from"]] + BAR_W / 2
            d.line("", SYNC, [(x, y), (x + 46, y), (x + 46, y + 30), (x, y + 30)])
            d.vertex(m["label"], TEXT, x + 54, y - 6, SPACING - 70, 40)
            continue
        style = REPLY if m.get("return") else ASYNC if m.get("async") else SYNC
        sx, tx = x_of[m["from"]], x_of[m["to"]]
        d.line(m["label"], style, [(edge_x(m["from"], tx), y), (edge_x(m["to"], sx), y)])
    return d


if __name__ == "__main__":
    folder = os.path.join(HERE, "seq")
    for name in sorted(os.listdir(folder)):
        spec = json.load(open(os.path.join(folder, name), encoding="utf-8"))
        build(spec).save(name.replace(".json", ".drawio"), spec.get("title", name))
        print("wrote", name.replace(".json", ".drawio"))
