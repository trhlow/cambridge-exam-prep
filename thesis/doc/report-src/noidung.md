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
CSV|Comma-Separated Values – tệp dữ liệu phân cách bằng dấu phẩy
E2E|End-to-End – kiểm thử đầu-cuối
EF Core|Entity Framework Core
HTML|HyperText Markup Language
HTTP|Hypertext Transfer Protocol
HTTPS|HTTP Secure – HTTP qua kênh mã hóa TLS
ITC|International Test Commission – Ủy ban Khảo thí Quốc tế
JSON|JavaScript Object Notation
LAN|Local Area Network – mạng cục bộ
ORM|Object-Relational Mapping – ánh xạ đối tượng – quan hệ
OWASP|Open Worldwide Application Security Project
PDF|Portable Document Format
PIN|Personal Identification Number – mã số dùng để mở bài thi
REST|Representational State Transfer
SEB|Safe Exam Browser
SHA|Secure Hash Algorithm – thuật toán băm an toàn
SPA|Single-Page Application – ứng dụng trang đơn
UML|Unified Modeling Language – ngôn ngữ mô hình hóa thống nhất
URL|Uniform Resource Locator – địa chỉ tài nguyên
UTC|Coordinated Universal Time – giờ phối hợp quốc tế
WCAG|Web Content Accessibility Guidelines
XSS|Cross-Site Scripting
@end

@lot DANH MỤC BẢNG BIỂU
@lof DANH MỤC HÌNH ẢNH

