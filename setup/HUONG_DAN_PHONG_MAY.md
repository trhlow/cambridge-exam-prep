# Chạy thử hệ thống trên phòng máy của trường

Hướng dẫn này dựng hệ thống trên **một máy chủ** và cho **nhiều máy con** (máy thí sinh, máy giám thị) trong cùng phòng vào thi thử qua mạng LAN. Dùng cho buổi chạy thử, kiểm thử và demo.

> Đây là **bản demo**: tài khoản demo có mật khẩu công khai, và mỗi lần API khởi động lại thì hai kỳ thi demo bị xoá và tạo lại. Không dùng cho một kỳ thi thật; kỳ thi thật xem `setup/INSTALL.md` mục 2.

```
MÁY CHỦ (MySQL + API + trang web, cổng 8080)
   |
   |  mạng LAN của phòng máy
   |
   +--- máy con 1 (thí sinh)
   +--- máy con 2 (thí sinh)
   +--- ...
   +--- máy giám thị
```

Máy thí sinh mở `http://<IP-máy-chủ>:8080`, máy giám thị mở `http://<IP-máy-chủ>:8080/staff`. Máy con chỉ cần Chrome hoặc Edge, không cài gì thêm.

## 1. Hỏi người phụ trách phòng máy trước khi đến

| Hỏi | Vì sao |
|---|---|
| Máy dùng làm máy chủ có **quyền quản trị** (admin) không? Cài được **Docker Desktop** không? | Quyết định dùng cách A, B hay C (mục 2) |
| Có **mở cổng TCP 8080** chiều vào trên máy chủ được không? | Không mở thì máy con không vào được. Người không có quyền admin không tự mở được |
| Các máy có cùng mạng không? Mạng có **chặn máy với máy** không (thường gặp ở Wi-Fi)? | Máy con phải gọi thẳng được tới máy chủ |
| Máy có **đóng băng** không (Deep Freeze, khởi động lại là mất hết)? | Phải cài vào ổ không bị đóng băng, hoặc dựng lại mỗi buổi |
| Máy chủ có Internet không? | Cách C cần Internet ở lần chạy đầu để tải thư viện |
| Đồng hồ các máy có đúng giờ không? | Máy lệch quá 2 phút sinh cảnh báo "đồng hồ lệch" trên trang giám thị |
| Máy con có tai nghe hoặc loa không? | Để thử Listening |

## 2. Chọn cách dựng máy chủ

| Cách | Máy chủ là | Cần | Khi nào dùng |
|---|---|---|---|
| **A** (khuyên dùng) | Laptop của bạn, cắm vào mạng phòng máy | Docker Desktop đã có sẵn trên laptop; mở cổng 8080 trên laptop | Hầu hết các trường hợp: không phụ thuộc máy trường |
| **B** | Một máy của trường | Quyền admin, cài Docker Desktop (bật ảo hoá trong BIOS, khởi động lại máy) | Trường cho cài Docker |
| **C** | Một máy của trường | Không cần admin, không cần Docker; cần Internet lần đầu và nhờ mở cổng 8080 | Trường không cho cài phần mềm |

Cả ba cách cho cùng một hệ thống ở `http://<IP-máy-chủ>:8080`. Sau khi dựng xong, làm tiếp mục 6.

## 3. Cách A: laptop của bạn làm máy chủ

**Ở nhà (có Internet), làm trước một lần:**

```powershell
cd D:\TranHoangLong-DA23TTA-DoAnChuyenNganh
git pull
docker compose -f docker/docker-compose.yml --env-file docker/.env up -d --build
```

Mở http://localhost:8080 để chắc là đã chạy. Lần build này tải sẵn mọi thứ, nên ở trường không cần Internet nữa.

**Ở phòng máy:**

1. Cắm **dây LAN** vào mạng phòng máy. Wi-Fi của trường hay chặn các máy gọi nhau, và địa chỉ IP qua Wi-Fi dễ đổi.
2. Mở Docker Desktop, đợi biểu tượng chuyển sang màu xanh, rồi chạy:

   ```powershell
   docker compose -f docker/docker-compose.yml --env-file docker/.env up -d
   ```

