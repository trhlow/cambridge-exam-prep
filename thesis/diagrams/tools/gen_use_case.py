"""Generate thesis/diagrams/use-case.drawio.

Usage (repo root):  python -I thesis/diagrams/tools/gen_use_case.py
"""
import os
from xml.sax.saxutils import escape

OUT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def q(s):
    return escape(s, {'"': "&quot;"})


class Doc:
    def __init__(self):
        self.cells = []
        self.n = 1

    def nid(self, prefix="c"):
        self.n += 1
        return f"{prefix}{self.n}"

    def vertex(self, value, style, x, y, w, h, parent="1", cid=None, extra=""):
        cid = cid or self.nid()
        self.cells.append(
            f'<mxCell id="{cid}" value="{q(value)}" style="{style}" vertex="1" parent="{parent}">'
            f'<mxGeometry x="{x}" y="{y}" width="{w}" height="{h}" as="geometry">{extra}</mxGeometry></mxCell>')
        return cid

    def edge(self, src, tgt, style, value="", points=None):
        cid = self.nid("e")
        pts = ""
        if points:
            pts = '<Array as="points">' + "".join(f'<mxPoint x="{x}" y="{y}"/>' for x, y in points) + "</Array>"
        self.cells.append(
            f'<mxCell id="{cid}" value="{q(value)}" style="{style}" edge="1" parent="1" source="{src}" target="{tgt}">'
            f'<mxGeometry relative="1" as="geometry">{pts}</mxGeometry></mxCell>')
        return cid

    def page(self, name):
        body = "".join(self.cells)
        return (f'<diagram name="{q(name)}"><mxGraphModel grid="1" gridSize="10" page="0">'
                f'<root><mxCell id="0"/><mxCell id="1" parent="0"/>{body}</root></mxGraphModel></diagram>')


def write(path, pages):
    with open(path, "w", encoding="utf-8") as f:
        f.write('<?xml version="1.0" encoding="UTF-8"?>\n<mxfile host="drawio" version="31.7.0">')
        f.write("".join(pages))
        f.write("</mxfile>\n")


