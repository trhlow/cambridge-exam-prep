# Progress Log

## Session: 2026-10-10

### Phase 0: Sprint 0 — Khởi động
- **Status:** complete
- Actions taken:
  - Khảo sát giao diện bài thi mẫu A2 (Chrome), ghi `thesis/refs/inspera-ui-a2.md`
  - Cài .NET 10, dựng solution, CSDL 21 bảng + migration, MySQL Docker
  - 16 test tích hợp đạt; ERD sinh từ schema, cập nhật use case
  - Commit `d0e5536` (chưa push — push vào Chủ nhật)
- Files created/modified: `src/backend/**`, `docker/**`, `thesis/diagrams/*`, `README.md`, `CLAUDE.md`

### Lập kế hoạch sprint
- **Status:** complete
- Actions taken:
  - Khởi tạo plan bằng `planning-with-files` (`.planning/2026-10-10-sprint-plan/`)
  - Viết `sprints.py` → `sprint-backlog.md`, `jira-backlog.csv` (12 sprint, 13 epic, 55 story)
  - Jira (victoryexam.atlassian.net, project SCRUM): tạo 13 epic (SCRUM-5…17) và 55 story (SCRUM-18…72) có điểm, nhãn, tiêu chí chấp nhận
  - VE-1…VE-12 xếp vào sprint đang chạy; VE-1, VE-2 chuyển Done; kiểm tra bằng JQL: 55 story, 12 trong sprint

## Test Results
| Test | Input | Expected | Actual | Status |
|------|-------|----------|--------|--------|
| dotnet test (có MySQL) | 16 test | 16 đạt | 16 đạt | ✓ |
| dotnet test (không có MySQL) | 16 test | 9 bỏ qua, 7 đạt | 9 bỏ qua, 7 đạt | ✓ |

## Error Log
| Timestamp | Error | Attempt | Resolution |
|-----------|-------|---------|------------|
| 2026-10-10 | Atlassian: không có site truy cập được | 1 | Người dùng cung cấp site và cấp quyền connector |

### Sprint 1 — Bước 0: rà soát CSDL (10/10, chiều)
- **Status:** complete
- Rà soát 21 bảng theo màn hình A2 và quy trình giám thị → 5 lỗ hổng D1–D5 (chi tiết trong findings.md)
- `AttemptClock` (Domain/Delivery) viết theo TDD: 26 test RED → GREEN ngay lần đầu
- Migration `SchemaReviewFixes`: `end_at_override` → `end_adjust_seconds`, `security_stamp` (users, candidates), `exam_sections.start_announced_at`, 14 CHECK constraint; DbContext tự đổi `concurrency_stamp`
- Test DB: RED (thiếu migration) → GREEN; toàn bộ 46/46 test đạt; migration đã áp vào DB phát triển
- Script sinh ERD / use case đưa vào `thesis/diagrams/tools/` (ERD đọc trực tiếp MySQL); ERD sinh lại: 21 bảng, 177 cột

### Sprint 1 — VE-3 đăng nhập nhân viên (10/10, tối)
- **Status:** complete (Jira SCRUM-20 → Done)
- Skill: `test-driven-development` (13 test tích hợp RED đúng lý do → GREEN), `security-review` (không có lỗ hổng độ tin cậy cao)
- API: `POST /api/staff/auth/login|logout`, `GET /api/staff/auth/me`, `GET /api/admin/users` (Admin)
- Cookie "Staff" HttpOnly + SameSite=Strict (Secure ở Production), 401/403 thay redirect, kiểm tra `IsActive` + `security_stamp` mỗi request
- Băm PBKDF2, rehash khi cần, thời gian thất bại đồng đều; admin đầu tiên từ `Bootstrap:AdminUsername/AdminPassword`
- Toàn bộ 59/59 test đạt; commit `85187ed`
- Còn lại cho VE-30: giới hạn tần suất đăng nhập