3. Làm tiếp mục 6.

## 4. Cách B: máy của trường làm máy chủ, cài Docker

Làm theo `setup/INSTALL.md` mục 1, 3 và 6 (hoặc tài liệu từng bước có ảnh `setup/HuongDan_CaiDat_ChayThu.docx`). Ở phòng máy cần để ý thêm:

- Docker Desktop cần bật ảo hoá (*Virtualization*, *VT-x* hoặc *SVM*) trong BIOS và WSL 2. Nếu Docker báo lỗi ảo hoá, nhờ phụ trách phòng máy bật.
- Máy bị đóng băng: chép mã nguồn vào ổ không bị đóng băng (thường là `D:`). Nếu cả máy bị đóng băng, Docker và dữ liệu mất sau mỗi lần khởi động lại; khi đó dùng cách A.
- Lần build đầu tải khoảng 1–2 GB.

## 5. Cách C: máy của trường, không có Docker, không có quyền admin

Chạy ba chương trình trong ba cửa sổ PowerShell: MySQL, API (.NET) và trang web (Node.js). Tất cả đặt trong một thư mục, ví dụ `D:\examprep`, nằm trên ổ không bị đóng băng. Cách này đã được thử ngày 18/11/2026: API tự tạo bảng trên cơ sở dữ liệu trống, tạo kỳ thi demo, và máy khác vào được qua địa chỉ LAN.

### 5.1. Tải về (có thể tải ở nhà rồi chép bằng USB)

| Thành phần | Tải | Giải nén vào |
|---|---|---|
| Mã nguồn | `https://github.com/trhlow/cambridge-exam-prep` → *Code* → *Download ZIP* (hoặc chép thư mục dự án từ laptop) | `D:\examprep\cambridge-exam-prep` |
| MySQL 8.4 | dev.mysql.com/downloads/mysql → *Windows (x86, 64-bit), ZIP Archive* | `D:\examprep\mysql` (trong đó có thư mục `bin`) |
| Node.js 22 LTS (hoặc mới hơn) | nodejs.org/en/download → *Windows Binary (.zip)* | `D:\examprep\node` (trong đó có `node.exe`) |
| .NET SDK 10 | Bằng lệnh ở mục 5.2, không cần tải tay | `D:\examprep\dotnet` |

Nếu chép thư mục dự án từ laptop, có thể bỏ thư mục `src/frontend/node_modules`, `bin` và `obj` để chép nhanh hơn.

### 5.2. Cài .NET SDK 10 (không cần admin)

```powershell
cd D:\examprep
Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1
powershell -ExecutionPolicy Bypass -File dotnet-install.ps1 -Channel 10.0 -InstallDir D:\examprep\dotnet
```

### 5.3. Cửa sổ 1: MySQL

Khởi tạo một lần (thư mục dữ liệu phải chưa có hoặc đang trống):

```powershell
cd D:\examprep\mysql\bin
.\mysqld --initialize-insecure --console --datadir=D:\examprep\mysql-data
```

Chạy MySQL (để cửa sổ này mở suốt buổi; chỉ máy chủ kết nối được, máy khác trong LAN thì không):

```powershell
cd D:\examprep\mysql\bin
.\mysqld --console --datadir=D:\examprep\mysql-data --port=3306 --bind-address=127.0.0.1 --character-set-server=utf8mb4 --collation-server=utf8mb4_0900_ai_ci --max-connections=300
```

Mở một cửa sổ PowerShell khác để tạo cơ sở dữ liệu và tài khoản, làm một lần. Đổi hai mật khẩu, chỉ dùng chữ và số:

```powershell
cd D:\examprep\mysql\bin
.\mysql -uroot -e "ALTER USER 'root'@'localhost' IDENTIFIED BY 'RootThu2026'; CREATE DATABASE examprep CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci; CREATE USER 'examprep'@'localhost' IDENTIFIED BY 'ApiThu2026'; GRANT ALL ON examprep.* TO 'examprep'@'localhost';"
```

