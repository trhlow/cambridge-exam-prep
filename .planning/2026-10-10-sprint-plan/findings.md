# Findings & Decisions

## Requirements
- Hệ thống giống Cambridge/Inspera (bố cục đề, format, giao diện, tiện ích), thi tại trung tâm trên máy tính.
- A2 Key for Schools trước (demo thứ Sáu 16/10/2026), sau đó B1 Preliminary for Schools, IELTS sau cùng.
- Xây lại toàn bộ, kể cả nhập đề (hệ thống cũ của Victory không dùng được).
- Logo Victory hoặc logo mẫu; admin đổi logo bằng PNG.
- Chấm tự động Reading/Listening theo file đáp án; Writing chấm tay.
- Commit thường xuyên, push mỗi Chủ nhật khi người dùng yêu cầu.
- Kế hoạch sprint phải đưa lên Jira.

## Research Findings
- Cấu trúc A2 Key fS: R&W 32 câu (Part 1–5 tự chấm 30 câu, Part 6–7 viết), Listening 25 câu / 5 part. Chi tiết: `thesis/refs/inspera-ui-a2.md`.
- Công cụ Atlassian (Rovo MCP) có tạo/sửa/tìm issue nhưng **không có API tạo sprint**; sprint phải tạo trên board hoặc qua CSV import.
- `atlassianUserInfo` trả về tài khoản HOÀNG LONG; `getAccessibleAtlassianResources` rỗng → chưa có site Jira nào truy cập được qua connector.

## Technical Decisions
| Decision | Rationale |
|----------|-----------|
| Backlog sinh từ `sprints.py` | Một nguồn cho task_plan.md và CSV Jira, không lệch nhau |
| CSV dùng `Issue Id` / `Parent Id` | Jira Cloud CSV import dựng quan hệ Epic → Story từ hai cột này |

## Resources
- Bài thi mẫu A2 R&W: https://ceq.inspera.com/player/?assessmentRunId=146724783&context=exam
- Bài thi mẫu A2 Listening: https://ceq.inspera.com/player/?assessmentRunId=146724576&context=exam
- Bài thi mẫu B1 Reading / Writing / Listening: assessmentRunId 160272732 / 146730107 / 146728853

## Rà soát CSDL trước Sprint 1 (10/10/2026)

Đối chiếu 21 bảng với `thesis/refs/inspera-ui-a2.md` (7 part R&W, 5 part Listening) và quy trình giám thị trong `thesis/refs/cambridge-digital/`.

**Đã đủ** — mọi dạng câu A2 biểu diễn được: Part 1 (ảnh thông báo theo từng câu → `questions.stimulus_html`), Part 2/3 (chia đôi → `paper_parts.layout`), Part 4 (từ trên ô trống → `InlineChoice` + `options_json` theo câu), Part 5 & Listening Part 2 (gõ từ → `InlineText`, khung email/biểu mẫu trong `body_html`), Part 6–7 (viết + đếm từ → `Essay`, `min_words`), Listening Part 1 (tranh → `ImageChoice`), Part 5 (kéo thả → `DragDropMatch` + `option_pool_json`). Đếm "n of m" và cờ suy ra từ `responses`.

**Lỗ hổng phải sửa trong schema**
| # | Vấn đề | Hậu quả | Sửa |
|---|---|---|---|
| D1 | `attempts.concurrency_stamp` phải tự đổi tay mỗi lần cập nhật | Quên một chỗ là mất bảo vệ ghi đè giữa giám thị và thí sinh | DbContext tự đổi stamp cho mọi Attempt bị sửa |
| D2 | `end_at_override` lưu mốc tuyệt đối | Tạm dừng sau "End at/End in" không còn dời giờ kết thúc; "End now" không giữ được giờ bù nếu không chọn Disregard | Thay bằng `end_adjust_seconds` (có dấu), mọi thao tác quy về số giây điều chỉnh |
| D3 | Không có cách vô hiệu phiên đăng nhập | Thí sinh chuyển máy thì máy cũ vẫn làm bài; khoá nhân viên không đá được phiên | Thêm `security_stamp` cho `users` và `candidates` |
| D4 | Không biết giám thị cho bắt đầu phần thi lúc nào | Không phát hiện được "thí sinh làm sớm" (Booklet: pause candidate who started early) | Thêm `exam_sections.start_announced_at` (chỉ để phát hiện, không chặn) |
| D5 | Không có ràng buộc giá trị | Thời lượng âm, % giờ bù > 100, điểm âm vẫn ghi được | CHECK constraint ở MySQL |

**Để ở tầng ứng dụng (ghi nhận, không đổi schema)**
- Số câu duy nhất trong một đề; đề gắn vào phần thi phải cùng `section_template`; số part khớp `part_count` → kiểm tra khi import (VE-5).
- Không cho xoá kỳ thi đã có lượt làm bài (giữ nhật ký giám thị) → quy tắc API.
- `candidates.password_protected` cần khoá Data Protection được lưu bền (file/DB), nếu không khởi động lại sẽ không in lại được phiếu → VE-22.
- `session_pin`, `invigilator_password` lưu rõ vì phải hiển thị cho giám thị → chỉ trả về cho nhân viên được phân công.
- Mọi mốc thời gian lưu UTC; giao diện hiển thị giờ Việt Nam (UTC+7).
- Ảnh nhúng trong HTML không có khoá ngoại tới `assets` → không xoá cứng asset đang được tham chiếu.
- Admin đầu tiên tạo lúc khởi động từ cấu hình (VE-3).

## VE-4 — quyết định đăng nhập thí sinh (11/10)
| Quyết định | Lý do |
|---|---|
| Hai bước: username/password rồi PIN, cùng một cookie (claim `pin_verified`) | Giống Inspera: giám thị đọc PIN khi cả phòng đã đăng nhập |
| Chỉ đăng nhập khi `exams.status = Active` (403 sau khi mật khẩu đúng) | Không cho vào trước ngày thi; không lộ thông tin khi sai mật khẩu |
| Mỗi lần đăng nhập đổi `candidates.security_stamp` | Chuyển máy thì máy cũ tự đăng xuất, khỏi hai máy cùng làm một bài |
| Cookie thí sinh tách scheme với nhân viên | Cookie này không gọi được API của bên kia |
| `DateOnly` cần converter với `MySql.EntityFrameworkCore` | Provider đọc cột `date` thành `DateTime`, ném InvalidCastException |
| Brute-force PIN 4 chữ số | Để VE-30 (rate limit) |