## Session: 2026-10-11

### Jira
- VE-1, VE-2, VE-3 (SCRUM-18/19/20): gán người làm, thêm comment báo cáo hoàn thành chi tiết
- VE-4 (SCRUM-21): In Progress → Done kèm comment

### Sprint 1 — VE-4 đăng nhập thí sinh + PIN
- **Status:** complete
- Skill: `test-driven-development` (17 test RED đúng lý do: 404 + chưa đăng ký hasher → GREEN), `security-review` (không có lỗ hổng độ tin cậy cao), `planning-with-files`
- API: `POST /api/candidate/auth/login|pin|logout`, `GET /api/candidate/auth/me`, `GET /api/candidate/sections`
- Cookie "Candidate" tách khỏi "Staff"; đăng nhập đổi `security_stamp` (máy cũ bị đăng xuất); chỉ vào được khi kỳ thi Active; PIN so sánh thời gian hằng
- Lỗi phát hiện nhờ test: provider MySQL của Oracle không đọc được cột `date` thành `DateOnly` → thêm converter trong `ModelConventions` (test DB tái hiện trước khi sửa)
- Toàn bộ 78/78 test đạt; commit `fb1a821`

### Sprint 1 — VE-5 phần 1: định dạng gói đề + import
- **Status:** complete (Jira SCRUM-22 → Done); phần 2 (bộ chuyển PDF) tách thành VE-56 (SCRUM-73)
- Skill: `test-driven-development` (38 test RED đúng lý do: NotImplemented + 404 → GREEN), `security-review` (không có lỗ hổng độ tin cậy cao), `planning-with-files`, `full-output-enforcement`
- Gói đề = 1 zip/1 paper (`paper.json` + images/ + audio/); tài liệu `setup/exam-package/FORMAT.md`; đề mẫu tự soạn A2 R&W 32 câu + Listening 25 câu (vừa là tài liệu, vừa là dữ liệu test); `pack.py` đóng gói
- Reader (zip, JSON chặt, chặn tên file ra ngoài gói, giới hạn 500 file/300 MB), Validator (báo mọi lỗi bằng câu dễ hiểu), Importer (lưu file theo SHA-256, đổi `asset:` → `/api/assets/{id}`, lọc HTML bằng HtmlSanitizer, 1 transaction, đề ở Draft)
- `POST /api/admin/papers/import` (Admin); 116/116 test đạt; commit `49e47de`
- Người dùng gửi đề mẫu chính thức A2 2022 (PDF + 1 mp3 31 phút) vào `setup/exam-package/sources/a2-key-fs-2022-sample/` → đã git-ignore; PDF trích được chữ (không phải scan), ảnh nhúng sẵn; có ffmpeg để tách audio

### Sprint 1 — VE-6/VE-7: giao diện thí sinh A2 Reading & Writing
- **Status:** chờ người dùng đánh giá (Jira SCRUM-23, SCRUM-24 → In Review)
- Skill: `image-to-code` (ảnh tham chiếu = màn hình thật của bài mẫu Inspera A2, chụp và đo CSS bằng Chrome trước khi code), `test-driven-development` (backend 13 test, frontend 21 test, đều RED trước), `security-review` (không có lỗ hổng độ tin cậy cao), `planning-with-files`
- Backend: `GET /api/candidate/sections/{id}/paper` (không có đáp án), `GET /api/assets/{id}` (thí sinh chỉ đọc file của kỳ thi mình), DemoSeeder (Development) + hình đề mẫu vẽ bằng PIL
- Frontend `src/frontend` (React 19 + TS + Vite): đăng nhập → PIN → chọn phần thi → làm bài; thanh trên, hộp hướng dẫn, nút ← →, thanh part "n of m", Part 1 từng câu, chia đôi có thanh kéo, chọn từ trên ô trống, gõ chữ, viết bài đếm từ, cờ, trang xem lại
- Kiểm tra trên Chrome với kỳ thi demo; sửa sau khi xem: chỉ đóng khung câu đang làm, căn giữa số trong ô trống
- Toàn bộ: backend 129/129, frontend 21/21; commit `48d63cd`, `df4ee52`, `e1d225d`

