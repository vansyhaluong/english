using EnglishHub.Models;

namespace EnglishHub.Data;

// Curated preview content. Replace through the content service when persistence is implemented.
public static class VocabularySamples
{
    public static IReadOnlyList<VocabularyDeck> Decks { get; } = [
        new("greetings", "👋", "Chào hỏi & Hằng ngày", "A1", "Những từ đầu tiên để làm quen và bắt đầu một cuộc trò chuyện.", [
            new("hello", "xin chào", "/həˈləʊ/", "thán từ", "Hello! My name is Anna."),
            new("welcome", "chào mừng", "/ˈwelkəm/", "thán từ", "Welcome to our home."),
            new("friend", "người bạn", "/frend/", "danh từ", "This is my friend, Tom."),
            new("thanks", "cảm ơn", "/θæŋks/", "thán từ", "Thanks for your help.")]),
        new("core-a1", "🌱", "Từ vựng cốt lõi A1", "A1", "Bắt đầu từ những từ tiếng Anh quen thuộc nhất.", [
            new("home", "nhà; tổ ấm", "/həʊm/", "danh từ", "I walk home after school."),
            new("learn", "học", "/lɜːn/", "động từ", "We learn English every day."),
            new("happy", "vui vẻ; hạnh phúc", "/ˈhæpi/", "tính từ", "I am happy to see you.")]),
        new("core-a2", "🌿", "Từ vựng cốt lõi A2", "A2", "Mở rộng vốn từ cho các tình huống thường ngày.", [
            new("journey", "chuyến đi", "/ˈdʒɜːni/", "danh từ", "Have a safe journey!"),
            new("improve", "cải thiện", "/ɪmˈpruːv/", "động từ", "I want to improve my English."),
            new("careful", "cẩn thận", "/ˈkeəfəl/", "tính từ", "Be careful on the road.")]),
        new("time", "🕐", "Số & Thời gian", "A1", "Học đếm, xem giờ và nói về lịch trình của bạn.", [
            new("morning", "buổi sáng", "/ˈmɔːnɪŋ/", "danh từ", "I read in the morning."),
            new("twelve", "mười hai", "/twelv/", "số từ", "There are twelve months in a year."),
            new("today", "hôm nay", "/təˈdeɪ/", "trạng từ", "Today is Monday.")]),
        new("core-b1", "🌳", "Từ vựng cốt lõi B1", "B1", "Tự tin kể chuyện và diễn đạt ý kiến bằng tiếng Anh.", [
            new("achieve", "đạt được", "/əˈtʃiːv/", "động từ", "You can achieve your goals."),
            new("experience", "kinh nghiệm; trải nghiệm", "/ɪkˈspɪəriəns/", "danh từ", "It was a wonderful experience."),
            new("confident", "tự tin", "/ˈkɒnfɪdənt/", "tính từ", "She feels confident about the exam.")]),
        new("food", "🍞", "Ăn & Uống", "A1", "Gọi món, đi chợ và nói về món ăn yêu thích.", [
            new("bread", "bánh mì", "/bred/", "danh từ", "I eat bread for breakfast."),
            new("water", "nước", "/ˈwɔːtə/", "danh từ", "May I have some water?"),
            new("hungry", "đói", "/ˈhʌŋɡri/", "tính từ", "I am hungry. Let's eat.")]),
        new("core-b2", "🏔️", "Từ vựng cốt lõi B2", "B2", "Diễn đạt ý tưởng rõ ràng trong học tập và công việc.", [
            new("perspective", "góc nhìn; quan điểm", "/pəˈspektɪv/", "danh từ", "Try to see it from her perspective."),
            new("contribute", "đóng góp", "/kənˈtrɪbjuːt/", "động từ", "Everyone can contribute to the discussion."),
            new("reliable", "đáng tin cậy", "/rɪˈlaɪəbəl/", "tính từ", "This is a reliable source.")]),
        new("core-c1", "🎓", "Từ vựng cốt lõi C1", "C1", "Nắm bắt sắc thái và sử dụng từ vựng học thuật.", [
            new("nuance", "sắc thái", "/ˈnjuːɑːns/", "danh từ", "The translation captures every nuance."),
            new("compelling", "thuyết phục; hấp dẫn", "/kəmˈpelɪŋ/", "tính từ", "She made a compelling argument."),
            new("scrutinize", "xem xét kỹ lưỡng", "/ˈskruːtɪnaɪz/", "động từ", "We need to scrutinize the evidence.")]),
        new("core-c2", "🏆", "Từ vựng cốt lõi C2", "C2", "Khám phá cách diễn đạt tinh tế và chính xác.", [
            new("ubiquitous", "có mặt khắp nơi", "/juːˈbɪkwɪtəs/", "tính từ", "Smartphones are ubiquitous today."),
            new("meticulous", "tỉ mỉ", "/məˈtɪkjələs/", "tính từ", "The work requires meticulous attention."),
            new("eloquent", "hùng biện; lưu loát", "/ˈeləkwənt/", "tính từ", "He gave an eloquent speech.")]),
        new("family", "🏡", "Gia đình & Bạn bè", "A1", "Giới thiệu những người thân yêu quanh bạn.", [
            new("sister", "chị; em gái", "/ˈsɪstə/", "danh từ", "My sister is a teacher."),
            new("parent", "cha hoặc mẹ", "/ˈpeərənt/", "danh từ", "Every parent wants their child to be happy."),
            new("together", "cùng nhau", "/təˈɡeðə/", "trạng từ", "We cook together.")]),
        new("travel", "✈️", "Du lịch & Khám phá", "A2", "Sẵn sàng cho chuyến đi tiếp theo của bạn.", [
            new("passport", "hộ chiếu", "/ˈpɑːspɔːt/", "danh từ", "Please show your passport."),
            new("luggage", "hành lý", "/ˈlʌɡɪdʒ/", "danh từ", "Where is my luggage?"),
            new("arrive", "đến nơi", "/əˈraɪv/", "động từ", "We arrive at six.")]),
        new("work", "💼", "Công việc & Sự nghiệp", "B1", "Giao tiếp với đồng nghiệp và chia sẻ kế hoạch.", [
            new("colleague", "đồng nghiệp", "/ˈkɒliːɡ/", "danh từ", "My colleague helped me."),
            new("deadline", "hạn chót", "/ˈdedlaɪn/", "danh từ", "The deadline is Friday."),
            new("apply", "ứng tuyển; áp dụng", "/əˈplaɪ/", "động từ", "I will apply for this job.")]),
        new("nature", "🌍", "Thiên nhiên & Môi trường", "B2", "Thảo luận về thế giới và những thay đổi quanh ta.", [
            new("sustainable", "bền vững", "/səˈsteɪnəbəl/", "tính từ", "We need sustainable energy."),
            new("habitat", "môi trường sống", "/ˈhæbɪtæt/", "danh từ", "Protect the natural habitat."),
            new("preserve", "bảo tồn; giữ gìn", "/prɪˈzɜːv/", "động từ", "We must preserve our forests.")]),
        new("study", "📚", "Trường học & Học tập", "A2", "Những từ đồng hành cùng bạn trong lớp học.", [
            new("subject", "môn học", "/ˈsʌbdʒɪkt/", "danh từ", "English is my favourite subject."),
            new("revise", "ôn tập", "/rɪˈvaɪz/", "động từ", "I revise before a test."),
            new("library", "thư viện", "/ˈlaɪbrəri/", "danh từ", "The library opens at nine.")])
    ];
}
