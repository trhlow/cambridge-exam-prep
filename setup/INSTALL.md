# Hướng dẫn cài đặt hệ thống thi Victory

Cài toàn bộ hệ thống (cơ sở dữ liệu MySQL, API, giao diện thí sinh và cổng nhân viên) trên **một máy chủ trong phòng thi** bằng Docker. Các máy thí sinh chỉ cần trình duyệt và mở địa chỉ của máy chủ qua mạng LAN.

> Có **hai cách chạy**, dùng hai lệnh khác nhau:
>
> - **Bản demo** (mục 1): chế độ Development, có sẵn hai kỳ thi demo (A2 Key for Schools và B1 Preliminary for Schools) và các tài khoản demo với mật khẩu công khai trong README. Dùng cho buổi demo, thi thử và kiểm thử. **Không được dùng bản demo cho một kỳ thi thật**: ai cùng mạng LAN cũng biết tài khoản quản trị của nó, và mỗi lần API khởi động lại (kể cả sau một sự cố) kỳ thi demo bị xoá và tạo lại, kèm bài làm của thí sinh demo (mục 1, "Kỳ thi demo được đặt lại thế nào").
> - **Bản thi thật** (mục 2): chế độ Production, chạy qua HTTPS, không có kỳ thi demo và không có tài khoản mẫu; admin đầu tiên do bạn đặt trong `docker/.env`.

## 1. Cài nhanh (khoảng 10 phút)

```powershell
git clone https://github.com/trhlow/cambridge-exam-prep.git
cd cambridge-exam-prep
copy docker\.env.example docker\.env        # rồi mở docker\.env, đổi các mật khẩu (mục 4)
docker compose -f docker/docker-compose.yml --env-file docker/.env up -d --build
```

Khi lệnh chạy xong (lần đầu mất vài phút để tải và build), mở (bản demo chạy HTTP, cổng 8080):

| Địa chỉ | Dành cho |
|---|---|
| http://localhost:8080 | Thí sinh (đăng nhập + mã PIN phòng thi) |
| http://localhost:8080/staff | Nhân viên: admin, quản trị viên, giám khảo, giám thị |
| http://localhost:8080/health | Kiểm tra: trả về `Healthy` là hệ thống và CSDL đều chạy |

**Kỳ thi demo được đặt lại thế nào.** Ở bản demo, `Demo:ResetOnStart` đang bật: mỗi lần API khởi động (gồm cả khi container khởi động lại sau lỗi hoặc sau khi tắt máy), hệ thống xoá hai kỳ thi demo và tạo lại từ đầu, cùng bài làm, đáp án và điểm của thí sinh demo. Chỉ những kỳ thi do bộ tạo demo tạo ra (cột `exams.is_demo`) mới bị xoá, dù đã đổi tên; kỳ thi bạn tự tạo, kể cả trùng tên, không bao giờ bị đụng đến (API ghi cảnh báo vào nhật ký). Hai kỳ thi demo đã có sẵn trong một CSDL cũ được đánh dấu một lần khi cập nhật. Nếu bộ tạo demo gặp lỗi (đề mẫu hỏng, tên đã bị trùng), lỗi được ghi vào nhật ký và API vẫn chạy.

## 2. Triển khai thi thật qua HTTPS

Bản thi thật dùng thêm tệp `docker/docker-compose.prod.yml` và **luôn có tên dự án riêng** (`-p examprep-prod`) để không dùng chung CSDL với bản demo. Khác bản demo: môi trường Production (không có kỳ thi demo, không tài khoản mẫu, không hiện chi tiết lỗi), cookie đăng nhập chỉ gửi qua HTTPS, nginx phục vụ HTTPS ở cổng 443 (cổng 80 chỉ chuyển sang HTTPS), hình và audio của đề cùng khoá mã hoá nằm trong volume Docker `api-data`, các đề mẫu không được gắn vào.

