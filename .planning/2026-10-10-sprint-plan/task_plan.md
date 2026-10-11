# Task Plan: Kế hoạch sprint — Hệ thống thi máy Victory

## Goal
Giao hệ thống thi máy cho phòng thi của Victory, tái hiện trải nghiệm Cambridge/Inspera: A2 Key for Schools chạy đầy đủ trước (demo 16/10/2026), rồi B1 Preliminary for Schools, IELTS nếu kịp, và nộp quyển báo cáo ngày 31/12/2026.

## Next Step
Người dùng (11/10) yêu cầu làm cổng admin và giám thị ngay, kéo việc của Sprint 2–3 lên trước. Thứ tự:
1. API admin (VE-21 kỳ thi + gắn đề + PIN/mật khẩu giám thị, danh sách đề + xuất bản; VE-22 thí sinh + phiếu đăng nhập; VE-23 tài khoản giám thị + phân công).
2. Giao diện admin `/staff` (đăng nhập nhân viên, Đề thi, Kỳ thi, Thí sinh, Giám thị, Kết quả).
3. API + màn hình giám thị theo Inspera "child test monitoring": bảng thí sinh theo phần thi, chọn nhiều → Pause/Resume, Incident extra time, Edit End Time, Rewind audio, thông báo (VE-13…17); làm mới bằng polling trước, SignalR sau.
Còn treo: VE-56 Listening, VE-12 báo cáo tuần.

## Current Phase
Phase 1 (Sprint 1, 12/10 – 18/10)

## Nhịp làm việc
- Sprint 1 tuần, **thứ Hai → Chủ nhật**. Chủ nhật: chốt sprint, viết báo cáo tiến độ tuần, **push lên GitHub** (chỉ push khi người dùng yêu cầu).
- Commit sau mỗi story xong (Khoa chấm lịch sử commit hằng tuần).
- Story points: 1 ≈ vài giờ, 3 ≈ một buổi, 5 ≈ một ngày, 8 ≈ hơn một ngày.
- Nguồn duy nhất của backlog là `sprints.py`; sửa ở đó rồi chạy `python -I sprints.py` để sinh lại `sprint-backlog.md` và `jira-backlog.csv`, sau đó chép phần Phases bên dưới.

## Epics
| Mã | Epic |
|---|---|
| E1 | Nền tảng & cơ sở dữ liệu |
| E2 | Xác thực & phân quyền |
| E3 | Nhập đề & tài nguyên (audio, hình) |
| E4 | Tổ chức kỳ thi (quản trị) |
| E5 | Màn hình thí sinh |
| E6 | Listening |
| E7 | Cổng giám thị thời gian thực |
| E8 | Chấm điểm & kết quả |
| E9 | Safe Exam Browser & bảo mật |
| E10 | B1 Preliminary for Schools |
| E11 | Kiểm thử & buổi thi thử |
| E12 | Báo cáo đồ án & tài liệu |
| E13 | IELTS (nếu kịp) |

## Phases
### Phase 0: Sprint 0 (10/10 – 11/10) — Khởi động: CSDL và khảo sát giao diện
Tổng: 2 story, 7 điểm.
- [x] VE-1 [E1] (5đ) Thiết kế CSDL 21 bảng, migration EF Core trên MySQL
- [x] VE-2 [E5] (2đ) Khảo sát giao diện bài thi mẫu A2 Key for Schools trên Inspera
- **Status:** complete

