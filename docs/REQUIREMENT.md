# REQUIREMENT.md

# Website Hỗ Trợ Học Tiếng Anh

## 1. Mục đích tài liệu

Tài liệu này mô tả yêu cầu cho đồ án **Website hỗ trợ học tiếng Anh** được xây dựng bằng **ASP.NET Core MVC**.

Tài liệu được dùng làm đầu vào cho Codex để:

1. Phân tích yêu cầu hệ thống.
2. Đề xuất kiến trúc tổng thể.
3. Thiết kế database.
4. Chia module và lập kế hoạch triển khai.
5. Xác định thứ tự ưu tiên.
6. Triển khai từng chức năng mà không vượt quá phạm vi MVP.
7. Giữ frontend và backend nhất quán trong toàn bộ project.

> Lưu ý: Đây là đồ án sinh viên. Ưu tiên hệ thống **rõ nghiệp vụ, dễ bảo trì, dễ demo và dễ giải thích khi bảo vệ**, không cần over-engineering.

---

# 2. Tổng quan dự án

## 2.1. Tên dự án

**Website hỗ trợ học tiếng Anh**

Tên gợi ý trong code:

`EnglishLearningSystem`

## 2.2. Mục tiêu

Xây dựng một website giúp người dùng học tiếng Anh tập trung trên một hệ thống duy nhất.

Hệ thống hỗ trợ:

- Học từ vựng.
- Học ngữ pháp.
- Luyện Listening.
- Luyện Speaking.
- Luyện Reading.
- Luyện Writing.
- Làm bài tập.
- Xem kết quả.
- Theo dõi tiến độ học tập.

Ngoài ra hệ thống có khu vực quản trị dành cho Admin để quản lý người dùng và nội dung học tập.

---

# 3. Định hướng sản phẩm

Website lấy cảm hứng trải nghiệm từ các nền tảng tự học ngoại ngữ như Brottin, nhưng **không clone giao diện, thương hiệu hoặc nội dung**.

Phiên bản đầu chỉ tập trung vào các chức năng cốt lõi.

Các chức năng nâng cao như AI, streak, leaderboard, roadmap A1-C2... chỉ nằm trong phần phát triển sau.

---

# 4. Technology Stack

## 4.1. Backend

- ASP.NET Core MVC
- C#
- Entity Framework Core
- ASP.NET Core Identity hoặc authentication/authorization tương đương
- LINQ

## 4.2. Database

- Microsoft SQL Server

## 4.3. Frontend

- Razor Views
- HTML5
- CSS3
- JavaScript
- Bootstrap 5

Có thể sử dụng thêm thư viện icon nhẹ như:

- Bootstrap Icons
- Font Awesome

Không sử dụng React/Vue/Angular nếu không thật sự cần thiết.

---

# 5. Actors

Hệ thống có 3 actor chính.

## ACT-01 — Khách

Người chưa đăng nhập.

Quyền:

- Đăng ký tài khoản.
- Đăng nhập.
- Xem landing page.
- Xem thông tin giới thiệu chung của website.

Khách không được truy cập khu vực học tập cá nhân.

## ACT-02 — Học viên

Người dùng đã đăng nhập với vai trò Student.

Quyền:

- Đăng xuất.
- Xem và cập nhật hồ sơ.
- Học từ vựng.
- Học ngữ pháp.
- Luyện Listening.
- Luyện Speaking.
- Luyện Reading.
- Luyện Writing.
- Làm bài tập.
- Xem kết quả.
- Xem lịch sử làm bài.
- Theo dõi tiến độ học tập.

## ACT-03 — Admin

Người quản trị hệ thống.

Quyền:

- Đăng nhập.
- Đăng xuất.
- Quản lý người dùng.
- Quản lý từ vựng.
- Quản lý ngữ pháp.
- Quản lý Listening.
- Quản lý Speaking.
- Quản lý Reading.
- Quản lý Writing.
- Quản lý câu hỏi.
- Quản lý đáp án.
- Xem kết quả học tập của học viên.

---

# 6. Phạm vi MVP

Phiên bản đầu PHẢI có các module:

1. Authentication & Account
2. Vocabulary
3. Grammar
4. Listening
5. Speaking
6. Reading
7. Writing
8. Exercise
9. Result
10. Learning Progress
11. Admin