### Sửa theo góp ý giao diện (11/10)
- TDD: backend 3 test (khoá Listening, mở khi R&W Submitted, 403 lấy đề section bị khoá) → 132/132; frontend 3 test (cờ trên ô part đóng, cờ trên số câu, danh sách phần thi bị khoá) → 24/24
- Kiểm tra lại trên Chrome: Listening hiện "Locked"; cờ Part 1 vẫn thấy khi ở Part 2; cờ căn giữa trên số câu

### Sprint 1 — VE-8: đồng hồ server, tự lưu, hết giờ tự nộp
- **Status:** complete (Jira SCRUM-25 → Done; VE-6/VE-7 Done sau khi người dùng duyệt)
- Skill: `test-driven-development` (backend 13 test, frontend 15 test, đều RED trước), `security-review` (không có lỗ hổng độ tin cậy cao), `planning-with-files`
- Backend: start/resume attempt bằng AttemptClock (+ giờ ưu tiên), GET attempt (giờ + đáp án đã lưu), PUT responses (upsert, ân hạn 5 s), AttemptExpiryWorker quét 5 s/lần tự nộp bài hết giờ kể cả khi thí sinh offline → mở section sau
- Frontend: useAttempt + AutosaveQueue (0,8 s sau thay đổi, thử lại 3 s, biểu tượng wifi đỏ khi offline), "59 minutes left", lớp phủ tạm dừng, màn "The test has now finished"; bỏ nút Submit (Cambridge: chờ hết giờ hoặc giám thị kết thúc)
- Lỗi thật phát hiện khi chạy thử: React StrictMode gửi 2 lần start → trùng khoá attempts → test tái hiện rồi sửa; thời gian lệch tick khi đọc lại từ MySQL → làm tròn mili giây
- Kiểm tra trên Chrome: chọn đáp án → tải lại vẫn còn; đẩy giờ bắt đầu lùi 1 tiếng → worker nộp trong < 8 s → màn kết thúc → Listening mở
- DemoSeeder ResetOnStart: mỗi lần bật API dev tạo lại kỳ thi demo
- Toàn bộ: backend 144/144, frontend 39/39; commit `8d6b490`, `853165c`

### Sprint 1 — VE-10: Listening A2
- **Status:** complete (Jira SCRUM-27 → Done)
- Skill: `image-to-code` (chụp lớp phủ Play, thanh "Audio is Playing", Part 5 kéo thả của bài mẫu Inspera — audio bị tắt tiếng bằng JS), `test-driven-development` (backend 5, frontend 9 test, RED trước), `security-review`, `planning-with-files`
- Backend: `POST .../audio` đặt AudioStartedAt một lần; AttemptState có `audioPositionMs`
- Frontend: lớp phủ Play / Continue listening; useRecordingPlayer phát nối 5 track, không có nút, chặn phím media; Part 1 Listening mỗi câu một trang; DragDropMatch kéo thả bằng pointer events (chuột, cảm ứng) hoặc chọn-đặt bằng bàn phím
- Âm thanh đề mẫu: make_audio.ps1 đọc tapescript tự soạn bằng giọng Windows (mỗi đoạn hai lần) → 5 mp3, ~17 phút
- Kiểm tra trên Chrome (tắt tiếng): Play → "Audio is Playing"; sang Part 5 audio vẫn chạy; tải lại → Continue listening tiếp ở giây 85 (không phát lại từ đầu); thử pause → tự phát tiếp; kéo "cake" vào ô 21 thành công
- Đổi cách kéo thả: HTML5 drag-and-drop không nhận chuột tự động/cảm ứng → pointer events
- Toàn bộ: backend 149/149, frontend 48/48; commit `51f5ae4`, `6cff1fc`

