### Phase 0: Sprint 0 (10/10 – 11/10) — Khởi động: CSDL và khảo sát giao diện
Tổng: 2 story, 7 điểm.
- [x] VE-1 [E1] (5đ) Thiết kế CSDL 21 bảng, migration EF Core trên MySQL
- [x] VE-2 [E5] (2đ) Khảo sát giao diện bài thi mẫu A2 Key for Schools trên Inspera
- **Status:** complete

### Phase 1: Sprint 1 (12/10 – 18/10) — Demo A2 phía thí sinh (16/10); làm sớm cổng nhân viên, chấm Writing, nhập đề từ PDF
Tổng: 23 story, 93 điểm.
- [x] VE-3 [E2] (5đ) Đăng nhập nhân viên (admin, giám thị) bằng cookie, phân quyền theo vai trò
- [x] VE-4 [E2] (5đ) Đăng nhập thí sinh bằng username/password + mã PIN phòng thi
- [x] VE-5 [E3] (5đ) Định dạng gói đề JSON (+audio, hình) và import đề A2
- [x] VE-56 [E3] (3đ) Bộ chuyển đề PDF → gói đề JSON (A2) và tách audio Listening thành 5 track
- [x] VE-57 [E3] (5đ) Nhập đề từ PDF ngay trên cổng nhân viên và trang duyệt đề
- [x] VE-6 [E5] (3đ) Dựng frontend React + TypeScript (Vite), khung màn hình kiểu Inspera
- [x] VE-7 [E5] (8đ) Reading & Writing A2: 4 kiểu câu (chọn đáp án, chọn từ trên ô trống, gõ từ, viết bài)
- [x] VE-8 [E5] (5đ) Đồng hồ do server nắm, tự lưu câu trả lời, hết giờ tự nộp
- [x] VE-9 [E5] (2đ) Đánh dấu câu hỏi (cờ) và đếm câu đã làm trên thanh part
- [x] VE-10 [E6] (5đ) Listening A2: phát nối 5 track một lần, chọn tranh, kéo thả
- [x] VE-11 [E8] (3đ) Chấm tự động Reading Part 1–5 và Listening theo đáp án
- [x] VE-14 [E7] (5đ) Tạm dừng / cho làm tiếp một, nhiều hoặc cả phòng
- [x] VE-15 [E7] (5đ) Cộng giờ bù sự cố và Edit End Time (End now / End at / End in, Disregard extra time)
- [x] VE-16 [E7] (3đ) Tua audio Listening N phút (chỉ khi không tạm dừng, tối đa số phút đã phát)
- [x] VE-17 [E7] (2đ) Thông báo từ giám thị (biểu tượng chuông)
- [x] VE-18 [E5] (3đ) Menu Options: độ tương phản, cỡ chữ, ẩn/hiện giờ, trang nộp bài
- [x] VE-19 [E5] (5đ) Ghi chú và highlight khi bôi đen chữ, bảng Notes bên phải
- [x] VE-21 [E4] (5đ) Tạo / sửa kỳ thi, gắn đề cho từng phần thi, mã PIN, mật khẩu giám thị
- [x] VE-22 [E4] (5đ) Nhập danh sách thí sinh từ Excel, sinh username kiểu Cambridge, in phiếu đăng nhập
- [x] VE-23 [E4] (2đ) Phân công giám thị cho kỳ thi
- [x] VE-26 [E8] (5đ) Chấm Writing cho giáo viên và bảng kết quả theo kỳ thi
- [x] VE-30 [E9] (3đ) Giới hạn tần suất đăng nhập, xử lý lỗi an toàn, nhật ký thao tác
- [x] VE-12 [E12] (1đ) Báo cáo tiến độ tuần 1 (progress-report)
- **Status:** complete

### Phase 2: Sprint 2 (19/10 – 25/10) — Tuần gặp GV 1: phân quyền nhiều tác nhân, duyệt chéo hai admin (Victory 16/10); đặc tả, sơ đồ, đặc tả API; hướng dẫn cài đặt
Tổng: 9 story, 34 điểm.
- [x] VE-58 [E12] (2đ) Bản đặc tả yêu cầu (quy trình thi máy, vai trò, chức năng, yêu cầu phi chức năng)
- [x] VE-45 [E12] (3đ) Sơ đồ trạng thái lượt làm bài, sơ đồ tuần tự, đặc tả API
- [x] VE-59 [E14] (5đ) Mô hình quyền: Admin, Quản trị viên theo mảng, Giám khảo, Giám thị
- [x] VE-60 [E14] (8đ) Duyệt chéo hai admin: yêu cầu xoá và thao tác tài khoản chờ duyệt
- [x] VE-61 [E14] (5đ) Giám khảo: xem bài làm, chấm Writing và xem kết quả theo kỳ thi được phân công
- [x] VE-62 [E14] (5đ) Cổng nhân viên theo quyền: tài khoản theo vai trò/mảng, trang yêu cầu chờ duyệt, nhật ký
- [x] VE-63 [E14] (2đ) Cập nhật sơ đồ use case, ERD và tài liệu cho các tác nhân mới
- [x] VE-46 [E11] (3đ) Đóng gói triển khai (Docker) và hướng dẫn cài đặt trong setup/
- [x] VE-20 [E12] (1đ) Báo cáo tiến độ tuần 2
- **Status:** complete

