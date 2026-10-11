# Định dạng gói đề (exam package) — phiên bản 1

Gói đề là **một file `.zip` cho một bài thi (paper)**, tức một phần thi của một loại kỳ thi. A2 Key for Schools có hai phần thi nên cần hai gói: Reading & Writing, và Listening. Admin import gói qua `POST /api/admin/papers/import`. Gói hợp lệ sẽ tạo đề ở trạng thái **Draft**. Gói sai thì không tạo gì cả và trả về **toàn bộ** lỗi.

```
a2-key-fs-listening.zip
├── paper.json          bắt buộc, nằm ở thư mục gốc của zip
├── images/…            hình (.png .jpg .jpeg .gif .webp)
└── audio/…             audio Listening (.mp3 .m4a .ogg .wav), mỗi part một track
```

Ví dụ đầy đủ, nội dung tự soạn (không phải đề Cambridge): `samples/a2-key-fs-reading-writing/paper.json` (32 câu) và `samples/a2-key-fs-listening/paper.json` (25 câu). Hai file này cũng là dữ liệu test của backend.

## paper.json

| Trường | Kiểu | Ghi chú |
|---|---|---|
| `formatVersion` | số | Luôn là `1` |
| `examType` | chuỗi | Mã loại kỳ thi: `A2_KEY_FS`, `B1_PRELIM_FS` |
| `section` | số | Thứ tự phần thi trong loại kỳ thi. A2: `1` = Reading & Writing, `2` = Listening. B1: `1` Reading, `2` Writing, `3` Listening |
| `title` | chuỗi | Tên đề, tối đa 256 ký tự |
| `parts` | mảng | Đúng số part của phần thi (A2 R&W: 7, A2 Listening: 5), đánh số 1, 2, 3… |

### Part

| Trường | Ghi chú |
|---|---|
| `partNo` | 1, 2, 3… |
| `layout` | `Single` (một cột) hoặc `Split` (chia đôi: bài đọc bên trái, câu hỏi bên phải, như Inspera) |
| `title` | Tiêu đề phía trên nội dung (không bắt buộc) |
| `stimulusHtml` | Bài đọc, đề viết hoặc tranh dùng chung cho cả part (HTML) |
| `audio` | **Chỉ Listening, và bắt buộc ở mọi part.** Đường dẫn trong zip, ví dụ `audio/part1.mp3` |
| `groups` | Các nhóm câu. Mỗi nhóm có một hộp hướng dẫn và một kiểu trả lời |

### Nhóm câu (group)

| Trường | Ghi chú |
|---|---|
| `interactionType` | Kiểu trả lời, xem bảng dưới |
| `instructions` | Dòng hướng dẫn, ví dụ "For each question, choose the correct answer." |
| `bodyHtml` | Đoạn văn chứa ô trống `{{q:19}}`. Bắt buộc với `InlineChoice` và `InlineText` |
| `optionPool` | `DragDropMatch`: các thẻ từ dùng chung `[{ "key": "A", "text": "cake" }]` |
| `allowOptionReuse` | `DragDropMatch`: một thẻ có được kéo vào nhiều ô không (mặc định `false`) |
| `maxWordsPerAnswer` | `InlineText`: số từ tối đa mỗi ô ("Write ONE word") |
| `questions` | Các câu hỏi |

### Câu hỏi

| Trường | Ghi chú |
|---|---|
| `number` | Số câu chạy liên tục **cả bài**: 1, 2, 3… theo thứ tự xuất hiện |
| `promptHtml` | Câu hỏi. Với `DragDropMatch` đây là nhãn của dòng (tên người) |
| `stimulusHtml` | Tài liệu riêng của câu, ví dụ ảnh thông báo ở A2 R&W Part 1 |
| `options` | Phương án `[{ "key": "A", "text": "…", "image": "images/…png" }]` |
| `answers` | Đáp án. Câu chọn: đúng **một** key. Câu gõ chữ: **một hoặc nhiều** đáp án được chấp nhận, ví dụ `["will", "'ll"]` |
| `points` | Điểm, mặc định 1 |
| `minWords` | `Essay`: số từ tối thiểu (25, 35…) |

## Kiểu trả lời và cách A2 dùng

| `interactionType` | Thí sinh làm gì | A2 dùng ở | Bắt buộc |
|---|---|---|---|
| `SingleChoice` | Chọn 1 phương án chữ | R&W Part 1, 2, 3; Listening Part 3, 4 | ≥ 2 `options`, đúng 1 đáp án nằm trong options |
| `ImageChoice` | Chọn 1 trong các tranh | Listening Part 1 | Như trên, và **mọi** option có `image` |
| `InlineChoice` | Bấm ô trống trong đoạn văn rồi chọn từ | R&W Part 4 | `bodyHtml` có đủ `{{q:n}}` cho từng câu; mỗi câu có `options` và 1 đáp án |
| `InlineText` | Gõ chữ vào ô trống | R&W Part 5; Listening Part 2 | `bodyHtml` có đủ `{{q:n}}`; mỗi câu ≥ 1 đáp án |
| `DragDropMatch` | Kéo thẻ từ vào ô | Listening Part 5 | `optionPool` ≥ 2 thẻ; mỗi câu có `promptHtml` (nhãn) và 1 đáp án nằm trong pool |
| `Essay` | Viết bài, có bộ đếm từ | R&W Part 6, 7 | `minWords`; **không** có `answers` (giáo viên chấm tay) |

## Hình và audio

- Hình nằm trong HTML được viết là `<img src='asset:images/notice1.png' alt='…'>`. Hình của phương án ghi ở `"image"`, audio ghi ở `"audio"`.
- Đường dẫn là đường dẫn tương đối trong zip. Không được có `..`, không bắt đầu bằng `/`, không chứa `:`.
- Khi import, mỗi file được lưu một lần theo SHA-256 (file trùng nội dung thì dùng lại). HTML được đổi thành `/api/assets/{id}`.
- HTML được lọc bằng HtmlSanitizer: `<script>`, `onclick`, `onerror`… đều bị xoá.
- Giới hạn: tối đa 500 file, 300 MB sau khi giải nén.

## Thông báo lỗi (ví dụ)

```json
{
  "title": "The exam package is invalid.",
  "status": 400,
  "errors": [
    "Part 1, question 1: answer 'D' is not one of the options (A, B, C).",
    "Part 4: gap {{q:21}} is missing from the text.",
    "Part 3: Listening parts need an audio track.",
    "Question numbers must run from 1 without gaps: Part 2 has question 11 where 10 was expected."
  ]
}
```

## Đóng gói và import thử

```bash
# Đóng gói một thư mục đề. --placeholders tạo hình xám và audio im lặng cho file còn thiếu (chỉ để thử)
python -I setup/exam-package/pack.py setup/exam-package/samples/a2-key-fs-reading-writing --placeholders

# Đăng nhập admin rồi import (cookie lưu trong cookies.txt)
curl -k -c cookies.txt -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"<mật khẩu>"}' https://localhost:7015/api/staff/auth/login
curl -k -b cookies.txt -F "package=@a2-key-fs-reading-writing.zip" https://localhost:7015/api/admin/papers/import
```

## Đề thật từ Victory (giai đoạn 2)

Victory gửi đề dạng PDF. Bộ chuyển PDF → `paper.json` sẽ làm khi có file thật:

1. Tách chữ theo part.
2. Gán `interactionType` theo số part (bảng trên).
3. Cắt hình.
4. Ghép đáp án theo số câu.

Bản nháp sinh ra **phải được người duyệt** trước khi import. Không bao giờ commit đề thật vào repo.
