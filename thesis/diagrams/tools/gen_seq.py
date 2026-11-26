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
# At most 860 wide, so 15 px text prints at about 8 pt on a 16.5 cm portrait column; labels wrap to their lane.
MAX_WIDTH, MAX_SPACING, MARGIN = 860, 340, 95
HEAD_H = 56
FIRST_ROW, ROW, SELF_ROW = HEAD_H + 40, 46, 76
BAR_W = 12
LINE, CHAR_PX = 20, 6.3

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


def wrap(text, width_px):
    """Breaks a label into lines that fit width_px (Times New Roman 15 px is about 6.3 px a character), as evenly as
    the line count allows, so no line is left with a single word."""
    chars = max(8, int(width_px / CHAR_PX))
    count = len(_wrap(text, chars))
    while chars > 8 and len(_wrap(text, chars - 1)) == count:
        chars -= 1
    return _wrap(text, chars)


def _wrap(text, chars):
    lines = []
    for part in text.split("\n"):
        line = ""
        for word in part.split(" "):
            if line and len(line) + 1 + len(word) > chars:
                lines.append(line)
                line = word
            else:
                line = f"{line} {word}" if line else word
        lines.append(line)
    return lines


def build(spec):
    parts = spec["participants"]
    n = len(parts)
    spacing = min(MAX_SPACING, (MAX_WIDTH - 2 * MARGIN) / max(n - 1, 1))
    head_w = spacing - 16
    left = MARGIN
    x_of = {p["id"]: left + i * spacing for i, p in enumerate(parts)}
    right = left + (n - 1) * spacing + MARGIN
    actors = {p["id"] for p in parts if p.get("actor")}

    # First pass: the y of every row, and the activation bars (open on a call, close on the reply). A label sits above
    # its arrow, so a row starts with room for the label's lines.
    y = FIRST_ROW
    rows, bars, open_bars = [], [], {}
    for m in spec["messages"]:
        if "note" in m:
            x0 = x_of[m["over"]] + 22
            w = min(2 * spacing - 44, right - x0)
            if w < spacing:                          # the last lifeline: the note goes on its left
                w = min(2 * spacing - 44, x_of[m["over"]] - 22)
                x0 = x_of[m["over"]] - 22 - w
            lines = wrap(m["note"], w - 20)
            h = len(lines) * LINE + 18
            rows.append((m, y, h, "\n".join(lines), x0, w))
            y += h + 26
            continue
        is_self = m["from"] == m["to"]
        if is_self:
            x0 = x_of[m["from"]] + BAR_W / 2 + 54
            w = x_of[m["from"]] + spacing - x0 - 8
            if m["from"] == parts[-1]["id"]:       # no lane on the right: the label goes in the lane on the left
                x0, w = x_of[m["from"]] - spacing + 20, spacing - 34
            lines = wrap(m["label"], w)
            rows.append((m, y, 0, "\n".join(lines), x0, w))
        else:
            span = abs(x_of[m["to"]] - x_of[m["from"]]) - 2 * BAR_W
            lines = wrap(m["label"], span - 12)
            y += (len(lines) - 1) * LINE          # room above the arrow for the extra lines
            rows.append((m, y, 0, "\n".join(lines), 0, 0))
        if m.get("return"):
            start = open_bars.pop(m["from"], None)
            if start is not None:
                bars.append((m["from"], start, y))
        elif m["to"] not in open_bars:
            open_bars[m["to"]] = y
        if m["from"] not in open_bars and not m.get("return"):
            open_bars[m["from"]] = y
        y += max(SELF_ROW, len(lines) * LINE + 34) if is_self else ROW
    bottom = y + 20
    bars += [(pid, start, bottom - 10) for pid, start in open_bars.items()]
    bars = [bar for bar in bars if bar[0] not in actors]   # people have no activation bar

    d = Doc()
    for p in parts:
        x = x_of[p["id"]]
        if p.get("actor"):
            d.vertex(p["label"], ACTOR, x - 15, 4, 30, HEAD_H - 24)
        else:
            d.vertex(p["label"], HEAD, x - head_w / 2, 0, head_w, HEAD_H)
        d.line("", LIFE, [(x, HEAD_H + (16 if p.get("actor") else 0)), (x, bottom)])
    for pid, start, end in bars:
        d.vertex("", BAR, x_of[pid] - BAR_W / 2, start - 6, BAR_W, max(end - start + 12, 24))

    def edge_x(pid, toward):
        if pid in actors:
            return x_of[pid]
        return x_of[pid] + (BAR_W / 2 if toward > x_of[pid] else -BAR_W / 2)

    for m, y, h, text, x0, w in rows:
        if "note" in m:
            d.vertex(text, NOTE, x0, y - 10, w, h)
            continue
        if m["from"] == m["to"]:
            x = x_of[m["from"]] + BAR_W / 2
            d.line("", SYNC, [(x, y), (x + 46, y), (x + 46, y + 30), (x, y + 30)])
            lines = text.count("\n") + 1
            d.vertex(text, TEXT, x0, y + 15 - lines * LINE / 2 - 4, w, lines * LINE + 8)
            continue
        style = REPLY if m.get("return") else ASYNC if m.get("async") else SYNC
        sx, tx = x_of[m["from"]], x_of[m["to"]]
        d.line(text, style, [(edge_x(m["from"], tx), y), (edge_x(m["to"], sx), y)])
    return d


if __name__ == "__main__":
    folder = os.path.join(HERE, "seq")
    for name in sorted(os.listdir(folder)):
        spec = json.load(open(os.path.join(folder, name), encoding="utf-8"))
        build(spec).save(name.replace(".json", ".drawio"), spec.get("title", name))
        print("wrote", name.replace(".json", ".drawio"))
