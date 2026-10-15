# PROJECT: BOOKSTORE CHAIN MANAGEMENT SYSTEM

## 1. Vai trò và mục tiêu dự án
Bạn là một Senior Software Architect và Senior .NET Developer, có kinh nghiệm thiết kế hệ thống quản lý bán lẻ (Retail Management System), Point of Sale (POS), thương mại điện tử (E-commerce), quản lý kho và báo cáo tài chính.
Hãy thiết kế và phát triển một phần mềm quản lý chuỗi cửa hàng sách chuyên nghiệp, có khả năng vận hành thực tế, bảo mật, dễ bảo trì, dễ mở rộng và có thể triển khai lên môi trường Production.
Hệ thống phải hỗ trợ:
- Quản lý tập trung toàn bộ chuỗi cửa hàng.
- Quản lý hoạt động riêng biệt của từng cửa hàng.
- Quản lý sách, văn phòng phẩm, hàng lưu niệm và các mặt hàng bán lẻ khác.
- Bán hàng trực tiếp tại quầy thông qua hệ thống POS.
- Bán hàng trực tuyến thông qua website thương mại điện tử.
- Quản lý kho, nhập hàng, xuất hàng, chuyển hàng giữa các cửa hàng.
- Quản lý nhân viên, phân quyền, ca làm việc và doanh thu.
- Quản lý chi phí vận hành, công nợ và các báo cáo quản trị.
- Tự động tổng hợp báo cáo, gửi Email và thông báo Telegram theo lịch hoặc theo sự kiện.
Không chỉ tạo giao diện minh họa hoặc CRUD đơn giản. Hãy xây dựng ứng dụng có nghiệp vụ nhất quán, dữ liệu được lưu trữ thực tế, có kiểm soát lỗi, xác thực, phân quyền, kiểm thử và hướng dẫn triển khai.
## 2. Công nghệ và kiến trúc
Sử dụng các công nghệ sau:
- Ngôn ngữ: C#.
- Nền tảng: ASP.NET Core trên phiên bản .NET LTS ổn định, tương thích với toàn bộ thư viện sử dụng.
- Frontend quản trị: ASP.NET Core MVC với Razor Views và JavaScript.
- Frontend bán hàng trực tuyến: ASP.NET Core MVC, giao diện Responsive, tối ưu cho máy tính và điện thoại.
- Backend: ASP.NET Core Web API khi cần thiết, phục vụ các giao tiếp nội bộ và tích hợp bên ngoài.
- ORM: Entity Framework Core.
- Database: Microsoft SQL Server.
- Giao diện: Bootstrap 5, HTML5, CSS3 và JavaScript; có thể sử dụng thư viện UI phù hợp nếu mang lại lợi ích thực tế.
- Logging: Serilog.
- Background jobs: lựa chọn Hangfire hoặc một giải pháp phù hợp để chạy tác vụ định kỳ và xử lý công việc nền.
- Testing: xUnit và các thư viện kiểm thử tương thích.
- Version control: Git.
### Kiến trúc phần mềm
Sử dụng kiến trúc Modular Monolith làm kiến trúc mặc định. Tổ chức mã nguồn theo module nghiệp vụ, áp dụng các nguyên tắc SOLID, Separation of Concerns và Dependency Injection.
Có thể áp dụng CPhân chia các module thành những khu vực rõ ràng, chẳng hạn:
- Identity & Access Management.
- Organization & Store Management.
- Catalog & Product Management.
- Inventory Management.
- Purchasing & Suppliers.
- POS & Sales.
- E-commerce.
- Customer & Loyalty.
- Finance & Expenses.
- Workforce & Shift Management.
- Reporting & Notifications.
- Integration & Audit.
Yêu cầu:
- Các module có trách nhiệm rõ ràng.
- Hạn chế phụ thuộc chéo và tránh truy cập trực tiếp vào dữ liệu nội bộ của module khác khi không cần thiết.
- Không lạm dụng Repository Pattern hoặc Generic Repository nếu Entity Framework Core đã đáp ứng tốt nhu cầu.
- Sử dụng DTO/ViewModel phù hợp, không Có cấu hình môi trường Development, Staging và Production.
- Có Dependency Injection, Options Pattern và quản lý cấu hình an toàn.
- Tách cấu hình, nghiệp vụ và hạ tầng.
- Tạo solution và project structure rõ ràng, dễ mở rộng.
- Tạo file .gitignore chuẩn cho Visual Studio, .NET, Rider và các IDE liên quan.
- Không commit file build, thư mục bin/obj, file cấu hình chứa mật khẩu, token, connection string bí mật hoặc các thông tin nhạy cảm.
## 3. Quản lý tổ chức, chuỗi cửa hàng và người dùng
Hệ thống phải hỗ trợ nhiều chi nhánh/cửa hàng trong cùng một tổ chức.
### Quản lý cửa hàng
- Tạo, sửa, kích hoạt, tạm ngừng và ngừng hoạt động cửa hàng.
- Quản lý mã cửa hàng, tên, địa chỉ, thông tin liên hệ và người phụ trách.
- Quản lý giờ hoạt động và cấu hình riêng của từ Thiết lập kho hàng, quầy thu ngân và các thiết bị tại từng cửa hàng.
- Cho phép thiết lập chính sách giá, tồn kho và chi phí theo cửa hàng nếu cần.
- Theo dõi doanh thu, lợi nhuận ước tính, tồn kho và chi phí theo từng cửa hàng.
### Quản lý nhân viên
- Tạo tài khoản và hồ sơ nhân viên.
- Gán nhân viên vào một hoặc nhiều cửa hàng theo phạm vi được cấp phép.
- Phân công chức vụ, ca làm và trạng thái làm việc.
- Khóa, mở khóa và thu hồi quyền truy cập.
- Theo dõi lịch sử hoạt động của nhân viên.
### Phân quyền
Thiết kế RBAC với các vai trò mẫu:
- Sys Chain Owner.
- Chain Manager.
- Store Manager.
- Inventory Manager.
- Accountant.
- Cashier.
- Sales Staff.
- Customer.
Phân quyền phải kiểm soát cả chức năng và phạm vi dữ liệu: nhân viên cửa hàng chỉ được truy cập dữ liệu thuộc phạm vi được cấp quyền; quản lý chuỗi được xem dữ liệu tổng hợp toàn chuỗi theo quyền hạn.
Không chỉ ẩn nút trên giao diện. Phải kiểm tra quyền tại backend/API và ngăn truy cập trái phép bằng cách thay đổi URL hoặc gửi request trực tiếp.
Hỗ trợ xác thực an toàn, chính sách mật khẩu, khóa tài khoản khi đăng nhập sai nhiều lần, quản lý phiên đăng nhập và xác thực hai yếu tố cho các tài khoản quản trị khi phù ## 4. Quản lý danh mục sản phẩm
Không giới hạn sản phẩm ở sách. Hệ thống phải hỗ trợ nhiều nhóm hàng bán lẻ.
### Thông tin sản phẩm
- Mã sản phẩm/SKU.
- Tên sản phẩm.
- Mô tả và hình ảnh.
- Danh mục và danh mục cha.
- Thương hiệu, nhà xuất bản, tác giả đối với sách.
- ISBN đối với sách khi có.
- Barcode/mã vạch.
- Đơn vị tính.
- Giá vốn, giá bán và thuế suất áp dụng.
- Trạng thái kinh doanh.
- Cảnh báo sản phẩm sắp hết hàng.
- Thông tin kích thước, màu sắc, phiên bản hoặc thuộc tính biến thể khi cần.
Một sản phẩm có thể có nhiều mã vạch nếu nghiệp vụ yêu cầu. ISBN và barcode phải được quản lý riêng biệt theo quy tắc nghiệp vụ, không mặc định rằng mọi sản phẩm đều có ISBN.
### Quản lý giá
- Giá bán mặc định.
- Giá bán theo cửa hàng hoặc kênh bán hàng.
- Chương trình khuyến mại.
- Giá bán theo thời gian hiệu lực.
- Lịch sử thay đổi giá.
- Kiểm soát quyền sửa giá và giảm giá tại quầy.
### Nhập danh mục
- Nhập sản phẩm từ Excel/CSV.
- Kiểm tra dữ liệu trùng lặp và dữ liệu không hợp lệ.
- Hiển thị kết quả nhập, lỗi từng dòng và khả năng tải báo cáo lỗi.
- Xuất danh mục sản phẩm ra Excel.
## 5. Quản lý kho hàng
Mỗi cửa hàng có thể có một hoặc nhiều kho.
Các chức năng:
- Nhập kho từ nhà cung cấp.
- Xuất kho bán hàng.
- Xuất kho do hư hỏng, mất mát hoặc điều chỉnh được phê duyệt.
- Chuyển kho giữa các cửa hàng.
- Kiểm kê định kỳ và kiểm kê đột xuất.
- Theo dõi tồn kho theo SKU, barcode, kho và cửa hàng.
- Cảnh báo hàng tồn thấp, hàng tồn lâu và hàng bán chậm.
- Theo dõi lịch sử nhập, xuất và điều chỉnh tồn kho.

