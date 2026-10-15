// Focused step 01 checks, plus an explicit EF connection/schema-only mode. Never writes a database.
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Xml.Linq;
using English;
using English.Models;
using English.Authorization;
using English.Controllers;
using English.Data;
using English.Interfaces;
using English.Models.Entities;
using English.Models.ViewModels.Listening;
using English.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Data.Common;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Http;

var root = args[0];
if (args.Contains("--read-only-schema"))
{
    Environment.ExitCode = await ListeningSchemaCheck.RunAsync(root);
    return;
}
if (args.Contains("--subtitle-import"))
{
    await ListeningSubtitleChecks.RunAsync(root);
    return;
}
var checks = 0;
void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException(label); checks++; Console.WriteLine("PASS " + label); }
List<ValidationResult> Validate(object input) { var errors = new List<ValidationResult>(); Validator.TryValidateObject(input, new ValidationContext(input), errors, true); return errors; }
var missing = Validate(new ListeningInputModel());
foreach (var field in new[] { "Title", "LevelId", "TopicId", "AudioUrl" }) Check(missing.Any(e => e.MemberNames.Contains(field)), "required " + field);
var input = new ListeningInputModel { Title = "  Lesson  ", AudioUrl = "  https://example.invalid/audio.mp3  ", LevelId = 1, TopicId = 1 };
Check(input.Title == "Lesson" && input.AudioUrl == "https://example.invalid/audio.mp3" && Validate(input).Count == 0, "trim and valid form");
input.Title = new string('a',301); input.AudioUrl = "https://example.invalid/" + new string('a',2048);
var longErrors = Validate(input); Check(longErrors.Any(e => e.MemberNames.Contains("Title")) && longErrors.Any(e => e.MemberNames.Contains("AudioUrl")), "Title/AudioUrl limits");
foreach (var field in new[] { "Kind", "IsDeleted", "DeletedAt", "SubtitleFileId", "TranscriptRevision", "TranscriptCues", "GrammarGroupId" }) Check(typeof(ListeningInputModel).GetProperty(field) == null, "no over-posting " + field);
Check(Validate(new ListeningVisibilityInputModel()).Count > 0 && Validate(new ListeningVisibilityInputModel { IsVisible = false }).Count == 0, "visibility requires explicit value, allows false");
var block = typeof(ListeningService).GetMethod("IsAudioChangeBlocked", BindingFlags.NonPublic | BindingFlags.Static)!;
bool Block(ListeningLesson l, string url, bool cues) => (bool)block.Invoke(null, new object[] { l, url, cues })!;
const string original = "https://example.invalid/original.mp3", changed = "https://example.invalid/changed.mp3";
foreach (var file in new int?[] { null, 1 }) foreach (var cues in new[] { false, true }) foreach (var revision in new[] { 0, 1, 5 })
{
    var l = new ListeningLesson { AudioUrl = original, SubtitleFileId = file, TranscriptRevision = revision };
    Check(Block(l, changed, cues) == (file != null || cues || revision > 0), $"audio change rule file={file} cues={cues} revision={revision}");
    Check(!Block(l, original, cues), "unchanged AudioUrl allows metadata edit");
    Check(l.AudioUrl == original && l.SubtitleFileId == file && l.TranscriptRevision == revision, "rule does not mutate lesson");
}
var version = typeof(ListeningService).GetMethod("ReadVersion", BindingFlags.NonPublic | BindingFlags.Static)!;
foreach (var value in new string?[] { null, "", "invalid", Convert.ToBase64String(new byte[7]), Convert.ToBase64String(new byte[9]) }) Check(version.Invoke(null, new object?[] { value }) == null, "reject malformed RowVersion");
var validVersion = Convert.ToBase64String(new byte[8]); Check(version.Invoke(null, new object?[] { validVersion }) is byte[] { Length: 8 }, "accept 8-byte RowVersion");
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer("Server=invalid;Database=NeverConnect;Integrated Security=true;TrustServerCertificate=true").Options;
await using var context = new ApplicationDbContext(options);
var service = new ListeningService(context, new MediaUrlValidator(), new SubtitleParser());
foreach (var url in new[] { "javascript:alert(1)", "file:///audio.mp3", "/audio.mp3", "ftp://example.invalid/audio.mp3" })
{
    var result = await service.SaveAsync(null, new ListeningInputModel { Title = "Lesson", LevelId = 1, TopicId = 1, AudioUrl = url }, default);
    Check(result.Status == ListeningResult.Invalid && result.Errors!.Any(e => e.Field == "AudioUrl"), "Service rejects unsafe URL before database access");
    Check(!context.ChangeTracker.HasChanges(), "invalid URL leaves no tracked changes");
}
var badVersion = await service.SaveAsync(1, new ListeningInputModel { Title = "Lesson", LevelId = 1, TopicId = 1, AudioUrl = original }, default);
Check(badVersion.Status == ListeningResult.Invalid, "edit requires RowVersion before database access");
Check(await service.DeleteAsync(1, "bad", default) == ListeningResult.Conflict, "delete rejects invalid RowVersion before database");
Check(await service.SetVisibilityAsync(1, new ListeningVisibilityInputModel(), default) == ListeningResult.Invalid, "visibility rejects absent value before database");
Check(typeof(AdminListeningController).GetCustomAttribute<AuthorizeAttribute>()?.Policy == AuthorizationPolicies.AdminOnly, "AdminOnly controller policy");
var posts = typeof(AdminListeningController).GetMethods().Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null).ToArray(); Check(posts.Length == 5, "only create/edit/visibility/delete/subtitle mutations");
foreach (var action in posts) Check(action.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() != null, action.Name + " antiforgery attribute");
var probe = new ListeningProbe { SaveResult = new(ListeningResult.Invalid, [new("AudioUrl", "ListeningAudioLocked")]) };
var admin = new AdminListeningController(probe, new ProbeLocalizer());
var invalidPage = (ViewResult)await admin.Edit(1, new ListeningInputModel { Title = "Changed", AudioUrl = changed }, default);
Check(invalidPage.ViewName == "Form" && admin.ModelState["Input.AudioUrl"]!.Errors.Single().ErrorMessage == "ListeningAudioLocked", "controller presents blocked AudioUrl field error");
Check(((ListeningFormViewModel)invalidPage.Model!).Input.Title == "Changed", "blocked request retains submitted metadata for correction");
probe.Item = new ListeningDetailsViewModel { Id = 1, IsDeleted = true };
Check(await admin.Edit(1, default) is NotFoundResult && await admin.Delete(1, default) is NotFoundResult, "deleted lesson not editable/deletable");
foreach (var file in new[] { "SharedResource.resx", "SharedResource.en.resx" })
{
    var entries = XDocument.Load(Path.Combine(root,"Resources",file)).Root!.Elements("data").Where(e => e.Attribute("name")!.Value.StartsWith("Listening")).ToArray();
    Check(entries.Length >= 19 && entries.Select(e => e.Attribute("name")!.Value).Distinct().Count() == entries.Length && entries.All(e => !string.IsNullOrWhiteSpace(e.Element("value")!.Value)), "Listening localized resources " + file);
}
Console.WriteLine($"Listening step 01: {checks} focused checks passed. SQL CRUD, live HTTP authorization/CSRF and race behavior NOT tested.");
sealed class ProbeLocalizer : IStringLocalizer<SharedResource>
{
    public LocalizedString this[string name] => new(name, name);
    public LocalizedString this[string name, params object[] arguments] => new(name, name);
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
}
sealed class ListeningProbe : IListeningService
{
    public ListeningSaveResult SaveResult { get; set; } = new(ListeningResult.Success);
    public ListeningDetailsViewModel? Item { get; set; } = new() { Id = 1, AudioLocked = true };
    public Task<ListeningChoices> GetChoicesAsync(CancellationToken c) => Task.FromResult(new ListeningChoices([], []));
    public Task<ListeningDetailsViewModel?> GetAsync(int id, CancellationToken c) => Task.FromResult(Item);
    public Task<ListeningSaveResult> SaveAsync(int? id, ListeningInputModel input, CancellationToken c) => Task.FromResult(SaveResult);
    public Task<ListeningListViewModel> ListAsync(ListeningQuery q, CancellationToken c) => throw new NotSupportedException();
    public Task<ListeningResult> DeleteAsync(int id, string? v, CancellationToken c) => throw new NotSupportedException();
    public Task<ListeningResult> SetVisibilityAsync(int id, ListeningVisibilityInputModel i, CancellationToken c) => throw new NotSupportedException();
    public Task<ListeningSubtitleImportResult> ImportSubtitleAsync(int id, ListeningSubtitleUpload u, CancellationToken c) => throw new NotSupportedException();
}