Nếu `mysqld` báo thiếu `MSVCP140.dll` hoặc `VCRUNTIME140.dll`, máy thiếu *Microsoft Visual C++ Redistributable*; nhờ phụ trách phòng máy cài, hoặc dùng cách A.

### 5.4. Cửa sổ 2: API

```powershell
$env:PATH = "D:\examprep\dotnet;$env:PATH"; $env:DOTNET_ROOT = "D:\examprep\dotnet"
$env:ConnectionStrings__ExamPrep = "server=localhost;port=3306;database=examprep;user=examprep;password=ApiThu2026"
$env:Database__MigrateOnStart = "true"
cd D:\examprep\cambridge-exam-prep\src\backend
dotnet run --project src/ExamPrep.Api --launch-profile http
```

Lần đầu mất vài phút để tải thư viện (cần Internet) và tạo bảng. Chạy xong khi thấy dòng `Now listening on: http://localhost:5180`. API ở chế độ Development, nên tự tạo các tài khoản demo và hai kỳ thi demo A2, B1.

### 5.5. Cửa sổ 3: trang web

```powershell
$env:PATH = "D:\examprep\node;$env:PATH"
cd D:\examprep\cambridge-exam-prep\src\frontend
npm ci
npm run build
npx vite preview --host 0.0.0.0 --port 8080 --strictPort
```

`npm ci` và `npm run build` chỉ cần chạy lần đầu, hoặc khi mã nguồn đổi. Lệnh cuối mở trang web ở cổng 8080 và chuyển `/api`, `/hubs` sang API; nó in ra các dòng `Network: http://...:8080/`, trong đó có địa chỉ cho máy con.

### 5.6. Tắt và bật lại

- Tắt: nhấn `Ctrl+C` lần lượt ở cửa sổ 3, cửa sổ 2, rồi tắt MySQL bằng `D:\examprep\mysql\bin\mysqladmin -uroot -pRootThu2026 shutdown`.
- Bật lại: chạy lại lệnh `mysqld --console ...` (mục 5.3, không khởi tạo lại), lệnh của mục 5.4, rồi `npx vite preview ...` (mục 5.5).

## 6. Cho máy con vào được (chung cho cả ba cách)

1. **Xem IP của máy chủ:** `ipconfig` → dòng *IPv4 Address* của card đang nối mạng phòng máy, ví dụ `192.168.1.10`. Bỏ qua các dòng *vEthernet (WSL)*.
2. **Mở cổng 8080** trên máy chủ (PowerShell **chạy bằng quyền admin**, làm một lần; cách C thì nhờ phụ trách phòng máy chạy):

   ```powershell
   New-NetFirewallRule -DisplayName "Victory exam (8080)" -Direction Inbound -Protocol TCP -LocalPort 8080 -Action Allow -Profile Any
   ```

   Để `-Profile Any`: mạng của trường thường được Windows coi là *Public*, nên luật chỉ có *Private* sẽ không có tác dụng.
3. **Kiểm tra từ một máy con.** Mở trình duyệt vào `http://192.168.1.10:8080/api/site`. Thấy dòng `{"centreName":"Victory",...}` là thông. Nếu không thông, chạy trong PowerShell của máy con:

   ```powershell
   Test-NetConnection 192.168.1.10 -Port 8080
   ```

   `TcpTestSucceeded : True` là cổng mở; `False` là tường lửa chưa mở, sai IP, hoặc mạng chặn máy với máy (mục 10).
4. **Địa chỉ cho từng người:**
   - Thí sinh: `http://192.168.1.10:8080`
   - Giám thị, giám khảo, quản trị: `http://192.168.1.10:8080/staff`

IP của máy chủ có thể đổi sau khi khởi động lại; kiểm tra lại `ipconfig` đầu mỗi buổi.

## 7. Chuẩn bị một buổi thi thử nhiều máy

Tài khoản demo (bảng đầy đủ trong `README.md`):

| Vai | Tên đăng nhập | Mật khẩu |
|---|---|---|
| Admin | `hoanglong` | `19072005` |
| Admin thứ hai | `admin2` | `19072005` |
| Giám thị | `giamthi` | `victory-invig` |
| Giám khảo | `giamkhao` | `19072005` |