### Sprint 1 — VE-11: chấm tự động
- **Status:** complete (Jira SCRUM-28 → Done)
- Skill: `test-driven-development` (25 test AnswerMatcher + 8 test chấm/kết quả, RED trước), `security-review` (phát hiện giám thị xem được đáp án → test RED → chỉ Admin), `planning-with-files`
- AnswerMatcher (Domain): câu chọn so khoá đáp án; câu gõ bỏ qua hoa/thường (trừ khi khoá phân biệt), khoảng trắng thừa, nháy cong, dấu chấm cuối — sai chính tả vẫn sai như Cambridge; nhiều dòng answer_keys = nhiều đáp án chấp nhận; Writing không chấm tự động
- AttemptMarker: chấm attempt Submitted + NotGraded, ghi is_correct/auto_score từng câu, raw_score/max_score; đề có Writing → NeedsManualMarking, còn lại AutoGraded; worker chạy ngay sau lượt nộp bài hết giờ
- `GET /api/staff/exams/{id}/results` (Staff: điểm từng thí sinh, từng phần), `GET /api/staff/attempts/{id}/marking` (chỉ Admin: đáp án thí sinh, đáp án đúng, điểm từng câu); thí sinh không xem điểm
- Chạy thật với kỳ thi demo: R&W hết giờ → 3/60, NeedsManualMarking
- Toàn bộ: backend 182/182, frontend 48/48; commit `7eb85e9` + sửa phân quyền

### Sprint 1 — VE-9 đóng (11/10)
- Người dùng duyệt: cờ và "Part n  x of y" đã làm trong VE-6/7 (+ sửa góp ý 11/10); Jira SCRUM-26 → Done

### Sprint 1 — VE-56 phần 1: bộ chuyển PDF → gói đề, Reading & Writing A2
- **Status:** R&W xong; Listening làm sau theo yêu cầu người dùng (Jira SCRUM-73 vẫn In Progress)
- Skill: `test-driven-development` (35 test pytest, RED trước: thiếu module → lỗi header trang 2 của đáp án → ảnh dính chữ → BLANK PAGE → kích thước ảnh → nháy đơn trong alt), `security-review` (alt đặt trong nháy đơn nhưng chưa thoát nháy → sửa), `planning-with-files`
- `setup/exam-package/pdf2package.py` + gói `converter/` (model, text, answer_key, parts, a2_reading_writing, output); test dùng dòng chữ tự soạn đúng toạ độ bố cục Cambridge, test đề thật chỉ chạy khi có PDF trong sources/
- Đề mẫu A2 2022: 7 part, 32 câu, 12 hình, khớp đáp án, 0 lỗi; 2 mục cần xem (q21 mất dấu chấm — người duyệt sửa; q28 trước chữ viết tắt viết hoa — đúng)
- Import qua `POST /api/admin/papers/import`: 201, Draft, 7 part, 32 câu; `Demo:ReadingWritingFolder` cho kỳ thi demo dùng đề thật (2 test DemoSeeder) → backend 184/184
- Xem trên trình duyệt (Playwright, Chrome extension không kết nối): đủ 7 part; sửa ảnh Part 2 quá to và 3 tranh Part 7 bị xuống dòng bằng `width` theo kích thước in
- .gitignore thêm `__pycache__/`, `.pytest_cache/`, `.playwright-mcp/` (ảnh chụp có nội dung đề)

