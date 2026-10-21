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
| `bodyHtml` | Đoạn văn chứa ô trống `{{q:19}}`. Bắt buộc với `InlineChoice` và `InlineText`; với `DragDropMatch` thì thẻ được kéo vào các ô trống này (B1 Reading Part 4) |
| `optionPool` | `DragDropMatch`: các thẻ dùng chung `[{ "key": "A", "text": "cake" }]`; thẻ dài có thể có `"title"` in đậm trước chữ (B1 Reading Part 2) |
| `itemsHeading`, `poolHeading` | `DragDropMatch`: tiêu đề cột câu hỏi và cột thẻ, tối đa 128 ký tự (B1: "Young People" / tên loại thẻ). Bỏ trống: cột câu hỏi ghi "People" (A2), cột thẻ không có tiêu đề |
| `allowOptionReuse` | `DragDropMatch`: một thẻ có được kéo vào nhiều ô không (mặc định `false`) |
| `maxWordsPerAnswer` | `InlineText`: số từ tối đa mỗi ô ("Write ONE word") |
| `questions` | Các câu hỏi |

### Câu hỏi

| Trường | Ghi chú |
|---|---|
| `number` | Số câu chạy liên tục **cả bài**: 1, 2, 3… theo thứ tự xuất hiện |
| `promptHtml` | Câu hỏi. Với `DragDropMatch` đây là nhãn của dòng (tên người), hoặc đoạn mô tả người khi câu có `stimulusHtml` (ảnh) |
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
| `DragDropMatch` | Kéo thẻ vào ô | A2 Listening Part 5; B1 Reading Part 2 (người ↔ thẻ dài), Part 4 (câu vào ô trống của bài) | `optionPool` ≥ 2 thẻ; 1 đáp án nằm trong pool; mỗi câu có `promptHtml` (nhãn) — trừ khi `bodyHtml` có đủ `{{q:n}}` cho từng câu |
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

## Chuyển đề PDF thành gói đề (VE-56, B1: VE-69)

**Trên web (cách dùng thường ngày):** admin vào `/staff` → **Papers** → **Import from PDF**, chọn PDF đề, PDF đáp án (B1 Writing không có) và (Listening) mỗi part một file nghe: A2 năm file, B1 bốn. Server chạy chính `pdf2package.py` bên dưới (`POST /api/admin/papers/convert`) rồi import như một gói `.zip`. Có lỗi thì không tạo gì và hiện từng lỗi; không lỗi thì đề vào trạng thái Draft, kèm danh sách mục cần xem lại. Bấm tên đề để mở trang duyệt (từng câu, hình, đáp án, nghe thử từng part), rồi **Publish**.

Victory gửi đề dạng PDF theo bố cục của Cambridge. `pdf2package.py` chuyển **đề A2 Key (Reading & Writing, Listening) hoặc B1 Preliminary (Reading, Writing, Listening)** cùng file đáp án thành một thư mục gói đề nháp. Cấp độ và loại đề được nhận ra từ trang bìa; thiếu đáp án (trừ B1 Writing) hay thiếu thư mục nghe thì lệnh báo lỗi trong `--report`.

```bash
python -I setup/exam-package/pdf2package.py "<đề>.pdf" "<đáp án>.pdf" --title "A2 Key for Schools – Test 1 – Reading & Writing" [-o <thư mục>]
python -I setup/exam-package/pdf2package.py "<đề Listening>.pdf" "<đáp án>.pdf" --audio-dir "<thư mục audio>" --title "A2 Key for Schools – Test 1 – Listening"
python -I setup/exam-package/pdf2package.py "<đề B1 Writing>.pdf" --title "B1 Preliminary – Test 1 – Writing"
```

Thư mục ra mặc định cạnh file PDF: `package-reading-writing/`, `package-listening/` (A2), `package-reading/`, `package-writing/`, `package-listening/` (B1).

Listening cần **mỗi part một file âm thanh** trong `--audio-dir`, tên bắt đầu bằng số part: `P1_….mp3`, `part2.mp3`, `Part 3 - ….m4a`. Bộ chuyển chép chúng vào `audio/part1.mp3` … `audio/part5.mp3`. Bố cục Listening A2:

- Part 1: câu hỏi và ba tranh A/B/C. Tranh có thể là ảnh, hình vẽ nét (đồng hồ) hoặc chữ in to (giá tiền), nên mỗi phương án được **cắt từ vùng trang** giữa câu hỏi và chữ cái, chia cột ở giữa hai chữ cái. Giá tiền in to được giữ làm chữ thay thế cho hình.
- Part 2: tiêu đề, câu giới thiệu và bảng "Nhãn: giá trị". `(6) Mrs` thành `Mrs {{q:6}}`, `(8)` + `a.m.` thành `{{q:8}} a.m.`. Đáp án là giờ (`7.30`) thì tự nhận thêm `7:30`, và ghi vào mục cần xem lại.
- Part 3–4: câu chọn A/B/C. Câu giới thiệu ("You will hear …") của Part 3 vào phần hướng dẫn; Part 4 ghép tình huống và câu hỏi.
- Part 5: tên người bên trái, thẻ A–H bên phải (kéo thả). Dòng ví dụ `0` được ghi vào hướng dẫn.

