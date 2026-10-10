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
