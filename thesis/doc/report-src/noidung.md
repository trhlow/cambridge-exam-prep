#! LỜI CAM ĐOAN
Tôi xin cam đoan báo cáo đồ án chuyên ngành “Xây dựng website hỗ trợ thi chứng chỉ tiếng Anh Cambridge” là công trình do tôi thực hiện dưới sự hướng dẫn của ThS. Nhan Minh Phúc. Mã nguồn, sơ đồ thiết kế và số liệu kiểm thử trình bày trong báo cáo do tôi tự xây dựng và đo đạc. Những nội dung tham khảo từ tài liệu và trang web của các tổ chức, tác giả khác đều được trích dẫn và ghi trong danh mục tài liệu tham khảo.
Kho mã nguồn của đồ án không chứa nội dung đề thi của Cambridge hay của Trung tâm Anh ngữ Victory. Các đề mẫu dùng để kiểm thử và minh họa trong báo cáo đều do tôi tự soạn.
Nếu phát hiện có sự gian lận, tôi xin chịu hoàn toàn trách nhiệm trước Khoa và Nhà trường.
@sign Vĩnh Long, ngày … tháng … năm 2026|Sinh viên thực hiện|(Ký và ghi rõ họ tên)||Trần Hoàng Long

#! LỜI CẢM ƠN
Tôi xin chân thành cảm ơn ThS. Nhan Minh Phúc, giảng viên hướng dẫn, đã định hướng đề tài, góp ý cho tôi qua từng buổi báo cáo tiến độ và giúp tôi điều chỉnh phạm vi đồ án cho phù hợp với nhu cầu thực tế.
Tôi xin cảm ơn quý thầy cô Khoa Công nghệ Thông tin, Trường Kỹ thuật và Công nghệ đã truyền đạt cho tôi những kiến thức nền tảng trong suốt quá trình học tập, nhờ đó tôi thực hiện được đồ án này.
Tôi cũng xin cảm ơn Trung tâm Anh ngữ Victory đã đặt ra bài toán thực tế, cung cấp yêu cầu nghiệp vụ và góp ý cho từng phiên bản của hệ thống.
Cuối cùng, tôi xin cảm ơn gia đình và bạn bè đã luôn động viên, hỗ trợ tôi trong thời gian thực hiện đồ án.
Dù đã cố gắng, báo cáo khó tránh khỏi thiếu sót. Tôi rất mong nhận được ý kiến đóng góp của quý thầy cô để báo cáo được hoàn thiện hơn.
@sign Vĩnh Long, ngày … tháng … năm 2026|Sinh viên thực hiện|(Ký và ghi rõ họ tên)||Trần Hoàng Long

@nhanxet NHẬN XÉT CỦA GIẢNG VIÊN HƯỚNG DẪN|Giảng viên hướng dẫn
@nhanxet NHẬN XÉT CỦA GIẢNG VIÊN CHẤM ĐỒ ÁN|Giảng viên chấm

@toc MỤC LỤC

#! DANH MỤC CHỮ VIẾT TẮT
@abbrev
API|Application Programming Interface – giao diện lập trình ứng dụng
CBT|Computer-Based Testing – thi trên máy tính
CEFR|Common European Framework of Reference for Languages – Khung tham chiếu ngôn ngữ chung châu Âu
CSP|Content Security Policy – chính sách bảo mật nội dung
CSV|Comma-Separated Values – tệp dữ liệu phân cách bằng dấu phẩy
EF Core|Entity Framework Core
HTML|HyperText Markup Language
HTTP|Hypertext Transfer Protocol
HTTPS|HTTP Secure – HTTP qua kênh mã hóa TLS
ITC|International Test Commission – Ủy ban Khảo thí Quốc tế
JSON|JavaScript Object Notation
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
Từ A2 Key đến C2 Proficiency, các kỳ thi Cambridge English Qualifications đều đã có hình thức thi trên máy tính. Thí sinh làm các phần Reading, Writing và Listening trên máy, còn giám thị theo dõi cả phòng qua một cổng quản lý. Trung tâm Anh ngữ Victory cần một hệ thống tổ chức thi thử ngay tại phòng máy của trung tâm, bằng đề do trung tâm cung cấp, để học viên làm quen với giao diện thi máy và để giám thị tập các thao tác xử lý sự cố.
Đồ án xây dựng một website đáp ứng nhu cầu này cho hai kỳ thi A2 Key for Schools và B1 Preliminary for Schools. Hướng tiếp cận được chọn là tái hiện bố cục, định dạng câu hỏi và quy trình giám sát của nền tảng thi máy mà Cambridge đang dùng. Căn cứ để tái hiện là tài liệu ngày thi, tài liệu tập huấn giám thị và các bài thi mẫu chính thức. Thiết kế còn được đối chiếu với bộ hướng dẫn về thi trên máy tính của Ủy ban Khảo thí Quốc tế (ITC).
Hệ thống gồm máy chủ ASP.NET Core trên .NET 10, cơ sở dữ liệu MySQL truy cập qua Entity Framework Core, giao diện React viết bằng TypeScript và kênh thời gian thực SignalR. Đồng hồ làm bài do máy chủ tính, vì vậy thí sinh mất mạng hay tắt máy vẫn được nộp bài đúng giờ. Giám thị có thể tạm dừng, cộng giờ, sửa giờ kết thúc, tua lại phần nghe và gửi thông báo, và các lệnh này đến màn hình thí sinh gần như tức thì. Hệ thống còn chấm tự động phần Reading và Listening, chia bài Writing ngẫu nhiên cho giám khảo chấm ẩn danh, nhập đề từ tệp PDF và cho phép soạn đề trên web. Từ hai đến sáu đề gốc có thể được trộn thành nhiều mã đề để chia theo vị trí máy. Về bảo mật, hệ thống phân quyền cho năm vai trò, yêu cầu hai quản trị viên duyệt chéo các thao tác nhạy cảm và kiểm tra cấu hình Safe Exam Browser.
Hệ thống đã chạy được trọn một buổi thi A2 và B1. Toàn bộ 1500 kiểm thử tự động phía máy chủ, 206 kiểm thử giao diện, 69 kiểm thử công cụ chuyển đề và 6 kịch bản kiểm thử đầu-cuối đều đạt. Với 200 thí sinh làm bài đồng thời, kiểm thử tải không ghi nhận lỗi máy chủ nào, còn thời gian lưu câu trả lời và thời gian lệnh của giám thị đến màn hình thí sinh đều dưới 0,2 giây. Buổi thi thử, trong đó các trình duyệt tự động đóng vai người dùng, đạt 16/16 tình huống sau khi sửa bốn lỗi phát hiện được. Kết quả này được giữ nguyên khi chạy lại sau đợt rà soát toàn hệ thống, trong đó 42 vấn đề đã được sửa, và lần chạy cuối với 17 tình huống đạt 17/17.

#! MỞ ĐẦU
## Lý do chọn đề tài
Cambridge English Qualifications là hệ thống chứng chỉ tiếng Anh được xây dựng theo từng bậc của Khung tham chiếu ngôn ngữ chung châu Âu (CEFR) [@a2hb]. Bên cạnh hình thức thi trên giấy, Cambridge đã đưa các kỳ thi từ A2 Key đến C2 Proficiency lên máy tính. Thí sinh làm phần Listening, Reading và Writing trên máy, riêng phần Speaking vẫn thi trực tiếp với giám khảo [@digital]. Hai kỳ thi dành cho học sinh là A2 Key for Schools và B1 Preliminary for Schools cũng có thể thi trên giấy hoặc trên máy [@a2hb], [@b1hb].
Thi trên máy đòi hỏi thí sinh phải làm quen với thiết bị và phần mềm. Bộ hướng dẫn về thi trên máy tính của Ủy ban Khảo thí Quốc tế (ITC) khuyến nghị đơn vị tổ chức tạo đủ cơ hội để thí sinh làm quen với phần mềm và thiết bị thi trước khi làm bài [@itc, tr. 20]. Khâu vận hành của giám thị cũng không đơn giản. Khi có sự cố, giám thị phải tạm dừng đúng thí sinh, cộng giờ bù và sửa giờ kết thúc; riêng với phần Listening, tua lại âm thanh và cộng giờ là hai thao tác riêng biệt [@invig, tr. 20]. Trong khi đó, buổi tập huấn giám thị chính thức chỉ dành cho mỗi phần thi 20 phút và phải dùng thiết bị riêng, không được cài lên máy dùng cho kỳ thi thật [@invig, tr. 3–4].
Trung tâm Anh ngữ Victory luyện thi Cambridge cho học sinh và tổ chức thi tại phòng máy của trung tâm. Trung tâm cần một công cụ để học viên thi thử trên giao diện giống kỳ thi thật, để giám thị tập thao tác và để trung tâm tự tổ chức kỳ thi thử bằng đề của mình. Hệ thống nhập đề cũ của trung tâm lại không còn sử dụng được. Từ những lý do đó, tôi chọn đề tài “Xây dựng website hỗ trợ thi chứng chỉ tiếng Anh Cambridge”.
## Mục đích nghiên cứu
Mục đích chung của đồ án là xây dựng một website chạy trong phòng máy của trung tâm và tổ chức được trọn một buổi thi trên máy, từ khâu chuẩn bị đề, tạo kỳ thi, cấp tài khoản cho thí sinh, làm bài, giám sát và xử lý sự cố cho đến chấm điểm và xuất kết quả. Mục đích chung này được cụ thể hóa thành năm mục đích sau.
- Tìm hiểu quy trình tổ chức và giám sát một buổi thi Cambridge trên máy, viết thành bản đặc tả yêu cầu.
- Thiết kế cơ sở dữ liệu và kiến trúc hệ thống cho nhiều kỳ thi dùng chung một bộ máy, trong đó cấu trúc phần thi được mô tả bằng dữ liệu.
- Xây dựng màn hình thí sinh tái hiện bố cục và định dạng câu hỏi của bài thi máy A2 Key for Schools và B1 Preliminary for Schools.
- Xây dựng cổng nhân viên cho quản trị viên, giám thị và giám khảo, trong đó giám thị theo dõi phòng thi theo thời gian thực.
- Kiểm thử chức năng, kiểm thử tải và tổ chức một buổi thi thử để đánh giá hệ thống.
## Đối tượng và phạm vi nghiên cứu
Đối tượng nghiên cứu của đồ án là quy trình tổ chức, giám sát và làm bài của kỳ thi Cambridge English Qualifications trên máy tính, các nguyên tắc về thi trên máy tính và các công nghệ xây dựng ứng dụng web thời gian thực, gồm ASP.NET Core, SignalR, Entity Framework Core, MySQL, React và Safe Exam Browser.
Về phạm vi, đồ án tập trung vào hai kỳ thi A2 Key for Schools và B1 Preliminary for Schools với các phần thi làm trên máy là Reading, Writing và Listening. Phần Speaking thi trực tiếp nên không thuộc phạm vi. Hệ thống chạy trên một máy chủ trong mạng cục bộ của trung tâm, còn máy thí sinh chạy Windows. Nội dung đề thi do trung tâm cung cấp và chỉ được lưu trên máy chủ của trung tâm; hệ thống không kết nối với hệ thống của Cambridge. Các kỳ thi B2 First, C1 Advanced, C2 Proficiency và IELTS nằm ngoài phạm vi đồ án.
So với đề cương chi tiết, phạm vi kỳ thi đã được điều chỉnh từ các cấp B2–C2 sang A2 và B1 theo nhu cầu của Trung tâm Anh ngữ Victory, đơn vị sử dụng hệ thống.
## Phương pháp nghiên cứu
Đồ án kết hợp bốn phương pháp. Trước hết là nghiên cứu tài liệu, gồm tài liệu ngày thi và tài liệu tập huấn giám thị của Cambridge, cẩm nang giáo viên của hai kỳ thi, bộ hướng dẫn của ITC và tài liệu chính thức của từng công nghệ. Phương pháp thứ hai là phân tích và thiết kế bằng UML [@uml] với sơ đồ use case, sơ đồ thực thể – liên kết, sơ đồ trạng thái và sơ đồ tuần tự. Tiếp theo, hệ thống được phát triển lặp theo Scrum [@scrum]; mỗi sprint kéo dài một tuần và kết thúc bằng một phần chạy được cùng một báo cáo tiến độ. Cuối cùng, phương pháp thực nghiệm được dùng để đánh giá hệ thống qua kiểm thử tự động ở nhiều mức, kiểm thử tải và một buổi thi thử theo kịch bản 16 tình huống.
## Bố cục báo cáo
Ngoài phần Mở đầu, báo cáo gồm năm chương. Chương 1 giới thiệu bài toán, kỳ thi Cambridge trên máy và các yêu cầu đặt ra cho hệ thống. Chương 2 trình bày cơ sở lý thuyết về thi trên máy tính và các công nghệ được sử dụng. Chương 3 mô tả quá trình phân tích, thiết kế và hiện thực hệ thống. Chương 4 trình bày kết quả đạt được, những khó khăn kỹ thuật cùng cách khắc phục, và đánh giá hệ thống so với mục tiêu. Chương 5 nêu kết luận và hướng phát triển.

# TỔNG QUAN
Chương này giới thiệu bài toán mà Trung tâm Anh ngữ Victory đặt ra. Nội dung đi từ cấu trúc hai kỳ thi A2 Key for Schools và B1 Preliminary for Schools, quy trình tổ chức một buổi thi Cambridge trên máy, đến các giải pháp hiện có. Từ đó, chương rút ra mục tiêu, yêu cầu và kế hoạch thực hiện của đồ án.
## Giới thiệu bài toán
### Bối cảnh
Trung tâm Anh ngữ Victory (sau đây gọi là trung tâm) luyện thi các chứng chỉ Cambridge cho học sinh và có phòng máy riêng để tổ chức thi. Khi các kỳ thi Cambridge chuyển dần sang hình thức thi trên máy [@digital], trung tâm có ba nhu cầu mới. Nhu cầu đầu tiên đến từ học viên, những người cần làm quen trước với màn hình thi máy, chẳng hạn cách chuyển câu, kéo thả đáp án, ghi chú, đổi cỡ chữ hay nghe bản ghi chỉ một lần và không dừng giữa chừng. Nhu cầu thứ hai thuộc về giám thị, những người cần tập các thao tác xử lý sự cố trên một hệ thống có hành vi giống hệ thống thật. Cuối cùng, trung tâm muốn tự tổ chức các kỳ thi thử bằng đề của mình, tự chấm điểm và xuất kết quả mà không phụ thuộc vào hệ thống bên ngoài.
### Phát biểu bài toán
Bài toán đặt ra là xây dựng một website chạy trên máy chủ đặt tại trung tâm và được truy cập từ các máy trong phòng thi qua mạng cục bộ. Website cần đáp ứng năm nhóm yêu cầu chính sau đây.
- Quản lý đề thi, gồm nhập đề từ tệp PDF và tệp âm thanh do trung tâm cung cấp, soạn và sửa đề trên web, xuất bản đề sau khi kiểm tra.
- Tổ chức kỳ thi, gồm tạo kỳ thi với các phần thi đúng cấu trúc của từng loại chứng chỉ, nhập danh sách thí sinh, in phiếu đăng nhập, phân công giám thị và giám khảo.
- Làm bài. Thí sinh đăng nhập bằng phiếu đăng nhập và mã PIN, rồi làm lần lượt từng phần thi trên giao diện giống bài thi máy của Cambridge, với đồng hồ do máy chủ tính và bài được nộp tự động khi hết giờ.
- Giám sát. Giám thị theo dõi cả phòng theo thời gian thực và có thể tạm dừng, cộng giờ, sửa giờ kết thúc, tua lại phần nghe, gửi thông báo.
- Chấm điểm, gồm chấm tự động phần có đáp án, để giám khảo chấm phần Writing và xuất bảng điểm.
## Kỳ thi Cambridge English Qualifications
### Khung tham chiếu CEFR
Khung tham chiếu ngôn ngữ chung châu Âu (CEFR) do Hội đồng châu Âu công bố năm 2001 mô tả năng lực ngôn ngữ theo sáu bậc A1, A2, B1, B2, C1, C2, chia thành ba nhóm là người dùng cơ bản (A), người dùng độc lập (B) và người dùng thành thạo (C) [@cefr2001]. Năm 2020, Hội đồng châu Âu phát hành tập bổ sung (Companion Volume), cập nhật các bộ mô tả năng lực, bổ sung bậc Pre-A1 và các thang đo mới như hòa giải (mediation) và tương tác trực tuyến [@cefr2020].
Mỗi chứng chỉ Cambridge English Qualifications nhắm vào một bậc CEFR [@a2hb, tr. 2]. Kết quả được báo bằng điểm trên thang Cambridge English Scale cho từng kỹ năng, điểm tổng là trung bình của bốn kỹ năng, và từ điểm tổng suy ra xếp loại cùng bậc CEFR [@a2hb, tr. 3].
### A2 Key for Schools
A2 Key for Schools là chứng chỉ bậc A2 dành cho học sinh. Chứng chỉ này xác nhận thí sinh hiểu và dùng được các cụm từ cơ bản, hiểu văn bản viết đơn giản và giao tiếp được ở mức cơ bản; kỳ thi có thể làm trên giấy hoặc trên máy [@a2hb, tr. 3]. Bảng 1.1 trình bày cấu trúc của kỳ thi.
|table| Cấu trúc kỳ thi A2 Key for Schools (nguồn: [@a2hb, tr. 5]) | 4.5,3.5,3.5,4.5
Bài thi|Thời gian|Số phần|Số câu hỏi
Reading and Writing|60 phút|7|32
Listening|khoảng 30 phút|5|25
Speaking|8–10 phút|2|–
|end|
Theo bảng trên, bài Reading and Writing dài nhất và gộp hai kỹ năng vào một bài thi. Bài này gồm năm phần đọc, với các dạng chọn một trong ba phương án, nối và điền một từ vào chỗ trống, cùng hai phần viết là viết email hoặc ghi chú ít nhất 25 từ và viết câu chuyện ít nhất 35 từ dựa trên ba tranh [@a2hb, tr. 7]. Bài Listening gồm năm phần, trong đó thí sinh chọn tranh đúng, điền thông tin vào ghi chú, trả lời câu hỏi trắc nghiệm và nối thông tin [@a2hb, tr. 24]. Về kết quả, thí sinh đạt 140–150 điểm nhận chứng chỉ ghi năng lực bậc B1, từ 120 đến 139 điểm nhận chứng chỉ bậc A2, còn từ 100 đến 119 điểm nhận chứng chỉ bậc A1 [@a2hb, tr. 5].
### B1 Preliminary for Schools
B1 Preliminary for Schools là chứng chỉ bậc B1, nối tiếp A2 Key. Chứng chỉ xác nhận thí sinh đã nắm vững kiến thức cơ bản và có kỹ năng tiếng Anh thực tế cho sinh hoạt hằng ngày; kỳ thi cũng có thể làm trên giấy hoặc trên máy [@b1hb]. Khác với A2, Reading và Writing ở B1 là hai bài thi riêng, như thể hiện trong Bảng 1.2.
|table| Cấu trúc kỳ thi B1 Preliminary for Schools (nguồn: [@b1hb]) | 4.5,3.5,3.5,4.5
Bài thi|Thời gian|Số phần|Số câu hỏi
Reading|45 phút|6|32
Writing|45 phút|2|–
Listening|khoảng 30 phút|4|25
Speaking|12–17 phút|4|–
|end|
So với A2, bài Reading của B1 có thêm hai dạng câu hỏi. Ở dạng thứ nhất, thí sinh nối mô tả của năm người với tám đoạn văn ngắn; ở dạng thứ hai, thí sinh điền năm câu bị lược bỏ vào một bài đọc dài [@b1hb]. Bài Writing gồm hai phần, mỗi phần khoảng 100 từ. Phần 1 yêu cầu trả lời một email, còn ở phần 2 thí sinh chọn viết một bài báo hoặc một câu chuyện [@b1hb]. Bài Listening gồm bốn phần với 25 câu hỏi, trong đó phần 3 là điền vào chỗ trống [@b1hb]. Những khác biệt này ảnh hưởng trực tiếp đến thiết kế, vì hệ thống phải hỗ trợ nhiều kiểu tương tác câu hỏi hơn và phải cho thí sinh chọn một trong hai câu ở phần Writing.
### Hình thức thi trên máy
Theo Cambridge, bài thi trên máy kiểm tra cùng kiến thức và kỹ năng như bài thi trên giấy [@digital]. Ở phần Listening, thí sinh dùng tai nghe có chỉnh âm lượng. Phần Writing có bộ đếm số từ tự động, còn ở phần Reading thí sinh có thể ghi chú, tô sáng văn bản và xem lại câu trả lời. Nội dung bài thi được xáo trộn để tăng tính bảo mật [@digital].
Bài thi máy của Cambridge chạy trên nền tảng Inspera. Trước giờ thi, nhân viên trung tâm mở trình duyệt khóa (lockdown browser) Inspera Integrity Browser trên mọi máy thí sinh và đưa về trang đăng nhập, còn giám thị theo dõi thí sinh qua Inspera Admin Portal [@booklet, tr. 3, 5]. Máy thi phải đáp ứng cấu hình tối thiểu do Cambridge công bố, trong đó có yêu cầu cài bản cập nhật hệ điều hành và tắt cập nhật tự động trong ngày thi [@inspera_specs].
## Quy trình tổ chức một buổi thi trên máy
### Trước và khi bắt đầu giờ thi
Tài liệu ngày thi của Cambridge mô tả các bước thí sinh vào bài. Thí sinh kiểm tra tên đăng nhập, vốn bắt đầu bằng các chữ cái của họ, rồi đăng nhập bằng tên đăng nhập và mật khẩu. Tiếp theo, thí sinh nhập mã PIN do giám thị cung cấp và chọn *Unlock and Select* (mở khóa và chọn bài) để mở bài, sau đó kiểm tra lại họ tên hiển thị trên màn hình [@booklet, tr. 11]. Trong lúc đó, giám thị theo dõi cổng quản lý và tạm dừng những thí sinh bắt đầu làm bài sớm hơn hiệu lệnh [@booklet, tr. 12]. Hình 1.1 tóm tắt quy trình của một buổi thi trên máy.
!fig ../../diagrams/report/exam-day.drawio.png | Quy trình một buổi thi trên máy (tổng hợp từ [@booklet] và [@invig]) | 16
### Các thao tác của giám thị trong giờ thi
Tài liệu tập huấn giám thị mô tả các thao tác trên cổng quản lý [@invig]. Thao tác thường gặp nhất là tạm dừng và cho làm tiếp. Giám thị chọn một hoặc nhiều thí sinh, chọn *Pause* (tạm dừng) và ghi lý do, khi đó màn hình thí sinh hiện thông báo bài thi đang tạm dừng [@invig, tr. 13]. Để theo dõi thời gian, giám thị có thể thêm vào màn hình theo dõi các cột thời gian còn lại, thời gian bù sự cố, thời gian cộng thêm và thời điểm bắt đầu [@invig, tr. 11]. Giờ kết thúc được sửa bằng chức năng *Edit End Time*, trong đó lựa chọn *End now* kết thúc bài làm của thí sinh ngay lập tức [@invig, tr. 18].
Hai tình huống sự cố được hướng dẫn riêng. Khi có sự cố trong phần Listening, giám thị phải tua âm thanh lại một số phút và cộng đúng số phút đó vào thời gian bù sự cố, và đây là hai thao tác tách biệt [@invig, tr. 20]. Khi mất điện, thí sinh chuyển sang trạng thái ngoại tuyến và giám thị không tạm dừng được. Thí sinh có thể đăng nhập lại trên máy khác hoặc trên chính máy đó khi có điện, sau đó giám thị cộng thời gian bù cho số phút bị gián đoạn [@invig, tr. 15]. Thời gian đi vệ sinh không được cộng thêm giờ [@booklet, tr. 13].
### Những khó khăn trong vận hành
Quy trình trên có một số điểm dễ sai sót khi vận hành. Lệnh kết thúc bài không hoàn tác được, nên một lần bấm nhầm ảnh hưởng trực tiếp đến thí sinh. Thí sinh mất kết nối vẫn bị tính giờ, vì thế giám thị phải theo dõi sát và cộng giờ bù chính xác. Phần Listening lại đòi hỏi hai thao tác phối hợp với nhau. Buổi tập huấn chính thức giúp giám thị làm quen, nhưng mỗi phần thi chỉ kéo dài 20 phút và phải chuẩn bị ít nhất hai thiết bị riêng cho vai thí sinh và vai giám thị [@invig, tr. 3]. Một hệ thống mô phỏng chạy ngay tại phòng máy của trung tâm sẽ cho giám thị luyện tập nhiều lần với số thí sinh thật.
## Các giải pháp liên quan
### Nền tảng Inspera
Inspera là nền tảng Cambridge dùng để tổ chức các kỳ thi trên máy [@booklet]. Nhân viên trung tâm thi truy cập cổng quản lý Inspera bằng tài khoản MyCambridge của Cambridge [@booklet, tr. 5], [@invig, tr. 3]. Nền tảng này phục vụ kỳ thi chính thức với đề của Cambridge, không phải công cụ để trung tâm tổ chức thi thử bằng đề tự soạn. Môi trường tập huấn giám thị là một bản riêng (demo tenant), phải đăng ký qua biểu mẫu và không được cài lên máy dùng cho kỳ thi thật [@invig, tr. 3–4].
### Safe Exam Browser kết hợp hệ thống quản lý học tập
Safe Exam Browser (SEB) là trình duyệt khóa mã nguồn mở do ETH Zurich phát triển. Trong thời gian làm bài, SEB biến máy tính thành máy thi an toàn bằng cách chặn chuyển ứng dụng, chụp màn hình và các phím tắt hệ thống [@seb]. SEB làm việc được với mọi hệ thống quản lý học tập dạng web và có sẵn kết nối với Moodle, ILIAS, OpenOlat [@seb]. Cách kết hợp này phù hợp với bài kiểm tra trắc nghiệm thông thường. Tuy nhiên, nó không tái hiện được giao diện và quy trình giám sát đặc thù của kỳ thi Cambridge, chẳng hạn tạm dừng từng thí sinh, sửa giờ kết thúc hay tua lại phần nghe.
### Nhận xét
Cả hai hướng trên đều chưa đáp ứng đủ nhu cầu của trung tâm. Hướng thứ nhất giống kỳ thi thật nhưng không dùng được đề riêng, còn hướng thứ hai dùng được đề riêng nhưng không giống kỳ thi thật. Vì vậy, đề tài chọn hướng tự xây dựng hệ thống, tái hiện giao diện và quy trình của kỳ thi máy Cambridge, đồng thời dùng SEB làm trình duyệt khóa ở phía thí sinh.
## Mục tiêu và yêu cầu của hệ thống
### Tác nhân
Theo yêu cầu của trung tâm, hệ thống có năm tác nhân là người và một tác nhân hệ thống. Vai trò của từng tác nhân được tóm tắt trong Bảng 1.3.
|table| Các tác nhân của hệ thống | 4,12.5
Tác nhân|Vai trò
Thí sinh|Đăng nhập bằng phiếu đăng nhập và mã PIN, làm lần lượt các phần thi, ghi chú, tô sáng, đổi cỡ chữ và độ tương phản.
Giám thị|Theo dõi phòng thi theo thời gian thực; tạm dừng, cho làm tiếp, cộng giờ sự cố, sửa giờ kết thúc, tua lại phần nghe, gửi thông báo; xử lý cảnh báo.
Giám khảo|Chấm phần Writing của các bài được chia ngẫu nhiên cho mình; chỉ thấy mã bài, không thấy tên thí sinh và bảng điểm.
Quản trị viên theo mảng|Quản lý đề thi, kỳ thi, thí sinh hoặc thông tin trung tâm trong phạm vi mảng được giao; chỉ thấy đề, kỳ thi do mình tạo hoặc được giao.
Quản trị viên (Admin)|Toàn quyền; quản lý tài khoản nhân viên; duyệt các yêu cầu xóa dữ liệu và thay đổi tài khoản; xem bảng điểm; sửa đáp án của đề đã xuất bản.
Hệ thống|Tự nộp bài khi hết giờ, chấm tự động, ghi nhận mất kết nối và các cảnh báo.
|end|
Vai trò quản trị viên theo mảng xuất phát từ yêu cầu của trung tâm ngày 16/10. Khi đó, việc quản lý được chia thành bốn mảng là đề thi, kỳ thi, thí sinh và thông tin trung tâm, và mỗi mảng có thể giao cho một người khác nhau. Từ đây trở đi, báo cáo gọi người có toàn quyền là quản trị viên, còn người chỉ phụ trách một số mảng là quản trị viên theo mảng.
### Yêu cầu phi chức năng
Ngoài các chức năng đã nêu ở mục 1.1.2, hệ thống cần thỏa một số yêu cầu phi chức năng. Phần lớn các yêu cầu này xuất phát từ bộ hướng dẫn của ITC được trình bày ở Chương 2.
- Đồng hồ làm bài do máy chủ tính, không phụ thuộc đồng hồ của máy thí sinh, và mất kết nối không làm mất câu trả lời đã chọn [@itc, tr. 16].
- Thao tác trên màn hình thí sinh có tác dụng ngay; theo ITC, độ trễ trên màn hình không nên vượt quá 2 giây [@itc, tr. 15].
- Lệnh của giám thị đến màn hình thí sinh trong vài giây, và trang của giám thị phát hiện được thí sinh mất kết nối.
- Một máy chủ phục vụ được khoảng 100 thí sinh làm bài đồng thời.
- Đề thi và dữ liệu thí sinh được bảo vệ bằng phân quyền theo vai trò, và mọi thao tác xóa hay thay đổi tài khoản phải được một quản trị viên khác duyệt.
## Phương pháp và kế hoạch thực hiện
Đồ án được thực hiện theo khung Scrum [@scrum], có điều chỉnh cho một người thực hiện. Danh sách công việc (product backlog) gồm các epic và user story, được quản lý trên Jira [@jira]. Mỗi sprint kéo dài một tuần, và cuối mỗi sprint có báo cáo tiến độ gửi giảng viên hướng dẫn. Mã nguồn được quản lý bằng Git [@git] trên GitHub, với ít nhất một lần commit mỗi tuần theo quy định của Khoa. Bảng 1.4 trình bày kế hoạch các sprint.
|table| Kế hoạch các sprint | 2.2,3.6,10.7
Sprint|Thời gian|Mục tiêu
0|10/10–11/10|Khởi động: cơ sở dữ liệu, khảo sát giao diện
1|12/10–18/10|Màn hình thí sinh A2 (demo 16/10); cổng nhân viên; chấm Writing; nhập đề từ PDF
2|19/10–25/10|Phân quyền nhiều tác nhân, duyệt chéo giữa hai quản trị viên; đặc tả yêu cầu, sơ đồ, đặc tả API
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
Trên thực tế, công việc chạy nhanh hơn kế hoạch. Các chức năng của hai kỳ thi, kiểm thử tải và buổi thi thử đã xong trước ngày 27/10. Các yêu cầu mới mà trung tâm đưa ra ngày 29/10 được làm ngay trong tuần đó, và báo cáo được viết từ đầu tháng 11. Tiến độ thực tế được trình bày ở mục 3.1.
## Tiểu kết chương
Chương 1 đã xác định bài toán của trung tâm là tổ chức thi thử trên máy với giao diện và quy trình giám sát giống kỳ thi Cambridge thật, nhưng dùng đề của chính trung tâm. Qua phân tích cấu trúc hai kỳ thi và quy trình của giám thị, chương cũng chỉ ra những điểm dễ sai sót khi vận hành, như lệnh kết thúc không hoàn tác được hay hai thao tác phối hợp ở phần Listening. Các giải pháp hiện có chưa đáp ứng đủ nhu cầu này, nên đồ án chọn hướng tự xây dựng hệ thống. Chương 2 trình bày cơ sở lý thuyết và công nghệ làm nền cho các yêu cầu đã nêu.

