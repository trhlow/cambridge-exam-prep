"""Generate thesis/diagrams/attempt-state.drawio: the UML state machine of one attempt (lượt làm một phần thi).

Follows Domain/Delivery/AttemptClock (status) and Infrastructure/Marking/AttemptMarker + the Writing marks
endpoint (grading status). Usage (repo root):  python -I thesis/diagrams/tools/gen_state.py
"""
import os
from xml.sax.saxutils import escape

OUT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FONT = "fontFamily=Times New Roman;fontSize=14;"
STATE = f"rounded=1;arcSize=30;whiteSpace=wrap;html=1;{FONT}fillColor=#dae8fc;strokeColor=#6c8ebf;verticalAlign=top;spacingTop=6;"
GRADE = f"rounded=1;arcSize=30;whiteSpace=wrap;html=1;{FONT}fillColor=#d5e8d4;strokeColor=#82b366;"
INITIAL = "ellipse;html=1;fillColor=#000000;strokeColor=#000000;"
FINAL = "ellipse;html=1;shape=doubleEllipse;fillColor=#000000;strokeColor=#000000;"
EDGE = ("endArrow=open;endSize=10;html=1;rounded=1;strokeColor=#333333;" + FONT +
        "fontSize=12;labelBackgroundColor=#ffffff;")


def q(s):
    return escape(s, {'"': "&quot;", "\n": "&#xa;"})


class Doc:
    def __init__(self):
        self.cells, self.n = [], 1

    def vertex(self, value, style, x, y, w, h, parent="1"):
        self.n += 1
        cid = f"v{self.n}"
        self.cells.append(f'<mxCell id="{cid}" value="{q(value)}" style="{style}" vertex="1" parent="{parent}">'
                          f'<mxGeometry x="{x}" y="{y}" width="{w}" height="{h}" as="geometry"/></mxCell>')
        return cid

    def edge(self, src, tgt, label="", style="", points=None, parent="1", dx=0, dy=0):
        self.n += 1
        pts = ""
        if points:
            pts = '<Array as="points">' + "".join(f'<mxPoint x="{x}" y="{y}"/>' for x, y in points) + "</Array>"
        offset = f'<mxPoint x="{dx}" y="{dy}" as="offset"/>' if dx or dy else ""
        self.cells.append(f'<mxCell id="e{self.n}" value="{q(label)}" style="{EDGE}{style}" edge="1" parent="{parent}" '
                          f'source="{src}" target="{tgt}"><mxGeometry relative="1" as="geometry">{pts}{offset}</mxGeometry></mxCell>')


