"""Checks generated diagrams for lines drawn over each other or across a table.

    python -I thesis/diagrams/tools/check_lines.py thesis/diagrams/erd*.drawio

For every edge given by free points (as gen_erd.py and gen_seq.py write them), reports
  - overlaps: two edges with collinear segments that share more than a point,
  - through:  a segment that runs across the inside of a table (not just touching its side),
and counts crossings (a horizontal and a vertical segment of different edges that cut each other).
"""
import itertools
import re
import sys
import xml.etree.ElementTree as ET


def segments(path):
    root = ET.parse(path).getroot()
    edges, boxes = [], []
    for cell in root.iter("mxCell"):
        geo = cell.find("mxGeometry")
        if geo is None:
            continue
        if cell.get("edge") == "1":
            src = geo.find("mxPoint[@as='sourcePoint']")
            dst = geo.find("mxPoint[@as='targetPoint']")
            if src is None or dst is None:
                continue
            middle = [(float(p.get("x")), float(p.get("y"))) for p in geo.findall("Array/mxPoint")]
            points = [(float(src.get("x")), float(src.get("y")))] + middle + [(float(dst.get("x")), float(dst.get("y")))]
            edges.append((cell.get("id"), list(zip(points, points[1:]))))
        elif cell.get("vertex") == "1" and cell.get("parent") == "1" and "shape=table" in (cell.get("style") or ""):
            x, y = float(geo.get("x")), float(geo.get("y"))
            boxes.append((cell.get("value"), x, y, x + float(geo.get("width")), y + float(geo.get("height"))))
    return edges, boxes


def check(path):
    edges, boxes = segments(path)
    overlaps, through, crossings = [], [], 0
    for (ida, sa), (idb, sb) in itertools.combinations(edges, 2):
        for (p1, p2), (q1, q2) in itertools.product(sa, sb):
            if p1[0] == p2[0] == q1[0] == q2[0]:                      # both vertical on one x
                lo, hi = max(min(p1[1], p2[1]), min(q1[1], q2[1])), min(max(p1[1], p2[1]), max(q1[1], q2[1]))
                if hi - lo > 0.5:
                    overlaps.append((ida, idb, f"x={p1[0]:.0f} y {lo:.0f}-{hi:.0f}"))
            elif p1[1] == p2[1] == q1[1] == q2[1]:                    # both horizontal on one y
                lo, hi = max(min(p1[0], p2[0]), min(q1[0], q2[0])), min(max(p1[0], p2[0]), max(q1[0], q2[0]))
                if hi - lo > 0.5:
                    overlaps.append((ida, idb, f"y={p1[1]:.0f} x {lo:.0f}-{hi:.0f}"))
            else:
                for (h1, h2), (v1, v2) in (((p1, p2), (q1, q2)), ((q1, q2), (p1, p2))):
                    if h1[1] == h2[1] and v1[0] == v2[0]:
                        if min(h1[0], h2[0]) < v1[0] < max(h1[0], h2[0]) and min(v1[1], v2[1]) < h1[1] < max(v1[1], v2[1]):
                            crossings += 1
    for eid, segs in edges:
        for (x1, y1), (x2, y2) in segs:
            for name, bx1, by1, bx2, by2 in boxes:
                if x1 == x2 and bx1 + 1 < x1 < bx2 - 1 and min(y1, y2) < by2 - 1 and max(y1, y2) > by1 + 1:
                    through.append((eid, name))
                if y1 == y2 and by1 + 1 < y1 < by2 - 1 and min(x1, x2) < bx2 - 1 and max(x1, x2) > bx1 + 1:
                    through.append((eid, name))
    return len(edges), overlaps, through, crossings


if __name__ == "__main__":
    bad = False
    for path in sys.argv[1:]:
        n, overlaps, through, crossings = check(path)
        print(f"{path}: {n} lines, {len(overlaps)} overlaps, {len(through)} through tables, {crossings} crossings")
        for o in overlaps:
            print("   overlap", *o)
        for t in through:
            print("   through", *t)
        bad |= bool(overlaps or through)
    sys.exit(1 if bad else 0)