### Sprint 1 (kéo từ Sprint 2–3) — Cổng nhân viên: VE-14…17, VE-21…23
- **Status:** complete (Jira SCRUM-31…34, SCRUM-38…40 → Done); VE-13 SignalR chưa làm
- Skill: `planning-with-files`, `test-driven-development` (34 test backend + 12 test frontend, RED trước), `minimalist-ui` (giao diện `/staff`), `security-review` (không phát hiện lỗ hổng mức High/Medium)
- API admin: kỳ thi (gắn đề đã xuất bản theo mẫu phần thi, PIN/mật khẩu giám thị tự sinh, trạng thái, khoá đề khi đã có thí sinh làm), đề (danh sách, xuất bản, xoá nếu chưa dùng), thí sinh (nhập cả danh sách, báo lỗi từng dòng, username Cambridge bỏ dấu, phiếu in lại), tài khoản nhân viên (tạo, đặt lại mật khẩu, khoá → đăng xuất ngay), phân công giám thị
- API giám thị: bảng theo dõi theo phần thi; Pause/Resume, cộng giờ sự cố, End now/at/in, Use test end time, tua audio; thông báo cho cả phòng hoặc từng thí sinh; heartbeat 5 s của thí sinh; giám thị chỉ thấy kỳ thi được phân công (kể cả kết quả)
- Giao diện `/staff` (bundle riêng): đăng nhập, Exams, kỳ thi (PIN, thí sinh dán từ Excel, giám thị, in phiếu, kết quả), trang theo dõi kiểu Inspera, Papers (import .zip), Staff
- Thí sinh: chuông thông báo + cửa sổ bật lên; Listening theo kịp khi giám thị tua
- Kiểm tra trên Chrome: thêm 2 thí sinh bằng dán, Pause → máy thí sinh hiện màn hình tạm dừng trong vài giây, Resume, +5 phút (59:37 → 1:04:26), thông báo hiện ngay; giám thị demo chỉ thấy kỳ thi của mình; phiếu đăng nhập in được
- Toàn bộ: backend 218/218, frontend 60/60; commit `8c11202`, `8f541fa`, `f127ba5`, `fbcafed`

### Rà soát tổng thể trước báo cáo (13/10)
- **Status:** complete — VE-18, VE-19, VE-26, VE-30 → Done
- Skill: `planning-with-files`, `test-driven-development` (RED trước cho mọi sửa đổi), `minimalist-ui` (cổng nhân viên), `security-review`
- Mốc ban đầu xanh (218/60/35), npm audit + NuGet sạch; phát hiện và sửa:
  - Bảo mật: khoá tài khoản 5 phút sau 10 lần sai mật khẩu / PIN (PIN 4 số trước đây dò được); khoá Data Protection lưu App_Data/keys (Docker khởi động lại sẽ làm mất phiên và không in lại được phiếu); security header; exception handler + HSTS ngoài Development; đặt lại mật khẩu thí sinh; CSV chống công thức Excel
  - security-review tìm thêm: MySQL so khớp username không phân biệt hoa/thường và dấu → "àdmin" vượt được khoá → đếm theo id tài khoản (test tái hiện)
  - Thí sinh: F5 vào lại đúng phần thi và trang; menu Options (3 độ tương phản, 3 cỡ chữ, ẩn giờ, trang nộp bài) lưu trên server; highlight + ghi chú (CSS Custom Highlight API), Notes tắt khi chữ lớn
  - Cổng nhân viên: hết phiên → về đăng nhập; tên + badge vai trò; cột ngày import; nút xác nhận mất nền đỏ khi hover; chấm Writing (điểm + nhận xét, Final khi chấm đủ); tải kết quả CSV
  - Test treo ngẫu nhiên: GET_LOCK('__EFMigrationsLock') toàn server + pool giữ lock → migrate tuần tự, không pool; 3 lần liên tiếp 230/230
- Kiểm tra trên Chrome (1366×768): Options + Yellow on black + Large, highlight/note, F5, hết phiên, End now hai bước, chấm Writing 13/60 Final
- Toàn bộ: backend 231/231, frontend 71/71, converter 35/35; commit 7594d40 … 6e9961b

## Session: 2026-10-15