static class ListeningSubtitleChecks
{
    public static async Task RunAsync(string root)
    {
        var count = 0;
        void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException(label); count++; Console.WriteLine("PASS " + label); }
        var parser = new SubtitleParser();
        async Task<SubtitleParseResult> Parse(string text, string name = "test.srt", bool bom = false)
        {
            var bytes = Encoding.UTF8.GetBytes((bom ? "\uFEFF" : "") + text);
            using var stream = new MemoryStream(bytes);
            return await parser.ParseAsync(stream, name, bytes.Length);
        }
        const string srt = "1\r\n00:00:00,000 --> 00:00:01,500\r\n Hello, Anna! \r\nSecond line.\r\n\r\n2\r\n00:00:01,500 --> 00:00:02,001\r\nLuyện nghe.\r\n";
        foreach (var bom in new[] { false, true })
        {
            var result = await Parse(srt, "lesson.SRT", bom);
            Check(result.IsValid && result.Cues.SequenceEqual(new[] { new SubtitleCue(1,0,1500," Hello, Anna! \nSecond line."), new SubtitleCue(2,1500,2001,"Luyện nghe.") }), "SRT BOM=" + bom + ": exact words/spaces, multiline, adjacent cues");
            var vtt = await Parse("WEBVTT\n\nanna-1\n00:00.000 --> 00:01.500\nHello\nAnna\n\n00:00:01.500 --> 00:00:02.001\nBye\n", "lesson.vtt", bom);
            Check(vtt.IsValid && vtt.Cues.SequenceEqual(new[] { new SubtitleCue(1,0,1500,"Hello\nAnna"), new SubtitleCue(2,1500,2001,"Bye") }), "basic VTT BOM=" + bom + ": optional identifier, file-order sequence, two timestamp forms");
        }
        var invalidCases = new (string Label, string Name, string Text, string Resource)[]
        {
            ("extension", "lesson.txt", srt, "ListeningSubtitleExtension"),
            ("path traversal", "../lesson.srt", srt, "ListeningSubtitleExtension"),
            ("Windows path", "C:\\lesson.srt", srt, "ListeningSubtitleExtension"),
            ("empty file", "lesson.srt", "", "ListeningSubtitleEmpty"),
            ("sequence starts at 2", "lesson.srt", srt.Replace("1\r\n00:","2\r\n00:"), "ListeningSubtitleSequence"),
            ("sequence gap", "lesson.srt", srt.Replace("2\r\n00:","3\r\n00:"), "ListeningSubtitleSequence"),
            ("sequence repeated", "lesson.srt", srt.Replace("2\r\n00:","1\r\n00:"), "ListeningSubtitleSequence"),
            ("minute range", "lesson.srt", srt.Replace("00:00:00,000","00:60:00,000"), "ListeningSubtitleTimestamp"),
            ("negative start", "lesson.srt", srt.Replace("00:00:00,000","-00:00:00,000"), "ListeningSubtitleTimestamp"),
            ("zero duration", "lesson.srt", srt.Replace("00:00:01,500","00:00:00,000"), "ListeningSubtitleTimestamp"),
            ("end before start", "lesson.srt", "1\n00:00:02,000 --> 00:00:01,000\nHello", "ListeningSubtitleTimestamp"),
            ("millisecond precision", "lesson.srt", srt.Replace("00:00:00,000","00:00:00,00"), "ListeningSubtitleTimestamp"),
            ("int timestamp overflow", "lesson.srt", "1\n00:00:00,000 --> 596:31:23,648\nHello", "ListeningSubtitleTimestamp"),
            ("huge timestamp", "lesson.srt", "1\n00:00:00,000 --> 999999999999999999999:00:00,000\nHello", "ListeningSubtitleTimestamp"),
            ("overlap", "lesson.srt", srt.Replace("2\r\n00:00:01,500", "2\r\n00:00:01,499"), "ListeningSubtitleOverlap"),
            ("duplicate timing", "lesson.srt", "1\n00:00:00,000 --> 00:00:01,000\nA\n\n2\n00:00:00,000 --> 00:00:01,000\nB", "ListeningSubtitleOverlap"),
            ("empty cue", "lesson.srt", "1\n00:00:00,000 --> 00:00:01,000\n\n", "ListeningSubtitleCueEmpty"),
            ("whitespace cue", "lesson.srt", "1\n00:00:00,000 --> 00:00:01,000\n  \t", "ListeningSubtitleCueEmpty"),
            ("missing separator", "lesson.srt", srt.Replace("\r\n\r\n2", "\r\n2"), "ListeningSubtitleStructure"),
            ("missing VTT header", "lesson.vtt", srt, "ListeningSubtitleVttHeader"),
            ("VTT header metadata", "lesson.vtt", "WEBVTT extra\n\n00:00.000 --> 00:01.000\nHello", "ListeningSubtitleVttHeader"),
            ("VTT header separator", "lesson.vtt", "WEBVTT\n00:00.000 --> 00:01.000\nHello", "ListeningSubtitleStructure"),
            ("VTT empty", "lesson.vtt", "WEBVTT\n\n", "ListeningSubtitleEmpty"),
            ("VTT settings", "lesson.vtt", "WEBVTT\n\n00:00.000 --> 00:01.000 align:start\nHello", "ListeningSubtitleVttUnsupported"),
            ("VTT voice annotation", "lesson.vtt", "WEBVTT\n\n00:00.000 --> 00:01.000\n<v Anna>Hello", "ListeningSubtitleVttUnsupported"),
            ("VTT timestamp annotation", "lesson.vtt", "WEBVTT\n\n00:00.000 --> 00:01.000\n<00:00.500>Hello", "ListeningSubtitleVttUnsupported"),
            ("embedded BOM", "lesson.srt", srt.Replace("Second", "\uFEFFSecond"), "ListeningSubtitleEncoding"),
            ("NUL character", "lesson.srt", srt.Replace("Second", "\0Second"), "ListeningSubtitleEncoding")
        };
        foreach (var test in invalidCases)
            Check((await Parse(test.Text, test.Name)).Error?.Resource == test.Resource, "reject " + test.Label);
        foreach (var block in new[] { "NOTE", "NOTE comment", "STYLE", "REGION" })
            Check((await Parse("WEBVTT\n\n"+block+"\ncontent\n\n00:00.000 --> 00:01.000\nHello", "lesson.vtt")).Error?.Resource == "ListeningSubtitleVttUnsupported", "reject VTT " + block);
        var lineError = await Parse("1\nwrong timing\nHello");
        Check(lineError.Error?.Line == 2, "timestamp error reports source line");
        using (var invalidUtf8 = new MemoryStream([0xC3, 0x28]))
            Check((await parser.ParseAsync(invalidUtf8,"lesson.srt",2)).Error?.Resource == "ListeningSubtitleEncoding", "reject invalid UTF-8");
        using (var utf16 = new MemoryStream(Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(srt)).ToArray()))
            Check((await parser.ParseAsync(utf16,"lesson.srt",utf16.Length)).Error?.Resource == "ListeningSubtitleEncoding", "reject UTF-16");
        const string prefix = "1\n00:00:00,000 --> 00:00:01,000\n";
        var exact = prefix + new string('a', ListeningSubtitleLimits.MaxSizeBytes - Encoding.UTF8.GetByteCount(prefix));
        Check((await Parse(exact)).IsValid, "accept exactly 1 MB");
        Check((await Parse(exact+"a")).Error?.Resource == "ListeningSubtitleSize", "reject size over 1 MB");
        using (var misleadingLength = new MemoryStream(Encoding.UTF8.GetBytes(exact+"a")))
            Check((await parser.ParseAsync(misleadingLength,"lesson.srt",1)).Error?.Resource == "ListeningSubtitleSize", "actual stream size enforced despite declared length");
        using (var broken = new FailedSubtitleStream())
            Check((await parser.ParseAsync(broken,"lesson.srt",1)).Error?.Resource == "ListeningSubtitleReadFailed", "read failure returns validation without storage/database access");
        var html = await Parse("1\n00:00:00,000 --> 00:00:01,000\n<script>alert(1)</script>");
        Check(html.IsValid && html.Cues[0].Text == "<script>alert(1)</script>", "SRT text is preserved, never interpreted as HTML");
        var maxTimestamp = await Parse("1\n00:00:00,000 --> 596:31:23,647\nHello");
        Check(maxTimestamp.IsValid && maxTimestamp.Cues[0].EndMs == int.MaxValue, "accept maximum database millisecond value");
        var equalMethod = typeof(ListeningService).GetMethod("CuesEqual", BindingFlags.NonPublic|BindingFlags.Static)!;
        var old = new[] { new TranscriptCue { Id=1, ListeningLessonId=42, Sequence=1, StartMs=0, EndMs=1000, Text="Hello\nAnna" } };
        bool Equal(params SubtitleCue[] cues) => (bool)equalMethod.Invoke(null,new object[]{old,cues})!;
        Check(Equal(new SubtitleCue(1,0,1000,"Hello\nAnna")), "identical cue comparison includes multiline text");
        Check(!Equal(new SubtitleCue(1,0,1001,"Hello\nAnna")) && !Equal(new SubtitleCue(1,0,1000,"Hello Anna")) && !Equal(new SubtitleCue(2,0,1000,"Hello\nAnna")) && !Equal(), "timestamp/content/order/count changes are replacements");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer("Server=invalid;Database=NeverConnect;Integrated Security=true;TrustServerCertificate=true").Options;
        await using var context = new ApplicationDbContext(options);
        var service = new ListeningService(context,new MediaUrlValidator(),parser);
        var version = Convert.ToBase64String(new byte[8]);
        using (var invalid = new MemoryStream(Encoding.UTF8.GetBytes("bad subtitle")))
        {
            var result = await service.ImportSubtitleAsync(42,new(invalid,"lesson.srt",invalid.Length,version,0),default);
            Check(result.Status == ListeningResult.Invalid && !context.ChangeTracker.HasChanges(), "invalid file rejected by Service before any database access/mutation");
        }
        using (var missingVersion = new MemoryStream())
            Check((await service.ImportSubtitleAsync(42,new(missingVersion,"lesson.srt",0,null,0),default)).Status == ListeningResult.Conflict, "import requires version before database access");
        var action = typeof(AdminListeningController).GetMethods().Single(m=>m.Name=="ImportSubtitle" && m.GetCustomAttribute<HttpPostAttribute>()!=null);
        Check(typeof(AdminListeningController).GetCustomAttribute<AuthorizeAttribute>()?.Policy==AuthorizationPolicies.AdminOnly && action.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()!=null, "import has AdminOnly and antiforgery attributes (not live authorization test)");
        Check((long)action.CustomAttributes.Single(a=>a.AttributeType==typeof(RequestSizeLimitAttribute)).ConstructorArguments[0].Value! == ListeningSubtitleLimits.MaxRequestBytes, "multipart request size bounded");
        var limits = action.GetCustomAttribute<RequestFormLimitsAttribute>()!;
        Check(limits.MemoryBufferThreshold >= limits.MultipartBodyLengthLimit && limits.MultipartBodyLengthLimit == ListeningSubtitleLimits.MaxRequestBytes, "form buffering threshold prevents disk spill within request limit");
        var admin = new AdminListeningController(new ListeningProbe { Item = new() { Id=42,IsDeleted=true } }, new ProbeLocalizer());
        Check(await admin.ImportSubtitle(42,default) is NotFoundResult && await admin.ImportSubtitle(42,new ListeningSubtitleInputModel(),default) is NotFoundResult, "deleted lesson rejected before reading file/calling import Service");
        foreach (var field in new[] { "AudioUrl", "Title", "IsVisible", "SubtitleFileId", "StoredFileId" })
            Check(typeof(ListeningSubtitleInputModel).GetProperty(field)==null, "subtitle form excludes " + field);
        var values = new List<HashSet<string>>();
        foreach (var name in new[] { "SharedResource.resx", "SharedResource.en.resx" })
        {
            var entries = XDocument.Load(Path.Combine(root,"Resources",name)).Root!.Elements("data").Where(e=>e.Attribute("name")!.Value.StartsWith("ListeningSubtitle")).ToArray();
            Check(entries.Length==22 && entries.All(e=>!string.IsNullOrWhiteSpace(e.Element("value")!.Value)), "22 subtitle resource strings " + name);
            values.Add(entries.Select(e=>e.Attribute("name")!.Value).ToHashSet());
        }
        Check(values[0].SetEquals(values[1]), "VI/EN subtitle resource keys match");
        Console.WriteLine($"Listening step 02: {count} focused checks passed. SQL schema/transaction/rollback/no-op persistence, live multipart buffering, HTTP authorization/antiforgery and import NOT TESTED. No database connection or writes.");
    }
}
sealed class FailedSubtitleStream : MemoryStream
{
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken=default) => throw new IOException("Synthetic upload read failure");
}