1. Đặt `BOOTSTRAP_ADMIN_USERNAME` và `BOOTSTRAP_ADMIN_PASSWORD` (≥ 12 ký tự) trong `docker/.env`; bản thi thật không chạy nếu thiếu. Đổi mọi mật khẩu mẫu trong file này.
2. Tạo chứng chỉ cho địa chỉ IP của máy chủ (xem bằng `ipconfig`; ví dụ `192.168.1.10`). Thư mục `docker/certs/` đã nằm trong `.gitignore`, **không bao giờ đưa khoá riêng lên Git**:

   ```powershell
   mkdir docker\certs
   openssl req -x509 -newkey rsa:2048 -nodes -days 825 -keyout docker/certs/server.key -out docker/certs/server.crt -subj "/CN=192.168.1.10" -addext "subjectAltName=IP:192.168.1.10"
   ```

   `openssl` có sẵn trong Git for Windows (`C:\Program Files\Git\usr\bin\openssl.exe`). Nếu chạy trong Git Bash, đặt `MSYS_NO_PATHCONV=1` trước lệnh. Nếu đã có chứng chỉ do một tổ chức cấp, chép thành `server.crt` (kèm chuỗi chứng chỉ) và `server.key` vào cùng thư mục.
3. Chạy:

   ```powershell
   docker compose -p examprep-prod -f docker/docker-compose.yml -f docker/docker-compose.prod.yml --env-file docker/.env up -d --build
   ```

4. Mở cổng 80 và 443 trên tường lửa (lệnh ở mục 6, đổi `-LocalPort 80,443`). Máy thí sinh và giám thị mở `https://192.168.1.10` (nhân viên: `https://192.168.1.10/staff`). Cổng 80 và 443 là cổng chuẩn; muốn đổi thì đặt `HTTP_PORT`, `HTTPS_PORT` trong `docker/.env` (khi đó địa chỉ phải có cổng, và cổng 80 chuyển sang HTTPS sẽ không còn đúng).
5. Chứng chỉ tự ký: cài `docker/certs/server.crt` vào *Trusted Root Certification Authorities* trên mọi máy (PowerShell quyền quản trị: `certutil -addstore -f Root server.crt`), nếu không trình duyệt báo "không an toàn" và thí sinh phải bấm bỏ qua cảnh báo. Thử mở trang đăng nhập bằng Safe Exam Browser trước ngày thi.
6. Safe Exam Browser: *Start URL* là địa chỉ `https://…`. Máy chủ tính Config Key từ đúng địa chỉ mà trình duyệt gọi (kể cả cổng), nên nhập địa chỉ giống nhau ở cấu hình SEB và khi mở trang.

Cookie ở bản thi thật có cờ Secure: đăng nhập qua `http://` không giữ được phiên (đăng nhập xong vẫn bị 401). Vì vậy phải dùng địa chỉ `https://`. Bản thi thật và bản demo không chạy cùng lúc trên một máy nếu dùng chung cổng; đổi `WEB_PORT`/`HTTP_PORT`/`HTTPS_PORT` nếu cần chạy cả hai.

Lệnh hằng ngày và cập nhật của bản thi thật giống mục 7, chỉ thêm `-p examprep-prod -f docker/docker-compose.yml -f docker/docker-compose.prod.yml` vào mọi lệnh `docker compose`.

## 3. Yêu cầu

