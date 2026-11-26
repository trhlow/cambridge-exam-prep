"""Generate the figures of report chapters 1-3 into thesis/diagrams/report/*.drawio.

    python -I thesis/diagrams/tools/gen_report_figs.py        (repo root)

Export each with draw.io desktop (see CLAUDE.md), e.g. from thesis/diagrams/report:
    "/d/draw.io/draw.io.exe" -x -f png -e -s 2 -b 10 -o exam-day.drawio.png exam-day.drawio
"""
import os
from xml.sax.saxutils import escape

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "report")
FONT = "fontFamily=Times New Roman;fontSize=15;"
BOX = f"rounded=1;arcSize=12;whiteSpace=wrap;html=1;{FONT}fillColor=#dae8fc;strokeColor=#6c8ebf;"
STEP = f"rounded=1;arcSize=12;whiteSpace=wrap;html=1;{FONT}fillColor=#ffffff;strokeColor=#6c8ebf;align=left;spacingLeft=56;spacingRight=10;"
NUM = f"ellipse;html=1;{FONT}fontStyle=1;fillColor=#6c8ebf;strokeColor=#6c8ebf;fontColor=#ffffff;"
NOTE = f"text;html=1;whiteSpace=wrap;{FONT}fontSize=13;align=center;verticalAlign=middle;"
EDGE = "endArrow=block;endFill=1;endSize=8;html=1;rounded=1;strokeColor=#333333;strokeWidth=1.5;" + FONT + "fontSize=13;labelBackgroundColor=#ffffff;"


def q(s):
    return escape(s, {'"': "&quot;", "\n": "&#xa;"})


class Doc:
    def __init__(self):
        self.cells, self.n = [], 1

    def vertex(self, value, style, x, y, w, h):
        self.n += 1
        cid = f"v{self.n}"
        self.cells.append(f'<mxCell id="{cid}" value="{q(value)}" style="{style}" vertex="1" parent="1">'
                          f'<mxGeometry x="{x}" y="{y}" width="{w}" height="{h}" as="geometry"/></mxCell>')
        return cid

    def edge(self, src, tgt, label="", style=""):
        self.n += 1
        self.cells.append(f'<mxCell id="e{self.n}" value="{q(label)}" style="{EDGE}{style}" edge="1" parent="1" '
                          f'source="{src}" target="{tgt}"><mxGeometry relative="1" as="geometry"/></mxCell>')

    def save(self, name):
        body = "".join(self.cells)
        xml = ('<mxfile host="gen_report_figs.py"><diagram name="Page-1" id="p1"><mxGraphModel dx="1200" dy="800" grid="0" '
               'gridSize="10" guides="1" tooltips="1" connect="1" arrows="1" fold="1" page="0" pageScale="1" '
               'pageWidth="1100" pageHeight="850" math="0" shadow="0"><root><mxCell id="0"/><mxCell id="1" parent="0"/>'
               f'{body}</root></mxGraphModel></diagram></mxfile>')
        os.makedirs(OUT, exist_ok=True)
        with open(os.path.join(OUT, name), "w", encoding="utf-8") as f:
            f.write(xml)
        print("wrote", name)


def exam_day():
    """Figure 1.1: one computer-based exam session, from the Cambridge exam day booklet and invigilator guide.
    Steps run top to bottom so the figure fits the width of a portrait page."""
    d = Doc()
    steps = [
        "Chuẩn bị phòng thi: mở trình duyệt khoá trên mọi máy thí sinh, vào trang đăng nhập",
        "Thí sinh đăng nhập bằng tên đăng nhập và mật khẩu in sẵn",
        "Giám thị đọc mã PIN, thí sinh nhập PIN để mở bài thi",
        "Thí sinh làm lần lượt từng phần thi, đồng hồ đếm ngược",
        "Giám thị theo dõi cả phòng: tạm dừng, cộng giờ sự cố, sửa giờ kết thúc, tua lại audio",
        "Hết giờ hoặc giám thị kết thúc: bài được nộp, chuyển sang phần thi tiếp theo",
    ]
    ids = []
    for i, text in enumerate(steps):
        y = 20 + i * 100
        ids.append(d.vertex(text, STEP, 20, y, 660, 64))
        d.vertex(str(i + 1), NUM, 32, y + 14, 36, 36)
    for a, b in zip(ids, ids[1:]):
        d.edge(a, b)
    d.save("exam-day.drawio")


