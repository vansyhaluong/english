# EnglishHub — TASKS

Trạng thái: **Approved**. Revision TASKS: **2 — refactor theo feature/phase**.
Quy mô: **49 → 36 task**, giữ đủ Phase P0–P9; Phase Gate không tính là task. Cơ cấu chi tiết được yêu cầu cộng thành 36 task, nên ưu tiên giữ ranh giới kiểm soát rủi ro thay vì gộp thêm để đạt khoảng 28–32.
Cơ sở: [PLAN.md revision 1.1 — Approved/Baselined Architecture Plan](PLAN.md).
P1-01 và P1-02 đã hoàn thành; các task còn lại chưa bắt đầu. Việc liệt kê task không có nghĩa được phép thực thi.

## 1. Phạm vi và cổng phê duyệt

- PLAN đã được duyệt sau ba sửa đổi: VocabularyMeaning quan hệ 1-N có thứ tự; Practice TOEIC tối thiểu một câu hợp lệ là rule riêng MVP; Reading bắt buộc Level nhưng Topic optional.
- Kiến trúc hiện tại dùng Database First với `ApplicationDbContext` và entities đã scaffold từ SQL Server; không dùng EF migrations để tạo schema hiện có.
- **Dừng sau khi tạo TASKS để người dùng duyệt.** Duyệt TASKS không tự khởi động implementation; cần yêu cầu bắt đầu riêng.
- Quyết định mới nhất của người dùng > PLAN/baseline được duyệt > REQUIREMENT.md cũ. Không tự thêm tính năng từ landing hoặc nội dung ngoài MVP.
- Task có thay đổi schema/install/test ở dưới chỉ được thực hiện sau cổng implementation. Mọi thay đổi schema phải theo quy trình Database First và được phê duyệt rõ ràng; không chạy trước để “chuẩn bị”.
- Phạm vi thư mục trong task là nơi dự kiến sửa khi thực hiện, không phải các file đã được tạo.

## 2. Cách thực hiện và cập nhật trạng thái

ID dạng Pn-xx đã được renumber trong revision 2; chỉ dùng ID hiện tại khi triển khai. Checkbox là đơn vị feature có đầu ra demo/kiểm chứng được; Phase Gate là điều kiện kết thúc phase, không là task độc lập. Mọi phase sau phụ thuộc Gate của phase trước; Gate đạt khi các task trong phase hoàn thành và tiêu chí cuối phase có bằng chứng.

ID cố định từ revision này dạng Pn-xx, checkbox chưa chọn = chưa làm. Chỉ chọn hoàn thành khi kết quả và kiểm tra nghiệm thu đã đạt; ghi bằng chứng lệnh/test/demo và giới hạn liên quan dưới task khi thực hiện. Nếu blocked, ghi lý do/phụ thuộc còn thiếu; không đánh dấu hoàn thành.

Mỗi task gồm đầu ra, phụ thuộc và nghiệm thu. Thực hiện theo phase; có thể đổi thứ tự trong phase khi đủ phụ thuộc nhưng không bỏ cổng kiểm tra. Không mặc định giao việc cho agent khác. UI/authorization/server validation/localization đi cùng từng chức năng, không dồn đến cuối.

Quy tắc chung:

- Dùng MVC monolith, service/DbContext theo PLAN; controller không chứa scoring/timing/format.
- Không rewrite landing; không gửi correct answer trước submit.
- **CSS:** global.css chỉ có design tokens, reset/base styles, typography, common components/utilities dùng chung toàn dự án. Style đặc thù đặt trong landing.css, student.css, admin.css hoặc tương đương. Layout chỉ nạp CSS khu vực tương ứng; scope selector khi cần. Chỉnh Student/Admin không được làm đổi landing hiện có; kiểm tra cả khi thay đổi shared global styles.
- **Form/Input:** mọi form MVC nhận dữ liệu Student/Admin dùng ViewModel, InputModel hoặc request DTO phù hợp; không bind trực tiếp domain entity, kể cả form Admin. AJAX như TOEIC autosave dùng request DTO riêng, explicit mapping/validation; không cho client ghi owner/role/score/status nội bộ.
- **Repository:** không tạo GenericRepository<T> hoặc UnitOfWork mặc định. Application services được dùng ApplicationDbContext trực tiếp; chỉ thêm repository/query abstraction chuyên biệt khi có lý do kỹ thuật cụ thể được ghi nhận, không thêm để đủ layer.
- Giữ build được; chỉ chạy kiểm tra thích hợp sau thay đổi. Không tạo tests chỉ kiểm tra property/getter.
- Integration dùng SQL Server riêng cho test, không dùng EF InMemory để chứng minh constraint/concurrency.
- Không tự ý thay đổi schema DB người dùng; khi được phê duyệt phải kiểm tra target/backup và cập nhật model bằng reverse engineering có kiểm soát. Không drop/reset để chữa lỗi.
- Fixture nhỏ đi cùng module. Demo không có AI key vẫn chạy phần còn lại, không báo AI thành công giả.

## 3. Bản đồ phase và phụ thuộc