# CƠ SỞ LÝ THUYẾT
Chương này trình bày các cơ sở lý thuyết được vận dụng trong đồ án. Phần đầu tóm tắt các nguyên tắc về thi trên máy tính theo bộ hướng dẫn của ITC, vì đây là căn cứ cho phần lớn yêu cầu phi chức năng. Các phần tiếp theo lần lượt trình bày kiến trúc ứng dụng web, giao tiếp thời gian thực, nền tảng phía máy chủ, bảo mật, kiểm thử, đóng gói triển khai và quy trình phát triển.
## Thi trên máy tính
### Khái niệm và các chế độ tổ chức thi
Thi trên máy tính (computer-based testing, CBT) là hình thức thí sinh làm bài và nộp bài trên máy tính thay vì trên giấy. Ủy ban Khảo thí Quốc tế (ITC) phân biệt bốn chế độ tổ chức thi theo mức độ giám sát, được tóm tắt trong Bảng 2.1 [@itc, tr. 10].
|table| Các chế độ tổ chức thi theo ITC (nguồn: [@itc, tr. 10]) | 3.5,13
Chế độ|Mô tả
Mở (open)|Không có người giám sát, không xác thực được danh tính thí sinh, ví dụ bài kiểm tra trên Internet không cần đăng ký.
Kiểm soát (controlled)|Không có người giám sát trực tiếp nhưng chỉ thí sinh đã biết trước mới được làm bài, thường qua tên đăng nhập và mật khẩu.
Có giám sát (supervised)|Có người giám sát trực tiếp điều kiện làm bài và xác thực được danh tính thí sinh.
Quản lý (managed)|Mức giám sát và kiểm soát cao, thường thực hiện tại trung tâm khảo thí chuyên dụng, kiểm soát ra vào, nhân sự và thiết bị.
|end|
Buổi thi tại phòng máy của trung tâm có giám thị trong phòng và máy thi do trung tâm quản lý, nên nằm giữa chế độ có giám sát và chế độ quản lý. Đây là căn cứ để hệ thống yêu cầu tài khoản cấp riêng cho từng thí sinh, mã PIN do giám thị đọc và trình duyệt khóa.
### Bộ hướng dẫn của ITC về thi trên máy tính
Bộ hướng dẫn của ITC về thi trên máy tính và qua Internet được Hội đồng ITC thông qua tháng 7 năm 2005 và đăng trên tạp chí International Journal of Testing năm 2006 [@itc]. Các hướng dẫn được chia thành bốn nhóm, lần lượt là chú ý các vấn đề công nghệ, bảo đảm chất lượng, kiểm soát việc tổ chức thi, và bảo mật cùng bảo vệ dữ liệu riêng tư [@itc, tr. 5]. Mỗi hướng dẫn được viết riêng cho ba đối tượng là đơn vị phát triển bài thi, đơn vị phát hành và đơn vị sử dụng. Bảng 2.2 chọn ra những hướng dẫn ảnh hưởng trực tiếp đến thiết kế của đồ án và nêu hệ quả của từng hướng dẫn.
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
Hai hướng dẫn đầu tiên trong bảng liên quan đến đồng hồ và việc lưu câu trả lời. Chúng được cụ thể hóa thành các quyết định thiết kế xử lý ở mục 3.5.
### Tính tương đương giữa thi trên máy và thi trên giấy
Khi một bài thi có cả hai hình thức, cần trả lời câu hỏi điểm số của hai hình thức có so sánh được với nhau hay không. ITC yêu cầu đơn vị phát triển cung cấp bằng chứng về tính tương đương giữa bản thi trên máy và bản thi trên giấy [@itc, tr. 24]. Với các bài kiểm tra đọc ở bậc phổ thông, hiệu ứng của hình thức thi (mode effect) đã được Wang và cộng sự tổng hợp trong một phân tích tổng hợp (meta-analysis) [@wang2008]. Với kỳ thi Cambridge, đơn vị khảo thí khẳng định hai hình thức kiểm tra cùng kiến thức và kỹ năng [@digital]. Hệ quả đối với đồ án là màn hình thi thử phải giống bài thi máy thật càng nhiều càng tốt, để thí sinh không gặp khác biệt về thao tác khi bước vào kỳ thi chính thức.
## Kiến trúc ứng dụng web
### Mô hình khách – chủ và kiểu kiến trúc REST
Ứng dụng web hoạt động theo mô hình khách – chủ, trong đó trình duyệt (máy khách) gửi yêu cầu, còn máy chủ xử lý rồi trả về kết quả. Fielding đề xuất kiểu kiến trúc REST với sáu ràng buộc, gồm khách – chủ, không lưu trạng thái phiên ở máy chủ giữa các yêu cầu, cho phép lưu đệm, giao diện thống nhất, hệ thống phân tầng và mã theo yêu cầu (ràng buộc tùy chọn) [@fielding]. Giao diện thống nhất xoay quanh khái niệm tài nguyên. Mỗi tài nguyên được định danh bằng URI và được thao tác thông qua các biểu diễn của nó.
HTTP là giao thức nền của web. Đặc tả RFC 9110 định nghĩa ngữ nghĩa của các phương thức. Theo đó, GET và HEAD là phương thức an toàn vì không làm thay đổi trạng thái, PUT và DELETE là lũy đẳng (idempotent), nghĩa là gửi nhiều lần cho cùng kết quả như gửi một lần, còn POST không có hai tính chất này [@rfc9110]. Tính lũy đẳng có ích cho bài thi: khi mạng chập chờn, trình duyệt có thể gửi lại yêu cầu lưu câu trả lời mà không tạo ra dữ liệu trùng. Dữ liệu trao đổi giữa trình duyệt và máy chủ dùng định dạng JSON theo RFC 8259 [@rfc8259].
### Ứng dụng trang đơn với React và TypeScript
Ứng dụng trang đơn (SPA) chỉ tải giao diện một lần, sau đó trao đổi dữ liệu với máy chủ và cập nhật đúng phần giao diện cần thay đổi. React là thư viện xây dựng giao diện theo thành phần (component). Mỗi thành phần là một hàm JavaScript trả về phần đánh dấu giao diện, thường được viết bằng cú pháp JSX. Thành phần nhận dữ liệu từ thành phần cha qua props và giữ dữ liệu riêng qua state; khi state thay đổi, React vẽ lại thành phần đó [@react]. Phiên bản React 19 được phát hành ngày 05/12/2024 [@react19].
TypeScript bổ sung hệ thống kiểu tĩnh cho JavaScript, nên lỗi kiểu dữ liệu được phát hiện ngay khi biên dịch thay vì khi chạy [@ts]. Vite là công cụ phát triển gồm hai phần. Phần thứ nhất là máy chủ phát triển dựa trên mô-đun ES gốc của trình duyệt, có khả năng thay mô-đun nóng (hot module replacement, HMR). Phần thứ hai là lệnh đóng gói mã nguồn thành các tệp tĩnh dùng cho môi trường chạy thật [@vite].
### Kiến trúc phân lớp và Clean Architecture
Việc chia ứng dụng thành các lớp theo trách nhiệm giúp quản lý độ phức tạp, tái sử dụng mã và giới hạn phạm vi ảnh hưởng của một thay đổi [@arch]. Kiến trúc N lớp truyền thống (giao diện, nghiệp vụ, truy cập dữ liệu) có nhược điểm là lớp nghiệp vụ phụ thuộc lớp truy cập dữ liệu, khiến việc kiểm thử nghiệp vụ cần đến cơ sở dữ liệu thật [@arch].
Clean Architecture đảo ngược chiều phụ thuộc này. Nghiệp vụ và mô hình ứng dụng nằm ở trung tâm (Application Core), lớp hạ tầng cài đặt các interface do lõi định nghĩa, còn lớp giao diện chỉ làm việc với lõi thông qua các interface đó. Các phụ thuộc lúc biên dịch luôn hướng vào lõi, còn phần cài đặt cụ thể được nối vào lúc chạy bằng cơ chế tiêm phụ thuộc (dependency injection) [@arch]. Hình 2.1 minh họa kiến trúc này theo chiều ngang.
!fig ../../diagrams/report/clean-architecture.drawio.png | Clean Architecture theo chiều ngang (phỏng theo [@arch]) | 16
Lợi ích chính của cách tổ chức trên là lõi không phụ thuộc hạ tầng. Các quy tắc nghiệp vụ, chẳng hạn quy tắc tính giờ của một lượt làm bài, vì thế có thể được kiểm thử đơn vị độc lập với cơ sở dữ liệu.
## Giao tiếp thời gian thực
### Giao thức WebSocket
Trong mô hình HTTP truyền thống, chỉ trình duyệt mới chủ động gửi yêu cầu. Giao thức WebSocket (RFC 6455) cho phép máy chủ đẩy dữ liệu xuống trình duyệt bằng một kênh truyền hai chiều trên một kết nối TCP. Kết nối bắt đầu bằng một yêu cầu HTTP xin nâng cấp giao thức (Upgrade). Sau khi bắt tay thành công, hai bên có thể gửi khung dữ liệu cho nhau vào bất kỳ lúc nào [@rfc6455].
### Thư viện SignalR
ASP.NET Core SignalR là thư viện mã nguồn mở bổ sung chức năng thời gian thực cho ứng dụng, cho phép mã phía máy chủ đẩy nội dung xuống máy khách ngay lập tức. Các ứng dụng phù hợp với SignalR gồm bảng điều khiển, ứng dụng giám sát và ứng dụng cần gửi thông báo [@signalr]. SignalR giao tiếp qua hub. Máy khách gọi được phương thức trên máy chủ, ngược lại máy chủ cũng gọi được phương thức ở máy khách, và thông điệp có thể gửi tới mọi máy khách, tới một máy khách hoặc tới một nhóm máy khách [@signalr].
SignalR tự chọn phương thức truyền tốt nhất mà cả hai phía hỗ trợ, theo thứ tự WebSockets, Server-Sent Events rồi Long Polling. Dữ liệu được mã hóa bằng giao thức văn bản dựa trên JSON (mặc định) hoặc giao thức nhị phân MessagePack [@signalr]. Mô hình hoạt động này được minh họa trong Hình 2.2.
!fig ../../diagrams/report/signalr.drawio.png | Máy chủ đẩy dữ liệu qua hub SignalR và thứ tự chọn phương thức truyền (tổng hợp từ [@signalr]) | 15
Trang theo dõi của giám thị dùng SignalR theo cách này: mỗi khi trạng thái của thí sinh thay đổi, máy chủ chỉ báo cho nhóm giám thị đang theo dõi đúng phần thi đó.
### Duy trì và phát hiện mất kết nối
Để phát hiện kết nối bị đứt, hai bên định kỳ gửi thông điệp giữ kết nối (keep-alive). Ở phía máy chủ, tham số chu kỳ giữ kết nối (KeepAliveInterval), mặc định 15 giây, quy định chu kỳ gửi thông điệp ping. Tham số thời gian chờ máy khách (ClientTimeoutInterval), mặc định 30 giây, là khoảng thời gian mà nếu không nhận được thông điệp nào thì máy chủ coi máy khách đã ngắt kết nối. Giá trị khuyến nghị của thời gian chờ là gấp đôi chu kỳ giữ kết nối. Ở phía máy khách JavaScript, chu kỳ giữ kết nối nên nhỏ hơn hoặc bằng một nửa thời gian chờ của máy chủ [@signalr_config]. Quan hệ giữa thời gian chờ và chu kỳ ping này là căn cứ để cấu hình kênh thời gian thực của hệ thống sao cho kết nối không bị cắt khi phòng thi yên lặng. Mục 4.5.1 cho thấy hậu quả khi quan hệ này bị vi phạm.
## Nền tảng phía máy chủ
### .NET và ASP.NET Core
.NET 10 là phiên bản hỗ trợ dài hạn (LTS) được hỗ trợ trong ba năm, đi kèm ngôn ngữ C# 14 và Entity Framework Core 10 [@net10]. ASP.NET Core là framework mã nguồn mở, đa nền tảng, dùng để xây dựng ứng dụng web trên .NET. Framework có sẵn đường ống xử lý yêu cầu HTTP dạng mô-đun, máy chủ web Kestrel, cơ chế tiêm phụ thuộc, cấu hình theo môi trường, SignalR và các thành phần bảo mật cho xác thực, phân quyền và bảo vệ dữ liệu [@aspnet].
Một số công việc của hệ thống thi phải chạy định kỳ mà không phụ thuộc vào yêu cầu của người dùng, ví dụ nộp bài cho những thí sinh đã hết giờ nhưng đang ngoại tuyến. ASP.NET Core hỗ trợ việc này bằng dịch vụ nền (hosted service), tức một thành phần được khởi động và dừng cùng với ứng dụng [@hosted].
### Entity Framework Core
Entity Framework Core (EF Core) là bộ ánh xạ đối tượng – quan hệ (ORM) mã nguồn mở, đa nền tảng. Lập trình viên làm việc với cơ sở dữ liệu thông qua các đối tượng .NET, truy vấn bằng LINQ và dùng cơ chế di trú (migration) để tạo, cập nhật lược đồ cơ sở dữ liệu theo mô hình [@efcore].
Khi nhiều yêu cầu cùng sửa một bản ghi, chẳng hạn giám thị tạm dừng đúng lúc máy chủ đang nộp bài vì hết giờ, hệ thống cần một cơ chế điều khiển tương tranh. EF Core dùng điều khiển tương tranh lạc quan (optimistic concurrency). Một thuộc tính của thực thể được cấu hình làm mã phiên bản (concurrency token), và khi cập nhật, EF Core thêm điều kiện so sánh mã này vào câu lệnh UPDATE. Nếu bản ghi đã bị thay đổi sau lúc đọc, câu lệnh không cập nhật dòng nào và EF Core báo lỗi xung đột để ứng dụng đọc lại dữ liệu rồi thử lại [@efconc]. Với cơ sở dữ liệu không tự sinh mã phiên bản, ứng dụng có thể tự quản lý mã này, ví dụ gán một GUID mới mỗi lần lưu [@efconc].
### Hệ quản trị cơ sở dữ liệu MySQL
MySQL là hệ quản trị cơ sở dữ liệu quan hệ mã nguồn mở. Trong MySQL 8.4, InnoDB là bộ máy lưu trữ mặc định. Các thao tác dữ liệu tuân theo mô hình ACID với giao dịch có commit, rollback và khôi phục sau sự cố. Khóa ở mức dòng giúp nhiều người dùng làm việc đồng thời tốt hơn, và InnoDB hỗ trợ ràng buộc khóa ngoại để bảo toàn tính toàn vẹn dữ liệu [@mysql]. Các tính chất này phù hợp với dữ liệu bài thi, nơi một lần nộp bài phải ghi trạng thái lượt làm bài và thời điểm nộp trong cùng một giao dịch.
## Bảo mật ứng dụng web
### Các rủi ro phổ biến theo OWASP
OWASP Top 10 là danh sách các nhóm rủi ro bảo mật quan trọng nhất của ứng dụng web. Phiên bản mới nhất là OWASP Top 10:2025, được liệt kê trong Bảng 2.3 [@owasp].
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
Với hệ thống thi, hai nhóm liên quan trực tiếp nhất là kiểm soát truy cập (A01) và xác thực (A07). Thí sinh chỉ được xem bài của mình, giám thị chỉ thao tác trên kỳ thi được phân công, và đề thi chỉ được truy cập trong giờ thi.
### Xác thực bằng cookie và quản lý phiên
Cookie là cơ chế lưu trạng thái của HTTP. Máy chủ gửi cookie qua tiêu đề Set-Cookie, trình duyệt gửi lại cookie trong các yêu cầu sau, và thuộc tính HttpOnly ngăn mã JavaScript đọc cookie [@rfc6265]. ASP.NET Core có sẵn cơ chế xác thực bằng cookie, độc lập với ASP.NET Core Identity. Sau khi người dùng đăng nhập, thông tin định danh (claims) được lưu trong cookie xác thực và được kiểm tra ở mỗi yêu cầu [@cookie]. Việc mã hóa và chống giả mạo cookie do hệ thống bảo vệ dữ liệu (Data Protection) đảm nhận. Hệ thống này đáp ứng các yêu cầu về tính xác thực, toàn vẹn và bảo mật, và có cơ chế quản lý, xoay vòng khóa [@dataprot].
Bảng hướng dẫn quản lý phiên của OWASP đưa ra một số khuyến nghị cho cookie phiên. Cookie nên có thuộc tính Secure để chỉ được gửi qua HTTPS, HttpOnly để chống bị đánh cắp qua XSS, và SameSite=Strict (ưu tiên) hoặc Lax để giảm nguy cơ giả mạo yêu cầu. Định danh phiên cần được cấp lại sau mỗi lần thay đổi mức quyền, điển hình là sau khi đăng nhập, và mọi phiên phải có thời hạn [@session]. Trong hệ thống thi, nguyên tắc cấp lại phiên còn là một quy tắc nghiệp vụ: khi thí sinh đăng nhập ở máy mới thì phiên ở máy cũ phải mất hiệu lực.
### Chống tấn công chèn mã kịch bản
Tấn công XSS xảy ra khi dữ liệu không đáng tin được đưa vào trang web và trình duyệt thực thi dữ liệu đó như mã. OWASP khuyến nghị mã hóa đầu ra theo ngữ cảnh. Khi ứng dụng buộc phải hiển thị HTML do người dùng nhập, HTML đó phải được làm sạch bằng thư viện chuyên dụng [@xss]. Nội dung đề thi, gồm đoạn văn và câu hỏi, là HTML, nên cần được làm sạch ở cả hai phía. Phía máy chủ có HtmlSanitizer, thư viện .NET làm sạch HTML để tránh XSS [@htmlsan], còn DOMPurify làm sạch HTML ngay trong trình duyệt [@dompurify]. Chính sách bảo mật nội dung (CSP) là lớp bảo vệ bổ sung, cho phép máy chủ khai báo những nguồn tài nguyên mà trang được phép tải và thực thi [@csp].
### Safe Exam Browser và khóa cấu hình
Safe Exam Browser chạy dưới dạng ứng dụng kiosk. Trên Windows, SEB vô hiệu hóa thanh tác vụ, menu Start, tổ hợp Alt+Tab, phím Print Screen và các phím tắt như Alt+F4. SEB còn có thể chặn các tiến trình không được phép và từ chối chạy trên máy ảo [@seb].
Để máy chủ biết bài thi có đang được mở bằng SEB với đúng cấu hình hay không, SEB cung cấp khóa cấu hình (Config Key). Đây là giá trị băm SHA-256 gồm 64 ký tự hệ 16, được tính từ toàn bộ thiết lập trong tệp cấu hình SEB và không đổi khi nâng cấp phiên bản SEB. Khi bật tùy chọn gửi khóa, SEB thêm vào mọi yêu cầu HTTP một tiêu đề riêng (X-SafeExamBrowser-ConfigKeyHash) chứa giá trị băm SHA-256 của địa chỉ URL tuyệt đối nối với khóa cấu hình. Máy chủ tự tính lại giá trị này từ URL được yêu cầu và khóa cấu hình đã lưu; nếu hai giá trị không khớp, máy chủ từ chối hoặc dừng bài thi [@sebkey]. Vì URL thay đổi theo từng yêu cầu, giá trị gửi đi không làm lộ khóa cấu hình.
### Khả năng tiếp cận
Bộ tiêu chí WCAG 2.2 của W3C đưa ra các tiêu chí giúp nội dung web dễ tiếp cận hơn, chẳng hạn độ tương phản tối thiểu giữa chữ và nền, hay khả năng phóng to chữ đến 200% mà không mất nội dung hoặc chức năng [@wcag]. Màn hình thi của Cambridge có tùy chọn đổi độ tương phản và cỡ chữ, và hệ thống của đồ án tái hiện các tùy chọn này. Chức năng tô sáng văn bản trong bài đọc có thể cài đặt bằng CSS Custom Highlight API. API này cho phép tô kiểu cho các đoạn văn bản bất kỳ mà không phải chèn thêm phần tử vào cây DOM [@highlight].
## Kiểm thử phần mềm
### Kim tự tháp kiểm thử
Mô hình kim tự tháp kiểm thử, được Mike Cohn phổ biến trong cuốn Succeeding with Agile [@cohn], chia kiểm thử tự động thành ba tầng. Đáy tháp là kiểm thử đơn vị với số lượng nhiều nhất, tầng giữa là kiểm thử ở mức dịch vụ thông qua API, còn đỉnh tháp là kiểm thử qua giao diện với số lượng ít nhất. Fowler giải thích rằng các bài kiểm thử đầu-cuối (end-to-end) qua giao diện dễ vỡ, tốn công viết và chạy chậm, vì thế số lượng của chúng nên ít nhất [@fowler]. Hình 2.3 minh họa mô hình này.
!fig ../../diagrams/report/test-pyramid.drawio.png | Kim tự tháp kiểm thử (phỏng theo [@cohn] và [@fowler]) | 13
### Công cụ kiểm thử
Đồ án dùng một công cụ cho mỗi tầng của kim tự tháp. Ở tầng đơn vị và tầng tích hợp phía máy chủ, xUnit.net, công cụ kiểm thử đơn vị mã nguồn mở cho .NET [@xunit], được dùng cho các quy tắc nghiệp vụ và cho kiểm thử tích hợp với cơ sở dữ liệu MySQL thật. Các thành phần giao diện React được kiểm thử bằng Vitest, một framework kiểm thử dựa trên Vite [@vitest]. Ở đỉnh tháp, Playwright là framework kiểm thử đầu-cuối cho ứng dụng web, hỗ trợ Chromium, WebKit và Firefox trên Windows, Linux và macOS, có sẵn bộ chạy kiểm thử, phép kiểm tra, cơ chế cách ly và chạy song song [@playwright]. Mỗi ngữ cảnh trình duyệt cách ly của Playwright đóng vai một người dùng, vì vậy một kịch bản có thể mô phỏng nhiều thí sinh và giám thị cùng lúc.
## Đóng gói và triển khai
Docker là nền tảng để phát triển, phân phối và chạy ứng dụng trong container. Ứng dụng được đóng gói cùng môi trường chạy thành image và chạy cách ly với máy chủ thật, giúp môi trường phát triển và môi trường chạy thật giống nhau [@docker]. Docker Compose định nghĩa và chạy ứng dụng gồm nhiều container bằng một tệp cấu hình YAML [@compose]. nginx là máy chủ HTTP, có thể vừa phục vụ tệp tĩnh vừa làm proxy ngược (reverse proxy) chuyển tiếp yêu cầu tới máy chủ ứng dụng [@nginx]. Với trung tâm, cách đóng gói này cho phép cài cả hệ thống, gồm cơ sở dữ liệu, máy chủ ứng dụng và giao diện, lên máy chủ của phòng thi bằng một lệnh.
## Quy trình phát triển và mô hình hóa
### Scrum
Scrum là khung làm việc gọn nhẹ giúp con người, nhóm và tổ chức tạo ra giá trị thông qua các giải pháp thích ứng cho những vấn đề phức tạp. Scrum dựa trên chủ nghĩa kinh nghiệm với ba trụ cột là minh bạch, kiểm tra và thích ứng [@scrum]. Nhóm Scrum có ba trách nhiệm là Product Owner, Scrum Master và Developers. Mọi sự kiện đều diễn ra trong sprint, vốn kéo dài không quá một tháng, gồm Sprint Planning, Daily Scrum, Sprint Review và Sprint Retrospective. Scrum có ba tạo phẩm, mỗi tạo phẩm gắn với một cam kết, lần lượt là Product Backlog với Product Goal, Sprint Backlog với Sprint Goal, và Increment với Definition of Done [@scrum]. Trong đồ án, một người đảm nhận cả ba trách nhiệm, còn giảng viên hướng dẫn và trung tâm là các bên liên quan trong Sprint Review.
### Ngôn ngữ mô hình hóa UML
UML là ngôn ngữ mô hình hóa do Object Management Group chuẩn hóa, phiên bản hiện hành là 2.5.1 [@uml]. Đồ án sử dụng ba loại sơ đồ UML. Sơ đồ use case mô tả chức năng theo từng tác nhân, sơ đồ máy trạng thái mô tả vòng đời của một lượt làm bài (chưa bắt đầu, đang làm, tạm dừng, đã nộp), còn sơ đồ tuần tự mô tả tương tác giữa các thành phần trong những thao tác chính. Mô hình dữ liệu được biểu diễn bằng sơ đồ thực thể – liên kết sinh trực tiếp từ lược đồ cơ sở dữ liệu. Các sơ đồ này được trình bày ở Chương 3.
## Tiểu kết chương
Chương 2 đã trình bày các cơ sở lý thuyết làm nền cho thiết kế của hệ thống. Bộ hướng dẫn của ITC đặt ra yêu cầu về đồng hồ, độ trễ, xác thực và bảo mật cho bài thi trên máy. Kiến trúc REST, Clean Architecture và SignalR quyết định cách tổ chức máy chủ và kênh thời gian thực, trong khi EF Core và MySQL cung cấp cơ chế giao dịch và điều khiển tương tranh mà dữ liệu bài thi cần đến. Các khuyến nghị của OWASP và cơ chế khóa cấu hình của Safe Exam Browser định hướng phần bảo vệ hệ thống. Cuối cùng, kim tự tháp kiểm thử quyết định cách kiểm chứng, còn Scrum quyết định cách tổ chức công việc. Chương 3 vận dụng các nội dung này để phân tích, thiết kế và hiện thực hệ thống.

