#! LỜI CAM ĐOAN
Tôi xin cam đoan báo cáo đồ án chuyên ngành “Xây dựng website hỗ trợ thi chứng chỉ tiếng Anh Cambridge” là công trình do tôi thực hiện dưới sự hướng dẫn của ThS. Nhan Minh Phúc. Mã nguồn, sơ đồ thiết kế và số liệu kiểm thử trình bày trong báo cáo do tôi tự xây dựng và đo đạc. Các nội dung tham khảo từ tài liệu, trang web của tổ chức và tác giả khác đều được trích dẫn và ghi trong danh mục tài liệu tham khảo.
Hệ thống không lưu nội dung đề thi của Cambridge hay của Trung tâm Anh ngữ Victory trong kho mã nguồn; các đề mẫu dùng để kiểm thử do tôi tự soạn.
Nếu phát hiện có sự gian lận, tôi xin chịu hoàn toàn trách nhiệm trước Khoa và Nhà trường.
@sign Vĩnh Long, ngày … tháng … năm 2026|Sinh viên thực hiện|(Ký và ghi rõ họ tên)||Trần Hoàng Long

#! LỜI CẢM ƠN
Tôi xin chân thành cảm ơn ThS. Nhan Minh Phúc, giảng viên hướng dẫn, đã định hướng đề tài, góp ý trong các buổi báo cáo tiến độ và giúp tôi điều chỉnh phạm vi cho phù hợp với nhu cầu thực tế.
Tôi xin cảm ơn quý thầy cô Khoa Công nghệ Thông tin, Trường Kỹ thuật và Công nghệ đã truyền đạt kiến thức nền tảng trong suốt quá trình học tập, giúp tôi có thể thực hiện đồ án này.
Tôi cũng xin cảm ơn Trung tâm Anh ngữ Victory đã đặt ra bài toán thực tế, cung cấp yêu cầu nghiệp vụ và góp ý cho từng phiên bản của hệ thống.
Cuối cùng, tôi xin cảm ơn gia đình và bạn bè đã luôn động viên, hỗ trợ tôi trong thời gian thực hiện đồ án.
Dù đã cố gắng, báo cáo khó tránh khỏi thiếu sót. Tôi rất mong nhận được ý kiến đóng góp của quý thầy cô để hoàn thiện hơn.
@sign Vĩnh Long, ngày … tháng … năm 2026|Sinh viên thực hiện|(Ký và ghi rõ họ tên)||Trần Hoàng Long

@nhanxet NHẬN XÉT CỦA GIẢNG VIÊN HƯỚNG DẪN|Giảng viên hướng dẫn
@nhanxet NHẬN XÉT CỦA GIẢNG VIÊN CHẤM ĐỒ ÁN|Giảng viên chấm

@toc MỤC LỤC

#! DANH MỤC CHỮ VIẾT TẮT
@abbrev
API|Application Programming Interface – giao diện lập trình ứng dụng
CBT|Computer-Based Testing – thi trên máy tính
CEFR|Common European Framework of Reference for Languages – Khung tham chiếu ngôn ngữ chung châu Âu
CSDL|Cơ sở dữ liệu
CSP|Content Security Policy – chính sách bảo mật nội dung
E2E|End-to-End – kiểm thử đầu-cuối
EF Core|Entity Framework Core
HTML|HyperText Markup Language
HTTP|Hypertext Transfer Protocol
ITC|International Test Commission – Ủy ban Khảo thí Quốc tế
JSON|JavaScript Object Notation
LAN|Local Area Network – mạng cục bộ
ORM|Object-Relational Mapping – ánh xạ đối tượng – quan hệ
OWASP|Open Worldwide Application Security Project
PIN|Personal Identification Number – mã số dùng để mở bài thi
REST|Representational State Transfer
SEB|Safe Exam Browser
SPA|Single-Page Application – ứng dụng trang đơn
UML|Unified Modeling Language – ngôn ngữ mô hình hóa thống nhất
WCAG|Web Content Accessibility Guidelines
XSS|Cross-Site Scripting
@end

@lot DANH MỤC BẢNG BIỂU
@lof DANH MỤC HÌNH ẢNH

#! TÓM TẮT ĐỒ ÁN
Các kỳ thi Cambridge English Qualifications từ A2 Key đến C2 Proficiency đã có hình thức thi trên máy tính: thí sinh làm các phần Reading, Writing và Listening trên máy, còn giám thị theo dõi cả phòng trên một cổng quản lý. Để thí sinh quen với giao diện thi máy và để giám thị tập các thao tác xử lý sự cố, Trung tâm Anh ngữ Victory cần một hệ thống tổ chức thi thử ngay tại phòng máy của trung tâm, bằng đề do trung tâm cung cấp.
Đồ án xây dựng một website đáp ứng nhu cầu đó cho hai kỳ thi A2 Key for Schools và B1 Preliminary for Schools. Hướng tiếp cận là tái hiện bố cục, định dạng câu hỏi và quy trình giám sát của nền tảng thi máy mà Cambridge đang dùng, dựa trên tài liệu ngày thi, tài liệu tập huấn giám thị và bài thi mẫu chính thức; đồng thời đối chiếu thiết kế với bộ hướng dẫn về thi trên máy tính của Ủy ban Khảo thí Quốc tế (ITC).
Hệ thống gồm máy chủ ASP.NET Core trên .NET 10, cơ sở dữ liệu MySQL truy cập qua Entity Framework Core, giao diện React viết bằng TypeScript và kênh thời gian thực SignalR. Đồng hồ làm bài do máy chủ nắm giữ nên thí sinh mất mạng hay tắt máy vẫn được nộp bài đúng giờ. Giám thị tạm dừng, cộng giờ, sửa giờ kết thúc, tua lại phần nghe và gửi thông báo; các lệnh đến màn hình thí sinh gần như tức thì. Hệ thống còn có chấm tự động phần Reading và Listening, giám khảo chấm Writing, nhập đề từ tệp PDF, soạn đề trên web, trộn hai đề gốc thành bốn đề theo vị trí máy, phân quyền năm vai trò với cơ chế hai quản trị viên duyệt chéo, và kiểm tra cấu hình Safe Exam Browser.
Kết quả: hệ thống chạy được trọn một buổi thi A2 và B1; có 400 kiểm thử tự động phía máy chủ, 109 kiểm thử giao diện, 69 kiểm thử công cụ chuyển đề và 6 kịch bản kiểm thử đầu-cuối, tất cả đều đạt. Kiểm thử tải với 200 thí sinh đồng thời không phát sinh lỗi máy chủ. Buổi thi thử 16 tình huống chạy tự động trên trình duyệt đạt 16/16 tình huống sau khi sửa bốn lỗi phát hiện được.