static class ListeningSchemaCheck
{
    public static async Task<int> RunAsync(string root)
    {
        var stage = "Read existing application configuration";
        try
        {
            // Uses the same configuration providers and AddDbContext/UseSqlServer registration as Program.cs.
            // Development is the environment in the existing launch profiles; process overrides retain priority.
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = root, EnvironmentName = environment, Args = []
            });
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is not configured.");
            var settings = new DbConnectionStringBuilder { ConnectionString = connectionString };
            string Setting(params string[] keys) => keys.FirstOrDefault(settings.ContainsKey) is string key
                ? Convert.ToString(settings[key]) ?? "" : "";
            var database = Setting("Initial Catalog", "Database");
            var server = Setting("Data Source", "Server", "Address", "Addr", "Network Address");
            Console.WriteLine(JsonSerializer.Serialize(new { Environment = environment, Server = server, Database = database }));
            if (database != "English") throw new InvalidOperationException("Read-only mode requires the explicitly authorized English database.");
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
            using var services = builder.Services.BuildServiceProvider();
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            stage = "ApplicationDbContext.Database.OpenConnectionAsync";
            // OpenConnectionAsync preserves exceptions; CanConnectAsync would hide diagnostic detail.
            await context.Database.OpenConnectionAsync();
            stage = "EF SELECT server/database identity";
            var identity = await context.Database.SqlQueryRaw<SchemaIdentity>(
                "SELECT CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')) AS ServerName, DB_NAME() AS DatabaseName").SingleAsync();
            Console.WriteLine(JsonSerializer.Serialize(identity));
            if (identity.DatabaseName != "English" || !string.Equals(identity.ServerName, server, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Connected SQL identity does not match the authorized configuration.");
            stage = "EF SELECT Listening column metadata";
            var columns = await context.Database.SqlQueryRaw<SchemaColumn>("""
                SELECT s.name AS SchemaName,t.name AS TableName,c.name AS ColumnName,
                       ty.name AS TypeName,CONVERT(int,c.max_length) AS MaxLength,c.is_nullable AS IsNullable,
                       c.is_identity AS IsIdentity,c.is_computed AS IsComputed,CONVERT(int,c.scale) AS Scale
                FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
                JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id
                WHERE s.name='dbo' AND t.name IN ('LearningItem','ListeningLesson','TranscriptCue','StoredFile','Level','Topic','AspNetUsers')
                """).ToListAsync();
            foreach (var column in columns.OrderBy(x => x.TableName).ThenBy(x => x.ColumnName)) Console.WriteLine(JsonSerializer.Serialize(column));
            stage = "Compare actual columns with ApplicationDbContext EF mapping";
            foreach (var type in new[] { typeof(LearningItem), typeof(ListeningLesson), typeof(TranscriptCue), typeof(StoredFile), typeof(Level), typeof(Topic), typeof(AspNetUser) })
            {
                var entity = context.Model.FindEntityType(type)!;
                var table = entity.GetTableName()!;
                var schema = entity.GetSchema() ?? "dbo";
                var store = StoreObjectIdentifier.Table(table, schema);
                foreach (var property in entity.GetProperties())
                {
                    var name = property.GetColumnName(store)!;
                    var actual = columns.SingleOrDefault(x => x.SchemaName == schema && x.TableName == table && x.ColumnName == name)
                        ?? throw new InvalidOperationException($"Missing column {schema}.{table}.{name}.");
                    var expectedType = property.GetColumnType(store)!;
                    var actualType = actual.TypeName.ToLowerInvariant();
                    if (actualType is "nvarchar" or "nchar" or "varchar" or "char" or "varbinary" or "binary")
                        actualType += actual.MaxLength == -1 ? "(max)" : $"({(actual.TypeName.StartsWith('n') ? actual.MaxLength / 2 : actual.MaxLength)})";
                    if (actualType == "datetimeoffset" && actual.Scale != 7) actualType += $"({actual.Scale})";
                    var rowVersion = expectedType == "rowversion" && actualType == "timestamp";
                    if ((!rowVersion && !string.Equals(expectedType, actualType, StringComparison.OrdinalIgnoreCase)) || property.IsNullable != actual.IsNullable)
                        throw new InvalidOperationException($"Column mapping mismatch {schema}.{table}.{name}: EF {expectedType} nullable={property.IsNullable}, SQL {actualType} nullable={actual.IsNullable}.");
                }
            }
            stage = "EF SELECT primary keys, foreign keys, indexes, checks and defaults";
            var metadata = await context.Database.SqlQueryRaw<string>("""
                SELECT (SELECT s.name AS [schema],t.name AS [table],i.name,i.is_primary_key,i.is_unique,i.is_disabled,
                    (SELECT c.name,ic.key_ordinal,ic.is_included_column FROM sys.index_columns ic JOIN sys.columns c
                     ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id ORDER BY ic.key_ordinal FOR JSON PATH) AS columns
                    FROM sys.indexes i JOIN sys.tables t ON t.object_id=i.object_id JOIN sys.schemas s ON s.schema_id=t.schema_id
                    WHERE s.name='dbo' AND t.name IN ('LearningItem','ListeningLesson','TranscriptCue','StoredFile','Level','Topic','AspNetUsers') AND i.index_id>0 FOR JSON PATH) AS Value
                UNION ALL
                SELECT (SELECT fk.name,OBJECT_SCHEMA_NAME(fk.parent_object_id) AS [schema],OBJECT_NAME(fk.parent_object_id) AS [table],
                    OBJECT_NAME(fk.referenced_object_id) AS referenced_table,fk.is_disabled,fk.is_not_trusted,fk.delete_referential_action_desc,
                    (SELECT COL_NAME(fkc.parent_object_id,fkc.parent_column_id) AS parent_column,COL_NAME(fkc.referenced_object_id,fkc.referenced_column_id) AS referenced_column
                     FROM sys.foreign_key_columns fkc WHERE fkc.constraint_object_id=fk.object_id FOR JSON PATH) AS columns
                    FROM sys.foreign_keys fk WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id)='dbo'
                    AND OBJECT_NAME(fk.parent_object_id) IN ('LearningItem','ListeningLesson','TranscriptCue','StoredFile','Level','Topic','AspNetUsers') FOR JSON PATH)
                UNION ALL
                SELECT (SELECT name,OBJECT_NAME(parent_object_id) AS [table],definition,is_disabled,is_not_trusted
                    FROM sys.check_constraints WHERE OBJECT_SCHEMA_NAME(parent_object_id)='dbo'
                    AND OBJECT_NAME(parent_object_id) IN ('LearningItem','ListeningLesson','TranscriptCue','StoredFile','Level','Topic','AspNetUsers') FOR JSON PATH)
                UNION ALL
                SELECT (SELECT name,OBJECT_NAME(parent_object_id) AS [table],COL_NAME(parent_object_id,parent_column_id) AS [column],definition
                    FROM sys.default_constraints WHERE OBJECT_SCHEMA_NAME(parent_object_id)='dbo'
                    AND OBJECT_NAME(parent_object_id) IN ('LearningItem','ListeningLesson','TranscriptCue','StoredFile','Level','Topic','AspNetUsers') FOR JSON PATH)
                """).ToListAsync();
            foreach (var item in metadata) Console.WriteLine(item);
            await context.Database.CloseConnectionAsync();
            Console.WriteLine("Connection and column mapping verified; key/constraint metadata collected for review. CRUD/HTTP NOT RUN; no data writes.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("STOP at: " + stage);
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                Console.Error.WriteLine(current.GetType().FullName + ": " + current.Message);
                Console.Error.WriteLine(current.StackTrace);
            }
            return 1;
        }
    }
}
sealed class SchemaIdentity
{
    public string ServerName { get; set; } = "";
    public string DatabaseName { get; set; } = "";
}
sealed class SchemaColumn
{
    public string SchemaName { get; set; } = "";
    public string TableName { get; set; } = "";
    public string ColumnName { get; set; } = "";
    public string TypeName { get; set; } = "";
    public int MaxLength { get; set; }
    public bool IsNullable { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsComputed { get; set; }
    public int Scale { get; set; }
}
