"""Single source of truth for the sprint plan.

Renders sprint-backlog.md (pasted into task_plan.md "Phases") and jira-backlog.csv (Jira Cloud CSV import).
Run:  python -I sprints.py
"""
import csv
import os

HERE = os.path.dirname(os.path.abspath(__file__))

EPICS = {
    "E1": "Nền tảng & cơ sở dữ liệu",
    "E2": "Xác thực & phân quyền",
    "E3": "Nhập đề & tài nguyên (audio, hình)",
    "E4": "Tổ chức kỳ thi (quản trị)",
    "E5": "Màn hình thí sinh",
    "E6": "Listening",
    "E7": "Cổng giám thị thời gian thực",
    "E8": "Chấm điểm & kết quả",
    "E9": "Safe Exam Browser & bảo mật",
    "E10": "B1 Preliminary for Schools",
    "E11": "Kiểm thử & buổi thi thử",
    "E12": "Báo cáo đồ án & tài liệu",
    "E13": "IELTS (nếu kịp)",
    "E14": "Phân quyền nhiều tác nhân và duyệt chéo hai admin",
    "E15": "Trộn đề: 2 đề gốc thành 4 đề, hai máy kế nhau khác đề",
}

# (sprint, start, end, goal, stories); story = (key, epic, points, summary, acceptance)
# Re-planned 15/10/2026 to match Jira (board SCRUM): ahead of schedule, so later stories moved up one sprint.
# 16/10: Victory asked for more roles and two-admin approval (E14, VE-59…63, Sprint 2); VE-13 and VE-25 moved to Sprint 3.
# 20/10: Victory asked for paper shuffling (E15, VE-64…68): two source papers per section mixed part by part
# into four papers, neighbouring computers never get the same one; A2 in Sprints 3–4, B1 with E10 in Sprint 6.
# On Jira, Sprint 0 and Sprint 1 run as one sprint, "SCRUM Sprint 1" (10/10 – 18/10).
SPRINTS = [
    ("Sprint 0", "2026-10-10", "2026-10-11", "Khởi động: CSDL và khảo sát giao diện", [
        ("VE-1", "E1", 5, "Thiết kế CSDL 21 bảng, migration EF Core trên MySQL", "Migration áp được; 16 test tích hợp đạt; ERD sinh từ schema thật"),
        ("VE-2", "E5", 2, "Khảo sát giao diện bài thi mẫu A2 Key for Schools trên Inspera", "thesis/refs/inspera-ui-a2.md mô tả đủ 7 part R&W, 5 part Listening, menu, ghi chú"),
    ]),
    ("Sprint 1", "2026-10-12", "2026-10-18", "Demo A2 phía thí sinh (16/10); làm sớm cổng nhân viên, chấm Writing, nhập đề từ PDF", [
        ("VE-3", "E2", 5, "Đăng nhập nhân viên (admin, giám thị) bằng cookie, phân quyền theo vai trò", "Sai mật khẩu bị từ chối; API quản trị trả 403 với giám thị"),
        ("VE-4", "E2", 5, "Đăng nhập thí sinh bằng username/password + mã PIN phòng thi", "Sai PIN không vào được; đúng PIN thấy danh sách phần thi"),
        ("VE-5", "E3", 5, "Định dạng gói đề JSON (+audio, hình) và import đề A2", "Import gói mẫu tạo đủ part, nhóm câu, câu hỏi, đáp án; gói sai báo lỗi rõ ràng"),
        ("VE-56", "E3", 3, "Bộ chuyển đề PDF → gói đề JSON (A2) và tách audio Listening thành 5 track", "Đề mẫu A2 2022 chuyển ra gói import được; người duyệt sửa ít; đề thật không vào git"),
        ("VE-57", "E3", 5, "Nhập đề từ PDF ngay trên cổng nhân viên và trang duyệt đề", "Admin tải PDF đề, đáp án (và 5 file nghe) trên web; đề vào Draft, duyệt từng câu trước khi Publish"),
        ("VE-6", "E5", 3, "Dựng frontend React + TypeScript (Vite), khung màn hình kiểu Inspera", "Thanh trên (logo, Candidate ID, 4 biểu tượng), thanh part 'n of m', nút ← →"),
        ("VE-7", "E5", 8, "Reading & Writing A2: 4 kiểu câu (chọn đáp án, chọn từ trên ô trống, gõ từ, viết bài)", "7 part hiển thị đúng bố cục một cột / chia đôi có thanh kéo; đếm từ ở Part 6–7"),
        ("VE-8", "E5", 5, "Đồng hồ do server nắm, tự lưu câu trả lời, hết giờ tự nộp", "Tải lại trang vẫn đúng giờ còn lại và đáp án; hết giờ chuyển Submitted"),
        ("VE-9", "E5", 2, "Đánh dấu câu hỏi (cờ) và đếm câu đã làm trên thanh part", "Cờ hiện ở thanh dưới; 'Part 2  3 of 7' cập nhật ngay"),
        ("VE-10", "E6", 5, "Listening A2: phát nối 5 track một lần, chọn tranh, kéo thả", "Không tạm dừng/tua được; chuyển part không làm dừng audio"),
        ("VE-11", "E8", 3, "Chấm tự động Reading Part 1–5 và Listening theo đáp án", "Đáp án thay thế được chấp nhận; không phân biệt hoa thường"),
        ("VE-14", "E7", 5, "Tạm dừng / cho làm tiếp một, nhiều hoặc cả phòng", "Chỉ tác động thí sinh online và đang làm; đồng hồ không trừ giờ khi tạm dừng"),
        ("VE-15", "E7", 5, "Cộng giờ bù sự cố và Edit End Time (End now / End at / End in, Disregard extra time)", "End now không hoàn tác ở cấp phần thi; mọi thao tác ghi nhật ký"),
        ("VE-16", "E7", 3, "Tua audio Listening N phút (chỉ khi không tạm dừng, tối đa số phút đã phát)", "Nhắc giám thị cộng giờ bù tương ứng sau khi tua"),
        ("VE-17", "E7", 2, "Thông báo từ giám thị (biểu tượng chuông)", "Gửi cho một thí sinh hoặc cả phòng; thí sinh thấy ngay"),
        ("VE-18", "E5", 3, "Menu Options: độ tương phản, cỡ chữ, ẩn/hiện giờ, trang nộp bài", "3 chế độ màu, 3 cỡ chữ; màn hình Options che bài làm"),
        ("VE-19", "E5", 5, "Ghi chú và highlight khi bôi đen chữ, bảng Notes bên phải", "Notes tắt khi chọn cỡ chữ Large/Extra large"),
        ("VE-21", "E4", 5, "Tạo / sửa kỳ thi, gắn đề cho từng phần thi, mã PIN, mật khẩu giám thị", "Kỳ thi A2 có 2 phần thi theo mẫu; không xoá được đề đang dùng"),
        ("VE-22", "E4", 5, "Nhập danh sách thí sinh từ Excel, sinh username kiểu Cambridge, in phiếu đăng nhập", "Username = 5 chữ họ + chữ đầu tên + số báo danh; in lại được phiếu"),
        ("VE-23", "E4", 2, "Phân công giám thị cho kỳ thi", "Giám thị chỉ thấy kỳ thi được phân công"),
        ("VE-26", "E8", 5, "Chấm Writing cho giáo viên và bảng kết quả theo kỳ thi", "Tổng điểm = tự động + Writing; xuất kết quả ra Excel"),
        ("VE-30", "E9", 3, "Giới hạn tần suất đăng nhập, xử lý lỗi an toàn, nhật ký thao tác", "Không lộ stack trace; đăng nhập sai liên tục bị chặn tạm"),
        ("VE-12", "E12", 1, "Báo cáo tiến độ tuần 1 (progress-report)", "File báo cáo theo mẫu 3 có trong progress-report/"),
    ]),
    ("Sprint 2", "2026-10-19", "2026-10-25", "Tuần gặp GV 1: phân quyền nhiều tác nhân, duyệt chéo hai admin (Victory 16/10); đặc tả, sơ đồ, đặc tả API; hướng dẫn cài đặt", [
        ("VE-58", "E12", 2, "Bản đặc tả yêu cầu (quy trình thi máy, vai trò, chức năng, yêu cầu phi chức năng)", "Bản đặc tả trong thesis/doc, đối chiếu được với use case và đề cương"),
        ("VE-45", "E12", 3, "Sơ đồ trạng thái lượt làm bài, sơ đồ tuần tự, đặc tả API", "Các sơ đồ có trong thesis/diagrams"),
        ("VE-59", "E14", 5, "Mô hình quyền: Admin, Quản trị viên theo mảng, Giám khảo, Giám thị", "Mỗi API kiểm tra đúng vai trò và mảng; quản trị viên tạo và sửa, không xoá"),
        ("VE-60", "E14", 8, "Duyệt chéo hai admin: yêu cầu xoá và thao tác tài khoản chờ duyệt", "Xoá và thao tác tài khoản chờ một admin khác duyệt; không tự duyệt; có nhật ký"),
        ("VE-61", "E14", 5, "Giám khảo: xem bài làm, chấm Writing và xem kết quả theo kỳ thi được phân công", "Chỉ giám khảo xem bài làm; admin chỉ xem điểm tổng; giám thị không xem kết quả"),
        ("VE-62", "E14", 5, "Cổng nhân viên theo quyền: tài khoản theo vai trò/mảng, trang yêu cầu chờ duyệt, nhật ký", "Menu theo quyền; trang Chờ duyệt và nhật ký cho admin"),
        ("VE-63", "E14", 2, "Cập nhật sơ đồ use case, ERD và tài liệu cho các tác nhân mới", "Use case có 5 tác nhân và luồng duyệt chéo; ERD sinh lại"),
        ("VE-46", "E11", 3, "Đóng gói triển khai (Docker) và hướng dẫn cài đặt trong setup/", "Máy mới cài theo hướng dẫn chạy được"),
        ("VE-20", "E12", 1, "Báo cáo tiến độ tuần 2", "File báo cáo có trong progress-report/"),
    ]),
    ("Sprint 3", "2026-10-26", "2026-11-01", "SignalR; sửa đề trên web; Safe Exam Browser và cảnh báo vi phạm; tên trung tâm và logo; trộn đề A2 (Victory 20/10)", [
        ("VE-13", "E7", 5, "SignalR hub: trạng thái thí sinh, phút còn lại, cảnh báo cập nhật tức thì", "Bảng giám thị đổi trong ≤2 giây khi thí sinh làm bài, mất kết nối hoặc nộp"),
        ("VE-25", "E3", 8, "Giao diện nhập và sửa đề (part, nhóm câu, câu hỏi, đáp án, upload audio/hình)", "Tạo được một đề A2 đầy đủ không cần file JSON; sửa được bản nháp nhập từ PDF"),
        ("VE-28", "E9", 5, "Kiểm tra cấu hình Safe Exam Browser (config key) trước khi cho vào thi", "Mở bằng trình duyệt thường bị chặn và sinh cảnh báo"),
        ("VE-29", "E9", 3, "Cảnh báo vi phạm phía server: thoát trình duyệt khoá, mất kết nối, lệch đồng hồ, làm sớm", "Cảnh báo hiện ở cột Warnings, giám thị đánh dấu đã xử lý"),
        ("VE-24", "E4", 2, "Đổi tên trung tâm và logo (tải PNG)", "Logo mới hiện trên thanh trên của thí sinh"),
        ("VE-64", "E15", 3, "Đề gốc thứ hai cho A2 (A2 Key sample): tách audio 5 part, chuyển PDF thành gói, kiểm tra hai đề cùng khuôn", "Hai đề gốc A2 (R&W, Listening) đã Publish; hai đề khác khuôn (số part, số câu, kiểu câu từng part) bị từ chối với lý do rõ ràng"),
        ("VE-65", "E15", 8, "Sinh 4 đề trộn từ 2 đề gốc: mỗi part lấy nguyên từ đề gốc 1 hoặc 2 (kèm hình, audio, đáp án)", "4 đề khác nhau từng đôi, đề nào cũng có part của cả hai đề gốc; là đề Published bình thường, chấm tự động đúng; cùng seed ra cùng kết quả"),
        ("VE-66", "E15", 5, "Sơ đồ phòng máy và chia đề theo vị trí máy (ô 2×2): hai máy kề nhau ngang, dọc, chéo luôn khác đề", "Test tự động kiểm tra mọi cặp máy kề nhau; thí sinh đổi máy giữa giờ vẫn giữ đề đã bắt đầu"),
        ("VE-27", "E12", 1, "Báo cáo tiến độ tuần 3", "File báo cáo có trong progress-report/"),
    ]),
    ("Sprint 4", "2026-11-02", "2026-11-08", "Kiểm thử đầu-cuối A2; bắt đầu B1 Preliminary for Schools", [
        ("VE-31", "E11", 5, "Test đầu-cuối Playwright cho luồng thi A2 và thao tác giám thị chính", "Chạy được bằng một lệnh; bao phủ đăng nhập → làm bài → nộp → chấm"),
        ("VE-33", "E10", 2, "Khảo sát giao diện bài thi mẫu B1 PfS (Reading, Writing, Listening)", "thesis/refs/inspera-ui-b1.md đầy đủ như bản A2"),
        ("VE-67", "E15", 5, "Cổng nhân viên cho trộn đề: chọn 2 đề gốc, xem 4 đề trộn, in sơ đồ phòng kèm mã đề; mã đề trên trang giám thị và bảng kết quả", "Quản trị viên kỳ thi trộn đề trên web; giám thị và giám khảo thấy mã đề từng thí sinh; giám khảo chấm theo đúng đề của thí sinh"),
        ("VE-34", "E10", 5, "Bổ sung kiểu câu còn thiếu cho B1 Reading (6 part)", "Import và làm được đề Reading B1 mẫu"),
        ("VE-32", "E12", 1, "Báo cáo tiến độ tuần 4", "File báo cáo có trong progress-report/"),
    ]),
    ("Sprint 5", "2026-11-09", "2026-11-15", "B1 Preliminary for Schools: Writing và Listening — tuần gặp GV 2", [
        ("VE-35", "E10", 3, "B1 Writing: Part 1 email, Part 2 chọn 1 trong 2 đề", "Thí sinh chọn đề Part 2 trước khi viết; đếm từ"),
        ("VE-37", "E10", 3, "B1 Listening 4 part", "Import và làm được đề Listening B1 mẫu"),
        ("VE-36", "E12", 1, "Báo cáo tiến độ tuần 5", "File báo cáo có trong progress-report/"),
    ]),
    ("Sprint 6", "2026-11-16", "2026-11-22", "Kỳ thi B1 chạy đầu-cuối, sửa lỗi tồn đọng A2/B1", [
        ("VE-38", "E10", 3, "Kỳ thi B1 3 phần thi chạy đầu-cuối, chấm tự động", "Test Playwright cho luồng B1"),
        ("VE-68", "E15", 3, "Trộn đề B1 (Reading, Writing, Listening) từ hai đề gốc B1 Preliminary 2022 và B1 Preliminary for Schools", "Kỳ thi B1 có 4 đề trộn mỗi phần thi; test Playwright cho máy kề nhau khác đề"),
        ("VE-39", "E11", 3, "Sửa lỗi tồn đọng A2/B1", "Không còn lỗi mức Cao trên backlog"),
        ("VE-40", "E12", 1, "Báo cáo tiến độ tuần 6", "File báo cáo có trong progress-report/"),
    ]),
    ("Sprint 7", "2026-11-23", "2026-11-29", "Kiểm thử tải và buổi thi thử", [
        ("VE-41", "E11", 5, "Kiểm thử tải ~100 thí sinh đồng thời (SignalR + lưu bài)", "Số liệu độ trễ và lỗi ghi vào báo cáo"),
        ("VE-42", "E11", 3, "Buổi thi thử với các bạn đóng vai thí sinh và giám thị", "Biên bản buổi thi thử và danh sách lỗi phát hiện"),
        ("VE-43", "E11", 3, "Sửa lỗi sau thi thử", "Lỗi mức Cao đã đóng"),
        ("VE-44", "E12", 1, "Báo cáo tiến độ tuần 7", "File báo cáo có trong progress-report/"),
    ]),
    ("Sprint 8", "2026-11-30", "2026-12-06", "Viết quyển báo cáo Mở đầu, Chương 1–3 — tuần gặp GV 3", [
        ("VE-47", "E12", 5, "Viết quyển báo cáo: Mở đầu, Chương 1–3", "Bản nháp theo mẫu của Khoa"),
    ]),
    ("Sprint 9", "2026-12-07", "2026-12-13", "IELTS (nếu kịp) hoặc dự phòng", [
        ("VE-48", "E13", 5, "IELTS Listening / Reading: kiểu câu nhiều đáp án, ghi nhãn bản đồ", "Làm được một đề IELTS mẫu tự soạn"),
        ("VE-49", "E13", 3, "IELTS Writing Task 1/2 (Academic, General Training)", "Đếm từ, ảnh đề bài cho Task 1"),
        ("VE-50", "E12", 1, "Báo cáo tiến độ tuần 9", "File báo cáo có trong progress-report/"),
    ]),
    ("Sprint 10", "2026-12-14", "2026-12-20", "Viết báo cáo: kết quả và kết luận", [
        ("VE-51", "E12", 5, "Viết Chương 4 (kết quả, ảnh giao diện, số liệu kiểm thử) và Chương 5", "Bản nháp hoàn chỉnh gửi GV"),
        ("VE-52", "E12", 1, "Báo cáo tiến độ tuần 10", "File báo cáo có trong progress-report/"),
    ]),
    ("Sprint 11", "2026-12-21", "2026-12-27", "Hoàn chỉnh và nộp — tuần gặp GV 4", [
        ("VE-53", "E12", 3, "Chỉnh sửa quyển báo cáo theo góp ý GV, xuất PDF", "Bản cuối trong thesis/pdf"),
        ("VE-54", "E12", 2, "Slide trình bày và video demo (thesis/abs)", "Slide và video sẵn sàng"),
        ("VE-55", "E12", 1, "Cập nhật README cuối cùng, nộp link GitHub, nộp quyển 31/12", "README đủ thông tin; đã nộp"),
    ]),
]

