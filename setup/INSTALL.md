# Hướng dẫn cài đặt hệ thống thi Victory

Cài toàn bộ hệ thống (cơ sở dữ liệu MySQL, API, giao diện thí sinh và cổng nhân viên) trên **một máy chủ trong phòng thi** bằng Docker. Các máy thí sinh chỉ cần trình duyệt và mở địa chỉ của máy chủ qua mạng LAN.

> Bản hiện tại chạy ở chế độ **Development** (có sẵn kỳ thi demo A2 và tài khoản demo), dùng cho buổi demo, thi thử và kiểm thử. Triển khai thi thật cần chạy HTTPS — sẽ bổ sung ở sprint đóng gói cuối.

## 1. Cài nhanh (khoảng 10 phút)

```powershell
git clone https://github.com/trhlow/cambridge-exam-prep.git
cd cambridge-exam-prep
copy docker\.env.example docker\.env        # rồi mở docker\.env, đổi các mật khẩu (mục 3)
docker compose -f docker/docker-compose.yml --env-file docker/.env up -d --build
```

Khi lệnh chạy xong (lần đầu mất vài phút để tải và build), mở:

| Địa chỉ | Dành cho |
|---|---|
| http://localhost:8080 | Thí sinh (đăng nhập + mã PIN phòng thi) |
| http://localhost:8080/staff | Nhân viên: admin, quản trị viên, giám khảo, giám thị |
| http://localhost:8080/health | Kiểm tra: trả về `Healthy` là hệ thống và CSDL đều chạy |

## 2. Yêu cầu