| Phase | Mục tiêu | Điều kiện vào | Tham chiếu PLAN |
|---|---|---|---|
| P0 | Chuẩn bị triển khai và schema chi tiết | TASKS duyệt + lệnh implementation | §2–4, §10 |
| P1 | Nền tảng, Account, layout/culture/media | Gate P0 | §3, §5–7 |
| P2 | Vocabulary/Grammar và phân loại | Gate P1 | §4.1, §4.6 |
| P3 | Listening/Reading/Books | Gate P2 | §4.1, §7 |
| P4 | Bài tập, snapshot, kết quả | Gate P3 | §4.2–4.3 |
| P5 | Writing/AI | Gate P4 | §4.4, §8 |
| P6 | Soạn và xuất bản TOEIC | Gate P5 | §4.2, §9.1 |
| P7 | Practice/Full TOEIC | Gate P6 | §9.2–9.4 |
| P8 | Dashboard/Progress/Admin kết quả | Gate P7 | §4.4, §6 |
| P9 | Demo, hồi quy, tài liệu vận hành | Gate P8 | §10–12 |

Database hiện có là nguồn sự thật cho persistence. Schema nền tảng đã tồn tại nên không có migration M1. Nếu task tương lai thực sự cần đổi schema, phải được phê duyệt riêng, thay đổi SQL Server trước, rồi reverse engineer `ApplicationDbContext`/entities có kiểm soát; không tạo EF migration hoặc tự động cập nhật production.

## 4. P0 — Chuẩn bị khi được phép implementation

- [ ] **P0-01 — Chuẩn bị implementation và chốt schema kỹ thuật**
  - Phụ thuộc: hai cổng phê duyệt ở §1.
  - Đầu ra: ghi nhận SDK .NET 10, SQL Server Express/LocalDB, đường dẫn uploads/test DB, hiện trạng source và lệnh chạy; không tạo lại ứng dụng.
  - Nghiệm thu: xác định đúng DB demo/test riêng biệt; ghi nhận landing trước thay đổi; xác nhận thiếu công cụ nếu có.
  - Đầu ra: PK/FK/types/nullability/index/delete mapping của entities PLAN; state matrix cho Attempt/Evaluation/Publication; resolve cyclic FK CurrentVersion/LastSuccessfulEvaluation và constraints same-owner.
  - Nghiệm thu: VocabularyMeaning có VocabularyId/MeaningVi/DisplayOrder; Reading Topic nullable qua LearningItem, Level required; Topic các module khác giữ nguyên. Question owner XOR và group cùng Part, snapshot FK, active-attempt uniqueness rõ ràng. Không tạo bảng Course/Skill hierarchy dư.
  - Đầu ra: versions EF/SQL/tooling phù hợp, chọn sanitizer/ảnh/parser tối thiểu, một AI provider adapter dự kiến, cấu hình clock/options/secrets và SQL test DB.
  - Nghiệm thu: không hard-code vendor vào domain; không cần API key để chạy module khác; các lựa chọn không vượt PLAN. Package chỉ cài khi task P1/P5 tương ứng thực sự cần.
  - Quy ước kỹ thuật: đưa CSS shared/area, ViewModel/InputModel/request DTO và service dùng DbContext trực tiếp vào checklist thiết kế; không tạo GenericRepository<T>/UnitOfWork mặc định.

### Phase Acceptance Criteria / Gate P0

Điều kiện: hoàn thành P0-01; kiểm tra/bằng chứng dưới đây đạt trước khi chuyển phase tiếp theo.

- Hồ sơ chuẩn bị bao phủ môi trường, DB demo/test, schema/PK/FK/index/constraints, package/adapters, clock/options/secrets; đáp ứng mọi tiêu chí của P0-01, không mở lại nghiệp vụ hoặc thêm layer không có lý do.

## 5. P1 — Nền tảng, tài khoản và layout

- [x] **P1-01 — Host ASP.NET Core và Database First foundation**
  - Phụ thuộc: P0-01.
  - Phạm vi: .NET 10 / ASP.NET Core MVC host, EF Core 10 SQL Server provider, cấu hình môi trường và lớp truy cập dữ liệu Database First.
  - Đầu ra: database SQL Server hiện có; `ApplicationDbContext` và entities được reverse engineer; `AddDbContext` đăng ký DI và đọc `DefaultConnection` từ configuration.
  - Nghiệm thu: build thành công; kết nối database và đọc dữ liệu `Level` đã được xác minh; connection string máy phát triển nằm ngoài `appsettings.json` được commit; không cần EF migration cho schema hiện có.
  - Quy ước repository: application services dùng `ApplicationDbContext` trực tiếp; không tạo `GenericRepository<T>`/`UnitOfWork` để đủ layer.
  - Bằng chứng: project target `net10.0`, EF Core SQL Server 10.0.12, scaffold namespace `English.Data`/`English.Models.Entities`, DI trong `Program.cs`, kiểm tra kết nối `Level` đã thành công và `dotnet build` pass sau foundation cleanup.

