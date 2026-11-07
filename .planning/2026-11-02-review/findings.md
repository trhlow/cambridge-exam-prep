# Review toàn hệ thống, 02/11/2026

Tại commit `1552ce0`. Năm reviewer đọc độc lập năm mảng (thí sinh; phòng thi; phân quyền; đề, phiên, chấm; dữ liệu, triển khai).
Cột **KT**: `x` = tự đọc code xác nhận; `~` = reviewer báo, chưa tự kiểm lại.

**Trạng thái 07/11/2026: đã sửa đủ 42 mục** theo `fix-plan.md` (8 nhóm, mỗi nhóm có review riêng, sau đó review toàn nhánh và hai lượt sửa thêm; commit `9d6600c` … `708f213`). Kiểm thử trên mã cuối: backend 1464/1464, frontend 201/201, bộ chuyển đề 69/69, e2e 6/6, buổi thi thử 16/16 (`setup/mock-exam/results/2026-11-07/`), tải 200 thí sinh không lỗi máy chủ (`setup/loadtest/results/2026-11-07-03-28-200.md`). Quyết định khác bản kế hoạch và giới hạn còn lại ghi ở mục cuối.

Kiểm thử chạy lại tại commit này: backend 1345/1345, frontend 119/119, bộ chuyển đề 69/69, e2e 6/6, buổi thi thử 16/16.
Test đều qua nhưng không phủ các tình huống dưới đây, nên mục nào sửa cũng cần thêm test riêng.

## A. Mất bài hoặc sai kết quả thi

| # | KT | Vấn đề | Vị trí | Hướng sửa |
|---|---|---|---|---|
| A1 | x | Đổi đáp án sát lúc bị Pause: server trả 409, hàng đợi tự lưu đóng hẳn. Sau Resume mọi đáp án của phần thi không lên server, màn hình vẫn hiện, không báo lỗi | `exam/autosave.ts:21,89` | Không đóng khi 409 paused; giữ pending, gửi lại sau Resume |
| A2 | x | 5 giây ân hạn gần như vô dụng: bộ quét, heartbeat, GET nộp đúng `EndsAt`, không ân hạn. Bản lưu cuối bị 409 | `AttemptExpiry.cs:38`, `CandidateAttemptsController.cs:124,159` | Dùng chung ân hạn cho mọi nơi nộp bài |
| A3 | ~ | Gõ liên tục đến hết giờ không gửi gì (debounce không có max wait); đóng màn hình vứt pending; F5 hoặc crash mất phần chưa gửi | `autosave.ts:50-53` | maxWait, flush khi Submitted, lưu tạm localStorage |
| A4 | x | End now với thí sinh đã vào giờ cộng thêm làm giờ kết thúc **dài ra**; bấm lần hai cũng vậy | `AttemptClock.cs:99` | `Math.Min(0, …)` |
| A5 | x | Giám khảo chấm mà **không thấy đề bài**: `QuestionMark` không có prompt/stimulus, đề trộn thì đề khác nhau theo version | `StaffResultsController.cs:24`, `MarkingPage.tsx` | Trả đề bài Writing của đúng version |
| A6 | x | Đề trắng publish được với đáp án giữ chỗ (A, B, C…), đề đã publish không sửa được, không chấm lại được | `AdminPapersController.cs`, `PaperSkeletons.cs` | Buộc xác nhận đáp án; thêm chấm lại |
| A7 | x | Rút giám khảo khỏi phiên thì bài Final và bài đang chấm cũng bị chia lại; lưu điểm xoá điểm cũ, không lịch sử, không khoá sau Final | `MarkingAllocator.cs:70`, `StaffResultsController.cs:278` | Giữ bài Final; lưu lịch sử điểm |
| A8 | ~ | Lưu điểm và tính tổng là hai lần lưu không transaction; `manual_marks.response_id` không unique | `StaffResultsController.cs:287` | Transaction, unique index |
| A9 | x | Nghe sau Resume: máy phải bấm "Continue listening", audio trên server vẫn chạy nên mất đoạn cho tới khi bấm | `ExamScreen.tsx:127,204` | Tự phát lại khi Paused → InProgress |
| A10 | ~ | Rewind sau khi nghe hết: máy không phát lại cho tới khi tải trang | `useRecordingPlayer.ts:128` | Rời `finished` khi vị trí server còn trong bài |
| A11 | x | Sau Rewind, hộp cộng giờ áp cho **cả người bị bỏ qua** | `MonitorDialogs.tsx:67` | Chỉ người đã rewind |
| A12 | ~ | Đổi số máy mỗi dãy hoặc thêm thí sinh sau khi đã thi: người chưa bắt đầu đổi đề, hàng xóm có thể trùng đề, không cảnh báo | `AdminExamsController.cs:167`, `PaperAssignment.cs:74` | Khoá khi đã bắt đầu |
| A13 | ~ | CSV ghi điểm tạm như điểm thật, không có cột trạng thái chấm | `resultsCsv.ts:18` | Thêm cột trạng thái |

