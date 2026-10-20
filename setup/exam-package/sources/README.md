# Đề gốc (không commit)

Thư mục này chứa đề gốc do Cambridge/Victory cung cấp (PDF câu hỏi, đáp án, tapescript, audio), dùng làm đầu vào cho bộ chuyển PDF → gói đề (VE-56). Mọi thứ ở đây đều bị `.gitignore` chặn, trừ file README này. **Không bao giờ commit đề thật.**

Mỗi bộ đề một thư mục con, đặt tên theo `<loại-kỳ-thi>-<nguồn>`:

| Thư mục | Nội dung |
|---|---|
| `a2-key-fs-2022-sample/` | Đề mẫu chính thức A2 Key for Schools 2022: R&W (câu hỏi, đáp án), Listening (đề, đáp án, tapescript, 1 file mp3 31 phút cho cả 5 part, và `audio-parts/` gồm 5 file `P1_…mp3` … `P5_…mp3` đã tách theo part), Speaking (ngoài phạm vi), tài liệu Test & Train |
| `a2-key-2020-sample/` | Đề mẫu A2 Key (bản thường, không "for Schools"): R&W và Listening (đề 2022, đáp án và tapescript 2020), 1 file mp3 ~30 phút cho cả 5 part — **chưa tách** `audio-parts/`; Speaking. Đề gốc thứ hai của A2 để trộn đề (E15) |
| `b1-pet-2022-sample/` | Đề mẫu B1 Preliminary 2022: Reading, Writing, Listening (đề, đáp án, tapescript, 1 file mp3 cho cả 4 part), Speaking |
| `b1-pet-fs-sample/` | Đề mẫu B1 Preliminary for Schools: đề 2022 (Reading, Writing, Listening), đáp án và tapescript 2020, 1 file mp3 cho cả 4 part, Speaking |
| `_chua-phan-loai/` | File chưa rõ thuộc đề nào (`B1 Q1 L P1 P2 V1.mp3`, ~15 phút) |

Trộn đề (E15): mỗi cấp độ có hai đề gốc cùng khuôn — A2: `a2-key-fs-2022-sample` + `a2-key-2020-sample`; B1: `b1-pet-2022-sample` + `b1-pet-fs-sample`.

`pdf2package.py` ghi bản nháp vào thư mục con của bộ đề, ví dụ `a2-key-fs-2022-sample/package-reading-writing/`, gồm `paper.json`, `images/` và `REVIEW.md`. Sau khi duyệt, đóng gói thành `package-reading-writing.zip` rồi import qua `POST /api/admin/papers/import` (xem `../FORMAT.md`).

Listening: thêm `--audio-dir a2-key-fs-2022-sample/audio-parts`, bản nháp nằm ở `package-listening/` (có thêm `audio/`).

Kỳ thi demo đọc `a2-key-fs-2022-sample/package-reading-writing/` và `package-listening/` (cài đặt `Demo:ReadingWritingFolder`, `Demo:ListeningFolder`). Máy nào không có các thư mục này thì kỳ thi demo dùng đề mẫu tự soạn.