- [x] **P1-02 — Đăng ký/đăng nhập/đăng xuất**
  - Phụ thuộc: P1-01.
  - Phạm vi: Account controller/views/ViewModels, Cookie Authentication, custom authentication service và bảng `AspNetUsers` hiện có.
  - Kiến trúc: dùng `IPasswordHasher<AspNetUser>` để hash/verify mật khẩu và claims tối thiểu cho danh tính/role; không dùng `IdentityDbContext`, không kế thừa `IdentityUser<Guid>` và không tạo schema/tables ASP.NET Core Identity mặc định.
  - Nghiệm thu: đăng ký chỉ Student, chuyển login, không tự login; mật khẩu >=6 không complexity; không temporary lockout/email confirmation/CAPTCHA; login role redirect đúng; logout POST+CSRF. Email trùng bị chặn kể cả request đồng thời.
  - Form/input: register/login dùng input model/DTO riêng, explicit mapping; không bind domain entity.
  - Bằng chứng: Cookie Authentication và middleware đúng thứ tự; `AuthService` dùng `ApplicationDbContext` + `IPasswordHasher<AspNetUser>`; duplicate email được kiểm tra sớm và chặn cuối bằng unique-key 2601/2627; claims tối thiểu, local return URL, logout POST+CSRF; `dotnet build` pass và GET smoke test cho Login/Register/landing pass. Không tạo Identity schema hoặc migration; POST có ghi DB chưa được manual-test để tránh tạo dữ liệu thử.

- [ ] **P1-03 — Policies, hồ sơ và quản lý user**
  - Phụ thuộc: P1-02.
  - Đầu ra: StudentOnly/AdminOnly/ActiveAccount, profile, password change, Admin list/search/detail/lock/unlock.
  - Nghiệm thu: sửa tên/avatar/password, email/role cố định; yêu cầu mật khẩu hiện tại khi đổi; Admin không làm bài như Student. Khóa tài khoản tác động request kế tiếp của phiên đang login; quyền sở hữu kiểm tra server.
  - Form/input: profile, đổi mật khẩu, Admin user actions đều dùng model/DTO riêng, không nhận trường role/owner ngoài use case.

- [ ] **P1-04 — Layout, localization và error handling**
  - Phụ thuộc: P1-02.
  - Phạm vi: Student/Admin areas/layouts, Resources, culture selection, error views.
  - Nghiệm thu: sidebar tối có icon+tên, mobile menu; default vi, chọn en qua request/route/query duy trì link/form; không profile/cookie ghi nhớ dài hạn; validation VI/EN. Giữ landing như PLAN, chỉ nối login thật; 403/404/500 không lộ stack trace.
  - CSS nghiệm thu: global.css chỉ shared tokens/base/typography/components/utilities; landing.css/student.css/admin.css hoặc tương đương chứa style khu vực. Thay style Student/Admin không đổi landing; kiểm tra stylesheet loading và selector scope, không chỉ kiểm tra tên file.

- [ ] **P1-05 — File storage và media boundary**
  - Phụ thuộc: P1-01, P1-03.
  - Đầu ra: IFileStorage local ngoài wwwroot/source deploy, protected endpoints, URL validator, avatar upload.
  - Nghiệm thu: JPG/PNG/WebP <=2 MB, kiểm tra content, random storage key/path traversal; Student không truy cập file riêng của người khác. Không fetch URL tùy ý server, error/fallback không crash.
  - Input upload/URL dùng model/DTO chuyên biệt; không bind StoredFile/domain entity trực tiếp.

### Phase Acceptance Criteria / Gate P1

Điều kiện: hoàn thành P1-01, P1-02, P1-03, P1-04, P1-05; kiểm tra/bằng chứng dưới đây đạt trước khi chuyển phase tiếp theo.

- Nghiệm thu: build, auth/CSRF/ownership và DB uniqueness tests pass; login/đổi mật khẩu/lock đang đăng nhập và VI/EN thử được. Không add lại quên mật khẩu hoặc email verification.
- Xác nhận global/area CSS đúng phạm vi, Student/Admin không đổi landing; form/input dùng model/DTO riêng; không generic repository hoặc UnitOfWork mặc định.

## 6. P2 — Phân loại, Vocabulary và Grammar

- [ ] **P2-01 — Schema Content theo Database First**
  - Phụ thuộc: Gate P1.
  - Đầu ra: LearningItem/shared PK subtype Vocabulary/Grammar, VocabularyMeaning, basic UserLearningProgress; mapping Topic/Group và Admin CRUD phân loại.
  - Nghiệm thu: Level required; VocabularyMeaning FK thật, MeaningVi nonempty, DisplayOrder dương/unique mỗi từ. Topic Vocabulary required, GrammarGroup rule; không MeaningVi đơn hoặc serialized meanings trên Vocabulary. Xóa nhóm có nội dung bị chặn đến khi chuyển, A1–C2 cố định.

- [ ] **P2-02 — Vocabulary end-to-end**
  - Phụ thuộc: P2-01.
  - Đầu ra: form thêm/sửa từ, collection editor meanings có reorder, URL ảnh/audio, visible/delete.
  - Nghiệm thu: từ có >=1 nghĩa, thêm/sửa/bỏ/reorder atomically; không bỏ nghĩa cuối; rollback lỗi không làm mất danh sách. Khác loại từ là mục riêng, soft delete từ giữ meaning con.
  - Đầu ra: list/detail/find English hoặc bất kỳ meaning; filter level/topic/learned; play URL nếu có, đánh dấu học.
  - Nghiệm thu: nghĩa thứ hai tìm được; nhiều nghĩa cùng khớp không lặp từ; thứ tự display đúng; thiếu audio ẩn nút, không TTS. Đánh dấu độc lập bài tập.
  - Form/input: collection meanings nằm trong ViewModel/InputModel, kiểm tra ID nghĩa thuộc đúng Vocabulary khi cập nhật; không bind đồ thị domain entity từ request.