### Docker, Listening, nhập đề từ PDF trên web (VE-12, VE-56, VE-57)
- **Status:** complete — commit cf049bf (báo cáo tuần 1), 22eec20 (bộ chuyển Listening), 6866f98 (tài khoản demo), 13462ed (Docker một lệnh), 32e1c6a (Import from PDF + trang duyệt đề)
- Toàn bộ: backend 244/244, frontend 74/74, converter 55/55
- Báo cáo tuần 1 điền vào tab "Trần Hoàng Long" của file tiến độ chung (Google Sheets)

### Jira: đủ 12 sprint
- Board SCRUM chỉ có 1 sprint đang chạy → đổi tên "SCRUM Sprint 1" (10/10–18/10, gồm Sprint 0 + 1 của kế hoạch), sprint trống thành "SCRUM Sprint 2" (19/10–25/10), tạo Sprint 3–11 theo lịch tuần
- 11 story làm sớm (VE-14…19, VE-21…23, VE-26, VE-30) đưa vào Sprint 1; VE-12, VE-56 → Done; VE-46 → In Progress (Docker xong, còn hướng dẫn cài đặt)
- Story mới: VE-57 = SCRUM-74 (Done, Sprint 1), VE-58 = SCRUM-75 (đặc tả yêu cầu, Sprint 2)
- Vượt kế hoạch nên dời lên một sprint: VE-25, VE-45, VE-46 → Sprint 2; VE-28, VE-29 → Sprint 3; VE-33, VE-34 → Sprint 4; VE-37 → Sprint 5 (mô tả và nhãn sprint-N trên Jira đã sửa theo)
- sprints.py sinh lại sprint-backlog.md và jira-backlog.csv cho khớp Jira

## Session: 2026-10-16

### Vai trò mới của Victory (E14: VE-59…VE-63)
- **Status:** complete — commit 514a5e8 (kế hoạch), bb3d386 (quản trị viên theo mảng, giám khảo), 81793a0 (hai admin duyệt chéo), c64649a (cổng nhân viên theo vai trò, trang Approvals), commit tài liệu VE-63
- Quyết định với người dùng: admin chỉ xem bảng điểm, giám khảo xem bài làm + chấm Writing + xem kết quả kỳ được phân công; cần duyệt = mọi thao tác xoá + thay đổi tài khoản; quản trị viên bốn mảng (đề thi, kỳ thi, thí sinh, trung tâm), tạo + sửa, không xoá
- CSDL 24 bảng (thêm `user_areas`, `exam_examiners`, `approval_requests`); ERD và use case (5 tác nhân) vẽ lại
- Toàn bộ: backend 268/268, frontend 82/82
- Jira: E14 = SCRUM-76, VE-59…63 = SCRUM-77…81 (Sprint 2); VE-13, VE-25 dời sang Sprint 3

## Session: 2026-10-18

### Sprint 2 hoàn tất (VE-13, VE-45, VE-46, VE-58, VE-20)
- **Status:** complete — commit b11ec0f (SignalR), e7b7e9b (hướng dẫn cài đặt), 06cc6bb (đặc tả yêu cầu, sơ đồ trạng thái, 3 sơ đồ tuần tự, đặc tả API), d4bafa4 (CLAUDE.md), 2b3e342 (báo cáo tuần 2)
- Báo cáo tuần 2: file docx trong progress-report/ và dòng tuần 2 trong Google Sheet (mục 6 để trống theo ý người dùng)
- Jira: VE-13 kéo vào Sprint 2; 10/10 story Sprint 2 Done. Đóng Sprint 1 / mở Sprint 2 trên Jira bị chặn quyền (cần người dùng tự làm hoặc cho phép)