# ---------------------------------------------------------------- USE CASE
def use_case():
    """Each actor stands next to its own use cases, so no association crosses another. The staff roles share one
    sign-in through the abstract actor "Nhân sự" (generalisation), instead of four long lines to one ellipse."""
    d = Doc()
    FONT = "fontFamily=Times New Roman;fontSize=14;"
    ACTOR = f"shape=umlActor;verticalLabelPosition=bottom;verticalAlign=top;html=1;outlineConnect=0;{FONT}fontStyle=1;"
    UC = {
        "admin": f"ellipse;whiteSpace=wrap;html=1;{FONT}fillColor=#e1d5e7;strokeColor=#9673a6;",
        "mgr": f"ellipse;whiteSpace=wrap;html=1;{FONT}fillColor=#fff2cc;strokeColor=#d6b656;",
        "exm": f"ellipse;whiteSpace=wrap;html=1;{FONT}fillColor=#f8cecc;strokeColor=#b85450;",
        "inv": f"ellipse;whiteSpace=wrap;html=1;{FONT}fillColor=#dae8fc;strokeColor=#6c8ebf;",
        "cand": f"ellipse;whiteSpace=wrap;html=1;{FONT}fillColor=#d5e8d4;strokeColor=#82b366;",
        "shared": f"ellipse;whiteSpace=wrap;html=1;{FONT}fillColor=#f5f5f5;strokeColor=#666666;",
        "seb": f"ellipse;whiteSpace=wrap;html=1;{FONT}fillColor=#ffe6cc;strokeColor=#d79b00;",
        "sys": f"ellipse;whiteSpace=wrap;html=1;{FONT}fillColor=#eeeeee;strokeColor=#555555;",
    }
    ASSOC = "endArrow=none;html=1;rounded=0;strokeColor=#333333;"
    DEP = ("endArrow=open;endSize=10;dashed=1;html=1;rounded=0;strokeColor=#333333;"
           "fontFamily=Times New Roman;fontSize=12;labelBackgroundColor=#ffffff;")
    GEN = "endArrow=block;endFill=0;endSize=14;html=1;rounded=0;strokeColor=#333333;edgeStyle=orthogonalEdgeStyle;"

    W, H, GAP = 280, 50, 62
    LX = 270                      # staff use cases
    BOX_X, BOX_W = 210, 1180
    ACTOR_X = 90

    # Victory, 16/10/2026: every staff account is created by an admin; managers ("quản trị viên") create and edit in
    # their areas but never delete; deletions and account changes wait for the other admin; only examiners open
    # candidates' work. The admin can do everything a manager can (generalisation). Victory 29/10 (VE-70, VE-73): mixes
    # of 2-6 source papers; examiners mark anonymous Writing scripts dealt to them and see no results; review of 2/11:
    # admins correct a published answer key, invigilators end the whole test (End all) and undo it.
    blocks = [
        ("admin", "Admin", ["Quản lý tài khoản nhân sự", "Duyệt yêu cầu xoá, đổi tài khoản", "Xem bảng điểm mọi kỳ thi",
                              "Sửa đáp án đề đã xuất bản (chấm lại)"]),
        ("mgr", "Quản trị viên", ["Nhập, kiểm tra và xuất bản đề thi", "Trộn 2–6 đề gốc thành nhiều mã đề",
                                  "Tạo kỳ thi, phân công giám thị và giám khảo", "Nhập thí sinh, in phiếu đăng nhập",
                                  "Đổi tên và logo trung tâm", "Gửi yêu cầu xoá (chờ Admin khác duyệt)"]),
        ("exm", "Giám khảo", ["Nhận bài Writing được chia ngẫu nhiên", "Chấm Writing ẩn danh theo mã bài"]),
        ("inv", "Giám thị", ["Theo dõi phòng thi thời gian thực", "Tạm dừng / cho làm tiếp", "Cộng giờ sự cố (incident time)",
                             "Sửa giờ kết thúc (End now / at / in)", "Tua lại phần nghe (Rewind audio)",
                             "Gửi thông báo cho thí sinh", "Xử lý cảnh báo", "Kết thúc cả bài thi (End all)",
                             "Khôi phục bài kết thúc nhầm"]),
    ]
    TOP = 70
    rows = 1 + sum(len(b[2]) for b in blocks)
    boundary_h = TOP + rows * GAP + 20 * len(blocks) + 60
    d.vertex("Hệ thống thi máy Victory (A2 Key for Schools, B1 Preliminary for Schools)",
             f"rounded=0;whiteSpace=wrap;html=1;fillColor=none;strokeColor=#333333;verticalAlign=top;{FONT}fontSize=16;fontStyle=1;",
             BOX_X, 0, BOX_W, boundary_h, cid="sys")

    LEFT = ASSOC + "entryX=0;entryY=0.5;entryDx=0;entryDy=0;"
    staff_login = d.vertex("Đăng nhập (nhân sự)", UC["shared"], LX, TOP, W, H)
    staff_y = TOP - 15
    staff = d.vertex("Nhân sự", ACTOR, ACTOR_X, staff_y, 40, 80)
    d.edge(staff, staff_login, LEFT)

    y = TOP + GAP + 30
    actors = {}
    for kind, name, ucs in blocks:
        ids = []
        for text in ucs:
            ids.append(d.vertex(text, UC[kind], LX, y, W, H))
            y += GAP
        actor_y = y - len(ucs) * GAP / 2 - GAP / 2 - 15
        actors[kind] = (d.vertex(name, ACTOR, ACTOR_X, actor_y, 40, 80), actor_y)
        for u in ids:
            d.edge(actors[kind][0], u, LEFT)
        y += 20

    # Generalisations run on a bus left of the actors, so they never cross an association.
    BUS = 40

    def generalise(child, parent):
        (child_id, child_y), (parent_id, parent_y) = child, parent
        d.edge(child_id, parent_id, GEN + "exitX=0;exitY=0.25;exitDx=0;exitDy=0;entryX=0;entryY=0.25;entryDx=0;entryDy=0;",
               points=[(BUS, child_y + 20), (BUS, parent_y + 20)])

    generalise(actors["admin"], actors["mgr"])
    for kind in ("mgr", "exm", "inv"):
        generalise(actors[kind], (staff, staff_y))

    # ---- right half: candidate, Safe Exam Browser, the system clock
    AX, AW = 1030, 310            # main candidate use cases
    BX, BW = 600, 270             # extensions
    RIGHT = ASSOC + "entryX=1;entryY=0.5;entryDx=0;entryDy=0;"

    seb_uc = d.vertex("Kiểm tra Config Key của SEB ở mỗi yêu cầu", UC["seb"], AX, 70, AW, H + 10)
    login = d.vertex("Đăng nhập (phiếu đăng nhập + mã PIN)", UC["cand"], AX, 190, AW, H + 10)
    ready = d.vertex("Kiểm tra sẵn sàng (Readiness check)", UC["cand"], AX, 290, AW, H)
    work = d.vertex("Làm bài thi (Reading, Writing, Listening)", UC["cand"], AX, 455, AW, 104)
    hide = d.vertex("Ẩn màn hình khi rời chỗ (menu)", UC["cand"], AX, 660, AW, H)
    msgs = d.vertex("Xem thông báo của giám thị", UC["cand"], AX, 740, AW, H)

    ext = ["Ghi chú và tô sáng", "Đánh dấu câu hỏi (cờ)", "Đổi cỡ chữ và độ tương phản",
           "Chỉnh âm lượng (Listening)", "Chọn một câu (B1 Writing Part 2)"]
    ext_ids = [d.vertex(t, UC["cand"], BX, 350 + i * 66, BW, H) for i, t in enumerate(ext)]

    seb = d.vertex("Safe Exam Browser", ACTOR, 1490, 60, 40, 80)
    cand = d.vertex("Thí sinh", ACTOR, 1490, 440, 40, 80)
    clock = d.vertex("Đồng hồ hệ thống", ACTOR, 1490, 920, 40, 80)

    d.edge(seb, seb_uc, RIGHT)
    for u in (login, ready, work, hide, msgs):
        d.edge(cand, u, RIGHT)
    d.edge(login, seb_uc, DEP, "«include»")
    # Each extension leaves from its right edge and enters "Làm bài thi" at its own point, so no two arrows meet.
    for e, entry_y in zip(ext_ids, (0.14, 0.32, 0.5, 0.68, 0.86)):
        d.edge(e, work, DEP + f"exitX=1;exitY=0.5;exitDx=0;exitDy=0;entryX=0.04;entryY={entry_y};entryDx=0;entryDy=0;", "«extend»")

    auto = d.vertex("Tự nộp bài khi hết giờ (cả máy mất mạng)", UC["sys"], AX, 860, AW, H)
    grade = d.vertex("Chấm tự động Reading, Listening", UC["sys"], AX, 940, AW, H)
    warn = d.vertex("Ghi cảnh báo (mất kết nối, lệch giờ, vào sớm)", UC["sys"], AX, 1020, AW, H)
    for u in (auto, grade, warn):
        d.edge(clock, u, RIGHT)

    # Legend
    legend_y = boundary_h + 30
    d.vertex("Chú thích", f"rounded=0;whiteSpace=wrap;html=1;fillColor=none;strokeColor=#666666;verticalAlign=top;align=left;spacingLeft=8;{FONT}fontStyle=1;",
             BOX_X, legend_y, BOX_W + 160, 60, cid="legend")
    items = [("#e1d5e7", "#9673a6", "Admin"), ("#fff2cc", "#d6b656", "Quản trị viên"),
             ("#f8cecc", "#b85450", "Giám khảo"), ("#dae8fc", "#6c8ebf", "Giám thị"),
             ("#d5e8d4", "#82b366", "Thí sinh"), ("#ffe6cc", "#d79b00", "SEB"),
             ("#eeeeee", "#555555", "Hệ thống"), ("#f5f5f5", "#666666", "Dùng chung")]
    x = 90
    for fill, stroke, label in items:
        d.vertex("", f"ellipse;html=1;fillColor={fill};strokeColor={stroke};", x, 28, 34, 18, parent="legend")
        d.vertex(label, f"text;html=1;align=left;verticalAlign=middle;{FONT}", x + 40, 25, 100, 24, parent="legend")
        x += 150
    return d.page("Use Case tổng quát")


if __name__ == "__main__":
    write(f"{OUT}/use-case.drawio", [use_case()])
    print("ok")