- [ ] **P2-03 — Grammar end-to-end**
  - Phụ thuộc: P2-01, P1-04.
  - Đầu ra: CRUD/read theo nhóm/level, formula/usage/examples/notes; sanitizer và editor đậm/nghiêng/list/table; tabs lý thuyết/luyện tập.
  - Nghiệm thu: không khóa tab theo tiến độ, bài tập được nối ở P4; tìm tên/filter/mark/unmark; nội dung rich text không chạy script/event handler.

### Phase Acceptance Criteria / Gate P2

Điều kiện: hoàn thành P2-01, P2-02, P2-03; kiểm tra/bằng chứng dưới đây đạt trước khi chuyển phase tiếp theo.

- Nghiệm thu: SQL/HTTP regression cho meaning order/transaction/search, required fields/soft delete, visibility/ownership, filter/pagination và empty state; snapshot bài tập không phụ thuộc live meaning khi nối P4.

## 7. P3 — Listening, Reading và Books

- [ ] **P3-01 — Listening end-to-end**
  - Phụ thuộc: Gate P2, P1-05.
  - Đầu ra: Listening subtype, TranscriptCue/file, Admin audio URL/subtitle import.
  - Nghiệm thu: Topic/Level/audio required; SRT/VTT <=1 MB, lỗi mốc/cấu trúc báo rõ; source file+cue lưu nhất quán, không orphan/HTML injection.
  - Nghiệm thu: chữ mặc định hiện, ẩn/hiện, highlight/tự cuộn từng câu đúng khi nghe và seek; nghe lại/tua tự do, không speed control; media fail có thông báo; learned toggle và tìm/lọc; nhiều bộ bài tập nối P4.
  - Form/input: audio/subtitle request model riêng; player styles ở student.css hoặc module scope, không rò sang landing.

- [ ] **P3-02 — Reading end-to-end — Topic optional**
  - Phụ thuộc: Gate P2.
  - Đầu ra: schema/subtype và Admin/Student CRUD/list/detail cho Reading, Level required, TopicId nullable.
  - Nghiệm thu: đọc không Topic lưu/list/detail/mark và gắn exercise được; có Topic thì FK đúng; query không inner join làm mất bài không Topic. Bản dịch optional mặc định ẩn, vocabulary notes bên dưới; level/topic/status filters không tự biến Topic thành bắt buộc.

- [ ] **P3-03 — Books & Chapters**
  - Phụ thuộc: Gate P2.
  - Đầu ra: schema Books, Chapters, chapter progress/reading position; Admin nhập text, Student đọc/đánh dấu/đọc tiếp.
  - Nghiệm thu: Book có Topic, không Level; thứ tự chương duy nhất; mở tự do, dịch tùy chọn, không exercises/PDF/EPUB; nhớ chương không nhớ scroll; hide/delete giữ dấu cũ.

### Phase Acceptance Criteria / Gate P3

Điều kiện: hoàn thành P3-01, P3-02, P3-03; kiểm tra/bằng chứng dưới đây đạt trước khi chuyển phase tiếp theo.

- Nghiệm thu: Reading null Topic pass cả SQL/HTTP; Vocabulary/Listening/Writing rule Topic không bị nới lỏng theo. Parser/seek/bản dịch/chapter position, M2 apply incremental, empty/missing-media UI pass.

## 8. P4 — Exercise, snapshots và Result

- [ ] **P4-01 — Schema M3 và Admin soạn bài**
  - Phụ thuộc: Gate P3.
  - Đầu ra: Exercise, Question, AnswerOption, AcceptedAnswer; owner Vocabulary topic+level hoặc Grammar/Listening/Reading; Admin create/update/order/delete.
  - Nghiệm thu: owner XOR, không Writing/Book; multiple-choice >=2 options đúng một correct, True/False đúng hai/one correct; fill một blank với nhiều accepted answers; không bank/randomization. Các module hỗ trợ nhiều exercise.

- [ ] **P4-02 — Start và immutable snapshots**
  - Phụ thuộc: P4-01.
  - Đầu ra: Attempt/AttemptQuestion/AttemptSnapshot/AttemptAnswer, active unique key, start service và dữ liệu câu hỏi an toàn cho browser.
  - Nghiệm thu: snapshot nhất quán gồm đáp án/explanation/stimulus nhưng keys không gửi UI; Admin sửa/ẩn/xóa không đổi lượt cũ; một active/source/user; không autosave đáp án tự học.

- [ ] **P4-03 — Làm bài, submit và scoring**
  - Phụ thuộc: P4-02.
  - Nghiệm thu: giữ thứ tự, cảnh báo câu trống, nộp cả bộ và khóa; normalize blank case/whitespace, typo sai; thang100 decimal hai chữ số, đúng/sai/trống; idempotent double-submit, không nhận score client, abandon/reload không result.