## B. Luồng bất thường

| # | KT | Vấn đề | Vị trí |
|---|---|---|---|
| B1 | x | Trạng thái kỳ thi nhảy tuỳ ý một cú bấm (Closed → Active, Active → Draft khi đang thi) | `AdminExamsController.cs:204` |
| B2 | x | Closed không đóng thật: người đang đăng nhập vẫn Start được phần sau; bài đang Pause không bao giờ hết giờ | `CandidateAttemptsController.cs:56`, `AttemptExpiry.cs:42` |
| B3 | x | Đề trắng B1 Reading không publish được từ giao diện: không có ô sửa tiêu đề cột và tiêu đề thẻ. Liên quan VE-71 và bản sửa review hôm 29/10 | `PaperEditors.tsx` |
| B4 | x | Kết thúc ở cấp bài thi (parent) và "Use test end time" chưa làm: không chỗ nào ghi `ParentEndOverrideAt`, `UseTestEndTime` không xoá nó. CLAUDE.md nói có | `AttemptExpiry.cs:20`, `AttemptClock.cs:110` |
| B5 | ~ | Resume chạy đồng hồ của người offline; trái mô tả "chỉ online" (cần quyết định) | `InvigilatorActionRules.cs:75` |
| B6 | ~ | Trang giám thị có thể không cập nhật khi phòng đông: mỗi `sectionChanged` huỷ lượt tải đang chạy | `MonitorPage.tsx:46-65` |
| B7 | ~ | Heartbeat đọc rồi ghi: Online có thể không báo về giám thị nếu xen với hub ngắt kết nối | `CandidateAttemptsController.cs:139` |
| B8 | ~ | `DbUpdateConcurrencyException` không bắt, ra 500 ở 5 chỗ | `CandidateAttemptsController.cs:80,126,161,202,291` |
| B9 | ~ | Resolve cảnh báo không giữ: ClockSkew quay lại sau 5 giây | `SecurityEventRecorder.cs:20` |
| B10 | ~ | Bài viết quá 20.000 ký tự: 400 lặp mãi và kéo theo cả lô không lưu | `autosave.ts:95` |
| B11 | ~ | Đổi vai trò hoặc tắt giám khảo: bài kẹt, cron chỉ nhìn bài chưa chia | `MarkingAllocator.cs:25,49` |
| B12 | ~ | Đổi tên kỳ thi demo làm API khởi động lỗi lặp vô hạn | `DemoSeeder.cs:179,243`, `Program.cs:73` |
| B13 | ~ | Ghi chú thí sinh còn sau khi Exit, người sau cùng tab thấy được | `App.tsx:98` |
| B14 | ~ | Sau đổi máy, thí sinh phải làm lại Readiness trong khi đồng hồ chạy | `SignInScreens.tsx:192` |
| B15 | ~ | Mix tạo nhầm không xoá được, khoá luôn đề nguồn; cột "Used by" đếm thiếu | `AdminPaperMixesController.cs`, `ApprovalActions.cs:56` |
| B16 | ~ | Trùng chỗ ngồi được chấp nhận; "cat ." bị chấm sai | `AdminCandidatesController.cs:244`, `AnswerMatcher.cs:58` |

## C. Phân quyền và bảo mật

| # | KT | Vấn đề | Vị trí |
|---|---|---|---|
| C1 | x | Quản lý đề đọc được file của người khác: chép `/api/assets/57` vào đề nháp của mình | `ExamAssetIndex.cs:57,97` |
| C2 | x | Giám khảo tải được file Reading và Listening của bài được chia | `ExamAssetIndex.cs:56` |
| C3 | x | Đổi vai trò không gỡ phân công cũ; giám thị thành giám khảo vẫn mở được phiên, PIN, phiếu mật khẩu | `ApprovalActions.cs:169`, `ExamAccess.cs:33` |
| C4 | x | Thí sinh đọc đề trước khi Start, không đồng hồ, không cảnh báo; đọc được cả file của phần chưa mở và version khác | `CandidateSectionsController.cs:31`, `ExamAssetIndex.cs:20` |
| C5 | ~ | `exam-paper-mixes` lộ nguồn, pattern, seed của người khác; `exam-papers` lộ số liệu | `AdminExamsController.cs:81,88` |
| C6 | ~ | Khi có cả 2 cookie, `/api/assets` có thể đọc nhầm id (chưa chắc) | `ExamAssetIndex.cs` |
| C7 | ~ | Yêu cầu của admin đã bị tắt vẫn duyệt được; khoá tài khoản từ xa bằng cách nhập sai 10 lần | `ApprovalActions.cs`, `LoginThrottle` |