#! MỞ ĐẦU
## Lý do chọn đề tài
Cambridge English Qualifications là hệ thống chứng chỉ tiếng Anh theo từng bậc của Khung tham chiếu ngôn ngữ chung châu Âu (CEFR) [@a2hb]. Bên cạnh hình thức thi trên giấy, Cambridge đã đưa các kỳ thi từ A2 Key đến C2 Proficiency lên máy tính: phần Listening, Reading và Writing làm trên máy, phần Speaking vẫn thi trực tiếp với giám khảo [@digital]. Hai kỳ thi dành cho học sinh là A2 Key for Schools và B1 Preliminary for Schools cũng có thể thi trên giấy hoặc trên máy [@a2hb], [@b1hb].
Thi trên máy đặt ra yêu cầu làm quen với thiết bị và phần mềm. Bộ hướng dẫn về thi trên máy tính của Ủy ban Khảo thí Quốc tế (ITC) khuyến nghị đơn vị tổ chức tạo đủ cơ hội để thí sinh làm quen với phần mềm và thiết bị thi trước khi làm bài [@itc, tr. 20]. Với giám thị, khâu vận hành cũng không đơn giản: khi có sự cố, giám thị phải tạm dừng đúng thí sinh, cộng giờ bù, sửa giờ kết thúc; riêng phần Listening, việc tua lại âm thanh và cộng giờ là hai thao tác riêng biệt [@invig, tr. 20]. Buổi tập huấn giám thị chính thức chỉ cho mỗi phần thi 20 phút và phải dùng thiết bị riêng, không được cài lên máy dùng cho kỳ thi thật [@invig, tr. 3–4].
Trung tâm Anh ngữ Victory luyện thi Cambridge cho học sinh và tổ chức thi tại phòng máy của trung tâm. Trung tâm cần một công cụ để học viên thi thử với giao diện giống kỳ thi thật, để giám thị tập thao tác và để trung tâm tự tổ chức kỳ thi thử bằng đề của mình. Hệ thống nhập đề cũ của trung tâm không còn sử dụng được. Vì vậy tôi chọn đề tài “Xây dựng website hỗ trợ thi chứng chỉ tiếng Anh Cambridge” để giải quyết bài toán này.
## Mục đích nghiên cứu
Mục đích chung là xây dựng một website chạy được trong phòng máy của trung tâm, tổ chức trọn một buổi thi trên máy từ khâu chuẩn bị đề, tạo kỳ thi, cấp tài khoản cho thí sinh, làm bài, giám sát, xử lý sự cố đến chấm điểm và xuất kết quả.
Các mục đích cụ thể:
- Tìm hiểu quy trình tổ chức và giám sát một buổi thi Cambridge trên máy, viết thành bản đặc tả yêu cầu.
- Thiết kế cơ sở dữ liệu và kiến trúc hệ thống cho nhiều kỳ thi dùng chung một bộ máy, trong đó cấu trúc phần thi được mô tả bằng dữ liệu.
- Xây dựng màn hình thí sinh tái hiện bố cục và định dạng câu hỏi của bài thi máy A2 Key for Schools và B1 Preliminary for Schools.
- Xây dựng cổng nhân viên cho quản trị viên, giám thị và giám khảo, trong đó giám thị theo dõi phòng thi theo thời gian thực.
- Kiểm thử chức năng, kiểm thử tải và tổ chức một buổi thi thử để đánh giá hệ thống.
## Đối tượng và phạm vi nghiên cứu
Đối tượng nghiên cứu gồm quy trình tổ chức, giám sát và làm bài của kỳ thi Cambridge English Qualifications trên máy tính; các nguyên tắc về thi trên máy tính; và các công nghệ xây dựng ứng dụng web thời gian thực (ASP.NET Core, SignalR, Entity Framework Core, MySQL, React, Safe Exam Browser).
Phạm vi nghiên cứu:
- Hai kỳ thi A2 Key for Schools và B1 Preliminary for Schools, gồm các phần thi làm trên máy (Reading, Writing, Listening). Phần Speaking thi trực tiếp nên nằm ngoài phạm vi.
- Hệ thống chạy trên một máy chủ trong mạng cục bộ của trung tâm, máy thí sinh chạy Windows.
- Nội dung đề thi do trung tâm cung cấp và chỉ lưu trên máy chủ của trung tâm. Hệ thống không kết nối với hệ thống của Cambridge.
- Các kỳ thi B2 First, C1 Advanced, C2 Proficiency và IELTS nằm ngoài phạm vi đồ án.
So với đề cương chi tiết, phạm vi kỳ thi đã được điều chỉnh từ các cấp B2–C2 sang A2 và B1 theo nhu cầu của Trung tâm Anh ngữ Victory, đơn vị sử dụng hệ thống.
## Phương pháp nghiên cứu
- Nghiên cứu tài liệu: tài liệu ngày thi và tài liệu tập huấn giám thị của Cambridge, cẩm nang giáo viên của hai kỳ thi, bộ hướng dẫn của ITC và tài liệu chính thức của từng công nghệ.
- Phân tích và thiết kế bằng UML [@uml]: sơ đồ use case, sơ đồ thực thể – liên kết, sơ đồ trạng thái và sơ đồ tuần tự.
- Phát triển lặp theo Scrum [@scrum]: mỗi sprint kéo dài một tuần, kết thúc bằng một phần chạy được và báo cáo tiến độ.
- Thực nghiệm: kiểm thử tự động ở nhiều mức, kiểm thử tải và một buổi thi thử theo kịch bản 16 tình huống.
## Bố cục báo cáo
Ngoài phần mở đầu, báo cáo gồm năm chương. Chương 1 giới thiệu tổng quan bài toán, kỳ thi Cambridge trên máy và các yêu cầu đặt ra. Chương 2 trình bày cơ sở lý thuyết về thi trên máy tính và các công nghệ sử dụng. Chương 3 mô tả quá trình phân tích, thiết kế và hiện thực hệ thống. Chương 4 trình bày kết quả và đánh giá. Chương 5 nêu kết luận và hướng phát triển.

