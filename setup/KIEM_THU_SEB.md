# Kiểm thử Safe Exam Browser trên phòng máy thật

Các test tự động chỉ giả lập header mà SEB gửi (`X-SafeExamBrowser-ConfigKeyHash`). Kịch bản này kiểm tra SEB thật trên máy của trung tâm trước ngày thi đầu tiên. Cần khoảng 45 phút, gồm một máy chủ, ít nhất một máy thí sinh và một máy giám thị.

## Chuẩn bị

1. Máy chủ chạy bản thi thật (`setup/INSTALL.md` mục 2, `https://<ip-máy-chủ>`) hoặc bản demo (`http://<ip-máy-chủ>:8080`). Nên dùng đúng bản sẽ thi.
2. Tạo một kỳ thi thử có một thí sinh và gắn đề A2 (Reading & Writing + Listening). Không dùng kỳ thi thật.
3. Tạo tệp `.seb` theo `setup/INSTALL.md` (mục *Khoá máy thí sinh bằng Safe Exam Browser*), rồi chép Config Key vào ô *SEB Config Key* của kỳ thi thử. Trang kỳ thi phải hiện "Safe Exam Browser: required".
4. Ghi lại phiên bản Windows, phiên bản SEB (*About* trong SEB) và địa chỉ Start URL.

## Các bước

| # | Làm gì | Kết quả đúng | Nếu sai |
|---|--------|--------------|---------|
| 1 | Trên máy thí sinh, mở Chrome hoặc Edge, vào trang đăng nhập, nhập username và mật khẩu của thí sinh | Báo *Open this test in Safe Exam Browser.* | Ô Config Key của kỳ thi đang trống |
| 2 | Mở tệp `.seb` | SEB chiếm toàn màn hình và vào thẳng trang đăng nhập | Kiểm tra Start URL |
| 3 | Đăng nhập, nhập PIN, xem tên thí sinh, qua bước kiểm tra âm thanh | Vào được trang danh sách phần thi | Nếu báo *Open this test in Safe Exam Browser.*: chưa bật *Use Browser Exam Key and Configuration Key (send in HTTP header)*, hoặc địa chỉ đang mở khác Start URL (khác http/https, khác cổng, IP thay vì tên máy). Máy chủ băm đúng địa chỉ trình duyệt gọi. |
| 4 | Giám thị mở trang theo dõi, phần Reading & Writing | Thí sinh *Never started*, cột cảnh báo trống | |
| 5 | Thí sinh bắt đầu Reading & Writing, trả lời vài câu, gõ một đoạn Writing, chờ 5 giây rồi nhấn F5 | Các câu trả lời vẫn còn sau khi tải lại, ảnh trong đề hiện đủ | Nếu câu trả lời mất hoặc ảnh không hiện: SEB không gửi header cho các yêu cầu nền (`fetch`) hoặc ảnh. Ghi lại phiên bản SEB và báo lại; kỳ thi không dùng SEB được cho tới khi sửa. |
| 6 | Trên trang giám thị: thí sinh *Online*, cột cảnh báo vẫn 0 | Đúng như vậy | Nếu có cảnh báo *Not in Safe Exam Browser*: một yêu cầu nào đó không mang header, mở cảnh báo để xem giờ |
| 7 | Giám thị bấm Pause cho thí sinh | Màn hình thí sinh tạm dừng trong khoảng 2 giây | Nếu mất 5 giây: kênh trực tiếp (SignalR) bị từ chối vì SEB không gửi header khi mở WebSocket. Xem nhật ký: `docker compose ... logs api` tìm dòng `Live connection of candidate ... refused`. Kỳ thi vẫn chạy, vì máy thí sinh hỏi máy chủ mỗi 5 giây, nhưng hãy ghi lại. |
| 8 | Giám thị bấm Resume, rồi End now để kết thúc Reading & Writing | Thí sinh quay về danh sách phần thi, Listening mở | |
| 9 | Thí sinh làm Listening, nghe qua tai nghe khoảng 2 phút | Âm thanh chạy liền, chỉnh được âm lượng | Nếu không có tiếng: tệp nghe không mang header, ghi lại như bước 5 |
| 10 | Giám thị tua audio lùi 1 phút (Rewind), rồi cộng 1 phút giờ sự cố | Máy thí sinh phát lại từ chỗ cũ trong vài giây | |
| 11 | Thí sinh thử Alt+Tab, Alt+F4, Ctrl+Alt+Del, phím Windows, Print Screen | Không rời được bài thi; Alt+F4 hoặc nút thoát hỏi mật khẩu | |
| 12 | Nhập mật khẩu giám thị của kỳ thi để thoát SEB | SEB đóng | Mật khẩu thoát trong tệp `.seb` khác mật khẩu giám thị |
| 13 | Mở Chrome trên máy đó và đăng nhập lại thí sinh | Bị từ chối như bước 1 | |
| 14 | Mở lại tệp `.seb`, đăng nhập lại thí sinh | Vào lại đúng Listening, đồng hồ vẫn chạy theo máy chủ | |
| 15 | Nếu đã đặt luật chặn `/staff` trong SEB: gõ `/staff` vào địa chỉ trong SEB (nếu SEB hiện thanh địa chỉ) hoặc mở bằng liên kết | SEB chặn trang | Kiểm tra luật URL filter |

## Ghi kết quả

| Mục | Giá trị |
|-----|---------|
| Ngày thử, người thử | |
| Windows / SEB | |
| Start URL | |
| Bước đạt / tổng | /15 |
| Bước 7 (kênh trực tiếp) | 2 giây / 5 giây |
| Ghi chú | |

Bước 5, 6 hoặc 9 không đạt nghĩa là không thể dùng SEB cho kỳ thi thật. Khi đó xoá Config Key của kỳ thi (thí sinh vào bằng trình duyệt thường, giám thị giám sát như phòng thi giấy) và báo lại phiên bản SEB.