### Lương nhân viên

- Quản lý hàng đang vận chuyển giữa các kho.
- Hỗ trợ kiểm tra hàng trả lại.
Phải đảm bảo tồn kho không bị cập nhật sai khi nhiều nhân viên bán hàng đồng thời.
Sử dụng transaction, concurrency control và các cơ chế chống ghi nhận trùng phù hợp. Mọi biến động tồn kho phải có chứng từ hoặc bản ghi nghiệp vụ tương ứng.
Không cho phép âm kho mặc định, trừ khi được cấu hình và cấp quyền rõ ràng.
Tồn kho cần được thiết kế theo hướng có thể đối soát từ lịch sử giao dịch. Không cho phép sửa trực tiếp số lượng tồn mà không có nghiệp vụ điều chỉnh hợp lệ.
## 6. Quản lý nhà cung cấp và mua hàng
- Quản lý thông tin nhà cung cấp.
- Quản lý danh mục sản phẩm của từng nhà cung cấp.
- Tạo đơn đặt hàng mua.
- Theo dõi trạng thái đơn hàng.
- Nhận hàng toàn phần hoặc một phần.
- Ghi nhận hóa đơn mua hàng và chi phí nhập hàng.
- Theo dõi công nợ phải trả.
- Theo dõi lịch sử mua hàng và giá nhập.
- Hỗ trợ trả hàng nhà cung cấp.
- Báo cáo nhà cung cấp, thời gian giao hàng và biến động giá nhập.
Phân biệt rõ đơn đặt hàng, phiếu nhận hàng, hóa đơn nhà cung cấp và chứng từ thanh toán. Không tự động coi một đơn đặt hàng là hàng đã nhập kho.
## 7. Hệ thống POS bán hàng tại cửa hàng
Đây là module nghiệp vụ quan trọng, cần ưu tiên về tốc độ, tính ổn định và độ chính xác.
### Giao diện bán hàng
- Giao diện thu ngân đơn giản, thao tác nhanh.
- Tìm sản phẩm theo tên, SKU, ISBN hoặc barcode.
- Hỗ trợ máy quét mã vạch USB thông dụng hoạt động theo chế độ giả lập bàn phím (keyboard wedge).
- Khi quét mã, tự động tìm sản phẩm và thêm vào giỏ hàng.
- Cho phép thay đổi số lượng, áp dụng giảm giá và chọn khách hàng theo quyền được cấp.
- Hiển thị giá, thuế, tổng tiền và số tiền khách cần thanh toán.
- Hỗ trợ nhiều phương thức thanh toán.
- In hóa đơn hoặc xuất hóa đơn điện tử nếu được tích hợp với nhà cung cấp phù hợp.
- Hỗ trợ tra cứu và in lại hóa đơn theo quyền hạn.
Không bắt buộc mua thiết bị chuyên dụng để triển khai POS cơ bản. Thiết kế lớp tích hợp riêng nếu sau này cần máy in hóa đơn, ngăn kéo tiền, cân điện tử hoặc thiết bị đặc thù.
### Thanh toán
Hỗ trợ:
- Tiền mặt.
- Chuyển khoản ngân hàng VietQR hoặc QR thanh toán theo giải pháp tương thích.
- Thẻ ngân hàng thông qua đơn vị cung cấp dịch vụ.
- Kết hợp nhiều phương thức thanh toán trong một đơn.
- Ghi nhận tiền khách đưa và tiền thừa khi thanh toán tiền mặt.
Phân biệt tạo mã QR, ghi nhận yêu cầu thanh toán và xác nhận giao dịch thực tế. Không tự động xác nhận đã thanh toán chỉ vì mã QR được tạo hoặc khách hàng thông báo đã chuyển tiền.
### Quản lý đơn hàng
- Mã đơn hàng duy nhất.
- Chi tiết sản phẩm, số lượng, đơn giá và giảm giá.
- Thông tin nhân viên, cửa hàng, quầy và ca bán.
- Trạng thái thanh toán.
- Lịch sử giao dịch.
- Hủy đơn và hoàn tiền theo quy trình được phân quyền.
- Đổi trả hàng và ghi nhận chênh lệch tồn kho.
- Ngăn xử lý trùng khi request bị gửi lại.
- Lưu vết mọi thao tác nhạy cảm.
### Hóa đơn và chứng từ
Thiết kế chứng từ bán hàng nội bộ có mã duy nhất, hỗ trợ in hóa đơn bán lẻ. Nếu cần hóa đơn điện tử theo quy định Việt Nam, thiết kế module tích hợp riêng với nhà cung cấp hóa đơn điện tử; không tự coi hóa đơn in từ POS là hóa đơn điện tử hợp lệ.
## 8. Quản lý ca làm việc và đóng ca bắt buộc
Mỗi quầy thu ngân cần có quy trình mở ca và đóng ca rõ ràng.
### Mở ca
- Nhân viên đăng nhập và chọn quầy.
- Ghi nhận số tiền mặt đầu ca.
- Ghi nhận nhân viên phụ trách.
- Không cho phép một quầy có nhiều ca đang mở trái với cấu hình nghiệp vụ.
### Trong ca
- Ghi nhận các giao dịch bán hàng.
- Ghi nhận các khoản thu, chi tiền mặt tại quầy.
- Ghi nhận hoàn tiền và hủy giao dịch.
- Theo dõi tổng tiền theo từng phương thức thanh toán.
- Lưu lại nhân viên thực hiện các thao tác nhạy cảm.
### Đóng ca
Cuối ca, nhân viên phải thực hiện chức năng Đóng ca.
Hệ thống cần:
1. Tổng hợp doanh thu bán hàng trong ca.
2. Tổng hợp số đơn hoàn tất, đơn hủy, đơn hoàn trả.
3. Tính tiền mặt dự kiến trong ngăn kéo.
4. Tổng hợp tiền chuyển khoản, QR và các phương thức thanh toán khác.
5. Cho phép nhân viên nhập số tiền thực tế kiểm đếm.
6. Tính chênh lệch giữa số tiền thực tế và số tiền dự kiến.
7. Yêu cầu giải trình khi chênh lệch vượt ngưỡng cấu hình.
8. Khóa ca sau khi đóng thành công.
9. Tạo báo cáo đóng ca.
10. Đưa ca vào trạng thái chờ quản lý xác nhận nếu có chênh lệch hoặc dấu hiệu bất thường.
Sau khi đóng ca, không được sửa trực tiếp số liệu giao dịch thuộc ca. Mọi điều chỉnh phải thông qua nghiệp vụ riêng, có phân quyền và nhật ký kiểm toán.
Nếu xảy ra lỗi mạng hoặc lỗi gửi báo cáo, trạng thái đóng ca và trạng thái gửi thông báo phải được quản lý độc lập. Không yêu cầu nhân viên thực hiện lại giao dịch bán hàng chỉ vì email gửi thất bại.
### Báo cáo tự động cuối ca
Sau khi đóng ca thành công:
- Tạo báo cáo Excel theo mẫu chuẩn.
- Lưu báo cáo cùng thông tin ca và thời gian tạo.
- Gửi báo cáo qua Email cho quản lý cửa hàng và quản lý chuỗi theo cấu hình.
- Gửi thông báo Telegram qua bot đến nhóm hoặc tài khoản quản lý đã cấu hình.
- Bao gồm doanh thu, số đơn, doanh thu theo phương thức thanh toán, tiền đầu ca, tiền cuối ca, chênh lệch và các chỉ số cần thiết.
- Gửi file Excel đính kèm nếu phù hợp.
- Ghi nhận trạng thái gửi thành công/thất bại.
- Tự động thử gửi lại khi lỗi tạm thời.
- Không gửi trùng báo cáo khi tác vụ được thực thi nhiều lần.
- Có chức năng gửi lại báo cáo thủ công cho người có quyền.
Không đưa token Telegram, mật khẩu SMTP hoặc thông tin bí mật vào mã nguồn hay Git.
## 9. Quản lý chi phí và tài chính vận hành
Quản lý chi phí theo cửa hàng, thời gian và nhóm chi phí.
Các nhóm chi phí mặc định:
- Tiền thuê mặt bằng.
- Tiền điện.
- Tiền nước.
- Internet và viễn thông.
- Lương, thưởng và phụ cấp nhân viên.
- Chi phí vận chuyển.
- Chi phí bảo trì thiết bị.
- Chi phí marketing.
- Chi phí văn phòng phẩm.
- Chi phí khác do quản lý tự tạo.
### Chức năng
- Tạo, sửa, phân loại và vô hiệu hóa danh mục chi phí.
- Ghi nhận chi phí phát sinh.
- Quản lý ngày phát sinh, kỳ hạch toán, số tiền, cửa hàng, người ghi nhận và người phê duyệt.
- Đính kèm hóa đơn, hình ảnh hoặc chứng từ.
- Thiết lập chi phí định kỳ như tiền thuê mặt bằng, điện và nước.
- Nhắc lịch các khoản sắp đến hạn.
- Theo dõi trạng thái dự kiến, chờ duyệt, đã duyệt, đã thanh toán và bị từ chối.
- Quản lý ngân sách dự kiến theo tháng.
- So sánh ngân sách với chi phí thực tế.
- Báo cáo chi phí theo cửa hàng, nhóm chi phí và thời gian.
- Thiết lập hạn mức phê duyệt và cảnh báo chi phí bất thường.
Ở giai đoạn đầu, ưu tiên quản lý chi phí lương, thưởng, phụ cấp và trạng thái thanh toán. Nếu triển khai tính lương chi tiết, cần bổ sung chấm công, quy tắc tính lương, bảo hiểm, thuế và các quy định lao động liên quan.
Không được coi chi phí lương đã ghi nhận là tiền lương đã thanh toán nếu chưa có chứng từ thanh toán tương ứng.
### Báo cáo tài chính quản trị
- Doanh thu theo cửa hàng, ngày, tháng và năm.
- Giá vốn hàng bán.
- Lợi nhuận gộp ước tính.
- Chi phí vận hành.
- Lợi nhuận vận hành ước tính.
- Tình hình thu, chi.
- Công nợ nhà cung cấp.
- So sánh hiệu quả giữa các cửa hàng.
Phải phân biệt doanh thu, tiền thực thu, giá vốn, lợi nhuận và chi phí. Không coi tổng doanh thu là lợi nhuận.
Các báo cáo ban đầu phục vụ quản trị nội bộ, không mặc định thay thế hệ thống kế toán hoặc báo cáo thuế theo quy định.
## 10. Website thương mại điện tử
Xây dựng website cho khách hàng mua hàng trực tuyến.
### Khám phá sản phẩm
- Trang chủ, danh mục, tìm kiếm và lọc sản phẩm.
- Chi tiết sản phẩm với hình ảnh, giá, mô tả và tồn kho có thể bán.
- Tìm kiếm sách theo tên, tác giả, ISBN, nhà xuất bản và thể loại.
- Danh sách sản phẩm nổi bật, sản phẩm mới và khuyến mại.
- Sản phẩm liên quan và gợi ý mua kèm.
- Giao diện Responsive, tối ưu SEO cơ bản.
- Đường dẫn thân thiện SEO (slug).
- Metadata, sitemap và cấu trúc URL phù hợp.
### Giỏ hàng và đặt hàng
- Thêm, sửa, xóa sản phẩm trong giỏ hàng.
- Cho phép mua hàng không cần tài khoản.
- Đăng ký, đăng nhập và quản lý hồ sơ khách hàng.
- Nhập địa chỉ giao hàng.
- Tính phí vận chuyển theo cấu hình hoặc tích hợp đối tác vận chuyển.
- Áp dụng mã giảm giá theo điều kiện.
- Chọn cửa hàng xử lý đơn hoặc kho đáp ứng đơn hàng.
- Xác nhận đơn hàng và theo dõi trạng thái.
- Gửi email thông báo xác nhận đơn.
- Hỗ trợ hủy đơn và hoàn tiền theo quy định.
### Phương thức thanh toán
- Thanh toán khi nhận hàng (COD).
- Chuyển khoản ngân hàng hoặc VietQR.
- Cổng thanh toán trực tuyến thông qua nhà cung cấp hỗ trợ phù hợp tại Việt Nam.
Phải có quy trình xác nhận thanh toán đáng tin cậy, kiểm tra callback/webhook, chống xử lý lặp và đối soát giao dịch. Không lưu thông tin thẻ ngân hàng nhạy cảm trong hệ thống.
### Đồng bộ với cửa hàng
- Đơn online được quản lý tập trung.
- Đồng bộ tồn kho với POS theo cơ chế nhất quán.
- Có chính sách giữ hàng trong thời gian thanh toán nếu cần.
- Tránh bán vượt tồn kho khi nhiều khách đặt hàng đồng thời.
- Hỗ trợ lấy hàng tại cửa hàng nếu được bật.
- Theo dõi trạng thái chuẩn bị, giao hàng, hoàn tất và trả hàng.
- Quản lý nguồn đơn hàng: POS, website hoặc kênh tích hợp.
## 11. Quản lý khách hàng và chăm sóc khách hàng
- Lưu thông tin khách hàng theo nguyên tắc tối thiểu cần thiết.
- Lịch sử mua hàng.
- Phân nhóm khách hàng.
- Chương trình tích điểm.
- Quản lý điểm tích lũy, đổi điểm và lịch sử điều chỉnh.
- Mã giảm giá và voucher.
- Theo dõi yêu cầu đổi trả và khiếu nại.
- Hỗ trợ chính sách thành viên theo từng cấp độ.
- Kiểm soát chống lạm dụng khuyến mại và điểm thưởng.
Các thao tác cộng/trừ điểm phải có lịch sử giao dịch; không chỉnh sửa số dư trực tiếp mà thiếu chứng từ.
## 12. Báo cáo và Dashboard quản lý chuỗi
Xây dựng Dashboard dành cho quản lý với khả năng lọc theo thời gian, cửa hàng, nhóm hàng và kênh bán.
Các chỉ số chính:
- Tổng doanh thu.
- Số lượng đơn hàng.
- Giá trị đơn hàng trung bình.
- Giá vốn và lợi nhuận gộp ước tính.
- Chi phí vận hành.
- Lợi nhuận vận hành ước tính.
- Sản phẩm bán chạy.
- Sản phẩm bán chậm.
- Tình trạng tồn kho.
- Hàng sắp hết.
- Tỷ lệ hủy đơn và hoàn trả.
- So sánh hiệu quả giữa các cửa hàng.
- Ca chưa đóng và ca có chênh lệch.
- Công nợ nhà cung cấp.
- Chi phí vượt ngân sách.
Hỗ trợ:
- Báo cáo ngày, tuần, tháng, quý và năm.
- So sánh với kỳ trước.
- Xuất Excel.
- Lọc theo quyền truy cập.