DONE = {"VE-1", "VE-2", "VE-3", "VE-4", "VE-5", "VE-6", "VE-7", "VE-8", "VE-9", "VE-10", "VE-11", "VE-12", "VE-14", "VE-15", "VE-16", "VE-17", "VE-18", "VE-19", "VE-21", "VE-22", "VE-23", "VE-26", "VE-30", "VE-56", "VE-57",
        "VE-13", "VE-20", "VE-24", "VE-25", "VE-28", "VE-29", "VE-31", "VE-45", "VE-46", "VE-58", "VE-59", "VE-60", "VE-61", "VE-62", "VE-63",
        "VE-64", "VE-65", "VE-66", "VE-27", "VE-67", "VE-33", "VE-34", "VE-32"}


def render_markdown() -> str:
    lines = []
    for name, start, end, goal, stories in SPRINTS:
        points = sum(s[2] for s in stories)
        done = all(s[0] in DONE for s in stories)
        lines.append(f"### Phase {name.split()[-1]}: {name} ({start[8:]}/{start[5:7]} – {end[8:]}/{end[5:7]}) — {goal}")
        lines.append(f"Tổng: {len(stories)} story, {points} điểm.")
        for key, epic, pts, summary, _ in stories:
            mark = "x" if key in DONE else " "
            lines.append(f"- [{mark}] {key} [{epic}] ({pts}đ) {summary}")
        lines.append(f"- **Status:** {'complete' if done else 'pending'}")
        lines.append("")
    return "\n".join(lines)


def write_csv(path: str) -> None:
    with open(path, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f)
        w.writerow(["Issue Id", "Parent Id", "Issue Type", "Summary", "Description", "Sprint", "Story Points", "Labels"])
        epic_ids = {}
        for i, (code, name) in enumerate(EPICS.items(), start=1):
            epic_ids[code] = str(i)
            w.writerow([i, "", "Epic", f"{code} · {name}", "", "", "", "epic"])
        next_id = len(EPICS) + 1
        for name, _, _, _, stories in SPRINTS:
            for key, epic, pts, summary, acceptance in stories:
                desc = f"Mã kế hoạch: {key}\n\nTiêu chí chấp nhận:\n- {acceptance}"
                w.writerow([next_id, epic_ids[epic], "Story", summary, desc, name, pts, key])
                next_id += 1


if __name__ == "__main__":
    with open(os.path.join(HERE, "sprint-backlog.md"), "w", encoding="utf-8", newline="\n") as f:
        f.write(render_markdown())
    write_csv(os.path.join(HERE, "jira-backlog.csv"))
    total = sum(len(s[4]) for s in SPRINTS)
    print(f"{len(SPRINTS)} sprints, {len(EPICS)} epics, {total} stories -> sprint-backlog.md, jira-backlog.csv")
