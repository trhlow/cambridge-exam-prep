# Victory 29/10: trộn N đề, lọc A2/B1, mỗi người chỉ thấy phần của mình, chấm ẩn danh

Yêu cầu của người dùng ngày 29/10/2026, đã duyệt thiết kế (brainstorming) cùng ngày. Bốn story mới, epic E16, đặt vào
Sprint 9 (tuần dự phòng) trong `sprints.py`; làm trước lịch như mọi sprint.

## Quyết định đã chốt

| Câu hỏi | Quyết định |
|---|---|
| Trộn bao nhiêu đề | N đề gốc (2–6) → K đề, người quản lý đề chọn K, 4 ≤ K ≤ min(3N, N^P) (P = số Part) |
| Cách trộn | K đề chia thành F = ⌈K/N⌉ "nhóm" cỡ đều nhau; trong một nhóm, đề thứ j lấy Part p từ đề gốc (b_f[p] + j) mod N, nên hai đề cùng nhóm không trùng Part nào. b_f là các vector khác hằng (mod dịch), chọn ngẫu nhiên theo seed; hết thì dùng nhóm hằng (đề gốc nguyên vẹn). 2 đề gốc, K = 4 cho đúng như cũ |
| Xếp chỗ | Dãy r dùng nhóm r mod F, máy ở cột c lấy đề thứ c mod cỡ nhóm: bên trái/phải không trùng Part nào, trước/sau/chéo là nhóm khác. Khi chỉ có một nhóm hoặc có nhóm cỡ 1 (N = 2, K = 5): đề (c + 2r) mod K, 8 máy xung quanh vẫn khác đề. Một dãy: 1, 2, …, K, 1, … |
| Import A2/B1 | Thanh lọc Cấp độ / Phần thi / Trạng thái / tên trên trang Papers; hộp "Import from PDF" chọn cấp độ + phần thi trước, chỉ hiện ô cần; server từ chối PDF có bìa khác lựa chọn; đề trắng B1 Reading / Writing / Listening |
| Quản lý đề | Chỉ thấy đề và đề trộn mình tạo; trộn từ đề của mình; admin thấy tất cả |
| Quản lý kỳ thi | Chỉ thấy phiên mình tạo; chọn đề cho phiên thấy *tên* mọi đề đã publish, không xem nội dung |
| Quản lý thí sinh | Chỉ phiên được người tạo gán (bảng mới `exam_candidate_managers`) |
| Giám thị | Cambridge (Exam Day Booklet 2026 tr. 4): ít nhất 1 giám thị / 25 thí sinh mỗi phòng; một phiên = một phòng; giám thị của phiên coi cả phòng; cảnh báo khi thí sinh > 25 × giám thị |
| Giám khảo | Thí sinh có bài Writing được chia đều, ngẫu nhiên cho các giám khảo của phiên; giám khảo chỉ thấy mã bài (không tên, số báo danh, chỗ ngồi), chỉ thấy phần Writing, không thấy Reading/Listening, không thấy bảng điểm |

## Story

- **VE-70 (E16, 5đ)** Trộn N đề gốc thành K đề (4 ≤ K ≤ 3N) và xếp chỗ theo nhóm.
  - Domain: `PaperMixPlan.Patterns(sources, parts, versions, seed)`, `PaperMixPlan.MaxVersions`, `PaperMixPlan.Families`;
    `SeatPattern.VersionFor(seat, seatsPerRow, versions, sources)`. Test trước (TDD): mọi cặp cùng nhóm khác hết
    Part; K đề khác nhau; 2 nguồn K = 4 giống mẫu cũ; 8 máy xung quanh khác đề cho mọi K, N, cỡ phòng nhỏ.
  - CSDL: `paper_mix_sources` (paper_mix_id, position, test_paper_id), `paper_mixes.version_count`; bỏ
    `source_a/b_paper_id`; CK `mix_version BETWEEN 1 AND 18`; migration chuyển mix cũ.
  - API: `NewPaperMix(SourcePaperIds, VersionCount, Title, Seed)`, view có `Sources` và `VersionCount`;
    `PaperAssignment` đọc K, N của mix theo phần thi.
  - Frontend: hộp "Mix papers" chọn nhiều đề + số đề; bảng Mixed papers; sơ đồ phòng.
- **VE-71 (E16, 3đ)** Lọc A2/B1 và import theo cấp độ / phần thi; đề trắng B1.
- **VE-72 (E16, 5đ)** Mỗi người chỉ thấy phần mình làm: đề và đề trộn theo người tạo, phiên theo người tạo, quản lý
  thí sinh theo phân công, cảnh báo tỉ lệ giám thị. `ExamAccess` + lọc ở `AdminPapersController`,
  `AdminPaperEditorController`, `AdminPaperMixesController`, `AssetsController` (tài sản của đề người khác → 404).
- **VE-73 (E16, 5đ)** Chia bài Writing ngẫu nhiên đều cho giám khảo, chấm ẩn danh, chỉ phần Writing.
  `candidates.examiner_user_id` + `candidates.marking_code`; `MarkingAllocator` chia khi cần (thí sinh chưa chia →
  giám khảo ít bài nhất, thứ tự ngẫu nhiên); bỏ giám khảo khỏi phiên → bài chưa Final của họ chia lại. Trang
  "Marking" mới cho giám khảo (danh sách mã bài); bảng điểm chỉ admin.

## Kiểm chứng (verification-before-completion)

Mỗi story: `dotnet test` (với `EXAMPREP_TEST_MYSQL`), `npm test`, `npm run lint`, `npm run build`; cuối cùng
e2e Playwright, `docker compose … up -d --build`, một vòng tay trên :8080. Commit ngay khi xong từng story, không push.