def clean_architecture():
    """Figure 2.1: Clean Architecture, after Microsoft's Common web application architectures: the core below, the
    two outer layers above it, every compile-time dependency pointing down into the core."""
    d = Doc()
    ui = d.vertex("Lớp giao diện / API\n(Controllers, Filters, Middleware,\nProgram.cs: composition root)", BOX, 20, 20, 250, 110)
    infra = d.vertex("Infrastructure\n(DbContext, Migrations,\ntruy cập dữ liệu, dịch vụ hạ tầng)",
                     BOX.replace("#dae8fc", "#fff2cc").replace("#6c8ebf", "#d6b656"), 430, 20, 250, 110)
    core = d.vertex("Application Core\n(Entities, Interfaces,\nDomain Services, quy tắc nghiệp vụ)",
                    BOX.replace("#dae8fc", "#d5e8d4").replace("#6c8ebf", "#82b366"), 205, 270, 290, 110)
    d.edge(ui, core, "phụ thuộc\nlúc biên dịch", "exitX=0.5;exitY=1;entryX=0.25;entryY=0;")
    d.edge(infra, core, "cài đặt\ncác interface", "exitX=0.5;exitY=1;entryX=0.75;entryY=0;")
    d.edge(ui, infra, "", "dashed=1;")
    d.vertex("chỉ nối lúc chạy\n(dependency injection)", NOTE, 270, 14, 160, 50)  # above the line, not on it
    d.vertex("Mũi tên liền: phụ thuộc lúc biên dịch, luôn hướng vào Application Core. "
             "Mũi tên đứt: phụ thuộc chỉ có lúc chạy.", NOTE, 20, 400, 660, 44)
    d.save("clean-architecture.drawio")


def signalr():
    """Figure 2.2: server push through a SignalR hub and the transport fallback order."""
    d = Doc()
    hub = d.vertex("Máy chủ ASP.NET Core\nHub SignalR", BOX, 220, 20, 260, 80)
    clients = [d.vertex(t, BOX.replace("#dae8fc", "#ffffff"), 20 + i * 240, 200, 180, 60)
               for i, t in enumerate(["Trình duyệt 1", "Trình duyệt 2", "Trình duyệt 3"])]
    for c in clients:
        d.edge(hub, c, "", "startArrow=block;startFill=1;")
    d.vertex("Máy chủ gọi phương thức ở client (đẩy dữ liệu), client gọi phương thức ở hub", NOTE, 20, 275, 660, 44)
    order = [("WebSockets", "#d5e8d4", "#82b366"), ("Server-Sent Events", "#fff2cc", "#d6b656"), ("Long Polling", "#f8cecc", "#b85450")]
    prev = None
    for i, (t, fill, stroke) in enumerate(order):
        b = d.vertex(t, BOX.replace("#dae8fc", fill).replace("#6c8ebf", stroke), 20 + i * 240, 350, 180, 50)
        if prev:
            d.edge(prev, b, "")
        prev = b
    d.vertex("Thứ tự chọn phương thức truyền: dùng phương thức đầu tiên mà cả máy chủ và trình duyệt hỗ trợ", NOTE, 20, 410, 660, 44)
    d.save("signalr.drawio")


