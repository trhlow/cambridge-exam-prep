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
    }
    ASSOC = "endArrow=none;html=1;rounded=0;strokeColor=#333333;"
    DEP = ("endArrow=open;endSize=10;dashed=1;html=1;rounded=0;strokeColor=#333333;"
           "fontFamily=Times New Roman;fontSize=12;labelBackgroundColor=#ffffff;")
    GEN = "endArrow=block;endFill=0;endSize=14;html=1;rounded=0;strokeColor=#333333;"

    TOP, W, H, GAP = 70, 250, 50, 62
    LX = 260  # left column x (staff)

    # Victory, 16/10/2026: every staff account is created by an admin; managers ("quản trị viên") create and edit in
    # their areas but never delete; deletions and account changes wait for the other admin; only examiners open
    # candidates' work. The admin can do everything a manager can (generalisation).
    admin_ucs = ["Quản lý tài khoản nhân sự", "Duyệt yêu cầu xoá, đổi tài khoản", "Xem bảng điểm"]
    mgr_ucs = ["Nhập, kiểm tra và xuất bản đề thi", "Tạo kỳ thi, phân công giám thị và giám khảo",
               "Cấp tài khoản thí sinh, in phiếu đăng nhập", "Đổi tên và logo trung tâm",
               "Gửi yêu cầu xoá (chờ Admin duyệt)"]
    exm_ucs = ["Xem lại bài làm của thí sinh", "Chấm bài viết (Writing)", "Xem kết quả kỳ thi được phân công"]
    inv_ucs = ["Theo dõi phòng thi thời gian thực", "Tạm dừng / cho làm tiếp",
               "Cộng giờ bù do sự cố", "Sửa giờ kết thúc (End now / at / in)",
               "Tua lại phần nghe (Rewind audio)", "Xử lý cảnh báo vi phạm",
               "Gửi thông báo cho thí sinh", "Khôi phục bài kết thúc nhầm"]

    groups = [admin_ucs, mgr_ucs, ["Đăng nhập"], exm_ucs, inv_ucs]
    n_rows = sum(len(g) for g in groups)
    boundary_h = TOP + n_rows * GAP + 30
    d.vertex("Hệ thống thi máy Victory (A2 Key, B1 Preliminary)",
             f"rounded=0;whiteSpace=wrap;html=1;fillColor=none;strokeColor=#333333;verticalAlign=top;{FONT}fontSize=16;fontStyle=1;",
             200, 0, 1000, boundary_h, cid="sys")

    y = TOP
    placed = []
    for group, kind in zip(groups, ["admin", "mgr", "shared", "exm", "inv"]):
        top = y
        ids = []
        for t in group:
            ids.append(d.vertex(t, UC[kind], LX, y, W, H)); y += GAP
        placed.append((ids, top + len(group) * GAP / 2 - 50))
    (admin_ids, a_mid), (mgr_ids, m_mid), ([login], _), (exm_ids, e_mid), (inv_ids, i_mid) = placed

    admin = d.vertex("Admin", ACTOR, 60, a_mid, 40, 80)
    mgr = d.vertex("Quản trị viên", ACTOR, 60, m_mid, 40, 80)
    exm = d.vertex("Giám khảo", ACTOR, 60, e_mid, 40, 80)
    inv = d.vertex("Giám thị", ACTOR, 60, i_mid, 40, 80)
    LEFT = ASSOC + "entryX=0;entryY=0.5;entryDx=0;entryDy=0;"
    for actor, ucs in ((admin, admin_ids), (mgr, mgr_ids), (exm, exm_ids), (inv, inv_ids)):
        for u in ucs + [login]:
            d.edge(actor, u, LEFT)
    # Admin is a manager of every area, and more.
    d.edge(admin, mgr, GEN + "exitX=0.5;exitY=1;exitDx=0;exitDy=20;entryX=0.5;entryY=0;entryDx=0;entryDy=0;")

    # Right half: candidate (column A near right edge) + extensions (column B)
    AX, BX = 900, 600
    viol = d.vertex("Phát hiện và gửi cảnh báo vi phạm", UC["seb"], AX, 70, W, H)
    seb_chk = d.vertex("Xác thực cấu hình SEB (Browser Exam Key)", UC["seb"], AX - 20, 170, W + 40, H + 10)
    pin = d.vertex("Đăng nhập (tài khoản + mã PIN phòng thi)", UC["cand"], AX - 20, 300, W + 40, H + 10)
    work = d.vertex("Làm bài thi (Reading, Writing, Listening)", UC["cand"], AX - 20, 470, W + 40, H + 10)
    submit = d.vertex("Nộp bài", UC["cand"], AX, 660, W, H)

    note = d.vertex("Ghi chú và highlight", UC["cand"], BX, 390, 200, H)
    flag = d.vertex("Đánh dấu câu hỏi", UC["cand"], BX, 475, 200, H)
    a11y = d.vertex("Đổi cỡ chữ và độ tương phản", UC["cand"], BX, 560, 200, H)
    auto = d.vertex("Tự động nộp khi hết giờ", UC["cand"], BX, 690, 200, H)

    cand = d.vertex("Thí sinh", ACTOR, 1290, 440, 40, 80)
    seb = d.vertex("Safe Exam Browser", ACTOR, 1290, 90, 40, 80)
    RIGHT = ASSOC + "entryX=1;entryY=0.5;entryDx=0;entryDy=0;"
    for u in (pin, work, submit):
        d.edge(cand, u, RIGHT)
    for u in (viol, seb_chk):
        d.edge(seb, u, RIGHT)

    d.edge(pin, seb_chk, DEP, "«include»")
    for e in (note, flag, a11y):
        d.edge(e, work, DEP, "«extend»")
    d.edge(auto, submit, DEP, "«extend»")
    grade = d.vertex("Chấm tự động (Reading, Listening)", UC["cand"], AX - 20, 790, W + 40, H)
    d.edge(submit, grade, DEP, "«include»")

    # Legend
    legend_y = boundary_h + 30
    leg = d.vertex("Chú thích", f"rounded=0;whiteSpace=wrap;html=1;fillColor=none;strokeColor=#666666;verticalAlign=top;align=left;spacingLeft=8;{FONT}fontStyle=1;",
                   200, legend_y, 1000, 60, cid="legend")
    items = [("#e1d5e7", "#9673a6", "Admin"), ("#fff2cc", "#d6b656", "Quản trị viên"),
             ("#f8cecc", "#b85450", "Giám khảo"), ("#dae8fc", "#6c8ebf", "Giám thị"),
             ("#d5e8d4", "#82b366", "Thí sinh"), ("#ffe6cc", "#d79b00", "SEB"),
             ("#f5f5f5", "#666666", "Dùng chung")]
    x = 100
    for fill, stroke, label in items:
        d.vertex("", f"ellipse;html=1;fillColor={fill};strokeColor={stroke};", x, 28, 34, 18, parent="legend")
        d.vertex(label, f"text;html=1;align=left;verticalAlign=middle;{FONT}", x + 40, 25, 90, 24, parent="legend")
        x += 128
    return d.page("Use Case tổng quát")


if __name__ == "__main__":
    write(f"{OUT}/use-case.drawio", [use_case()])
    print("ok")
