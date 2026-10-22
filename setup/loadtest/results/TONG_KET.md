# Tổng kết kiểm thử tải (VE-41, 22/10/2026)

## Môi trường

- Một máy xách tay chạy cả hệ thống bằng Docker Desktop (WSL2): 16 luồng CPU, 7,6 GiB RAM cho Docker. MySQL 8.4, API .NET 10, nginx; stack e2e tách riêng (cổng 8090), CSDL trống mỗi lần dựng.
- Công cụ `setup/loadtest/loadtest.mjs` chạy trên cùng máy (nên số liệu đã gồm cả tải của công cụ).
- Kịch bản: thí sinh đăng nhập rải đều, làm phần Reading & Writing A2; heartbeat 5 s/lần, lưu một câu mỗi 4–12 s, giữ kết nối SignalR; một giám thị theo dõi qua SignalR, tải lại trang giám sát mỗi 3 s, giữa giờ tạm dừng rồi cho làm tiếp một nhóm thí sinh.

## Kết quả sau khi sửa lỗi

| Chỉ số | 100 thí sinh, 5 phút | 200 thí sinh, 3 phút |
|---|---|---|
| Vào được bài / còn kết nối lúc kết thúc | 100/100 / 100/100 | 200/200 / 200/200 |
| Tổng yêu cầu, lỗi máy chủ (5xx) | 10 322, 0 | 12 681, 0 |
| Lưu câu trả lời p50 / p95 / p99 | 17 / 27 / 35 ms | 15 / 29 / 73 ms |
| Heartbeat p50 / p95 / p99 | 118 / 144 / 161 ms | 208 / 334 / 546 ms |
| Trang giám sát p50 / p95 | 19 / 27 ms | 19 / 55 ms |
| Đăng nhập thí sinh p50 / p95 | 65 / 72 ms | 65 / 76 ms |
| Lệnh tạm dừng đến màn hình thí sinh | 10/10, 16 ms | 20/20, 73 ms |
| Kết nối SignalR bị nối lại | 0 | 0 |
| CPU cao nhất: API / MySQL | 64 % / 12 % | 136 % / 71 % |

Lỗi 409 khi lưu bài (3 và 6 lần) là đúng thiết kế: thí sinh đang bị giám thị tạm dừng thì máy chủ không nhận câu trả lời. Chi tiết từng lần chạy: `2026-10-22-07-27-100.md`, `2026-10-22-07-21-200.md`.

## Lỗi kiểm thử tải tìm ra và đã sửa

1. **Kết nối SignalR bị cắt mỗi 12 giây.** Máy chủ ngắt kết nối nào im lặng quá 12 s (`ClientTimeoutInterval`), còn thư viện SignalR phía trình duyệt mặc định 15 s mới gửi keep-alive. Trang giám thị và màn hình thí sinh (khi không lưu gì) bị cắt rồi nối lại liên tục: giám thị lỡ cập nhật (chỉ 25 sự kiện trong 5 phút thay vì ~600), thí sinh chớp Offline. Sửa: trình duyệt gửi keep-alive mỗi 4 s, chờ máy chủ tối đa 12 s (`src/frontend/src/api/live.ts`, có test giữ ràng buộc).
2. **Hết kết nối CSDL ở 200 thí sinh.** Trình điều khiển MySql.Data của Oracle chạy các lệnh "async" theo kiểu đồng bộ: mỗi yêu cầu giữ một luồng và một kết nối trong lúc chờ MySQL. Với pool mặc định 100 kết nối, ở 200 thí sinh có 1 485 lỗi "max pool size was reached" và heartbeat lỗi 500. Sửa: pool 250 kết nối khi chuỗi kết nối không tự đặt (`DependencyInjection.WithPoolSize`), MySQL `--max-connections=300`, thread pool tối thiểu 200 luồng (`Program.cs`).

## Nhận xét

- Phòng thi của Victory nhỏ hơn nhiều so với 100–200 máy, nên hệ thống dư tải trên một máy chủ bình thường.
- Heartbeat là yêu cầu tốn nhất: mỗi lần ghi `last_heartbeat_at` là một commit xuống đĩa (12 ms trên ổ ảo của Docker Desktop) và đọc lại đồng hồ, câu trả lời, thông báo. Ở 200 thí sinh p99 là 0,55 s, không ảnh hưởng vì nhịp 5 s. Nếu cần phòng lớn hơn: gộp các lần ghi heartbeat thành một lệnh mỗi 1–2 s.
- CPU của API cao nhất lúc đăng nhập (băm mật khẩu) và khi tạo 100–200 thí sinh (5 s cho cả danh sách); lúc làm bài ổn định 15–25 %.