# HIỆN THỰC HÓA NGHIÊN CỨU
Chương này trình bày quá trình hiện thực hệ thống theo thứ tự các bước đã thực hiện, đặc tả yêu cầu, thiết kế kiến trúc, thiết kế cơ sở dữ liệu, thiết kế xử lý, thiết kế giao diện, bảo mật, cài đặt và phương pháp kiểm thử. Tài liệu thiết kế, gồm bản đặc tả yêu cầu và bản đặc tả thiết kế kèm đặc tả API, được viết trong sprint 2 và được cập nhật theo mã nguồn. Nội dung dưới đây mô tả hệ thống ở phiên bản ngày 07/11/2026. Các sơ đồ đều được vẽ tự động bằng chương trình đọc từ mã nguồn hoặc từ lược đồ cơ sở dữ liệu đang chạy, vì vậy chúng khớp với hệ thống thật.
## Các bước thực hiện
Đồ án đi theo kế hoạch sprint ở Bảng 1.4, nhưng thực tế chạy nhanh hơn lịch. Các chức năng của hai kỳ thi hoàn thành trong khoảng bốn tuần, thời gian còn lại dành cho kiểm thử, rà soát và viết báo cáo. Bảng 3.1 tóm tắt các bước theo thời gian thực tế.
|table| Các bước thực hiện và sản phẩm | 2.6,6.2,7.7
Thời gian|Bước|Sản phẩm chính
10/10–11/10|Khảo sát và khởi động|Ghi chép giao diện bài thi mẫu A2 trên Inspera; lược đồ cơ sở dữ liệu đầu tiên; lớp tính giờ làm bài viết theo hướng kiểm thử trước
11/10–15/10|Hiện thực A2 phía thí sinh và cổng nhân viên|Đăng nhập hai bước, gói đề, màn hình Reading & Writing và Listening, đồng hồ máy chủ, tự lưu, chấm tự động, cổng giám thị, bộ chuyển đề PDF; buổi demo 16/10
16/10–19/10|Yêu cầu mới của trung tâm về vai trò|Năm tác nhân, quản trị viên theo mảng, duyệt chéo giữa hai quản trị viên; bản đặc tả yêu cầu, đặc tả thiết kế và API, sơ đồ; đóng gói Docker
18/10–21/10|Hoàn thiện A2|SignalR, Safe Exam Browser, cảnh báo, soạn đề trên web, tên và logo trung tâm, trộn đề A2 theo vị trí máy
21/10|Mở rộng sang B1|Khảo sát bài thi mẫu B1; các kiểu câu mới của Reading, Writing chọn một trong hai câu, Listening một câu mỗi trang; kiểm thử đầu-cuối
22/10–27/10|Kiểm thử tải và thi thử|Kiểm thử tải 100 và 200 thí sinh; buổi thi thử 16 tình huống bằng trình duyệt tự động; sửa lỗi; siết ràng buộc lược đồ
29/10–01/11|Yêu cầu mới của trung tâm ngày 29/10|Trộn 2–6 đề gốc; lọc đề theo cấp độ; mỗi người chỉ thấy phần việc của mình; chia bài Writing ngẫu nhiên và chấm ẩn danh
02/11–07/11|Rà soát và sửa lỗi|Rà soát toàn hệ thống (42 vấn đề), sửa theo 8 nhóm, chạy lại toàn bộ kiểm thử, thi thử và kiểm thử tải
|end|
Bảng cho thấy hai đợt yêu cầu mới của trung tâm, ngày 16/10 và ngày 29/10, đều được đưa vào hệ thống ngay trong tuần, trước khi bước sang kiểm thử và rà soát.
Khi khảo sát, tôi làm thử các bài thi mẫu chính thức của A2 Key for Schools và B1 Preliminary for Schools trên nền tảng Inspera và ghi lại từng màn hình, từ các biểu tượng ở đầu trang, thanh chuyển part, cách chia đôi màn hình, cách chọn từ, kéo thả cho đến menu tùy chọn. Màu sắc và kích thước được đo bằng công cụ dành cho nhà phát triển của trình duyệt. Các thao tác của giám thị được lấy từ tài liệu ngày thi [@booklet] và tài liệu tập huấn giám thị [@invig]. Những ghi chép này là căn cứ cho đặc tả yêu cầu ở mục 3.2 và thiết kế giao diện ở mục 3.6.
## Đặc tả yêu cầu
### Tác nhân và sơ đồ use case
Các tác nhân đã được giới thiệu ở Bảng 1.3. Sơ đồ use case tổng quát ở Hình 3.1 cho thấy chức năng của từng tác nhân. Trong sơ đồ, tác nhân trừu tượng “Nhân sự” gom chức năng đăng nhập chung của bốn vai trò nhân viên, và quản trị viên kế thừa mọi chức năng của quản trị viên theo mảng. Hai tác nhân hệ thống là Safe Exam Browser, gửi khóa cấu hình trong mỗi yêu cầu, và đồng hồ hệ thống, có nhiệm vụ tự nộp bài, chấm tự động và ghi cảnh báo.
!fig ../../diagrams/use-case.drawio.png | Sơ đồ use case tổng quát | 15.5
Việc phân quyền tuân theo nguyên tắc “mỗi người chỉ thấy phần việc của mình” do trung tâm yêu cầu ngày 29/10. Theo nguyên tắc này, quản trị viên mảng đề thi chỉ thấy đề thi và bộ đề trộn do mình tạo, quản trị viên mảng kỳ thi chỉ tổ chức các kỳ thi do mình tạo, và quản trị viên mảng thí sinh chỉ nhập thí sinh cho những kỳ thi mà người tổ chức đã giao. Giám thị chỉ thấy kỳ thi được phân công. Giám khảo không mở được kỳ thi nào mà chỉ nhận các bài Writing được chia cho mình. Riêng quản trị viên thấy toàn bộ.
### Đặc tả các use case chính
Phần này đặc tả tám use case quan trọng nhất, chia thành ba nhóm. Nhóm thứ nhất là các use case làm bài của thí sinh, nhóm thứ hai là các use case giám sát mô phỏng quy trình của Cambridge, và nhóm cuối cùng xuất phát từ yêu cầu riêng của trung tâm. Mỗi use case được trình bày trong một bảng, từ Bảng 3.2 đến Bảng 3.9.
Use case đầu tiên mô tả cách thí sinh vào bài, bám theo các bước trong tài liệu ngày thi (Bảng 3.2).
|table| Use case UC01: Thí sinh đăng nhập và mở phần thi | 3.6,12.9
Mục|Nội dung
Tác nhân|Thí sinh; Safe Exam Browser
Tiền điều kiện|Kỳ thi đang mở (Active); thí sinh có phiếu đăng nhập; giám thị biết mã PIN của buổi thi.
Luồng chính|1) Thí sinh nhập tên đăng nhập và mật khẩu in trên phiếu. 2) Hệ thống kiểm tra mật khẩu, cấp phiên chưa xác nhận PIN và làm mất hiệu lực phiên ở máy khác. 3) Khi cả phòng đã đăng nhập, giám thị đọc mã PIN; thí sinh nhập PIN và chọn *Unlock and Select*. 4) Thí sinh kiểm tra họ tên và làm bước kiểm tra sẵn sàng. 5) Thí sinh chọn phần thi đầu tiên, đọc hướng dẫn, xác nhận và bấm *Start*. 6) Hệ thống tạo lượt làm bài, tính giờ kết thúc và trả về đề.
Luồng ngoại lệ|Sai mật khẩu hoặc PIN 10 lần thì tài khoản bị khóa 5 phút. Kỳ thi chưa mở thì không đăng nhập được. Kỳ thi có khóa cấu hình mà yêu cầu không đến từ Safe Exam Browser đúng cấu hình thì bị từ chối. Phần thi sau khi phần trước chưa nộp thì hiện *Locked*.
Hậu điều kiện|Có một lượt làm bài ở trạng thái đang làm bài (InProgress), đồng hồ do máy chủ tính.
|end|
Bảng 3.3 đặc tả việc làm bài. Phần quan trọng của use case này nằm ở luồng ngoại lệ, vì thí sinh không được mất câu trả lời khi mất mạng hay đổi máy. Mỗi thay đổi của thí sinh được đưa vào hàng đợi tự lưu (autosave), còn trình duyệt định kỳ gửi nhịp kiểm tra kết nối (heartbeat) để nhận trạng thái mới nhất từ máy chủ.
|table| Use case UC02: Thí sinh làm bài | 3.6,12.9
Mục|Nội dung
Tác nhân|Thí sinh
Tiền điều kiện|Lượt làm bài đang chạy.
Luồng chính|1) Thí sinh chuyển giữa các part bằng thanh dưới màn hình và trả lời câu hỏi theo từng kiểu tương tác. 2) Mỗi thay đổi được đưa vào hàng đợi tự lưu và gửi lên máy chủ. 3) Năm giây một lần, trình duyệt gửi nhịp kiểm tra kết nối và nhận lại thời gian còn lại, trạng thái, thông báo của giám thị. 4) Thí sinh có thể đánh dấu câu, tô sáng, ghi chú, đổi cỡ chữ và độ tương phản, ẩn màn hình bằng menu.
Luồng ngoại lệ|Mất mạng: câu trả lời được giữ ở trình duyệt và gửi lại khi có mạng. Tải lại trang hoặc đổi máy: mở lại đúng phần thi, đúng trang, đúng câu trả lời. Bị tạm dừng: màn hình bị che, không nhập được.
Hậu điều kiện|Câu trả lời được lưu trên máy chủ. Thí sinh không có nút nộp bài; phần thi kết thúc khi hết giờ hoặc khi giám thị kết thúc.
|end|
Hai use case tiếp theo mô phỏng các thao tác xử lý sự cố của giám thị. Bảng 3.4 đặc tả thao tác tạm dừng và cho làm tiếp, còn Bảng 3.5 đặc tả quy trình xử lý sự cố phần Listening với hai thao tác riêng biệt.
|table| Use case UC03: Giám thị tạm dừng và cho làm tiếp | 3.6,12.9
Mục|Nội dung
Tác nhân|Giám thị (hoặc người tổ chức kỳ thi)
Tiền điều kiện|Giám thị được phân công cho kỳ thi và đang mở trang theo dõi của phần thi.
Luồng chính|1) Giám thị chọn một, nhiều thí sinh hoặc cả phòng. 2) Chọn *Pause*. 3) Hệ thống tạm dừng những thí sinh đang làm bài và đang trực tuyến, ghi nhật ký thao tác. 4) Màn hình thí sinh hiện thông báo tạm dừng gần như ngay lập tức; trang theo dõi cập nhật. 5) Khi xử lý xong, giám thị chọn *Resume* (cho làm tiếp); thời gian dừng được cộng vào giờ kết thúc.
Luồng ngoại lệ|Thí sinh ngoại tuyến, chưa bắt đầu hoặc đã nộp bị bỏ qua, và kết quả ghi rõ lý do cho từng người.
Hậu điều kiện|Đồng hồ của thí sinh không chạy trong thời gian bị tạm dừng.
|end|
|table| Use case UC04: Xử lý sự cố phần Listening | 3.6,12.9
Mục|Nội dung
Tác nhân|Giám thị
Tiền điều kiện|Thí sinh đang làm phần Listening, không bị tạm dừng, bản ghi đã phát.
Luồng chính|1) Giám thị chọn thí sinh, chọn tua lại N phút và ghi chi tiết (tùy chọn). 2) Hệ thống lùi vị trí phát; máy thí sinh phát lại từ vị trí mới. 3) Hộp cộng giờ mở sẵn với cùng N phút cho đúng những thí sinh đã được tua. 4) Giám thị xác nhận; hệ thống cộng N phút thời gian bù sự cố.
Luồng ngoại lệ|Số phút lớn hơn phần đã phát hoặc thí sinh đang tạm dừng thì bị từ chối.
Hậu điều kiện|Hai thao tác riêng biệt được ghi nhật ký, đúng như hướng dẫn của Cambridge [@invig, tr. 20].
|end|
Bảng 3.6 đặc tả các cách kết thúc bài làm. Đây là use case cần thận trọng nhất, vì lệnh *End now* ở cấp phần thi không hoàn tác được.
|table| Use case UC05: Kết thúc bài làm | 3.6,12.9
Mục|Nội dung
Tác nhân|Giám thị
Tiền điều kiện|Thí sinh đang làm phần thi.
Luồng chính|1) Ở cấp phần thi, giám thị chọn *Edit end time* với *End in*, *End at* hoặc *End now* và bấm hai lần để xác nhận; *End now* có thêm tùy chọn bỏ thời gian cộng thêm. 2) Hệ thống tính lại giờ kết thúc. Với *End now*, thời gian chuẩn kết thúc ngay và bài được nộp nếu thí sinh không còn thời gian cộng thêm. 3) Ở cấp kỳ thi, nút *End the whole test* kết thúc cả bài thi của các thí sinh được chọn.
Luồng phụ|Nếu lỡ kết thúc ở cấp kỳ thi, *Use test end time* trả lại giờ kết thúc chuẩn; phần đã nộp giữ nguyên, các phần sau được đủ giờ.
Hậu điều kiện|*End now* ở cấp phần thi không hoàn tác được và chỉ rút ngắn giờ, không bao giờ kéo dài.
|end|
Ba use case cuối xuất phát từ yêu cầu riêng của trung tâm. Bảng 3.7 mô tả việc trộn đề và chia mã đề theo vị trí máy, Bảng 3.8 mô tả việc chấm Writing ẩn danh, và Bảng 3.9 mô tả cơ chế duyệt chéo giữa hai quản trị viên.
|table| Use case UC06: Trộn đề và chia đề theo vị trí máy | 3.6,12.9
Mục|Nội dung
Tác nhân|Quản trị viên mảng đề thi; quản trị viên mảng kỳ thi
Tiền điều kiện|Có từ 2 đến 6 đề gốc đã xuất bản, cùng phần thi, cùng cấu trúc (số part, kiểu câu, số câu, điểm, bản ghi).
Luồng chính|1) Quản trị viên mảng đề thi chọn các đề gốc theo thứ tự A, B, C… và số mã đề K. 2) Hệ thống kiểm tra các đề có cùng khuôn hay không, rồi sinh K đề trộn ở trạng thái đã xuất bản, mỗi part lấy nguyên từ một đề gốc. 3) Quản trị viên mảng kỳ thi chọn bộ đề trộn cho phần thi và nhập số máy mỗi dãy. 4) Khi thí sinh bắt đầu, hệ thống giao mã đề theo số máy; sơ đồ phòng in kèm mã đề.
Luồng ngoại lệ|Các đề không cùng khuôn thì hệ thống liệt kê từng điểm khác nhau. K ngoài khoảng cho phép thì bị từ chối.
Hậu điều kiện|Hai máy kề nhau theo hàng ngang, hàng dọc và đường chéo luôn khác mã đề; thí sinh đổi máy giữa giờ vẫn giữ đề đã nhận.
|end|
|table| Use case UC07: Chấm Writing ẩn danh | 3.6,12.9
Mục|Nội dung
Tác nhân|Giám khảo; hệ thống
Tiền điều kiện|Giám khảo được phân công cho kỳ thi; thí sinh đã nộp phần thi có Writing.
Luồng chính|1) Hệ thống chia bài ngẫu nhiên và đều cho các giám khảo của kỳ thi, gán mỗi bài một mã sáu ký tự. 2) Giám khảo mở danh sách bài được chia, chọn một mã bài. 3) Hệ thống hiển thị đề Writing của đúng mã đề thí sinh đã làm và bài viết, không có tên thí sinh và các phần khác. 4) Giám khảo nhập điểm và nhận xét cho từng câu. 5) Khi mọi câu Writing đã có điểm, điểm phần thi bằng điểm tự động cộng điểm Writing và trạng thái chấm chuyển sang đã chấm xong (Final).
Luồng ngoại lệ|Bài không được chia cho giám khảo này hoặc phần thi chưa kết thúc thì hệ thống trả lời như không tồn tại.
Hậu điều kiện|Mọi lần chấm được lưu thành lịch sử; điểm mới nhất được tính.
|end|
|table| Use case UC08: Duyệt chéo hai Admin | 3.6,12.9
Mục|Nội dung
Tác nhân|Quản trị viên theo mảng hoặc quản trị viên (người yêu cầu); một quản trị viên khác (người duyệt)
Tiền điều kiện|Người yêu cầu có quyền ở mảng tương ứng.
Luồng chính|1) Người yêu cầu xóa đề, kỳ thi, thí sinh, bộ đề trộn hoặc thay đổi tài khoản nhân viên. 2) Hệ thống kiểm tra điều kiện và tạo yêu cầu chờ duyệt, chưa thực hiện thay đổi. 3) Một quản trị viên khác xem danh sách yêu cầu và duyệt. 4) Hệ thống kiểm tra lại điều kiện và thực hiện thay đổi cùng với quyết định duyệt trong một lần ghi.
Luồng phụ|Người duyệt từ chối kèm lý do; người yêu cầu hủy khi yêu cầu còn chờ; nếu điều kiện không còn đúng lúc duyệt thì yêu cầu chuyển sang thất bại (Failed) kèm lý do.
Hậu điều kiện|Không ai tự duyệt được yêu cầu của mình; mọi yêu cầu được giữ làm nhật ký.
|end|
### Quy tắc nghiệp vụ
Các quy tắc nghiệp vụ ở Bảng 3.10 được rút ra từ tài liệu của Cambridge và từ yêu cầu của trung tâm. Đây là những ràng buộc do máy chủ kiểm tra, còn giao diện chỉ ẩn bớt những thao tác không hợp lệ.
|table| Các quy tắc nghiệp vụ chính | 1.6,10.4,4.5
Mã|Quy tắc|Nguồn
QT01|Các phần thi chạy lần lượt theo thứ tự của loại bài thi; phần sau chỉ mở khi mọi phần trước đã nộp. Thứ tự là dữ liệu, không viết cứng trong mã nguồn.|Cambridge [@booklet]
QT02|Đồng hồ và giờ kết thúc chỉ tính trên máy chủ; thí sinh ngoại tuyến vẫn bị nộp bài đúng giờ.|[@itc, tr. 16]
QT03|Thí sinh không có nút nộp bài; phần thi kết thúc khi hết giờ hoặc khi giám thị kết thúc.|Cambridge [@booklet]
QT04|Giám thị chỉ tạm dừng, cho làm tiếp những thí sinh đang trực tuyến và đang làm bài.|[@invig, tr. 15]
QT05|*End now* ở cấp phần thi kết thúc bài ngay, không hoàn tác được và chỉ rút ngắn giờ kết thúc.|[@invig, tr. 18]
QT06|Tua lại phần nghe và cộng giờ bù là hai thao tác riêng; chỉ tua khi thí sinh không bị tạm dừng và không quá phần đã phát.|[@invig, tr. 20]
QT07|Không cộng giờ cho thời gian đi vệ sinh; thời gian cộng thêm theo kế hoạch cố định từ lúc thí sinh bắt đầu, sự cố được bù bằng giờ bù riêng.|[@booklet, tr. 13]; thiết kế
QT08|Mỗi phòng thi có ít nhất một giám thị cho 25 thí sinh; hệ thống cảnh báo khi thiếu.|[@booklet, tr. 4]
QT09|Xóa đề, kỳ thi, thí sinh, bộ đề trộn và thay đổi tài khoản nhân viên phải được một quản trị viên khác duyệt.|Trung tâm, 16/10
QT10|Hai máy kề nhau không làm cùng một mã đề; đề của thí sinh cố định từ lúc bắt đầu phần thi.|Trung tâm, 20/10 và 29/10
QT11|Bài Writing được chia ngẫu nhiên và đều cho giám khảo; giám khảo chỉ thấy mã bài, không thấy tên thí sinh và bảng điểm; thí sinh không bao giờ thấy điểm.|Trung tâm, 29/10
QT12|Mỗi người chỉ thấy đề, kỳ thi, thí sinh thuộc phần việc của mình; quản trị viên thấy tất cả.|Trung tâm, 29/10
QT13|Bài làm là hồ sơ thi: không xóa được thí sinh, phần thi hay kỳ thi đã có bài làm.|Thiết kế
|end|
Tám quy tắc đầu có nguồn từ tài liệu của Cambridge và ITC, bốn quy tắc tiếp theo đến từ yêu cầu của trung tâm, còn quy tắc cuối cùng là một quyết định thiết kế nhằm bảo vệ hồ sơ thi.
### Yêu cầu phi chức năng
Các yêu cầu phi chức năng ở mục 1.5.2 được chuyển thành chỉ tiêu đo được, kèm cách kiểm chứng, như trình bày trong Bảng 3.11. Kết quả đo được trình bày ở Chương 4.
|table| Chỉ tiêu cho các yêu cầu phi chức năng | 3.4,6.6,6.5
Yêu cầu|Chỉ tiêu|Cách kiểm chứng
Độ trễ màn hình|Lưu câu trả lời và chuyển trang dưới 2 giây [@itc, tr. 15]|Kiểm thử tải, đo phân vị 95 và 99
Lệnh giám thị|Đến màn hình thí sinh trong vài giây|Kiểm thử tải và buổi thi thử đo thời gian từ lúc bấm đến lúc màn hình đổi
Phát hiện mất kết nối|Trang giám thị báo *Offline* trong khoảng 15 giây|Buổi thi thử, tình huống mất mạng
Đúng giờ|Mọi thí sinh, kể cả máy đã tắt, được nộp bài khi hết giờ|Kiểm thử tích hợp và buổi thi thử
Không mất bài|Câu trả lời chọn lúc mất mạng, lúc bị tạm dừng hay khi tải lại trang không bị mất|Kiểm thử đơn vị hàng đợi tự lưu, kiểm thử đầu-cuối, buổi thi thử
Quy mô|100 thí sinh đồng thời trên một máy chủ, không lỗi máy chủ|Kiểm thử tải 100 và 200 thí sinh
Bảo mật|Phân quyền ở API; duyệt chéo; khóa tài khoản khi nhập sai|Kiểm thử tích hợp cho từng quy tắc truy cập
|end|
## Thiết kế kiến trúc
### Kiến trúc tổng thể
Hệ thống chạy trên một máy chủ đặt trong phòng thi và gồm ba thành phần, đóng gói thành ba container Docker như trong Hình 3.2. Thành phần thứ nhất là nginx, vừa phục vụ các tệp tĩnh của giao diện React vừa chuyển tiếp các yêu cầu API và các kết nối thời gian thực tới máy chủ ứng dụng. Thành phần thứ hai là ứng dụng ASP.NET Core, cung cấp REST API, hai hub SignalR và các dịch vụ nền, đồng thời chạy bộ chuyển đề PDF viết bằng Python. Thành phần thứ ba là cơ sở dữ liệu MySQL 8.4. Vì nginx đứng trước, trình duyệt chỉ làm việc với một địa chỉ duy nhất, và cookie đăng nhập nhờ vậy giữ được thuộc tính SameSite=Strict.
!fig ../../diagrams/report/deployment.drawio.png | Sơ đồ triển khai hệ thống trong phòng thi | 16
Máy thí sinh chỉ cần trình duyệt, và trong kỳ thi thật trình duyệt đó là Safe Exam Browser. Máy của giám thị và nhân viên dùng Chrome hoặc Edge để mở cổng nhân viên. Cơ sở dữ liệu chỉ lắng nghe ở địa chỉ nội bộ 127.0.0.1 của máy chủ, nên các máy khác trong mạng không kết nối trực tiếp được. Hình ảnh, âm thanh của đề và khóa mã hóa cookie được lưu trong một thư mục dữ liệu riêng, tách khỏi cơ sở dữ liệu.
### Kiến trúc phía máy chủ
Mã nguồn phía máy chủ được chia thành ba project theo Clean Architecture (mục 2.2.3), cộng thêm một project kiểm thử. Trách nhiệm của từng project được tóm tắt trong Bảng 3.12.
|table| Các project phía máy chủ | 3.8,12.7
Project|Trách nhiệm
ExamPrep.Domain|28 lớp thực thể, các kiểu liệt kê và các quy tắc nghiệp vụ thuần: tính giờ và vị trí phần nghe, so khớp đáp án, chọn câu được chấm, chia bài cho giám khảo, so khuôn đề, lập mẫu trộn, xếp mã đề theo chỗ ngồi, sinh tên đăng nhập, định mức giám thị. Thao tác vi phạm quy tắc bị từ chối bằng một ngoại lệ nghiệp vụ riêng.
ExamPrep.Infrastructure|Ngữ cảnh cơ sở dữ liệu, một lớp cấu hình cho mỗi thực thể, các bản di trú lược đồ, dữ liệu khởi tạo; đọc, kiểm tra và nhập gói đề; lưu tệp theo băm SHA-256; trộn đề; chấm tự động; chia bài cho giám khảo; đề trắng theo khung.
ExamPrep.Api|Ứng dụng ASP.NET Core: controller theo nhóm chức năng, hai cơ chế xác thực bằng cookie, chính sách phân quyền, bộ lọc Safe Exam Browser, hub SignalR, dịch vụ nền (nộp bài hết giờ, theo dõi mất kết nối), duyệt chéo, bộ tạo dữ liệu demo.
ExamPrep.Tests|Kiểm thử đơn vị cho Domain và kiểm thử tích hợp chạy API thật trên cơ sở dữ liệu MySQL tạo riêng cho mỗi lần chạy.
|end|
Chiều phụ thuộc luôn hướng vào lõi. Infrastructure và Api phụ thuộc Domain, còn Domain không phụ thuộc Entity Framework Core hay ASP.NET Core. Kết quả là các quy tắc tính giờ, chấm điểm và trộn đề được kiểm thử đơn vị mà không cần cơ sở dữ liệu.
API được chia theo ba nhóm người dùng. Nhóm thứ nhất phục vụ thí sinh, nhóm thứ hai phục vụ các thao tác trong phòng thi và việc chấm bài, còn nhóm thứ ba phục vụ việc quản lý đề, kỳ thi, thí sinh và tài khoản. Danh sách các endpoint, gồm 82 phương thức xử lý, nằm ở Phụ lục 1. Dữ liệu trao đổi ở dạng JSON. Khi có lỗi, máy chủ trả về theo định dạng mô tả lỗi chuẩn (problem details) với một câu dành cho người dùng. Riêng mã 409 còn kèm một mã lý do, cho biết thí sinh đang bị tạm dừng, phần thi đã kết thúc hay máy chủ đang bận, để trình duyệt biết nên thử lại hay dừng.
### Kiến trúc phía giao diện
Giao diện là một ứng dụng trang đơn React viết bằng TypeScript và đóng gói bằng Vite. Ứng dụng có hai phần, gồm màn hình thí sinh ở địa chỉ gốc và cổng nhân viên ở một đường dẫn riêng. Cổng nhân viên là một gói mã tách biệt, chỉ được tải khi người dùng mở cổng, nên máy thí sinh không phải tải mã của phần quản lý.
Mã nguồn giao diện được tổ chức thành bốn nhóm. Nhóm làm bài chứa màn hình làm bài, phần phân trang, hàng đợi tự lưu, các kiểu câu hỏi, trình phát phần nghe, chức năng ghi chú, tô sáng và menu tùy chọn. Nhóm đăng nhập chứa các màn hình đăng nhập theo đúng các bước trong tài liệu ngày thi. Nhóm cổng nhân viên gồm các trang theo đường dẫn, lớp gọi API, bảng quyền theo vai trò phản chiếu chính sách của máy chủ, cùng các trang của giám thị, giám khảo và quản lý đề. Nhóm cuối cùng là lớp gọi API chung và kết nối SignalR. HTML của đề được làm sạch hai lần, bằng HtmlSanitizer khi nhập vào máy chủ và bằng DOMPurify khi hiển thị.
### Kênh thời gian thực và tác vụ nền
Hệ thống có hai hub SignalR. Hub thứ nhất phục vụ trang theo dõi của giám thị. Khi mở trang, trình duyệt xin tham gia nhóm của một phần thi, và máy chủ kiểm tra lại quyền của tài khoản trong cơ sở dữ liệu trước khi chấp nhận. Từ đó, trang nhận một thông điệp báo phần thi thay đổi mỗi khi bảng theo dõi cần tải lại. Các thông điệp này được gộp, tối đa hai lần mỗi giây cho mỗi phần thi, để phòng đông không làm trang giám thị tải lại liên tục. Hub thứ hai gửi thông điệp yêu cầu làm mới tới máy thí sinh sau mỗi thao tác của giám thị, khi có thông báo hoặc khi hết giờ. Nhận được thông điệp, máy thí sinh lập tức gửi nhịp kiểm tra kết nối để lấy trạng thái mới. Khi kết nối cuối cùng của một thí sinh đóng lại, máy chủ đánh dấu thí sinh đó ngoại tuyến. Các thông số thời gian được chọn theo khuyến nghị của tài liệu SignalR [@signalr_config] và được trình bày trong Bảng 3.13.
|table| Các thông số thời gian của hệ thống | 7.5,3,6
Thông số|Giá trị|Ghi chú
Chu kỳ nhịp kiểm tra kết nối của máy thí sinh|5 giây|Đồng hồ, trạng thái, thông báo, vị trí phần nghe
Cửa sổ trực tuyến|15 giây|Không có nhịp kiểm tra kết nối trong 15 giây thì coi là ngoại tuyến
Thời gian chờ của máy chủ SignalR (ClientTimeoutInterval)|12 giây|Kết nối im lặng quá thời gian này bị ngắt
Chu kỳ giữ kết nối của trình duyệt|4 giây|Nhỏ hơn một nửa thời gian chờ của máy chủ
Gộp thông điệp báo phần thi thay đổi|500 mili giây|Tối đa hai lần tải lại mỗi giây cho mỗi phần thi
Trang theo dõi tự tải lại|20 giây hoặc 3 giây|20 giây khi có SignalR, 3 giây khi mất kết nối SignalR
Quét bài hết giờ|5 giây|Kèm 5 giây ân hạn cho lần tự lưu cuối
Tự lưu: chờ sau thay đổi cuối|800 mili giây|Không quá 3 giây kể từ thay đổi chưa gửi đầu tiên
|end|
Điểm cần lưu ý trong bảng là quan hệ giữa thời gian chờ 12 giây của máy chủ và chu kỳ giữ kết nối 4 giây của trình duyệt. Quan hệ này tuân theo nguyên tắc ở mục 2.3.3, và giá trị 4 giây chỉ được đặt sau khi kiểm thử tải phát hiện lỗi (mục 4.5.1).
Hai dịch vụ nền chạy cùng ứng dụng theo cơ chế dịch vụ nền của ASP.NET Core [@hosted]. Dịch vụ quét bài hết giờ chạy mỗi 5 giây, nộp các lượt làm bài đã quá giờ kết thúc, kể cả của thí sinh ngoại tuyến, rồi gọi bộ chấm tự động cho các bài vừa nộp. Dịch vụ phát cập nhật phòng thi gửi các thông điệp đã gộp, đồng thời phát hiện các thí sinh vừa ngừng gửi nhịp kiểm tra kết nối để ghi cảnh báo mất kết nối và báo cho trang giám thị. Ở môi trường phát triển (Development), máy chủ còn chạy thêm bộ tạo dữ liệu demo khi khởi động.
## Thiết kế cơ sở dữ liệu
### Tổng quan lược đồ
Cơ sở dữ liệu gồm 28 bảng, chia thành bốn nhóm như trong Bảng 3.14. Lược đồ được tạo bằng các bản di trú của Entity Framework Core. Tên bảng và tên cột viết thường, các từ nối với nhau bằng dấu gạch dưới. Mọi kiểu liệt kê được lưu dưới dạng chữ để dễ đọc khi tra cứu trực tiếp, và mọi thời điểm được lưu theo giờ UTC.
|table| Các nhóm bảng của cơ sở dữ liệu | 4,2,10.5
Nhóm|Số bảng|Các bảng
Nội dung đề thi|11|exam_types, section_templates, test_papers, paper_parts, question_groups, questions, answer_keys, answer_corrections, paper_mixes, paper_mix_sources, assets
Kỳ thi và thí sinh|7|exams, exam_sections, candidates, exam_invigilators, exam_examiners, exam_candidate_managers, candidate_messages
Làm bài, chấm điểm và giám sát|6|attempts, responses, annotations, manual_marks, invigilator_actions, security_events
Tài khoản nhân sự và cấu hình|4|users, user_areas, approval_requests, site_settings
|end|
Hình 3.3 cho thấy bốn nhóm bảng cùng các khóa ngoại nối giữa các nhóm.
!fig ../../diagrams/erd.drawio.png | Sơ đồ tổng quan bốn nhóm bảng và các khóa ngoại giữa các nhóm | 14
Sơ đồ thực thể – liên kết được vẽ tự động bằng một chương trình đọc trực tiếp lược đồ MySQL đang chạy, nên luôn khớp với bản di trú mới nhất. Mỗi nhóm có một sơ đồ chi tiết riêng, từ Hình 3.4 đến Hình 3.7. Các sơ đồ chi tiết chỉ vẽ khóa ngoại trong nhóm, còn khóa ngoại sang nhóm khác được ghi ngay sau tên cột.
### Nhóm nội dung đề thi
Quyết định thiết kế quan trọng nhất của lược đồ là mô tả loại bài thi bằng dữ liệu. Một bảng chứa các loại bài thi (A2 Key for Schools, B1 Preliminary for Schools), bảng còn lại chứa các phần thi của mỗi loại theo thứ tự cùng thời lượng, trong đó A2 có hai phần thi và B1 có ba. Vì vậy, B1 dùng chung bộ máy với A2 mà không phải sửa mã, và IELTS sau này chỉ cần thêm dữ liệu. Hình 3.4 trình bày chi tiết nhóm bảng này.
!fig ../../diagrams/erd-content.drawio.png | Sơ đồ thực thể – liên kết nhóm nội dung đề thi | 16
Một đề thi thuộc một mẫu phần thi và gồm nhiều part. Mỗi part có các nhóm câu, mỗi nhóm câu có các câu hỏi, và mỗi câu hỏi có một hoặc nhiều đáp án được chấp nhận. Kiểu tương tác của nhóm câu quyết định giao diện câu hỏi. Mỗi part Listening tham chiếu một tệp âm thanh. Bảng tệp chỉ lưu thông tin của hình và âm thanh, còn nội dung tệp nằm trên đĩa và được đặt tên theo băm SHA-256. Một bộ đề trộn ghi lại các đề gốc theo vị trí A, B, C… Mỗi đề trộn lại là một đề thi bình thường, chỉ có thêm mã đề và mẫu trộn (ví dụ “ACBBA”). Bảng nhật ký sửa đáp án ghi lại mỗi lần quản trị viên sửa đáp án của một đề đã xuất bản.
### Nhóm kỳ thi và thí sinh
Nhóm thứ hai lưu kỳ thi, thí sinh và các phân công, được trình bày trong Hình 3.5.
!fig ../../diagrams/erd-exams.drawio.png | Sơ đồ thực thể – liên kết nhóm kỳ thi và thí sinh | 16
Một kỳ thi có trạng thái, mã PIN, mật khẩu giám thị, khóa cấu hình Safe Exam Browser (nếu dùng) và số máy mỗi dãy. Mỗi phần thi của kỳ thi gắn với một đề hoặc một bộ đề trộn và ghi thời điểm giám thị báo bắt đầu. Thông tin của thí sinh gồm số báo danh, số máy, phần trăm thời gian cộng thêm, mật khẩu đã băm, mốc kết thúc ở cấp bài thi, giám khảo được chia, mã bài Writing, mốc thông báo đã đọc và điểm Speaking do quản trị viên nhập. Ba bảng phân công nối kỳ thi với giám thị, giám khảo và quản trị viên mảng thí sinh, còn một bảng riêng lưu thông báo của giám thị cho cả phòng hoặc cho từng thí sinh.
Kỳ thi có bốn trạng thái là nháp (Draft), đã lên lịch (Scheduled), đang mở (Active) và đã đóng (Closed), và chỉ chuyển giữa chúng theo các bước hợp lệ. Kỳ thi nháp và kỳ thi đã lên lịch chuyển qua lại được với nhau. Từ trạng thái đã lên lịch, kỳ thi được mở. Một kỳ thi đang mở chỉ quay về đã lên lịch khi chưa ai bắt đầu làm bài, và chỉ đóng được khi không còn ai đang làm bài. Chỉ quản trị viên mới mở lại được một kỳ thi đã đóng. Để mở kỳ thi, hệ thống yêu cầu đã có thí sinh, giám thị và số máy dạng số.
### Nhóm làm bài, chấm điểm và giám sát
Hình 3.6 trình bày nhóm bảng làm bài, chấm điểm và giám sát, trong đó bảng lượt làm bài là trung tâm của toàn hệ thống.
!fig ../../diagrams/erd-delivery.drawio.png | Sơ đồ thực thể – liên kết nhóm làm bài, chấm điểm và giám sát | 16
Mỗi thí sinh có một lượt làm bài cho mỗi phần thi. Các cột của bảng lượt làm bài chia thành bốn nhóm, gồm trạng thái làm bài và chấm bài, các mốc và đại lượng của đồng hồ, trạng thái phần nghe, và trạng thái kết nối. Bảng 3.15 giải thích các cột của đồng hồ, vốn là phần phức tạp nhất.
|table| Các cột đồng hồ của bảng attempts | 5,11.5
Cột|Ý nghĩa
started_at|Thời điểm thí sinh bắt đầu phần thi
paused_at, paused_seconds|Thời điểm bắt đầu lần tạm dừng hiện tại; tổng số giây đã tạm dừng
end_adjust_seconds|Lượng điều chỉnh giờ kết thúc (số giây có dấu) do *End now*, *End at*, *End in* (tương đối, nên lần tạm dừng sau vẫn dời được giờ kết thúc)
planned_extra_seconds|Thời gian cộng thêm theo kế hoạch, tính từ phần trăm của thí sinh lúc bắt đầu
incident_extra_seconds|Tổng thời gian bù sự cố
disregard_extra_time|Bỏ thời gian cộng thêm (tùy chọn của *End now*)
ends_at|Giờ kết thúc đã tính, dùng để quét bài hết giờ
audio_started_at, audio_adjust_ms|Thời điểm bắt đầu phát phần nghe; lượng điều chỉnh âm thanh do tạm dừng và tua lại
concurrency_stamp|Mã phiên bản của dòng, đổi mỗi lần lưu
|end|
Một cột quan trọng khác ghi đề mà lượt làm bài sử dụng. Cột này được cố định ngay lúc thí sinh bắt đầu, và mọi chức năng sau đó, từ giao đề, phát âm thanh đến chấm điểm và bảng điểm, đều đọc cột này chứ không đọc đề đang gắn với phần thi. Vì vậy, thí sinh đổi máy giữa giờ vẫn giữ đúng đề đã nhận.
Các bảng còn lại của nhóm có vai trò đơn giản hơn. Câu trả lời được lưu theo từng câu hỏi, kèm lựa chọn *Yes*, *No* hoặc *Undecided* ở phần Writing chọn một trong hai câu của B1. Điểm Writing của giám khảo được lưu dưới dạng lịch sử, và điểm mới nhất được tính. Ghi chú và tô sáng có bảng riêng. Hai bảng giám sát ghi nhật ký mọi thao tác của giám thị và các cảnh báo mà máy chủ quan sát được.
### Nhóm tài khoản nhân sự và cấu hình
Nhóm cuối cùng lưu tài khoản nhân sự và cấu hình của trung tâm (Hình 3.7).
!fig ../../diagrams/erd-accounts.drawio.png | Sơ đồ thực thể – liên kết nhóm tài khoản nhân sự và cấu hình | 16
Bảng người dùng chỉ chứa nhân viên, mỗi người có một trong bốn vai trò là quản trị viên, quản trị viên theo mảng, giám khảo và giám thị. Các mảng của quản trị viên theo mảng (đề thi, kỳ thi, thí sinh, trung tâm) nằm ở một bảng riêng. Bảng yêu cầu duyệt vừa là hàng đợi vừa là nhật ký của cơ chế duyệt chéo. Mỗi dòng ghi loại yêu cầu, người yêu cầu, người quyết định, trạng thái và dữ liệu của thay đổi, với năm trạng thái là chờ duyệt (Pending), đã duyệt (Approved), bị từ chối (Rejected), đã hủy (Cancelled) và thất bại (Failed). Bảng cấu hình chỉ có một dòng, chứa tên và logo của trung tâm.
### Ràng buộc toàn vẹn và điều khiển tương tranh
Ngoài khóa chính, khóa ngoại và chỉ mục duy nhất, lược đồ dùng ràng buộc CHECK của MySQL để cơ sở dữ liệu tự từ chối dữ liệu sai, kể cả khi mã ứng dụng có lỗi. Các ràng buộc được chia thành năm loại.
- Mỗi cột kiểu liệt kê có một ràng buộc chỉ nhận đúng các giá trị của kiểu liệt kê đó. Ràng buộc được sinh từ mô hình, nên khi thêm một giá trị mới chỉ cần một migration.
- Một ràng buộc buộc trạng thái của lượt làm bài khớp với các mốc thời gian. Lượt đã bắt đầu phải có thời điểm bắt đầu, lượt có thời điểm tạm dừng khi và chỉ khi đang tạm dừng, lượt đã nộp khi và chỉ khi có thời điểm và lý do nộp, và chỉ bài đã nộp mới được chấm.
- Các ràng buộc giá trị giữ cho thời lượng luôn dương, phần trăm cộng thêm từ 0 đến 100, các đại lượng đồng hồ không âm và điểm không vượt điểm tối đa. Mã PIN có từ 4 đến 8 chữ số, khóa cấu hình gồm 64 ký tự hệ 16, bảng cấu hình chỉ có một dòng, một yêu cầu đang chờ duyệt khi và chỉ khi chưa có người quyết định, và người quyết định phải khác người yêu cầu.
- Khóa ngoại từ lượt làm bài tới thí sinh và phần thi dùng chế độ RESTRICT, nên cơ sở dữ liệu từ chối xóa thí sinh, phần thi hay kỳ thi đã có bài làm.
- Các ràng buộc liên quan đến nhiều dòng, như số câu hỏi không trùng nhau trong một đề hay đề phải khớp mẫu phần thi, do bộ nhập đề kiểm tra.
Về tương tranh, cùng một lượt làm bài có thể bị nhiều nguồn sửa cùng lúc, gồm nhịp kiểm tra kết nối của thí sinh, thao tác của giám thị và dịch vụ quét bài hết giờ. Hệ thống dùng điều khiển tương tranh lạc quan [@efconc]. Mỗi lần một lượt làm bài được lưu, mã phiên bản của dòng được đổi. Một lần ghi dựa trên dữ liệu cũ sẽ gây lỗi xung đột; khi đó máy chủ đọc lại dữ liệu và thử lại một lần, nếu vẫn xung đột thì trả mã 409 kèm một câu giải thích, không bao giờ ghi đè.
Với các thao tác liên quan đến cả kỳ thi, như đổi trạng thái hay sửa kỳ thi, thí sinh bắt đầu phần thi, thêm hoặc sửa thí sinh, cơ chế trên từng lượt làm bài là không đủ. Hệ thống dùng thêm khóa dòng (row lock) trên bản ghi kỳ thi, trong giao dịch có mức cô lập READ COMMITTED. Đổi trạng thái và sửa kỳ thi giữ khóa độc quyền, còn bắt đầu phần thi và sửa thí sinh giữ khóa chia sẻ. Do đó, không thể đóng kỳ thi đúng lúc một thí sinh đang bắt đầu làm bài. Lỗi chờ khóa quá hạn hay khóa chết (deadlock) của MySQL được chuyển thành mã 409 với lý do máy chủ đang bận, để trình duyệt tự thử lại. Bảng lượt làm bài còn có thêm chỉ mục cho các truy vấn quét định kỳ.
## Thiết kế xử lý
### Vòng đời của lượt làm bài
Hình 3.8 là sơ đồ máy trạng thái của một lượt làm bài. Trạng thái làm bài và trạng thái chấm được lưu ở hai cột riêng. Trạng thái làm bài gồm chưa bắt đầu (NotStarted), đang làm bài (InProgress), đang tạm dừng (Paused) và đã nộp (Submitted). Trạng thái chấm gồm chưa chấm (NotGraded), đã chấm tự động (AutoGraded), chờ giám khảo chấm (NeedsManualMarking) và đã chấm xong (Final). Sau khi nộp, lượt làm bài tiếp tục di chuyển trong các trạng thái chấm.
!fig ../../diagrams/attempt-state.drawio.png | Sơ đồ máy trạng thái của một lượt làm bài | 16
Một lượt làm bài được nộp với một trong hai lý do. Lý do thứ nhất là hết giờ (TimeExpired), kể cả khi giờ kết thúc bị kéo về sớm bởi *End in*, *End at* hoặc lệnh kết thúc cả bài thi. Lý do thứ hai là giám thị bấm *End now* (InvigilatorEndNow). Việc tách hai trạng thái giúp một bài đã nộp vẫn chờ giám khảo chấm được mà không ảnh hưởng đến các quy tắc của đồng hồ.
### Đồng hồ do máy chủ tính
Mọi thay đổi đồng hồ đều đi qua một lớp tính giờ làm bài nằm trong lõi nghiệp vụ. Lớp này không cho sửa giờ kết thúc trực tiếp. Thay vào đó, mỗi khi có thao tác, giờ kết thúc được tính lại từ các đại lượng ở Bảng 3.15 theo các công thức sau.
- Giờ kết thúc chuẩn = thời điểm bắt đầu + thời lượng phần thi + tổng thời gian tạm dừng + lượng điều chỉnh giờ kết thúc.
- Giờ kết thúc theo lịch = giờ kết thúc chuẩn + thời gian cộng thêm theo kế hoạch + thời gian bù sự cố. Nếu giám thị chọn bỏ thời gian cộng thêm, giờ kết thúc theo lịch bằng giờ kết thúc chuẩn.
- Giờ kết thúc thực tế = giá trị nhỏ hơn giữa giờ kết thúc theo lịch và mốc kết thúc cấp bài thi của thí sinh, khi mốc này đã được đặt.
- Thời gian còn lại = giờ kết thúc thực tế trừ thời điểm hiện tại, hoặc trừ thời điểm tạm dừng nếu thí sinh đang bị tạm dừng.
- Vị trí phần nghe = thời điểm hiện tại (hoặc thời điểm tạm dừng) trừ thời điểm bắt đầu phát, trừ lượng điều chỉnh âm thanh.
Nhờ cách tính tương đối này, các thao tác kết hợp đúng với nhau. Chẳng hạn, nếu giám thị đặt *End in* 2 phút rồi tạm dừng thí sinh 1 phút, giờ kết thúc vẫn dời thêm 1 phút. Tác động của từng thao tác lên các đại lượng được tóm tắt trong Bảng 3.16.
|table| Tác động của các thao tác lên đồng hồ | 3.6,6.5,6.4
Thao tác|Điều kiện|Tác động
*Start*|Chưa bắt đầu|Ghi thời điểm bắt đầu; tính thời gian cộng thêm theo kế hoạch từ phần trăm của thí sinh
*Pause*|Đang làm, trực tuyến|Ghi thời điểm tạm dừng; đồng hồ đứng yên
*Resume*|Đang tạm dừng, trực tuyến|Cộng thời gian dừng vào tổng thời gian tạm dừng; nếu phần nghe đã phát thì cộng vào lượng điều chỉnh âm thanh để bản ghi tiếp tục đúng chỗ
Cộng giờ bù sự cố|Đang làm hoặc tạm dừng|Cộng vào thời gian bù sự cố
*End in*, *End at*|Đang làm|Đặt lượng điều chỉnh giờ kết thúc để còn đúng số phút hoặc kết thúc đúng giờ chỉ định
*End now*|Đang làm hoặc tạm dừng|Cộng vào lượng điều chỉnh giá trị min(0, phần nguyên của thời điểm hiện tại trừ giờ kết thúc chuẩn), nên chỉ rút ngắn; nộp ngay nếu không còn thời gian cộng thêm
*Use test end time*|Đang làm hoặc tạm dừng|Đưa lượng điều chỉnh giờ kết thúc về 0 và bỏ lựa chọn bỏ thời gian cộng thêm
*End the whole test*|Kỳ thi đang mở|Ghi mốc kết thúc cấp bài thi của thí sinh, tính lại các lượt đang chạy; dịch vụ quét nộp bài
Tua lại phần nghe|Đang làm, không tạm dừng|Cộng vào lượng điều chỉnh âm thanh, không quá phần đã phát
|end|
Riêng *End now* được làm tròn xuống đến giây. Buổi thi thử ngày 26/10 cho thấy nếu làm tròn đến giây gần nhất, giờ kết thúc có thể rơi vào nửa giây sau thời điểm bấm, và bài bị dịch vụ quét nộp với lý do hết giờ thay vì do giám thị kết thúc. Trình duyệt chỉ hiển thị thời gian còn lại do máy chủ trả về và tự đếm ngược giữa hai nhịp kiểm tra kết nối, không dùng đồng hồ của máy thí sinh để tính giờ.
### Đăng nhập và bắt đầu phần thi
Hình 3.9 là sơ đồ tuần tự của use case UC01. Giống Inspera, việc đăng nhập gồm hai bước. Ở bước một, máy chủ kiểm tra phiếu đăng nhập và cấp một cookie ghi rằng thí sinh chưa xác nhận PIN. Ở bước hai, máy chủ kiểm tra mã PIN và cấp lại cookie với trạng thái đã xác nhận. Mọi API làm bài đều yêu cầu cookie đã xác nhận PIN. Mỗi lần thí sinh đăng nhập, máy chủ đổi mã bảo mật (security stamp) của thí sinh. Vì mỗi yêu cầu đều được đối chiếu với mã này, máy cũ bị đăng xuất ngay khi thí sinh đăng nhập ở máy mới.
!fig ../../diagrams/seq-candidate-login.drawio.png | Sơ đồ tuần tự thí sinh đăng nhập và bắt đầu phần thi | 12.5
Tên đăng nhập được sinh theo quy tắc của Cambridge, gồm năm chữ cái đầu của họ (bỏ dấu), chữ cái đầu của tên và số báo danh. Ví dụ, thí sinh Nguyễn An có số báo danh 0101 sẽ có tên đăng nhập nguyea0101. Nếu họ ngắn hơn năm chữ cái, hệ thống lấy thêm chữ cái của tên để đủ sáu chữ cái khi có thể. Mật khẩu được sinh ngẫu nhiên. Mật khẩu được băm để kiểm tra khi đăng nhập, đồng thời được mã hóa bằng Data Protection để in lại phiếu khi cần.
### Lệnh của giám thị và cập nhật tức thì
Hình 3.10 là sơ đồ tuần tự của use case UC03. Trang theo dõi giữ kết nối với hub của nhân viên, còn máy thí sinh giữ kết nối với hub của thí sinh. Khi giám thị bấm *Pause*, máy chủ xét từng thí sinh theo các quy tắc thao tác của giám thị, áp dụng lớp tính giờ, ghi nhật ký, rồi gửi thông điệp yêu cầu làm mới tới máy của những thí sinh bị ảnh hưởng. Máy thí sinh gửi nhịp kiểm tra kết nối ngay, nhận trạng thái tạm dừng và che màn hình. Song song với đó, trang theo dõi nhận thông điệp báo phần thi thay đổi và tải lại bảng. Giống cách Inspera báo, kết quả trả về cho giám thị ghi rõ thao tác đã áp dụng cho bao nhiêu người, bỏ qua những ai và vì sao.
!fig ../../diagrams/seq-invigilator-pause.drawio.png | Sơ đồ tuần tự giám thị tạm dừng thí sinh | 16
Mọi lệnh của giám thị đều được ghi vào bảng nhật ký thao tác. Trang *Invigilator log* của kỳ thi đọc lại bảng này theo thời gian, nên nhà trường có sẵn biên bản sự cố mà không phải ghi tay. Nếu kênh SignalR bị đứt, cả hai phía vẫn hoạt động nhờ thăm dò định kỳ. Máy thí sinh vẫn gửi nhịp kiểm tra kết nối 5 giây một lần, và trang theo dõi tự tải lại mỗi 3 giây. Nói cách khác, SignalR chỉ giúp cập nhật đến sớm hơn chứ không phải điều kiện để hệ thống chạy đúng.
### Tự lưu câu trả lời
Hàng đợi tự lưu ở trình duyệt được thiết kế để không mất câu trả lời trong những tình huống đã gặp khi kiểm thử. Trước hết, các thay đổi được gom lại và gửi sau 800 mili giây không có thay đổi mới, nhưng không quá 3 giây kể từ thay đổi chưa gửi đầu tiên, nên thí sinh gõ liên tục vẫn được lưu đều đặn. Yêu cầu lưu dùng phương thức PUT và ghi đè câu trả lời của từng câu, vì vậy gửi lại nhiều lần không tạo dữ liệu trùng (tính lũy đẳng, mục 2.2.1).
Câu trả lời chưa gửi còn được sao vào bộ nhớ cục bộ của trình duyệt, tách theo kỳ thi, thí sinh và phần thi. Khi trang được mở lại mà lượt làm bài còn chạy, trình duyệt gửi lại các câu trả lời này. Khi người dùng rời trang, trình duyệt gửi chúng bằng một yêu cầu được phép hoàn tất cả sau khi trang đã đóng.
Phản hồi của máy chủ quyết định hàng đợi làm gì tiếp theo. Nếu thí sinh đang bị tạm dừng, máy chủ trả mã 409 với lý do tạm dừng; hàng đợi giữ nguyên câu trả lời và thử lại. Lý do máy chủ đang bận cũng dẫn đến thử lại. Ngược lại, nếu phần thi đã kết thúc hoặc phiên đăng nhập không còn hiệu lực (mã 401, 403), hàng đợi gửi lần cuối rồi đóng. Riêng khi giám thị bấm *End now*, lượt làm bài được nộp ngay, nhưng máy chủ vẫn nhận các câu trả lời đến trong 5 giây sau đó, giống khi hết giờ. Trình duyệt gửi ngay những câu còn chờ trong hàng đợi khi thấy phần thi đã kết thúc, và lượt làm bài được đưa về trạng thái chưa chấm để bộ chấm chấm lại với các câu này. Trong lúc tạm dừng, vùng làm bài bị vô hiệu hóa nên thí sinh không gõ được dưới lớp che.
Mọi đường nộp bài đúng giờ, gồm dịch vụ quét, nhịp kiểm tra kết nối, yêu cầu đọc lượt làm bài và yêu cầu lưu câu trả lời, dùng chung 5 giây ân hạn (grace period). Nhờ vậy, lần lưu cuối gửi ngay trước khi hết giờ vẫn được nhận. Ô nhập bị giới hạn 20 000 ký tự, bằng giới hạn của máy chủ, để một câu trả lời quá dài không làm hỏng cả lô. Cuối cùng, khi thí sinh thoát, mọi bản sao chưa gửi, ghi chú và tô sáng bị xóa khỏi trình duyệt để người dùng máy sau không đọc được.
### Phát phần nghe
Theo bố cục của Inspera, mỗi part Listening có một tệp âm thanh riêng, nhưng thí sinh nghe nối tiếp như một bản ghi duy nhất, chỉ một lần và không có nút điều khiển ngoài nút chỉnh âm lượng. Khi thí sinh bấm phát lần đầu, máy chủ ghi thời điểm bắt đầu phát, và chỉ ghi một lần duy nhất. Từ đó, vị trí phát là đại lượng do máy chủ tính (mục 3.5.2). Trình phát ở trình duyệt nối các tệp theo thứ tự part và luôn đồng bộ với vị trí của máy chủ. Khi trang được tải lại, bản ghi phát tiếp từ vị trí hiện tại. Sau khi thí sinh được cho làm tiếp, hoặc sau khi giám thị tua lại, kể cả lúc bản ghi đã phát hết, trình phát tự phát lại từ vị trí mới mà thí sinh không cần bấm gì. Một tệp không tải được trong 10 giây sẽ bị bỏ qua để các lần tua sau vẫn hoạt động.
### Trộn đề và chia đề theo vị trí máy
Đơn vị trộn là part. Mỗi part của đề trộn được sao nguyên từ một đề gốc, kèm đoạn văn, hình, âm thanh và đáp án, nên bố cục và cách chấm không thay đổi. Trước khi trộn, hệ thống so khuôn các đề gốc và liệt kê mọi điểm khác nhau về phần thi, số part, nhóm câu, kiểu tương tác, số câu, điểm và bản ghi. Chỉ các đề cùng khuôn mới trộn được với nhau.
Với N đề gốc (2 ≤ N ≤ 6) và một phần thi có P part, quản trị viên mảng đề thi chọn số mã đề K, với 4 ≤ K ≤ min(3N, N mũ P). Thuật toán lập mẫu trộn chia K mã đề thành các nhóm có kích thước đều nhau, mỗi nhóm không quá N mã. Mỗi nhóm bắt đầu từ một mẫu b, trong đó b[p] là đề gốc của part p. Mẫu này được chọn ngẫu nhiên từ một hạt giống (seed) được lưu lại, ưu tiên các mẫu pha trộn nhiều đề gốc. Mã đề thứ j của nhóm lấy part p từ đề gốc (b[p] + j) mod N, tức là “xoay” mẫu qua các đề gốc, vì vậy hai mã đề trong cùng một nhóm không có part nào trùng nhau. Cùng một hạt giống luôn cho cùng kết quả, nên có thể tái tạo khi cần đối chiếu.
Mã đề được giao theo số máy. Các máy được đánh số từ 1 theo từng dãy. Mỗi dãy lấy lần lượt một nhóm mã đề, và các máy trong dãy chạy qua các mã của nhóm đó. Như vậy hai máy ngồi cạnh nhau không trùng part nào, còn dãy trước và dãy sau dùng nhóm khác. Khi chỉ có một nhóm hoặc có nhóm chỉ gồm một mã, hệ thống dùng công thức mã = (cột + 2 × dãy) mod K. Trong cả hai trường hợp, tám máy xung quanh một máy, gồm trái, phải, trước, sau và bốn góc chéo, đều có mã đề khác. Bảng 3.17 minh họa trường hợp hai đề gốc, bốn mã đề (hai nhóm gồm mã 1, 2 và mã 3, 4) trong phòng có bốn máy mỗi dãy.
|table| Ví dụ chia mã đề cho phòng bốn máy mỗi dãy (hai đề gốc, bốn mã đề) | 3,3.2,3.2,3.2,3.2
Dãy|Máy 1|Máy 2|Máy 3|Máy 4
1|Mã 1|Mã 2|Mã 1|Mã 2
2|Mã 3|Mã 4|Mã 3|Mã 4
3|Mã 1|Mã 2|Mã 1|Mã 2
|end|
Trong bảng, mỗi máy có mã đề khác với mọi máy kề nó theo hàng ngang, hàng dọc và đường chéo, dù chỉ có hai đề gốc. Mã đề của thí sinh được cố định ngay khi bắt đầu phần thi. Khi kỳ thi được mở, các thí sinh chưa có số máy được gán số máy nhỏ nhất chưa ai nhập, theo thứ tự danh sách. Số máy và số máy mỗi dãy bị khóa khi đã có người bắt đầu, nên không ai bị đổi đề giữa chừng. Trang sơ đồ phòng in được danh sách mã đề kèm mẫu trộn của từng mã và vị trí của từng thí sinh.
### Chấm điểm và chia bài cho giám khảo
Sau mỗi lần quét bài hết giờ, bộ chấm tự động chấm các lượt làm bài đã nộp nhưng chưa chấm. Bộ so khớp đáp án so sánh câu trả lời với các đáp án được chấp nhận. Câu chọn được so theo mã lựa chọn. Câu gõ chữ được so sau khi bỏ khoảng trắng thừa và chuẩn hóa chữ hoa, chữ thường (trừ câu có phân biệt), dấu nháy cong và dấu chấm cuối câu, nhưng lỗi chính tả không bao giờ được bỏ qua. Bộ chuyển đề mở rộng các đáp án có phần tùy chọn trong đáp án của Cambridge, ví dụ “(a/the) waterfall(s)”, thành mọi dạng được chấp nhận. Đề không có Writing được chấm xong ngay, còn đề có Writing chuyển sang trạng thái chờ giám khảo chấm.
Ở B1 Writing Part 2, thí sinh chọn một trong hai câu. Câu được chấm là những câu thí sinh chọn *Yes*. Nếu không có câu nào như vậy, hệ thống chấm những câu đã viết đầu tiên ở trạng thái *Undecided*, và không bao giờ chấm câu chọn *No*. Điểm tối đa của nhóm câu được tính theo số câu được chọn.
Bài Writing được chia cho giám khảo theo cách sau. Các thí sinh chưa được chia được xáo trộn, rồi mỗi bài được giao cho giám khảo đang có ít bài nhất; nếu nhiều giám khảo bằng nhau thì chọn ngẫu nhiên. Với 20 thí sinh và 4 giám khảo, mỗi người nhận 5 bài. Mỗi bài nhận một mã sáu ký tự từ bảng chữ không gây nhầm lẫn (không có I, O, 0, 1). Chỉ tài khoản đang hoạt động có vai trò giám khảo mới được chia bài. Bài đã chấm xong ở lại với giám khảo đã chấm, còn khi danh sách giám khảo thay đổi, những bài chưa ai bắt đầu chấm được chia lại. Mỗi lần lưu điểm là một giao dịch giữ khóa dòng, trong đó hệ thống kiểm tra lại rằng bài còn thuộc giám khảo đó, ghi thêm một dòng lịch sử và tính lại điểm.
Khi phát hiện đáp án của một đề đã xuất bản bị sai, quản trị viên sửa đáp án trên đề gốc. Bản sửa được áp sang các đề trộn đã lấy câu đó, mọi lượt làm bài đã nộp trên các đề này được chấm lại, và lần sửa được ghi vào nhật ký sửa đáp án.
Bảng điểm của quản trị viên còn quy đổi điểm sang thang Cambridge English Scale. Cambridge báo kết quả theo từng kỹ năng trên thang này, điểm tổng là trung bình của bốn kỹ năng, và từ điểm tổng suy ra xếp loại cùng bậc CEFR [@a2hb, tr. 3]. Hệ thống tách điểm của phần thi thành kỹ năng. Ở A2, điểm chấm tự động của phần Reading & Writing là Reading, còn điểm giám khảo chấm là Writing. Điểm thô của mỗi kỹ năng được quy đổi theo bảng của Cambridge dành cho đề luyện tập từ năm 2020 [@cescale_a2], [@cescale_b1]. Bảng này chỉ cho một số mốc, chẳng hạn 20/30 câu Reading A2 ứng với 120 và 28/30 câu ứng với 140. Vì vậy, giữa hai mốc, điểm được nội suy tuyến tính; điểm tối đa ứng với 150 ở A2 và 170 ở B1; điểm dưới mốc thấp nhất không được báo. Phần Speaking thi trực tiếp, nên quản trị viên nhập điểm của giám khảo nói vào bảng điểm. Cambridge lưu ý rằng các bảng này chỉ cho biết mức sẵn sàng của học viên, không dự đoán chính xác điểm của kỳ thi thật.
### Duyệt chéo giữa hai quản trị viên
Hình 3.11 là sơ đồ tuần tự của use case UC08, lấy ví dụ yêu cầu xóa một kỳ thi. Lệnh xóa không xóa ngay mà trả mã 202 kèm thông tin của yêu cầu. Điều kiện của từng loại yêu cầu được kiểm tra hai lần, một lần khi tạo yêu cầu để báo lỗi sớm và một lần khi duyệt, vì tình huống có thể đã thay đổi. Việc thực hiện thay đổi và quyết định duyệt được lưu trong cùng một lần ghi. Cột trạng thái của yêu cầu được dùng làm mã phiên bản, nên hai quản trị viên không thể cùng quyết định một yêu cầu. Khi hệ thống chỉ có một quản trị viên, quản trị viên thứ hai được tạo trực tiếp để cơ chế có thể bắt đầu hoạt động.
!fig ../../diagrams/seq-approval.drawio.png | Sơ đồ tuần tự duyệt chéo hai Admin cho yêu cầu xóa kỳ thi | 13
### Nhập đề từ tệp PDF
Vì hệ thống nhập đề cũ của trung tâm không còn, tôi xây dựng bộ chuyển đề bằng Python, dùng thư viện PyMuPDF. Quản trị viên chọn cấp độ và phần thi, rồi tải lên tệp PDF đề, tệp PDF đáp án và, với Listening, mỗi part một tệp âm thanh. Máy chủ lưu các tệp vào một thư mục tạm, chạy bộ chuyển đề với danh sách tham số cố định và giới hạn thời gian, đọc báo cáo kết quả, rồi nhập kết quả như một gói đề.
Khó khăn chính khi đọc PDF là thứ tự chữ thuần trong tệp không trùng với thứ tự đọc. Vì vậy, mỗi phần thi có một bộ đọc riêng làm việc trên các dòng chữ kèm tọa độ. Bộ chuyển nhận biết loại đề qua trang bìa và từ chối tệp có trang bìa khác phần thi đã chọn. Hình được lấy từ ảnh gốc nhúng trong PDF thay vì chụp vùng trang, để không dính chữ bên cạnh. Bộ chuyển đề còn sinh một tệp ghi chú cho người duyệt, liệt kê lỗi, các điểm cần xem và bảng đối chiếu từng câu.
Gói đề là một tệp nén gồm tệp mô tả đề, thư mục hình và thư mục âm thanh, và được nhập qua ba bước. Bước đọc kiểm tra JSON chặt chẽ, không cho tên tệp trỏ ra ngoài gói và giới hạn số tệp cùng dung lượng. Bước kiểm tra xét số part theo mẫu phần thi, việc đánh số câu từ 1 đến N và quy tắc của từng kiểu tương tác, rồi trả mọi lỗi về thành câu dễ đọc. Bước nhập lưu tệp theo băm SHA-256, làm sạch HTML và ghi toàn bộ trong một giao dịch, với đề ở trạng thái nháp. Người nhập kiểm tra từng câu cùng đáp án, hình và âm thanh rồi mới xuất bản. Quản trị viên cũng có thể tạo đề trắng theo khung của cả năm phần thi A2 và B1 và viết toàn bộ đề trên web. Đề trắng không xuất bản được khi còn chữ giữ chỗ, hình giữ chỗ hoặc câu chưa có đáp án.
## Thiết kế giao diện
### Màn hình thí sinh
Màn hình thí sinh được thiết kế để thí sinh không gặp khác biệt về thao tác khi bước vào kỳ thi chính thức (mục 2.1.3). Tôi không sao chép mã hay hình ảnh của Inspera mà dựng lại bố cục từ ghi chép và số đo trên bài thi mẫu. Đầu trang có logo trung tâm, họ tên và số báo danh, đồng hồ, các biểu tượng kết nối, chuông thông báo, menu, ghi chú và thông tin thí sinh. Cuối trang là thanh chuyển part, trong đó part đang mở hiện các số câu, còn các part khác hiện số câu đã làm dạng “0 of 7”. Nút chuyển trang nằm ở góc phải dưới. Các giá trị đo được đặt trong biến CSS ở một chỗ duy nhất, và Bảng 3.18 liệt kê một số giá trị tiêu biểu.
|table| Một số giá trị giao diện đo trên bài thi mẫu | 5.5,11
Thành phần|Giá trị
Nền trang, hộp nội dung|Nền #F7FAFA, hộp trắng viền #D5D5D5; chữ Arial 16 px
Đầu trang, thanh part|Đầu trang cao 72 px; thanh part cao 53 px, part không mở nền #EFEFEF
Số câu hỏi|Viền 1,6 px màu #418EC8, bo góc 3 px
Lựa chọn đã chọn|Nền xanh nhạt #C4DCF0
Nút chuyển trang|56 × 56 px; nút tiếp theo màu #007E86, nút đang tắt #DDDDDD
|end|
Cấu trúc câu hỏi do dữ liệu điều khiển. Kiểu tương tác của nhóm câu quyết định thành phần giao diện được dùng, như liệt kê trong Bảng 3.19, còn các ô trống được đánh dấu trong đoạn văn bằng một ký hiệu giữ chỗ chứa số câu.
|table| Các kiểu tương tác câu hỏi | 3.8,7,5.7
Kiểu tương tác|Giao diện|Dùng ở
Chọn một phương án (SingleChoice)|Chọn một trong ba phương án chữ|A2 Reading 1–3, Listening 3–4; B1 Reading 1, 3, Listening 2, 4
Chọn hình (ImageChoice)|Chọn một trong ba hình|A2 và B1 Listening Part 1
Chọn từ trong ô trống (InlineChoice)|Bấm vào ô trống, chọn từ trên thanh hiện phía trên|A2 Reading 4; B1 Reading 5
Gõ từ vào ô trống (InlineText)|Gõ một từ vào ô trống|A2 Reading 5, Listening 2; B1 Reading 6, Listening 3
Kéo thả (DragDropMatch)|Kéo thẻ vào ô; ba bố cục: hàng nhãn, người kèm mô tả, ô trống trong bài đọc|A2 Listening 5; B1 Reading 2, 4
Viết bài (Essay)|Ô viết bài có đếm số từ; chọn một trong hai câu ở B1 Writing Part 2|A2 Writing 6–7; B1 Writing 1–2
|end|
Bảng cho thấy B1 dùng lại cả sáu kiểu tương tác của A2. Phần khác biệt chỉ nằm ở hai bố cục kéo thả mới và ở việc chọn một trong hai câu Writing, đúng với những khác biệt đã phân tích ở mục 1.2.3.
Một part được hiển thị mỗi câu một trang khi dữ liệu yêu cầu (B1 Listening Part 2, B1 Writing Part 2) hoặc khi mọi câu có hình riêng (A2 Reading Part 1, Listening Part 1). Các part khác hiển thị trọn trên một trang và có thể chia đôi màn hình với thanh kéo ở giữa.
Thí sinh có thêm một số tiện ích. Thí sinh đánh dấu được câu bằng cờ, tô sáng và ghi chú khi bôi đen chữ (dùng CSS Custom Highlight API [@highlight], không chèn phần tử vào đoạn văn), và dùng menu tùy chọn với ba chế độ tương phản, ba cỡ chữ được lưu trên máy chủ theo thí sinh. Đồng hồ có thể ẩn, và màn hình có thể được che khi thí sinh rời chỗ. Từ 5 phút cuối, đồng hồ hiển thị cả phút và giây. Toàn bộ chữ trên màn hình thí sinh và cổng nhân viên bằng tiếng Anh như bài thi thật, và các bước đăng nhập theo đúng lời hướng dẫn trong tài liệu ngày thi [@booklet, tr. 11].
### Cổng nhân viên
Cổng nhân viên dùng bố cục đơn giản, khác hẳn màn hình thí sinh, để nhân viên không nhầm hai giao diện. Thanh điều hướng chỉ hiện những trang thuộc vai trò của người đăng nhập, như liệt kê trong Bảng 3.20. Dù vậy, máy chủ vẫn kiểm tra quyền ở mọi endpoint.
|table| Các trang của cổng nhân viên theo vai trò | 3.4,13.1
Vai trò|Trang
Quản trị viên|Exams (mọi kỳ thi, kết quả, xuất CSV), Papers, Centre, Staff (tài khoản nhân viên), Approvals (duyệt yêu cầu, nhật ký)
Quản trị viên theo mảng|Các trang của mảng được giao: Papers (nhập, soạn, trộn đề), Exams (kỳ thi do mình tạo, phân công, sơ đồ phòng), thí sinh của kỳ thi được giao, Centre; Approvals (yêu cầu của mình)
Giám thị|Exams (kỳ thi được phân công: mã PIN, phiếu đăng nhập), trang theo dõi từng phần thi, thông báo, cảnh báo
Giám khảo|Marking (danh sách bài được chia theo mã, trang chấm Writing)
|end|
Trang quan trọng nhất của cổng là trang theo dõi phòng thi, mô phỏng trang theo dõi phần thi của Inspera. Trang có bảng thí sinh với các cột trạng thái, kết nối, số phút còn lại, giờ bắt đầu, thời gian cộng thêm, thời gian bù sự cố, số câu đã làm và cảnh báo, và thanh thao tác hiện ở phía dưới khi giám thị chọn thí sinh. Các thao tác không hoàn tác được đều cần bấm hai lần để xác nhận.
## Bảo mật
Thiết kế bảo mật bám theo danh sách rủi ro OWASP Top 10:2025 (Bảng 2.3) và bộ hướng dẫn của ITC về xác thực và bảo vệ dữ liệu. Bảng 3.21 tóm tắt các biện pháp chính theo từng nhóm rủi ro.
|table| Các biện pháp bảo mật theo nhóm rủi ro | 4.2,12.3
Nhóm rủi ro|Biện pháp
Kiểm soát truy cập (A01)|Chính sách theo vai trò và mảng ở mọi endpoint; kỳ thi được lọc theo người tạo và phân công, và chỉ phân công khớp với vai trò hiện tại mới có hiệu lực; đề và tệp của đề chỉ được phục vụ khi thí sinh có lượt làm bài đang chạy; giám khảo chỉ đọc được tệp Writing của bài được chia; tài nguyên ngoài phạm vi trả 404 như không tồn tại
Xác thực (A07)|Hai cơ chế cookie riêng cho nhân viên và thí sinh (HttpOnly, SameSite=Strict, Secure ở môi trường chạy thật); mật khẩu băm bằng bộ băm của ASP.NET Core Identity; mỗi yêu cầu đối chiếu mã bảo mật của tài khoản; khóa tài khoản 5 phút sau 10 lần nhập sai, đếm theo mã tài khoản vì MySQL so tên không phân biệt hoa thường và dấu
Chèn mã (A05)|Truy vấn qua EF Core có tham số; HTML của đề làm sạch bằng HtmlSanitizer khi nhập và DOMPurify khi hiển thị; HTML chỉ được liên kết tới tệp của chính người soạn hoặc tệp đề đã dùng; bảng điểm CSV chống chèn công thức Excel
Cấu hình (A02)|Mọi phản hồi có các tiêu đề nosniff, X-Frame-Options DENY, no-referrer và CSP default-src 'none'; ngoài môi trường phát triển dùng HSTS và trang lỗi không lộ chi tiết; bản thi thật chạy HTTPS, không có tài khoản demo; MySQL chỉ mở ở 127.0.0.1; container API chạy bằng người dùng không có quyền root
Thiết kế không an toàn (A06)|Duyệt chéo giữa hai quản trị viên cho mọi thao tác xóa và thay đổi tài khoản; không ai tự duyệt yêu cầu của mình (cả bằng ràng buộc CHECK); hạ quyền hoặc khóa một quản trị viên thì các yêu cầu đang chờ của người đó bị hủy
Toàn vẹn dữ liệu (A08)|Ràng buộc CHECK, khóa ngoại RESTRICT cho bài làm, mã phiên bản của lượt làm bài, khóa dòng; tệp gói đề được kiểm tra tên và kích thước; logo chỉ nhận PNG theo chữ ký tệp, tối đa 1 MB
Ghi nhật ký (A09)|Nhật ký thao tác giám thị, cảnh báo an ninh, nhật ký duyệt chéo, lịch sử điểm Writing, nhật ký sửa đáp án
|end|
Hai nhóm A01 và A07 có nhiều biện pháp nhất, phù hợp với nhận định ở mục 2.5.1 rằng kiểm soát truy cập và xác thực là hai rủi ro liên quan trực tiếp nhất đến hệ thống thi.
Đối với Safe Exam Browser, mỗi kỳ thi có thể lưu khóa cấu hình của tệp cấu hình SEB. Một bộ lọc chung kiểm tra mọi yêu cầu của thí sinh thuộc kỳ thi đó. Giá trị băm mà SEB gửi kèm phải bằng băm SHA-256 của URL tuyệt đối nối với khóa cấu hình [@sebkey]; nếu không, máy chủ trả mã 403 với câu *Open this test in Safe Exam Browser.* Vì giá trị băm phụ thuộc cả số cổng, nginx chuyển nguyên tiêu đề Host cho máy chủ ứng dụng. Kênh thời gian thực của thí sinh cũng được kiểm tra theo cùng cách; nếu kênh này bị từ chối, máy thí sinh vẫn nhận lệnh của giám thị qua lần gửi nhịp kiểm tra kết nối 5 giây một lần. Khi một trình duyệt giữ cả phiên của nhân viên và phiên của thí sinh, phiên thí sinh không có giá trị ngoài SEB, còn quyền của nhân viên vẫn giữ nguyên.
Các cảnh báo trên trang giám thị chỉ gồm những gì máy chủ thực sự quan sát được. Có bốn loại cảnh báo, gồm yêu cầu không đến từ SEB đúng cấu hình trong khi đang làm bài, mất kết nối, đồng hồ máy thí sinh lệch quá 2 phút và bắt đầu trước khi giám thị báo bắt đầu. Một cảnh báo chưa xử lý không bị ghi lặp, và một cảnh báo vừa được xử lý sẽ không xuất hiện lại cùng loại trong 30 phút.
## Cài đặt và triển khai
### Công nghệ sử dụng
Bảng 3.22 liệt kê các công nghệ và phiên bản được dùng trong mã nguồn.
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
Quy mô mã nguồn tại thời điểm viết báo cáo được trình bày trong Bảng 3.23. Số liệu là số dòng của các tệp trong kho mã, và phần mã máy chủ không tính các migration sinh tự động.
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
Mã kiểm thử phía máy chủ (11 602 dòng) có quy mô gần bằng mã máy chủ (12 285 dòng), vì mọi thay đổi hành vi đều được viết kiểm thử trước khi viết mã (mục 3.9).
### Hai môi trường triển khai
Toàn bộ hệ thống được cài bằng Docker Compose với một lệnh. Có hai cách chạy dùng chung tệp cấu hình cơ sở, được so sánh trong Bảng 3.24. Bản demo dùng cho buổi demo, thi thử và kiểm thử. Bản thi thật bổ sung một tệp cấu hình ghi đè cho môi trường chạy thật (Production) và luôn chạy dưới tên dự án riêng để không dùng chung cơ sở dữ liệu với bản demo. Hướng dẫn cài đặt chi tiết nằm trong tài liệu cài đặt đi kèm kho mã nguồn và được tóm tắt ở Phụ lục 2.
|table| So sánh bản demo và bản thi thật | 4,6,6.5
Đặc điểm|Bản demo|Bản thi thật
Môi trường|Development|Production
Giao thức, cổng|HTTP, cổng 8080|HTTPS cổng 443, cổng 80 chuyển sang HTTPS
Dữ liệu mẫu|Hai kỳ thi demo A2 và B1, tài khoản demo|Không có; quản trị viên đầu tiên đặt trong tệp cấu hình, mật khẩu ít nhất 12 ký tự
Kỳ thi demo|Được xóa và tạo lại mỗi lần máy chủ khởi động (chỉ những kỳ thi được đánh dấu là demo)|Không có
Tệp đề, khóa mã hóa|Thư mục dữ liệu trong mã nguồn|Volume Docker riêng
Cookie|Không bắt buộc HTTPS|Chỉ gửi qua HTTPS (cờ Secure)
|end|
Cơ sở dữ liệu tự cập nhật lược đồ khi máy chủ khởi động, và một trang kiểm tra tình trạng cho biết cả máy chủ ứng dụng lẫn cơ sở dữ liệu có đang chạy hay không. Sau kiểm thử tải (mục 4.4), cấu hình được điều chỉnh với kho kết nối (connection pool) 250 kết nối, giới hạn 300 kết nối của MySQL và 200 luồng tối thiểu cho máy chủ. Lý do của điều chỉnh này được phân tích ở mục 4.5.2.
## Phương pháp kiểm thử
Kiểm thử được tổ chức theo kim tự tháp kiểm thử (mục 2.6.1) và bổ sung thêm hai hình thức kiểm thử ở mức hệ thống, như trình bày trong Bảng 3.25. Mọi thay đổi hành vi đều được viết theo hướng kiểm thử trước, nghĩa là viết kiểm thử, chạy để thấy thất bại đúng lý do, rồi mới viết mã.
|table| Các mức kiểm thử | 3.6,4.6,8.3
Mức|Công cụ|Nội dung
Đơn vị|xUnit v3, Vitest, pytest|Quy tắc của Domain (đồng hồ, chấm, trộn đề, xếp chỗ, chia bài); logic giao diện (phân trang, tự lưu, đồng hồ hiển thị); các bộ đọc của bộ chuyển đề trên các dòng chữ tự soạn đặt đúng tọa độ như đề thật
Tích hợp|xUnit v3, máy chủ chạy trong tiến trình kiểm thử, MySQL thật|Gọi API thật trên cơ sở dữ liệu MySQL tạo riêng và xóa sau mỗi lần chạy; kiểm tra phân quyền của từng endpoint, ràng buộc CHECK, giao dịch, tương tranh (hai yêu cầu đồng thời)
Đầu-cuối|Playwright|6 kịch bản chạy trên một bộ container Docker tách riêng (cổng 8090, cơ sở dữ liệu trống) với đề mẫu tự soạn
Thi thử|Playwright, mỗi người một ngữ cảnh trình duyệt|17 tình huống của một buổi thi thật, 11 người (hai quản trị viên, giám thị, giám khảo, sáu thí sinh A2, một thí sinh B1), Listening chạy đủ 30 phút
Tải|Script Node.js tự viết|100 và 200 thí sinh đồng thời làm bài, một giám thị theo dõi và thao tác qua SignalR
|end|
Kiểm thử tích hợp chạy trên MySQL thật thay vì cơ sở dữ liệu trong bộ nhớ, vì nhiều quy tắc nằm ở ràng buộc CHECK, khóa dòng và mức cô lập giao dịch của MySQL. Mỗi lần chạy tạo một cơ sở dữ liệu riêng và xóa khi xong. Các bản di trú được chạy tuần tự trên một kết nối không dùng kho kết nối, vì khóa di trú của EF Core trên MySQL có phạm vi cả máy chủ cơ sở dữ liệu.
Buổi thi thử được chạy theo một kịch bản viết sẵn, lúc đầu gồm 16 tình huống và từ ngày 25/11 là 17 tình huống. Kịch bản được tự động hóa bằng Playwright để có thể chạy lại sau mỗi đợt sửa lỗi, nên mọi người tham gia, kể cả thí sinh và giám thị, đều là trình duyệt tự động chứ không phải người dùng thật. Mỗi người là một ngữ cảnh trình duyệt riêng với cookie riêng và thao tác qua giao diện như người dùng. Kịch bản tự chuẩn bị hai kỳ thi qua cổng nhân viên, đi qua từng tình huống, đo thời gian, chụp màn hình, ghi kết quả từng tình huống và vẫn tiếp tục khi một tình huống không đạt. Biên bản Word được sinh tự động từ kết quả.
Sau khi hoàn thành các chức năng, toàn bộ hệ thống còn được rà soát độc lập theo năm mảng, gồm thí sinh, phòng thi, phân quyền, đề cùng kỳ thi và chấm điểm, dữ liệu cùng triển khai. Mỗi vấn đề được ghi vị trí trong mã nguồn và hướng sửa, được đối chiếu lại với mã nguồn trước khi sửa, và mỗi bản sửa đi kèm một kiểm thử tái hiện vấn đề.
## Tiểu kết chương
Chương 3 đã trình bày quá trình hiện thực hệ thống từ đặc tả đến cài đặt. Yêu cầu được đặc tả qua tám use case chính và mười ba quy tắc nghiệp vụ có dẫn nguồn. Phần lớn hệ thống dựa trên hai quyết định thiết kế. Quyết định thứ nhất là mô tả loại bài thi bằng dữ liệu, giúp B1 dùng chung bộ máy với A2. Quyết định thứ hai là để máy chủ tính đồng hồ theo các đại lượng tương đối, giúp các thao tác của giám thị kết hợp đúng với nhau. Cơ sở dữ liệu 28 bảng tự bảo vệ bằng ràng buộc CHECK, điều khiển tương tranh lạc quan và khóa dòng. Kênh SignalR giúp lệnh của giám thị đến nhanh, nhưng hệ thống vẫn chạy đúng khi kênh này bị đứt. Chương 4 trình bày kết quả đạt được, các số liệu kiểm thử và những khó khăn gặp phải trong quá trình thực hiện.

