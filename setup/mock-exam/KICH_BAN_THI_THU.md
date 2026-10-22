# Kịch bản buổi thi thử (VE-42)

Mục tiêu: chạy một buổi thi trên máy giống ngày thi thật, với người đóng vai thí sinh, giám thị và giám khảo, để tìm lỗi mà kiểm thử tự động không thấy (thao tác thật, mạng thật, nhiều máy cùng lúc). Kết quả ghi vào biên bản `BienBan_ThiThu_Mau.docx` (sao ra, đặt tên theo ngày) và danh sách lỗi; lỗi mức **Cao** sửa trong VE-43.

## Người tham gia

| Vai | Số người | Tài khoản |
|---|---|---|
| Quản trị viên / admin (chuẩn bị kỳ thi) | 1 (có thể là sinh viên thực hiện) | `hoanglong`, admin thứ hai `admin2` để duyệt yêu cầu |
| Giám thị | 1 | `giamthi` (hoặc tài khoản giám thị tạo mới) |
| Giám khảo | 1 | `giamkhao` |
| Thí sinh | 4–8, mỗi người một máy | tên đăng nhập và mật khẩu in trên phiếu |

## Chuẩn bị (trước buổi thi 1 ngày)

1. Một máy chủ chạy `docker compose -f docker/docker-compose.yml --env-file docker/.env up -d --build` (xem `setup/INSTALL.md`); các máy thí sinh cùng mạng LAN, mở `http://<địa chỉ máy chủ>:8080`.
2. Admin vào `/staff`:
   - Papers: kiểm tra có đề A2 (R&W, Listening) và B1 (Reading, Writing, Listening) đã Publish; nếu muốn thử trộn đề, chọn "Mix two papers".
   - Exams → New: tạo kỳ thi "Thi thử A2 <ngày>" (hoặc B1), chọn đề hoặc bộ trộn cho từng phần thi, số máy mỗi hàng đúng với phòng.
   - Dán danh sách thí sinh (số báo danh, họ, tên, % thêm giờ, số máy) từ Excel; cho một thí sinh 25 % thêm giờ.
   - Phân công giám thị và giám khảo; đặt trạng thái kỳ thi Active.
   - In phiếu đăng nhập và sơ đồ phòng (Room plan).
3. Thử tai nghe và loa ở từng máy (Listening).

## Diễn biến buổi thi và các tình huống cần thử

Mỗi dòng là một mục trong biên bản; ghi **Đạt / Không đạt** và mô tả nếu có lỗi.

| # | Lúc | Ai làm | Tình huống | Kết quả mong đợi |
|---|---|---|---|---|
| 1 | Đầu giờ | Thí sinh | Đăng nhập bằng phiếu, nhập mã PIN giám thị đọc | Vào màn hình chọn phần thi; Listening bị khoá |
| 2 | Đầu giờ | Thí sinh | Gõ sai PIN 2–3 lần | Báo sai, không vào được; đúng PIN thì vào |
| 3 | Đầu giờ | Giám thị | Một thí sinh bắt đầu trước, sau đó bấm "Announce start" | Thí sinh bắt đầu sớm có cảnh báo StartedEarly ở cột Warnings |
| 4 | Trong giờ | Thí sinh | Làm bài, dùng ghi chú/highlight, đổi cỡ chữ và độ tương phản (☰) | Câu trả lời lưu (biểu tượng lưu), giao diện đổi ngay |
| 5 | Trong giờ | Thí sinh | Tải lại trang (F5) | Quay lại đúng phần thi, đúng trang, còn đáp án, đồng hồ đúng |
| 6 | Trong giờ | Giám thị | Tạm dừng một thí sinh, rồi cả phòng; cho làm tiếp | Màn hình thí sinh dừng ngay (≤ 2 s), đồng hồ không trừ giờ khi dừng |
| 7 | Trong giờ | Giám thị | Gửi thông báo cho cả phòng và cho một thí sinh | Chuông báo trên màn hình đúng người nhận |
| 8 | Trong giờ | Thí sinh + giám thị | Rút dây mạng (hoặc tắt Wi-Fi) một máy 1 phút rồi cắm lại | Trang giám thị báo Offline và cảnh báo ConnectionLost; đồng hồ vẫn chạy; giám thị cộng giờ bù sau khi thí sinh vào lại |
| 9 | Trong giờ | Thí sinh | Chuyển sang máy khác, đăng nhập lại | Máy cũ bị đăng xuất; máy mới tiếp tục đúng đề (đề trộn không đổi) |
| 10 | Trong giờ | Giám thị | Cộng giờ bù sự cố 5 phút cho một thí sinh; Edit end time → End in 2 phút cho thí sinh khác | Giờ kết thúc trên trang giám thị và màn hình thí sinh đổi đúng |
| 11 | Listening | Giám thị | Sự cố âm thanh: tua lại 1 phút rồi cộng 1 phút giờ bù | Audio phát lại từ chỗ cũ; thêm 1 phút |
| 12 | Writing B1 | Thí sinh | Part 2 chọn câu 2 (Yes), thử chọn thêm câu 3 | Câu 3 không chọn Yes được; dòng "1 of 1 question selected" |
| 13 | Cuối giờ | Giám thị | End now cho một thí sinh (hai lần bấm) | Thí sinh thấy "The test has now finished"; không hoàn tác được |
| 14 | Cuối giờ | Hệ thống | Để các thí sinh còn lại hết giờ (kể cả người được 25 % thêm giờ) | Tự nộp đúng giờ của từng người, kể cả máy đang offline |
| 15 | Sau thi | Giám khảo | Chấm Writing, xem bảng kết quả, xuất CSV mở bằng Excel | Điểm Final = tự động + Writing; CSV đọc đúng tiếng Việt |
| 16 | Sau thi | Admin | Yêu cầu xoá một thí sinh; admin2 duyệt | Không xoá ngay; xoá sau khi admin khác duyệt; có nhật ký |

## Ghi lỗi

Mỗi lỗi một dòng trong biên bản: mô tả, bước tái hiện, máy/trình duyệt, ảnh chụp, mức độ:

- **Cao**: làm mất bài, sai giờ, sai điểm, thí sinh không làm tiếp được, lộ đáp án.
- **Trung bình**: thao tác của giám thị/giám khảo sai hoặc khó dùng nhưng có cách khác.
- **Thấp**: giao diện, chữ, bố cục.

Sau buổi thi, đưa các lỗi vào Jira (VE-43) và sửa hết lỗi mức Cao trước khi viết Chương 4.