- [ ] **P4-04 — Student history và Admin results**
  - Phụ thuộc: P4-03.
  - Đầu ra: history newest first, detail selected/correct/explanation, timestamps/duration; Admin filter học viên/loại/ngày read-only.
  - Nghiệm thu: học viên chỉ xem của mình, Admin không sửa điểm; hidden/deleted source không làm mất kết quả; làm lại tạo attempt mới.

### Phase Acceptance Criteria / Gate P4

Điều kiện: hoàn thành P4-01, P4-02, P4-03, P4-04; kiểm tra/bằng chứng dưới đây đạt trước khi chuyển phase tiếp theo.

- Nghiệm thu: SQL owner/active/concurrency tests; scoring normalization/denominator0 prevented, duplicate submit, snapshot immutable, no leaked answer trước submit; Reading không Topic vẫn làm bài được.

## 9. P5 — Writing và AI

- [ ] **P5-01 — WritingTopic và versioning**
  - Phụ thuộc: Gate P4.
  - Đầu ra: WritingTopic theo Topic/Level bắt buộc; UserWriting, WritingVersion, current-version; Admin đề/Student write/save/history.
  - Nghiệm thu: nhiều bài trên một đề, text thuần/đếm từ, 20.000 ký tự; rỗng từ chối, không word-count gate. Save y hệt không duplicate version; manual save/cảnh báo chưa lưu; soft delete Admin vẫn xem.

- [ ] **P5-02 — AI adapter và cấu hình**
  - Phụ thuộc: P5-01, P0-01.
  - Đầu ra: IWritingEvaluator, một adapter thật, typed HttpClient/options, secret/provider/model/timeout; DTO validation.
  - Nghiệm thu: vendor không lọt domain/controller, thiếu key không ngăn save; input coi là dữ liệu, không tool access; feedback Việt, không viết lại toàn bài; provider response lỗi/out of range bị reject.

- [ ] **P5-03 — Evaluation lifecycle và concurrency**
  - Phụ thuộc: P5-02.
  - Đầu ra: Evaluation records/lease, RunNumber, unique success/version, một running/writing, retry explicit.
  - Nghiệm thu: không transaction DB dài lúc gọi AI; cùng version success trả cached; timeout/error giữ bài, no fake score. Sửa/lưu lúc chấm không gắn nhầm version; double-click/multi-tab không tạo hai successful records. Overall mean bốn score 25%, round một chữ số; đủ metadata/prompt snapshot.

- [ ] **P5-04 — UI AI và latest-success score**
  - Phụ thuộc: P5-03.
  - Nghiệm thu: Lưu/Chấm riêng; đang chấm/lỗi/thử lại rõ; lịch sử từng version; bản mới chưa chấm vẫn hiển thị last successful score với ghi chú; xóa bài loại khỏi summary nhưng Admin xem được. Không quota ngày/chọn ngôn ngữ AI.

### Phase Acceptance Criteria / Gate P5

Điều kiện: hoàn thành P5-01, P5-02, P5-03, P5-04; kiểm tra/bằng chứng dưới đây đạt trước khi chuyển phase tiếp theo.

- Nghiệm thu: boundary text, duplicate save, concurrent versions/evaluations/delete, provider schema/timeout, rounding, latest score tests pass. Adapter fake chỉ test; một smoke thật khi có credential không lộ bài/key trong log.

## 10. P6 — TOEIC authoring và publication

- [ ] **P6-01 — TOEIC schema & format**
  - Phụ thuộc: Gate P5, P4-01.
  - Đầu ra: ToeicFormatProfile versioned, Test/Section/Part, QuestionGroup/GroupStimulus; liên kết Question XOR và same-Part constraints.
  - Nghiệm thu: 7 Part cấu hình trong profile, không controller constants; group chỉ một Part/đề, nhiều stimulus có thứ tự; TOEIC single-choice theo format (không free-text Part5/6); active snapshot giữ profile đã chọn.

- [ ] **P6-02 — Admin TOEIC authoring**
  - Phụ thuộc: P6-01.
  - Đầu ra: nhập từng phần/câu/option/answer/stimulus/audio URL, lưu Draft chưa đủ, hiển thị errors theo vị trí.
  - Nghiệm thu: không crawler/import Study4; section audio Full và practice stimulus chuẩn bị riêng; Draft không Student thấy, data cũ giữ khi validation fail.
  - Form/input: DTO/ViewModel riêng cho đề/Part/group/câu hỏi; không bind trực tiếp graph domain entity từ form Admin.

- [ ] **P6-03 — Validation, capabilities & publication**
  - Phụ thuộc: P6-02.
  - Nghiệm thu Practice/MVP: Part có ít nhất 1 câu hợp lệ, đủ options/one correct/stimulus bắt buộc được Practice. Đây không phải số câu chuẩn Full; không lấy count Full làm ngưỡng Practice. Câu không hợp lệ không được đưa vào lượt.
  - Nghiệm thu Full: profile Part1–7 = 6/25/39/30/30/16/54, sections 100+100, group counts theo PLAN, audio/timing đủ; chỉ 7 Part mỗi Part một câu vẫn không Full.
  - Nghiệm thu: Admin bấm Publish mới hiển thị; >=1 Part valid bật Practice tương ứng. Listening cần mọi Part1–4 valid, Reading cần mọi Part5–7 valid; Full chỉ validator Full pass. Published edit về Draft; hide/show tính lại eligibility, lượt cũ không ảnh hưởng.

