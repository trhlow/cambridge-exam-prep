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