# TỔNG QUAN
## Giới thiệu bài toán
### Bối cảnh
Trung tâm Anh ngữ Victory (sau đây gọi là trung tâm) luyện thi các chứng chỉ Cambridge cho học sinh và có phòng máy để tổ chức thi. Khi các kỳ thi Cambridge chuyển dần sang hình thức thi trên máy [@digital], trung tâm có ba nhu cầu. Thứ nhất, học viên cần làm quen trước với màn hình thi máy: cách chuyển câu, kéo thả đáp án, ghi chú, đổi cỡ chữ, nghe một lần liền mạch. Thứ hai, giám thị cần được tập các thao tác xử lý sự cố trên một hệ thống có hành vi giống hệ thống thật. Thứ ba, trung tâm muốn tự tổ chức các kỳ thi thử bằng đề của mình, chấm điểm và xuất kết quả mà không phụ thuộc vào hệ thống bên ngoài.
### Phát biểu bài toán
Bài toán đặt ra là xây dựng một website chạy trên máy chủ đặt tại trung tâm, được truy cập từ các máy trong phòng thi qua mạng cục bộ, với các yêu cầu chính:
- Quản lý đề thi: nhập đề từ tệp PDF và tệp âm thanh do trung tâm cung cấp, soạn và sửa đề trên web, xuất bản đề sau khi kiểm tra.
- Tổ chức kỳ thi: tạo kỳ thi gồm các phần thi theo đúng cấu trúc của từng loại chứng chỉ, nhập danh sách thí sinh, in phiếu đăng nhập, phân công giám thị và giám khảo.
- Làm bài: thí sinh đăng nhập bằng phiếu và mã PIN, làm lần lượt từng phần thi trên giao diện giống bài thi máy của Cambridge; đồng hồ do máy chủ tính; hết giờ thì bài được nộp tự động.
- Giám sát: giám thị theo dõi cả phòng theo thời gian thực và thực hiện các thao tác tạm dừng, cộng giờ, sửa giờ kết thúc, tua lại phần nghe, gửi thông báo.
- Chấm điểm: chấm tự động phần có đáp án, giám khảo chấm phần Writing, xuất bảng điểm.
## Kỳ thi Cambridge English Qualifications
### Khung tham chiếu CEFR
Khung tham chiếu ngôn ngữ chung châu Âu (CEFR) do Hội đồng châu Âu công bố năm 2001 mô tả năng lực ngôn ngữ theo sáu bậc A1, A2, B1, B2, C1, C2, chia thành ba nhóm: người dùng cơ bản (A), người dùng độc lập (B) và người dùng thành thạo (C) [@cefr2001]. Năm 2020, Hội đồng châu Âu phát hành tập bổ sung (Companion Volume) cập nhật các bộ mô tả năng lực, bổ sung bậc Pre-A1 và các thang đo mới như hòa giải (mediation) và tương tác trực tuyến [@cefr2020].
Mỗi chứng chỉ Cambridge English Qualifications nhắm vào một bậc CEFR [@a2hb, tr. 2]. Kết quả được báo bằng điểm trên thang Cambridge English Scale cho từng kỹ năng và điểm tổng là trung bình của bốn kỹ năng; từ điểm tổng suy ra xếp loại và bậc CEFR [@a2hb, tr. 3].
### A2 Key for Schools
A2 Key for Schools là chứng chỉ ở bậc A2, dành cho học sinh, chứng nhận thí sinh có thể hiểu và dùng các cụm từ cơ bản, hiểu văn bản viết đơn giản và giao tiếp ở mức cơ bản; kỳ thi có thể làm trên giấy hoặc trên máy [@a2hb, tr. 3]. Cấu trúc kỳ thi được trình bày trong Bảng 1.1.
|table| Cấu trúc kỳ thi A2 Key for Schools (nguồn: [@a2hb, tr. 5]) | 4.5,3.5,3.5,4.5
Bài thi|Thời gian|Số phần|Số câu hỏi
Reading and Writing|60 phút|7|32
Listening|khoảng 30 phút|5|25
Speaking|8–10 phút|2|–
|end|
Bài Reading and Writing gồm năm phần đọc (chọn một trong ba phương án, nối, điền một từ vào chỗ trống) và hai phần viết: viết email hoặc ghi chú ít nhất 25 từ, và viết câu chuyện ít nhất 35 từ theo ba bức tranh [@a2hb, tr. 7]. Bài Listening gồm năm phần: chọn tranh đúng, điền thông tin vào ghi chú, trả lời câu hỏi trắc nghiệm và nối thông tin [@a2hb, tr. 24]. Thí sinh đạt điểm 140–150 nhận chứng chỉ ghi năng lực bậc B1; 120–139 nhận chứng chỉ bậc A2; 100–119 nhận chứng chỉ bậc A1 [@a2hb, tr. 5].
### B1 Preliminary for Schools
B1 Preliminary for Schools là chứng chỉ ở bậc B1, tiếp nối A2 Key, chứng nhận thí sinh đã nắm vững kiến thức cơ bản và có kỹ năng tiếng Anh thực tế cho sinh hoạt hằng ngày; kỳ thi cũng có thể làm trên giấy hoặc trên máy [@b1hb]. Khác với A2, Reading và Writing là hai bài thi riêng (Bảng 1.2).
|table| Cấu trúc kỳ thi B1 Preliminary for Schools (nguồn: [@b1hb]) | 4.5,3.5,3.5,4.5
Bài thi|Thời gian|Số phần|Số câu hỏi
Reading|45 phút|6|32
Writing|45 phút|2|–
Listening|khoảng 30 phút|4|25
Speaking|12–17 phút|4|–
|end|
Bài Reading có các dạng câu hỏi mới so với A2: nối mô tả của năm người với tám đoạn văn ngắn, và điền năm câu bị lược bỏ vào một bài đọc dài [@b1hb]. Bài Writing gồm hai phần, mỗi phần khoảng 100 từ: phần 1 trả lời một email, phần 2 thí sinh chọn viết một bài báo hoặc một câu chuyện [@b1hb]. Bài Listening gồm bốn phần với 25 câu hỏi, trong đó phần 3 là điền vào chỗ trống [@b1hb]. Những khác biệt này ảnh hưởng trực tiếp đến thiết kế: hệ thống phải hỗ trợ nhiều kiểu tương tác câu hỏi và cho phép thí sinh chọn một trong hai câu ở phần Writing.
### Hình thức thi trên máy
Theo Cambridge, bài thi trên máy kiểm tra cùng kiến thức và kỹ năng như bài thi trên giấy [@digital]. Ở phần Listening thí sinh dùng tai nghe có chỉnh âm lượng; ở phần Writing có bộ đếm số từ tự động; ở phần Reading thí sinh có thể ghi chú, tô sáng văn bản và xem lại câu trả lời; nội dung bài thi được xáo trộn để tăng tính bảo mật [@digital].
Bài thi máy của Cambridge chạy trên nền tảng Inspera. Trước giờ thi, nhân viên trung tâm mở trình duyệt khóa Inspera Integrity Browser trên mọi máy thí sinh và đưa về trang đăng nhập; giám thị theo dõi thí sinh qua Inspera Admin Portal [@booklet, tr. 3, 5]. Máy thi phải đáp ứng cấu hình tối thiểu do Cambridge công bố, trong đó có yêu cầu cài bản cập nhật hệ điều hành và tắt cập nhật tự động trong ngày thi [@inspera_specs].
## Quy trình tổ chức một buổi thi trên máy
### Trước và khi bắt đầu giờ thi
Tài liệu ngày thi của Cambridge mô tả các bước thí sinh vào bài: kiểm tra tên đăng nhập (bắt đầu bằng các chữ cái của họ thí sinh), đăng nhập bằng tên đăng nhập và mật khẩu, nhập mã PIN do giám thị cung cấp rồi chọn “Unlock and Select” để mở bài, sau đó kiểm tra lại họ tên hiển thị trên màn hình [@booklet, tr. 11]. Giám thị theo dõi cổng quản lý và tạm dừng những thí sinh bắt đầu làm bài sớm hơn hiệu lệnh [@booklet, tr. 12]. Quy trình được tóm tắt trong Hình 1.1.
!fig ../../diagrams/report/exam-day.drawio.png | Quy trình một buổi thi trên máy (tổng hợp từ [@booklet] và [@invig]) | 16
### Các thao tác của giám thị trong giờ thi
Tài liệu tập huấn giám thị mô tả các thao tác trên cổng quản lý [@invig]:
- Tạm dừng và cho làm tiếp: giám thị chọn một hoặc nhiều thí sinh, chọn Pause và ghi lý do; màn hình thí sinh hiện thông báo bài thi đang tạm dừng [@invig, tr. 13].
- Theo dõi thời gian: giám thị thêm các cột thời gian còn lại, thời gian bù sự cố, thời gian cộng thêm và thời điểm bắt đầu vào màn hình theo dõi [@invig, tr. 11].
- Sửa giờ kết thúc (Edit End Time), trong đó End now kết thúc bài làm của thí sinh ngay lập tức [@invig, tr. 18].
- Tua lại phần nghe: khi có sự cố trong phần Listening, giám thị phải tua âm thanh lại một số phút và cộng đúng số phút đó vào thời gian bù sự cố, đây là hai thao tác riêng biệt [@invig, tr. 20].
- Xử lý thí sinh mất kết nối: khi mất điện, thí sinh chuyển sang trạng thái ngoại tuyến và giám thị không tạm dừng được; thí sinh có thể đăng nhập lại trên máy khác hoặc trên chính máy đó khi có điện, sau đó giám thị cộng thời gian bù cho số phút bị gián đoạn [@invig, tr. 15].
Ngoài ra, thí sinh không được cộng thêm giờ cho thời gian đi vệ sinh [@booklet, tr. 13].
### Những khó khăn trong vận hành
Từ quy trình trên có thể thấy các điểm dễ sai sót khi vận hành. Lệnh kết thúc bài không thể hoàn tác, nên bấm nhầm sẽ ảnh hưởng trực tiếp đến thí sinh. Thí sinh mất kết nối vẫn bị tính giờ, nên giám thị phải theo dõi và cộng giờ bù chính xác. Phần Listening đòi hỏi hai thao tác phối hợp. Buổi tập huấn chính thức giúp giám thị làm quen, nhưng mỗi phần thi chỉ kéo dài 20 phút và phải chuẩn bị ít nhất hai thiết bị riêng cho vai thí sinh và vai giám thị [@invig, tr. 3]. Một hệ thống mô phỏng chạy ngay tại phòng máy của trung tâm sẽ giúp giám thị luyện tập nhiều lần với số thí sinh thật.
## Các giải pháp liên quan
### Nền tảng Inspera
Inspera là nền tảng mà Cambridge dùng để tổ chức các kỳ thi trên máy [@booklet]. Nhân viên trung tâm thi truy cập cổng quản lý Inspera qua tài khoản MyCambridge của Cambridge [@booklet, tr. 5], [@invig, tr. 3]; nền tảng phục vụ kỳ thi chính thức với đề của Cambridge, không phải công cụ để trung tâm tổ chức thi thử bằng đề tự soạn. Môi trường tập huấn giám thị là một bản riêng (demo tenant), cần đăng ký qua biểu mẫu và không được cài lên máy dùng cho kỳ thi thật [@invig, tr. 3–4].
### Safe Exam Browser kết hợp hệ thống quản lý học tập
Safe Exam Browser (SEB) là trình duyệt khóa mã nguồn mở do ETH Zurich phát triển, biến máy tính thành máy thi an toàn trong thời gian làm bài: chặn chuyển ứng dụng, chụp màn hình và các phím tắt hệ thống [@seb]. SEB làm việc với bất kỳ hệ thống quản lý học tập dạng web nào và có kết nối dựng sẵn với Moodle, ILIAS, OpenOlat [@seb]. Cách kết hợp này phù hợp với bài kiểm tra trắc nghiệm thông thường nhưng không tái hiện được giao diện và quy trình giám sát đặc thù của kỳ thi Cambridge (tạm dừng theo thí sinh, sửa giờ kết thúc, tua lại phần nghe).
### Nhận xét
Hai hướng trên đều không đáp ứng đầy đủ nhu cầu của trung tâm: một hướng giống kỳ thi thật nhưng không dùng được đề riêng, hướng còn lại dùng được đề riêng nhưng không giống kỳ thi thật. Đề tài chọn hướng tự xây dựng hệ thống, tái hiện giao diện và quy trình của kỳ thi máy Cambridge, và dùng SEB làm trình duyệt khóa phía thí sinh.
## Mục tiêu và yêu cầu của hệ thống
### Tác nhân
Theo yêu cầu của trung tâm, hệ thống có năm tác nhân người và một tác nhân hệ thống (Bảng 1.3).
|table| Các tác nhân của hệ thống | 4,12.5
Tác nhân|Vai trò
Thí sinh|Đăng nhập bằng phiếu và mã PIN, làm lần lượt các phần thi, ghi chú, tô sáng, đổi cỡ chữ và độ tương phản.
Giám thị|Theo dõi phòng thi theo thời gian thực; tạm dừng, cho làm tiếp, cộng giờ sự cố, sửa giờ kết thúc, tua lại phần nghe, gửi thông báo; xử lý cảnh báo.
Giám khảo|Xem bài làm của thí sinh được phân công, chấm phần Writing, xem bảng điểm.
Quản trị viên theo mảng|Quản lý đề thi, kỳ thi, thí sinh hoặc thông tin trung tâm trong phạm vi mảng được giao.
Admin|Toàn quyền; quản lý tài khoản nhân viên; duyệt các yêu cầu xóa dữ liệu và thay đổi tài khoản.
Hệ thống|Tự nộp bài khi hết giờ, chấm tự động, ghi nhận mất kết nối và các cảnh báo.
|end|
### Yêu cầu phi chức năng
Ngoài các chức năng đã nêu ở phần phát biểu bài toán, hệ thống cần thỏa các yêu cầu phi chức năng sau, phần lớn xuất phát từ bộ hướng dẫn của ITC (Chương 2):
- Đồng hồ làm bài do máy chủ tính và không bị ảnh hưởng bởi đồng hồ máy thí sinh; mất kết nối không làm mất câu trả lời đã chọn [@itc, tr. 16].
- Thao tác trên màn hình thí sinh có tác dụng ngay; theo ITC, độ trễ trên màn hình không nên quá 2 giây [@itc, tr. 15].
- Lệnh của giám thị đến màn hình thí sinh trong vài giây; trang giám thị phát hiện thí sinh mất kết nối.
- Phục vụ được khoảng 100 thí sinh làm bài đồng thời trên một máy chủ.
- Bảo vệ đề thi và dữ liệu thí sinh: phân quyền theo vai trò, mọi thao tác xóa và thay đổi tài khoản cần một quản trị viên khác duyệt.
## Phương pháp và kế hoạch thực hiện
Đồ án được thực hiện theo khung Scrum [@scrum], điều chỉnh cho một người thực hiện: danh sách công việc (product backlog) gồm các epic và user story được quản lý trên Jira [@jira], mỗi sprint kéo dài một tuần, cuối mỗi sprint có báo cáo tiến độ gửi giảng viên hướng dẫn. Mã nguồn được quản lý bằng Git [@git] trên GitHub, commit ít nhất mỗi tuần theo quy định của Khoa. Kế hoạch các sprint được trình bày trong Bảng 1.4.
|table| Kế hoạch các sprint | 2.2,3.6,10.7
Sprint|Thời gian|Mục tiêu
0|10/10–11/10|Khởi động: cơ sở dữ liệu, khảo sát giao diện
1|12/10–18/10|Màn hình thí sinh A2 (demo 16/10); cổng nhân viên; chấm Writing; nhập đề từ PDF
2|19/10–25/10|Phân quyền nhiều tác nhân, duyệt chéo hai admin; đặc tả yêu cầu, sơ đồ, đặc tả API
3|26/10–01/11|SignalR; sửa đề trên web; Safe Exam Browser và cảnh báo; trộn đề A2
4|02/11–08/11|Kiểm thử đầu-cuối A2; bắt đầu B1 Preliminary for Schools
5|09/11–15/11|B1: Writing và Listening
6|16/11–22/11|Kỳ thi B1 chạy đầu-cuối; sửa lỗi tồn đọng
7|23/11–29/11|Kiểm thử tải và buổi thi thử
8|30/11–06/12|Viết báo cáo: Mở đầu, Chương 1–3
9|07/12–13/12|Dự phòng
10|14/12–20/12|Viết báo cáo: kết quả và kết luận
11|21/12–27/12|Hoàn chỉnh và nộp báo cáo
|end|
Chương này đã giới thiệu bài toán của trung tâm, cấu trúc hai kỳ thi A2 Key for Schools và B1 Preliminary for Schools, quy trình tổ chức một buổi thi trên máy và các yêu cầu đặt ra cho hệ thống. Chương 2 trình bày cơ sở lý thuyết và công nghệ để đáp ứng các yêu cầu này.