### Phase 1: Sprint 1 (12/10 – 18/10) — Demo A2 phía thí sinh vào thứ Sáu 16/10
Tổng: 10 story, 42 điểm.
- [x] VE-3 [E2] (5đ) Đăng nhập nhân viên (admin, giám thị) bằng cookie, phân quyền theo vai trò
- [x] VE-4 [E2] (5đ) Đăng nhập thí sinh bằng username/password + mã PIN phòng thi
- [x] VE-5 [E3] (5đ) Định dạng gói đề JSON (+audio, hình) và import đề A2
- [ ] VE-56 [E3] (3đ) Bộ chuyển đề PDF → gói đề JSON (A2) và tách audio Listening thành 5 track — R&W xong 11/10, còn Listening
- [x] VE-6 [E5] (3đ) Dựng frontend React + TypeScript (Vite), khung màn hình kiểu Inspera
- [x] VE-7 [E5] (8đ) Reading & Writing A2: 4 kiểu câu (chọn đáp án, chọn từ trên ô trống, gõ từ, viết bài)
- [x] VE-8 [E5] (5đ) Đồng hồ do server nắm, tự lưu câu trả lời, hết giờ tự nộp
- [x] VE-9 [E5] (2đ) Đánh dấu câu hỏi (cờ) và đếm câu đã làm trên thanh part
- [x] VE-10 [E6] (5đ) Listening A2: phát nối 5 track một lần, chọn tranh, kéo thả
- [x] VE-11 [E8] (3đ) Chấm tự động Reading Part 1–5 và Listening theo đáp án
- [ ] VE-12 [E12] (1đ) Báo cáo tiến độ tuần 1 (progress-report)
- **Status:** in_progress

### Phase 2: Sprint 2 (19/10 – 25/10) — Cổng giám thị thời gian thực (tuần gặp GV 1)
Tổng: 8 story, 29 điểm.
- [ ] VE-13 [E7] (5đ) SignalR hub: trạng thái thí sinh, phút còn lại, cảnh báo cập nhật tức thì
- [ ] VE-14 [E7] (5đ) Tạm dừng / cho làm tiếp một, nhiều hoặc cả phòng
- [ ] VE-15 [E7] (5đ) Cộng giờ bù sự cố và Edit End Time (End now / End at / End in, Disregard extra time)
- [ ] VE-16 [E7] (3đ) Tua audio Listening N phút (chỉ khi không tạm dừng, tối đa số phút đã phát)
- [ ] VE-17 [E7] (2đ) Thông báo từ giám thị (biểu tượng chuông)
- [ ] VE-18 [E5] (3đ) Menu Options: độ tương phản, cỡ chữ, ẩn/hiện giờ, trang nộp bài
- [ ] VE-19 [E5] (5đ) Ghi chú và highlight khi bôi đen chữ, bảng Notes bên phải
- [ ] VE-20 [E12] (1đ) Báo cáo tiến độ tuần 2
- **Status:** pending

### Phase 3: Sprint 3 (26/10 – 01/11) — Quản trị A2 hoàn chỉnh
Tổng: 7 story, 28 điểm.
- [ ] VE-21 [E4] (5đ) Tạo / sửa kỳ thi, gắn đề cho từng phần thi, mã PIN, mật khẩu giám thị
- [ ] VE-22 [E4] (5đ) Nhập danh sách thí sinh từ Excel, sinh username kiểu Cambridge, in phiếu đăng nhập
- [ ] VE-23 [E4] (2đ) Phân công giám thị cho kỳ thi
- [ ] VE-24 [E4] (2đ) Đổi tên trung tâm và logo (tải PNG)
- [ ] VE-25 [E3] (8đ) Giao diện nhập và sửa đề (part, nhóm câu, câu hỏi, đáp án, upload audio/hình)
- [ ] VE-26 [E8] (5đ) Chấm Writing cho giáo viên và bảng kết quả theo kỳ thi
- [ ] VE-27 [E12] (1đ) Báo cáo tiến độ tuần 3
- **Status:** pending

