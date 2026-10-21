# Giao diện bài thi Inspera: B1 Preliminary for Schools

Ghi lại từ bài thi mẫu chính thức trên `ceq.inspera.com` (Reading `assessmentRunId=160272732`, Writing `146730107`, Listening `146728853`; xem ngày 21/10/2026 bằng trình duyệt tự động, khung 1366×768). Chỉ ghi lại bố cục và cách thao tác, không chép nội dung đề. Khung màn hình, menu ☰ (Options), ghi chú/highlight, cờ, thanh part "Part n  x of y", nút ← → và trang nộp bài **giống hệt A2** (xem `inspera-ui-a2.md`); dưới đây chỉ ghi phần khác và bố cục từng part.

B1 có **3 phần thi** chạy lần lượt: Reading (45 phút, 32 câu), Writing (45 phút, 2 part), Listening (khoảng 30 phút, 25 câu) — khớp `section_templates` 3–5 trong `SeedData`.

## Reading (45 phút, 32 câu, 6 part)

| Part | Câu | Bố cục | Cách trả lời |
|---|---|---|---|
| 1 | 1–5 | Một cột, **mỗi câu một trang**: ảnh thông báo/tin nhắn của câu ở trên, 3 phương án bên dưới | Chọn 1 trong 3 (radio) — như A2 Part 1 |
| 2 | 6–10 | **Chia đôi, hai tiêu đề cột**: trái là 5 người, mỗi người có ảnh, đoạn mô tả và **một ô thả có viền đứt** đánh số câu; phải là **8 thẻ văn bản dài** (tên in đậm + đoạn mô tả), có 3 thẻ thừa | **Kéo thẻ** từ cột phải thả vào ô dưới người. Thẻ đã thả **biến khỏi cột phải** (để lại khoảng trống) và hiện nguyên văn trong ô; bộ đếm part tăng "1 of 5" |
| 3 | 11–15 | Chia đôi có thanh kéo ↔: trái là bài đọc dài (tiêu đề, dòng giới thiệu in nghiêng), phải là 5 câu hỏi | Chọn 1 trong **4** phương án (radio, không ghi chữ A–D) |
| 4 | 16–20 | Chia đôi: trái là bài viết có **5 ô trống đánh số nằm trong đoạn văn** (ô viền đứt, rộng như một câu); phải là **8 câu** dạng thẻ (3 câu thừa) | **Kéo câu** thả vào ô trống trong bài; câu đã dùng biến khỏi danh sách |
| 5 | 21–26 | Một cột: đoạn văn có tiêu đề, 6 ô trống đánh số trong câu | Bấm ô → chọn 1 trong **4** từ (như A2 Part 4, A2 chỉ có 3) |
| 6 | 27–32 | Một cột: đoạn văn có tiêu đề và tác giả, 6 ô trống trong câu | Gõ **một từ** vào ô (như A2 Part 5) |

## Writing (45 phút, 2 part)

| Part | Câu | Bố cục | Cách trả lời |
|---|---|---|---|
| 1 | 1 | Chia đôi có thanh kéo: trái là đề (một email kèm **ghi chú viết tay chỉ vào từng ý**, thường là một hình), phải là ô soạn thảo; dưới ô có bộ đếm "Words: 0" | Bắt buộc: viết email **khoảng 100 từ** (không có số từ tối thiểu, chỉ đếm từ) |
| 2 | 2–3 | **Mỗi câu một trang**, cùng bố cục chia đôi: trái là đề (câu 2 thường là bài báo theo thông báo, câu 3 là truyện bắt đầu bằng một câu cho sẵn), phải là ô soạn thảo | **Chọn một trong hai câu**: trên ô soạn thảo có khung "Answering this question? [Undecided ▾]" với 3 lựa chọn **Yes / No / Undecided** và dòng "0 of 1 questions selected"; thí sinh viết khoảng 100 từ cho câu mình chọn |

Ghi chú: hướng dẫn của Part 2 là "Answer **one** of these questions"; ô chọn chỉ để thí sinh và giám khảo biết câu nào được chấm — câu không chọn không tính điểm. Ở bài mẫu (không có phiên đăng nhập) việc chọn làm trang thoát ra, nên chỉ ghi được ba lựa chọn và dòng đếm.

## Listening (khoảng 30 phút, 25 câu, 4 part)

Lớp phủ "You will be listening to an audio clip…" và nút **Play**, sau đó "Audio is Playing" trên thanh trên cùng, audio chạy liền cả bài — giống A2.

| Part | Câu | Bố cục | Cách trả lời |
|---|---|---|---|
| 1 | 1–7 | **Mỗi câu một trang**: câu hỏi + 3 tranh, radio dưới mỗi tranh | Chọn 1 trong 3 tranh — như A2 Part 1 |
| 2 | 8–13 | **Mỗi câu một trang**: câu hỏi có dòng ngữ cảnh in đậm ("You will hear …") rồi câu hỏi, 3 phương án chữ | Chọn 1 trong 3 (radio) |
| 3 | 14–19 | Một cột: tiêu đề + 6 **câu** có ô trống đánh số (không phải bảng biểu mẫu như A2 Part 2) | Gõ **một hoặc hai từ**, một số, ngày hoặc giờ |
| 4 | 20–25 | Cả part một trang: 6 câu hỏi liên tiếp | Chọn 1 trong 3 (radio) |

## So với engine hiện tại (A2)

| Dạng B1 | Đã có | Cần bổ sung |
|---|---|---|
| Reading 1, Listening 1 (mỗi câu một trang) | `SingleChoice`/`ImageChoice`, phân trang khi mỗi câu có hình riêng | — |
| Reading 3, Listening 4 (chọn 1 trong 3–4) | `SingleChoice`, bố cục `Split` | — |
| Reading 5 (chọn từ 4 phương án trên ô trống) | `InlineChoice` | — |
| Reading 6, Listening 3 (gõ từ) | `InlineText`, `max_words_per_answer` (Listening 3: 2 từ) | — |
| Writing 1 | `Essay`, bố cục `Split` | — (khoảng 100 từ: đặt `min_words` = 0 hoặc để trống, chỉ đếm) |
| **Reading 2**: ghép người ↔ thẻ văn bản dài | `DragDropMatch` (thẻ chữ ngắn, ô theo hàng tên) | Mỗi câu có ảnh + mô tả (đã có `questions.stimulus_html`), **thẻ có tiêu đề in đậm + đoạn văn**, bố cục chia đôi, thẻ đã dùng biến khỏi danh sách — **VE-34** |
| **Reading 4**: kéo câu vào ô trống trong bài | Ô `{{q:n}}` trong `body_html` (chỉ cho `InlineChoice`/`InlineText`) | Kiểu kéo thả vào **ô trống trong đoạn văn** dùng chung bộ thẻ của nhóm — **VE-34** |
| **Listening 2**: mỗi câu một trang dù không có hình | Phân trang chỉ khi mỗi câu có hình/ảnh thông báo | Quy tắc phân trang theo dữ liệu (cờ trên part) — **VE-37** |
| **Writing 2**: chọn một trong hai câu | — | Ô "Answering this question? Yes/No/Undecided", chỉ chấm câu được chọn — **VE-35** |