# KẾT QUẢ NGHIÊN CỨU
Chương này trình bày các chức năng đã hoàn thành qua ảnh chụp màn hình, cùng kết quả kiểm thử tự động, buổi thi thử và kiểm thử tải. Tiếp theo, chương phân tích những khó khăn kỹ thuật đã gặp và cách khắc phục, trước khi đánh giá hệ thống so với mục tiêu đặt ra. Mọi ảnh chụp đều dùng đề mẫu tự soạn, không có nội dung đề của Cambridge hay của trung tâm.
## Kết quả chức năng
### Màn hình thí sinh
Thí sinh đăng nhập bằng phiếu, chờ giám thị đọc mã PIN rồi nhập mã PIN để mở bài (Hình 4.1). Sau đó, màn hình chọn phần thi hiện bước kiểm tra sẵn sàng và các phần thi theo thứ tự; Listening bị khóa cho đến khi Reading & Writing kết thúc (Hình 4.2).
!fig img/ts-pin.png | Bước nhập mã PIN của buổi thi | 11
!fig img/ts-chon-phan-thi.png | Màn hình chọn phần thi, Listening bị khóa | 11
Hình 4.3 là màn hình làm bài Reading & Writing A2 với bố cục đã mô tả ở mục 3.6.1. Đầu trang có đồng hồ và các biểu tượng, còn thanh part ở cuối trang hiện số câu đã làm của từng part. Hình 4.4 là cùng bài thi ở chế độ chữ trắng trên nền đen và cỡ chữ lớn, với một đoạn văn được tô sáng.
!fig img/ts-lam-bai.png | Màn hình làm bài A2 Reading & Writing, Part 1 | 15.5
!fig img/ts-tuong-phan.png | Chế độ tương phản chữ trắng trên nền đen, cỡ chữ lớn và đoạn văn được tô sáng | 15.5
Ở phần Listening (Hình 4.5), dòng *Audio is Playing* dưới tên thí sinh cho biết bản ghi đang phát. Khi giám thị tua lại, màn hình báo *The invigilator has moved the recording back.* và bản ghi phát tiếp từ vị trí mới. Hình 4.6 là phần Writing Part 2 của B1, nơi thí sinh chọn một trong hai câu bằng hộp *Answering this question?*. Khi bị tạm dừng, nội dung bài thi bị che và thí sinh được báo rằng đồng hồ không chạy (Hình 4.7).
!fig img/ts-listening.png | Phần Listening A2 sau khi giám thị tua lại bản ghi | 15.5
!fig img/ts-b1-writing.png | B1 Writing Part 2: chọn một trong hai câu | 15.5
!fig img/ts-tam-dung.png | Màn hình thí sinh khi bị giám thị tạm dừng | 15.5
### Trang giám thị
Hình 4.8 là trang theo dõi phần Reading & Writing ngay sau khi giám thị tạm dừng cả phòng. Thanh phía trên báo thao tác đã áp dụng cho 6 thí sinh và bỏ qua một thí sinh chưa bắt đầu, kèm lý do. Hình 4.9 cho thấy một thí sinh ngoại tuyến và các cảnh báo ở cột *Warnings*, gồm một cảnh báo bắt đầu trước khi giám thị báo bắt đầu và một cảnh báo mất kết nối. Hình 4.10 là hộp *Edit end time* với lời nhắc rằng *End now* ở cấp phần thi không hoàn tác được.
!fig img/gt-tam-dung-ca-phong.png | Trang theo dõi phần thi sau khi tạm dừng cả phòng | 15.5
!fig img/gt-offline.png | Trang theo dõi báo thí sinh ngoại tuyến và các cảnh báo | 15.5
!fig img/gt-end-now.png | Hộp Edit end time với lựa chọn End now | 15.5
### Trang giám khảo
Giám khảo chỉ thấy danh sách bài được chia cho mình theo mã bài (Hình 4.11). Trang chấm hiện đề Writing của đúng mã đề thí sinh đã làm, bài viết, số từ, ô điểm và nhận xét cho từng câu, nhưng không có tên thí sinh (Hình 4.12).
!fig img/gk-danh-sach-bai.png | Danh sách bài Writing được chia cho giám khảo | 15.5
!fig img/gk-cham-writing.png | Trang chấm Writing ẩn danh | 15.5
### Trang quản trị
Quản trị viên xem được bảng điểm của kỳ thi, có mã bài Writing, giám khảo được chia và trạng thái chấm của từng phần thi, và tải bảng điểm dưới dạng CSV mở được bằng Excel (Hình 4.13). Trang *Approvals* hiển thị hàng đợi và nhật ký duyệt chéo (Hình 4.14). Sơ đồ phòng của kỳ thi B1 dùng đề trộn liệt kê mẫu trộn của bốn mã đề cho từng phần thi và mã đề của từng máy (Hình 4.15).
!fig img/ad-ket-qua.png | Bảng điểm của kỳ thi thử A2 | 15.5
!fig img/ad-duyet-cheo.png | Nhật ký duyệt chéo hai Admin | 15.5
!fig img/ad-so-do-phong.png | Sơ đồ phòng của kỳ thi B1 với bốn mã đề trộn | 15.5
### Quản lý đề thi
Trang *Papers* lọc đề theo cấp độ, phần thi, trạng thái và tên, và có các nút nhập từ PDF, tạo đề trắng, trộn đề và nhập gói đề (Hình 4.16). Hộp nhập từ PDF chỉ hỏi những tệp cần cho phần thi đã chọn; chẳng hạn B1 Listening cần bốn tệp âm thanh (Hình 4.17). Hình 4.18 là một đề trắng A2 vừa tạo, trong đó các chữ “[to write: …]” chờ người soạn điền.
!fig img/de-danh-sach.png | Trang danh sách đề thi | 15.5
!fig img/de-nhap-pdf.png | Hộp nhập đề từ tệp PDF | 15.5
!fig img/de-trang.png | Đề trắng A2 Reading & Writing soạn trên web | 15.5
### Nhật ký giám thị và thang điểm Cambridge
Trang *Invigilator log* liệt kê theo thời gian mọi thao tác của giám thị trong kỳ thi, gồm báo bắt đầu, tạm dừng, cho làm tiếp, cộng giờ, sửa giờ kết thúc, tua phần nghe và thông báo, kèm thí sinh, phần thi, lý do và người thực hiện (Hình 4.19). Thông báo cho cả phòng được ghi là *Whole room*, còn thông báo riêng ghi tên người nhận. Nhật ký tải được dưới dạng CSV để đính kèm biên bản sự cố. Hình 4.20 là bảng điểm của kỳ thi demo A2 sau khi quản trị viên nhập điểm Speaking. Cột cuối ghi điểm thang Cambridge của bốn kỹ năng, rồi đến điểm tổng, xếp loại và bậc CEFR. Chẳng hạn, thí sinh thứ nhất có Reading 138, Writing 138, Listening 140 và Speaking 139, nên điểm tổng là 139, xếp loại B, bậc A2. Điểm Speaking ngoài khoảng 0 đến 45 hoặc không phải bội của 0,5 bị máy chủ từ chối, và lý do hiện ngay dưới ô nhập.
!fig img/gt-nhat-ky.png | Nhật ký thao tác của giám thị | 15.5
!fig img/ad-thang-diem.png | Bảng điểm có thang Cambridge English Scale | 15.5
### Mức độ hoàn thành yêu cầu
Bảng 4.1 đối chiếu các nhóm yêu cầu chức năng ở mục 1.1.2 với kết quả đạt được.
|table| Mức độ hoàn thành các nhóm yêu cầu chức năng | 3.4,9.5,3.6
Nhóm yêu cầu|Kết quả|Trạng thái
Quản lý đề thi|Nhập từ PDF cho cả năm phần thi A2 và B1, nhập gói đề, soạn và sửa đề trên web, đề trắng, xuất bản, sửa đáp án đề đã xuất bản, trộn 2–6 đề gốc|Hoàn thành
Tổ chức kỳ thi|Tạo kỳ thi theo cấu trúc dữ liệu, vòng đời bốn trạng thái, nhập thí sinh từ Excel, phiếu đăng nhập, sơ đồ phòng, phân công giám thị, giám khảo, quản trị viên mảng thí sinh|Hoàn thành
Làm bài|Đăng nhập hai bước, sáu kiểu tương tác, đồng hồ máy chủ, tự lưu, Listening phát nối tiếp một lần, tiện ích của thí sinh, Safe Exam Browser|Hoàn thành; SEB chưa thử trên phòng máy thật
Giám sát|Theo dõi thời gian thực, tạm dừng, cộng giờ, sửa giờ kết thúc, kết thúc cả bài thi, tua phần nghe, thông báo, cảnh báo, nhật ký thao tác|Hoàn thành
Chấm điểm|Chấm tự động, chia bài Writing ẩn danh, lịch sử điểm, bảng điểm CSV, thang Cambridge English Scale với điểm Speaking nhập tay|Hoàn thành
Phân quyền|Năm tác nhân, quản trị viên theo mảng, mỗi người chỉ thấy phần việc của mình, duyệt chéo|Hoàn thành
|end|
Cả sáu nhóm yêu cầu đều đã hoàn thành. Điểm còn để ngỏ là Safe Exam Browser chưa được thử trên phòng máy thật của trung tâm, và hạn chế này được phân tích ở mục 4.6.2.
## Kết quả kiểm thử tự động
Ngày 25/11/2026, toàn bộ kiểm thử tự động đều đạt, với số liệu chi tiết trong Bảng 4.2.
|table| Kết quả kiểm thử tự động ngày 25/11/2026 | 5,3,8.5
Bộ kiểm thử|Số kiểm thử đạt|Phạm vi
Máy chủ (xUnit)|1500/1500|Đơn vị Domain, tích hợp API trên MySQL thật
Giao diện (Vitest)|206/206|Thành phần React, tự lưu, phân trang, kết nối SignalR, quyền theo vai trò
Bộ chuyển đề (pytest)|69/69|Các bộ đọc theo part, đáp án, tách bản ghi Listening, dòng lệnh
Đầu-cuối (Playwright)|6/6|Thí sinh; giám thị; đề và duyệt chéo; kết thúc và chấm; kỳ thi B1; mã đề theo chỗ ngồi B1
|end|
Số kiểm thử qua các mốc của dự án được trình bày trong Bảng 4.3.
|table| Số kiểm thử qua các mốc | 1.7,6.4,2.3,2.3,2.3,1.5
Ngày|Mốc|Máy chủ|Giao diện|Chuyển đề|Đầu-cuối
13/10|Rà soát đầu tiên|231|71|35|–
21/10|Hết sprint 3 (A2 hoàn chỉnh, trộn đề)|350|88|60|–
21/10|Hết sprint 6 (B1 chạy đầu-cuối)|381|98|69|6
26/10|Hết sprint 7 (thi thử, sửa lỗi)|390|104|69|6
02/11|Bắt đầu rà soát|1345|119|69|6
07/11|Sau khi sửa 42 vấn đề|1464|201|69|6
18/11|Đo độ dài bản ghi, SEB cho kênh thời gian thực|1476|201|69|6
25/11|Ân hạn sau End now, nhật ký giám thị, thông báo đã đọc trên máy chủ, thang Cambridge English Scale|1500|206|69|6
|end|
Số kiểm thử tăng mạnh từ cuối tháng 10, khi các ràng buộc lược đồ được siết lại và các yêu cầu mới của trung tâm được bổ sung. Số kiểm thử còn tăng thêm trong đợt sửa lỗi sau rà soát, vì mỗi vấn đề được sửa kèm một kiểm thử tái hiện. Riêng số kịch bản đầu-cuối giữ nguyên ở 6, đúng với vị trí đỉnh tháp của loại kiểm thử này (mục 2.6.1).
## Kết quả buổi thi thử
Buổi thi thử được chạy đầy đủ ba lần, và cả ba lần đều do trình duyệt tự động đóng vai 11 người tham gia. Lần ngày 26/10 tìm được bốn lỗi; sau khi sửa, lần chạy cuối của ngày đó đạt 16/16 tình huống. Lần ngày 07/11 chạy lại trên mã nguồn đã sửa sau đợt rà soát và cũng đạt 16/16. Lần ngày 25/11, từ 14 giờ 48 đến 15 giờ 31, chạy kịch bản có thêm tình huống 17 về nhật ký giám thị. Ở lần này, tình huống 9 kiểm tra rằng máy mới không hiện lại thông báo đã đọc ở máy cũ, còn tình huống 15 có thêm bước nhập điểm Speaking. Kết quả là 17/17 tình huống đạt. Bảng 4.4 trình bày kết quả của lần chạy ngày 25/11 cùng các số đo chính.
|table| Kết quả 17 tình huống của buổi thi thử ngày 25/11/2026 | 1,6,7.9,1.6
STT|Tình huống|Kết quả quan sát|Đánh giá
1|Đăng nhập bằng phiếu và mã PIN|6 thí sinh vào màn hình chọn phần thi, Listening bị khóa|Đạt
2|Nhập sai mã PIN|3 lần sai bị từ chối với thông báo *Incorrect PIN.*, lần đúng vào được|Đạt
3|Bắt đầu trước khi giám thị báo bắt đầu|Chỉ thí sinh bắt đầu sớm có cảnh báo bắt đầu sớm|Đạt
4|Làm bài, tô sáng, đổi cỡ chữ và tương phản|Câu trả lời lưu ngay; tùy chọn có hiệu lực và được lưu trên máy chủ|Đạt
5|Tải lại trang giữa giờ|Về đúng Part 3, còn câu trả lời và đoạn tô sáng, đồng hồ lệch 0 giây|Đạt
6|Tạm dừng một thí sinh, cả phòng; cho làm tiếp|Một thí sinh dừng sau 165 ms; cả phòng 6/6 sau tối đa 224 ms; đồng hồ đứng yên trong 10 giây dừng|Đạt
7|Thông báo cho cả phòng và một thí sinh|Cả phòng 6/6 sau tối đa 180 ms; thông báo riêng chỉ đến đúng người|Đạt
8|Mất mạng 1 phút rồi vào lại|Trang giám thị báo *Offline* sau 9 giây, có cảnh báo; đồng hồ vẫn chạy; câu trả lời chọn lúc mất mạng được lưu khi có mạng|Đạt
9|Chuyển sang máy khác|Máy cũ về màn hình đăng nhập; máy mới tiếp tục cùng lượt làm bài và không hiện lại thông báo đã đọc|Đạt
10|Cộng giờ bù sự cố; *End in*|+5 phút: thời gian còn lại tăng 300 giây; *End in* 2 phút: tự nộp khi hết 2 phút|Đạt
11|Listening: tua lại 1 phút và cộng 1 phút|Vị trí phát từ 75 giây về 16 giây; hộp cộng giờ mở sẵn 1 phút|Đạt
12|B1 Writing Part 2 chọn một câu|*1 of 1 question selected*; gọi thẳng API để chọn thêm câu bị từ chối với mã 409|Đạt
13|*End now* cho một thí sinh|Màn hình kết thúc sau 145 ms, ghi *Ended by invigilator*, không hoàn tác được|Đạt
14|Hết giờ tự nộp|Cả 6 thí sinh tự nộp trong 15 giây sau giờ kết thúc, kể cả thí sinh thêm 25 % giờ (37,5 phút) và máy đã tắt|Đạt
15|Chấm Writing ẩn danh, điểm Speaking, bảng điểm, CSV|Điểm 6 tự động + 7 Writing = 13/60, trạng thái đã chấm xong; Speaking 30/45 quy đổi thành 124; CSV đọc đúng tiếng Việt, có các cột thang Cambridge|Đạt
16|Yêu cầu xóa thí sinh, quản trị viên khác duyệt|Chưa xóa khi chờ duyệt; người yêu cầu không có nút duyệt; sau khi quản trị viên thứ hai duyệt thì xóa và ghi nhật ký|Đạt
17|Giám thị xem nhật ký thao tác, tải CSV|Nhật ký 27 dòng gồm báo bắt đầu, tạm dừng, làm tiếp, cộng giờ, thông báo, *End in*, *End now*, tua Listening; CSV 27 dòng kèm tiêu đề|Đạt
|end|
Các số đo trong bảng cho thấy lệnh của giám thị đến màn hình thí sinh trong khoảng 145 đến 224 mili giây, kể cả khi áp dụng cho cả phòng. Tình huống 14 kiểm chứng trực tiếp nguyên tắc đồng hồ do máy chủ tính, vì thí sinh đã tắt máy vẫn được nộp bài đúng giờ.
## Kết quả kiểm thử tải
Kiểm thử tải được chạy trên một máy xách tay (16 luồng CPU, 7,6 GiB RAM dành cho Docker), trên đó cả hệ thống chạy bằng Docker Desktop. Công cụ kiểm thử chạy trên cùng máy, nên số liệu đã bao gồm cả tải của công cụ. Mỗi thí sinh ảo đăng nhập, nhập PIN, bắt đầu phần Reading & Writing A2, gửi nhịp kiểm tra kết nối 5 giây một lần, lưu một câu trả lời mỗi 4 đến 12 giây và giữ kết nối SignalR. Một giám thị ảo theo dõi qua SignalR, tải lại trang theo dõi mỗi 3 giây và giữa giờ tạm dừng rồi cho làm tiếp một nhóm thí sinh. Thời gian phản hồi được báo theo phân vị (percentile) 50, 95 và 99, ký hiệu p50, p95 và p99. Bảng 4.5 là kết quả của bốn lần chạy.
|table| Kết quả kiểm thử tải | 4.4,3,3,3,3.1
Chỉ số|100 thí sinh, 5 phút (22/10)|200 thí sinh, 3 phút (22/10)|200 thí sinh, 3 phút (07/11)|200 thí sinh, 3 phút (25/11)
Vào được bài; còn kết nối lúc kết thúc|100/100; 100/100|200/200; 200/200|200/200; 200/200|200/200; 200/200
Tổng yêu cầu; lỗi máy chủ|10 322; 0|12 681; 0|12 676; 0|12 654; 0
Lưu câu trả lời p50 / p95 / p99 (ms)|17 / 27 / 35|15 / 29 / 73|11 / 17 / 55|19 / 65 / 177
Nhịp kiểm tra kết nối p50 / p95 / p99 (ms)|118 / 144 / 161|208 / 334 / 546|219 / 265 / 502|406 / 523 / 849
Đăng nhập thí sinh p50 / p95 (ms)|65 / 72|65 / 76|59 / 71|100 / 124
Lệnh tạm dừng đến màn hình thí sinh|10/10, 16 ms|20/20, 73 ms|20/20, tối đa 20 ms|20/20, 128 ms
Số lần SignalR phải nối lại|0|0|0|0
|end|
Các lần lưu bị từ chối với mã 409 (3, 6, 5 và 11 lần) đều thuộc những thí sinh đang bị tạm dừng, đúng như thiết kế. Lần chạy ngày 25/11 kiểm tra lại mã nguồn sau đợt hoàn thiện. Trong lần này, máy xách tay còn chạy một trình giả lập Android chiếm khoảng hai lõi CPU, nên mọi thời gian phản hồi đều cao hơn lần 07/11. Riêng việc đăng nhập, vốn chủ yếu là băm mật khẩu và không đổi mã nguồn, cũng chậm khoảng 1,7 lần. Số yêu cầu và số lỗi máy chủ vẫn như cũ. ITC khuyến nghị độ trễ trên màn hình không quá 2 giây [@itc, tr. 15]. Thao tác mà thí sinh cảm nhận trực tiếp là lưu câu trả lời, với phân vị 99 cao nhất là 177 ms. Nhịp kiểm tra kết nối là yêu cầu chạy nền, nên độ trễ dưới 1 giây không ảnh hưởng đến thao tác. Còn lệnh của giám thị mất từ 16 đến 128 ms để đến màn hình thí sinh.
Phòng máy của trung tâm nhỏ hơn nhiều so với 100 đến 200 máy, nên một máy chủ bình thường là đủ và còn dư nhiều. Trong lần chạy ngày 22/10, CPU của máy chủ ứng dụng ổn định ở mức 15 đến 25 % trong lúc làm bài và chỉ lên cao lúc đăng nhập (do băm mật khẩu) và lúc tạo danh sách thí sinh. Các số liệu trên có được sau khi sửa hai lỗi cấu hình do chính kiểm thử tải phát hiện. Hai lỗi này được phân tích ở mục 4.5.
## Khó khăn và cách khắc phục
Phần này trình bày những khó khăn kỹ thuật chính đã gặp trong quá trình thực hiện, nguyên nhân của từng khó khăn và cách khắc phục. Phần lớn các khó khăn không lộ ra khi kiểm thử từng thành phần riêng lẻ. Chúng chỉ xuất hiện khi nhiều người dùng thao tác đồng thời, khi tải tăng cao hoặc khi các thao tác chen nhau.
### Kết nối thời gian thực bị cắt định kỳ
Lần kiểm thử tải đầu tiên cho thấy trang giám thị nhận được rất ít cập nhật. Trong 5 phút, giám thị chỉ nhận 25 sự kiện thay vì khoảng 600. Nguyên nhân là máy chủ cắt mọi kết nối SignalR im lặng quá 12 giây, trong khi thư viện SignalR phía trình duyệt mặc định 15 giây mới gửi một thông điệp giữ kết nối. Khi phòng thi yên lặng, cả trang giám thị lẫn màn hình thí sinh bị cắt rồi nối lại liên tục.
Cách khắc phục là đặt chu kỳ giữ kết nối của trình duyệt là 4 giây, đúng nguyên tắc chu kỳ này phải nhỏ hơn hoặc bằng một nửa thời gian chờ của máy chủ (mục 2.3.3). Một kiểm thử được bổ sung để giữ ràng buộc giữa hai giá trị, tránh việc một thay đổi cấu hình sau này vô tình phá vỡ nó. Sau khi sửa, cả ba lần chạy tải trong Bảng 4.5 đều không phải nối lại SignalR lần nào.
### Cạn kho kết nối cơ sở dữ liệu
Khi tăng lên 200 thí sinh, máy chủ báo 1 485 lỗi hết kết nối tới cơ sở dữ liệu, với kho kết nối mặc định là 100 kết nối. Nguyên nhân nằm ở trình điều khiển MySQL của Oracle. Trình điều khiển này thực hiện các lệnh bất đồng bộ theo kiểu chặn luồng, nên mỗi lệnh giữ một luồng và một kết nối trong suốt thời gian chạy. Khi 200 thí sinh cùng gửi nhịp kiểm tra kết nối và lưu câu trả lời, số kết nối cần dùng cùng lúc vượt quá 100. Trình điều khiển thay thế là Pomelo lại chưa có bản cho EF Core 10 (Bảng 3.22), nên không thể giải quyết bằng cách đổi trình điều khiển.
Vì vậy, cấu hình được điều chỉnh ở ba chỗ. Kho kết nối được tăng lên 250 kết nối, giới hạn của MySQL được nâng lên 300 kết nối, và số luồng tối thiểu của máy chủ được đặt là 200 để các lệnh bị chặn không phải chờ cấp thêm luồng. Sau điều chỉnh, lần chạy 200 thí sinh không còn lỗi máy chủ nào.
### Lỗi chỉ xuất hiện khi nhiều người cùng thao tác
Lần thi thử ngày 26/10 tìm được bốn lỗi, được liệt kê trong Bảng 4.6. Cả bốn lỗi đều lọt qua kiểm thử tự động trước đó, vì chúng chỉ xuất hiện khi nhiều người dùng thao tác thật trên giao diện.
|table| Các lỗi tìm được trong buổi thi thử ngày 26/10/2026 và cách sửa | 7,2,7.5
Lỗi|Mức độ|Cách sửa
Thí sinh đăng nhập ở máy khác thì máy cũ vẫn mở bài, chỉ báo mất kết nối, vì trình duyệt coi mã 401 là mất mạng|Cao|Máy cũ về màn hình đăng nhập kèm thông báo lý do
*End now* đôi khi ghi lý do nộp bài là hết giờ, do giờ kết thúc được làm tròn đến giây gần nhất|Trung bình|Làm tròn xuống
Thông báo đã đọc ở Reading & Writing bật lại khi sang Listening, vì trạng thái đã đọc lưu theo phần thi|Trung bình|Lưu theo thí sinh và kỳ thi
Cột *Audio* của trang giám thị vẫn chạy sau khi thí sinh đã nộp|Thấp|Để trống khi đã nộp
|end|
Lỗi nghiêm trọng nhất là lỗi thứ nhất. Khi thí sinh đăng nhập ở máy khác, máy chủ đã từ chối máy cũ bằng mã 401, nhưng trình duyệt lại hiểu mã này là mất mạng nên vẫn để bài mở. Lỗi thứ hai cho thấy một chi tiết nhỏ trong cách làm tròn thời gian cũng có thể làm sai lý do nộp bài, vốn là một phần của hồ sơ thi. Một điểm được ghi nhận nhưng không sửa là khi thí sinh chuyển sang máy mới, thông báo đã đọc ở máy cũ hiện lại một lần, vì trạng thái đã đọc nằm trong trình duyệt.
### Các yêu cầu chen nhau
Trong phòng thi, nhiều yêu cầu có thể tác động lên cùng một dữ liệu vào cùng một lúc. Hai tình huống điển hình được xử lý trong đợt sửa lỗi sau rà soát.
Ở tình huống thứ nhất, thí sinh bấm đúp nút bắt đầu hoặc mở hai thẻ trình duyệt, nên hai yêu cầu bắt đầu cùng một phần thi đến máy chủ gần như đồng thời. Cả hai yêu cầu đều đọc thấy chưa có lượt làm bài và cùng định tạo một lượt mới. Cơ sở dữ liệu chỉ cho phép một lượt làm bài cho mỗi cặp thí sinh và phần thi, nên yêu cầu đến sau vấp ràng buộc duy nhất này. Khi đó, thay vì báo lỗi, máy chủ đọc lại và trả về chính lượt làm bài mà yêu cầu kia vừa tạo. Để yêu cầu đến sau nhìn thấy được dòng vừa ghi, giao dịch chạy ở mức cô lập READ COMMITTED thay vì giữ ảnh chụp dữ liệu cũ. Kết quả là chỉ có một lượt làm bài, và cả hai yêu cầu nhận cùng một giờ kết thúc.
Ở tình huống thứ hai, kỳ thi bị đổi trạng thái, chẳng hạn bị đóng, đúng lúc một thí sinh bấm bắt đầu. Trước khi sửa, cả hai yêu cầu đều qua được bước kiểm tra rồi cùng ghi. Cách khắc phục là khóa dòng của kỳ thi như đã mô tả ở mục 3.4.6. Đổi trạng thái giữ khóa độc quyền, còn bắt đầu phần thi giữ khóa chia sẻ, nên hai yêu cầu buộc phải chờ nhau. Khóa luôn được lấy theo cùng một thứ tự, bắt đầu từ kỳ thi, rồi đến thí sinh và lượt làm bài. Nếu MySQL vẫn báo chờ khóa quá hạn hoặc khóa chết, máy chủ trả mã 409 để trình duyệt tự thử lại. Cũng trong đợt này, các lỗi xung đột tương tranh trước đây trả mã 500 nay được đọc lại, thử lại và trả mã 409 kèm câu giải thích.
Khó khăn lớn của loại lỗi này là rất khó tái hiện bằng kiểm thử thông thường, vì hai yêu cầu phải gặp nhau đúng vào khoảng giữa bước kiểm tra và bước ghi. Các kiểm thử tích hợp cho những tình huống này giữ một yêu cầu lại ngay trước lúc ghi trong 2 giây, chạy yêu cầu kia trong khoảng đó, rồi kiểm tra rằng yêu cầu kia thực sự đã phải chờ. Thiếu bước kiểm tra cuối cùng này, kiểm thử có thể đạt mà không hề kiểm tra được tình huống chen nhau.
### Những vấn đề tìm ra khi rà soát toàn hệ thống
Ngày 02/11, khi các chức năng đã đủ và mọi kiểm thử đều đạt, toàn hệ thống được rà soát theo năm mảng để tìm những tình huống mà kiểm thử chưa phủ. Đợt rà soát ghi nhận 42 vấn đề chia thành bốn nhóm, cộng với một nhóm tài liệu chưa khớp mã nguồn (Bảng 4.7). Các vấn đề được sửa theo tám nhóm công việc từ 02/11 đến 07/11. Mỗi nhóm có một lần xem xét riêng, sau đó toàn bộ thay đổi được xem xét lại một lần nữa.
|table| Kết quả rà soát ngày 02/11/2026 | 4,1.6,10.9
Nhóm|Số vấn đề|Ví dụ và cách sửa
Mất bài hoặc sai kết quả|13|Đổi đáp án sát lúc bị tạm dừng làm hàng đợi tự lưu đóng hẳn: nay giữ và gửi lại sau khi cho làm tiếp. *End now* với thí sinh đang ở thời gian cộng thêm làm giờ kết thúc dài ra: nay chỉ rút ngắn. Giám khảo không thấy đề Writing: nay hiện đề của đúng mã đề. Đề trắng xuất bản được với đáp án giữ chỗ: nay bắt buộc đặt đáp án, và quản trị viên sửa được đáp án đề đã xuất bản kèm chấm lại.
Luồng bất thường|16|Trạng thái kỳ thi đổi tùy ý: nay chỉ theo các chuyển hợp lệ, có xác nhận hai lần, khóa dòng kỳ thi. Chưa có chức năng kết thúc ở cấp bài thi: nay có *End the whole test* và *Use test end time*. Xung đột tương tranh trả lỗi 500: nay đọc lại, thử lại, trả 409 kèm câu giải thích.
Phân quyền và bảo mật|7|Đổi vai trò không gỡ phân công cũ: nay gỡ, và mọi bộ lọc yêu cầu vai trò hiện tại. Thí sinh đọc được đề trước khi bắt đầu: nay đề và tệp chỉ được phục vụ khi có lượt làm bài đang chạy. Giám khảo tải được tệp Reading, Listening: nay chỉ tệp Writing.
Triển khai|6|Docker luôn chạy môi trường phát triển với tài khoản demo công khai: nay có bản thi thật qua HTTPS, MySQL chỉ mở ở 127.0.0.1, container không chạy bằng root, thêm chỉ mục cho các truy vấn quét.
Tài liệu|–|Sửa các mô tả trong tài liệu dự án không còn khớp với mã nguồn
|end|
Nhóm luồng bất thường có nhiều vấn đề nhất (16), tiếp theo là nhóm mất bài hoặc sai kết quả (13). Điều này cho thấy các kiểm thử trước đó chủ yếu phủ luồng chính, còn những tình huống như đổi đáp án sát lúc bị tạm dừng hay đổi vai trò giữa chừng chưa được tính đến. Một số quyết định cũng được đưa ra trong khi sửa. *Resume* nay từ chối thí sinh ngoại tuyến giống như *Pause*. Sau lệnh kết thúc cả bài thi, thí sinh chưa bắt đầu phần sau sẽ không bắt đầu được cho tới khi giám thị hoàn tác. Điểm Writing được lưu thành lịch sử thay vì một dòng duy nhất. Khi kỳ thi được mở, số máy dạng chữ như “A3” bị từ chối thay vì bị ghi đè. Sau đợt sửa, toàn bộ kiểm thử, buổi thi thử và kiểm thử tải đều được chạy lại và đều đạt (mục 4.2 đến 4.4).
## Đánh giá
### Đối chiếu với mục tiêu
Bảng 4.8 đối chiếu kết quả với các mục đích cụ thể đã nêu ở phần Mở đầu.
|table| Đối chiếu kết quả với mục đích nghiên cứu | 6,10.5
Mục đích|Kết quả
Tìm hiểu quy trình tổ chức, giám sát buổi thi Cambridge trên máy, viết đặc tả yêu cầu|Bản đặc tả yêu cầu, ghi chép giao diện bài thi mẫu A2 và B1, các quy tắc nghiệp vụ có dẫn nguồn (mục 3.2)
Thiết kế cơ sở dữ liệu và kiến trúc cho nhiều kỳ thi dùng chung bộ máy|28 bảng; loại bài thi và phần thi là dữ liệu; B1 được thêm mà không sửa bộ máy (mục 3.3, 3.4)
Màn hình thí sinh tái hiện bố cục, định dạng câu hỏi của A2 và B1|Sáu kiểu tương tác, đủ các part của năm phần thi trên máy, các tiện ích của thí sinh (mục 4.1.1)
Cổng nhân viên cho quản trị viên, giám thị, giám khảo; giám sát thời gian thực|Lệnh giám thị đến màn hình thí sinh dưới 0,25 giây trong thi thử và dưới 0,15 giây trong kiểm thử tải (mục 4.3, 4.4)
Kiểm thử chức năng, kiểm thử tải, buổi thi thử|1781 kiểm thử tự động đạt; 200 thí sinh không lỗi máy chủ; thi thử 17/17 tình huống
|end|
Yêu cầu phi chức năng về quy mô đặt ra là khoảng 100 thí sinh, trong khi hệ thống phục vụ được 200 thí sinh đồng thời. Các yêu cầu về đồng hồ, không mất bài và phát hiện mất kết nối đều được kiểm chứng trong buổi thi thử, ở các tình huống 5, 6, 8 và 14. Các mục đích đề ra đều đạt trong môi trường thử nghiệm, còn mức độ đạt trong phòng máy thật còn phụ thuộc vào những hạn chế được nêu dưới đây.
### Những điểm chưa làm được và hạn chế
Hạn chế đầu tiên nằm ở phạm vi kỳ thi. IELTS đã được chuyển sang đồ án tốt nghiệp hoặc báo cáo thực tập, vì định dạng đề IELTS ít cố định hơn A2 và B1, và việc làm IELTS chỉ bắt đầu sau khi hai kỳ thi này hoàn thành. B2 First, C1 Advanced, C2 Proficiency và phần Speaking cũng nằm ngoài phạm vi, trong đó phần Speaking thi trực tiếp với giám khảo chứ không làm trên máy; hệ thống chỉ nhận điểm Speaking do quản trị viên nhập để tính điểm tổng. Điểm trên thang Cambridge English Scale được quy đổi theo bảng dành cho đề luyện tập của Cambridge, nên chỉ có giá trị tham khảo về mức sẵn sàng của học viên, không thay cho điểm của kỳ thi thật.
Hạn chế quan trọng nhất là hệ thống chưa được thử trên phòng máy thật của trung tâm. Kiểm thử tải chạy trên một máy xách tay, buổi thi thử chạy trên một máy, và các thí sinh trong buổi thi thử đều là trình duyệt tự động. Vì vậy tai nghe, loa, Safe Exam Browser và mạng cục bộ thật chưa được thử. Đây là việc cần làm trước khi dùng hệ thống cho một kỳ thi thật. Cũng vì lý do này, đồ án chưa có đánh giá trải nghiệm của người dùng thật, tức học viên và giám thị của trung tâm, bằng khảo sát.
Hệ thống còn ba hạn chế kỹ thuật đã rõ nguyên nhân. Hai hạn chế khác được nêu trong lần rà soát, gồm câu trả lời còn trong khoảng chờ tự lưu bị từ chối sau *End now* và thông báo đã đọc hiện lại khi thí sinh chuyển máy, đã được khắc phục ngày 25/11: máy chủ vẫn nhận câu trả lời trong 5 giây sau *End now* rồi chấm lại bài, và mốc thông báo đã đọc được lưu trên máy chủ.
- Máy chủ chỉ kiểm tra khóa cấu hình của SEB, chưa kiểm tra khóa trình duyệt (Browser Exam Key). Khóa cấu hình chứng minh trình duyệt dùng đúng tệp cấu hình, nhưng không chứng minh đó là ứng dụng SEB thật; người có tệp cấu hình và tự thêm được tiêu đề vào yêu cầu vẫn có thể vượt qua. Trong phòng máy đã khóa bằng SEB, việc này khó thực hiện, nhưng vẫn là một giới hạn.
- Hệ thống chạy trên một máy chủ, chưa hỗ trợ nhiều máy chủ, vì phạm vi đặt ra từ đầu là phòng máy của một trung tâm. Máy thí sinh được hỗ trợ là Windows với Chrome, Edge hoặc Safe Exam Browser.
- Bộ chuyển đề phụ thuộc bố cục PDF của Cambridge, vì các bộ đọc dựa vào vị trí dòng chữ trong bố cục này. Đề do trung tâm tự soạn theo bố cục khác cần được nhập bằng gói đề hoặc soạn trên web.
## Tiểu kết chương
Chương 4 đã trình bày kết quả của hệ thống qua giao diện, kiểm thử tự động, buổi thi thử và kiểm thử tải. Hệ thống đáp ứng cả sáu nhóm yêu cầu chức năng; 1781 kiểm thử tự động đều đạt, buổi thi thử đạt 17/17 tình huống và kiểm thử tải với 200 thí sinh không có lỗi máy chủ. Những khó khăn lớn nhất, gồm kết nối thời gian thực bị cắt, cạn kho kết nối, lỗi chỉ lộ ra khi nhiều người thao tác và các yêu cầu chen nhau, đều được tìm ra nhờ kiểm thử ở mức hệ thống và đã được khắc phục. Hạn chế lớn nhất còn lại là hệ thống chưa được thử trong phòng máy thật của trung tâm. Chương 5 tóm tắt kết luận và hướng phát triển.

# KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN
Chương này tóm tắt kết quả đạt được, những đóng góp của đồ án và các hướng phát triển tiếp theo.
## Kết luận
Đồ án đã xây dựng một website tổ chức trọn một buổi thi trên máy cho hai kỳ thi A2 Key for Schools và B1 Preliminary for Schools tại phòng máy của Trung tâm Anh ngữ Victory, từ chuẩn bị đề, tạo kỳ thi, cấp tài khoản thí sinh, làm bài, giám sát, xử lý sự cố đến chấm điểm và xuất kết quả. Hệ thống gồm máy chủ ASP.NET Core trên .NET 10, cơ sở dữ liệu MySQL 28 bảng, giao diện React và kênh thời gian thực SignalR, được cài đặt bằng một lệnh Docker Compose ở hai chế độ demo và thi thật.
Về kiểm chứng, 1500 kiểm thử máy chủ, 206 kiểm thử giao diện, 69 kiểm thử bộ chuyển đề và 6 kịch bản đầu-cuối đều đạt. Buổi thi thử 17 tình huống đạt 17/17. Kiểm thử tải với 200 thí sinh đồng thời không có lỗi máy chủ; thời gian lưu câu trả lời và thời gian lệnh giám thị đến màn hình thí sinh đều dưới 0,2 giây, thấp hơn nhiều so với ngưỡng 2 giây của ITC.
Đồ án có các đóng góp chính sau.
- Tái hiện quy trình giám sát của kỳ thi Cambridge trên máy với đồng hồ do máy chủ tính theo các đại lượng tương đối, để các thao tác tạm dừng, cộng giờ, sửa giờ kết thúc, kết thúc cả bài thi và tua phần nghe kết hợp với nhau đúng quy tắc.
- Mô hình hóa loại bài thi và phần thi bằng dữ liệu, giúp B1 dùng chung bộ máy với A2.
- Thuật toán trộn từ hai đến sáu đề gốc theo part thành nhiều mã đề và chia mã đề theo vị trí máy sao cho tám máy xung quanh một máy luôn khác mã đề.
- Các cơ chế theo yêu cầu riêng của trung tâm, gồm duyệt chéo giữa hai quản trị viên, mỗi người chỉ thấy phần việc của mình, chia bài Writing ngẫu nhiên và chấm ẩn danh.
- Bộ chuyển đề từ tệp PDF của Cambridge sang gói đề cho năm phần thi, thay cho hệ thống nhập đề cũ của trung tâm.
- Kịch bản thi thử tự động bằng trình duyệt, chạy lại được sau mỗi lần sửa lỗi.
## Hướng phát triển
- Thử hệ thống trên phòng máy thật của trung tâm với tai nghe, Safe Exam Browser và mạng cục bộ, có học viên và giám thị tham gia, kèm khảo sát ý kiến người dùng.
- Mở rộng sang IELTS (các kiểu câu nhiều đáp án, ghi nhãn bản đồ, Writing Task 1 và 2) trong đồ án tốt nghiệp hoặc báo cáo thực tập, sau đó là B2 First, C1 Advanced, C2 Proficiency.
- Nhận các câu trả lời còn trong khoảng chờ tự lưu khi giám thị bấm *End now*, và kiểm tra thêm khóa trình duyệt của SEB cho từng phiên bản SEB được phép.
- Gộp các lần ghi nhịp kiểm tra kết nối thành một lệnh mỗi 1 đến 2 giây nếu cần phục vụ phòng thi lớn hơn.
- Bổ sung thống kê kết quả theo câu hỏi để trung tâm đánh giá độ khó của đề.