## D. Triển khai

| # | KT | Vấn đề | Vị trí |
|---|---|---|---|
| D1 | x | Docker luôn chạy Development: admin `hoanglong/19072005` công khai trong README, có mặt trên mọi bản cài. Mở ra LAN thì thí sinh đăng nhập `/staff` được | `docker-compose.yml:45` |
| D2 | ~ | Production chưa dùng được: chỉ có HTTP, cookie Secure thì đăng nhập xong 401 | `nginx.conf`, `ApiCookieOptions.cs:18` |
| D3 | ~ | MySQL mở mọi giao diện mạng với mật khẩu mẫu | `docker-compose.yml:23` |
| D4 | x | `ResetOnStart` xoá bài demo mỗi lần API khởi động, kể cả crash; INSTALL nói dữ liệu được giữ | `appsettings.Development.json:36` |
| D5 | ~ | Thiếu index `attempts` cho các quét 2–5 giây; N+1 khi chấm ~400 truy vấn lúc 200 thí sinh hết giờ | `AttemptExpiry.cs:42`, `AttemptMarker.cs:48` |
| D6 | ~ | Khôi phục backup qua PowerShell có thể hỏng tiếng Việt (chưa chắc) | `INSTALL.md:107` |

## E. Tài liệu sai so với code

- `CLAUDE.md`: `Results` "admin or examiner" (thực tế chỉ admin); "all 25 tables" (27); "Mix two papers" (Mix papers); "candidate submit" (không có); parent-level end.
- `StaffAssignmentPanel.tsx:9` hiện cho người dùng "see the results" cho giám khảo: sai.
- `README.md:139`, `INSTALL.md:5`: chỉ nói demo A2. `README.md:34`: "Browser Exam Key" (code dùng Config Key).
- Hướng dẫn Word bỏ sót tài khoản `admin` do `.env` mẫu tạo; ZIP trên GitHub chưa có hướng dẫn cho tới khi push.

## Đã kiểm, không có vấn đề

Phép toán trộn đề và xếp chỗ; không lộ đáp án cho thí sinh; khoá phần thi theo thứ tự; PIN, xoay stamp; đếm giờ pause, incident, EndIn; 78 endpoint còn lại đúng quy tắc 404/403; duyệt hai admin không tự duyệt được; upload, zip, sanitise, CSV formula; migration giữ dữ liệu.

## Quyết định và giới hạn còn lại (07/11)

- B5: Resume cũng từ chối thí sinh offline (như Pause); thí sinh offline bị dừng chỉ chạy lại khi có mạng và giám thị cho làm tiếp.
- B4: "End the whole test" có nút hoàn tác cấp bài thi "Use test end time"; phần thi đã nộp vẫn giữ nguyên (bài làm là hồ sơ thi), các phần sau chạy đủ giờ. Sau End all, thí sinh chưa bắt đầu phần sau thì không bắt đầu được (409) cho tới khi giám thị hoàn tác.
- A7, A8: `manual_marks` giữ lịch sử, điểm mới nhất được tính; không đặt unique index trên `response_id` (mâu thuẫn với lịch sử), thay bằng khoá dòng `FOR UPDATE` khi lưu.
- A6: sửa đáp án chỉ trên đề gốc hoặc đề thường, tự áp sang các mã đề trộn đã lấy câu đó; mã đề trộn từ chối và chỉ sang đề gốc; ghi nhật ký `answer_corrections`. Gửi lại cùng bản sửa là cách khôi phục nếu việc chấm lại bị gián đoạn.
- A12: khi chuyển Active, máy trống nhận số máy nhỏ nhất chưa ai gõ; nhãn chữ như "A3" bị từ chối (không ghi đè).
- Giới hạn còn lại: End now nộp ngay nên câu trả lời còn trong 3 giây chờ tự lưu bị từ chối; `assets.duration_ms` chưa được ghi nên giới hạn tua theo độ dài bản ghi chưa có hiệu lực; heartbeat ghi hai lệnh UPDATE; chưa thử tai nghe, loa, Safe Exam Browser và mạng LAN thật của phòng máy.