### Sprint 3 làm sớm (VE-24, VE-25, VE-28, VE-29)
- **Status:** VE-24, VE-28 Done; VE-25 In Progress (sửa đề nháp xong, còn tạo đề mới từ đầu trên web); VE-29 In Progress (3/5 cảnh báo, còn "làm sớm" cần nút Announce start); VE-27 làm cuối tuần 3 — commit 6d4a67b (tên + logo trung tâm), 66a9475 (SEB + cảnh báo), 78b4a79 (sửa đề nháp trên web)
- Toàn bộ: backend 287/287, frontend 87/87; Docker build lại, kiểm tra /api/site, đăng nhập thí sinh
- Tài liệu đặc tả cập nhật trạng thái và 64 API

## Session: 2026-10-19

### Sprint 3: xong VE-25 và VE-29
- VE-29: nút Announce start trên trang giám thị; thí sinh đã làm trước đó nhận cảnh báo StartedEarly (một lần). "Thoát trình duyệt khoá" vẫn tính là ConnectionLost (server không phân biệt được)
- VE-25: "New blank paper" tạo đề nháp theo khung A2 (R&W 7 part/32 câu, Listening 5 part/25 câu), chữ "[to write…]", hình/file nghe giữ chỗ; editor tải hình cho đoạn văn và thay hình lựa chọn; xuất bản bị chặn khi còn chỗ chưa viết
- Toàn bộ: backend 291/291, frontend 88/88; Docker build lại, thử tạo đề trắng qua nginx (đề thử đã xoá qua duyệt chéo)
- Còn lại Sprint 3: VE-27 báo cáo tuần 3 (cuối tuần 3)

## Session: 2026-10-20

### Victory: trộn đề (E15) và hai bộ đề mới
- Thư mục `đề a2/`, `đề b1/` chuyển vào `setup/exam-package/sources/` (git-ignored): `a2-key-2020-sample/` (A2 Key, mới), `b1-pet-2022-sample/`, `b1-pet-fs-sample/`; `504343-…` trùng từng byte với `a2-key-fs-2022-sample/` nên bỏ; bỏ các bản sao mp3 trùng; `_chua-phan-loai/B1 Q1 L P1 P2 V1.mp3` (~15 phút) chờ người dùng cho biết thuộc đề nào
- Kế hoạch: E15 + VE-64…68 trong sprints.py (VE-64…66 Sprint 3, VE-67 Sprint 4, VE-68 Sprint 6); Jira E15 = SCRUM-82, VE-64…68 = SCRUM-83…87
- Thiết kế dự kiến ghi trong findings.md; chờ Victory trả lời 3 câu hỏi trước khi làm

## Session: 2026-10-21

### Sprint 3 xong sớm: E15 cho A2 (VE-64, VE-65, VE-66) và báo cáo tuần 3 (VE-27)
- Người dùng: làm vượt lịch; sprint nào xong thì viết báo cáo tuần của sprint đó ngay, ghi đúng tuần theo kế hoạch. Trộn đề làm theo mặc định (trộn nguyên part, không xáo câu; lưới 2×2 cho từng phần thi), chờ Victory trả lời thì chỉnh
- VE-64 (849dc6d): `split_listening.py` (ffmpeg qua imageio-ffmpeg, cài `pip --user`); A2 Key Listening tách 5 part; bộ chuyển đo cột đáp án Part 2 theo "(n)" và ghép tiêu đề 2 dòng; `PaperShape`; demo nạp đề gốc thứ hai
- VE-65 (00781e8): `PaperMixPlan`, `PaperMixer`, `POST /api/admin/paper-mixes`, bảng `paper_mixes`
- VE-66 (c97897c): `SeatPattern`, `PaperAssignment`, `exams.seats_per_row`, `exam_sections.paper_mix_id`, `attempts.test_paper_id`; demo trộn cả hai phần thi
- Gói SignalR client/MVC testing lên 10.0.13 (d439291, cảnh báo GHSA-334f-vvp4-7g6p)
- Toàn bộ: backend 350/350, bộ chuyển 60/60; Docker build lại, thí sinh demo nhận đề trộn (Part 2 từ A2 for Schools, Part 3 từ A2 Key)
- VE-27: `progress-report/BaoCaoTienDo_Tuan03_TranHoangLong.docx` (26/10–01/11, ký 01/11). Dòng tuần 3 trên Google Sheet để trống: người dùng chỉ điền Sheet khi đến đúng ngày báo cáo (đã điền thử rồi xoá lại 21/10)
- Còn lưu ý: form kỳ thi trên web chưa gửi `paperMixId`/`seatsPerRow` → VE-67; file nghe B1 mẫu không tách tự động được

