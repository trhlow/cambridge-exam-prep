# Cài máy chủ của trường từ xa qua UltraViewer

Hướng dẫn này cài hệ thống thi lên **một máy chủ của trường** khi bạn không ngồi ở phòng máy: bạn điều khiển máy chủ từ laptop của mình qua UltraViewer. Sau khi cài, các **máy con** trong phòng (máy thí sinh, máy giám thị) vào thi qua mạng LAN bằng trình duyệt, không cài gì thêm.

> Đây là bản demo, dùng cho thi thử: tài khoản demo có mật khẩu công khai, và mỗi lần hệ thống khởi động lại thì hai kỳ thi demo bị tạo lại. Kỳ thi bạn tự tạo thì không bị ảnh hưởng. Kỳ thi thật xem `setup/INSTALL.md` mục 2.

Cách cài này không cần Docker và không cần quyền admin. Mọi thứ nằm trong một thư mục `D:\examprep`; không ghi registry, không đổi cấu hình Windows. Chỉ hai việc cần người có quyền admin ở trường: mở cổng 8080 (Phần 6) và, nếu máy thiếu, cài Visual C++ Redistributable (Phần 1).

## 1. Chuẩn bị trước buổi cài

Gửi bảng này cho người phụ trách phòng máy trước một ngày.

| Việc | Ai làm | Cách kiểm tra |
|---|---|---|
| Bật máy chủ, cắm **dây LAN** vào mạng phòng máy | Người ở trường | Máy có Internet và cùng mạng với các máy con |
| Cài và mở **UltraViewer** trên máy chủ, gửi cho bạn **ID** và **mật khẩu** | Người ở trường | Bạn kết nối thử được từ laptop |
| Tắt chế độ ngủ của máy chủ trong suốt buổi thi | Người ở trường | *Settings → System → Power & sleep → Sleep: Never* |
| Máy chủ có tệp `C:\Windows\System32\msvcp140.dll` | Người có quyền admin | Nếu không có: tải và cài https://aka.ms/vs/17/release/vc_redist.x64.exe |
| Ổ `D:` còn trống ít nhất 3 GB và không bị đóng băng (Deep Freeze) | Người ở trường | Nếu ổ D: bị đóng băng thì mọi thứ mất sau khi khởi động lại |
| Có người có **quyền admin** lúc cài để mở cổng 8080 | Người ở trường | Xem Phần 6 |
| Máy con có Chrome hoặc Edge, có tai nghe hoặc loa | Người ở trường | Để thử phần Listening |

## 2. Kết nối vào máy chủ

1. Trên laptop, mở **UltraViewer**, nhập **ID đối tác** và **mật khẩu** người ở trường gửi, bấm **Kết nối**.
2. Trên máy chủ (trong cửa sổ UltraViewer), bấm **Start**, gõ `PowerShell`, chọn **Windows PowerShell**. Mở bình thường, không cần *Run as administrator*.
3. Các lệnh trong hướng dẫn này có thể chép trên laptop rồi dán vào cửa sổ PowerShell của máy chủ: UltraViewer dùng chung bộ nhớ chép (clipboard) giữa hai máy. Trong PowerShell, bấm chuột phải để dán. Nếu dán không được, gõ tay.

## 3. Tải script cài đặt

Dán ba dòng sau vào PowerShell của máy chủ, rồi nhấn Enter:

```powershell
mkdir D:\examprep-setup
cd D:\examprep-setup
Invoke-WebRequest https://raw.githubusercontent.com/trhlow/cambridge-exam-prep/main/setup/lab/cai-dat.ps1 -OutFile cai-dat.ps1
```

Gõ `dir` để kiểm tra: phải thấy tệp `cai-dat.ps1`.

Nếu mạng của trường chặn GitHub, dùng cách gửi tệp:

1. Trên laptop, tạo gói mã nguồn (chỉ gồm phần máy chủ cần chạy, khoảng 8 MB). Mở PowerShell trong thư mục dự án:

   ```powershell
   git archive --format zip --prefix cambridge-exam-prep/ -o D:\examprep-setup\ma-nguon.zip HEAD src setup/lab setup/exam-package/samples
   copy setup\lab\cai-dat.ps1 D:\examprep-setup\
   ```

2. Trong UltraViewer, dùng chức năng **gửi tệp** để chép hai tệp `cai-dat.ps1` và `ma-nguon.zip` vào thư mục `D:\examprep-setup` của máy chủ.
3. Ở Phần 4, thêm `-SourceZip .\ma-nguon.zip` vào cuối lệnh cài.