# CƠ SỞ LÝ THUYẾT
## Thi trên máy tính
### Khái niệm và các chế độ tổ chức thi
Thi trên máy tính (computer-based testing, CBT) là hình thức thí sinh làm bài và nộp bài trên máy tính thay vì trên giấy. Ủy ban Khảo thí Quốc tế (ITC) phân biệt bốn chế độ tổ chức thi theo mức độ giám sát, trình bày trong Bảng 2.1 [@itc, tr. 10].
|table| Các chế độ tổ chức thi theo ITC (nguồn: [@itc, tr. 10]) | 3.5,13
Chế độ|Mô tả
Mở (open)|Không có người giám sát, không xác thực được danh tính thí sinh, ví dụ bài kiểm tra trên Internet không cần đăng ký.
Kiểm soát (controlled)|Không có người giám sát trực tiếp nhưng chỉ thí sinh đã biết trước mới được làm bài, thường qua tên đăng nhập và mật khẩu.
Có giám sát (supervised)|Có người giám sát trực tiếp điều kiện làm bài và xác thực được danh tính thí sinh.
Quản lý (managed)|Mức giám sát và kiểm soát cao, thường thực hiện tại trung tâm khảo thí chuyên dụng, kiểm soát ra vào, nhân sự và thiết bị.
|end|
Buổi thi tại phòng máy của trung tâm, có giám thị trong phòng và máy thi do trung tâm quản lý, thuộc chế độ có giám sát đến quản lý. Đây là căn cứ để hệ thống yêu cầu đăng nhập bằng tài khoản cấp riêng cho từng thí sinh, mã PIN do giám thị đọc và trình duyệt khóa.
### Bộ hướng dẫn của ITC về thi trên máy tính
Bộ hướng dẫn của ITC về thi trên máy tính và qua Internet được Hội đồng ITC thông qua tháng 7 năm 2005 và đăng trên tạp chí International Journal of Testing năm 2006 [@itc]. Bộ hướng dẫn chia thành bốn nhóm: chú ý các vấn đề công nghệ; bảo đảm chất lượng; kiểm soát việc tổ chức thi; bảo mật và bảo vệ dữ liệu riêng tư [@itc, tr. 5]. Mỗi hướng dẫn được viết riêng cho ba đối tượng: đơn vị phát triển bài thi, đơn vị phát hành và đơn vị sử dụng. Bảng 2.2 tóm tắt các hướng dẫn có ảnh hưởng trực tiếp đến thiết kế của đồ án.
|table| Một số hướng dẫn của ITC và hệ quả đối với thiết kế | 8,8.5
Hướng dẫn của ITC|Hệ quả đối với thiết kế
Bài thi tính giờ phải phản hồi ngay, độ trễ trên màn hình không quá 2 giây; thời gian chuyển câu và ghi nhận câu trả lời không tính vào thời gian làm bài [@itc, tr. 15–16].|Câu trả lời được lưu ngầm, không chặn thao tác của thí sinh; đồng hồ do máy chủ tính theo thời điểm bắt đầu.
Giảm thiểu ảnh hưởng của treo máy, mất kết nối; không để mất thông tin khi mất kết nối [@itc, tr. 16].|Câu trả lời được giữ lại ở trình duyệt và gửi lại khi có mạng; máy chủ tự nộp bài khi hết giờ kể cả khi thí sinh ngoại tuyến.
Thiết kế “an toàn khi lỗi”: ngăn thí sinh vô tình thoát bài, thông báo lỗi rõ ràng [@itc, tr. 15].|Thí sinh không có nút nộp bài; bài thi chạy trong trình duyệt khóa.
Hiển thị tên bài thi, số câu và hướng dẫn ở cùng vị trí trên mọi trang [@itc, tr. 17].|Bố cục màn hình thí sinh cố định theo bài thi máy của Cambridge.
Tạo đủ cơ hội để thí sinh làm quen với phần mềm và thiết bị thi [@itc, tr. 20].|Mục đích chính của hệ thống: cho thí sinh thi thử trên giao diện giống bài thi thật.
Thí sinh phải được xác thực ở mức phù hợp trước khi làm bài [@itc, tr. 32].|Tài khoản cấp riêng, mã PIN của buổi thi, khóa tài khoản khi nhập sai nhiều lần.
Bảo mật nội dung bài thi và dữ liệu thí sinh, sao lưu thường xuyên [@itc, tr. 33–36].|Phân quyền theo vai trò, đề chỉ lưu trên máy chủ của trung tâm, hướng dẫn sao lưu cơ sở dữ liệu.
|end|
### Tính tương đương giữa thi trên máy và thi trên giấy
Khi một bài thi có cả hai hình thức, câu hỏi đặt ra là điểm số của hai hình thức có so sánh được với nhau hay không. ITC yêu cầu đơn vị phát triển cung cấp bằng chứng về tính tương đương giữa bản thi trên máy và bản thi trên giấy [@itc, tr. 24]. Hiệu ứng của hình thức thi (mode effect) là chủ đề được nghiên cứu nhiều, chẳng hạn phân tích tổng hợp (meta-analysis) của Wang và cộng sự về hiệu ứng hình thức thi trong các bài kiểm tra đọc ở bậc phổ thông [@wang2008]. Với kỳ thi Cambridge, đơn vị khảo thí khẳng định hai hình thức kiểm tra cùng kiến thức và kỹ năng [@digital]. Đối với đồ án, hệ quả là màn hình thi thử phải giống bài thi máy thật càng nhiều càng tốt, để thí sinh không gặp khác biệt về thao tác khi bước vào kỳ thi chính thức.
## Kiến trúc ứng dụng web
### Mô hình khách – chủ và kiểu kiến trúc REST
Ứng dụng web hoạt động theo mô hình khách – chủ: trình duyệt (máy khách) gửi yêu cầu, máy chủ xử lý và trả về kết quả. Fielding đề xuất kiểu kiến trúc REST với các ràng buộc: khách – chủ, không lưu trạng thái phiên ở máy chủ giữa các yêu cầu, cho phép lưu đệm, giao diện thống nhất, hệ thống phân tầng và (tùy chọn) mã theo yêu cầu [@fielding]. Giao diện thống nhất xoay quanh khái niệm tài nguyên được định danh bằng URI và được thao tác qua các biểu diễn của nó.
HTTP là giao thức nền của web. Đặc tả RFC 9110 định nghĩa ngữ nghĩa các phương thức: GET và HEAD là an toàn (không làm thay đổi trạng thái), PUT và DELETE là lũy đẳng (gửi nhiều lần cho cùng kết quả như gửi một lần), còn POST không có hai tính chất này [@rfc9110]. Tính lũy đẳng có ý nghĩa thực tế với bài thi: khi mạng chập chờn, trình duyệt có thể gửi lại yêu cầu lưu câu trả lời mà không tạo ra dữ liệu trùng. Dữ liệu trao đổi giữa trình duyệt và máy chủ dùng định dạng JSON theo RFC 8259 [@rfc8259].
### Ứng dụng trang đơn với React và TypeScript
Ứng dụng trang đơn (SPA) tải giao diện một lần, sau đó chỉ trao đổi dữ liệu với máy chủ và cập nhật phần giao diện cần thay đổi. React là thư viện xây dựng giao diện theo thành phần: mỗi thành phần là một hàm JavaScript trả về phần đánh dấu giao diện, thường viết bằng cú pháp JSX; thành phần nhận dữ liệu từ thành phần cha qua props và giữ dữ liệu riêng của mình qua state, khi state thay đổi React vẽ lại thành phần đó [@react]. Phiên bản React 19 được phát hành ngày 05/12/2024 [@react19].
TypeScript bổ sung hệ thống kiểu tĩnh cho JavaScript, giúp phát hiện lỗi kiểu dữ liệu ngay khi biên dịch thay vì khi chạy [@ts]. Vite là công cụ phát triển gồm hai phần: máy chủ phát triển dựa trên mô-đun ES gốc của trình duyệt với khả năng thay mô-đun nóng (HMR), và lệnh build đóng gói mã nguồn thành các tệp tĩnh tối ưu cho môi trường chạy thật [@vite].
### Kiến trúc phân lớp và Clean Architecture
Chia ứng dụng thành các lớp theo trách nhiệm giúp quản lý độ phức tạp, tái sử dụng mã và giới hạn ảnh hưởng của thay đổi [@arch]. Kiến trúc N lớp truyền thống (giao diện – nghiệp vụ – truy cập dữ liệu) có nhược điểm là lớp nghiệp vụ phụ thuộc vào lớp truy cập dữ liệu, khiến việc kiểm thử nghiệp vụ cần đến cơ sở dữ liệu thật [@arch].
Clean Architecture đảo ngược chiều phụ thuộc này: nghiệp vụ và mô hình ứng dụng nằm ở trung tâm (Application Core), lớp hạ tầng cài đặt các interface do lõi định nghĩa, còn lớp giao diện chỉ làm việc với lõi qua các interface; các phụ thuộc lúc biên dịch luôn hướng vào lõi, phần cài đặt cụ thể được nối vào lúc chạy qua cơ chế tiêm phụ thuộc (dependency injection) [@arch]. Hình 2.1 minh họa kiến trúc này. Nhờ lõi không phụ thuộc hạ tầng, các quy tắc nghiệp vụ, chẳng hạn quy tắc tính giờ của một lượt làm bài, có thể được kiểm thử đơn vị độc lập.
!fig ../../diagrams/report/clean-architecture.drawio.png | Clean Architecture theo chiều ngang (phỏng theo [@arch]) | 16
## Giao tiếp thời gian thực
### Giao thức WebSocket
Theo mô hình HTTP truyền thống, chỉ trình duyệt mới chủ động gửi yêu cầu. Để máy chủ đẩy dữ liệu xuống trình duyệt, giao thức WebSocket (RFC 6455) cho phép thiết lập kênh truyền hai chiều trên một kết nối TCP: kết nối bắt đầu bằng một yêu cầu HTTP xin nâng cấp giao thức (Upgrade), sau khi bắt tay thành công hai bên gửi khung dữ liệu cho nhau bất kỳ lúc nào [@rfc6455].
### Thư viện SignalR
ASP.NET Core SignalR là thư viện mã nguồn mở giúp bổ sung chức năng thời gian thực, cho phép mã phía máy chủ đẩy nội dung xuống máy khách ngay lập tức; các ứng dụng phù hợp gồm bảng điều khiển, ứng dụng giám sát và ứng dụng cần thông báo [@signalr]. SignalR giao tiếp qua hub: máy khách gọi phương thức trên máy chủ và ngược lại, máy chủ gọi phương thức ở máy khách; thư viện có thể gửi thông điệp tới mọi máy khách, tới một máy khách hoặc tới một nhóm máy khách [@signalr].
SignalR tự chọn phương thức truyền tốt nhất mà cả máy chủ và máy khách hỗ trợ, theo thứ tự WebSockets, Server-Sent Events, Long Polling; dữ liệu được mã hóa bằng giao thức dạng văn bản dựa trên JSON (mặc định) hoặc giao thức nhị phân MessagePack [@signalr]. Mô hình hoạt động được minh họa trong Hình 2.2. Trang giám thị là trường hợp điển hình: khi trạng thái của thí sinh thay đổi, máy chủ báo cho đúng nhóm giám thị đang theo dõi phần thi đó.
!fig ../../diagrams/report/signalr.drawio.png | Máy chủ đẩy dữ liệu qua hub SignalR và thứ tự chọn phương thức truyền (tổng hợp từ [@signalr]) | 15
### Duy trì và phát hiện mất kết nối
Để phát hiện kết nối bị đứt, hai bên định kỳ gửi thông điệp giữ kết nối (keep-alive). Phía máy chủ, tham số KeepAliveInterval (mặc định 15 giây) quy định chu kỳ gửi thông điệp ping, còn ClientTimeoutInterval (mặc định 30 giây) là khoảng thời gian không nhận được thông điệp nào thì máy chủ coi máy khách đã ngắt; giá trị khuyến nghị của ClientTimeoutInterval là gấp đôi KeepAliveInterval. Phía máy khách JavaScript, chu kỳ giữ kết nối nên nhỏ hơn hoặc bằng một nửa thời gian chờ máy chủ [@signalr_config]. Quan hệ “thời gian chờ gấp đôi chu kỳ ping” này là căn cứ để cấu hình kênh thời gian thực của hệ thống sao cho kết nối không bị cắt khi phòng thi yên lặng.
## Nền tảng phía máy chủ
### .NET và ASP.NET Core
.NET 10 là phiên bản hỗ trợ dài hạn (LTS) được hỗ trợ trong ba năm, đi kèm ngôn ngữ C# 14 và Entity Framework Core 10 [@net10]. ASP.NET Core là framework mã nguồn mở, đa nền tảng, hiệu năng cao để xây dựng ứng dụng web trên .NET, có sẵn đường ống xử lý yêu cầu HTTP dạng mô-đun, máy chủ web Kestrel, cơ chế tiêm phụ thuộc, cấu hình theo môi trường, SignalR và các thành phần bảo mật cho xác thực, phân quyền và bảo vệ dữ liệu [@aspnet].
Một số công việc của hệ thống thi phải chạy định kỳ mà không phụ thuộc vào yêu cầu của người dùng, ví dụ nộp bài cho những thí sinh đã hết giờ nhưng đang ngoại tuyến. ASP.NET Core hỗ trợ việc này qua dịch vụ nền (hosted service), là lớp cài đặt interface IHostedService hoặc kế thừa lớp BackgroundService và chạy cùng vòng đời của ứng dụng [@hosted].
### Entity Framework Core
Entity Framework Core (EF Core) là công nghệ truy cập dữ liệu mã nguồn mở, đa nền tảng, đóng vai trò bộ ánh xạ đối tượng – quan hệ (ORM): lập trình viên làm việc với cơ sở dữ liệu qua các đối tượng .NET, truy vấn bằng LINQ và dùng migration để tạo và cập nhật lược đồ cơ sở dữ liệu theo mô hình [@efcore].
Khi nhiều yêu cầu cùng sửa một bản ghi, ví dụ giám thị tạm dừng đúng lúc máy chủ đang nộp bài vì hết giờ, cần cơ chế điều khiển tương tranh. EF Core dùng điều khiển tương tranh lạc quan: một thuộc tính được cấu hình làm concurrency token; khi cập nhật, EF Core thêm điều kiện so sánh token vào câu lệnh UPDATE; nếu bản ghi đã bị thay đổi sau khi đọc, câu lệnh không cập nhật dòng nào và EF Core ném ngoại lệ DbUpdateConcurrencyException để ứng dụng đọc lại dữ liệu và thử lại [@efconc]. Với cơ sở dữ liệu không tự sinh token, ứng dụng có thể tự quản lý token, ví dụ gán một GUID mới mỗi lần lưu [@efconc].
### Hệ quản trị cơ sở dữ liệu MySQL
MySQL là hệ quản trị cơ sở dữ liệu quan hệ mã nguồn mở. Trong MySQL 8.4, InnoDB là bộ máy lưu trữ mặc định; các thao tác dữ liệu tuân theo mô hình ACID với giao dịch có commit, rollback và khôi phục sau sự cố; khóa ở mức dòng giúp tăng khả năng phục vụ nhiều người dùng đồng thời; InnoDB hỗ trợ ràng buộc khóa ngoại để bảo toàn tính toàn vẹn dữ liệu [@mysql]. Các tính chất này phù hợp với dữ liệu bài thi, nơi một lần nộp bài phải ghi đồng thời trạng thái lượt làm bài và thời điểm nộp trong cùng một giao dịch.
## Bảo mật ứng dụng web
### Các rủi ro phổ biến theo OWASP
OWASP Top 10 là danh sách các nhóm rủi ro bảo mật quan trọng nhất của ứng dụng web, phiên bản mới nhất là OWASP Top 10:2025 (Bảng 2.3) [@owasp].
|table| Danh sách OWASP Top 10:2025 (nguồn: [@owasp]) | 2.5,14
Mã|Nhóm rủi ro
A01|Broken Access Control – kiểm soát truy cập sai
A02|Security Misconfiguration – cấu hình bảo mật sai
A03|Software Supply Chain Failures – lỗi chuỗi cung ứng phần mềm
A04|Cryptographic Failures – lỗi mật mã
A05|Injection – chèn mã độc
A06|Insecure Design – thiết kế không an toàn
A07|Authentication Failures – lỗi xác thực
A08|Software or Data Integrity Failures – lỗi toàn vẹn phần mềm hoặc dữ liệu
A09|Security Logging and Alerting Failures – thiếu ghi nhật ký và cảnh báo
A10|Mishandling of Exceptional Conditions – xử lý sai các tình huống ngoại lệ
|end|
Với hệ thống thi, kiểm soát truy cập (A01) và xác thực (A07) có ý nghĩa đặc biệt: thí sinh chỉ được xem bài của mình, giám thị chỉ thao tác trên kỳ thi được phân công, và đề thi chỉ được truy cập trong giờ thi.
### Xác thực bằng cookie và quản lý phiên
Cookie là cơ chế lưu trạng thái của HTTP: máy chủ gửi cookie qua tiêu đề Set-Cookie, trình duyệt gửi lại trong các yêu cầu sau; thuộc tính HttpOnly ngăn mã JavaScript đọc cookie [@rfc6265]. ASP.NET Core có sẵn cơ chế xác thực bằng cookie độc lập với ASP.NET Core Identity: sau khi đăng nhập, thông tin định danh của người dùng (claims) được lưu trong cookie xác thực và được kiểm tra ở mỗi yêu cầu [@cookie]. Việc mã hóa và chống giả mạo cookie do hệ thống bảo vệ dữ liệu (Data Protection) đảm nhận, với các yêu cầu về tính xác thực, toàn vẹn và bảo mật, kèm cơ chế quản lý và xoay vòng khóa [@dataprot].
Bảng hướng dẫn quản lý phiên của OWASP khuyến nghị: cookie phiên đặt thuộc tính Secure để chỉ gửi qua HTTPS, HttpOnly để chống đánh cắp qua XSS, SameSite=Strict (ưu tiên) hoặc Lax để giảm nguy cơ giả mạo yêu cầu; cấp lại định danh phiên sau mỗi lần thay đổi mức quyền, điển hình là sau khi đăng nhập; và mọi phiên phải có thời hạn [@session]. Trong hệ thống thi, nguyên tắc cấp lại phiên còn có ý nghĩa nghiệp vụ: khi thí sinh đăng nhập ở máy mới, phiên ở máy cũ phải mất hiệu lực.
### Chống tấn công chèn mã kịch bản
Tấn công XSS xảy ra khi dữ liệu không đáng tin được đưa vào trang web và trình duyệt thực thi như mã. Bảng hướng dẫn của OWASP khuyến nghị mã hóa đầu ra theo ngữ cảnh, và khi ứng dụng buộc phải hiển thị HTML do người dùng nhập thì phải làm sạch HTML bằng thư viện chuyên dụng [@xss]. Nội dung đề thi (đoạn văn, câu hỏi) là HTML, nên cần được làm sạch ở cả hai phía: HtmlSanitizer là thư viện .NET làm sạch HTML để tránh XSS [@htmlsan], còn DOMPurify làm sạch HTML ngay trong trình duyệt [@dompurify]. Lớp bảo vệ bổ sung là chính sách bảo mật nội dung (CSP), cho phép máy chủ khai báo những nguồn tài nguyên mà trang được phép tải và thực thi [@csp].
### Safe Exam Browser và Config Key
Safe Exam Browser chạy dưới dạng ứng dụng kiosk: trên Windows, SEB vô hiệu hóa thanh tác vụ, menu Start, tổ hợp Alt+Tab, phím Print Screen và các phím tắt như Alt+F4; SEB cũng có thể chặn các tiến trình không được phép và từ chối chạy trên máy ảo [@seb].
Để máy chủ biết bài thi có đang được mở bằng SEB với đúng cấu hình hay không, SEB cung cấp Config Key: một giá trị băm SHA-256 (64 ký tự hệ 16) tính từ toàn bộ thiết lập của tệp cấu hình SEB, không đổi khi nâng cấp phiên bản SEB. Khi bật tùy chọn gửi khóa, SEB thêm tiêu đề X-SafeExamBrowser-ConfigKeyHash vào mọi yêu cầu HTTP, có giá trị là băm SHA-256 của địa chỉ URL tuyệt đối nối với Config Key. Máy chủ tự tính lại giá trị này từ URL được yêu cầu và Config Key đã lưu; nếu không khớp, máy chủ từ chối hoặc dừng bài thi [@sebkey]. Vì URL thay đổi theo từng yêu cầu, giá trị gửi đi không làm lộ Config Key.
### Khả năng tiếp cận
Bộ tiêu chí WCAG 2.2 của W3C đưa ra các tiêu chí giúp nội dung web dễ tiếp cận hơn, chẳng hạn độ tương phản tối thiểu giữa chữ và nền, và khả năng phóng to chữ đến 200% mà không mất nội dung hay chức năng [@wcag]. Màn hình thi của Cambridge có tùy chọn đổi độ tương phản và cỡ chữ; hệ thống của đồ án tái hiện các tùy chọn này. Chức năng tô sáng văn bản trong bài đọc có thể cài đặt bằng CSS Custom Highlight API, cho phép tô kiểu cho các đoạn văn bản bất kỳ mà không phải chèn thêm phần tử vào cây DOM [@highlight].
## Kiểm thử phần mềm
### Kim tự tháp kiểm thử
Mô hình kim tự tháp kiểm thử do Mike Cohn phổ biến trong cuốn Succeeding with Agile [@cohn] chia kiểm thử tự động thành ba tầng: đáy là kiểm thử đơn vị với số lượng nhiều nhất, giữa là kiểm thử ở tầng dịch vụ thông qua API, đỉnh là kiểm thử qua giao diện với số lượng ít nhất. Fowler giải thích lý do: các bài kiểm thử đầu-cuối qua giao diện dễ vỡ, tốn công viết và chạy chậm [@fowler]. Hình 2.3 minh họa mô hình này.
!fig ../../diagrams/report/test-pyramid.drawio.png | Kim tự tháp kiểm thử (phỏng theo [@cohn] và [@fowler]) | 13
### Công cụ kiểm thử
Đồ án dùng một công cụ cho mỗi tầng của kim tự tháp:
- xUnit.net: công cụ kiểm thử đơn vị mã nguồn mở cho .NET [@xunit], dùng cho quy tắc nghiệp vụ phía máy chủ và kiểm thử tích hợp với cơ sở dữ liệu MySQL thật.
- Vitest: framework kiểm thử dựa trên Vite [@vitest], dùng cho các thành phần giao diện React.
- Playwright: framework kiểm thử đầu-cuối cho ứng dụng web hiện đại, hỗ trợ Chromium, WebKit và Firefox trên Windows, Linux và macOS, có sẵn bộ chạy kiểm thử, phép kiểm tra, cách ly và chạy song song [@playwright]. Mỗi ngữ cảnh trình duyệt cách ly đóng vai một người dùng, nhờ đó một kịch bản có thể mô phỏng nhiều thí sinh và giám thị cùng lúc.
## Đóng gói và triển khai
Docker là nền tảng phát triển, phân phối và chạy ứng dụng trong container: ứng dụng được đóng gói cùng môi trường chạy thành image, chạy cách ly với máy chủ thật, giúp môi trường phát triển và môi trường chạy thật giống nhau [@docker]. Docker Compose định nghĩa và chạy ứng dụng gồm nhiều container bằng một tệp cấu hình YAML [@compose]. nginx là máy chủ HTTP, có thể dùng làm máy chủ phục vụ tệp tĩnh và làm proxy ngược chuyển tiếp yêu cầu tới máy chủ ứng dụng [@nginx]. Với trung tâm, cách đóng gói này cho phép cài cả hệ thống (cơ sở dữ liệu, máy chủ ứng dụng, giao diện) lên máy chủ của phòng thi bằng một lệnh.
## Quy trình phát triển và mô hình hóa
### Scrum
Scrum là khung làm việc gọn nhẹ giúp con người, nhóm và tổ chức tạo ra giá trị qua các giải pháp thích ứng cho vấn đề phức tạp, dựa trên chủ nghĩa kinh nghiệm với ba trụ cột minh bạch, kiểm tra và thích ứng [@scrum]. Nhóm Scrum có ba trách nhiệm: Product Owner, Scrum Master và Developers. Mọi sự kiện diễn ra trong sprint, kéo dài không quá một tháng, gồm Sprint Planning, Daily Scrum, Sprint Review và Sprint Retrospective. Ba tạo phẩm là Product Backlog (cam kết: Product Goal), Sprint Backlog (cam kết: Sprint Goal) và Increment (cam kết: Definition of Done) [@scrum]. Trong đồ án, một người đảm nhận cả ba trách nhiệm; giảng viên hướng dẫn và trung tâm đóng vai trò các bên liên quan trong Sprint Review.
### Ngôn ngữ mô hình hóa UML
UML là ngôn ngữ mô hình hóa do Object Management Group chuẩn hóa, phiên bản hiện hành là 2.5.1 [@uml]. Đồ án sử dụng các loại sơ đồ: sơ đồ use case để mô tả chức năng theo tác nhân; sơ đồ máy trạng thái để mô tả vòng đời của một lượt làm bài (chưa bắt đầu, đang làm, tạm dừng, đã nộp); sơ đồ tuần tự để mô tả tương tác giữa các thành phần trong các thao tác chính. Mô hình dữ liệu được biểu diễn bằng sơ đồ thực thể – liên kết sinh trực tiếp từ lược đồ cơ sở dữ liệu. Các sơ đồ này được trình bày ở Chương 3.
Chương này đã trình bày cơ sở lý thuyết về thi trên máy tính theo bộ hướng dẫn của ITC, các kiến thức về kiến trúc ứng dụng web, giao tiếp thời gian thực, bảo mật, kiểm thử và quy trình phát triển. Chương 3 vận dụng các nội dung này để phân tích, thiết kế và hiện thực hệ thống.

# HIỆN THỰC HÓA NGHIÊN CỨU
@todo Nội dung chương này sẽ được viết ở bước tiếp theo (Sprint 8).

# KẾT QUẢ NGHIÊN CỨU
@todo Nội dung chương này sẽ được viết ở Sprint 10.

# KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN
@todo Nội dung chương này sẽ được viết ở Sprint 10.

@refs DANH MỤC TÀI LIỆU THAM KHẢO

#! PHỤ LỤC
@todo Phụ lục sẽ được bổ sung khi hoàn chỉnh báo cáo.