Bố cục B1 Preliminary (VE-69; mỗi phần một section riêng, như template `B1_PRELIM_FS`):

- Reading Part 1: năm thông báo, mỗi thông báo cắt thành hình (chữ in trong khung giữ làm chữ thay thế) với A/B/C bên phải. Part 2: năm người (ảnh + mô tả) kéo vào tám đoạn giới thiệu có tiêu đề in đậm (`DragDropMatch` dạng người, `itemsHeading` từ "The … below", `poolHeading` là tiêu đề cột). Part 3: bài đọc có tiêu đề + câu A–D (cả kiểu 2×2). Part 4: bài đọc có ô trống `{{q:16}}`, kéo câu A–H vào (`DragDropMatch` dạng ô trống). Part 5: chọn từ A–D cho ô trống. Part 6: gõ một từ vào ô trống.
- Writing: Part 1 email (Essay, `minWords` từ "about 100 words", hình email cắt cả khung); Part 2 câu 2 và câu 3, mỗi câu một trang, thí sinh chọn một (VE-35). Không có đáp án, điểm do giám khảo chấm.
- Listening (4 part, 25 câu): Part 1 như A2 Part 1 (ba tranh); Part 2 mỗi câu có dòng ngữ cảnh "You will hear …" ngay trên câu hỏi; Part 3 tiêu đề + các câu có ô `(14)` → `{{q:14}}`, "Write one or two words" → `maxWordsPerAnswer` 2; Part 4 phỏng vấn A/B/C.
- Đáp án B1 có từ tuỳ chọn trong ngoặc: `(a/an/the) (fantastic) waterfall(s)` nhận mọi tổ hợp (`waterfall`, `the waterfalls`, …, tối đa 64 dạng); một từ tuỳ chọn đứng trước từ khác còn được viết liền (`(news) paper` → `newspaper`), trừ mạo từ, đại từ sở hữu và giới từ ngắn. Cột "ACCEPTABLE MIS-SPELLINGS" của Listening Part 3 cũng thành đáp án được chấp nhận.

Bộ chuyển làm các việc sau:

1. Đọc chữ kèm vị trí trên trang (PyMuPDF) và tách part theo dòng "Part n".
2. Bỏ số trang, "Turn over ►", "BLANK PAGE" và dòng "… on your answer sheet".
3. Lấy dòng "Questions a – b" và câu hướng dẫn.
4. Đọc từng part theo bố cục A2:
   - Part 1: thông báo bên trái, phương án bên phải.
   - Part 2: ba người có ảnh, các cột A/B/C nằm dưới tên.
   - Part 3: bài đọc có tiêu đề và tác giả.
   - Part 4: `(19) ......` → `{{q:19}}` kèm hàng ba từ để chọn.
   - Part 5: email From/To, có ví dụ `(0)`, giới hạn "Write one word".
   - Part 6–7: đề viết, gạch đầu dòng, ba tranh, số từ tối thiểu.
5. Reading & Writing: lưu hình gốc nhúng trong PDF vào `images/`. Không chụp vùng trang, nên chữ xung quanh không lẫn vào hình. Mỗi hình có `width` bằng 2 px cho mỗi pt in.
6. Ghép đáp án theo số câu. `your / the` thành hai đáp án được chấp nhận.

Thư mục ra có:

- `paper.json`;
- `images/`;
- `REVIEW.md`, gồm lỗi phải sửa (nếu có lỗi, lệnh trả mã 1), các mục cần xem lại, và bảng từng câu với phương án và đáp án để người duyệt đối chiếu.

Một mục cần xem lại hay gặp: ô trống đứng ngay trước chữ hoa, ví dụ `at the shop (21) ........ The owner`. Dấu chấm câu có thể đã bị dãy chấm "nuốt" mất, và chỉ người đọc mới phân biệt được với `(26) ..... BBC`.

Sau khi duyệt và sửa `paper.json`, đóng gói bằng `pack.py` rồi import. **Bản nháp là nội dung đề thật**, nên phải để trong `sources/` (đã git-ignore) và không bao giờ commit.

Kiểm thử bộ chuyển:

```bash
cd setup/exam-package && python -I -m pytest tests
```

Các test dùng dòng chữ tự soạn theo đúng toạ độ của bố cục Cambridge. Test chạy trên đề mẫu thật chỉ chạy khi có PDF trong `sources/`.

Kỳ thi demo (Development) dùng gói đề đã chuyển nếu `Demo:ReadingWritingFolder` trỏ tới thư mục có `paper.json`. Nếu không, nó dùng đề mẫu tự soạn.

## Đề mẫu tự soạn

`samples/a2-key-fs-reading-writing`, `samples/a2-key-fs-listening` (A2) và `samples/b1-pfs-reading` (B1 Preliminary for Schools Reading, 6 part, 32 câu, có hai dạng kéo thả của B1) do nhóm tự viết, dùng làm dữ liệu test; hình vẽ bằng `samples/make_images.py`.
