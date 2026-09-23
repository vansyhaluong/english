using System;
using System.Collections.Generic;
using English.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace English.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AcceptedAnswer> AcceptedAnswers { get; set; }

    public virtual DbSet<AnswerOption> AnswerOptions { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<Attempt> Attempts { get; set; }

    public virtual DbSet<AttemptAnswer> AttemptAnswers { get; set; }

    public virtual DbSet<AttemptQuestion> AttemptQuestions { get; set; }

    public virtual DbSet<AttemptSection> AttemptSections { get; set; }

    public virtual DbSet<AttemptSnapshot> AttemptSnapshots { get; set; }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<BookChapter> BookChapters { get; set; }

    public virtual DbSet<BookReadingPosition> BookReadingPositions { get; set; }

    public virtual DbSet<Exercise> Exercises { get; set; }

    public virtual DbSet<GrammarGroup> GrammarGroups { get; set; }

    public virtual DbSet<GrammarLesson> GrammarLessons { get; set; }

    public virtual DbSet<GroupStimulus> GroupStimuli { get; set; }

    public virtual DbSet<LearningItem> LearningItems { get; set; }

    public virtual DbSet<Level> Levels { get; set; }

    public virtual DbSet<ListeningLesson> ListeningLessons { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<QuestionGroup> QuestionGroups { get; set; }

    public virtual DbSet<ReadingLesson> ReadingLessons { get; set; }

    public virtual DbSet<StoredFile> StoredFiles { get; set; }

    public virtual DbSet<ToeicFormatProfile> ToeicFormatProfiles { get; set; }

    public virtual DbSet<ToeicPart> ToeicParts { get; set; }

    public virtual DbSet<ToeicSection> ToeicSections { get; set; }

    public virtual DbSet<ToeicTest> ToeicTests { get; set; }

    public virtual DbSet<Topic> Topics { get; set; }

    public virtual DbSet<TranscriptCue> TranscriptCues { get; set; }

    public virtual DbSet<UserChapterProgress> UserChapterProgresses { get; set; }

    public virtual DbSet<UserLearningProgress> UserLearningProgresses { get; set; }

    public virtual DbSet<UserWriting> UserWritings { get; set; }

    public virtual DbSet<Vocabulary> Vocabularies { get; set; }

    public virtual DbSet<VocabularyMeaning> VocabularyMeanings { get; set; }

    public virtual DbSet<WritingEvaluation> WritingEvaluations { get; set; }

    public virtual DbSet<WritingTopic> WritingTopics { get; set; }

    public virtual DbSet<WritingVersion> WritingVersions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AcceptedAnswer>(entity =>
        {
            entity.ToTable("AcceptedAnswer");

            entity.HasIndex(e => new { e.QuestionId, e.NormalizedText }, "UQ_AcceptedAnswer_Normalized").IsUnique();

            entity.Property(e => e.NormalizedText).HasMaxLength(400);
            entity.Property(e => e.Text).HasMaxLength(400);

            entity.HasOne(d => d.Question).WithMany(p => p.AcceptedAnswers)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AcceptedAnswer_Question");
        });

        modelBuilder.Entity<AnswerOption>(entity =>
        {
            entity.ToTable("AnswerOption");

            entity.HasIndex(e => new { e.QuestionId, e.DisplayOrder }, "UQ_AnswerOption_Order").IsUnique();

            entity.HasOne(d => d.Question).WithMany(p => p.AnswerOptions)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AnswerOption_Question");
        });

        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.HasIndex(e => e.NormalizedEmail, "UQ_AspNetUsers_NormalizedEmail").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_AspNetUsers_IsActive");
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);

            entity.HasOne(d => d.AvatarFile).WithMany(p => p.AspNetUsers)
                .HasForeignKey(d => d.AvatarFileId)
                .HasConstraintName("FK_AspNetUsers_AvatarFile");

            entity.HasOne(d => d.SelectedLevel).WithMany(p => p.AspNetUsers)
                .HasForeignKey(d => d.SelectedLevelId)
                .HasConstraintName("FK_AspNetUsers_SelectedLevel");
        });

        modelBuilder.Entity<Attempt>(entity =>
        {
            entity.ToTable("Attempt");

            entity.HasIndex(e => new { e.UserId, e.Status, e.SubmittedAtUtc, e.Id }, "IX_Attempt_User_History").IsDescending(false, false, true, false);

            entity.HasIndex(e => new { e.UserId, e.ExerciseId }, "UX_Attempt_Active_Exercise")
                .IsUnique()
                .HasFilter("([Status]=(1) AND [ExerciseId] IS NOT NULL)");

            entity.HasIndex(e => new { e.UserId, e.ToeicTestId }, "UX_Attempt_Active_Toeic")
                .IsUnique()
                .HasFilter("([Status]=(1) AND [ToeicTestId] IS NOT NULL)");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.ScorePercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Status).HasDefaultValue((byte)1, "DF_Attempt_Status");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(32)
                .IsFixedLength();

            entity.HasOne(d => d.Exercise).WithMany(p => p.Attempts)
                .HasForeignKey(d => d.ExerciseId)
                .HasConstraintName("FK_Attempt_Exercise");

            entity.HasOne(d => d.ToeicTest).WithMany(p => p.Attempts)
                .HasForeignKey(d => d.ToeicTestId)
                .HasConstraintName("FK_Attempt_ToeicTest");

            entity.HasOne(d => d.User).WithMany(p => p.Attempts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attempt_User");
        });

        modelBuilder.Entity<AttemptAnswer>(entity =>
        {
            entity.HasKey(e => e.AttemptQuestionId);

            entity.ToTable("AttemptAnswer");

            entity.Property(e => e.AttemptQuestionId).ValueGeneratedNever();
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.SelectedOptionSnapshotKey)
                .HasMaxLength(64)
                .IsUnicode(false);

            entity.HasOne(d => d.AttemptQuestion).WithOne(p => p.AttemptAnswer)
                .HasForeignKey<AttemptAnswer>(d => d.AttemptQuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttemptAnswer_Question");
        });

        modelBuilder.Entity<AttemptQuestion>(entity =>
        {
            entity.ToTable("AttemptQuestion");

            entity.HasIndex(e => new { e.AttemptId, e.DisplayOrder }, "UQ_AttemptQuestion_Order").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Attempt).WithMany(p => p.AttemptQuestions)
                .HasForeignKey(d => d.AttemptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttemptQuestion_Attempt");
        });

        modelBuilder.Entity<AttemptSection>(entity =>
        {
            entity.ToTable("AttemptSection");

            entity.HasIndex(e => new { e.AttemptId, e.Kind }, "UQ_AttemptSection_Attempt_Kind").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Attempt).WithMany(p => p.AttemptSections)
                .HasForeignKey(d => d.AttemptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttemptSection_Attempt");
        });

        modelBuilder.Entity<AttemptSnapshot>(entity =>
        {
            entity.HasKey(e => e.AttemptId);

            entity.ToTable("AttemptSnapshot");

            entity.Property(e => e.AttemptId).ValueGeneratedNever();
            entity.Property(e => e.SchemaVersion).HasDefaultValue(1, "DF_AttemptSnapshot_SchemaVersion");

            entity.HasOne(d => d.Attempt).WithOne(p => p.AttemptSnapshot)
                .HasForeignKey<AttemptSnapshot>(d => d.AttemptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttemptSnapshot_Attempt");
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.ToTable("Book");

            entity.HasIndex(e => new { e.TopicId, e.IsDeleted, e.IsVisible, e.Id }, "IX_Book_Topic_Visibility");

            entity.Property(e => e.Author).HasMaxLength(200);
            entity.Property(e => e.CoverUrl).HasMaxLength(2048);
            entity.Property(e => e.IsVisible).HasDefaultValue(true, "DF_Book_IsVisible");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Title).HasMaxLength(300);

            entity.HasOne(d => d.Topic).WithMany(p => p.Books)
                .HasForeignKey(d => d.TopicId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Book_Topic");
        });

        modelBuilder.Entity<BookChapter>(entity =>
        {
            entity.ToTable("BookChapter");

            entity.HasIndex(e => new { e.Id, e.BookId }, "UQ_BookChapter_Id_Book").IsUnique();

            entity.HasIndex(e => new { e.Id, e.BookId }, "UQ_BookChapter_Id_BookId").IsUnique();

            entity.HasIndex(e => new { e.BookId, e.DisplayOrder }, "UQ_BookChapter_Order").IsUnique();

            entity.Property(e => e.IsVisible).HasDefaultValue(true, "DF_BookChapter_IsVisible");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Title).HasMaxLength(300);

            entity.HasOne(d => d.Book).WithMany(p => p.BookChapters)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookChapter_Book");
        });

        modelBuilder.Entity<BookReadingPosition>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.BookId });

            entity.ToTable("BookReadingPosition");

            entity.HasIndex(e => new { e.UserId, e.LastOpenedAt, e.BookId }, "IX_BookReadingPosition_Recent").IsDescending(false, true, false);

            entity.HasOne(d => d.Book).WithMany(p => p.BookReadingPositions)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookReadingPosition_Book");

            entity.HasOne(d => d.User).WithMany(p => p.BookReadingPositions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookReadingPosition_User");

            entity.HasOne(d => d.BookChapter).WithMany(p => p.BookReadingPositions)
                .HasPrincipalKey(p => new { p.Id, p.BookId })
                .HasForeignKey(d => new { d.LastChapterId, d.BookId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookReadingPosition_Chapter");
        });

        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.ToTable("Exercise");

            entity.HasIndex(e => new { e.LearningItemId, e.IsDeleted, e.IsVisible, e.Id }, "IX_Exercise_LearningItem");

            entity.HasIndex(e => new { e.VocabularyTopicId, e.LevelId, e.IsDeleted, e.IsVisible, e.Id }, "IX_Exercise_Vocabulary");

            entity.Property(e => e.IsVisible).HasDefaultValue(true, "DF_Exercise_IsVisible");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Title).HasMaxLength(300);

            entity.HasOne(d => d.LearningItem).WithMany(p => p.Exercises)
                .HasForeignKey(d => d.LearningItemId)
                .HasConstraintName("FK_Exercise_LearningItem");

            entity.HasOne(d => d.Level).WithMany(p => p.Exercises)
                .HasForeignKey(d => d.LevelId)
                .HasConstraintName("FK_Exercise_Level");

            entity.HasOne(d => d.VocabularyTopic).WithMany(p => p.Exercises)
                .HasForeignKey(d => d.VocabularyTopicId)
                .HasConstraintName("FK_Exercise_VocabularyTopic");
        });

        modelBuilder.Entity<GrammarGroup>(entity =>
        {
            entity.ToTable("GrammarGroup");

            entity.HasIndex(e => e.Name, "IX_GrammarGroup_Name");

            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<GrammarLesson>(entity =>
        {
            entity.HasKey(e => e.LearningItemId);

            entity.ToTable("GrammarLesson");

            entity.Property(e => e.LearningItemId).ValueGeneratedNever();

            entity.HasOne(d => d.LearningItem).WithOne(p => p.GrammarLesson)
                .HasForeignKey<GrammarLesson>(d => d.LearningItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GrammarLesson_LearningItem");
        });

        modelBuilder.Entity<GroupStimulus>(entity =>
        {
            entity.ToTable("GroupStimulus");

            entity.HasIndex(e => new { e.GroupId, e.DisplayOrder }, "UQ_GroupStimulus_Group_Order").IsUnique();

            entity.HasOne(d => d.Group).WithMany(p => p.GroupStimuli)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupStimulus_Group");
        });

        modelBuilder.Entity<LearningItem>(entity =>
        {
            entity.ToTable("LearningItem");

            entity.HasIndex(e => new { e.GrammarGroupId, e.LevelId, e.Id }, "IX_LearningItem_GrammarGroup_Level");

            entity.HasIndex(e => new { e.Kind, e.LevelId, e.IsDeleted, e.IsVisible, e.Id }, "IX_LearningItem_Kind_Level_Visibility");

            entity.HasIndex(e => new { e.TopicId, e.LevelId, e.Kind, e.Id }, "IX_LearningItem_Topic_Level_Kind");

            entity.Property(e => e.IsVisible).HasDefaultValue(true, "DF_LearningItem_IsVisible");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Title).HasMaxLength(300);

            entity.HasOne(d => d.GrammarGroup).WithMany(p => p.LearningItems)
                .HasForeignKey(d => d.GrammarGroupId)
                .HasConstraintName("FK_LearningItem_GrammarGroup");

            entity.HasOne(d => d.Level).WithMany(p => p.LearningItems)
                .HasForeignKey(d => d.LevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LearningItem_Level");

            entity.HasOne(d => d.Topic).WithMany(p => p.LearningItems)
                .HasForeignKey(d => d.TopicId)
                .HasConstraintName("FK_LearningItem_Topic");
        });

        modelBuilder.Entity<Level>(entity =>
        {
            entity.ToTable("Level");

            entity.HasIndex(e => e.Code, "UQ_Level_Code").IsUnique();

            entity.HasIndex(e => e.SortOrder, "UQ_Level_SortOrder").IsUnique();

            entity.Property(e => e.Code)
                .HasMaxLength(2)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ListeningLesson>(entity =>
        {
            entity.HasKey(e => e.LearningItemId);

            entity.ToTable("ListeningLesson");

            entity.HasIndex(e => e.SubtitleFileId, "IX_ListeningLesson_SubtitleFile");

            entity.Property(e => e.LearningItemId).ValueGeneratedNever();
            entity.Property(e => e.AudioUrl).HasMaxLength(2048);

            entity.HasOne(d => d.LearningItem).WithOne(p => p.ListeningLesson)
                .HasForeignKey<ListeningLesson>(d => d.LearningItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ListeningLesson_LearningItem");

            entity.HasOne(d => d.SubtitleFile).WithMany(p => p.ListeningLessons)
                .HasForeignKey(d => d.SubtitleFileId)
                .HasConstraintName("FK_ListeningLesson_SubtitleFile");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("Question");

            entity.HasIndex(e => new { e.QuestionGroupId, e.ToeicPartId }, "IX_Question_QuestionGroup");

            entity.HasIndex(e => new { e.ExerciseId, e.DisplayOrder }, "UX_Question_Exercise_DisplayOrder")
                .IsUnique()
                .HasFilter("([ExerciseId] IS NOT NULL)");

            entity.HasIndex(e => new { e.ToeicPartId, e.DisplayOrder }, "UX_Question_ToeicPart_DisplayOrder")
                .IsUnique()
                .HasFilter("([ToeicPartId] IS NOT NULL)");

            entity.HasOne(d => d.Exercise).WithMany(p => p.Questions)
                .HasForeignKey(d => d.ExerciseId)
                .HasConstraintName("FK_Question_Exercise");

            entity.HasOne(d => d.ToeicPart).WithMany(p => p.Questions)
                .HasForeignKey(d => d.ToeicPartId)
                .HasConstraintName("FK_Question_ToeicPart");

            entity.HasOne(d => d.QuestionGroup).WithMany(p => p.Questions)
                .HasPrincipalKey(p => new { p.Id, p.PartId })
                .HasForeignKey(d => new { d.QuestionGroupId, d.ToeicPartId })
                .HasConstraintName("FK_Question_QuestionGroup");
        });

        modelBuilder.Entity<QuestionGroup>(entity =>
        {
            entity.ToTable("QuestionGroup");

            entity.HasIndex(e => new { e.Id, e.PartId }, "UQ_QuestionGroup_Id_Part").IsUnique();

            entity.HasIndex(e => new { e.PartId, e.DisplayOrder }, "UQ_QuestionGroup_Part_Order").IsUnique();

            entity.HasOne(d => d.Part).WithMany(p => p.QuestionGroups)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuestionGroup_Part");
        });

        modelBuilder.Entity<ReadingLesson>(entity =>
        {
            entity.HasKey(e => e.LearningItemId);

            entity.ToTable("ReadingLesson");

            entity.Property(e => e.LearningItemId).ValueGeneratedNever();

            entity.HasOne(d => d.LearningItem).WithOne(p => p.ReadingLesson)
                .HasForeignKey<ReadingLesson>(d => d.LearningItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReadingLesson_LearningItem");
        });

        modelBuilder.Entity<StoredFile>(entity =>
        {
            entity.ToTable("StoredFile");

            entity.HasIndex(e => new { e.UploadedByUserId, e.Kind, e.CreatedAt, e.Id }, "IX_StoredFile_User_Kind");

            entity.HasIndex(e => e.StorageKey, "UQ_StoredFile_StorageKey").IsUnique();

            entity.Property(e => e.ContentType)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.OriginalName).HasMaxLength(255);
            entity.Property(e => e.StorageKey)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.UploadedByUser).WithMany(p => p.StoredFiles)
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StoredFile_User");
        });

        modelBuilder.Entity<ToeicFormatProfile>(entity =>
        {
            entity.ToTable("ToeicFormatProfile");

            entity.HasIndex(e => new { e.Code, e.Version }, "UQ_ToeicFormatProfile_Code_Version").IsUnique();

            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RulesSchemaVersion).HasDefaultValue(1, "DF_ToeicFormatProfile_RulesSchemaVersion");
        });

        modelBuilder.Entity<ToeicPart>(entity =>
        {
            entity.ToTable("ToeicPart");

            entity.HasIndex(e => new { e.SectionId, e.Number }, "UQ_ToeicPart_Section_Number").IsUnique();

            entity.HasIndex(e => new { e.SectionId, e.DisplayOrder }, "UQ_ToeicPart_Section_Order").IsUnique();

            entity.HasOne(d => d.Section).WithMany(p => p.ToeicParts)
                .HasForeignKey(d => d.SectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ToeicPart_Section");
        });

        modelBuilder.Entity<ToeicSection>(entity =>
        {
            entity.ToTable("ToeicSection");

            entity.HasIndex(e => new { e.TestId, e.Kind }, "UQ_ToeicSection_Test_Kind").IsUnique();

            entity.Property(e => e.FullAudioUrl).HasMaxLength(2048);

            entity.HasOne(d => d.Test).WithMany(p => p.ToeicSections)
                .HasForeignKey(d => d.TestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ToeicSection_Test");
        });

        modelBuilder.Entity<ToeicTest>(entity =>
        {
            entity.ToTable("ToeicTest");

            entity.HasIndex(e => e.FormatProfileId, "IX_ToeicTest_FormatProfile");

            entity.HasIndex(e => new { e.PublicationStatus, e.IsDeleted, e.IsVisible, e.Id }, "IX_ToeicTest_Publication");

            entity.Property(e => e.IsVisible).HasDefaultValue(true, "DF_ToeicTest_IsVisible");
            entity.Property(e => e.PublicationStatus).HasDefaultValue((byte)1, "DF_ToeicTest_PublicationStatus");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Title).HasMaxLength(300);

            entity.HasOne(d => d.FormatProfile).WithMany(p => p.ToeicTests)
                .HasForeignKey(d => d.FormatProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ToeicTest_FormatProfile");
        });

        modelBuilder.Entity<Topic>(entity =>
        {
            entity.ToTable("Topic");

            entity.HasIndex(e => e.Name, "IX_Topic_Name");

            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<TranscriptCue>(entity =>
        {
            entity.ToTable("TranscriptCue");

            entity.HasIndex(e => new { e.ListeningLessonId, e.Sequence }, "UQ_TranscriptCue_Sequence").IsUnique();

            entity.HasOne(d => d.ListeningLesson).WithMany(p => p.TranscriptCues)
                .HasForeignKey(d => d.ListeningLessonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TranscriptCue_ListeningLesson");
        });

        modelBuilder.Entity<UserChapterProgress>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.ChapterId });

            entity.ToTable("UserChapterProgress");

            entity.HasIndex(e => new { e.UserId, e.IsRead, e.UpdatedAt, e.ChapterId }, "IX_UserChapterProgress_User").IsDescending(false, false, true, false);

            entity.HasOne(d => d.Chapter).WithMany(p => p.UserChapterProgresses)
                .HasForeignKey(d => d.ChapterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserChapterProgress_Chapter");

            entity.HasOne(d => d.User).WithMany(p => p.UserChapterProgresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserChapterProgress_User");
        });

        modelBuilder.Entity<UserLearningProgress>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LearningItemId });

            entity.ToTable("UserLearningProgress");

            entity.HasIndex(e => new { e.UserId, e.IsLearned, e.UpdatedAt, e.LearningItemId }, "IX_UserLearningProgress_User").IsDescending(false, false, true, false);

            entity.HasOne(d => d.LearningItem).WithMany(p => p.UserLearningProgresses)
                .HasForeignKey(d => d.LearningItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserLearningProgress_LearningItem");

            entity.HasOne(d => d.User).WithMany(p => p.UserLearningProgresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserLearningProgress_User");
        });

        modelBuilder.Entity<UserWriting>(entity =>
        {
            entity.ToTable("UserWriting");

            entity.HasIndex(e => new { e.UserId, e.WritingTopicId, e.IsDeleted, e.UpdatedAt, e.Id }, "IX_UserWriting_User_Topic").IsDescending(false, false, false, true, false);

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.User).WithMany(p => p.UserWritings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserWriting_User");

            entity.HasOne(d => d.WritingTopic).WithMany(p => p.UserWritings)
                .HasForeignKey(d => d.WritingTopicId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserWriting_WritingTopic");
        });

        modelBuilder.Entity<Vocabulary>(entity =>
        {
            entity.HasKey(e => e.LearningItemId);

            entity.ToTable("Vocabulary");

            entity.HasIndex(e => new { e.Word, e.LearningItemId }, "IX_Vocabulary_Word");

            entity.Property(e => e.LearningItemId).ValueGeneratedNever();
            entity.Property(e => e.AudioUrl).HasMaxLength(2048);
            entity.Property(e => e.ImageUrl).HasMaxLength(2048);
            entity.Property(e => e.PartOfSpeech).HasMaxLength(50);
            entity.Property(e => e.Pronunciation).HasMaxLength(300);
            entity.Property(e => e.Word).HasMaxLength(200);

            entity.HasOne(d => d.LearningItem).WithOne(p => p.Vocabulary)
                .HasForeignKey<Vocabulary>(d => d.LearningItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vocabulary_LearningItem");
        });

        modelBuilder.Entity<VocabularyMeaning>(entity =>
        {
            entity.ToTable("VocabularyMeaning");

            entity.HasIndex(e => new { e.VocabularyId, e.DisplayOrder }, "UQ_VocabularyMeaning_Order").IsUnique();

            entity.Property(e => e.MeaningVi).HasMaxLength(1000);

            entity.HasOne(d => d.Vocabulary).WithMany(p => p.VocabularyMeanings)
                .HasForeignKey(d => d.VocabularyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VocabularyMeaning_Vocabulary");
        });

        modelBuilder.Entity<WritingEvaluation>(entity =>
        {
            entity.ToTable("WritingEvaluation");

            entity.HasIndex(e => new { e.UserWritingId, e.Status, e.StartedAt, e.Id }, "IX_WritingEvaluation_Writing").IsDescending(false, false, true, false);

            entity.HasIndex(e => new { e.VersionId, e.RunNumber }, "UQ_WritingEvaluation_Run").IsUnique();

            entity.HasIndex(e => e.VersionId, "UX_WritingEvaluation_Success")
                .IsUnique()
                .HasFilter("([Status]=(3))");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ErrorCode)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.GrammarScore).HasColumnType("decimal(4, 2)");
            entity.Property(e => e.Model).HasMaxLength(200);
            entity.Property(e => e.OrganizationCohesionScore).HasColumnType("decimal(4, 2)");
            entity.Property(e => e.OverallScore).HasColumnType("decimal(3, 1)");
            entity.Property(e => e.PromptVersion)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Provider).HasMaxLength(100);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.TaskRelevanceScore).HasColumnType("decimal(4, 2)");
            entity.Property(e => e.VocabularyScore).HasColumnType("decimal(4, 2)");

            entity.HasOne(d => d.UserWriting).WithMany(p => p.WritingEvaluations)
                .HasForeignKey(d => d.UserWritingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WritingEvaluation_UserWriting");

            entity.HasOne(d => d.WritingVersion).WithMany(p => p.WritingEvaluations)
                .HasPrincipalKey(p => new { p.Id, p.UserWritingId })
                .HasForeignKey(d => new { d.VersionId, d.UserWritingId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WritingEvaluation_Version");
        });

        modelBuilder.Entity<WritingTopic>(entity =>
        {
            entity.HasKey(e => e.LearningItemId);

            entity.ToTable("WritingTopic");

            entity.Property(e => e.LearningItemId).ValueGeneratedNever();

            entity.HasOne(d => d.LearningItem).WithOne(p => p.WritingTopic)
                .HasForeignKey<WritingTopic>(d => d.LearningItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WritingTopic_LearningItem");
        });

        modelBuilder.Entity<WritingVersion>(entity =>
        {
            entity.ToTable("WritingVersion");

            entity.HasIndex(e => new { e.Id, e.UserWritingId }, "UQ_WritingVersion_Id_Owner").IsUnique();

            entity.HasIndex(e => new { e.UserWritingId, e.VersionNumber }, "UQ_WritingVersion_Number").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.TextHash)
                .HasMaxLength(32)
                .IsFixedLength();

            entity.HasOne(d => d.UserWriting).WithMany(p => p.WritingVersions)
                .HasForeignKey(d => d.UserWritingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WritingVersion_UserWriting");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
