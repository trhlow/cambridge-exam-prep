# Frontend: màn hình thí sinh

React 19 + TypeScript + Vite. Giao diện dựng theo trình thi Inspera mà Cambridge dùng cho A2 Key for Schools (xem `thesis/refs/inspera-ui-a2.md`).

## Chạy thử

```bash
# 1. Chỉ MySQL (từ thư mục gốc repo); không chạy cả bộ Docker cùng lúc với dotnet run
docker compose -f docker/docker-compose.yml --env-file docker/.env up -d mysql

# 2. API ở môi trường Development, cổng 5180. Mỗi lần khởi động xoá và tạo lại hai kỳ thi demo do bộ tạo demo làm
#    (mục "Demo" trong src/backend/src/ExamPrep.Api/appsettings.Development.json); kỳ thi tự tạo không bị đụng đến
cd src/backend
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/ExamPrep.Api --launch-profile http

# 3. Frontend, cổng 5173. /api được chuyển tiếp sang API
cd src/frontend
npm install
npm run dev
```

Mở http://localhost:5173. Đăng nhập bằng tài khoản thí sinh demo và mã PIN ghi trong mục `Demo` của `appsettings.Development.json`.

## Lệnh

| Lệnh | Việc |
|---|---|
| `npm run dev` | Chạy dev server |
| `npm test` | Chạy test (Vitest + Testing Library) |
| `npm run build` | Kiểm tra kiểu và build |
| `npm run lint` | oxlint |
| `npm run e2e` | Playwright trên stack Docker riêng cổng 8090 (xem CLAUDE.md) |

## Cấu trúc

- `src/api`: gọi API (cookie HttpOnly, cùng origin) và các kiểu dữ liệu. Đáp án không bao giờ được gửi xuống trình duyệt.
- `src/screens`: đăng nhập, nhập PIN, chọn phần thi.
- `src/exam`: màn làm bài:
  - `TopBar`, `BottomBar` (thanh part "n of m"), `PartView` (một cột hoặc chia đôi);
  - `SplitPane` (thanh kéo ↔), `ReviewPage`;
  - `questions/`: chọn đáp án, chọn từ trên ô trống, gõ chữ, viết bài có đếm từ;
  - `paperLogic`: chia trang, ô trống `{{q:n}}`, đếm từ.