### Phase 4: Sprint 4 (02/11 – 08/11) — Safe Exam Browser, bảo mật, kiểm thử A2 đầu-cuối
Tổng: 5 story, 17 điểm.
- [ ] VE-28 [E9] (5đ) Kiểm tra cấu hình Safe Exam Browser (config key) trước khi cho vào thi
- [ ] VE-29 [E9] (3đ) Cảnh báo vi phạm phía server: thoát trình duyệt khoá, mất kết nối, lệch đồng hồ, làm sớm
- [ ] VE-30 [E9] (3đ) Giới hạn tần suất đăng nhập, xử lý lỗi an toàn, nhật ký thao tác
- [ ] VE-31 [E11] (5đ) Test đầu-cuối Playwright cho luồng thi A2 và thao tác giám thị chính
- [ ] VE-32 [E12] (1đ) Báo cáo tiến độ tuần 4
- **Status:** pending

### Phase 5: Sprint 5 (09/11 – 15/11) — B1 Preliminary for Schools (1) — tuần gặp GV 2
Tổng: 4 story, 11 điểm.
- [ ] VE-33 [E10] (2đ) Khảo sát giao diện bài thi mẫu B1 PfS (Reading, Writing, Listening)
- [ ] VE-34 [E10] (5đ) Bổ sung kiểu câu còn thiếu cho B1 Reading (6 part)
- [ ] VE-35 [E10] (3đ) B1 Writing: Part 1 email, Part 2 chọn 1 trong 2 đề
- [ ] VE-36 [E12] (1đ) Báo cáo tiến độ tuần 5
- **Status:** pending

### Phase 6: Sprint 6 (16/11 – 22/11) — B1 Preliminary for Schools (2)
Tổng: 4 story, 10 điểm.
- [ ] VE-37 [E10] (3đ) B1 Listening 4 part
- [ ] VE-38 [E10] (3đ) Kỳ thi B1 3 phần thi chạy đầu-cuối, chấm tự động
- [ ] VE-39 [E11] (3đ) Sửa lỗi tồn đọng A2/B1
- [ ] VE-40 [E12] (1đ) Báo cáo tiến độ tuần 6
- **Status:** pending

### Phase 7: Sprint 7 (23/11 – 29/11) — Kiểm thử tải và buổi thi thử
Tổng: 4 story, 12 điểm.
- [ ] VE-41 [E11] (5đ) Kiểm thử tải ~100 thí sinh đồng thời (SignalR + lưu bài)
- [ ] VE-42 [E11] (3đ) Buổi thi thử với các bạn đóng vai thí sinh và giám thị
- [ ] VE-43 [E11] (3đ) Sửa lỗi sau thi thử
- [ ] VE-44 [E12] (1đ) Báo cáo tiến độ tuần 7
- **Status:** pending

### Phase 8: Sprint 8 (30/11 – 06/12) — Đóng gói và tài liệu thiết kế — tuần gặp GV 3
Tổng: 3 story, 11 điểm.
- [ ] VE-45 [E12] (3đ) Sơ đồ trạng thái lượt làm bài, sơ đồ tuần tự, đặc tả API
- [ ] VE-46 [E11] (3đ) Đóng gói triển khai (Docker) và hướng dẫn cài đặt trong setup/
- [ ] VE-47 [E12] (5đ) Viết quyển báo cáo: Mở đầu, Chương 1–3
- **Status:** pending

### Phase 9: Sprint 9 (07/12 – 13/12) — IELTS (nếu kịp) hoặc dự phòng
Tổng: 3 story, 9 điểm.
- [ ] VE-48 [E13] (5đ) IELTS Listening / Reading: kiểu câu nhiều đáp án, ghi nhãn bản đồ
- [ ] VE-49 [E13] (3đ) IELTS Writing Task 1/2 (Academic, General Training)
- [ ] VE-50 [E12] (1đ) Báo cáo tiến độ tuần 9
- **Status:** pending

### Phase 10: Sprint 10 (14/12 – 20/12) — Viết báo cáo: kết quả và kết luận
Tổng: 2 story, 6 điểm.
- [ ] VE-51 [E12] (5đ) Viết Chương 4 (kết quả, ảnh giao diện, số liệu kiểm thử) và Chương 5
- [ ] VE-52 [E12] (1đ) Báo cáo tiến độ tuần 10
- **Status:** pending