| Thành phần | Tối thiểu |
|---|---|
| Máy chủ | Windows 10/11 64-bit (hoặc Linux), CPU 4 nhân, RAM 8 GB, trống 20 GB ổ đĩa |
| Phần mềm | [Docker Desktop](https://www.docker.com/products/docker-desktop/) (bật WSL 2), Git |
| Máy thí sinh | Chrome hoặc Edge bản mới; cùng mạng LAN với máy chủ |
| Mạng | Bản demo: cổng `8080` (web) mở trên máy chủ. Bản thi thật: cổng `80` và `443`. MySQL (`3306`) chỉ mở cho chính máy chủ (`127.0.0.1`), máy khác trong LAN không kết nối được |

Lần đầu cài Docker Desktop cần quyền quản trị Windows và khởi động lại máy.

## 4. Cấu hình `docker/.env`

File này chứa mật khẩu nên **không được đưa lên Git** (đã có trong `.gitignore`).

| Biến | Ý nghĩa |
|---|---|
| `MYSQL_ROOT_PASSWORD` | Mật khẩu root của MySQL (chỉ dùng để sao lưu, quản trị) |
| `MYSQL_DATABASE`, `MYSQL_USER`, `MYSQL_PASSWORD` | CSDL và tài khoản mà API dùng |
| `MYSQL_PORT` | Cổng MySQL mở trên máy chủ, chỉ ở địa chỉ 127.0.0.1 (mặc định 3306) |
| `WEB_PORT` | Cổng của trang web ở bản demo (mặc định 8080) |
| `HTTP_PORT`, `HTTPS_PORT` | Cổng web ở bản thi thật (mặc định 80 và 443) |
| `BOOTSTRAP_ADMIN_USERNAME`, `BOOTSTRAP_ADMIN_PASSWORD` | Admin đầu tiên, **chỉ được tạo khi CSDL chưa có admin nào** (mật khẩu ≥ 12 ký tự). Bản thi thật bắt buộc phải đặt; `.env.example` đã có hai dòng này, bản demo tự tạo thêm các tài khoản demo |

Đổi `.env` xong thì chạy lại lệnh `up -d` (mục 1 hoặc mục 2) để áp dụng.

## 5. Tài khoản

- **Admin đầu tiên**: tên và mật khẩu trong `BOOTSTRAP_ADMIN_*`. Đăng nhập `/staff` → **Staff** để tạo các tài khoản còn lại: admin thứ hai, quản trị viên (chọn mảng: đề thi, kỳ thi, thí sinh, trung tâm), giám khảo, giám thị. Từ admin thứ ba trở đi, và mọi thao tác xoá hoặc đổi tài khoản, cần admin còn lại duyệt ở trang **Approvals**.
- **Tài khoản `admin`**: nếu giữ `.env.example` nguyên văn thì admin đầu tiên tên `admin` với mật khẩu mẫu `change-me-admin-password`; đổi mật khẩu này trước khi cài.
- **Tài khoản demo** (chỉ bản demo, chế độ Development tự tạo nếu chưa có; bản thi thật không có): xem bảng trong [README](../README.md#chạy-cả-hệ-thống-bằng-docker-một-lệnh).
- **Thí sinh** nhận tên đăng nhập kiểu Cambridge và mật khẩu in trên phiếu (trang kỳ thi → *Print login slips*); mã PIN phòng thi do giám thị đọc khi bắt đầu.

## 6. Cho máy thí sinh truy cập qua mạng LAN

1. Xem địa chỉ IP của máy chủ: `ipconfig` → dòng *IPv4 Address*, ví dụ `192.168.1.10`.
2. Mở cổng 8080 (bản demo) trên tường lửa Windows (PowerShell **chạy bằng quyền quản trị**, làm một lần):

   ```powershell
   New-NetFirewallRule -DisplayName "Victory exam (8080)" -Direction Inbound -Protocol TCP -LocalPort 8080 -Action Allow -Profile Private
   ```

3. Trên mỗi máy thí sinh mở `http://192.168.1.10:8080` (bản thi thật: `https://192.168.1.10`, mục 2). Giám thị mở `http://192.168.1.10:8080/staff` trên máy của mình.

Trang theo dõi của giám thị cập nhật tức thì qua SignalR (WebSocket). Nếu mạng chặn WebSocket, trang tự chuyển sang tải lại mỗi 3 giây.

### Khoá máy thí sinh bằng Safe Exam Browser

Kỳ thi Cambridge chạy trong trình duyệt khoá; ở đây dùng Safe Exam Browser (SEB, miễn phí, safeexambrowser.org):

1. Cài SEB trên mọi máy thí sinh. Trên một máy, mở **SEB Config Tool** và đặt:
   - *Start URL*: `http://192.168.1.10:8080` (địa chỉ máy chủ ở trên).
   - *Quit/unlock password*: **mật khẩu giám thị của kỳ thi** (Invigilator password, xem ở trang kỳ thi). Giống Inspera, mật khẩu này dùng để thoát trình duyệt khoá trước giờ kết thúc và khi chuyển thí sinh sang máy khác.
   - Bật *Use Browser & Config Keys (send in HTTP header)*.
2. Lưu tệp `.seb`, chép **Config Key** (64 ký tự) mà Config Tool hiện ra vào ô *SEB Config Key* của kỳ thi. Từ đó máy chủ từ chối mọi yêu cầu không đến từ SEB với đúng tệp cấu hình này.
3. Trước khi thí sinh vào phòng, mở tệp `.seb` trên từng máy để SEB vào thẳng trang đăng nhập (như hướng dẫn ngày thi của Cambridge: mở trình duyệt khoá và để sẵn trang đăng nhập trên mọi máy).

## 7. Hằng ngày

```powershell
docker compose -f docker/docker-compose.yml --env-file docker/.env ps        # xem trạng thái
docker compose -f docker/docker-compose.yml --env-file docker/.env logs -f api   # xem nhật ký API
docker compose -f docker/docker-compose.yml --env-file docker/.env stop      # tắt (dữ liệu vẫn giữ)
docker compose -f docker/docker-compose.yml --env-file docker/.env start     # bật lại
```

Các lệnh trên là của bản demo (tên dự án `examprep`). Bản thi thật thêm `-p examprep-prod -f docker/docker-compose.yml -f docker/docker-compose.prod.yml`.

Các container có `restart: unless-stopped`: khởi động lại máy chủ thì hệ thống tự chạy lại khi Docker Desktop mở.

**Cập nhật phiên bản mới:**

```powershell
git pull
docker compose -f docker/docker-compose.yml --env-file docker/.env up -d --build
```

API tự cập nhật cấu trúc CSDL (migration) khi khởi động; dữ liệu cũ được giữ nguyên. Nên sao lưu (mục 8) trước khi cập nhật.

## 8. Sao lưu và khôi phục

Dữ liệu nằm ở hai nơi: **CSDL MySQL** (volume Docker `<dự án>_mysql-data`) và thư mục **`App_Data`** của API (hình, audio của đề; khoá mã hoá cookie và mật khẩu thí sinh). Phải sao lưu cả hai. Bản demo để `App_Data` ở `src/backend/src/ExamPrep.Api/App_Data`; bản thi thật để trong volume `<dự án>_api-data` và lấy ra bằng `docker cp`. Tên container là `<dự án>-mysql-1` và `<dự án>-api-1`: `examprep-…` ở bản demo, `examprep-prod-…` ở bản thi thật (các lệnh dưới đặt tên container MySQL vào biến `$m`).

Các lệnh dưới chạy mysqldump/mysql **bên trong container** và chép tệp bằng `docker cp`, không dùng ống dẫn (`|`, `>`) của PowerShell, vì PowerShell 5 ghi lại chuỗi theo bảng mã của nó và có thể làm hỏng tiếng Việt trong tệp `.sql`. Đã thử: sao lưu rồi khôi phục vào một CSDL mới giữ nguyên tên "Hoàng Long".

**Sao lưu** (PowerShell, tại thư mục dự án). Dòng đầu chọn container MySQL của bản đang dùng:

```powershell
$m = "examprep-mysql-1"            # bản demo; bản thi thật: $m = "examprep-prod-mysql-1"
$d = Get-Date -Format yyyyMMdd-HHmm
docker exec $m sh -c 'mysqldump -uroot -p"$MYSQL_ROOT_PASSWORD" --default-character-set=utf8mb4 --single-transaction --routines examprep > /tmp/backup.sql'
docker cp "${m}:/tmp/backup.sql" "backup-$d.sql"
# App_Data: bản demo là thư mục trên ổ đĩa, bản thi thật nằm trong container
Compress-Archive src\backend\src\ExamPrep.Api\App_Data "backup-appdata-$d.zip"            # bản demo
docker cp examprep-prod-api-1:/app/App_Data "backup-appdata-$d"                                 # bản thi thật
```

**Khôi phục** (ghi đè dữ liệu hiện tại; làm khi hệ thống không có người thi), cũng đặt `$m` như trên:

```powershell
docker cp backup-<ngày>.sql "${m}:/tmp/restore.sql"
docker exec $m sh -c 'mysql -uroot -p"$MYSQL_ROOT_PASSWORD" --default-character-set=utf8mb4 examprep < /tmp/restore.sql'
```

Bản demo, sau đó:

```powershell
Expand-Archive backup-appdata-<ngày>.zip -DestinationPath src\backend\src\ExamPrep.Api -Force
docker compose -f docker/docker-compose.yml --env-file docker/.env restart api
```

Bản thi thật, sau đó (`docker cp` chép tệp với chủ sở hữu root, còn API chạy bằng người dùng `app`, nên phải trả quyền lại, nếu không API không ghi được hình mới và khoá cookie):

```powershell
docker cp "backup-appdata-<ngày>/." examprep-prod-api-1:/app/App_Data
docker exec -u 0 examprep-prod-api-1 chown -R app:app /app/App_Data
docker compose -p examprep-prod -f docker/docker-compose.yml -f docker/docker-compose.prod.yml --env-file docker/.env restart api
```

Các lệnh trên viết cho PowerShell. Trong Git Bash, đặt `m=examprep-mysql-1`, viết `"$m:/tmp/…"` thay cho `"${m}:/tmp/…"`, và thêm `MSYS_NO_PATHCONV=1` trước mỗi lệnh `docker cp` / `docker exec` để Git Bash không đổi các đường dẫn `/tmp/…`, `/app/…` thành đường dẫn Windows. Mật khẩu MySQL chỉ nên dùng chữ và số, vì PowerShell 5 bỏ dấu ngoặc kép bên trong `sh -c '…'`.

Tệp `/tmp/backup.sql` và `/tmp/restore.sql` nằm trong container MySQL, mất khi container được tạo lại; có thể xoá bằng `docker exec $m rm /tmp/backup.sql /tmp/restore.sql`. Giữ bản sao lưu ở ổ khác hoặc ổ USB. Nên sao lưu ngay sau mỗi buổi thi.

## 9. Xử lý sự cố

| Hiện tượng | Cách xử lý |
|---|---|
| `failed to connect to the docker API` | Docker Desktop chưa chạy: mở Docker Desktop, đợi biểu tượng chuyển xanh rồi chạy lại lệnh |
| `port is already allocated` (8080, 80, 443 hoặc 3306) | Cổng đang bị chương trình khác dùng: đổi `WEB_PORT`, `HTTP_PORT`, `HTTPS_PORT` hoặc `MYSQL_PORT` trong `docker/.env` |
| `/health` báo `Unhealthy` | MySQL chưa sẵn sàng hoặc sai mật khẩu: xem `logs mysql` và `logs api`; mật khẩu trong `.env` phải giống lúc tạo volume lần đầu |
| Không đăng nhập được admin đầu tiên | `BOOTSTRAP_ADMIN_*` chỉ có tác dụng khi CSDL chưa có admin; dùng admin hiện có, hoặc admin còn lại đặt lại mật khẩu (cần duyệt) |
| Tài khoản bị khoá 5 phút | Nhập sai mật khẩu (hoặc mã PIN) 10 lần liên tiếp; không có nút mở khoá, kể cả với quản trị viên: đợi 5 phút rồi đăng nhập lại |
| Máy thí sinh không mở được trang | Kiểm tra cùng mạng, đúng IP, đã mở tường lửa cổng 8080 hoặc 80/443 (mục 6) |
| Thí sinh "Offline" trên trang giám thị | Máy mất mạng hoặc đóng trình duyệt; đồng hồ vẫn chạy trên server, giám thị cộng giờ bù khi thí sinh vào lại |
| Bản thi thật: đăng nhập xong vẫn bị đăng xuất / 401 | Đang mở bằng `http://`; cookie Secure chỉ chạy với `https://` (mục 2) |
| Bản thi thật không khởi động, báo thiếu `BOOTSTRAP_ADMIN_…` | Đặt hai biến này trong `docker/.env` (mục 2) |
| Sau khi đổi máy chủ, thí sinh phải in lại mật khẩu | Thư mục `App_Data/keys` không được chép theo: khôi phục `App_Data` từ bản sao lưu |

## 10. Dành cho người phát triển

Image của API chạy bằng người dùng `app` (không phải root), nên thư mục `App_Data` gắn từ ổ đĩa của máy Linux phải ghi được bởi uid 64198 (Docker Desktop trên Windows không cần làm gì). Chạy riêng MySQL bằng Docker, API bằng `dotnet run` và giao diện bằng `npm run dev`; lệnh và cách chạy test có trong [README](../README.md) và [CLAUDE.md](../CLAUDE.md). Định dạng gói đề: [exam-package/FORMAT.md](exam-package/FORMAT.md).
