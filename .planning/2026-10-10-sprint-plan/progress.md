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
- **Status:** complete (phần Jira đang chờ site)
- Actions taken:
  - Khởi tạo plan bằng `planning-with-files` (`.planning/2026-10-10-sprint-plan/`)
  - Viết `sprints.py` → `sprint-backlog.md`, `jira-backlog.csv` (12 sprint, 13 epic, 55 story)
  - Kiểm tra Jira: tài khoản đã kết nối, chưa thấy site

## Test Results
| Test | Input | Expected | Actual | Status |
|------|-------|----------|--------|--------|
| dotnet test (có MySQL) | 16 test | 16 đạt | 16 đạt | ✓ |
| dotnet test (không có MySQL) | 16 test | 9 bỏ qua, 7 đạt | 9 bỏ qua, 7 đạt | ✓ |

## Error Log
| Timestamp | Error | Attempt | Resolution |
|-----------|-------|---------|------------|
| 2026-10-10 | Atlassian: không có site truy cập được | 1 | Chờ người dùng cung cấp site |