#! TÓM TẮT ĐỒ ÁN
Các kỳ thi Cambridge English Qualifications từ A2 Key đến C2 Proficiency đã có hình thức thi trên máy tính: thí sinh làm các phần Reading, Writing và Listening trên máy, còn giám thị theo dõi cả phòng trên một cổng quản lý. Để thí sinh quen với giao diện thi máy và để giám thị tập các thao tác xử lý sự cố, Trung tâm Anh ngữ Victory cần một hệ thống tổ chức thi thử ngay tại phòng máy của trung tâm, bằng đề do trung tâm cung cấp.
Đồ án xây dựng một website đáp ứng nhu cầu đó cho hai kỳ thi A2 Key for Schools và B1 Preliminary for Schools. Hướng tiếp cận là tái hiện bố cục, định dạng câu hỏi và quy trình giám sát của nền tảng thi máy mà Cambridge đang dùng, dựa trên tài liệu ngày thi, tài liệu tập huấn giám thị và bài thi mẫu chính thức; đồng thời đối chiếu thiết kế với bộ hướng dẫn về thi trên máy tính của Ủy ban Khảo thí Quốc tế (ITC).
Hệ thống gồm máy chủ ASP.NET Core trên .NET 10, cơ sở dữ liệu MySQL truy cập qua Entity Framework Core, giao diện React viết bằng TypeScript và kênh thời gian thực SignalR. Đồng hồ làm bài do máy chủ nắm giữ nên thí sinh mất mạng hay tắt máy vẫn được nộp bài đúng giờ. Giám thị tạm dừng, cộng giờ, sửa giờ kết thúc, tua lại phần nghe và gửi thông báo; các lệnh đến màn hình thí sinh gần như tức thì. Hệ thống còn có chấm tự động phần Reading và Listening, chia bài Writing ngẫu nhiên cho giám khảo chấm ẩn danh, nhập đề từ tệp PDF, soạn đề trên web, trộn từ hai đến sáu đề gốc thành nhiều mã đề chia theo vị trí máy, phân quyền năm vai trò với cơ chế hai quản trị viên duyệt chéo, và kiểm tra cấu hình Safe Exam Browser.
Kết quả: hệ thống chạy được trọn một buổi thi A2 và B1; có 1464 kiểm thử tự động phía máy chủ, 201 kiểm thử giao diện, 69 kiểm thử công cụ chuyển đề và 6 kịch bản kiểm thử đầu-cuối, tất cả đều đạt. Kiểm thử tải với 200 thí sinh đồng thời không phát sinh lỗi máy chủ; lưu câu trả lời và lệnh của giám thị đều dưới 0,1 giây. Buổi thi thử 16 tình huống chạy tự động trên trình duyệt đạt 16/16 tình huống sau khi sửa bốn lỗi phát hiện được, và vẫn đạt 16/16 khi chạy lại sau đợt rà soát toàn hệ thống với 42 vấn đề đã được sửa.

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
Giám khảo|Chấm phần Writing của các bài được chia ngẫu nhiên cho mình; chỉ thấy mã bài, không thấy tên thí sinh và bảng điểm.
Quản trị viên theo mảng|Quản lý đề thi, kỳ thi, thí sinh hoặc thông tin trung tâm trong phạm vi mảng được giao; chỉ thấy đề, kỳ thi do mình tạo hoặc được giao.
Admin|Toàn quyền; quản lý tài khoản nhân viên; duyệt các yêu cầu xóa dữ liệu và thay đổi tài khoản; xem bảng điểm; sửa đáp án đề đã xuất bản.
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
9|07/12–13/12|Dự phòng (dùng cho các yêu cầu mới của trung tâm ngày 29/10)
10|14/12–20/12|Viết báo cáo: kết quả và kết luận
11|21/12–27/12|Hoàn chỉnh và nộp báo cáo
|end|
Thực tế, công việc chạy nhanh hơn kế hoạch: các chức năng của hai kỳ thi, kiểm thử tải và buổi thi thử xong trước ngày 27/10, các yêu cầu mới của trung tâm ngày 29/10 được làm ngay trong tuần đó, và báo cáo được viết từ đầu tháng 11. Tiến độ thực tế được trình bày ở mục 3.1.
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
Chương này trình bày quá trình hiện thực hệ thống theo thứ tự: các bước đã thực hiện, đặc tả yêu cầu, thiết kế kiến trúc, thiết kế cơ sở dữ liệu, thiết kế xử lý, thiết kế giao diện, bảo mật, cài đặt và phương pháp kiểm thử. Các tài liệu thiết kế được viết trong sprint 2 (bản đặc tả yêu cầu và bản đặc tả thiết kế kèm đặc tả API) và được cập nhật theo mã nguồn; nội dung dưới đây mô tả hệ thống ở phiên bản ngày 07/11/2026. Các sơ đồ đều được sinh bằng script từ mã nguồn hoặc từ lược đồ cơ sở dữ liệu đang chạy, nên khớp với hệ thống thật.
## Các bước thực hiện
Đồ án đi theo kế hoạch sprint ở Bảng 1.4 nhưng thực tế chạy nhanh hơn lịch: các chức năng của hai kỳ thi xong trong khoảng bốn tuần, thời gian còn lại dành cho kiểm thử, rà soát và viết báo cáo. Bảng 3.1 tóm tắt các bước theo thời gian thực tế.
|table| Các bước thực hiện và sản phẩm | 2.6,6.2,7.7
Thời gian|Bước|Sản phẩm chính
10/10–11/10|Khảo sát và khởi động|Ghi chép giao diện bài thi mẫu A2 trên Inspera; lược đồ cơ sở dữ liệu đầu tiên; lớp đồng hồ AttemptClock viết theo hướng kiểm thử trước
11/10–15/10|Hiện thực A2 phía thí sinh và cổng nhân viên|Đăng nhập hai bước, gói đề, màn hình Reading & Writing và Listening, đồng hồ máy chủ, tự lưu, chấm tự động, cổng giám thị, bộ chuyển đề PDF; buổi demo 16/10
16/10–19/10|Yêu cầu mới của trung tâm về vai trò|Năm tác nhân, quản trị viên theo mảng, duyệt chéo hai Admin; bản đặc tả yêu cầu, đặc tả thiết kế và API, sơ đồ; đóng gói Docker
18/10–21/10|Hoàn thiện A2|SignalR, Safe Exam Browser, cảnh báo, soạn đề trên web, tên và logo trung tâm, trộn đề A2 theo vị trí máy
21/10|Mở rộng sang B1|Khảo sát bài thi mẫu B1; các kiểu câu mới của Reading, Writing chọn một trong hai câu, Listening một câu mỗi trang; kiểm thử đầu-cuối
22/10–27/10|Kiểm thử tải và thi thử|Kiểm thử tải 100 và 200 thí sinh; buổi thi thử 16 tình huống bằng trình duyệt tự động; sửa lỗi; siết ràng buộc lược đồ
29/10–01/11|Yêu cầu mới của trung tâm ngày 29/10|Trộn 2–6 đề gốc; lọc đề theo cấp độ; mỗi người chỉ thấy phần việc của mình; chia bài Writing ngẫu nhiên và chấm ẩn danh
02/11–07/11|Rà soát và sửa lỗi|Rà soát toàn hệ thống (42 vấn đề), sửa theo 8 nhóm, chạy lại toàn bộ kiểm thử, thi thử và kiểm thử tải
|end|
Khi khảo sát, tôi làm thử các bài thi mẫu chính thức của A2 Key for Schools và B1 Preliminary for Schools trên nền tảng Inspera, ghi lại từng màn hình (biểu tượng ở đầu trang, thanh chuyển phần, cách chia đôi màn hình, cách chọn từ, kéo thả, menu tùy chọn) và đo màu, kích thước bằng công cụ của trình duyệt. Các thao tác của giám thị được lấy từ tài liệu ngày thi [@booklet] và tài liệu tập huấn giám thị [@invig]. Những ghi chép này là căn cứ cho đặc tả yêu cầu ở mục 3.2 và thiết kế giao diện ở mục 3.6.
## Đặc tả yêu cầu
### Tác nhân và sơ đồ use case
Các tác nhân đã được giới thiệu ở Bảng 1.3. Sơ đồ use case tổng quát ở Hình 3.1 cho thấy chức năng của từng tác nhân. Tác nhân trừu tượng “Nhân sự” gom chức năng đăng nhập chung của bốn vai trò nhân viên; Admin kế thừa mọi chức năng của quản trị viên theo mảng. Hai tác nhân hệ thống là Safe Exam Browser (gửi khóa cấu hình trong mỗi yêu cầu) và đồng hồ hệ thống (tự nộp bài, chấm tự động, ghi cảnh báo).
!fig ../../diagrams/use-case.drawio.png | Sơ đồ use case tổng quát | 15.5
Phân quyền được thiết kế theo nguyên tắc “mỗi người chỉ thấy phần việc của mình”, do trung tâm yêu cầu ngày 29/10: quản trị viên mảng đề thi chỉ thấy đề và đề trộn do mình tạo; quản trị viên mảng kỳ thi chỉ tổ chức kỳ thi do mình tạo; quản trị viên mảng thí sinh chỉ nhập thí sinh cho kỳ thi mà người tổ chức đã giao; giám thị chỉ thấy kỳ thi được phân công; giám khảo không mở được kỳ thi nào mà chỉ nhận các bài Writing được chia cho mình. Admin thấy toàn bộ.
### Đặc tả các use case chính
Phần này đặc tả tám use case quan trọng nhất, gồm các use case làm bài của thí sinh, các use case giám sát mô phỏng quy trình của Cambridge và các use case xuất phát từ yêu cầu riêng của trung tâm (Bảng 3.2 đến Bảng 3.9).
|table| Use case UC01: Thí sinh đăng nhập và mở phần thi | 3.6,12.9
Mục|Nội dung
Tác nhân|Thí sinh; Safe Exam Browser
Tiền điều kiện|Kỳ thi ở trạng thái Active; thí sinh có phiếu đăng nhập; giám thị biết mã PIN của buổi thi.
Luồng chính|1) Thí sinh nhập tên đăng nhập và mật khẩu in trên phiếu. 2) Hệ thống kiểm tra mật khẩu, cấp phiên chưa xác nhận PIN và làm mất hiệu lực phiên ở máy khác. 3) Khi cả phòng đã đăng nhập, giám thị đọc mã PIN; thí sinh nhập PIN và chọn Unlock and Select. 4) Thí sinh kiểm tra họ tên, làm bước kiểm tra sẵn sàng (Readiness check). 5) Thí sinh chọn phần thi đầu tiên, đọc hướng dẫn, xác nhận và bấm Start. 6) Hệ thống tạo lượt làm bài, tính giờ kết thúc và trả về đề.
Luồng ngoại lệ|Sai mật khẩu hoặc PIN 10 lần: tài khoản bị khóa 5 phút. Kỳ thi chưa Active: không đăng nhập được. Kỳ thi có Config Key mà yêu cầu không đến từ Safe Exam Browser đúng cấu hình: từ chối. Phần thi sau khi phần trước chưa nộp: hiện Locked.
Hậu điều kiện|Có một lượt làm bài ở trạng thái InProgress, đồng hồ do máy chủ tính.
|end|
|table| Use case UC02: Thí sinh làm bài | 3.6,12.9
Mục|Nội dung
Tác nhân|Thí sinh
Tiền điều kiện|Lượt làm bài đang chạy.
Luồng chính|1) Thí sinh chuyển giữa các phần bằng thanh dưới màn hình, trả lời câu hỏi theo từng kiểu tương tác. 2) Mỗi thay đổi được đưa vào hàng đợi tự lưu và gửi lên máy chủ. 3) Năm giây một lần trình duyệt gửi tín hiệu heartbeat, nhận lại thời gian còn lại, trạng thái, thông báo của giám thị. 4) Thí sinh có thể đánh dấu câu, tô sáng, ghi chú, đổi cỡ chữ và độ tương phản, ẩn màn hình bằng menu.
Luồng ngoại lệ|Mất mạng: câu trả lời được giữ ở trình duyệt và gửi lại khi có mạng. Tải lại trang hoặc đổi máy: mở lại đúng phần thi, đúng trang, đúng câu trả lời. Bị tạm dừng: màn hình bị che, không nhập được.
Hậu điều kiện|Câu trả lời được lưu trên máy chủ. Thí sinh không có nút nộp bài: phần thi kết thúc khi hết giờ hoặc khi giám thị kết thúc.
|end|
|table| Use case UC03: Giám thị tạm dừng và cho làm tiếp | 3.6,12.9
Mục|Nội dung
Tác nhân|Giám thị (hoặc người tổ chức kỳ thi)
Tiền điều kiện|Giám thị được phân công cho kỳ thi và đang mở trang theo dõi của phần thi.
Luồng chính|1) Giám thị chọn một, nhiều thí sinh hoặc cả phòng. 2) Chọn Pause. 3) Hệ thống tạm dừng những thí sinh đang làm bài và đang trực tuyến, ghi nhật ký thao tác. 4) Màn hình thí sinh hiện thông báo tạm dừng gần như ngay lập tức; trang theo dõi cập nhật. 5) Khi xử lý xong, giám thị chọn Resume; thời gian dừng được cộng vào giờ kết thúc.
Luồng ngoại lệ|Thí sinh ngoại tuyến, chưa bắt đầu hoặc đã nộp: bị bỏ qua, kết quả ghi rõ lý do cho từng người.
Hậu điều kiện|Đồng hồ của thí sinh không chạy trong thời gian bị tạm dừng.
|end|
|table| Use case UC04: Xử lý sự cố phần Listening | 3.6,12.9
Mục|Nội dung
Tác nhân|Giám thị
Tiền điều kiện|Thí sinh đang làm phần Listening, không bị tạm dừng, bản ghi đã phát.
Luồng chính|1) Giám thị chọn thí sinh, chọn tua lại N phút và ghi chi tiết (tùy chọn). 2) Hệ thống lùi vị trí phát; máy thí sinh phát lại từ vị trí mới. 3) Hộp cộng giờ mở sẵn với cùng N phút cho đúng những thí sinh đã được tua. 4) Giám thị xác nhận; hệ thống cộng N phút thời gian bù sự cố.
Luồng ngoại lệ|Số phút lớn hơn phần đã phát hoặc thí sinh đang tạm dừng: bị từ chối.
Hậu điều kiện|Hai thao tác riêng biệt được ghi nhật ký, đúng như hướng dẫn của Cambridge [@invig, tr. 20].
|end|
|table| Use case UC05: Kết thúc bài làm | 3.6,12.9
Mục|Nội dung
Tác nhân|Giám thị
Tiền điều kiện|Thí sinh đang làm phần thi.
Luồng chính|1) Ở cấp phần thi, giám thị chọn Edit end time với End in, End at hoặc End now (bấm hai lần để xác nhận); End now có thêm tùy chọn bỏ thời gian cộng thêm. 2) Hệ thống tính lại giờ kết thúc; với End now, thời gian chuẩn kết thúc ngay và bài được nộp nếu thí sinh không còn thời gian cộng thêm. 3) Ở cấp kỳ thi, End all kết thúc cả bài thi của các thí sinh được chọn.
Luồng phụ|Lỡ kết thúc ở cấp kỳ thi: Use test end time trả lại giờ kết thúc chuẩn; phần đã nộp giữ nguyên, các phần sau được đủ giờ.
Hậu điều kiện|End now ở cấp phần thi không hoàn tác được và chỉ rút ngắn giờ, không bao giờ kéo dài.
|end|
|table| Use case UC06: Trộn đề và chia đề theo vị trí máy | 3.6,12.9
Mục|Nội dung
Tác nhân|Quản trị viên mảng đề thi; quản trị viên mảng kỳ thi
Tiền điều kiện|Có từ 2 đến 6 đề gốc đã xuất bản, cùng phần thi, cùng cấu trúc (số part, kiểu câu, số câu, điểm, bản ghi).
Luồng chính|1) Người quản lý đề chọn các đề gốc theo thứ tự A, B, C… và số mã đề K. 2) Hệ thống kiểm tra các đề cùng khuôn, sinh K đề trộn ở trạng thái đã xuất bản, mỗi part lấy nguyên từ một đề gốc. 3) Người tổ chức kỳ thi chọn bộ đề trộn cho phần thi và nhập số máy mỗi dãy. 4) Khi thí sinh bắt đầu, hệ thống giao mã đề theo số máy; sơ đồ phòng in được kèm mã đề.
Luồng ngoại lệ|Các đề không cùng khuôn: hệ thống liệt kê từng điểm khác nhau. K ngoài khoảng cho phép: từ chối.
Hậu điều kiện|Hai máy kề nhau theo hàng ngang, hàng dọc và đường chéo luôn khác mã đề; thí sinh đổi máy giữa giờ vẫn giữ đề đã nhận.
|end|
|table| Use case UC07: Chấm Writing ẩn danh | 3.6,12.9
Mục|Nội dung
Tác nhân|Giám khảo; hệ thống
Tiền điều kiện|Giám khảo được phân công cho kỳ thi; thí sinh đã nộp phần thi có Writing.
Luồng chính|1) Hệ thống chia bài cho các giám khảo của kỳ thi một cách ngẫu nhiên và đều, gán mỗi bài một mã sáu ký tự. 2) Giám khảo mở danh sách bài được chia, chọn một mã bài. 3) Hệ thống hiển thị đề Writing của đúng mã đề thí sinh đã làm và bài viết, không có tên thí sinh và các phần khác. 4) Giám khảo nhập điểm và nhận xét cho từng câu. 5) Khi đủ điểm mọi câu Writing, điểm phần thi bằng điểm tự động cộng điểm Writing và trạng thái chuyển sang Final.
Luồng ngoại lệ|Bài không được chia cho giám khảo này hoặc phần thi chưa kết thúc: hệ thống trả lời như không tồn tại.
Hậu điều kiện|Mọi lần chấm được lưu thành lịch sử; điểm mới nhất được tính.
|end|
|table| Use case UC08: Duyệt chéo hai Admin | 3.6,12.9
Mục|Nội dung
Tác nhân|Quản trị viên mảng hoặc Admin (người yêu cầu); một Admin khác (người duyệt)
Tiền điều kiện|Người yêu cầu có quyền ở mảng tương ứng.
Luồng chính|1) Người yêu cầu xóa đề, kỳ thi, thí sinh, bộ đề trộn hoặc thay đổi tài khoản nhân viên. 2) Hệ thống kiểm tra điều kiện và tạo yêu cầu chờ duyệt, chưa thực hiện thay đổi. 3) Một Admin khác xem danh sách yêu cầu và duyệt. 4) Hệ thống kiểm tra lại điều kiện và thực hiện thay đổi cùng với quyết định duyệt trong một lần ghi.
Luồng phụ|Người duyệt từ chối kèm lý do; người yêu cầu hủy khi yêu cầu còn chờ; điều kiện không còn đúng lúc duyệt thì yêu cầu chuyển Failed kèm lý do.
Hậu điều kiện|Không ai tự duyệt được yêu cầu của mình; mọi yêu cầu được giữ làm nhật ký.
|end|
### Quy tắc nghiệp vụ
Các quy tắc nghiệp vụ ở Bảng 3.10 được rút ra từ tài liệu của Cambridge và từ yêu cầu của trung tâm. Đây là những ràng buộc mà máy chủ kiểm tra; giao diện chỉ ẩn bớt những thao tác không hợp lệ.
|table| Các quy tắc nghiệp vụ chính | 1.6,10.4,4.5
Mã|Quy tắc|Nguồn
QT01|Các phần thi chạy lần lượt theo thứ tự của loại bài thi; phần sau chỉ mở khi mọi phần trước đã nộp. Thứ tự là dữ liệu, không viết cứng trong mã nguồn.|Cambridge [@booklet]
QT02|Đồng hồ và giờ kết thúc chỉ tính trên máy chủ; thí sinh ngoại tuyến vẫn bị nộp bài đúng giờ.|[@itc, tr. 16]
QT03|Thí sinh không có nút nộp bài; phần thi kết thúc khi hết giờ hoặc khi giám thị kết thúc.|Cambridge [@booklet]
QT04|Giám thị chỉ tạm dừng, cho làm tiếp những thí sinh đang trực tuyến và đang làm bài.|[@invig, tr. 15]
QT05|End now ở cấp phần thi kết thúc bài ngay, không hoàn tác được và chỉ rút ngắn giờ kết thúc.|[@invig, tr. 18]
QT06|Tua lại phần nghe và cộng giờ bù là hai thao tác riêng; chỉ tua khi thí sinh không bị tạm dừng và không quá phần đã phát.|[@invig, tr. 20]
QT07|Không cộng giờ cho thời gian đi vệ sinh; thời gian cộng thêm theo kế hoạch cố định từ lúc thí sinh bắt đầu, sự cố được bù bằng giờ bù riêng.|[@booklet, tr. 13]; thiết kế
QT08|Mỗi phòng thi có ít nhất một giám thị cho 25 thí sinh; hệ thống cảnh báo khi thiếu.|[@booklet, tr. 4]
QT09|Xóa đề, kỳ thi, thí sinh, bộ đề trộn và thay đổi tài khoản nhân viên phải được một Admin khác duyệt.|Trung tâm, 16/10
QT10|Hai máy kề nhau không làm cùng một mã đề; đề của thí sinh cố định từ lúc bắt đầu phần thi.|Trung tâm, 20/10 và 29/10
QT11|Bài Writing được chia ngẫu nhiên và đều cho giám khảo; giám khảo chỉ thấy mã bài, không thấy tên thí sinh và bảng điểm; thí sinh không bao giờ thấy điểm.|Trung tâm, 29/10
QT12|Mỗi người chỉ thấy đề, kỳ thi, thí sinh thuộc phần việc của mình; Admin thấy tất cả.|Trung tâm, 29/10
QT13|Bài làm là hồ sơ thi: không xóa được thí sinh, phần thi hay kỳ thi đã có bài làm.|Thiết kế
|end|
### Yêu cầu phi chức năng
Các yêu cầu phi chức năng đã nêu ở mục 1.5.2 được chuyển thành chỉ tiêu đo được và cách kiểm chứng, trình bày trong Bảng 3.11. Kết quả đo được trình bày ở Chương 4.
|table| Chỉ tiêu cho các yêu cầu phi chức năng | 3.4,6.6,6.5
Yêu cầu|Chỉ tiêu|Cách kiểm chứng
Độ trễ màn hình|Lưu câu trả lời và chuyển trang dưới 2 giây [@itc, tr. 15]|Kiểm thử tải, đo phân vị 95 và 99
Lệnh giám thị|Đến màn hình thí sinh trong vài giây|Kiểm thử tải và buổi thi thử đo thời gian từ lúc bấm đến lúc màn hình đổi
Phát hiện mất kết nối|Trang giám thị báo Offline trong khoảng 15 giây|Buổi thi thử, tình huống mất mạng
Đúng giờ|Mọi thí sinh, kể cả máy đã tắt, được nộp bài khi hết giờ|Kiểm thử tích hợp và buổi thi thử
Không mất bài|Câu trả lời chọn lúc mất mạng, lúc bị tạm dừng hay khi tải lại trang không bị mất|Kiểm thử đơn vị hàng đợi tự lưu, kiểm thử đầu-cuối, buổi thi thử
Quy mô|100 thí sinh đồng thời trên một máy chủ, không lỗi máy chủ|Kiểm thử tải 100 và 200 thí sinh
Bảo mật|Phân quyền ở API; duyệt chéo; khóa tài khoản khi nhập sai|Kiểm thử tích hợp cho từng quy tắc truy cập
|end|
## Thiết kế kiến trúc
### Kiến trúc tổng thể
Hệ thống chạy trên một máy chủ đặt trong phòng thi, gồm ba thành phần đóng gói thành ba container Docker (Hình 3.2). Thành phần web là nginx, vừa phục vụ các tệp tĩnh của giao diện React vừa chuyển tiếp các yêu cầu /api và /hubs tới máy chủ ứng dụng. Thành phần api là ứng dụng ASP.NET Core, cung cấp REST API, hai hub SignalR, các dịch vụ nền và chạy bộ chuyển đề PDF viết bằng Python. Thành phần mysql là cơ sở dữ liệu MySQL 8.4. Nhờ nginx đứng trước, trình duyệt chỉ làm việc với một địa chỉ duy nhất, nên cookie đăng nhập giữ được thuộc tính SameSite=Strict.
!fig ../../diagrams/report/deployment.drawio.png | Sơ đồ triển khai hệ thống trong phòng thi | 16
Máy thí sinh chỉ cần trình duyệt; trong kỳ thi thật trình duyệt là Safe Exam Browser. Máy giám thị và nhân viên dùng Chrome hoặc Edge, mở cổng nhân viên tại đường dẫn /staff. Cơ sở dữ liệu chỉ mở ở địa chỉ 127.0.0.1 của máy chủ, các máy khác trong mạng không kết nối trực tiếp được. Hình ảnh, âm thanh của đề và khóa mã hóa cookie nằm trong thư mục App_Data, tách khỏi cơ sở dữ liệu.
### Kiến trúc phía máy chủ
Mã nguồn phía máy chủ chia thành ba project theo Clean Architecture (mục 2.2.3), cộng một project kiểm thử (Bảng 3.12). Chiều phụ thuộc hướng vào lõi: Infrastructure và Api phụ thuộc Domain, còn Domain không phụ thuộc Entity Framework Core hay ASP.NET Core.
|table| Các project phía máy chủ | 3.8,12.7
Project|Trách nhiệm
ExamPrep.Domain|28 lớp thực thể, các kiểu liệt kê và các quy tắc nghiệp vụ thuần: AttemptClock (đồng hồ, âm thanh), AnswerMatcher và ChosenQuestions (chấm), MarkingAllocation (chia bài), PaperShape, PaperMixPlan và SeatPattern (trộn đề, xếp chỗ), CandidateUsername, Invigilation. Vi phạm quy tắc ném DomainRuleException.
ExamPrep.Infrastructure|DbContext, một lớp cấu hình cho mỗi thực thể, các migration, dữ liệu khởi tạo; đọc, kiểm tra và nhập gói đề; lưu tệp theo băm SHA-256; trộn đề; chấm tự động (AttemptMarker); chia bài cho giám khảo (MarkingAllocator); đề trắng theo khung.
ExamPrep.Api|Ứng dụng ASP.NET Core: controller theo nhóm chức năng, hai cơ chế xác thực bằng cookie, chính sách phân quyền, bộ lọc Safe Exam Browser, hub SignalR, dịch vụ nền (nộp bài hết giờ, theo dõi mất kết nối), duyệt chéo, bộ tạo dữ liệu demo.
ExamPrep.Tests|Kiểm thử đơn vị cho Domain và kiểm thử tích hợp chạy API thật trên cơ sở dữ liệu MySQL tạo riêng cho mỗi lần chạy.
|end|
API được tổ chức theo tiền tố: /api/candidate cho thí sinh, /api/staff cho các thao tác trong phòng thi và chấm bài, /api/admin cho quản lý đề, kỳ thi, thí sinh, tài khoản. Danh sách các endpoint (82 phương thức xử lý) ở Phụ lục 1. Dữ liệu trao đổi là JSON, lỗi trả về theo định dạng ProblemDetails với một câu dành cho người dùng; mã 409 kèm trường code (paused, finished, busy) để trình duyệt biết nên thử lại hay dừng.
### Kiến trúc phía giao diện
Giao diện là một ứng dụng trang đơn React viết bằng TypeScript, đóng gói bằng Vite. Ứng dụng có hai phần: màn hình thí sinh ở đường dẫn gốc và cổng nhân viên ở /staff. Cổng nhân viên là một gói mã riêng chỉ tải khi mở /staff, nên máy thí sinh không tải mã của cổng quản lý. Các thư mục chính:
- exam: màn hình làm bài, phân trang (paperLogic), hàng đợi tự lưu (autosave), các kiểu câu hỏi, trình phát phần nghe, ghi chú và tô sáng, menu tùy chọn.
- screens: các màn hình đăng nhập theo đúng các bước trong tài liệu ngày thi.
- staff: cổng nhân viên, gồm các trang theo đường dẫn, lớp gọi API, bảng quyền theo vai trò (access.ts, phản chiếu chính sách của API) và các trang của giám thị, giám khảo, quản lý đề.
- api: lớp gọi API chung và kết nối SignalR (live.ts).
HTML của đề được làm sạch hai lần: khi nhập vào máy chủ bằng HtmlSanitizer và khi hiển thị bằng DOMPurify.
### Kênh thời gian thực và tác vụ nền
Hệ thống có hai hub SignalR. Hub /hubs/staff phục vụ trang theo dõi: trang gọi WatchSection để tham gia nhóm của một phần thi (máy chủ kiểm tra lại quyền của tài khoản trong cơ sở dữ liệu), sau đó nhận thông điệp sectionChanged mỗi khi bảng theo dõi cần tải lại. Thông điệp được gộp, tối đa hai lần mỗi giây cho mỗi phần thi, để phòng đông không làm trang giám thị tải lại liên tục. Hub /hubs/candidate gửi thông điệp refresh tới máy thí sinh sau mỗi thao tác của giám thị, thông báo hoặc khi hết giờ; máy thí sinh lập tức gửi heartbeat để lấy trạng thái mới. Khi kết nối cuối cùng của thí sinh đóng, máy chủ đánh dấu thí sinh ngoại tuyến. Các thông số thời gian được chọn theo khuyến nghị của tài liệu SignalR [@signalr_config] và trình bày trong Bảng 3.13.
|table| Các thông số thời gian của hệ thống | 7.5,3,6
Thông số|Giá trị|Ghi chú
Chu kỳ heartbeat của máy thí sinh|5 giây|Đồng hồ, trạng thái, thông báo, vị trí phần nghe
Cửa sổ trực tuyến|15 giây|Không có heartbeat trong 15 giây thì coi là ngoại tuyến
ClientTimeoutInterval của máy chủ|12 giây|Kết nối im lặng quá thời gian này bị ngắt
Keep-alive của trình duyệt|4 giây|Nhỏ hơn một nửa thời gian chờ của máy chủ
Gộp thông điệp sectionChanged|500 mili giây|Tối đa hai lần tải lại mỗi giây cho mỗi phần thi
Trang theo dõi tự tải lại|20 giây hoặc 3 giây|20 giây khi có SignalR, 3 giây khi mất kết nối SignalR
Quét bài hết giờ|5 giây|Kèm 5 giây ân hạn cho lần tự lưu cuối
Tự lưu: chờ sau thay đổi cuối|800 mili giây|Không quá 3 giây kể từ thay đổi chưa gửi đầu tiên
|end|
Hai dịch vụ nền chạy cùng ứng dụng theo cơ chế hosted service [@hosted]. AttemptExpiryWorker quét mỗi 5 giây, nộp các lượt làm bài đã quá giờ kết thúc (kể cả của thí sinh ngoại tuyến), sau đó gọi AttemptMarker chấm các bài vừa nộp. RoomBroadcaster gửi các thông điệp sectionChanged đã gộp và phát hiện các thí sinh vừa ngừng gửi heartbeat, ghi cảnh báo mất kết nối và báo trang giám thị. Ở môi trường Development, bộ tạo dữ liệu demo chạy thêm khi máy chủ khởi động.
## Thiết kế cơ sở dữ liệu
### Tổng quan lược đồ
Cơ sở dữ liệu gồm 28 bảng chia bốn nhóm (Bảng 3.14, Hình 3.3). Lược đồ được tạo bằng migration của Entity Framework Core. Tên bảng và tên cột viết thường, nối bằng dấu gạch dưới; mọi kiểu liệt kê lưu dưới dạng chữ để dễ đọc khi tra cứu trực tiếp; mọi thời điểm lưu theo giờ UTC.
|table| Các nhóm bảng của cơ sở dữ liệu | 4,2,10.5
Nhóm|Số bảng|Các bảng
Nội dung đề thi|11|exam_types, section_templates, test_papers, paper_parts, question_groups, questions, answer_keys, answer_corrections, paper_mixes, paper_mix_sources, assets
Kỳ thi và thí sinh|7|exams, exam_sections, candidates, exam_invigilators, exam_examiners, exam_candidate_managers, candidate_messages
Làm bài, chấm điểm và giám sát|6|attempts, responses, annotations, manual_marks, invigilator_actions, security_events
Tài khoản nhân sự và cấu hình|4|users, user_areas, approval_requests, site_settings
|end|
!fig ../../diagrams/erd.drawio.png | Sơ đồ tổng quan bốn nhóm bảng và các khóa ngoại giữa các nhóm | 14
Sơ đồ thực thể – liên kết được sinh bằng script đọc trực tiếp lược đồ MySQL đang chạy, nên luôn khớp với migration mới nhất. Mỗi nhóm có một sơ đồ chi tiết (Hình 3.4 đến Hình 3.7), chỉ vẽ khóa ngoại trong nhóm; khóa ngoại sang nhóm khác được ghi ngay sau tên cột.
### Nhóm nội dung đề thi
Quyết định thiết kế quan trọng nhất là mô tả loại bài thi bằng dữ liệu: bảng exam_types chứa A2 Key for Schools và B1 Preliminary for Schools, bảng section_templates chứa các phần thi theo thứ tự cùng thời lượng (A2 có hai phần, B1 có ba phần). Nhờ vậy B1 dùng chung bộ máy với A2 mà không phải sửa mã, và IELTS sau này chỉ cần thêm dữ liệu.
!fig ../../diagrams/erd-content.drawio.png | Sơ đồ thực thể – liên kết nhóm nội dung đề thi | 16
Một đề (test_papers) thuộc một mẫu phần thi, gồm các part (paper_parts), mỗi part có các nhóm câu (question_groups), mỗi nhóm có các câu hỏi (questions) và đáp án (answer_keys; nhiều dòng là các đáp án được chấp nhận). Cột interaction_type của nhóm câu quyết định giao diện câu hỏi. Mỗi part Listening tham chiếu một tệp âm thanh. Bảng assets lưu thông tin tệp hình và âm thanh, nội dung tệp nằm trên đĩa theo băm SHA-256. Bộ đề trộn (paper_mixes) ghi các đề gốc theo vị trí A, B, C… trong paper_mix_sources; mỗi đề trộn là một test_papers bình thường, có thêm mã đề và mẫu trộn (ví dụ “ACBBA”). Bảng answer_corrections ghi nhật ký mỗi lần Admin sửa đáp án của đề đã xuất bản.
### Nhóm kỳ thi và thí sinh
!fig ../../diagrams/erd-exams.drawio.png | Sơ đồ thực thể – liên kết nhóm kỳ thi và thí sinh | 16
Một kỳ thi (exams) có trạng thái Draft, Scheduled, Active hoặc Closed, mã PIN, mật khẩu giám thị, Config Key của Safe Exam Browser (nếu dùng) và số máy mỗi dãy. Mỗi phần thi của kỳ thi (exam_sections) gắn một đề hoặc một bộ đề trộn và ghi thời điểm giám thị báo bắt đầu. Thí sinh (candidates) có số báo danh, số máy, phần trăm thời gian cộng thêm, mật khẩu đã băm, mốc kết thúc ở cấp kỳ thi (parent_end_override_at), giám khảo được chia và mã bài Writing. Ba bảng phân công nối kỳ thi với giám thị, giám khảo và quản trị viên mảng thí sinh. Bảng candidate_messages lưu thông báo của giám thị cho cả phòng hoặc từng thí sinh.
Kỳ thi chỉ đổi trạng thái theo các chuyển hợp lệ: Draft và Scheduled qua lại; Scheduled lên Active; Active về Scheduled khi chưa ai bắt đầu; Active sang Closed khi không còn ai đang làm bài; Closed mở lại Active chỉ do Admin. Chuyển sang Active cần có thí sinh, giám thị và số máy dạng số.
### Nhóm làm bài, chấm điểm và giám sát
!fig ../../diagrams/erd-delivery.drawio.png | Sơ đồ thực thể – liên kết nhóm làm bài, chấm điểm và giám sát | 16
Bảng attempts là trung tâm của hệ thống: mỗi thí sinh có một lượt làm bài cho mỗi phần thi. Các cột của bảng chia thành bốn nhóm: trạng thái (status, submit_reason, grading_status), các mốc và đại lượng của đồng hồ (Bảng 3.15), trạng thái phần nghe (audio_started_at, audio_adjust_ms) và kết nối (last_heartbeat_at, connection_status). Cột test_paper_id ghi đề mà lượt làm bài dùng, được cố định lúc bắt đầu; mọi chức năng sau đó (giao đề, phát âm thanh, chấm, bảng điểm) đọc cột này chứ không đọc đề của phần thi, nên thí sinh đổi máy vẫn giữ đúng đề.
|table| Các cột đồng hồ của bảng attempts | 5,11.5
Cột|Ý nghĩa
started_at|Thời điểm thí sinh bắt đầu phần thi
paused_at, paused_seconds|Thời điểm bắt đầu lần tạm dừng hiện tại; tổng số giây đã tạm dừng
end_adjust_seconds|Số giây điều chỉnh có dấu do End now, End at, End in (tương đối, nên lần tạm dừng sau vẫn dời được giờ kết thúc)
planned_extra_seconds|Thời gian cộng thêm theo kế hoạch, tính từ phần trăm của thí sinh lúc bắt đầu
incident_extra_seconds|Tổng thời gian bù sự cố
disregard_extra_time|Bỏ thời gian cộng thêm (tùy chọn của End now)
ends_at|Giờ kết thúc đã tính, dùng để quét bài hết giờ
concurrency_stamp|Mã phiên bản của dòng, đổi mỗi lần lưu
|end|
Bảng responses lưu câu trả lời theo câu hỏi và lựa chọn Yes, No, Undecided ở phần Writing chọn một trong hai câu của B1. Bảng manual_marks lưu điểm Writing của giám khảo dưới dạng lịch sử, điểm mới nhất được tính. Bảng annotations dành cho ghi chú và tô sáng. Hai bảng giám sát là invigilator_actions (nhật ký mọi thao tác của giám thị) và security_events (các cảnh báo mà máy chủ quan sát được).
### Nhóm tài khoản nhân sự và cấu hình
!fig ../../diagrams/erd-accounts.drawio.png | Sơ đồ thực thể – liên kết nhóm tài khoản nhân sự và cấu hình | 16
Bảng users chỉ chứa nhân viên, với một trong bốn vai trò Admin, Manager, Examiner, Invigilator; các mảng của quản trị viên nằm ở user_areas (Papers, Exams, Candidates, Centre). Bảng approval_requests vừa là hàng đợi vừa là nhật ký của cơ chế duyệt chéo: loại yêu cầu, người yêu cầu, người quyết định, trạng thái (Pending, Approved, Rejected, Cancelled, Failed) và dữ liệu của thay đổi. Bảng site_settings chỉ có một dòng, chứa tên và logo của trung tâm.
### Ràng buộc toàn vẹn và điều khiển tương tranh
Ngoài khóa chính, khóa ngoại và chỉ mục duy nhất, lược đồ dùng ràng buộc CHECK của MySQL để cơ sở dữ liệu tự từ chối dữ liệu sai, kể cả khi có lỗi trong mã ứng dụng:
- Mỗi cột kiểu liệt kê có một ràng buộc CHECK chỉ nhận đúng các tên của kiểu liệt kê đó; ràng buộc được sinh từ mô hình, nên thêm một giá trị mới chỉ cần một migration.
- Ràng buộc CK_attempts_state buộc trạng thái của lượt làm bài khớp với các mốc thời gian: đã bắt đầu thì có started_at, tạm dừng khi và chỉ khi có paused_at, đã nộp khi và chỉ khi có submitted_at và lý do nộp, chỉ bài đã nộp mới được chấm.
- Các ràng buộc giá trị: thời lượng dương, phần trăm cộng thêm từ 0 đến 100, các đại lượng đồng hồ không âm, điểm không vượt điểm tối đa, mã PIN từ 4 đến 8 chữ số, Config Key là 64 ký tự hệ 16, bảng site_settings chỉ một dòng, yêu cầu chờ duyệt khi và chỉ khi chưa có người quyết định, người quyết định khác người yêu cầu.
- Khóa ngoại từ attempts tới thí sinh và phần thi dùng RESTRICT: cơ sở dữ liệu từ chối xóa thí sinh, phần thi hay kỳ thi đã có bài làm.
- Các ràng buộc liên dòng (số câu hỏi không trùng trong một đề, đề khớp mẫu phần thi) do bộ nhập đề kiểm tra.
Về tương tranh, cùng một lượt làm bài có thể bị nhiều nguồn sửa cùng lúc: heartbeat của thí sinh, thao tác của giám thị, dịch vụ quét bài hết giờ. Hệ thống dùng điều khiển tương tranh lạc quan [@efconc]: cột concurrency_stamp được DbContext đổi mỗi lần lưu một lượt làm bài; lần ghi dựa trên dữ liệu cũ gây ngoại lệ DbUpdateConcurrencyException, khi đó máy chủ đọc lại dữ liệu và thử lại một lần, nếu vẫn xung đột thì trả lời 409 kèm một câu giải thích, không bao giờ ghi đè. Với các thao tác liên quan cả kỳ thi (đổi trạng thái hay sửa kỳ thi, thí sinh bắt đầu phần thi, thêm hoặc sửa thí sinh), hệ thống dùng khóa dòng của bảng exams trong giao dịch mức READ COMMITTED: đổi trạng thái và sửa kỳ thi giữ khóa độc quyền, bắt đầu phần thi và sửa thí sinh giữ khóa chia sẻ. Nhờ vậy không thể đóng kỳ thi đúng lúc một thí sinh đang bắt đầu. Lỗi chờ khóa quá hạn hay deadlock của MySQL được chuyển thành mã 409 với code busy để trình duyệt tự thử lại. Bảng attempts có thêm chỉ mục cho các truy vấn quét định kỳ.
## Thiết kế xử lý
### Vòng đời của lượt làm bài
Hình 3.8 là sơ đồ máy trạng thái của một lượt làm bài. Trạng thái làm bài (NotStarted, InProgress, Paused, Submitted) và trạng thái chấm (NotGraded, AutoGraded, NeedsManualMarking, Final) là hai cột riêng: sau khi nộp, lượt làm bài đi tiếp trong trạng thái chấm. Một lượt làm bài được nộp với một trong hai lý do: hết giờ (TimeExpired, kể cả khi giờ kết thúc bị kéo về sớm bởi End in, End at hoặc End all) hoặc giám thị bấm End now (InvigilatorEndNow).
!fig ../../diagrams/attempt-state.drawio.png | Sơ đồ máy trạng thái của một lượt làm bài | 16
### Đồng hồ do máy chủ tính
Mọi thay đổi đồng hồ đi qua lớp AttemptClock trong Domain. Lớp này không cho sửa giờ kết thúc trực tiếp mà tính lại từ các đại lượng ở Bảng 3.15 mỗi khi có thao tác:
- Giờ kết thúc chuẩn = started_at + thời lượng phần thi + paused_seconds + end_adjust_seconds.
- Giờ kết thúc theo lịch = giờ kết thúc chuẩn + planned_extra_seconds + incident_extra_seconds (bằng giờ kết thúc chuẩn nếu disregard_extra_time được bật).
- Giờ kết thúc thực tế (ends_at) = giá trị nhỏ hơn giữa giờ kết thúc theo lịch và mốc kết thúc cấp kỳ thi parent_end_override_at của thí sinh (nếu có).
- Thời gian còn lại = ends_at trừ thời điểm hiện tại, hoặc trừ paused_at khi đang tạm dừng.
- Vị trí phần nghe = thời điểm hiện tại (hoặc paused_at) trừ audio_started_at trừ audio_adjust_ms.
Cách tính tương đối này giúp các thao tác kết hợp với nhau đúng: chẳng hạn giám thị đặt End in 2 phút rồi tạm dừng thí sinh 1 phút, giờ kết thúc vẫn dời thêm 1 phút. Bảng 3.16 tóm tắt tác động của từng thao tác.
|table| Tác động của các thao tác lên đồng hồ | 3.6,6.5,6.4
Thao tác|Điều kiện|Tác động
Start|Chưa bắt đầu|Ghi started_at, tính planned_extra_seconds từ phần trăm của thí sinh
Pause|Đang làm, trực tuyến|Ghi paused_at; đồng hồ đứng yên
Resume|Đang tạm dừng, trực tuyến|Cộng thời gian dừng vào paused_seconds; nếu phần nghe đã phát thì cộng vào audio_adjust_ms để âm thanh tiếp tục đúng chỗ
Incident extra time|Đang làm hoặc tạm dừng|Cộng vào incident_extra_seconds
End in, End at|Đang làm|Điều chỉnh end_adjust_seconds để còn đúng số phút hoặc kết thúc đúng giờ chỉ định
End now|Đang làm hoặc tạm dừng|end_adjust_seconds cộng thêm min(0, phần nguyên của hiện tại trừ giờ kết thúc chuẩn), nên chỉ rút ngắn; nộp ngay nếu không còn thời gian cộng thêm
Use test end time|Đang làm hoặc tạm dừng|Đưa end_adjust_seconds về 0 và bỏ cờ disregard_extra_time
End all|Kỳ thi Active|Ghi parent_end_override_at của thí sinh, tính lại các lượt đang chạy; dịch vụ quét nộp bài
Rewind audio|Đang làm, không tạm dừng|Cộng vào audio_adjust_ms, không quá phần đã phát
|end|
Riêng End now được làm tròn xuống đến giây. Buổi thi thử ngày 26/10 cho thấy nếu làm tròn đến giây gần nhất, giờ kết thúc có thể rơi vào nửa giây sau thời điểm bấm, bài bị dịch vụ quét nộp với lý do hết giờ thay vì giám thị kết thúc. Trình duyệt chỉ hiển thị thời gian còn lại do máy chủ trả về và tự đếm ngược giữa hai lần heartbeat, không dùng đồng hồ của máy thí sinh để tính giờ.
### Đăng nhập và bắt đầu phần thi
Hình 3.9 là sơ đồ tuần tự của use case UC01. Đăng nhập gồm hai bước như trên Inspera: bước một kiểm tra phiếu đăng nhập và cấp cookie có claim pin_verified bằng False; bước hai kiểm tra mã PIN và cấp lại cookie với pin_verified bằng True. Mọi API làm bài yêu cầu cookie đã xác nhận PIN. Lúc đăng nhập, máy chủ đổi security_stamp của thí sinh; vì mỗi yêu cầu đều đối chiếu security_stamp, máy cũ bị đăng xuất ngay khi thí sinh đăng nhập ở máy mới.
!fig ../../diagrams/seq-candidate-login.drawio.png | Sơ đồ tuần tự thí sinh đăng nhập và bắt đầu phần thi | 12.5
Tên đăng nhập được sinh theo quy tắc của Cambridge: năm chữ cái đầu của họ (bỏ dấu), chữ cái đầu của tên và số báo danh, ví dụ thí sinh Nguyễn An số báo danh 0101 có tên đăng nhập nguyea0101; nếu họ ngắn hơn năm chữ cái, hệ thống lấy thêm chữ cái của tên để đủ sáu chữ cái khi có thể. Mật khẩu sinh ngẫu nhiên, được băm để đăng nhập và đồng thời được mã hóa bằng Data Protection để in lại phiếu khi cần.
### Lệnh của giám thị và cập nhật tức thì
Hình 3.10 là sơ đồ tuần tự của use case UC03. Trang theo dõi giữ kết nối với /hubs/staff; máy thí sinh giữ kết nối với /hubs/candidate. Khi giám thị bấm Pause, máy chủ kiểm tra từng thí sinh theo các quy tắc thao tác của giám thị (lớp InvigilatorActionRules), áp dụng AttemptClock, ghi nhật ký, rồi gửi refresh cho máy của những thí sinh bị ảnh hưởng. Máy thí sinh gọi heartbeat ngay, nhận trạng thái Paused và che màn hình. Song song, trang theo dõi nhận sectionChanged và tải lại bảng. Kết quả trả về cho giám thị ghi rõ thao tác được áp dụng cho bao nhiêu người và bỏ qua những ai, vì sao, như cách Inspera báo.
!fig ../../diagrams/seq-invigilator-pause.drawio.png | Sơ đồ tuần tự giám thị tạm dừng thí sinh | 16
Nếu kênh SignalR bị đứt, cả hai phía vẫn hoạt động nhờ thăm dò định kỳ: máy thí sinh vẫn gửi heartbeat 5 giây một lần, trang theo dõi tự tải lại mỗi 3 giây. SignalR chỉ làm cho cập nhật đến sớm hơn, không phải điều kiện để hệ thống chạy đúng.
### Tự lưu câu trả lời
Hàng đợi tự lưu ở trình duyệt được thiết kế để không mất câu trả lời trong các tình huống đã gặp:
- Mỗi thay đổi được gom lại và gửi sau 800 mili giây không có thay đổi mới, nhưng không quá 3 giây kể từ thay đổi chưa gửi đầu tiên, nên thí sinh gõ liên tục vẫn được lưu đều đặn. Yêu cầu dùng phương thức PUT và ghi đè câu trả lời của từng câu, nên gửi lại nhiều lần không tạo dữ liệu trùng (tính lũy đẳng, mục 2.2.1).
- Câu trả lời chưa gửi được sao vào localStorage theo kỳ thi, thí sinh và phần thi; khi mở lại trang mà lượt làm bài còn chạy, trình duyệt gửi lại; khi rời trang, trình duyệt gửi bằng fetch với tùy chọn keepalive.
- Máy chủ trả 409 với code paused khi thí sinh đang bị tạm dừng: hàng đợi giữ nguyên câu trả lời và thử lại; code busy cũng thử lại; code finished, 401 và 403 thì gửi lần cuối rồi đóng hàng đợi. Trong lúc tạm dừng, vùng làm bài bị đặt thuộc tính inert nên thí sinh không gõ được dưới lớp che.
- Mọi đường nộp bài đúng giờ (dịch vụ quét, heartbeat, đọc lượt làm bài, lưu câu trả lời) dùng chung 5 giây ân hạn, để lần lưu cuối gửi ngay trước khi hết giờ vẫn được nhận.
- Ô nhập bị giới hạn 20 000 ký tự, bằng giới hạn của máy chủ, để một câu trả lời quá dài không làm hỏng cả lô.
Khi thí sinh thoát, mọi bản sao chưa gửi, ghi chú và tô sáng bị xóa khỏi trình duyệt, để người dùng máy sau không đọc được.
### Phát phần nghe
Theo bố cục của Inspera, mỗi part Listening có một tệp âm thanh riêng nhưng thí sinh nghe như một bản ghi liền mạch, chỉ một lần, không có nút điều khiển (chỉ có nút chỉnh âm lượng). Khi thí sinh bấm phát lần đầu, máy chủ ghi audio_started_at một lần duy nhất. Từ đó vị trí phát là đại lượng do máy chủ tính (mục 3.5.2). Trình phát ở trình duyệt nối các tệp theo thứ tự part và đồng bộ với vị trí của máy chủ: tải lại trang thì phát tiếp từ vị trí hiện tại; sau khi bị tạm dừng rồi cho làm tiếp, hoặc sau khi giám thị tua lại, kể cả khi bản ghi đã phát hết, trình phát tự phát lại từ vị trí mới mà không cần thí sinh bấm. Một tệp không tải được trong 10 giây thì bị bỏ qua để các lần tua sau vẫn hoạt động.
### Trộn đề và chia đề theo vị trí máy
Đơn vị trộn là part: mỗi part của đề trộn được sao nguyên từ một đề gốc, kèm đoạn văn, hình, âm thanh và đáp án, nên bố cục và cách chấm không đổi. Trước khi trộn, lớp PaperShape so sánh các đề gốc và liệt kê mọi điểm khác nhau (phần thi, số part, nhóm câu, kiểu tương tác, số câu, điểm, bản ghi); chỉ các đề cùng khuôn mới trộn được.
Với N đề gốc (2 ≤ N ≤ 6) và một phần thi có P part, người quản lý đề chọn số mã đề K với 4 ≤ K ≤ min(3N, N mũ P). Thuật toán PaperMixPlan chia K mã đề thành các nhóm có kích thước đều nhau, mỗi nhóm không quá N mã. Mỗi nhóm bắt đầu từ một mẫu b (với mỗi part p, b[p] là đề gốc của part đó) được chọn ngẫu nhiên từ một hạt giống lưu lại, ưu tiên các mẫu có pha trộn nhiều đề gốc. Mã đề thứ j của nhóm lấy part p từ đề gốc (b[p] + j) mod N, tức là “xoay” mẫu qua các đề gốc; vì vậy hai mã đề trong cùng một nhóm không có part nào trùng nhau. Cùng hạt giống luôn cho cùng kết quả, nên có thể tái tạo khi cần đối chiếu.
Lớp SeatPattern giao mã đề theo số máy. Các máy được đánh số từ 1 theo từng dãy; mỗi dãy lấy lần lượt một nhóm mã đề và các máy trong dãy chạy qua các mã của nhóm đó, nên hai máy ngồi cạnh nhau không trùng part nào, còn dãy trước và dãy sau dùng nhóm khác. Khi chỉ có một nhóm hoặc có nhóm chỉ một mã, hệ thống dùng công thức mã = (cột + 2 × dãy) mod K. Trong cả hai trường hợp, tám máy xung quanh một máy (trái, phải, trước, sau và bốn góc chéo) đều có mã đề khác. Bảng 3.17 minh họa trường hợp hai đề gốc, bốn mã đề (hai nhóm gồm mã 1, 2 và mã 3, 4), phòng bốn máy mỗi dãy.
|table| Ví dụ chia mã đề cho phòng bốn máy mỗi dãy (hai đề gốc, bốn mã đề) | 3,3.2,3.2,3.2,3.2
Dãy|Máy 1|Máy 2|Máy 3|Máy 4
1|Mã 1|Mã 2|Mã 1|Mã 2
2|Mã 3|Mã 4|Mã 3|Mã 4
3|Mã 1|Mã 2|Mã 1|Mã 2
|end|
Mã đề của thí sinh được cố định trong attempts.test_paper_id ngay khi bắt đầu phần thi. Khi kỳ thi chuyển sang Active, các thí sinh chưa có số máy được gán số máy nhỏ nhất chưa ai nhập, theo thứ tự danh sách; số máy và số máy mỗi dãy bị khóa khi đã có người bắt đầu, nên không ai bị đổi đề giữa chừng. Trang sơ đồ phòng in được danh sách mã đề kèm mẫu trộn của từng mã và vị trí từng thí sinh.
### Chấm điểm và chia bài cho giám khảo
Sau mỗi lần quét bài hết giờ, AttemptMarker chấm các lượt làm bài đã nộp mà chưa chấm. Lớp AnswerMatcher so sánh câu trả lời với các đáp án được chấp nhận: câu chọn so theo mã lựa chọn; câu gõ chữ so sau khi bỏ khoảng trắng thừa, chuẩn hóa chữ hoa chữ thường (trừ câu phân biệt), dấu nháy cong và dấu chấm cuối câu, nhưng không bao giờ bỏ qua lỗi chính tả. Bộ chuyển đề mở rộng các đáp án có phần tùy chọn trong đáp án của Cambridge, ví dụ “(a/the) waterfall(s)”, thành mọi dạng được chấp nhận. Đề không có Writing được chấm xong (AutoGraded); đề có Writing chờ giám khảo (NeedsManualMarking).
Ở B1 Writing Part 2, thí sinh chọn một trong hai câu. Lớp ChosenQuestions quyết định câu được chấm: những câu thí sinh chọn Yes; nếu không có, những câu đã viết đầu tiên ở trạng thái Undecided; không bao giờ chấm câu chọn No. Điểm tối đa của nhóm tính theo số câu được chọn.
Lớp MarkingAllocation chia bài: các thí sinh chưa được chia được xáo trộn, mỗi bài giao cho giám khảo đang có ít bài nhất, nếu bằng nhau thì chọn ngẫu nhiên; 20 thí sinh và 4 giám khảo cho mỗi người 5 bài. Mỗi bài nhận một mã sáu ký tự từ bảng chữ không gây nhầm lẫn (không có I, O, 0, 1). Chỉ những tài khoản đang hoạt động có vai trò Examiner mới được chia bài; bài đã chấm xong ở lại với giám khảo đã chấm; khi danh sách giám khảo thay đổi, những bài chưa ai bắt đầu chấm được chia lại. Lưu một điểm là một giao dịch giữ khóa dòng, kiểm tra lại bài vẫn thuộc giám khảo đó, ghi thêm một dòng lịch sử và tính lại điểm.
Khi phát hiện đáp án của một đề đã xuất bản bị sai, Admin sửa đáp án của đề gốc; bản sửa được áp sang các đề trộn đã lấy câu đó, mọi lượt làm bài đã nộp trên các đề này được chấm lại, và lần sửa được ghi vào answer_corrections.
### Duyệt chéo hai Admin
Hình 3.11 là sơ đồ tuần tự của use case UC08 với ví dụ xóa kỳ thi. Endpoint xóa không xóa ngay mà trả mã 202 kèm thông tin yêu cầu. Lớp ApprovalActions kiểm tra điều kiện của từng loại yêu cầu hai lần: khi tạo (để báo lỗi sớm) và khi duyệt (vì tình huống có thể đã đổi). Việc thực hiện và quyết định duyệt được lưu trong cùng một lần ghi; cột status là concurrency token, nên hai Admin không thể cùng quyết định một yêu cầu. Khi hệ thống chỉ có một Admin, Admin thứ hai được tạo trực tiếp để cơ chế có thể bắt đầu.
!fig ../../diagrams/seq-approval.drawio.png | Sơ đồ tuần tự duyệt chéo hai Admin cho yêu cầu xóa kỳ thi | 13
### Nhập đề từ tệp PDF
Vì hệ thống nhập đề cũ của trung tâm không còn, tôi xây dựng bộ chuyển đề bằng Python dùng thư viện PyMuPDF. Quản trị viên chọn cấp độ và phần thi, tải lên tệp PDF đề, tệp PDF đáp án và (với Listening) mỗi part một tệp âm thanh. Máy chủ lưu các tệp vào thư mục tạm, chạy bộ chuyển đề với danh sách tham số cố định và giới hạn thời gian, đọc báo cáo JSON, rồi nhập kết quả như một gói đề. Bộ chuyển nhận biết loại đề qua trang bìa và từ chối tệp có trang bìa khác phần thi đã chọn; mỗi phần thi có một bộ đọc riêng làm việc trên các dòng chữ có tọa độ, vì thứ tự chữ thuần trong PDF không đúng thứ tự đọc. Hình được lấy từ ảnh gốc nhúng trong PDF thay vì chụp vùng trang, để không dính chữ bên cạnh. Bộ chuyển đề còn sinh tệp REVIEW.md liệt kê lỗi, các điểm cần xem và bảng từng câu.
Gói đề (một tệp zip gồm paper.json, thư mục hình và âm thanh) đi qua ba bước: đọc (JSON chặt, tên tệp không được ra ngoài gói, giới hạn số tệp và dung lượng), kiểm tra (số part theo mẫu phần thi, đánh số câu từ 1 đến N, quy tắc của từng kiểu tương tác; mọi lỗi được trả về thành câu dễ đọc) và nhập (lưu tệp theo băm SHA-256, làm sạch HTML, ghi trong một giao dịch, đề ở trạng thái nháp). Người nhập kiểm tra từng câu với đáp án, hình và âm thanh rồi mới xuất bản. Ngoài ra, quản trị viên có thể tạo đề trắng theo khung của cả năm phần thi A2 và B1 và viết toàn bộ đề trên web; đề trắng không xuất bản được khi còn chữ giữ chỗ, hình giữ chỗ hoặc câu chưa có đáp án.
## Thiết kế giao diện
### Màn hình thí sinh
Màn hình thí sinh được thiết kế để thí sinh không gặp khác biệt về thao tác khi bước vào kỳ thi chính thức (mục 2.1.3). Tôi không sao chép mã hay hình ảnh của Inspera mà dựng lại bố cục từ ghi chép và số đo trên bài thi mẫu: đầu trang có logo trung tâm, họ tên và số báo danh, đồng hồ, các biểu tượng kết nối, chuông thông báo, menu, ghi chú và thông tin thí sinh; cuối trang là thanh chuyển part, part đang mở hiện các số câu, các part khác hiện số câu đã làm dạng “0 of 7”; nút chuyển trang ở góc phải dưới. Các giá trị đo được đưa vào biến CSS ở một chỗ duy nhất (Bảng 3.18).
|table| Một số giá trị giao diện đo trên bài thi mẫu | 5.5,11
Thành phần|Giá trị
Nền trang, hộp nội dung|Nền #F7FAFA, hộp trắng viền #D5D5D5; chữ Arial 16 px
Đầu trang, thanh part|Đầu trang cao 72 px; thanh part cao 53 px, part không mở nền #EFEFEF
Số câu hỏi|Viền 1,6 px màu #418EC8, bo góc 3 px
Lựa chọn đã chọn|Nền xanh nhạt #C4DCF0
Nút chuyển trang|56 × 56 px; nút tiếp theo màu #007E86, nút đang tắt #DDDDDD
|end|
Cấu trúc câu hỏi được điều khiển bởi dữ liệu: cột interaction_type của nhóm câu chọn thành phần giao diện (Bảng 3.19), các ô trống được đánh dấu bằng {{q:n}} trong đoạn văn. Một part được hiển thị mỗi câu một trang khi dữ liệu yêu cầu (B1 Listening Part 2, B1 Writing Part 2) hoặc khi mọi câu có hình riêng (A2 Reading Part 1, Listening Part 1); các part khác hiển thị cả part trên một trang, có thể chia đôi màn hình với thanh kéo ở giữa.
|table| Các kiểu tương tác câu hỏi | 3.8,7,5.7
Kiểu|Giao diện|Dùng ở
SingleChoice|Chọn một trong ba phương án chữ|A2 Reading 1–3, Listening 3–4; B1 Reading 1, 3, Listening 2, 4
ImageChoice|Chọn một trong ba hình|A2 và B1 Listening Part 1
InlineChoice|Bấm vào ô trống, chọn từ trên thanh hiện phía trên|A2 Reading 4; B1 Reading 5
InlineText|Gõ một từ vào ô trống|A2 Reading 5, Listening 2; B1 Reading 6, Listening 3
DragDropMatch|Kéo thẻ vào ô; ba bố cục: hàng nhãn, người kèm mô tả, ô trống trong bài đọc|A2 Listening 5; B1 Reading 2, 4
Essay|Ô viết bài có đếm số từ; chọn một trong hai câu ở B1 Writing Part 2|A2 Writing 6–7; B1 Writing 1–2
|end|
Các tiện ích của thí sinh gồm: đánh dấu câu (cờ), tô sáng và ghi chú khi bôi đen chữ (dùng CSS Custom Highlight API [@highlight], không chèn phần tử vào đoạn văn), menu tùy chọn với ba chế độ tương phản và ba cỡ chữ (lưu trên máy chủ theo thí sinh), ẩn đồng hồ, ẩn màn hình khi rời chỗ. Từ 5 phút cuối, đồng hồ hiển thị phút và giây. Toàn bộ chữ trên màn hình thí sinh và cổng nhân viên bằng tiếng Anh, như bài thi thật; các bước đăng nhập theo đúng lời hướng dẫn trong tài liệu ngày thi [@booklet, tr. 11].
### Cổng nhân viên
Cổng nhân viên dùng một bố cục đơn giản, khác hẳn màn hình thí sinh, để nhân viên không nhầm hai giao diện. Thanh điều hướng chỉ hiện những trang thuộc vai trò của người đăng nhập (Bảng 3.20); máy chủ vẫn kiểm tra quyền ở mọi endpoint. Trang theo dõi phòng thi mô phỏng trang theo dõi phần thi của Inspera: bảng thí sinh với các cột trạng thái, kết nối, số phút còn lại, giờ bắt đầu, thời gian cộng thêm, thời gian bù sự cố, số câu đã làm, cảnh báo, và thanh thao tác hiện ở dưới khi chọn thí sinh. Các thao tác không hoàn tác được đều cần bấm hai lần để xác nhận.
|table| Các trang của cổng nhân viên theo vai trò | 3.4,13.1
Vai trò|Trang
Admin|Exams (mọi kỳ thi, kết quả, xuất CSV), Papers, Centre, Staff (tài khoản nhân viên), Approvals (duyệt yêu cầu, nhật ký)
Quản trị viên|Các trang của mảng được giao: Papers (nhập, soạn, trộn đề), Exams (kỳ thi do mình tạo, phân công, sơ đồ phòng), thí sinh của kỳ thi được giao, Centre; Approvals (yêu cầu của mình)
Giám thị|Exams (kỳ thi được phân công: mã PIN, phiếu đăng nhập), trang theo dõi từng phần thi, thông báo, cảnh báo
Giám khảo|Marking (danh sách bài được chia theo mã, trang chấm Writing)
|end|
## Bảo mật
Thiết kế bảo mật bám theo danh sách rủi ro OWASP Top 10:2025 (Bảng 2.3) và bộ hướng dẫn của ITC về xác thực và bảo vệ dữ liệu. Bảng 3.21 tóm tắt các biện pháp chính.
|table| Các biện pháp bảo mật theo nhóm rủi ro | 4.2,12.3
Nhóm rủi ro|Biện pháp
Kiểm soát truy cập (A01)|Chính sách theo vai trò và mảng ở mọi endpoint; lớp ExamAccess lọc kỳ thi theo người tạo và phân công, chỉ tính phân công khớp vai trò hiện tại; đề và tệp của đề chỉ phục vụ khi thí sinh có lượt làm bài đang chạy; giám khảo chỉ đọc được tệp Writing của bài được chia; tài nguyên ngoài phạm vi trả 404 như không tồn tại
Xác thực (A07)|Hai cơ chế cookie riêng cho nhân viên và thí sinh (HttpOnly, SameSite=Strict, Secure ở môi trường Production); mật khẩu băm bằng bộ băm của ASP.NET Core Identity; mỗi yêu cầu đối chiếu security_stamp; khóa tài khoản 5 phút sau 10 lần sai, đếm theo mã tài khoản vì MySQL so tên không phân biệt hoa thường và dấu
Chèn mã (A05)|Truy vấn qua EF Core có tham số; HTML của đề làm sạch bằng HtmlSanitizer khi nhập và DOMPurify khi hiển thị; HTML chỉ được liên kết tới tệp của chính người soạn hoặc tệp đề đã dùng; bảng điểm CSV chống chèn công thức Excel
Cấu hình (A02)|Mọi phản hồi có nosniff, X-Frame-Options DENY, no-referrer và CSP default-src 'none'; ngoài Development dùng HSTS và trang lỗi không lộ chi tiết; bản thi thật chạy HTTPS, không có tài khoản demo; MySQL chỉ mở ở 127.0.0.1; container API chạy bằng người dùng không có quyền root
Thiết kế không an toàn (A06)|Duyệt chéo hai Admin cho mọi thao tác xóa và thay đổi tài khoản; không ai tự duyệt yêu cầu của mình (cả ràng buộc CHECK); hạ quyền hoặc khóa một Admin hủy các yêu cầu đang chờ của người đó
Toàn vẹn dữ liệu (A08)|Ràng buộc CHECK, khóa ngoại RESTRICT cho bài làm, concurrency_stamp, khóa dòng; tệp gói đề kiểm tra tên và kích thước; logo chỉ nhận PNG theo chữ ký tệp, tối đa 1 MB
Ghi nhật ký (A09)|Nhật ký thao tác giám thị, cảnh báo an ninh, nhật ký duyệt chéo, lịch sử điểm Writing, nhật ký sửa đáp án
|end|
Với Safe Exam Browser, mỗi kỳ thi có thể lưu Config Key của tệp cấu hình SEB. Một bộ lọc toàn cục kiểm tra mọi yêu cầu của thí sinh thuộc kỳ thi đó: tiêu đề X-SafeExamBrowser-ConfigKeyHash phải bằng băm SHA-256 của URL tuyệt đối nối với Config Key [@sebkey]; nếu không, máy chủ trả 403 với câu “Open this test in Safe Exam Browser.”. Vì băm phụ thuộc cả cổng, nginx chuyển nguyên tiêu đề Host cho máy chủ ứng dụng. Các cảnh báo trên trang giám thị chỉ gồm những gì máy chủ thực sự quan sát được: yêu cầu không đến từ SEB đúng cấu hình trong khi đang làm bài, mất kết nối, đồng hồ máy thí sinh lệch quá 2 phút, và bắt đầu trước khi giám thị báo bắt đầu. Một cảnh báo chưa xử lý không được ghi lặp; cảnh báo vừa được xử lý không xuất hiện lại cùng loại trong 30 phút.
## Cài đặt và triển khai
### Công nghệ sử dụng
Bảng 3.22 liệt kê các công nghệ và phiên bản dùng trong mã nguồn.
|table| Công nghệ và phiên bản | 4.5,4.5,7.5
Thành phần|Công nghệ|Ghi chú
Máy chủ|.NET 10, ASP.NET Core, C#|SDK 10.0.401
Truy cập dữ liệu|EF Core 10, MySql.EntityFrameworkCore 10.0.9|Nhà cung cấp của Oracle, vì Pomelo chưa có bản cho EF Core 10
Cơ sở dữ liệu|MySQL 8.4|InnoDB, bảng mã utf8mb4
Thời gian thực|ASP.NET Core SignalR; @microsoft/signalr 8|WebSocket là phương thức chính
Giao diện|React 19, TypeScript 6, Vite 8|DOMPurify 3 làm sạch HTML
Làm sạch HTML máy chủ|HtmlSanitizer 9|
Bộ chuyển đề|Python 3, PyMuPDF 1.28|ffmpeg cho công cụ tách bản ghi Listening
Kiểm thử|xUnit v3, Vitest 5, Testing Library, Playwright 1.56, pytest|
Triển khai|Docker, Docker Compose, nginx|
Quản lý dự án|Git, GitHub, Jira|
|end|
Quy mô mã nguồn tại thời điểm viết báo cáo được trình bày trong Bảng 3.23 (đếm số dòng của các tệp trong kho mã; mã máy chủ không tính migration sinh tự động).
|table| Quy mô mã nguồn | 6,3,7.5
Phần|Số dòng|Ghi chú
Máy chủ (C#)|12 285|162 tệp, không gồm migration
Migration của EF Core (C#)|37 212|Sinh tự động
Kiểm thử máy chủ (C#)|11 602|59 tệp
Giao diện (TypeScript, TSX)|9 047|Không gồm tệp kiểm thử
Kiểm thử giao diện|3 031|43 tệp
CSS|2 715|
Bộ chuyển đề (Python)|2 476|Không gồm tệp kiểm thử
Kiểm thử đầu-cuối và thi thử (Playwright)|1 222|
|end|
### Hai môi trường triển khai
Cả hệ thống được cài bằng Docker Compose với một lệnh. Có hai cách chạy dùng chung tệp cấu hình cơ sở (Bảng 3.24). Bản demo dùng cho buổi demo, thi thử và kiểm thử; bản thi thật thêm tệp cấu hình ghi đè cho môi trường Production và luôn chạy dưới tên dự án riêng để không dùng chung cơ sở dữ liệu với bản demo. Hướng dẫn cài đặt chi tiết nằm trong tệp setup/INSTALL.md (tóm tắt ở Phụ lục 2).
|table| So sánh bản demo và bản thi thật | 4,6,6.5
Đặc điểm|Bản demo|Bản thi thật
Môi trường|Development|Production
Giao thức, cổng|HTTP, cổng 8080|HTTPS cổng 443, cổng 80 chuyển sang HTTPS
Dữ liệu mẫu|Hai kỳ thi demo A2 và B1, tài khoản demo|Không có; Admin đầu tiên đặt trong tệp cấu hình, mật khẩu ít nhất 12 ký tự
Kỳ thi demo|Được xóa và tạo lại mỗi lần máy chủ khởi động (chỉ những kỳ thi đánh dấu is_demo)|Không có
Tệp đề, khóa mã hóa|Thư mục App_Data của mã nguồn|Volume Docker riêng
Cookie|Không bắt buộc HTTPS|Chỉ gửi qua HTTPS (cờ Secure)
|end|
Cơ sở dữ liệu tự cập nhật lược đồ khi máy chủ khởi động. Trang /health kiểm tra cả máy chủ ứng dụng và cơ sở dữ liệu. Sau kiểm thử tải (mục 4.4), cấu hình được điều chỉnh: kho kết nối 250 kết nối, MySQL cho phép 300 kết nối và số luồng tối thiểu của máy chủ là 200, vì trình điều khiển MySQL của Oracle thực hiện các lệnh bất đồng bộ theo kiểu chặn luồng.
## Phương pháp kiểm thử
Kiểm thử được tổ chức theo kim tự tháp kiểm thử (mục 2.6.1) và được bổ sung bằng hai hình thức kiểm thử ở mức hệ thống (Bảng 3.25). Mọi thay đổi hành vi được viết theo hướng kiểm thử trước: viết kiểm thử, chạy thấy thất bại đúng lý do, rồi mới viết mã.
|table| Các mức kiểm thử | 3.6,4.6,8.3
Mức|Công cụ|Nội dung
Đơn vị|xUnit v3, Vitest, pytest|Quy tắc của Domain (đồng hồ, chấm, trộn đề, xếp chỗ, chia bài); logic giao diện (phân trang, tự lưu, đồng hồ hiển thị); các bộ đọc của bộ chuyển đề trên các dòng chữ tự soạn đặt đúng tọa độ như đề thật
Tích hợp|xUnit v3, WebApplicationFactory, MySQL thật|Gọi API thật trên cơ sở dữ liệu MySQL tạo riêng và xóa sau mỗi lần chạy; kiểm tra phân quyền của từng endpoint, ràng buộc CHECK, giao dịch, tương tranh (hai yêu cầu đồng thời)
Đầu-cuối|Playwright|6 kịch bản chạy trên một stack Docker tách riêng (cổng 8090, cơ sở dữ liệu trống) với đề mẫu tự soạn
Thi thử|Playwright, mỗi người một ngữ cảnh trình duyệt|16 tình huống của một buổi thi thật, 11 người (hai Admin, giám thị, giám khảo, sáu thí sinh A2, một thí sinh B1), Listening chạy đủ 30 phút
Tải|Script Node.js tự viết|100 và 200 thí sinh đồng thời làm bài, một giám thị theo dõi và thao tác qua SignalR
|end|
Kiểm thử tích hợp chạy trên MySQL thật thay vì cơ sở dữ liệu trong bộ nhớ, vì nhiều quy tắc nằm ở ràng buộc CHECK, khóa dòng và mức cô lập giao dịch của MySQL. Mỗi lần chạy tạo một cơ sở dữ liệu riêng có tên bắt đầu bằng examprep_test_ và xóa khi xong; các migration được chạy tuần tự trên một kết nối không dùng kho kết nối, vì khóa migration của EF Core trên MySQL có phạm vi cả máy chủ.
Buổi thi thử theo kịch bản trong tệp setup/mock-exam/KICH_BAN_THI_THU.md. Kịch bản được tự động hóa bằng Playwright để chạy lại được sau mỗi đợt sửa lỗi: mỗi người là một ngữ cảnh trình duyệt riêng với cookie riêng, thao tác qua giao diện như người dùng; kịch bản tự chuẩn bị hai kỳ thi qua cổng nhân viên, đi qua 16 tình huống, đo thời gian, chụp màn hình, ghi kết quả từng tình huống và tiếp tục khi một tình huống không đạt. Biên bản Word được sinh từ kết quả.
Cuối cùng, sau khi hoàn thành các chức năng, toàn bộ hệ thống được rà soát độc lập theo năm mảng (thí sinh; phòng thi; phân quyền; đề, kỳ thi và chấm điểm; dữ liệu và triển khai). Mỗi vấn đề được ghi vị trí trong mã nguồn và hướng sửa, đối chiếu lại với mã nguồn trước khi sửa, và mỗi bản sửa đi kèm kiểm thử tái hiện vấn đề. Chương này đã trình bày đặc tả, thiết kế và cách xây dựng hệ thống; Chương 4 trình bày kết quả đạt được và các số liệu kiểm thử.

# KẾT QUẢ NGHIÊN CỨU
Chương này trình bày các chức năng đã hoàn thành qua ảnh chụp màn hình, kết quả kiểm thử tự động, buổi thi thử, kiểm thử tải, đợt rà soát toàn hệ thống và đánh giá so với mục tiêu đặt ra. Mọi ảnh chụp dùng đề mẫu do tôi tự soạn, không có nội dung đề của Cambridge hay của trung tâm.
## Kết quả chức năng
### Màn hình thí sinh
Thí sinh đăng nhập bằng phiếu, chờ giám thị đọc mã PIN rồi nhập mã PIN để mở bài (Hình 4.1). Màn hình chọn phần thi hiện bước kiểm tra sẵn sàng và các phần thi theo thứ tự; Listening bị khóa cho đến khi Reading & Writing kết thúc (Hình 4.2).
!fig img/ts-pin.png | Bước nhập mã PIN của buổi thi | 11
!fig img/ts-chon-phan-thi.png | Màn hình chọn phần thi, Listening bị khóa | 11
Hình 4.3 là màn hình làm bài Reading & Writing A2 với bố cục như mô tả ở mục 3.6.1: đầu trang có đồng hồ và các biểu tượng, thanh part ở cuối trang hiện số câu đã làm của từng part. Hình 4.4 là cùng bài thi ở chế độ chữ trắng trên nền đen, cỡ chữ lớn, với một đoạn được tô sáng.
!fig img/ts-lam-bai.png | Màn hình làm bài A2 Reading & Writing, Part 1 | 15.5
!fig img/ts-tuong-phan.png | Chế độ tương phản chữ trắng trên nền đen, cỡ chữ lớn và đoạn văn được tô sáng | 15.5
Ở phần Listening (Hình 4.5), dòng “Audio is Playing” dưới tên thí sinh cho biết bản ghi đang phát; khi giám thị tua lại, màn hình báo “The invigilator has moved the recording back.” và phát tiếp từ vị trí mới. Hình 4.6 là phần Writing Part 2 của B1, thí sinh chọn một trong hai câu bằng hộp “Answering this question?”. Khi bị tạm dừng, nội dung bài thi bị che và thí sinh được báo đồng hồ không chạy (Hình 4.7).
!fig img/ts-listening.png | Phần Listening A2 sau khi giám thị tua lại bản ghi | 15.5
!fig img/ts-b1-writing.png | B1 Writing Part 2: chọn một trong hai câu | 15.5
!fig img/ts-tam-dung.png | Màn hình thí sinh khi bị giám thị tạm dừng | 15.5
### Trang giám thị
Hình 4.8 là trang theo dõi phần Reading & Writing ngay sau khi giám thị tạm dừng cả phòng: thanh trên báo thao tác áp dụng cho 6 thí sinh và bỏ qua một thí sinh chưa bắt đầu, kèm lý do. Hình 4.9 cho thấy một thí sinh ngoại tuyến và các cảnh báo ở cột Warnings (một cảnh báo bắt đầu trước khi giám thị báo bắt đầu, một cảnh báo mất kết nối). Hình 4.10 là hộp Edit end time với lời nhắc End now không hoàn tác được ở cấp phần thi.
!fig img/gt-tam-dung-ca-phong.png | Trang theo dõi phần thi sau khi tạm dừng cả phòng | 15.5
!fig img/gt-offline.png | Trang theo dõi báo thí sinh ngoại tuyến và các cảnh báo | 15.5
!fig img/gt-end-now.png | Hộp Edit end time với lựa chọn End now | 15.5
### Trang giám khảo
Giám khảo chỉ thấy danh sách bài được chia cho mình theo mã bài (Hình 4.11). Trang chấm hiện đề Writing của đúng mã đề thí sinh đã làm, bài viết, số từ, ô điểm và nhận xét cho từng câu, không có tên thí sinh (Hình 4.12).
!fig img/gk-danh-sach-bai.png | Danh sách bài Writing được chia cho giám khảo | 15.5
!fig img/gk-cham-writing.png | Trang chấm Writing ẩn danh | 15.5
### Trang quản trị
Admin xem bảng điểm của kỳ thi, có mã bài Writing và giám khảo được chia, trạng thái chấm từng phần thi, và tải bảng điểm dưới dạng CSV mở được bằng Excel (Hình 4.13). Trang Approvals hiển thị hàng đợi và nhật ký duyệt chéo (Hình 4.14). Sơ đồ phòng của kỳ thi B1 dùng đề trộn liệt kê mẫu trộn của bốn mã đề cho từng phần thi và mã đề của từng máy (Hình 4.15).
!fig img/ad-ket-qua.png | Bảng điểm của kỳ thi thử A2 | 15.5
!fig img/ad-duyet-cheo.png | Nhật ký duyệt chéo hai Admin | 15.5
!fig img/ad-so-do-phong.png | Sơ đồ phòng của kỳ thi B1 với bốn mã đề trộn | 15.5
### Quản lý đề thi
Trang Papers lọc đề theo cấp độ, phần thi, trạng thái và tên, có các nút nhập từ PDF, tạo đề trắng, trộn đề và nhập gói đề (Hình 4.16). Hộp nhập từ PDF chỉ hỏi những tệp cần cho phần thi đã chọn, ví dụ B1 Listening cần bốn tệp âm thanh (Hình 4.17). Hình 4.18 là một đề trắng A2 vừa tạo, các chữ “[to write: …]” chờ người soạn điền.
!fig img/de-danh-sach.png | Trang danh sách đề thi | 15.5
!fig img/de-nhap-pdf.png | Hộp nhập đề từ tệp PDF | 15.5
!fig img/de-trang.png | Đề trắng A2 Reading & Writing soạn trên web | 15.5
### Mức độ hoàn thành yêu cầu
Bảng 4.1 đối chiếu các nhóm yêu cầu chức năng ở mục 1.1.2 với kết quả.
|table| Mức độ hoàn thành các nhóm yêu cầu chức năng | 3.4,9.5,3.6
Nhóm yêu cầu|Kết quả|Trạng thái
Quản lý đề thi|Nhập từ PDF cho cả năm phần thi A2 và B1, nhập gói đề, soạn và sửa đề trên web, đề trắng, xuất bản, sửa đáp án đề đã xuất bản, trộn 2–6 đề gốc|Hoàn thành
Tổ chức kỳ thi|Tạo kỳ thi theo cấu trúc dữ liệu, vòng đời bốn trạng thái, nhập thí sinh từ Excel, phiếu đăng nhập, sơ đồ phòng, phân công giám thị, giám khảo, quản trị viên thí sinh|Hoàn thành
Làm bài|Đăng nhập hai bước, sáu kiểu tương tác, đồng hồ máy chủ, tự lưu, Listening liền mạch, tiện ích của thí sinh, Safe Exam Browser|Hoàn thành; SEB chưa thử trên phòng máy thật
Giám sát|Theo dõi thời gian thực, tạm dừng, cộng giờ, sửa giờ kết thúc, End all, tua phần nghe, thông báo, cảnh báo|Hoàn thành
Chấm điểm|Chấm tự động, chia bài Writing ẩn danh, lịch sử điểm, bảng điểm CSV|Hoàn thành
Phân quyền|Năm tác nhân, quản trị viên theo mảng, mỗi người chỉ thấy phần việc của mình, duyệt chéo|Hoàn thành
|end|
## Kết quả kiểm thử tự động
Ngày 07/11/2026, toàn bộ kiểm thử tự động đều đạt (Bảng 4.2). Số kiểm thử tăng mạnh từ cuối tháng 10, khi các ràng buộc lược đồ được siết lại và các yêu cầu mới của trung tâm được bổ sung, và tăng thêm trong đợt sửa lỗi sau rà soát, vì mỗi vấn đề được sửa kèm kiểm thử tái hiện (Bảng 4.3).
|table| Kết quả kiểm thử tự động ngày 07/11/2026 | 5,3,8.5
Bộ kiểm thử|Số kiểm thử đạt|Phạm vi
Máy chủ (xUnit)|1464/1464|Đơn vị Domain, tích hợp API trên MySQL thật
Giao diện (Vitest)|201/201|Thành phần React, tự lưu, phân trang, kết nối SignalR, quyền theo vai trò
Bộ chuyển đề (pytest)|69/69|Các bộ đọc theo part, đáp án, tách bản ghi Listening, dòng lệnh
Đầu-cuối (Playwright)|6/6|Thí sinh; giám thị; đề và duyệt chéo; kết thúc và chấm; kỳ thi B1; mã đề theo chỗ ngồi B1
|end|
|table| Số kiểm thử qua các mốc | 1.7,6.4,2.3,2.3,2.3,1.5
Ngày|Mốc|Máy chủ|Giao diện|Chuyển đề|E2E
13/10|Rà soát đầu tiên|231|71|35|–
21/10|Hết sprint 3 (A2 hoàn chỉnh, trộn đề)|350|88|60|–
21/10|Hết sprint 6 (B1 chạy đầu-cuối)|381|98|69|6
26/10|Hết sprint 7 (thi thử, sửa lỗi)|390|104|69|6
02/11|Bắt đầu rà soát|1345|119|69|6
07/11|Sau khi sửa 42 vấn đề|1464|201|69|6
|end|
## Kết quả buổi thi thử
Buổi thi thử chạy đầy đủ hai lần. Lần ngày 26/10 tìm được bốn lỗi; sau khi sửa, lần chạy cuối của ngày đó đạt 16/16 tình huống. Lần ngày 07/11 chạy lại trên mã nguồn đã sửa sau đợt rà soát, từ 9 giờ 29 đến 10 giờ 12, cũng đạt 16/16 và không phát sinh lỗi mới. Bảng 4.4 trình bày kết quả của lần chạy ngày 07/11 với các số đo chính.
|table| Kết quả 16 tình huống của buổi thi thử ngày 07/11/2026 | 1,6,7.9,1.6
STT|Tình huống|Kết quả quan sát|Đánh giá
1|Đăng nhập bằng phiếu và mã PIN|6 thí sinh vào màn hình chọn phần thi, Listening bị khóa|Đạt
2|Nhập sai mã PIN|3 lần sai bị từ chối với “Incorrect PIN.”, lần đúng vào được|Đạt
3|Bắt đầu trước khi giám thị báo bắt đầu|Chỉ thí sinh bắt đầu sớm có cảnh báo StartedEarly|Đạt
4|Làm bài, tô sáng, đổi cỡ chữ và tương phản|Câu trả lời lưu ngay; tùy chọn có hiệu lực và được lưu trên máy chủ|Đạt
5|Tải lại trang giữa giờ|Về đúng Part 3, còn câu trả lời và đoạn tô sáng, đồng hồ lệch 0 giây|Đạt
6|Tạm dừng một thí sinh, cả phòng; cho làm tiếp|Một thí sinh dừng sau 146 ms; cả phòng 6/6 sau tối đa 166 ms; đồng hồ đứng yên trong 10 giây dừng|Đạt
7|Thông báo cho cả phòng và một thí sinh|Cả phòng 6/6 sau tối đa 163 ms; thông báo riêng chỉ đến đúng người|Đạt
8|Mất mạng 1 phút rồi vào lại|Trang giám thị báo Offline sau 10 giây, có cảnh báo; đồng hồ vẫn chạy; câu trả lời chọn lúc mất mạng được lưu khi có mạng|Đạt
9|Chuyển sang máy khác|Máy cũ về màn hình đăng nhập; máy mới tiếp tục cùng lượt làm bài|Đạt
10|Cộng giờ bù sự cố; End in|+5 phút: thời gian còn lại tăng 299 giây; End in 2 phút: tự nộp khi hết 2 phút|Đạt
11|Listening: tua lại 1 phút và cộng 1 phút|Vị trí phát từ 75 giây về 16 giây; hộp cộng giờ mở sẵn 1 phút|Đạt
12|B1 Writing Part 2 chọn một câu|“1 of 1 question selected”; gọi thẳng API chọn thêm câu bị từ chối 409|Đạt
13|End now cho một thí sinh|Màn hình kết thúc sau 152 ms, ghi “Ended by invigilator”, không hoàn tác được|Đạt
14|Hết giờ tự nộp|Cả 6 thí sinh tự nộp trong 15 giây sau giờ kết thúc, kể cả thí sinh thêm 25 % giờ (37,5 phút) và máy đã tắt|Đạt
15|Chấm Writing ẩn danh, bảng điểm, CSV|Điểm 6 tự động + 7 Writing = 13/60, Final; CSV đọc đúng tiếng Việt|Đạt
16|Yêu cầu xóa thí sinh, Admin khác duyệt|Chưa xóa khi chờ duyệt; người yêu cầu không có nút duyệt; sau khi Admin thứ hai duyệt thì xóa và ghi nhật ký|Đạt
|end|
Bảng 4.5 liệt kê bốn lỗi mà lần thi thử ngày 26/10 tìm được. Cả bốn đều là lỗi mà kiểm thử tự động trước đó không phát hiện được vì chỉ xuất hiện khi nhiều người dùng thao tác thật trên giao diện.
|table| Các lỗi tìm được trong buổi thi thử ngày 26/10/2026 và cách sửa | 7,2,7.5
Lỗi|Mức|Cách sửa
Thí sinh đăng nhập ở máy khác thì máy cũ vẫn mở bài, chỉ báo mất kết nối, vì trình duyệt coi mã 401 là mất mạng|Cao|Máy cũ về màn hình đăng nhập kèm thông báo lý do
End now đôi khi ghi lý do nộp bài là hết giờ, do giờ kết thúc được làm tròn đến giây gần nhất|Trung bình|Làm tròn xuống
Thông báo đã đọc ở Reading & Writing bật lại khi sang Listening, vì trạng thái đã đọc lưu theo phần thi|Trung bình|Lưu theo thí sinh và kỳ thi
Cột Audio của trang giám thị vẫn chạy sau khi thí sinh đã nộp|Thấp|Để trống khi đã nộp
|end|
Một điểm được ghi nhận nhưng không sửa: khi thí sinh chuyển sang máy mới, thông báo đã đọc ở máy cũ hiện lại một lần, vì trạng thái đã đọc nằm trong trình duyệt.
## Kết quả kiểm thử tải
Kiểm thử tải chạy trên một máy xách tay (16 luồng CPU, 7,6 GiB RAM dành cho Docker) chạy cả hệ thống bằng Docker Desktop; công cụ kiểm thử chạy trên cùng máy nên số liệu đã gồm cả tải của công cụ. Mỗi thí sinh ảo đăng nhập, nhập PIN, bắt đầu phần Reading & Writing A2, gửi heartbeat 5 giây một lần, lưu một câu trả lời mỗi 4 đến 12 giây và giữ kết nối SignalR; một giám thị theo dõi qua SignalR, tải lại trang theo dõi mỗi 3 giây và giữa giờ tạm dừng rồi cho làm tiếp một nhóm thí sinh. Bảng 4.6 là kết quả của ba lần chạy.
|table| Kết quả kiểm thử tải | 5.6,3.6,3.6,3.7
Chỉ số|100 thí sinh, 5 phút (22/10)|200 thí sinh, 3 phút (22/10)|200 thí sinh, 3 phút (07/11)
Vào được bài; còn kết nối lúc kết thúc|100/100; 100/100|200/200; 200/200|200/200; 200/200
Tổng yêu cầu; lỗi máy chủ|10 322; 0|12 681; 0|12 676; 0
Lưu câu trả lời p50 / p95 / p99 (ms)|17 / 27 / 35|15 / 29 / 73|11 / 17 / 55
Heartbeat p50 / p95 / p99 (ms)|118 / 144 / 161|208 / 334 / 546|219 / 265 / 502
Đăng nhập thí sinh p50 / p95 (ms)|65 / 72|65 / 76|59 / 71
Lệnh tạm dừng đến màn hình thí sinh|10/10, 16 ms|20/20, 73 ms|20/20, tối đa 20 ms
Số lần SignalR phải nối lại|0|0|0
|end|
Các lần lưu bị từ chối 409 (3, 6 và 5 lần) đều thuộc những thí sinh đang bị tạm dừng, đúng thiết kế. So với hướng dẫn của ITC rằng độ trễ trên màn hình không nên quá 2 giây [@itc, tr. 15], thao tác thí sinh cảm nhận trực tiếp là lưu câu trả lời có phân vị 99 tối đa 73 ms; heartbeat là yêu cầu chạy nền nên độ trễ khoảng 0,5 giây không ảnh hưởng thao tác. Lệnh của giám thị đến màn hình thí sinh dưới 0,1 giây.
Kiểm thử tải tìm ra hai lỗi cấu hình, đều đã sửa trước khi có số liệu trên. Thứ nhất, máy chủ ngắt kết nối SignalR im lặng quá 12 giây, trong khi thư viện phía trình duyệt mặc định 15 giây mới gửi keep-alive; trang giám thị và màn hình thí sinh bị cắt rồi nối lại liên tục, giám thị chỉ nhận 25 sự kiện trong 5 phút thay vì khoảng 600. Cách sửa là đặt keep-alive của trình duyệt 4 giây, đúng nguyên tắc nhỏ hơn một nửa thời gian chờ của máy chủ (mục 2.3.3), kèm kiểm thử giữ ràng buộc này. Thứ hai, với kho kết nối mặc định 100, ở 200 thí sinh có 1 485 lỗi hết kết nối, vì trình điều khiển MySQL chặn một luồng và giữ một kết nối trong suốt mỗi lệnh; cách sửa là tăng kho kết nối, giới hạn kết nối của MySQL và số luồng tối thiểu (mục 3.8.2).
Phòng máy của trung tâm nhỏ hơn nhiều so với 100 đến 200 máy, nên hệ thống dư tải trên một máy chủ bình thường. Trong lần chạy ngày 22/10, khi làm bài, CPU của máy chủ ứng dụng ổn định ở mức 15 đến 25 %; cao nhất là lúc đăng nhập (băm mật khẩu) và lúc tạo danh sách thí sinh.
## Kết quả rà soát toàn hệ thống
Ngày 02/11, khi các chức năng đã đủ và mọi kiểm thử đều đạt, tôi rà soát toàn hệ thống theo năm mảng để tìm những tình huống mà kiểm thử chưa phủ. Đợt rà soát ghi nhận 42 vấn đề chia bốn nhóm, cộng một nhóm tài liệu chưa khớp mã nguồn (Bảng 4.7). Các vấn đề được sửa theo tám nhóm công việc từ 02/11 đến 07/11, mỗi nhóm có một lần xem xét riêng, sau đó xem xét lại toàn bộ.
|table| Kết quả rà soát ngày 02/11/2026 | 4,1.6,10.9
Nhóm|Số vấn đề|Ví dụ và cách sửa
Mất bài hoặc sai kết quả|13|Đổi đáp án sát lúc bị tạm dừng làm hàng đợi tự lưu đóng hẳn: nay giữ và gửi lại sau khi cho làm tiếp. End now với thí sinh đang ở thời gian cộng thêm làm giờ kết thúc dài ra: nay chỉ rút ngắn. Giám khảo không thấy đề Writing: nay hiện đề của đúng mã đề. Đề trắng xuất bản được với đáp án giữ chỗ: nay bắt buộc đặt đáp án và Admin sửa được đáp án đề đã xuất bản kèm chấm lại.
Luồng bất thường|16|Trạng thái kỳ thi đổi tùy ý: nay chỉ theo các chuyển hợp lệ, có xác nhận hai lần, khóa dòng kỳ thi. Kết thúc ở cấp kỳ thi chưa có: nay có End all và Use test end time. Xung đột tương tranh trả lỗi 500: nay đọc lại, thử lại, trả 409 kèm câu giải thích.
Phân quyền và bảo mật|7|Đổi vai trò không gỡ phân công cũ: nay gỡ và mọi bộ lọc yêu cầu vai trò hiện tại. Thí sinh đọc được đề trước khi bắt đầu: nay đề và tệp chỉ phục vụ khi có lượt làm bài đang chạy. Giám khảo tải được tệp Reading, Listening: nay chỉ tệp Writing.
Triển khai|6|Docker luôn chạy Development với tài khoản demo công khai: nay có bản thi thật qua HTTPS, MySQL chỉ mở ở 127.0.0.1, container không chạy bằng root, thêm chỉ mục cho các truy vấn quét.
Tài liệu|–|Sửa các mô tả trong tài liệu dự án không còn khớp với mã nguồn
|end|
Một số quyết định được đưa ra trong khi sửa: Resume cũng từ chối thí sinh ngoại tuyến như Pause; sau End all, thí sinh chưa bắt đầu phần sau không bắt đầu được cho tới khi giám thị hoàn tác; điểm Writing được lưu thành lịch sử thay vì một dòng duy nhất; khi kỳ thi chuyển sang Active, số máy dạng chữ như “A3” bị từ chối thay vì bị ghi đè.
## Đánh giá
### Đối chiếu với mục tiêu
Bảng 4.8 đối chiếu kết quả với các mục đích cụ thể đã nêu ở phần Mở đầu.
|table| Đối chiếu kết quả với mục đích nghiên cứu | 6,10.5
Mục đích|Kết quả
Tìm hiểu quy trình tổ chức, giám sát buổi thi Cambridge trên máy, viết đặc tả yêu cầu|Bản đặc tả yêu cầu, ghi chép giao diện bài thi mẫu A2 và B1, các quy tắc nghiệp vụ có dẫn nguồn (mục 3.2)
Thiết kế cơ sở dữ liệu và kiến trúc cho nhiều kỳ thi dùng chung bộ máy|28 bảng; loại bài thi và phần thi là dữ liệu; B1 được thêm mà không sửa bộ máy (mục 3.3, 3.4)
Màn hình thí sinh tái hiện bố cục, định dạng câu hỏi của A2 và B1|Sáu kiểu tương tác, đủ các part của năm phần thi trên máy, các tiện ích của thí sinh (mục 4.1.1)
Cổng nhân viên cho quản trị viên, giám thị, giám khảo; giám sát thời gian thực|Lệnh giám thị đến màn hình thí sinh dưới 0,2 giây trong thi thử và dưới 0,1 giây trong kiểm thử tải (mục 4.3, 4.4)
Kiểm thử chức năng, kiểm thử tải, buổi thi thử|1740 kiểm thử tự động đạt; 200 thí sinh không lỗi máy chủ; thi thử 16/16 tình huống
|end|
Yêu cầu phi chức năng về quy mô đặt ra là khoảng 100 thí sinh; hệ thống phục vụ được 200 thí sinh đồng thời. Các yêu cầu về đồng hồ, không mất bài và phát hiện mất kết nối đều được kiểm chứng trong buổi thi thử (tình huống 5, 6, 8, 14).
### Những điểm chưa làm được và hạn chế
- Phạm vi kỳ thi: IELTS đã được chuyển sang đồ án tốt nghiệp hoặc báo cáo thực tập; B2 First, C1 Advanced, C2 Proficiency và phần Speaking nằm ngoài phạm vi.
- Chưa thử trên phòng máy thật của trung tâm: buổi thi thử chạy trên một máy, các thí sinh là trình duyệt tự động; tai nghe, loa, Safe Exam Browser và mạng cục bộ thật chưa được thử. Đây là việc cần làm trước khi dùng hệ thống cho một kỳ thi thật.
- Chưa có đánh giá trải nghiệm của người dùng thật (học viên, giám thị của trung tâm) bằng khảo sát.
- End now nộp bài ngay, nên câu trả lời còn nằm trong khoảng chờ tự lưu (tối đa 3 giây) có thể bị từ chối.
- Độ dài bản ghi (assets.duration_ms) chưa được ghi khi nhập đề, nên giới hạn tua theo độ dài bản ghi chưa có hiệu lực; giới hạn theo phần đã phát vẫn hoạt động.
- Khi thí sinh chuyển sang máy mới, thông báo đã đọc ở máy cũ hiện lại một lần.
- Hệ thống chạy trên một máy chủ, chưa hỗ trợ nhiều máy chủ; máy thí sinh được hỗ trợ là Windows với Chrome, Edge hoặc Safe Exam Browser.
- Bộ chuyển đề phụ thuộc bố cục PDF của Cambridge; đề do trung tâm tự soạn theo bố cục khác cần nhập bằng gói đề hoặc soạn trên web.
Chương này đã trình bày kết quả chức năng và các số liệu kiểm thử của hệ thống. Chương 5 tóm tắt kết luận và hướng phát triển.

# KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN
## Kết luận
Đồ án đã xây dựng được một website tổ chức trọn một buổi thi trên máy cho hai kỳ thi A2 Key for Schools và B1 Preliminary for Schools tại phòng máy của Trung tâm Anh ngữ Victory: chuẩn bị đề, tạo kỳ thi, cấp tài khoản thí sinh, làm bài, giám sát, xử lý sự cố, chấm điểm và xuất kết quả. Hệ thống gồm máy chủ ASP.NET Core trên .NET 10, cơ sở dữ liệu MySQL 28 bảng, giao diện React và kênh thời gian thực SignalR, cài đặt bằng một lệnh Docker Compose ở hai chế độ demo và thi thật.
Kết quả kiểm chứng: 1464 kiểm thử máy chủ, 201 kiểm thử giao diện, 69 kiểm thử bộ chuyển đề và 6 kịch bản đầu-cuối đều đạt; buổi thi thử 16 tình huống đạt 16/16; kiểm thử tải 200 thí sinh đồng thời không có lỗi máy chủ, lưu câu trả lời và lệnh giám thị đều dưới 0,1 giây, thấp hơn nhiều so với ngưỡng 2 giây của ITC.
Những đóng góp chính của đồ án:
- Tái hiện quy trình giám sát của kỳ thi Cambridge trên máy với đồng hồ do máy chủ tính theo các đại lượng tương đối, nhờ đó các thao tác tạm dừng, cộng giờ, sửa giờ kết thúc, kết thúc cả bài thi và tua phần nghe kết hợp với nhau đúng quy tắc.
- Mô hình loại bài thi và phần thi bằng dữ liệu, giúp B1 dùng chung bộ máy với A2.
- Thuật toán trộn từ hai đến sáu đề gốc theo part thành nhiều mã đề và chia mã đề theo vị trí máy sao cho tám máy xung quanh một máy luôn khác mã đề.
- Các cơ chế theo yêu cầu riêng của trung tâm: duyệt chéo hai Admin, mỗi người chỉ thấy phần việc của mình, chia bài Writing ngẫu nhiên và chấm ẩn danh.
- Bộ chuyển đề từ tệp PDF của Cambridge sang gói đề cho năm phần thi, thay cho hệ thống nhập đề cũ của trung tâm.
- Kịch bản thi thử tự động bằng trình duyệt, chạy lại được sau mỗi lần sửa lỗi.
## Hướng phát triển
- Thử hệ thống trên phòng máy thật của trung tâm với tai nghe, Safe Exam Browser và mạng cục bộ, có học viên và giám thị tham gia, kèm khảo sát ý kiến người dùng.
- Mở rộng sang IELTS (các kiểu câu nhiều đáp án, ghi nhãn bản đồ, Writing Task 1 và 2) trong đồ án tốt nghiệp hoặc báo cáo thực tập; sau đó là B2 First, C1 Advanced, C2 Proficiency.
- Ghi độ dài bản ghi khi nhập đề để giới hạn tua theo độ dài; nhận câu trả lời còn trong khoảng chờ tự lưu khi giám thị bấm End now.
- Gộp các lần ghi heartbeat thành một lệnh mỗi 1 đến 2 giây nếu cần phục vụ phòng thi lớn hơn.
- Bổ sung thống kê kết quả theo câu hỏi để trung tâm đánh giá độ khó của đề.

@refs DANH MỤC TÀI LIỆU THAM KHẢO

#! PHỤ LỤC
## Danh sách API
Bảng PL.1 liệt kê các endpoint của máy chủ, nhóm theo chức năng. Cột quyền ghi tác nhân được gọi; phạm vi dữ liệu được lọc thêm theo người tạo và phân công như mô tả ở mục 3.2.1.
|table| Danh sách endpoint của API | 1.9,8.4,6.2
PT|Đường dẫn|Quyền và ghi chú
POST|/api/staff/auth/login, /logout|Đăng nhập, đăng xuất nhân viên
GET|/api/staff/auth/me|Nhân viên: vai trò, mảng
POST|/api/candidate/auth/login, /pin, /logout|Đăng nhập hai bước của thí sinh
GET|/api/candidate/auth/me|Thí sinh
PUT|/api/candidate/auth/preferences|Thí sinh: cỡ chữ, tương phản
GET|/api/candidate/sections|Thí sinh đã nhập PIN
GET|/api/candidate/sections/{id}/paper|Chỉ khi có lượt làm bài đang chạy
POST, GET|/api/candidate/sections/{id}/attempt|Bắt đầu hoặc tiếp tục; đọc đồng hồ, câu trả lời
POST|/api/candidate/sections/{id}/heartbeat|Mỗi 5 giây
POST|/api/candidate/sections/{id}/audio|Bắt đầu phát Listening một lần
PUT|/api/candidate/sections/{id}/responses|Lưu câu trả lời
PUT|/api/candidate/sections/{id}/selections|Chọn câu ở B1 Writing Part 2
GET|/api/assets/{id}|Hình, âm thanh theo quyền của người đọc
GET|/api/site, /api/site/logo|Công khai
PUT, POST|/api/admin/site, /api/admin/site/logo|Mảng trung tâm
GET|/api/admin/papers, /papers/{id}, /papers/sections, /papers/blank-layouts|Mảng đề thi
POST|/api/admin/papers/import, /convert, /blank|Nhập gói đề, nhập từ PDF, đề trắng
POST|/api/admin/papers/{id}/publish|Xuất bản
DELETE|/api/admin/papers/{id}|Yêu cầu xóa (202)
PUT|/api/admin/papers/{id}, /parts/{n}, /parts/{n}/groups/{m}, /questions/{n}|Sửa đề nháp
POST|/api/admin/papers/{id}/parts/{n}/audio, /images, /questions/{n}/options/{key}/image|Tải âm thanh, hình
PUT|/api/admin/papers/{id}/questions/{n}/answers|Admin sửa đáp án đề đã xuất bản
GET, POST|/api/admin/paper-mixes, /paper-mixes/{id}|Xem, tạo bộ đề trộn
DELETE|/api/admin/paper-mixes/{id}|Yêu cầu xóa bộ đề trộn (202)
GET|/api/admin/exam-types, /exam-papers, /exam-paper-mixes, /assignable-staff|Dữ liệu cho biểu mẫu kỳ thi
POST, PUT|/api/admin/exams, /exams/{id}|Mảng kỳ thi: tạo, sửa
POST|/api/admin/exams/{id}/status|Đổi trạng thái kỳ thi
DELETE|/api/admin/exams/{id}|Yêu cầu xóa (202)
PUT|/api/admin/exams/{id}/invigilators, /examiners, /candidate-managers|Phân công
GET, POST|/api/admin/exams/{id}/candidates|Danh sách, thêm thí sinh
PUT, DELETE|/api/admin/candidates/{id}|Sửa; yêu cầu xóa (202)
POST|/api/admin/candidates/{id}/reset-password|Cấp lại mật khẩu
GET, POST, PUT|/api/admin/users, /users/{id}|Admin: tài khoản nhân viên
GET|/api/staff/exams, /exams/{id}|Kỳ thi người dùng được thấy
GET|/api/staff/exams/{id}/room-plan, /login-slips|Sơ đồ phòng, phiếu đăng nhập
GET|/api/staff/exams/{id}/sections/{sid}/monitor|Bảng theo dõi
POST|/api/staff/exams/{id}/sections/{sid}/actions|Pause, Resume, cộng giờ, End now/at/in, Use test end time, tua audio
POST|/api/staff/exams/{id}/end-all, /use-test-end-time|Kết thúc cả bài thi và hoàn tác
POST|/api/staff/exams/{id}/sections/{sid}/announce-start|Báo bắt đầu
GET, POST|/api/staff/exams/{id}/sections/{sid}/warnings, /warnings/{wid}/resolve|Cảnh báo
GET, POST|/api/staff/exams/{id}/messages|Thông báo cho thí sinh
GET|/api/staff/exams/{id}/results|Admin: bảng điểm
GET|/api/staff/marking, /api/staff/attempts/{id}/marking|Giám khảo: bài được chia, bài cần chấm
POST|/api/staff/attempts/{id}/writing-marks|Giám khảo: lưu điểm Writing
GET, POST|/api/staff/approvals, /approvals/{id}/approve, /reject, /cancel|Duyệt chéo
WS|/hubs/staff, /hubs/candidate|Hub SignalR
GET|/health|Kiểm tra máy chủ và cơ sở dữ liệu
|end|
## Tóm tắt cài đặt
Hướng dẫn đầy đủ nằm trong tệp setup/INSTALL.md của kho mã nguồn, gồm cài đặt cho phòng thi qua mạng cục bộ, cấu hình tường lửa, sao lưu và khôi phục cơ sở dữ liệu, xử lý sự cố. Các bước chính:
- Cài Docker Desktop (bật WSL 2) và Git trên máy chủ: Windows 10/11 64 bit hoặc Linux, CPU 4 nhân, RAM 8 GB, trống 20 GB ổ đĩa.
- Tải mã nguồn, sao tệp docker/.env.example thành docker/.env và đổi mọi mật khẩu trong đó.
- Bản demo: chạy lệnh docker compose -f docker/docker-compose.yml --env-file docker/.env up -d --build, rồi mở http://localhost:8080 (thí sinh) và http://localhost:8080/staff (nhân viên).
- Bản thi thật: đặt tài khoản Admin đầu tiên trong docker/.env, tạo chứng chỉ cho địa chỉ IP của máy chủ vào thư mục docker/certs, rồi chạy docker compose -p examprep-prod -f docker/docker-compose.yml -f docker/docker-compose.prod.yml --env-file docker/.env up -d --build; máy thí sinh mở địa chỉ https của máy chủ.
- Kiểm tra bằng trang /health; trả về Healthy là máy chủ và cơ sở dữ liệu đều chạy.
Bản demo tạo sẵn hai kỳ thi demo và các tài khoản trong Bảng PL.2. Các mật khẩu này công khai trong tệp README của kho mã nguồn, chỉ dùng cho bản demo, không bao giờ dùng cho kỳ thi thật.
|table| Tài khoản của bản demo | 4.4,2.4,3.3,6.4
Vai trò|Địa chỉ|Tên đăng nhập|Mật khẩu
Admin|/staff|hoanglong|19072005
Admin thứ hai|/staff|admin2|19072005
Quản trị viên (bốn mảng)|/staff|quantri|19072005
Giám khảo|/staff|giamkhao|19072005
Giám thị|/staff|giamthi|victory-invig
Thí sinh A2|/|hoanglong|19072005, mã PIN 4821
Thí sinh B1|/|hoanglongb1|19072005, mã PIN 4821
|end|