Hai kỳ thi demo chỉ có một thí sinh. Muốn mỗi máy con một thí sinh, hãy tạo một kỳ thi thử:

1. Trên máy giám thị, vào `/staff`, đăng nhập `hoanglong`, bấm **New exam**.
2. Điền **Name** (ví dụ `Thử phòng máy`), **Exam** `A2 Key for Schools`, **Date**, **Room**. Ở mục **Papers**:
   - Chọn đề cho từng phần thi. Nếu danh sách có mục *Mixed papers — given by seat*, chọn mục đó để thử chia mã đề theo vị trí máy.
   - Đặt **Computers per row** bằng số máy mỗi dãy của phòng.

   Để trống **Session PIN** (hệ thống tự tạo) và ô **Safe Exam Browser config key**. Bấm **Create exam**.
3. Trong trang kỳ thi, bấm **Add candidates**, dán danh sách dưới đây (số báo danh, họ, tên, % thêm giờ, số máy), giữ số dòng bằng số máy con, rồi bấm **Add**:

   ```
   0001,Nguyen,An,0,1
   0002,Tran,Binh,0,2
   0003,Le,Chi,0,3
   0004,Pham,Dung,0,4
   0005,Hoang,Giang,0,5
   0006,Vo,Hanh,0,6
   0007,Dang,Khanh,0,7
   0008,Bui,Linh,0,8
   0009,Do,Minh,0,9
   0010,Ngo,Nam,25,10
   ```

   Dòng cuối có 25 % thêm giờ để thử thí sinh được cộng giờ. Thêm dòng theo cùng mẫu nếu phòng có nhiều máy hơn.
4. Ở khối **Invigilators**, tích `giamthi`; ở khối **Examiners**, tích `giamkhao`.
5. Ở khối **Status**, bấm **Scheduled** rồi **Active**. Mỗi nút bấm hai lần: lần đầu hiện dòng xác nhận màu đỏ, lần hai mới đổi trạng thái.
6. Bấm **Print login slips** để in phiếu, hoặc mở trên màn hình và đọc tên đăng nhập, mật khẩu cho từng máy. Bấm **Room plan** để xem máy nào nhận mã đề nào.

## 8. Kịch bản thi thử

Ghi kết quả vào bảng ở cuối mục.

| # | Ai | Làm gì | Kết quả đúng |
|---|---|---|---|
| 1 | Mỗi máy con | Mở `http://<IP>:8080`, đăng nhập theo phiếu, **Next** | Hiện tên thí sinh |
| 2 | Giám thị | Mở trang kỳ thi, đọc **Session PIN** | |
| 3 | Mỗi máy con | Nhập PIN, **Unlock and Select**, làm *Readiness check* (thử âm thanh qua tai nghe) | Vào màn hình danh sách phần thi |
| 4 | Mỗi máy con | **Open** ở Reading & Writing → **I confirm** → **Start** | Bài thi hiện ra, đồng hồ chạy |
| 5 | Giám thị | Bấm vào ô Reading & Writing (**Monitor →**) | Mọi thí sinh *Online*, *In progress* |
| 6 | Giám thị | Tích hết, **Pause**, rồi **Resume** | Máy con dừng và chạy lại trong khoảng 2 giây |
| 7 | Một máy con | Rút dây mạng 1 phút rồi cắm lại | Trang giám thị hiện *Offline* và cảnh báo *Connection lost*; cắm lại thì máy con làm tiếp, câu trả lời vẫn còn |
| 8 | Một máy con | Trả lời vài câu, nhấn F5 | Câu trả lời và trang đang làm vẫn còn |
| 9 | Giám thị | **Edit end time** → **End now** cho Reading & Writing | Máy con về danh sách, Listening mở |
| 10 | Mỗi máy con | Làm Listening | Âm thanh phát liền một mạch, không tua được |
| 11 | Giám thị | **Rewind audio** 1 phút cho một máy, rồi cộng 1 phút giờ sự cố | Máy đó nghe lại từ chỗ cũ |
| 12 | Giám thị | Kết thúc Listening (End now) | |
| 13 | Giám khảo | Đăng nhập `giamkhao`, **Marking**, chấm Writing | Chỉ thấy mã bài, không thấy tên |
| 14 | Admin | Trang kỳ thi → **Results** → **Download for Excel (.csv)** | Có điểm của mọi thí sinh |
| 15 | Admin | **Room plan** | Hai máy cạnh nhau (cả chéo) không cùng mã đề, nếu dùng đề trộn |