@refs DANH MỤC TÀI LIỆU THAM KHẢO

#! PHỤ LỤC
## Danh sách API
Bảng PL.1 liệt kê các endpoint của máy chủ theo nhóm chức năng. Cột quyền ghi tác nhân được phép gọi, còn phạm vi dữ liệu được lọc thêm theo người tạo và phân công như mô tả ở mục 3.2.1.
|table| Danh sách endpoint của API | 1.9,8.4,6.2
Phương thức|Đường dẫn|Quyền và ghi chú
POST|/api/staff/auth/login, /logout|Đăng nhập, đăng xuất nhân viên
GET|/api/staff/auth/me|Nhân viên: vai trò, mảng
POST|/api/candidate/auth/login, /pin, /logout|Đăng nhập hai bước của thí sinh
GET|/api/candidate/auth/me|Thí sinh
PUT|/api/candidate/auth/preferences|Thí sinh: cỡ chữ, tương phản
PUT|/api/candidate/auth/messages-read|Thí sinh: mốc thông báo đã đọc
GET|/api/candidate/sections|Thí sinh đã nhập PIN
GET|/api/candidate/sections/{id}/paper|Chỉ khi có lượt làm bài đang chạy
POST, GET|/api/candidate/sections/{id}/attempt|Bắt đầu hoặc tiếp tục; đọc đồng hồ, câu trả lời
POST|/api/candidate/sections/{id}/heartbeat|Nhịp kiểm tra kết nối, mỗi 5 giây
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
PUT|/api/admin/papers/{id}/questions/{n}/answers|Quản trị viên sửa đáp án đề đã xuất bản
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
GET, POST, PUT|/api/admin/users, /users/{id}|Quản trị viên: tài khoản nhân viên
GET|/api/staff/exams, /exams/{id}|Kỳ thi người dùng được thấy
GET|/api/staff/exams/{id}/room-plan, /login-slips|Sơ đồ phòng, phiếu đăng nhập
GET|/api/staff/exams/{id}/sections/{sid}/monitor|Bảng theo dõi
POST|/api/staff/exams/{id}/sections/{sid}/actions|*Pause*, *Resume*, cộng giờ, *End now*/*at*/*in*, *Use test end time*, tua phần nghe
POST|/api/staff/exams/{id}/end-all, /use-test-end-time|Kết thúc cả bài thi và hoàn tác
POST|/api/staff/exams/{id}/sections/{sid}/announce-start|Báo bắt đầu
GET, POST|/api/staff/exams/{id}/sections/{sid}/warnings, /warnings/{wid}/resolve|Cảnh báo
GET, POST|/api/staff/exams/{id}/messages|Thông báo cho thí sinh
GET|/api/staff/exams/{id}/results|Quản trị viên: bảng điểm, thang Cambridge English Scale
PUT|/api/staff/exams/{id}/candidates/{cid}/speaking|Quản trị viên: điểm Speaking
GET|/api/staff/exams/{id}/actions|Người giám sát phiên: nhật ký thao tác của giám thị
GET|/api/staff/marking, /api/staff/attempts/{id}/marking|Giám khảo: bài được chia, bài cần chấm
POST|/api/staff/attempts/{id}/writing-marks|Giám khảo: lưu điểm Writing
GET, POST|/api/staff/approvals, /approvals/{id}/approve, /reject, /cancel|Duyệt chéo
WS|/hubs/staff, /hubs/candidate|Hub SignalR
GET|/health|Kiểm tra máy chủ và cơ sở dữ liệu
|end|
## Tóm tắt cài đặt
Hướng dẫn đầy đủ nằm trong tài liệu cài đặt của kho mã nguồn, gồm cài đặt cho phòng thi qua mạng cục bộ, cấu hình tường lửa, sao lưu và khôi phục cơ sở dữ liệu, và xử lý sự cố. Bảng PL.2 tóm tắt các bước chính.
|table| Các bước cài đặt hệ thống | 3.5,13
Bước|Thao tác
Chuẩn bị máy chủ|Cài Docker Desktop (bật WSL 2) và Git trên máy Windows 10/11 64 bit hoặc Linux, CPU 4 nhân, RAM 8 GB, trống 20 GB ổ đĩa
Cấu hình|Tải mã nguồn, sao tệp docker/.env.example thành docker/.env và đổi mọi mật khẩu trong đó
Bản demo|Chạy docker compose -f docker/docker-compose.yml --env-file docker/.env up -d --build, rồi mở http://localhost:8080 (thí sinh) và http://localhost:8080/staff (nhân viên)
Bản thi thật|Đặt tài khoản quản trị viên đầu tiên trong docker/.env, tạo chứng chỉ cho địa chỉ IP của máy chủ vào thư mục docker/certs, rồi chạy docker compose -p examprep-prod -f docker/docker-compose.yml -f docker/docker-compose.prod.yml --env-file docker/.env up -d --build; máy thí sinh mở địa chỉ https của máy chủ
Kiểm tra|Mở trang /health; kết quả Healthy nghĩa là máy chủ và cơ sở dữ liệu đều đang chạy
|end|
Bản demo tạo sẵn hai kỳ thi demo và các tài khoản trong Bảng PL.3. Các mật khẩu này công khai trong tệp README của kho mã nguồn, chỉ dùng cho bản demo và không bao giờ được dùng cho kỳ thi thật.
|table| Tài khoản của bản demo | 4.4,2.4,3.3,6.4
Vai trò|Địa chỉ|Tên đăng nhập|Mật khẩu
Quản trị viên|/staff|hoanglong|19072005
Quản trị viên thứ hai|/staff|admin2|19072005
Quản trị viên theo mảng (bốn mảng)|/staff|quantri|19072005
Giám khảo|/staff|giamkhao|19072005
Giám thị|/staff|giamthi|victory-invig
Thí sinh A2|/|hoanglong|19072005, mã PIN 4821
Thí sinh B1|/|hoanglongb1|19072005, mã PIN 4821
|end|