### Phase 11: Sprint 11 (21/12 – 27/12) — Hoàn chỉnh và nộp — tuần gặp GV 4
Tổng: 3 story, 6 điểm.
- [ ] VE-53 [E12] (3đ) Chỉnh sửa quyển báo cáo theo góp ý GV, xuất PDF
- [ ] VE-54 [E12] (2đ) Slide trình bày và video demo (thesis/abs)
- [ ] VE-55 [E12] (1đ) Cập nhật README cuối cùng, nộp link GitHub, nộp quyển 31/12
- **Status:** pending

## Key Questions
1. ~~Jira site nào?~~ Đã giải quyết: https://victoryexam.atlassian.net, project `SCRUM` (xem "Jira" bên dưới).
2. Khi nào Victory gửi đề A2 đầy đủ (câu hỏi, hình, đáp án, 5 file audio)? Chặn VE-5/VE-10 bằng dữ liệu thật; trước đó dùng đề mẫu tự soạn.
3. Ai chấm Writing — giáo viên với tài khoản admin hay cần vai trò "giáo viên" riêng?

## Decisions Made
| Decision | Rationale |
|----------|-----------|
| Sprint 1 tuần, Thứ Hai – Chủ nhật | Khớp nhịp push Chủ nhật và báo cáo tiến độ tuần của Khoa |
| Sprint 1 ưu tiên luồng thí sinh A2 | Demo thứ Sáu 16/10 phải chạy được từ đăng nhập tới nộp bài |
| Giám thị thời gian thực ở Sprint 2 | Trùng tuần gặp GV 1, có thể demo phần kỹ thuật chính |
| IELTS để Sprint 9 và là phần "nếu kịp" | Người dùng chốt làm sau A2 và B1; đề IELTS không cố định format |
| Writing chấm tay | File đáp án Cambridge không có đáp án Writing |

## Risks
| Rủi ro | Ứng phó |
|---|---|
| Sprint 1 = 42 điểm trong 5 ngày, quá tải | Thứ tự ưu tiên VE-3/4 → VE-5 → VE-6/7/8 → VE-10 → VE-11; VE-9 có thể dời sang Sprint 2 |
| Chưa có đề A2 thật | Tự soạn đề mẫu đúng format để demo |
| Connector Jira không tạo được sprint | Tạo issue qua connector, người dùng tạo sprint trên board; hoặc nhập `jira-backlog.csv` |

## Jira
- Site https://victoryexam.atlassian.net, project **Victory Exam** (key `SCRUM`, team-managed Scrum).
- Epic E1–E13 = SCRUM-5 … SCRUM-17; story VE-n = SCRUM-(n+17) (VE-1 = SCRUM-18 … VE-55 = SCRUM-72). Mỗi story có nhãn `VE-n` và `sprint-N`, điểm ở *Story point estimate*.
- Sprint đang chạy "SCRUM Sprint 0" (10/10–24/10) chứa VE-1…VE-12; VE-1, VE-2 đã Done.
- Connector không tạo được sprint: tạo Sprint 2…11 trên board rồi lọc backlog theo nhãn `sprint-N` để kéo vào.

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
| Atlassian connector trả về danh sách site rỗng | 1 | Cần người dùng cung cấp site Jira / cấp quyền |
| `sprints.py` in ra console lỗi UnicodeEncodeError (cp1252) | 1 | Ghi ra file UTF-8 thay vì in |
| Jira: "cloud id isn't explicitly granted" | 2 | Người dùng kết nối lại connector (/mcp) và chọn site victoryexam |

## Notes
- Cập nhật trạng thái phase: pending → in_progress → complete.
- Đọc lại plan trước quyết định lớn; ghi mọi lỗi vào bảng.