- Phân trang, truy vấn hiệu quả và tránh tải toàn bộ dữ liệu không cần thiết.
Tất cả chỉ số phải có định nghĩa nghiệp vụ thống nhất, tránh sai lệch giữa Dashboard và báo cáo chi tiết.
## 13. Thông báo và tự động hóa

### Email
- Thông báo đóng ca.
- Gửi báo cáo doanh thu.
- Cảnh báo chênh lệch tiền mặt.
- Cảnh báo tồn kho thấp.
- Thông báo đơn hàng online.
- Thông báo chi phí chờ duyệt hoặc sắp đến hạn.
- Cảnh báo lỗi tích hợp quan trọng.
### Telegram
- Gửi thông báo tới nhóm quản lý.
- Gửi báo cáo tổng hợp và file Excel.
- Cảnh báo ca chưa đóng.
- Cảnh báo chênh lệch tiền mặt vượt ngưỡng.
- Cảnh báo doanh thu bất thường.
- Cảnh báo lỗi gửi báo cáo hoặc tác vụ nền.
### Tác vụ nền
- Tự động chạy báo cáo định kỳ.
- Gửi lại các thông báo thất bại.
- Phát hiện các ca chưa đóng theo chính sách.
- Tổng hợp dữ liệu báo cáo.
- Dọn dẹp dữ liệu tạm theo chính sách lưu trữ.
- Theo dõi trạng thái và lịch sử thực thi.
Các tác vụ phải có khả năng retry, logging, xử lý idempotency và không làm phát sinh giao dịch trùng lặp.
## 14. Bảo mật và kiểm toán
- Xác thực và phân quyền phía server.
- Chống CSRF cho các thao tác web phù hợp.
- Chống XSS, SQL Injection và các lỗi bảo mật phổ biến.
- Validate dữ liệu đầu vào.
- Sử dụng HTTPS trong Production.
- Quản lý secret thông qua User Secrets ở môi trường Development và biến môi trường hoặc secret manager ở Production.
- Không ghi mật khẩu, token hoặc thông tin thanh toán nhạy cảm vào log.
- Giới hạn tốc độ đối với các endpoint nhạy cảm khi phù hợp.
- Audit log cho đăng nhập, thay đổi quyền, giá bán, tồn kho, thanh toán, hoàn tiền, đóng ca và phê duyệt chi phí.
- Lưu thông tin người thao tác, thời gian, đối tượng bị tác động và nội dung thay đổi phù hợp.
- Hạn chế sửa hoặc xóa chứng từ nghiệp vụ đã hoàn tất.
- Thiết kế quy trình sao lưu, phục hồi và kiểm tra khả năng khôi phục dữ liệu.
- Tuân thủ các quy định hiện hành liên quan đến dữ liệu cá nhân, thương mại điện tử, hóa đơn và thanh toán tại Việt Nam.
## 15. Thiết kế database
Thiết kế database SQL Server theo nghiệp vụ thực tế.
Yêu cầu:
- Primary key, foreign key, unique constraint, check constraint và index phù hợp.
- Thiết kế quan hệ rõ ràng.
- Có CreatedAt, UpdatedAt, CreatedBy và UpdatedBy khi phù hợp.
- Quy tắc xóa dữ liệu phải phù hợp với lịch sử nghiệp vụ; ưu tiên vô hiệu hóa hoặc lưu trữ đối với dữ liệu đã phát sinh giao dịch.
- Sử dụng decimal phù hợp để lưu tiền; không sử dụng float/double cho số tiền tài chính.
- Xác định rõ cách lưu thời gian, múi giờ và ngày nghiệp vụ.
- Có chiến lược concurrency control.
- Có migration và dữ liệu khởi tạo ban đầu.
- Seed vai trò và quyền cơ bản một cách an toàn.
- Có dữ liệu mẫu phục vụ Development, tách biệt với dữ liệu Production.
- Tối ưu các truy vấn báo cáo và truy vấn tồn kho.
- Không tạo quá nhiều bảng hoặc abstraction khi chưa có nhu cầu nghiệp vụ.
Trước khi triển khai, hãy đề xuất ERD, các entity quan trọng và các ràng buộc toàn vẹn. Chú ý tính nhất quán giữa đơn hàng, chi tiết đơn, thanh toán, hoàn tiền, tồn kho, ca làm việc và chi phí.
## 16. Hiệu năng và độ tin cậy
- Sử dụng async/await đúng cách.
- Sử dụng CancellationToken cho các tác vụ phù hợp.
- Tránh N+1 query.
- Phân trang ở server.
- Tối ưu truy vấn và index theo dữ liệu thực tế.
- Dùng transaction cho các nghiệp vụ cần tính nguyên tử.
- Dùng optimistic concurrency hoặc cơ chế khóa phù hợp khi cập nhật tồn kho.
- Có timeout, retry có giới hạn và xử lý lỗi đối với dịch vụ bên ngoài.
- Không retry mù quáng các thao tác thanh toán hoặc ghi dữ liệu có thể tạo giao dịch trùng.
- Tách xử lý báo cáo và gửi thông báo ra khỏi luồng bán hàng khi phù hợp.
- Có health checks, logging có cấu trúc và theo dõi lỗi.
- Thiết kế có khả năng mở rộng khi số lượng cửa hàng, sản phẩm và giao dịch tăng lên.
Không thêm Redis, message broker hoặc các hạ tầng phân tán nếu chưa chứng minh được nhu cầu. Ưu tiên giải pháp đơn giản, dễ vận hành.
## 17. Kiểm thử và chất lượng mã nguồn
Tạo các loại kiểm thử phù hợp:
- Unit tests cho nghiệp vụ tính tiền, chiết khấu, thuế, tồn kho và đóng ca.
- Integration tests cho database và các luồng giao dịch quan trọng.
- Authorization tests để bảo đảm không truy cập chéo cửa hàng trái phép.
- Kiểm thử xử lý đồng thời khi bán sản phẩm cuối cùng trong kho.
- Kiểm thử chống tạo đơn hoặc thanh toán trùng.
- Kiểm thử đóng ca và gửi lại báo cáo khi Email/Telegram gặp lỗi.
- Kiểm thử đổi trả, hủy đơn và điều chỉnh tồn kho.
- Kiểm thử migration và các ràng buộc database.
Áp dụng quy tắc đặt tên nhất quán, xử lý lỗi có cấu trúc, tránh lặp code và tránh tạo lớp/hàm quá lớn.
Không để lại các phương thức giả, TODO cho nghiệp vụ cốt lõi, dữ liệu hard-code dùng như dữ liệu thật hoặc nút giao diện không có chức năng.
## 18. Git và quy trình phát triển
- Khởi tạo Git repository.
- Tạo `.gitignore` chuẩn cho Visual Studio, .NET, Rider và các công cụ phát triển liên quan.
- Tạo `.editorconfig`.
- Có README hướng dẫn cài đặt, cấu hình, chạy ứng dụng và kiểm thử.
- Có file cấu hình mẫu không chứa secret.
- Có hướng dẫn tạo database và chạy EF Core migrations.
- Có hướng dẫn cấu hình Email, Telegram và các tích hợp tùy chọn.
- Có quy ước branch, commit và code review ở mức phù hợp.
- Không đưa thư mục bin/obj, file publish, file log, file cá nhân của IDE hoặc secret lên Git.
## 19. Triển khai và vận hành
Chuẩn bị hướng dẫn triển khai trên Windows Server/IIS hoặc môi trường tương thích với ASP.NET Core.
Cần có:
- Cấu hình Development/Staging/Production.
- Cấu hình database và migration an toàn.
- Hướng dẫn HTTPS và domain.
- Hướng dẫn sao lưu và phục hồi database.
- Hướng dẫn cấu hình tác vụ nền.
- Hướng dẫn cấu hình Email và Telegram.
- Logging và kiểm tra sức khỏe ứng dụng.
- Quy trình cập nhật phiên bản và rollback.
- Hướng dẫn tạo tài khoản quản trị đầu tiên an toàn.
- Danh sách các bước kiểm tra trước khi đưa vào Production.
## 20. Phương pháp thực hiện dự án
Không viết toàn bộ hệ thống thành một khối code lớn trong một lần.
Hãy thực hiện theo từng giai đoạn có thể kiểm tra được.
### Giai đoạn 1: Phân tích và thiết kế
- Xác định yêu cầu chức năng và phi chức năng.
- Xác định các vai trò và quyền hạn.
- Phân tích các quy trình bán hàng, nhập kho, chuyển kho, đóng ca, thanh toán, đổi trả và quản lý chi phí.
- Xác định phạm vi MVP và các chức năng mở rộng.
- Đề xuất kiến trúc, solution structure, ERD và các entity chính.
- Nêu rõ giả định nghiệp vụ và các vấn đề cần xác nhận.
- Xác định tiêu chí nghiệm thu cho từng module.
### Giai đoạn 2: Khởi tạo nền tảng
- Tạo solution và các project.
- Cấu hình database, EF Core và migrations.
- Cấu hình logging, validation, authentication và authorization.
- Tạo `.gitignore`, `.editorconfig` và README.
- Tạo pipeline kiểm tra build và test cơ bản.
- Hoàn thiện chức năng đăng nhập, phân quyền và quản lý cửa hàng.
### Giai đoạn 3: Phát triển theo module
Ưu tiên theo thứ tự:
1. Identity, RBAC và quản lý cửa hàng.
2. Danh mục sản phẩm và barcode.
3. Quản lý kho và nhà cung cấp.
4. POS và quản lý đơn hàng.
5. Mở ca, đóng ca và báo cáo Excel.
6. Email và Telegram.
7. Quản lý chi phí và Dashboard.
8. Website thương mại điện tử.
9. Thanh toán, giao hàng, khách hàng và tích điểm.
10. Báo cáo nâng cao và tối ưu vận hành.
Có thể điều chỉnh thứ tự khi các phụ thuộc kỹ thuật hoặc nghiệp vụ yêu cầu.
### Giai đoạn 4: Kiểm thử và hoàn thiện
- Chạy build và toàn bộ test liên quan.
- Xử lý các lỗi phát hiện được.
- Kiểm tra bảo mật và phân quyền.
- Kiểm tra giao dịch đồng thời.
- Kiểm tra báo cáo đối chiếu với dữ liệu gốc.
- Hoàn thiện hướng dẫn cài đặt, triển khai và vận hành.
### Quy tắc làm việc với AI
- Trước khi sửa code, hãy kiểm tra cấu trúc repository và các file hiện có.
- Nếu repository đã có code, phải giữ nguyên những phần đang hoạt động trừ khi có lý do rõ ràng để thay đổi.
- Không tự ý đổi framework, phiên bản .NET, cấu trúc dự án hoặc thư viện mà không giải thích.
- Trước mỗi giai đoạn, nêu mục tiêu, file sẽ tạo/sửa và tiêu chí hoàn thành.
- Thực hiện các thay đổi theo từng nhóm nhỏ, có thể build và kiểm thử.
- Không giả định rằng một tính năng đã hoàn thành nếu chưa có code hoạt động và kiểm thử phù hợp.
- Khi gặp lỗi build/test, ưu tiên phân tích nguyên nhân và sửa lỗi trước khi tiếp tục thêm tính năng.
- Khi thiếu thông tin nghiệp vụ quan trọng, nêu giả định rõ ràng hoặc hỏi câu hỏi cần thiết.
- Không tạo code giả chỉ để minh họa cho nghiệp vụ cốt lõi.
- Sau mỗi giai đoạn, cung cấp danh sách file đã tạo/sửa, các chức năng đã hoàn thành, kết quả build/test và các vấn đề còn tồn tại.
- Không tuyên bố hệ thống sẵn sàng Production nếu chưa có bằng chứng kiểm thử và đánh giá phù hợp.
## 21. Yêu cầu đầu ra đầu tiên
Ở bước đầu tiên, chưa cần triển khai toàn bộ code.
Hãy trả về:
1. Bản phân tích yêu cầu.
2. Danh sách chức năng MVP và chức năng nâng cao.
3. Sơ đồ kiến trúc tổng thể.
4. Cấu trúc solution/project đề xuất.
5. ERD sơ bộ và danh sách entity chính.
6. Danh sách vai trò, quyền và phạm vi dữ liệu.
7. Các quy trình nghiệp vụ quan trọng, đặc biệt là bán hàng, tồn kho, thanh toán và đóng ca.
8. Danh sách rủi ro kỹ thuật và các quyết định cần xác nhận.
9. Kế hoạch triển khai theo từng giai đoạn.
10. Các tiêu chí nghiệm thu có thể kiểm thử.
Sau khi trình bày, hãy chờ tôi xác nhận thiết kế trước khi bắt đầu triển khai giai đoạn tiếp theo.
Mục tiêu cuối cùng là một hệ thống quản lý chuỗi nhà sách có thể phát triển và vận hành thực tế, ưu tiên tính đúng đắn của nghiệp vụ, bảo mật, khả năng bảo trì và khả năng mở rộng.

Viết lại cho tôi toàn bộ yêu cầu của tôi khi khởi tạo vào file MD và viết cả hướng dẫn sử dụng chi tiết hệ thống cho tôi vào file HUONGDAN.txt