| Thành phần | Tối thiểu |
|---|---|
| Máy chủ | Windows 10/11 64-bit (hoặc Linux), CPU 4 nhân, RAM 8 GB, trống 20 GB ổ đĩa |
| Phần mềm | [Docker Desktop](https://www.docker.com/products/docker-desktop/) (bật WSL 2), Git |
| Máy thí sinh | Chrome hoặc Edge bản mới; cùng mạng LAN với máy chủ |
| Mạng | Cổng `8080` (web) mở trên máy chủ; cổng `3306` (MySQL) chỉ cần khi phát triển |

Lần đầu cài Docker Desktop cần quyền quản trị Windows và khởi động lại máy.

## 3. Cấu hình `docker/.env`

File này chứa mật khẩu nên **không được đưa lên Git** (đã có trong `.gitignore`).

| Biến | Ý nghĩa |
|---|---|
| `MYSQL_ROOT_PASSWORD` | Mật khẩu root của MySQL (chỉ dùng để sao lưu, quản trị) |
| `MYSQL_DATABASE`, `MYSQL_USER`, `MYSQL_PASSWORD` | CSDL và tài khoản mà API dùng |
| `MYSQL_PORT` | Cổng MySQL mở ra máy chủ (mặc định 3306) |
| `WEB_PORT` | Cổng của trang web (mặc định 8080) |
| `BOOTSTRAP_ADMIN_USERNAME`, `BOOTSTRAP_ADMIN_PASSWORD` | Admin đầu tiên, **chỉ được tạo khi CSDL chưa có admin nào** (mật khẩu ≥ 12 ký tự) |

Đổi `.env` xong thì chạy lại lệnh `up -d` ở mục 1 để áp dụng.

## 4. Tài khoản

- **Admin đầu tiên**: tên và mật khẩu trong `BOOTSTRAP_ADMIN_*`. Đăng nhập `/staff` → **Staff** để tạo các tài khoản còn lại: admin thứ hai, quản trị viên (chọn mảng: đề thi, kỳ thi, thí sinh, trung tâm), giám khảo, giám thị. Từ admin thứ ba trở đi, và mọi thao tác xoá hoặc đổi tài khoản, cần admin còn lại duyệt ở trang **Approvals**.
- **Tài khoản demo** (chế độ Development tự tạo nếu chưa có): xem bảng trong [README](../README.md#chạy-cả-hệ-thống-bằng-docker-một-lệnh).
- **Thí sinh** nhận tên đăng nhập kiểu Cambridge và mật khẩu in trên phiếu (trang kỳ thi → *Print login slips*); mã PIN phòng thi do giám thị đọc khi bắt đầu.

## 5. Cho máy thí sinh truy cập qua mạng LAN

1. Xem địa chỉ IP của máy chủ: `ipconfig` → dòng *IPv4 Address*, ví dụ `192.168.1.10`.
2. Mở cổng 8080 trên tường lửa Windows (PowerShell **chạy bằng quyền quản trị**, làm một lần):

   ```powershell
   New-NetFirewallRule -DisplayName "Victory exam (8080)" -Direction Inbound -Protocol TCP -LocalPort 8080 -Action Allow -Profile Private
   ```

3. Trên mỗi máy thí sinh mở `http://192.168.1.10:8080`. Giám thị mở `http://192.168.1.10:8080/staff` trên máy của mình.

Trang theo dõi của giám thị cập nhật tức thì qua SignalR (WebSocket). Nếu mạng chặn WebSocket, trang tự chuyển sang tải lại mỗi 3 giây.

## 6. Hằng ngày

```powershell
docker compose -f docker/docker-compose.yml --env-file docker/.env ps        # xem trạng thái
docker compose -f docker/docker-compose.yml --env-file docker/.env logs -f api   # xem nhật ký API
docker compose -f docker/docker-compose.yml --env-file docker/.env stop      # tắt (dữ liệu vẫn giữ)
docker compose -f docker/docker-compose.yml --env-file docker/.env start     # bật lại
```

Các container có `restart: unless-stopped`: khởi động lại máy chủ thì hệ thống tự chạy lại khi Docker Desktop mở.

**Cập nhật phiên bản mới:**

```powershell
git pull
docker compose -f docker/docker-compose.yml --env-file docker/.env up -d --build
```

API tự cập nhật cấu trúc CSDL (migration) khi khởi động; dữ liệu cũ được giữ nguyên. Nên sao lưu (mục 7) trước khi cập nhật.

## 7. Sao lưu và khôi phục

Dữ liệu nằm ở hai nơi: **CSDL MySQL** (volume Docker `examprep_mysql-data`) và thư mục **`src/backend/src/ExamPrep.Api/App_Data`** (hình, audio của đề; khoá mã hoá cookie và mật khẩu thí sinh). Phải sao lưu cả hai.

**Sao lưu** (PowerShell, tại thư mục dự án):

```powershell
$d = Get-Date -Format yyyyMMdd-HHmm
docker exec examprep-mysql-1 sh -c 'exec mysqldump -uroot -p"$MYSQL_ROOT_PASSWORD" --single-transaction --routines examprep' > "backup-$d.sql"
Compress-Archive src\backend\src\ExamPrep.Api\App_Data "backup-appdata-$d.zip"
```

**Khôi phục** (ghi đè dữ liệu hiện tại):

```powershell
Get-Content backup-<ngày>.sql -Raw | docker exec -i examprep-mysql-1 sh -c 'exec mysql -uroot -p"$MYSQL_ROOT_PASSWORD" examprep'
Expand-Archive backup-appdata-<ngày>.zip -DestinationPath src\backend\src\ExamPrep.Api -Force
docker compose -f docker/docker-compose.yml --env-file docker/.env restart api
```

Giữ bản sao lưu ở ổ khác hoặc ổ USB. Nên sao lưu ngay sau mỗi buổi thi.

## 8. Xử lý sự cố

| Hiện tượng | Cách xử lý |
|---|---|
| `failed to connect to the docker API` | Docker Desktop chưa chạy: mở Docker Desktop, đợi biểu tượng chuyển xanh rồi chạy lại lệnh |
| `port is already allocated` (8080 hoặc 3306) | Cổng đang bị chương trình khác dùng: đổi `WEB_PORT` / `MYSQL_PORT` trong `docker/.env` |
| `/health` báo `Unhealthy` | MySQL chưa sẵn sàng hoặc sai mật khẩu: xem `logs mysql` và `logs api`; mật khẩu trong `.env` phải giống lúc tạo volume lần đầu |
| Không đăng nhập được admin đầu tiên | `BOOTSTRAP_ADMIN_*` chỉ có tác dụng khi CSDL chưa có admin; dùng admin hiện có, hoặc admin còn lại đặt lại mật khẩu (cần duyệt) |
| Tài khoản bị khoá 5 phút | Nhập sai mật khẩu (hoặc mã PIN) 10 lần liên tiếp; đợi 5 phút |
| Máy thí sinh không mở được trang | Kiểm tra cùng mạng, đúng IP, đã mở tường lửa cổng 8080 (mục 5) |
| Thí sinh "Offline" trên trang giám thị | Máy mất mạng hoặc đóng trình duyệt; đồng hồ vẫn chạy trên server, giám thị cộng giờ bù khi thí sinh vào lại |
| Sau khi đổi máy chủ, thí sinh phải in lại mật khẩu | Thư mục `App_Data/keys` không được chép theo: khôi phục `App_Data` từ bản sao lưu |

## 9. Dành cho người phát triển

Chạy riêng MySQL bằng Docker, API bằng `dotnet run` và giao diện bằng `npm run dev`; lệnh và cách chạy test có trong [README](../README.md) và [CLAUDE.md](../CLAUDE.md). Định dạng gói đề: [exam-package/FORMAT.md](exam-package/FORMAT.md).
