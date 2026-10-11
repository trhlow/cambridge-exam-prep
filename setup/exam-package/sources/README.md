# Đề gốc (không commit)

Thư mục này chứa đề gốc do Cambridge/Victory cung cấp (PDF câu hỏi, đáp án, tapescript, audio), dùng làm đầu vào cho bộ chuyển PDF → gói đề (VE-56). Mọi thứ ở đây đều bị `.gitignore` chặn, trừ file README này. **Không bao giờ commit đề thật.**

Mỗi bộ đề một thư mục con, đặt tên theo `<loại-kỳ-thi>-<nguồn>`:

| Thư mục | Nội dung |
|---|---|
| `a2-key-fs-2022-sample/` | Đề mẫu chính thức A2 Key for Schools 2022: R&W (câu hỏi, đáp án), Listening (đề, đáp án, tapescript, 1 file mp3 31 phút cho cả 5 part), Speaking (ngoài phạm vi), tài liệu Test & Train |

`pdf2package.py` ghi bản nháp vào thư mục con của bộ đề, ví dụ `a2-key-fs-2022-sample/package-reading-writing/`, gồm `paper.json`, `images/` và `REVIEW.md`. Sau khi duyệt, đóng gói thành `package-reading-writing.zip` rồi import qua `POST /api/admin/papers/import` (xem `../FORMAT.md`).

Kỳ thi demo đọc thư mục `a2-key-fs-2022-sample/package-reading-writing/` (cài đặt `Demo:ReadingWritingFolder`). Máy nào không có thư mục này thì kỳ thi demo dùng đề mẫu tự soạn.