Không xây dựng module Course trong MVP.

---

# 7. Functional Requirements

## FR-01 — Đăng ký tài khoản

### Actor

Khách

### Mô tả

Khách có thể tạo tài khoản học viên mới.

### Dữ liệu đầu vào

- Họ tên.
- Email.
- Mật khẩu.
- Xác nhận mật khẩu.

### Quy tắc

- Email là bắt buộc.
- Email phải đúng định dạng.
- Email không được trùng.
- Mật khẩu phải được lưu dưới dạng hash.
- Tài khoản mới mặc định có role `Student`.
- Tài khoản mặc định ở trạng thái hoạt động.

### Kết quả

Tạo thành công tài khoản học viên.

---

## FR-02 — Đăng nhập

### Actor

Khách, Học viên, Admin

### Dữ liệu đầu vào

- Email.
- Mật khẩu.

### Xử lý

- Kiểm tra tài khoản tồn tại.
- Kiểm tra mật khẩu.
- Kiểm tra trạng thái tài khoản.
- Xác định role.

### Điều hướng

Student:

`/Student` hoặc Dashboard học viên.

Admin:

`/Admin`

---

## FR-03 — Đăng xuất

### Actor

Học viên, Admin

### Kết quả

- Xóa phiên đăng nhập.
- Chuyển về landing page hoặc trang đăng nhập.

---

# 8. Module Hồ sơ cá nhân

## FR-04 — Xem hồ sơ

Học viên có thể xem:

- Họ tên.
- Email.
- Avatar.
- Ngày tạo tài khoản.

## FR-05 — Cập nhật hồ sơ

Học viên có thể cập nhật:

- Họ tên.
- Avatar.
- Các thông tin hồ sơ được hệ thống cho phép.

Không cho phép người dùng tự thay đổi role.

---

# 9. Module Từ vựng

## FR-06 — Danh sách từ vựng

Học viên có thể xem danh sách từ vựng.

Thông tin tối thiểu:

- Word
- Meaning
- Pronunciation
- Part of Speech
- Example

## FR-07 — Chi tiết từ vựng

Hiển thị:

- Từ tiếng Anh.
- Nghĩa tiếng Việt.
- Phiên âm.
- Loại từ.
- Ví dụ.
- Audio nếu có.
- Hình ảnh nếu có.

## FR-08 — Tìm kiếm từ vựng

Cho phép tìm theo:

- Từ tiếng Anh.
- Nghĩa.

## FR-09 — Nghe phát âm

Nếu từ vựng có audio:

- Có nút Play.
- Không tự động phát khi mở trang.

Có thể dùng file audio hoặc Text-to-Speech đơn giản.

Không cần AI pronunciation scoring trong MVP.

---

# 10. Module Ngữ pháp

## FR-10 — Danh sách chủ đề ngữ pháp

Ví dụ:

- Present Simple
- Present Continuous
- Past Simple
- Present Perfect
- Future Simple
- Passive Voice
- Conditional Sentences

## FR-11 — Chi tiết ngữ pháp

Một chủ đề cần có:

- Title
- Description
- Formula
- Usage
- Example
- Notes nếu có

## FR-12 — Tìm kiếm ngữ pháp

Học viên có thể tìm theo tên chủ đề.

---

# 11. Module Listening

## FR-13 — Danh sách bài Listening

Mỗi bài có:

- Title
- Description
- AudioURL
- Transcript
- Difficulty hoặc Level nếu cần

## FR-14 — Luyện nghe

Trang bài nghe gồm:

- Audio player.
- Tiêu đề.
- Nội dung mô tả.
- Câu hỏi nghe hiểu.

## FR-15 — Transcript

Học viên có thể chủ động bấm xem transcript.

Transcript không bắt buộc hiển thị ngay từ đầu.

## FR-16 — Câu hỏi Listening

Hỗ trợ tối thiểu:

- Multiple Choice
- True/False

Sau khi nộp bài:

- Chấm điểm.
- Hiển thị đáp án đúng/sai.

---

# 12. Module Speaking

## FR-17 — Danh sách chủ đề Speaking

Ví dụ:

- Introduce Yourself
- Daily Routine
- Travel
- Shopping
- Job Interview
- At the Restaurant

## FR-18 — Nội dung Speaking

Mỗi chủ đề có:

- Title
- Situation
- Prompt
- Suggested Questions
- Sample Sentences
- Audio mẫu nếu có

## FR-19 — Luyện nói

MVP chỉ cần:

- Hiển thị chủ đề.
- Hiển thị câu mẫu.
- Cho nghe câu mẫu.
- Người dùng tự luyện nói.

Không yêu cầu:

- Speech-to-Text.
- AI đánh giá phát âm.
- Chấm pronunciation.

---

# 13. Module Reading

## FR-20 — Danh sách bài Reading

Mỗi bài có:

- Title
- Description.
- Content.
- Vocabulary list nếu có.

## FR-21 — Bài đọc

Hiển thị:

- Tiêu đề.
- Nội dung bài đọc.
- Từ vựng quan trọng.
- Câu hỏi đọc hiểu.

## FR-22 — Câu hỏi Reading

Hỗ trợ:

- Multiple Choice
- True/False

Sau khi nộp:

- Chấm điểm.
- Hiển thị câu đúng/sai.

---

# 14. Module Writing

## FR-23 — Danh sách chủ đề Writing

Ví dụ:

- Introduce yourself.
- Describe your family.
- Write about your favorite place.
- Write an email.
- Describe your daily routine.

## FR-24 — Viết bài

Trang Writing có:

- Title.
- Prompt.
- Description.
- Suggested Vocabulary nếu có.
- Textarea/editor để viết.

## FR-25 — Lưu bài viết

Học viên có thể:

- Lưu bài viết.
- Chỉnh sửa bài đã lưu.
- Xem lại lịch sử Writing.

MVP không cần AI chấm bài.

---

# 15. Module Bài tập

## FR-26 — Danh sách bài tập

Bài tập có thể gắn với:

- Vocabulary.
- Grammar.
- Listening.
- Reading.

Không bắt buộc phải gắn với Course.

## FR-27 — Loại câu hỏi

MVP hỗ trợ:

### Multiple Choice

Ví dụ:

> She ___ to school every day.

- go
- goes
- going
- gone

### True / False

Ví dụ:

> John lives in London.

- True
- False

Có thể bổ sung Fill in the Blank sau.

## FR-28 — Nộp bài

Khi học viên nộp:

1. Khóa câu trả lời của lần làm đó.
2. Chấm điểm.
3. Lưu kết quả.
4. Lưu thời gian hoàn thành.

## FR-29 — Chấm điểm

Đối với câu hỏi có đáp án xác định:

```text
Score = CorrectAnswer / TotalQuestion * 100
```

Điểm có thể lưu dạng integer hoặc decimal.

---

# 16. Module Kết quả

## FR-30 — Xem kết quả

Hiển thị:

- Tên bài.
- Điểm.
- Tổng số câu.
- Số câu đúng.
- Số câu sai.
- Ngày làm.
- Thời gian hoàn thành nếu có.

## FR-31 — Xem chi tiết đáp án

Học viên có thể xem:

- Câu hỏi.
- Đáp án đã chọn.
- Đáp án đúng.
- Explanation nếu có.

## FR-32 — Lịch sử làm bài

Hiển thị danh sách các lần làm bài theo thời gian.

Sắp xếp mới nhất trước.

---

# 17. Module Tiến độ học tập

## FR-33 — Theo dõi tiến độ

Hệ thống lưu hoạt động học của từng học viên.

Ví dụ:

- Vocabulary đã xem/học.
- Grammar đã học.
- Listening đã hoàn thành.
- Reading đã hoàn thành.
- Writing đã lưu.
- Exercise đã làm.

## FR-34 — Dashboard học viên

Dashboard có thể hiển thị:

- Số bài đã hoàn thành.
- Số bài tập đã làm.
- Điểm trung bình.
- Hoạt động gần đây.

MVP không yêu cầu biểu đồ phức tạp.

---

# 18. Admin Module

Admin sử dụng layout riêng.

URL gợi ý:

```text
/Admin
/Admin/Users
/Admin/Vocabularies
/Admin/Grammars
/Admin/Listening
/Admin/Speaking
/Admin/Reading
/Admin/Writing
/Admin/Exercises
/Admin/Results
```