### Phase Acceptance Criteria / Gate P6

Điều kiện: hoàn thành P6-01, P6-02, P6-03; kiểm tra/bằng chứng dưới đây đạt trước khi chuyển phase tiếp theo.

- Nghiệm thu: Part1 một câu đủ dữ liệu Practice được/Full bị chặn; thiếu answer/stimulus fail; partial skill capability đúng; group cross-Part bị chặn. Fixture full chuẩn và lỗi group/part/số câu có cùng tổng200 vẫn phân biệt được.

## 11. P7 — TOEIC Practice và Full Test

- [ ] **P7-01 — Practice scopes**
  - Phụ thuộc: Gate P6, P4-02.
  - Đầu ra: list published/capabilities, chọn một Part/toàn Listening/toàn Reading, start snapshot, question grid/flag.
  - Nghiệm thu: không multi-select Part tùy ý, không timer, nghe lại/tua tự do, chưa xem transcript/đáp án khi làm; kết quả theo tổng câu thực của scope; không resume/reload.

- [ ] **P7-02 — Full start và server clock**
  - Phụ thuộc: P7-01.
  - Đầu ra: AttemptSection/deadlines, token RAM/hash server, clock injection, section player.
  - Nghiệm thu: T0/ListeningEnd/ReadingEnd do server, 45/75 phút profile; readiness/gesture trước Start, không phát xong sớm mà chuyển section; không nhận Reading trước giờ, Listening sau khóa; Full không tua/nghe lại. Timer client chỉ hiển thị.

- [ ] **P7-03 — Autosave có ACK và thứ tự**
  - Phụ thuộc: P7-02.
  - Đầu ra: batch/debounce/nhịp kiểm tra cấu hình theo PLAN, sequence, ACK sau commit, status UI.
  - Nghiệm thu: owner/active/token/clock guard trên server, không backdate; retry/out-of-order không overwrite mới; rowversion/transaction tránh deadline race; pending offline chỉ RAM, không localStorage/IndexedDB.
  - AJAX input: autosave request DTO riêng cho answers/sequence/token; không nhận entity Attempt/AttemptAnswer hoặc timestamp/score/status do client quyết định.

- [ ] **P7-04 — Submit/finalize và offline deadline**
  - Phụ thuộc: P7-03.
  - Nghiệm thu: submit sớm commit batch hợp lệ/finalize atomically; hết giờ khóa và chỉ dùng server-saved trước hạn từng section. Reconnect sau hạn không nhận pending mới; không answer record thì không ghi result; một finalize dù double call. Không làm mất ACK trước hạn do client mất mạng đúng lúc cuối.

- [ ] **P7-05 — Thoát, abandon và no-resume**
  - Phụ thuộc: P7-04.
  - Đầu ra: modal Tiếp tục/Nộp và thoát/Bỏ; audio failure cancel, pagehide best-effort, clear stale slot, cleanup orphan.
  - Nghiệm thu: abandon không result/history Admin; không trả lại token/answers để resume; tab gốc tiếp tục nếu chưa bỏ, tab mới không duplicate. Cleanup không tự chấm mọi expired attempt khi browser mất; thiếu heartbeat tạm không bị bỏ ngay. Đóng tab bất thường nêu giới hạn, không hứa xử lý hoàn hảo.

- [ ] **P7-06 — TOEIC results**
  - Phụ thuộc: P7-04, P4-04.
  - Nghiệm thu: số đúng/tổng và percent decimal hai chữ số hiển thị, không scale TOEIC; Full denominator200, Practice theo snapshot; breakdown section/Part đúng. Sau submit mới hiện keys/explanation/transcript/bản dịch nếu có; Admin read-only.

### Phase Acceptance Criteria / Gate P7

Điều kiện: hoàn thành P7-01, P7-02, P7-03, P7-04, P7-05, P7-06; kiểm tra/bằng chứng dưới đây đạt trước khi chuyển phase tiếp theo.

- Nghiệm thu: fake-clock tests ngay trước/tại/sau hai deadline, audio end sớm, offline trước/sau hạn, mixed section batch, seq cũ, lock chờ quá hạn, submit/autosave race, no records, no resume, partial vs full counts. Browser demo đúng ba lựa chọn, progress grid/flag, no keys trước submit.

## 12. P8 — Dashboard, lộ trình, tiến độ

- [ ] **P8-01 — Tiến độ nội dung và sách**
  - Phụ thuộc: Gate P7, P2-02, P3-03.
  - Nghiệm thu: mark/unmark tự học độc lập bài tập; denominator chỉ visible/nondeleted, dấu cũ giữ khi restore; Books không Level, complete khi các chương hiện hữu visible đã đọc, không chia0.