### Phase 3: Sprint 3 (26/10 – 01/11) — SignalR; sửa đề trên web; Safe Exam Browser và cảnh báo vi phạm; tên trung tâm và logo; trộn đề A2 (Victory 20/10)
Tổng: 9 story, 40 điểm.
- [x] VE-13 [E7] (5đ) SignalR hub: trạng thái thí sinh, phút còn lại, cảnh báo cập nhật tức thì
- [x] VE-25 [E3] (8đ) Giao diện nhập và sửa đề (part, nhóm câu, câu hỏi, đáp án, upload audio/hình)
- [x] VE-28 [E9] (5đ) Kiểm tra cấu hình Safe Exam Browser (config key) trước khi cho vào thi
- [x] VE-29 [E9] (3đ) Cảnh báo vi phạm phía server: thoát trình duyệt khoá, mất kết nối, lệch đồng hồ, làm sớm
- [x] VE-24 [E4] (2đ) Đổi tên trung tâm và logo (tải PNG)
- [x] VE-64 [E15] (3đ) Đề gốc thứ hai cho A2 (A2 Key sample): tách audio 5 part, chuyển PDF thành gói, kiểm tra hai đề cùng khuôn
- [x] VE-65 [E15] (8đ) Sinh 4 đề trộn từ 2 đề gốc: mỗi part lấy nguyên từ đề gốc 1 hoặc 2 (kèm hình, audio, đáp án)
- [x] VE-66 [E15] (5đ) Sơ đồ phòng máy và chia đề theo vị trí máy (ô 2×2): hai máy kề nhau ngang, dọc, chéo luôn khác đề
- [x] VE-27 [E12] (1đ) Báo cáo tiến độ tuần 3
- **Status:** complete

### Phase 4: Sprint 4 (02/11 – 08/11) — Kiểm thử đầu-cuối A2; bắt đầu B1 Preliminary for Schools
Tổng: 5 story, 18 điểm.
- [x] VE-31 [E11] (5đ) Test đầu-cuối Playwright cho luồng thi A2 và thao tác giám thị chính
- [x] VE-33 [E10] (2đ) Khảo sát giao diện bài thi mẫu B1 PfS (Reading, Writing, Listening)
- [x] VE-67 [E15] (5đ) Cổng nhân viên cho trộn đề: chọn 2 đề gốc, xem 4 đề trộn, in sơ đồ phòng kèm mã đề; mã đề trên trang giám thị và bảng kết quả
- [x] VE-34 [E10] (5đ) Bổ sung kiểu câu còn thiếu cho B1 Reading (6 part)
- [x] VE-32 [E12] (1đ) Báo cáo tiến độ tuần 4
- **Status:** complete

### Phase 5: Sprint 5 (09/11 – 15/11) — B1 Preliminary for Schools: Writing và Listening — tuần gặp GV 2
Tổng: 4 story, 15 điểm.
- [x] VE-69 [E10] (8đ) Bộ chuyển đề PDF → gói đề cho B1 Preliminary (Reading, Writing, Listening) và nhập đề B1 trên cổng nhân viên
- [x] VE-35 [E10] (3đ) B1 Writing: Part 1 email, Part 2 chọn 1 trong 2 đề
- [x] VE-37 [E10] (3đ) B1 Listening 4 part
- [x] VE-36 [E12] (1đ) Báo cáo tiến độ tuần 5
- **Status:** complete

### Phase 6: Sprint 6 (16/11 – 22/11) — Kỳ thi B1 chạy đầu-cuối, sửa lỗi tồn đọng A2/B1
Tổng: 4 story, 10 điểm.
- [x] VE-38 [E10] (3đ) Kỳ thi B1 3 phần thi chạy đầu-cuối, chấm tự động
- [x] VE-68 [E15] (3đ) Trộn đề B1 (Reading, Writing, Listening) từ hai đề gốc B1 Preliminary 2022 và B1 Preliminary for Schools
- [x] VE-39 [E11] (3đ) Sửa lỗi tồn đọng A2/B1
- [x] VE-40 [E12] (1đ) Báo cáo tiến độ tuần 6
- **Status:** complete

### Phase 7: Sprint 7 (23/11 – 29/11) — Kiểm thử tải và buổi thi thử
Tổng: 4 story, 12 điểm.
- [ ] VE-41 [E11] (5đ) Kiểm thử tải ~100 thí sinh đồng thời (SignalR + lưu bài)
- [ ] VE-42 [E11] (3đ) Buổi thi thử với các bạn đóng vai thí sinh và giám thị
- [ ] VE-43 [E11] (3đ) Sửa lỗi sau thi thử
- [ ] VE-44 [E12] (1đ) Báo cáo tiến độ tuần 7
- **Status:** pending

### Phase 8: Sprint 8 (30/11 – 06/12) — Viết quyển báo cáo Mở đầu, Chương 1–3 — tuần gặp GV 3
Tổng: 1 story, 5 điểm.
- [ ] VE-47 [E12] (5đ) Viết quyển báo cáo: Mở đầu, Chương 1–3
- **Status:** pending

### Phase 9: Sprint 9 (07/12 – 13/12) — Dự phòng (IELTS tách khỏi đồ án chuyên ngành)
Tổng: 1 story, 1 điểm.
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

### Ngoài đồ án chuyên ngành — để dành cho đồ án tốt nghiệp / báo cáo thực tập
- [ ] VE-48 [E13] (5đ) IELTS Listening / Reading: kiểu câu nhiều đáp án, ghi nhãn bản đồ
- [ ] VE-49 [E13] (3đ) IELTS Writing Task 1/2 (Academic, General Training)