## 4. Cài đặt (làm một lần)

Dán lệnh sau vào PowerShell của máy chủ:

```powershell
cd D:\examprep-setup
powershell -ExecutionPolicy Bypass -File cai-dat.ps1
```

Script tải và cài sáu phần, mỗi phần hiện một dòng `== n/6`:

| Bước | Script làm gì |
|---|---|
| 1/6 MySQL 8.4 | Tải MySQL (khoảng 260 MB), tạo cơ sở dữ liệu `examprep` |
| 2/6 Node.js 22 | Tải Node.js (khoảng 35 MB) |
| 3/6 .NET SDK 10 | Tải .NET SDK (khoảng 300 MB) |
| 4/6 Mã nguồn | Tải mã nguồn từ GitHub và chỉ giữ lại phần cần chạy |
| 5/6 Trang web | Tải thư viện và build trang web |
| 6/6 API | Tải thư viện và build máy chủ API |

Lần đầu mất khoảng **15–20 phút** tuỳ tốc độ mạng. Xong khi thấy dòng màu xanh `Cài xong`.

Nếu script dừng giữa chừng, đọc dòng lỗi cuối cùng và xem Phần 10. Sửa xong thì chạy lại đúng lệnh trên: những phần đã cài sẽ được bỏ qua.

## 5. Bật hệ thống

Dán lệnh sau vào PowerShell của máy chủ:

```powershell
powershell -ExecutionPolicy Bypass -File D:\examprep\cambridge-exam-prep\setup\lab\chay.ps1
```

Ba cửa sổ mới hiện ra, tên là **1 MySQL**, **2 API** và **3 Web**. **Để ba cửa sổ này mở suốt buổi thi**: đóng cửa sổ nào thì phần đó tắt.

Lần đầu, bước "Đợi API" mất 1–2 phút để tạo bảng dữ liệu. Khi xong, script in ra địa chỉ cho máy con, ví dụ:

```
Hệ thống đã chạy. Địa chỉ cho máy con:
  Thí sinh:              http://192.168.1.10:8080
  Giám thị, quản trị:    http://192.168.1.10:8080/staff
```

Ghi lại địa chỉ này. Nếu script in thêm dòng màu vàng "Chưa có luật tường lửa", làm tiếp Phần 6.

Thoát UltraViewer không làm tắt hệ thống, vì hệ thống chạy trên máy chủ.

## 6. Mở cổng 8080 (cần quyền admin)

Máy con chỉ vào được khi tường lửa của máy chủ mở cổng 8080. Làm một lần:

1. Trên máy chủ, bấm **Start**, gõ `PowerShell`, bấm chuột phải vào **Windows PowerShell**, chọn **Run as administrator**, nhập tài khoản admin nếu được hỏi.
2. Dán lệnh:

   ```powershell
   New-NetFirewallRule -DisplayName "Victory exam (8080)" -Direction Inbound -Protocol TCP -LocalPort 8080 -Action Allow -Profile Any
   ```

Khi Windows hỏi quyền admin, màn hình trong UltraViewer có thể chuyển đen. Lúc đó nhờ người ở trường bấm **Yes** trên chính máy chủ.

## 7. Kiểm tra từ một máy con

1. Trên một máy con, mở Chrome hoặc Edge, vào `http://<IP-máy-chủ>:8080/api/site` (thay `<IP-máy-chủ>` bằng địa chỉ ở Phần 5).
2. Thấy dòng bắt đầu bằng `{"centreName":"Victory"` là máy con đã vào được.
3. Nếu không vào được, mở PowerShell trên máy con và chạy (thay địa chỉ):

   ```powershell
   Test-NetConnection 192.168.1.10 -Port 8080
   ```

   `TcpTestSucceeded : True` là cổng đã mở. `False` là tường lửa chưa mở (Phần 6), sai địa chỉ, hoặc mạng chặn các máy gọi nhau (thường gặp với Wi-Fi; chuyển sang dây LAN).

## 8. Đăng nhập và tạo buổi thi thử

Tài khoản demo:

| Vai | Địa chỉ | Tên đăng nhập | Mật khẩu |
|---|---|---|---|
| Quản trị | `http://<IP>:8080/staff` | `hoanglong` | `19072005` |
| Quản trị thứ hai | `http://<IP>:8080/staff` | `admin2` | `19072005` |
| Giám thị | `http://<IP>:8080/staff` | `giamthi` | `victory-invig` |
| Giám khảo | `http://<IP>:8080/staff` | `giamkhao` | `19072005` |