## FR-35 — Quản lý người dùng

Admin có thể:

- Xem danh sách.
- Tìm kiếm.
- Xem chi tiết.
- Khóa tài khoản.
- Mở khóa tài khoản.

Không nên xóa cứng tài khoản đã có dữ liệu học tập.

## FR-36 — Quản lý từ vựng

Admin CRUD:

- Create
- Read
- Update
- Delete hoặc Soft Delete

Các trường:

- Word
- Meaning
- Pronunciation
- PartOfSpeech
- Example
- AudioURL
- ImageURL

## FR-37 — Quản lý ngữ pháp

Admin CRUD:

- Title
- Description
- Formula
- Usage
- Example
- Notes

## FR-38 — Quản lý Listening

Admin CRUD:

- Title
- Description
- AudioURL
- Transcript
- Questions

## FR-39 — Quản lý Speaking

Admin CRUD:

- Title
- Situation
- Prompt
- SuggestedQuestions
- SampleSentences
- AudioURL

## FR-40 — Quản lý Reading

Admin CRUD:

- Title
- Description
- Content
- Vocabulary
- Questions

## FR-41 — Quản lý Writing

Admin CRUD:

- Title
- Prompt
- Description
- SuggestedVocabulary

## FR-42 — Quản lý câu hỏi

Admin có thể:

- Tạo câu hỏi.
- Sửa câu hỏi.
- Xóa câu hỏi.
- Chọn QuestionType.
- Gắn câu hỏi với Exercise hoặc nội dung tương ứng.

## FR-43 — Quản lý đáp án

Admin có thể:

- Tạo nhiều answer options.
- Đánh dấu đáp án đúng.
- Thêm explanation.

## FR-44 — Xem kết quả học tập

Admin có thể xem:

- Học viên.
- Bài tập.
- Điểm.
- Ngày làm.

Có thể lọc theo:

- Học viên.
- Loại bài.
- Ngày.

---

# 19. Data Model sơ bộ

Codex phải phân tích và đề xuất ERD trước khi tạo migration.

Các entity dự kiến:

```text
Role
User

Vocabulary
Grammar

Skill
LearningContent

ListeningContent
SpeakingContent
ReadingContent
WritingTopic

Exercise
Question
AnswerOption

UserExercise
UserAnswer

UserWriting

UserProgress
```

Codex có thể đề xuất cấu trúc tốt hơn nếu cần, nhưng phải giải thích lý do trước khi thay đổi.

---

# 20. Quan hệ dữ liệu dự kiến

```text
Role 1 --- N User

Exercise 1 --- N Question
Question 1 --- N AnswerOption

User 1 --- N UserExercise
Exercise 1 --- N UserExercise

UserExercise 1 --- N UserAnswer
Question 1 --- N UserAnswer

User 1 --- N UserWriting
WritingTopic 1 --- N UserWriting

User 1 --- N UserProgress
```

---

# 21. Business Rules

## BR-01
Người dùng phải đăng nhập mới được sử dụng chức năng học tập.

## BR-02
Student không được truy cập `/Admin`.

## BR-03
Admin không được tự động trở thành Student nếu không có yêu cầu.

## BR-04
User chỉ được chỉnh sửa hồ sơ của chính mình.

## BR-05
Student không được CRUD nội dung học.

## BR-06
Admin mới được CRUD nội dung.

## BR-07
Một câu hỏi Multiple Choice phải có ít nhất 2 đáp án.

## BR-08
Một câu hỏi phải có đáp án đúng để hệ thống chấm tự động.

## BR-09
Kết quả làm bài phải gắn với User và Exercise.

## BR-10
Không xóa cứng dữ liệu nếu việc xóa làm mất lịch sử học tập.

Ưu tiên Soft Delete cho dữ liệu đã được sử dụng.

---

# 22. UI / UX Requirements

## 22.1. Style

Phong cách mong muốn:

- Modern.
- Minimal.
- Blue tone.
- Glassmorphism.
- Full-screen background.
- Card trắng mờ.
- Border nhẹ.
- Blur background.
- Rounded corners.
- Animation nhẹ.

Có thể tham khảo cảm giác giao diện của Brottin nhưng không sao chép trực tiếp.