### Sprint 4 xong sớm (21/10): VE-67, VE-33, VE-34, VE-32
- VE-67 (b6313e7): trộn đề trên cổng nhân viên, sơ đồ phòng in được, cột mã đề (thí sinh, giám thị, kết quả + CSV, chấm)
- VE-33 (6e48eb3): `thesis/refs/inspera-ui-b1.md` từ bài mẫu B1 trên ceq.inspera.com (Playwright headless)
- VE-34 (c35cd51): DragDropMatch 3 bố cục (hàng nhãn, người có ảnh, ô trống trong bài), tiêu đề thẻ/cột, đề mẫu `samples/b1-pfs-reading`
- VE-32: `progress-report/BaoCaoTienDo_Tuan04_TranHoangLong.docx` (02–08/11, ký 08/11); Google Sheet chưa điền (chờ đúng ngày)
- Toàn bộ: backend 359/359, frontend 93/93, bộ chuyển 60/60
- Tiếp theo Sprint 5: VE-35 B1 Writing (chọn 1 trong 2 câu: Yes/No/Undecided), VE-37 B1 Listening (Part 2 mỗi câu một trang), VE-36 báo cáo tuần 5

### Sprint 5 — VE-69 bộ chuyển đề B1 (21/10)
- `converter/b1_reading.py`, `b1_writing.py` (7b976a1), `b1_listening.py`; `pdf2package.py` nhận cấp độ + phần thi từ trang bìa (`component()`), mọi loại đều ghi `--report`; thiếu đáp án (trừ B1 Writing) hay thư mục nghe → lỗi trong report
- Đáp án B1: từ tuỳ chọn trong ngoặc (`expand_optional`, tối đa 64 dạng, không ghép liền mạo từ/giới từ), cột "ACCEPTABLE MIS-SPELLINGS"
- `split_listening.py` tách được file nghe B1 (mỗi khoảng lặng 10 s là hết part, trừ khoảng trước thời gian chép đáp án) — cả hai bộ B1 đã có `audio-parts/` 4 file
- Web: file đáp án không bắt buộc (`PaperConversionInput.AnswerKeyPdf` null), hộp thoại ghi "A2 Key 5, B1 Preliminary 4"; thử qua Docker: B1 Writing (2 part, 3 câu), Listening (4 part, 25 câu) import thành Draft, Reading thiếu đáp án báo lỗi rõ
- Bộ chuyển 69/69 test (thêm test B1 Listening Part 2/3 và đáp án tuỳ chọn), backend conversion 12/12

### Sprint 5 — VE-37 B1 Listening (21/10)
- `paper_parts.question_per_page` (migration `QuestionPerPage`, gói `questionPerPage`, importer, trộn đề, API thí sinh và trang duyệt đề); `buildPages` chia trang theo cờ, kể cả bố cục Split (dùng lại cho B1 Writing Part 2)
- Bộ chuyển B1 đặt cờ cho Listening Part 2 và Writing Part 2
- Đề mẫu tự soạn `samples/b1-pfs-listening` (4 part, 25 câu; 21 hình vẽ; `make_audio.ps1` đọc bằng giọng Windows, mỗi đoạn hai lần)
- Test: `B1ListeningTests` (import, trộn giữ cờ, thí sinh nhận Part 2 từng trang), validator nhận đề mẫu, frontend `buildPages`