def state_machine():
    d = Doc()
    d.vertex("Máy trạng thái của một lượt làm bài (attempts)", f"text;html=1;{FONT}fontSize=16;fontStyle=1;align=left;", 40, 0, 600, 30)

    init = d.vertex("", INITIAL, 50, 162, 26, 26)
    todo = d.vertex("Chưa bắt đầu\n(NotStarted)", STATE + "verticalAlign=middle;", 130, 140, 170, 70)
    work = d.vertex("Đang làm (InProgress)\n\nentry / tính ends_at\ndo / tự lưu câu trả lời,\nheartbeat 5 giây",
                    STATE, 470, 115, 230, 120)
    pause = d.vertex("Tạm dừng (Paused)\n\nentry / ghi paused_at\ndo / đồng hồ đứng yên",
                     STATE, 470, 400, 230, 100)

    d.edge(init, todo)
    d.edge(todo, work, "Thí sinh mở phần thi\n[các phần trước đã nộp]\n/ AttemptClock.Start", dy=-34)
    d.edge(work, pause, "Giám thị Pause\n[thí sinh đang online]", "exitX=0.3;exitY=1;entryX=0.3;entryY=0;", dx=-78)
    d.edge(pause, work, "Giám thị Resume\n/ paused_seconds += thời gian dừng", "exitX=0.7;exitY=0;entryX=0.7;entryY=1;", dx=96)
    d.edge(work, work, "Cộng giờ sự cố · End at / End in\nTua audio · Use test end time\n/ tính lại ends_at",
           "exitX=0.25;exitY=0;entryX=0.75;entryY=0;", points=[(527, 70), (642, 70)], dy=-30)
    d.edge(pause, pause, "Cộng giờ sự cố · Use test end time", "exitX=0.25;exitY=1;entryX=0.75;entryY=1;",
           points=[(527, 545), (642, 545)], dy=18)

    # Submitted, with the grading sub-machine inside.
    done = d.vertex("Đã nộp (Submitted) — chấm điểm (grading_status)",
                    f"swimlane;rounded=1;html=1;{FONT}fontStyle=1;startSize=30;fillColor=#f5f5f5;strokeColor=#666666;",
                    960, 60, 520, 460)
    d.edge(work, done, "Hết giờ [TimeExpired]\nGiám thị End now [InvigilatorEndNow]\n/ ghi submitted_at", "entryX=0;entryY=0.24;", dy=-44)

    g_init = d.vertex("", INITIAL, 30, 60, 22, 22, parent=done)
    not_graded = d.vertex("Chưa chấm\n(NotGraded)", GRADE, 90, 46, 160, 50, parent=done)
    auto = d.vertex("Đã chấm tự động\n(AutoGraded)", GRADE, 30, 230, 170, 56, parent=done)
    manual = d.vertex("Chờ chấm Writing\n(NeedsManualMarking)", GRADE, 260, 230, 190, 56, parent=done)
    final = d.vertex("Hoàn tất\n(Final)", GRADE, 270, 370, 170, 50, parent=done)
    end = d.vertex("", FINAL, 104, 384, 24, 24, parent=done)

    d.edge(g_init, not_graded, parent=done)
    d.edge(not_graded, auto, "AttemptMarker\n[đề không có Writing]", "exitX=0.3;exitY=1;entryX=0.5;entryY=0;", parent=done, dx=-30)
    d.edge(not_graded, manual, "AttemptMarker\n[có bài Writing]", "exitX=0.8;exitY=1;entryX=0.5;entryY=0;", parent=done, dx=30)
    d.edge(manual, manual, "Giám khảo chấm một câu\n[còn câu chưa chấm]", "exitX=1;exitY=0.3;entryX=1;entryY=0.7;",
           points=[(478, 247), (478, 270)], parent=done, dx=-70, dy=-46)
    d.edge(manual, final, "Giám khảo chấm câu Writing cuối\n/ điểm = tự động + Writing", "", parent=done, dx=-4)
    d.edge(auto, end, "", "exitX=0.5;exitY=1;entryX=0.5;entryY=0;", parent=done)
    d.edge(final, end, "", "exitX=0;exitY=0.5;entryX=1;entryY=0.5;", parent=done)

    note = ("Ghi chú\n• Mọi thay đổi đồng hồ chỉ đi qua AttemptClock; giờ kết thúc do server tính.\n"
            "• Thí sinh offline vẫn bị nộp đúng giờ (AttemptExpiryWorker quét mỗi 5 giây).\n"
            "• Không có nút nộp bài của thí sinh (giống Cambridge): chờ hết giờ hoặc giám thị End now.\n"
            "• Phần thi sau (Listening) chỉ mở khi mọi phần trước đã ở trạng thái Đã nộp.")
    d.vertex(note, f"shape=note;whiteSpace=wrap;html=1;size=14;align=left;spacingLeft=8;{FONT}fontSize=12;fillColor=#fff2cc;strokeColor=#d6b656;",
             40, 590, 820, 100)

    body = "".join(d.cells)
    return (f'<diagram name="Trạng thái lượt làm bài"><mxGraphModel grid="1" gridSize="10" page="0">'
            f'<root><mxCell id="0"/><mxCell id="1" parent="0"/>{body}</root></mxGraphModel></diagram>')


if __name__ == "__main__":
    with open(f"{OUT}/attempt-state.drawio", "w", encoding="utf-8") as f:
        f.write('<?xml version="1.0" encoding="UTF-8"?>\n<mxfile host="drawio" version="31.7.0">')
        f.write(state_machine())
        f.write("</mxfile>\n")
    print("ok")