def test_pyramid():
    """Figure 2.3: the test pyramid (Cohn 2009, Fowler 2012)."""
    d = Doc()
    top = d.vertex("Kiểm thử giao diện /\nđầu-cuối (E2E)", f"triangle;direction=north;whiteSpace=wrap;html=1;{FONT}fillColor=#f8cecc;strokeColor=#b85450;verticalAlign=bottom;spacingBottom=6;", 330, 20, 300, 150)
    mid = d.vertex("Kiểm thử tích hợp / tầng dịch vụ (API, cơ sở dữ liệu)", f"shape=trapezoid;perimeter=trapezoidPerimeter;fixedSize=1;size=75;whiteSpace=wrap;html=1;{FONT}fillColor=#fff2cc;strokeColor=#d6b656;", 255, 170, 450, 100)
    low = d.vertex("Kiểm thử đơn vị (unit test)", f"shape=trapezoid;perimeter=trapezoidPerimeter;fixedSize=1;size=75;whiteSpace=wrap;html=1;{FONT}fillColor=#d5e8d4;strokeColor=#82b366;", 180, 270, 600, 100)
    d.vertex("Ít test, chạy chậm,\ndễ vỡ khi giao diện đổi", NOTE + "align=left;", 800, 60, 230, 60)
    d.vertex("Nhiều test, chạy nhanh,\nchi phí viết thấp", NOTE + "align=left;", 800, 290, 230, 60)
    d.edge(d.vertex("", "text;html=1;", 790, 330, 1, 1), d.vertex("", "text;html=1;", 790, 70, 1, 1), "", "endArrow=block;strokeColor=#666666;")
    del top, mid, low
    d.save("test-pyramid.drawio")


def deployment():
    """Figure 3.x: how the system is deployed in the exam room (docker/docker-compose.yml and the production override).
    The computers sit above the server, and the three containers are stacked inside it."""
    d = Doc()
    white = BOX.replace("#dae8fc", "#ffffff")
    cand = d.vertex("Máy thí sinh (× N)\nWindows, Safe Exam Browser", white, 20, 20, 300, 70)
    staff = d.vertex("Máy giám thị, nhân viên\nChrome hoặc Edge", white, 380, 20, 300, 70)
    d.vertex("Máy chủ phòng thi: Docker Compose", f"rounded=1;arcSize=4;whiteSpace=wrap;html=1;{FONT}fontStyle=1;"
             "fillColor=none;strokeColor=#666666;dashed=1;verticalAlign=top;spacingTop=8;", 20, 160, 660, 450)
    web = d.vertex("web: nginx\ntệp tĩnh React; chuyển tiếp /api, /hubs", BOX, 60, 210, 340, 80)
    api = d.vertex("api: ASP.NET Core (.NET 10)\nREST API, hub SignalR,\ndịch vụ nền nộp bài và chấm,\nbộ chuyển đề Python",
                   BOX.replace("#dae8fc", "#d5e8d4").replace("#6c8ebf", "#82b366"), 60, 340, 340, 120)
    db = d.vertex("mysql: MySQL 8.4\nchỉ mở ở 127.0.0.1", BOX.replace("#dae8fc", "#fff2cc").replace("#6c8ebf", "#d6b656"),
                  60, 510, 340, 70)
    cyl = f"shape=cylinder3;whiteSpace=wrap;html=1;boundedLbl=1;size=10;{FONT}fontSize=13;fillColor=#f5f5f5;strokeColor=#666666;"
    files = d.vertex("App_Data: hình, audio,\nkhoá Data Protection", cyl, 450, 360, 200, 80)
    data = d.vertex("mysql-data", cyl, 450, 505, 200, 80)
    d.edge(cand, web, "HTTP(S), WebSocket", "exitX=0.5;exitY=1;entryX=0.3;entryY=0;")
    d.edge(staff, web, "/staff", "exitX=0.5;exitY=1;entryX=0.85;entryY=0;")
    d.edge(web, api, "")
    d.edge(api, db, "EF Core")
    d.edge(api, files, "", "dashed=1;endArrow=none;")
    d.edge(db, data, "", "dashed=1;endArrow=none;")
    d.vertex("Bản demo: HTTP cổng 8080, môi trường Development. Bản thi thật: thêm docker-compose.prod.yml, "
             "HTTPS cổng 443, môi trường Production.", NOTE, 20, 625, 660, 44)
    d.save("deployment.drawio")


if __name__ == "__main__":
    exam_day()
    clean_architecture()
    signalr()
    test_pyramid()
    deployment()
