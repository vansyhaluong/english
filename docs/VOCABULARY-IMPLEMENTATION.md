# Trang từ vựng

- URL: `/vocabulary` và `/vi/vocabulary`.
- Tham chiếu: HTML công khai https://brottin.quest/vi/vocabulary, đối chiếu ngày 2026-09-17. Giữ shell EnglishHub, nền và font hiện có; dùng bố cục hai khối gợi ý, notice, lưới bộ thẻ và phân trang. Nội dung tiếng Anh thay cho tiếng Đức; không thêm luyện Artikel.
- Có 14 bộ thẻ mẫu tự soạn (43 từ), ghi rõ trên giao diện. `Data/VocabularySamples.cs` chỉ là dữ liệu preview, không thay thế mô hình VocabularyMeaning quan hệ trong kế hoạch database.
- GET filter theo A1–C2, tìm tên bộ/từ/nghĩa không phân biệt dấu và hoa thường, 12 bộ/trang. Filter và danh sách từ vẫn sử dụng được khi tắt JavaScript.
- Flashcard dùng native dialog: hiện nghĩa, ví dụ, phiên âm, thẻ trước/tiếp, đóng bằng Escape và trả focus. SpeechSynthesis dùng giọng en-GB khi trình duyệt hỗ trợ; không tự phát, dừng khi đổi thẻ/đóng.
- Chưa lưu tiến độ, đăng nhập, spaced repetition hoặc kết nối database. Không báo đã lưu/đã thành thạo sau khi chỉ xem thẻ.
- Các liên kết từ vựng trên trang chủ và thanh điều hướng trỏ vào trang mới. Thanh điều hướng đánh dấu trang đang mở bằng aria-current.

## Kiểm tra

- `dotnet build --no-restore`: 0 lỗi, 0 cảnh báo.
- `node --check wwwroot/js/vocabulary.js`: hợp lệ.
- `node scripts/check-vocabulary.mjs http://127.0.0.1:5098`: 14 kiểm tra HTTP/markup pass, gồm route alias, lọc, tìm nghĩa không dấu, trang ngoài phạm vi, input HTML được encode, trạng thái không kết quả, tài nguyên và CTA trang chủ.
- Chưa kiểm tra trực quan hoặc thao tác flashcard trong browser: không có trình duyệt kết nối trong phiên này. Cần kiểm tra 375/768/1440px, Tab/Escape, phát âm và prefers-reduced-motion trong browser.
