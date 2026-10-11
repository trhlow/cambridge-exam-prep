# Frontend: màn hình thí sinh

React 19 + TypeScript + Vite. Giao diện dựng theo trình thi Inspera mà Cambridge dùng cho A2 Key for Schools (xem `thesis/refs/inspera-ui-a2.md`).

## Chạy thử

```bash
# 1. MySQL (từ thư mục gốc repo)
docker compose -f docker/docker-compose.yml --env-file docker/.env up -d

# 2. API ở môi trường Development, cổng 5180. Lần chạy đầu tự tạo kỳ thi demo
#    (mục "Demo" trong src/backend/src/ExamPrep.Api/appsettings.Development.json)
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

## Cấu trúc

- `src/api`: gọi API (cookie HttpOnly, cùng origin) và các kiểu dữ liệu. Đáp án không bao giờ được gửi xuống trình duyệt.
- `src/screens`: đăng nhập, nhập PIN, chọn phần thi.
- `src/exam`: màn làm bài:
  - `TopBar`, `BottomBar` (thanh part "n of m"), `PartView` (một cột hoặc chia đôi);
  - `SplitPane` (thanh kéo ↔), `ReviewPage`;
  - `questions/`: chọn đáp án, chọn từ trên ô trống, gõ chữ, viết bài có đếm từ;
  - `paperLogic`: chia trang, ô trống `{{q:n}}`, đếm từ.