Hai kỳ thi demo có sẵn chỉ có một thí sinh. Để mỗi máy con một thí sinh, tạo một kỳ thi thử:

1. Vào `/staff`, đăng nhập `hoanglong`, bấm **New exam**, điền tên, chọn **A2 Key for Schools**, chọn đề cho từng phần thi, đặt **Computers per row** bằng số máy mỗi dãy, bấm **Create exam**.
2. Bấm **Add candidates**, dán danh sách thí sinh mỗi dòng một người theo mẫu `0001,Nguyen,An,0,1` (số báo danh, họ, tên, phần trăm thêm giờ, số máy), bấm **Add**.
3. Tích giám thị `giamthi` và giám khảo `giamkhao`.
4. Ở khối **Status**, bấm **Scheduled** rồi **Active** (mỗi nút bấm hai lần để xác nhận).
5. Bấm **Print login slips** để có tên đăng nhập và mật khẩu cho từng máy. Mã PIN của buổi thi ở đầu trang kỳ thi.
6. Trên mỗi máy con, mở `http://<IP>:8080`, đăng nhập theo phiếu, nhập mã PIN.

Kịch bản thi thử 15 bước và bảng ghi kết quả có trong `setup/HUONG_DAN_PHONG_MAY.md` mục 7–8.

## 9. Sau buổi thi

- **Tắt hệ thống**: trên máy chủ, chạy `powershell -ExecutionPolicy Bypass -File D:\examprep\cambridge-exam-prep\setup\lab\chay.ps1 -Stop`, rồi đóng ba cửa sổ.
- **Bật lại lần sau**: chỉ chạy lại lệnh ở Phần 5, không cài lại. Địa chỉ IP của máy chủ có thể đổi sau khi khởi động lại, nên đọc lại địa chỉ script in ra.
- **Lấy bản mới của mã nguồn**: tắt hệ thống, xoá thư mục `D:\examprep\cambridge-exam-prep`, chạy lại lệnh cài ở Phần 4 (các phần khác không tải lại).
- **Gỡ hẳn**: tắt hệ thống rồi xoá thư mục `D:\examprep` và `D:\examprep-setup`. Nhờ người có quyền admin xoá luật tường lửa: `Remove-NetFirewallRule -DisplayName "Victory exam (8080)"`.

## 10. Sự cố thường gặp

| Hiện tượng | Cách xử lý |
|---|---|
| `Invoke-WebRequest` báo lỗi khi tải | Máy chủ không có Internet hoặc mạng trường chặn trang tải. Dùng cách gửi tệp ở Phần 3; MySQL, Node.js và .NET vẫn cần Internet, nếu bị chặn hẳn thì nhờ trường mở mạng |
| Bước 1/6 báo thiếu `MSVCP140.dll` hoặc `VCRUNTIME140.dll` | Máy thiếu Visual C++ Redistributable: người có quyền admin cài https://aka.ms/vs/17/release/vc_redist.x64.exe rồi chạy lại lệnh cài |
| `chay.ps1` báo "Chưa cài" | Lệnh cài ở Phần 4 chưa chạy xong; chạy lại nó |
| "Đợi API" quá lâu rồi báo lỗi | Xem cửa sổ **2 API**: dòng lỗi màu đỏ cuối cùng cho biết nguyên nhân. Thường là cửa sổ **1 MySQL** đã bị đóng; chạy lại lệnh ở Phần 5 |
| `Port 8080 is already in use` | Chương trình khác đang dùng cổng 8080. Thêm `-Port 8081` vào lệnh ở Phần 5 và mở cổng 8081 ở Phần 6 |
| Máy chủ mở được trang nhưng máy con thì không | Tường lửa chưa mở (Phần 6), sai địa chỉ IP, hoặc mạng chặn các máy gọi nhau. Thử `Test-NetConnection` ở Phần 7 |
| Máy con đăng nhập báo lỗi | Kỳ thi chưa ở trạng thái **Active**, hoặc sai mã PIN. Sai 10 lần thì tài khoản bị khoá 5 phút |
| Mất kết nối UltraViewer giữa chừng | Hệ thống vẫn chạy trên máy chủ. Kết nối lại rồi làm tiếp từ bước đang dở |