## 22.2. Landing Page

Landing page ưu tiên hiển thị trong một màn hình desktop.

Các chức năng nổi bật:

```text
Vocabulary
Grammar
Listening
Speaking
Reading
Writing
```

Navbar:

```text
Logo
Trang chủ
Đăng nhập
Đăng ký
```

Sau khi đăng nhập có thể thay bằng:

```text
Dashboard
Profile
Logout
```

## 22.3. Background

Có thể sử dụng:

- Ảnh full-screen.
- Video background nhẹ.

Nếu dùng video:

- autoplay
- muted
- loop
- playsinline

Phải có fallback image.

Không để background làm giảm khả năng đọc chữ.

## 22.4. Glass Card

Style định hướng:

```css
background: rgba(255, 255, 255, 0.15);
border: 1px solid rgba(255, 255, 255, 0.25);
backdrop-filter: blur(12px);
border-radius: 20px;
```

Thông số thực tế có thể điều chỉnh theo UI.

---

# 23. Responsive Requirements

Website phải sử dụng được trên:

- Desktop.
- Tablet.
- Mobile.

Desktop là màn hình ưu tiên của đồ án.

Không để:

- Text overflow.
- Horizontal scroll ngoài ý muốn.
- Card bị tràn.
- Button quá nhỏ trên mobile.

---

# 24. Validation

Phải có validation ở cả:

- Client side.
- Server side.

Ví dụ:

- Required.
- MaxLength.
- EmailAddress.
- Range.

Không chỉ dựa vào JavaScript.

---

# 25. Error Handling

Hệ thống cần xử lý tối thiểu:

- 404 Not Found.
- 403 Forbidden.
- 500 Error.
- Validation errors.
- Database errors ở mức phù hợp.

Không hiển thị stack trace ra production UI.

---

# 26. Security Requirements

- Password phải hash.
- Authorization theo Role.
- CSRF protection cho form.
- Không tin dữ liệu từ client.
- Validate upload nếu có.
- Không lưu password plain text.
- Không hard-code secret trong source code.

---

# 27. Performance cơ bản

- Dùng async khi phù hợp.
- Không query toàn bộ database nếu chỉ cần một phần.
- Có pagination cho danh sách lớn.
- Tránh N+1 query.
- Chỉ Include entity cần thiết.
- Tối ưu ảnh/video ở mức hợp lý.

---

# 28. Logging

Sử dụng logging của ASP.NET Core cho:

- Error.
- Authentication failure nếu cần.
- Các lỗi nghiệp vụ quan trọng.

Không cần xây hệ thống audit phức tạp trong MVP.

---

# 29. Seed Data

Project nên có seed data tối thiểu để demo.

## Role

```text
Admin
Student
```

## Admin Account

Tạo một tài khoản admin demo thông qua seed hoặc cấu hình an toàn.

## Nội dung demo

Tối thiểu:

- 15-30 từ vựng.
- 5-10 chủ đề ngữ pháp.
- 3 bài Listening.
- 3 chủ đề Speaking.
- 3 bài Reading.
- 3 chủ đề Writing.
- 3-5 bài Exercise.

---

# 30. Out of Scope — Không làm trong MVP

Codex KHÔNG tự ý triển khai các chức năng sau:

- Course.
- Enrollment.
- Payment.
- Subscription.
- AI chatbot.
- AI Writing scoring.
- AI Speaking scoring.
- Speech recognition.
- Pronunciation scoring.
- XP.
- Gems.
- Badge.
- Streak.
- Leaderboard.
- Daily Missions.
- Pomodoro.
- Flashcard SRS.
- CEFR placement test.
- Roadmap A1-C2.
- Live class.
- Teacher role.
- Video call.
- Mobile app.

Nếu cần các chức năng trên để mở rộng kiến trúc, chỉ được chuẩn bị ở mức không gây phức tạp cho MVP.

---

# 31. Future Features

## Phase 2

- Flashcard.
- Favorite Vocabulary.
- Level A1-C2.
- Placement Test.
- Learning Roadmap.

## Phase 3

- XP.
- Streak.
- Daily Mission.
- Badge.
- Leaderboard.

## Phase 4