- [ ] **P8-02 — Writing completion và aggregate score**
  - Phụ thuộc: P8-01, P5-04.
  - Nghiệm thu: Topic hoàn thành nếu còn >=1 bài không rỗng/chưa xóa; xóa bài cuối mất complete; latest-success còn dùng khi version mới chưa chấm; AI mới thành công thay điểm summary. Không lấy trung bình mọi version/lần thử.

- [ ] **P8-03 — Dashboard/Roadmap/Admin readouts**
  - Phụ thuộc: P8-02, P7-06.
  - Đầu ra: chọn/nhớ level A1–C2, grouping kỹ năng, tiến độ, recent activity, averages tự học100/AI10, TOEIC records riêng.
  - Nghiệm thu: Reading null Topic vẫn thuộc level và xuất hiện; không khóa level; no content empty; không biểu đồ phức tạp/roadmap cá nhân hóa; history/Admin lọc đầy đủ và không lộ cross-user.

### Phase Acceptance Criteria / Gate P8

Điều kiện: hoàn thành P8-01, P8-02, P8-03; kiểm tra/bằng chứng dưới đây đạt trước khi chuyển phase tiếp theo.

- Nghiệm thu: dữ liệu nhiều lần làm/chấm, hidden/delete/restore, sách/chương, writing cuối bị xóa và bản sửa chưa chấm cho summary đúng; không N+1/query toàn DB/pagination mất ổn định.

## 13. P9 — Seed, hoàn thiện và bàn giao implementation

- [ ] **P9-01 — Seed demo có nguồn rõ ràng**
  - Phụ thuộc: Gate P8.
  - Đầu ra: DemoEnabled idempotent; Admin từ secret, 2 Student có/trống lịch sử, A1/A2 vừa đủ, B1–C2 mẫu/empty, 2–3 sách vài chương, Part1–7 sample.
  - Nghiệm thu: ít nhất một Vocabulary nhiều nghĩa/order; một Reading không Topic và một có Topic; Part một câu chứng minh Practice không Full. Không overwrite Admin sửa; không fake AI success; không crawl/copy Study4. Full demo thật optional, Full test fixture vẫn bắt buộc.

- [ ] **P9-02 — Rà UI/localization/accessibility**
  - Phụ thuộc: P9-01.
  - Nghiệm thu: Account/Student/Admin VI/EN, mặc định Việt, layout dark/sidebar/mobile, keyboard/focus, empty/success/error/loading, không overflow. Landing không bị global CSS thay đổi; nội dung học không bị dịch tự động.
  - Kiểm tra tổ chức CSS: global chỉ shared; Student/Admin area styles không ảnh hưởng landing ở desktop/mobile.

- [ ] **P9-03 — Hồi quy security/database/logic**
  - Phụ thuộc: P9-02.
  - Nghiệm thu: build + relevant/full regression pass, database deployment scripts clean/incremental khi có thay đổi schema được phê duyệt, FK/index/transaction verified. Upload/XSS/CSRF/owner/active user, keys không leak, AI failures và deadline tests pass; không secret trong repo/log.
  - Rà quy ước code: mọi form MVC/AJAX dùng input model/DTO phù hợp; không bind domain entity; không GenericRepository<T>/UnitOfWork mặc định. Query abstraction chuyên biệt nếu có phải ghi lý do kỹ thuật.

- [ ] **P9-04 — Hướng dẫn chạy, backup/restore và demo**
  - Phụ thuộc: P9-03.
  - Đầu ra: tài liệu cấu hình DB/uploads/AI secrets, apply schema script/seed có kiểm soát khi được phê duyệt, cách chạy Student/Admin; backup DB+files và restore thử.
  - Nghiệm thu: run được trên máy cá nhân theo hướng dẫn; thiếu key/media báo đúng; demo Practice partial và timing Full fixture phân biệt dữ liệu minh họa với đề thật. Ghi bằng chứng và giới hạn còn lại, không tuyên bố sản phẩm production-ready.

### Phase Acceptance Criteria / Gate P9

Điều kiện: hoàn thành P9-01, P9-02, P9-03, P9-04; kiểm tra/bằng chứng dưới đây đạt trước khi bàn giao implementation.

- Seed/demo, UI, hồi quy và run guide/backup đạt các nghiệm thu P9-01 đến P9-04; có bằng chứng build/tests/demo/restore và giới hạn còn lại, không tuyên bố production-ready.

## 14. Checklist thay đổi schema theo Database First

Chỉ áp dụng khi thay đổi schema đã được phê duyệt rõ ràng, gắn vào task có schema:

- Hoàn tất field/nullability/PK/FK/index/delete matrix trước khi sửa SQL Server.
- Kiểm tra SQL và model scaffold, đặc biệt VocabularyMeaning FK/order; Reading.TopicId nullable, Topic module khác Required.
- Chạy trên DB test sạch và nâng từ phase trước có dữ liệu; unique filtered indexes/rowversion dùng SQL Server thực.
- Xác định DB đích, backup trước thay đổi có dữ liệu; không tự drop/reset hoặc hard delete lịch sử.
- Dùng script SQL idempotent hoặc command apply có kiểm soát; không auto update schema khi production startup.
- Reverse engineer lại `ApplicationDbContext`/entities từ schema đã xác minh, bảo toàn phần application-owned; không dùng `--force` khi chưa kiểm tra thay đổi. Demo seed opt-in, không secrets; verify seed chạy lại không duplicate/overwrite.