Muốn thử Safe Exam Browser, làm thêm `setup/KIEM_THU_SEB.md`.

| Mục | Ghi lại |
|---|---|
| Ngày, phòng, số máy con | |
| Cách dựng máy chủ (A, B, C) | |
| Số bước đạt | /15 |
| Máy nào lỗi, lỗi gì | |

## 9. Kết thúc buổi thử

- Muốn giữ dữ liệu (kỳ thi thử, bài làm): sao lưu theo `setup/INSTALL.md` mục 8 trước khi tắt. Cách C chép cả thư mục `D:\examprep\mysql-data` (sau khi tắt MySQL) và `src\backend\src\ExamPrep.Api\App_Data`.
- Tắt: cách A và B dùng `docker compose -f docker/docker-compose.yml --env-file docker/.env stop`; cách C xem mục 5.6.
- Nếu mở cổng 8080 trên máy của trường, nhờ phụ trách phòng máy xoá luật sau buổi thử: `Remove-NetFirewallRule -DisplayName "Victory exam (8080)"`.

## 10. Sự cố thường gặp

| Hiện tượng | Cách xử lý |
|---|---|
| Máy chủ mở được `localhost:8080` nhưng máy con không vào được | Tường lửa chưa mở cổng 8080 (hoặc luật chỉ có *Private*), sai IP (lấy nhầm dòng *vEthernet*), hoặc mạng chặn máy với máy. Thử `Test-NetConnection` (mục 6). Nếu là Wi-Fi, chuyển sang dây LAN |
| Máy con mở trang được nhưng đăng nhập báo lỗi | Kỳ thi chưa ở **Active**, hoặc sai PIN. Sai mật khẩu hay PIN 10 lần thì tài khoản bị khoá 5 phút |
| Trang giám thị đổi chậm (khoảng 3 giây) | Mạng chặn WebSocket; trang tự chuyển sang tải lại định kỳ, vẫn dùng được |
| Cảnh báo *Computer clock wrong* (đồng hồ máy lệch) | Đồng hồ máy con lệch quá 2 phút. Bài thi vẫn tính giờ theo máy chủ; chỉnh giờ máy con nếu được |
| Không có tiếng ở Listening | Kiểm tra tai nghe và âm lượng ở bước *Readiness check*; thử *Play test sound* |
| Kỳ thi demo bị tạo lại, mất bài làm demo | Bình thường ở bản demo: mỗi lần API khởi động lại thì kỳ thi demo bị đặt lại. Kỳ thi bạn tự tạo (mục 7) không bị đụng đến |
| Docker: `failed to connect to the docker API` | Docker Desktop chưa chạy xong; đợi biểu tượng xanh |
| Cách C: `dotnet` hoặc `npm` không được nhận ra | Chạy lại dòng `$env:PATH = ...` ở đầu mục 5.4 hoặc 5.5 trong đúng cửa sổ đó |
| Cách C: API báo `Unable to connect to any of the specified MySQL hosts` | Cửa sổ MySQL (mục 5.3) chưa chạy, hoặc mật khẩu trong `ConnectionStrings__ExamPrep` khác mật khẩu đã đặt |
| Cách C: `npm ci` hoặc `dotnet run` báo lỗi tải gói | Máy chủ không có Internet hoặc mạng trường chặn; chạy lần đầu ở nơi có mạng, hoặc dùng cách A |
| Cách C: `Port 8080 is already in use` | Một chương trình khác đang dùng cổng 8080; tắt nó, hoặc dùng `--port 8081` và mở luật tường lửa cho 8081 |