- AI Writing Assistant.
- AI Speaking.
- Speech Recognition.
- Pronunciation Scoring.
- Chatbot.

---

# 32. Definition of Done

Một chức năng chỉ được xem là hoàn thành khi:

1. Backend chạy đúng.
2. Database migration hợp lệ.
3. Authorization đúng.
4. Validation đầy đủ.
5. UI hoạt động.
6. Responsive cơ bản.
7. Không có lỗi build.
8. Không có exception khi thực hiện luồng chuẩn.
9. Dữ liệu được lưu đúng.
10. Có empty state nếu danh sách rỗng.
11. Có thông báo success/error phù hợp.

---

# 33. Yêu cầu dành riêng cho Codex

Trước khi sửa hoặc tạo code, hãy thực hiện theo thứ tự:

## Step 1 — Đọc project

Phân tích:

- Folder structure.
- Existing Models.
- DbContext.
- Controllers.
- Views.
- Authentication.
- Existing CSS/JS.
- Existing migrations.

KHÔNG bắt đầu viết lại project khi chưa phân tích code hiện tại.

## Step 2 — Gap Analysis

So sánh source code hiện tại với REQUIREMENT.md.

Trả về:

```text
Implemented
Partially Implemented
Missing
Needs Refactor
```

cho từng module.

## Step 3 — Architecture Proposal

Đề xuất:

- Folder structure.
- Entities.
- Relationships.
- DbContext.
- Services nếu cần.
- Controllers.
- ViewModels.
- Authorization strategy.
- File/media storage strategy.

Không over-engineer.

## Step 4 — Database Plan

Trước khi tạo migration:

1. Liệt kê entities.
2. Liệt kê fields.
3. PK/FK.
4. Relationships.
5. Delete behavior.
6. Indexes cần thiết.

Sau đó mới tạo migration.

## Step 5 — Implementation Plan

Chia thành phase nhỏ.

Gợi ý thứ tự:

```text
Phase 1
Authentication + Role + User

Phase 2
Admin Layout + User Management

Phase 3
Vocabulary + Grammar

Phase 4
Listening + Reading

Phase 5
Speaking + Writing

Phase 6
Exercise + Question + Answer

Phase 7
Result + History

Phase 8
Progress + Student Dashboard

Phase 9
UI Polish + Responsive + Validation

Phase 10
Testing + Seed Data + Cleanup
```

---

# 34. Quy tắc làm việc của Codex

Codex phải:

- Giữ project build được sau từng phase.
- Không thay đổi stack nếu không cần thiết.
- Không cài package thừa.
- Không tự thêm chức năng ngoài scope.
- Không phá code đã chạy.
- Ưu tiên reuse component/layout.
- Dùng ViewModel khi View cần dữ liệu tổng hợp.
- Không bind trực tiếp entity cho form nhạy cảm nếu có nguy cơ over-posting.
- Sử dụng async database calls khi phù hợp.
- Giữ naming convention nhất quán.
- Comment chỉ khi logic không rõ ràng.
- Không comment những dòng hiển nhiên.

---

# 35. Output mong muốn từ Codex trước khi code

Sau khi đọc REQUIREMENT.md và source code, Codex KHÔNG nên code ngay.

Trước tiên hãy trả về tài liệu phân tích gồm:

## A. System Understanding

Tóm tắt hệ thống mà Codex hiểu.

## B. Current Project Analysis

Project hiện tại đã có gì.

## C. Gap Analysis

Thiếu gì so với requirement.

## D. Proposed Architecture

Kiến trúc đề xuất.

## E. Proposed Database Schema

Danh sách bảng và quan hệ.

## F. Implementation Plan

Các phase triển khai.

## G. Risks / Open Questions

Các điểm cần xác nhận trước khi làm.

---

# 36. Nguyên tắc ưu tiên

Ưu tiên theo thứ tự:

```text
Correctness
>
Security
>
Maintainability
>
Usability
>
Visual Polish
>
Advanced Features
```

Mục tiêu cuối cùng là một hệ thống đồ án:

- Chạy ổn định.
- Có nghiệp vụ rõ ràng.
- Database hợp lý.
- UI đẹp.
- Dễ demo.
- Dễ giải thích.
- Có khả năng mở rộng sau này.