## 15. Truy vết các yêu cầu dễ sai

Mã task và Gate dưới đây dùng revision 2 hiện tại; không dùng ID cũ hoặc dạng viết tắt gây nhầm sau renumber.

| Quyết định đã duyệt | Tasks / Phase Gate chịu trách nhiệm | Bằng chứng phải có khi hoàn thành |
|---|---|---|
| Nhiều nghĩa relational có thứ tự | P0-01, P2-01, P2-02; Gate P2; P9-01 | DB nhiều rows, reorder atomic, search nghĩa thứ hai không duplicate |
| Reading Topic optional, Level required | P0-01, P3-02; Gate P3; Gate P4; P8-03 | Null Topic CRUD/học/exercise/roadmap pass; module khác vẫn required |
| Practice >=1 câu không phải Full standard | P6-03; Gate P6; P7-01, P7-06; P9-01 | Một câu Practice pass, Full fail; denominator theo scope |
| TOEIC đúng/tổng/percent, không scale990 | P7-06; Gate P7 | Counts và decimal percent cho Full/Practice |
| Server autosave nhưng không resume | P7-02, P7-03, P7-04, P7-05; Gate P7 | Reconnect dùng saved-before-deadline, reload không tiếp tục |
| Snapshot giữ đề cũ | P4-02; Gate P4; P6-03; Gate P7 | Admin sửa/xóa không đổi attempt/results |
| AI chấm version riêng, latest success | P5-01, P5-03, P5-04; Gate P5; P8-02 | Race/version mới/timeout/retry không giả điểm hoặc gắn nhầm |
| Hidden/delete giữ history | P4-04, P5-04, P8-01, P8-02; Gate P8 | History cũ còn, summary hiện tại tính đúng |
| Authorization/mật khẩu theo baseline | P1-02, P1-03; Gate P1; P9-03 | 6 ký tự/no lockout, Admin khóa request sau, owner checks |
| Landing giữ nguyên, VI/EN khu vực mới | P1-04, P9-02 | UI comparison và culture navigation |
| Sources/demo không crawler/AI fake | P9-01, P9-04 | Nguồn/tài khoản/seed idempotent và unavailable states |
| CSS shared/area | P0-01, P1-04; Gate P1; P9-02 | global chỉ shared; stylesheet khu vực không ảnh hưởng landing |
| MVC form và AJAX input models/DTO | P0-01, P1-02, P1-03, P1-05, P2-02, P6-02, P7-03, P9-03 | Mọi form Student/Admin không bind entity; autosave DTO kiểm soát fields |
| Không generic repository/UnitOfWork mặc định | P0-01, P1-01; Gate P1; P9-03 | Services dùng DbContext trực tiếp; abstraction riêng chỉ khi có lý do |

## 16. Definition of Done và điểm dừng hiện tại

Khi implementation được cho phép, một task hoàn thành khi: đầu ra đúng PLAN, build hoạt động, authorization/validation/UI/localization tương ứng đầy đủ, test có ý nghĩa pass, không phá dữ liệu/landing, có ghi bằng chứng. Không đánh dấu complete chỉ vì tạo file hoặc xong happy path.

**Hiện tại P1-01 và P1-02 đã hoàn thành; các task chức năng còn lại chưa bắt đầu. Không tiếp tục P1-03, thay đổi schema, tạo migration hoặc chạy seed cho đến khi có yêu cầu implementation riêng.**


## 17. Tổng kết refactor revision 2

| Phase | Task trước | Task sau | Thay đổi |
|---|---:|---:|---|
| P0 | 3 | 1 | Gộp chuẩn bị môi trường/schema/adapters; thêm Gate P0 |
| P1 | 6 | 5 | Giữ năm đầu ra chính; chuyển kiểm tra sang Gate P1 |
| P2 | 5 | 3 | Gộp Admin/Student Vocabulary end-to-end; giữ schema và Grammar; Gate P2 |
| P3 | 5 | 3 | Gộp schema/import/player Listening end-to-end; giữ Reading và Books; Gate P3 |
| P4 | 5 | 4 | Giữ authoring, snapshot, scoring, history riêng; Gate P4 |
| P5 | 5 | 4 | Giữ version, adapter, concurrency, UI score riêng; Gate P5 |
| P6 | 5 | 3 | Gộp validators/capabilities/publication; Gate P6 |
| P7 | 7 | 6 | Giữ riêng sáu phần rủi ro; Gate P7 |
| P8 | 4 | 3 | Giữ ba feature tiến độ/dashboard; Gate P8 |
| P9 | 4 | 4 | Giữ bốn đầu ra cuối; tổng hợp Gate P9 |
| **Tổng** | **49** | **36** | **Bớt 13 task; không loại acceptance criteria** |

36 task là tổng của cơ cấu chi tiết yêu cầu (1+5+3+3+4+4+3+6+3+4). Không gộp thêm các ranh giới snapshot/scoring hoặc timing/autosave/finalize chỉ để đạt mục tiêu gần đúng 28–32. Gate là điều kiện review phase, không task implementation bổ sung. PLAN.md và baseline nghiệp vụ không thay đổi.
