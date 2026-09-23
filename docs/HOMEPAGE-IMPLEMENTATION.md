# Trang chủ EnglishHub

Giao diện triển khai theo `DESIGN.md` và HTML/CSS công khai của https://brottin.quest/ (đối chiếu ngày 2026-09-13). Giữ thương hiệu EnglishHub và nội dung tiếng Anh của dự án.

## Foundations

`wwwroot/css/brottin-theme.css` định nghĩa token ngữ nghĩa cho màu, font, spacing, radius và motion. Mulish là font nội dung; Be Vietnam Pro dành cho thương hiệu và số liệu; IBM Plex Mono dành cho bước học và trình độ. Nền đen, chữ #e9e6ee, chữ phụ #c9c5d2, màu nhấn #c4a6ff, surface kính rgba(21,21,31,.66). Ảnh nền tĩnh dùng lại `images/anim-castle-night.jpg` có sẵn trong dự án.

## Components và trạng thái

- Link/nút must có focus-visible, hover và active. Nút disabled must giảm opacity và không cho thao tác. Trạng thái aria-busy must có cursor progress khi có thao tác bất đồng bộ; trang hiện tại không có thao tác tải dữ liệu.
- Card must dùng token padding, radius, border và surface; card liên kết must có vùng click toàn thẻ. Nội dung dài must tự xuống dòng, không cắt mô tả trình độ.
- FAQ must dùng details/summary gốc: Enter/Space, click hoặc chạm để mở/đóng; nội dung mở must không bị giới hạn chiều cao.
- Dialog must dùng showModal, hỗ trợ Escape, đóng bằng nút và trả focus về nút mở. Hộp đăng nhập hiện giải thích tính năng chưa kết nối, không thu thập mật khẩu hay báo thành công giả.
- Layout must chuyển số cột tại 640/1024/1280px; header must xuống dòng trên màn hình hẹp; CTA must đủ rộng và có vùng chạm tối thiểu 44px.
- Motion must tắt khi prefers-reduced-motion. Surface must chuyển đặc hơn khi prefers-reduced-transparency.

## Nội dung

Giữ nội dung tiếng Anh của dự án. Các liên kết hiện dẫn tới phần giới thiệu trong trang; khóa học, AI, tiến độ và đăng nhập chưa có backend trong dự án này. Không thêm thông báo khẳng định đã hoàn thành hành động khi chưa có kết quả thực.

## Nguồn hình

Các file learning-mascot.png, vocabulary-mascot.png, reading-mascot.png, listening-mascot.png và exam-mascot.png được tải từ các đường dẫn công khai /mascot/dashboard.png, /mascot/vocabulary.png, /mascot/loading.png, /newbie/hear.png, /mascot/cup.png của trang tham chiếu theo yêu cầu tái tạo giao diện. Đây là các tài nguyên tham chiếu, không phải hình do EnglishHub tạo.

## QA

- Build Razor: `dotnet build --no-restore`.
- JavaScript: `node --check wwwroot/js/site.js`.
- HTTP: trang chủ, CSS, JS và hình must trả 200; link nội trang must có ID đích duy nhất.
- Kiểm tra thủ công khi có trình duyệt: 320/375/768/1440px không cuộn ngang; Tab thấy focus; FAQ dùng Enter/Space; dialog Escape và focus trở lại; kiểm tra tương phản chữ trên nền ảnh đạt WCAG AA.

Không dùng outline:none, cắt nội dung FAQ bằng max-height cố định, animation bắt buộc hoặc thông báo đăng nhập thành công giả.

Kết quả kiểm tra tự động: build 0 lỗi/0 cảnh báo; JS hợp lệ; HTTP trang chủ và 8 tài nguyên trả 200; 30 liên kết nội trang có đích hợp lệ, ID không trùng. Chạy kiểm tra bằng profile Development với `--Logging:EventLog:LogLevel:Default=None` vì sandbox không cho ghi Windows Event Log. Chưa kiểm tra trực quan/bàn phím bằng trình duyệt do không có browser được kết nối trong phiên làm việc.
