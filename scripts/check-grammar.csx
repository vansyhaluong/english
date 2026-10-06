// Run: powershell -File scripts/check-grammar.ps1 [-Database] [-BaseUrl http://localhost:PORT]
// Database mode writes ONLY disposable fixture rows, then deletes those exact rows in finally.
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AngleSharp.Html.Parser;
using English;
using English.Authorization;
using English.Controllers;
using English.Data;
using English.Interfaces;
using English.Models;
using English.Models.Entities;
using English.Models.ViewModels.Grammar;
using English.Models.ViewModels.Vocabulary;
using English.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var root = args[0];
void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); Console.WriteLine("PASS " + message); }
var sanitizer = new GrammarHtmlSanitizer();
var safe = "<p><strong>Bold</strong><b>B</b><em>Italic</em><i>I</i><br>Text</p><ul><li>One</li></ul><ol><li>Two</li></ol><table><thead><tr><th>Header</th></tr></thead><tbody><tr><td>Cell</td></tr></tbody></table>";
var cleaned = sanitizer.Sanitize(safe);
foreach (var tag in new[] { "p", "br", "strong", "b", "em", "i", "ul", "ol", "li", "table", "thead", "tbody", "tr", "th", "td" }) Check(cleaned.Contains("<" + tag + ">"), "safe HTML retains " + tag);
var payloads = new[] {
    "<script>alert(1)</script><p>safe</p>",
    "<p onclick=\"alert(1)\" onmouseover=\"alert(2)\">safe</p>",
    "<a href=\"javascript:alert(1)\">unsafe link</a><p>safe</p>",
    "<p href=\"javascript:alert(1)\" style=\"background:url(javascript:alert(1))\">safe</p>",
    "<iframe srcdoc=\"<script>alert(1)</script>\"></iframe><object data=\"javascript:alert(1)\"></object><embed src=\"javascript:alert(1)\"><p>safe</p>",
    "<svg><g onload=\"alert(1)\"></g></svg><math><mtext><img src=x onerror=alert(1)></mtext></math><p>safe</p>",
    "<table style=\"color:red\"><tr><td colspan=\"2\" class=\"x\" id=\"x\" onfocus=\"alert(1)\">safe</td></tr></table>"
};
var allowed = new HashSet<string>(new[] { "html", "head", "body", "p", "br", "strong", "b", "em", "i", "ul", "ol", "li", "table", "thead", "tbody", "tr", "th", "td" });
foreach (var (payload, i) in payloads.Select((p, i) => (p, i))) {
    var output = sanitizer.Sanitize(payload);
    var dom = new HtmlParser().ParseDocument(output);
    Check(dom.All.All(e => allowed.Contains(e.LocalName) && e.Attributes.Length == 0), "XSS payload " + i + " allows only approved tags/no attributes");
    Check(!output.Contains("alert(", StringComparison.OrdinalIgnoreCase), "XSS payload " + i + " removes executable content");
    Check(sanitizer.Sanitize(output) == output, "XSS payload " + i + " sanitizer is idempotent");
}
Check(!sanitizer.HasContent(sanitizer.Sanitize("<script>alert(1)</script>")), "script-only required content becomes empty");
Check(!sanitizer.HasContent("<p>&nbsp;<br></p>"), "blank rich text is not required content");
var invalid = new GrammarInputModel { Title = "Title", Formula = safe, Usage = safe, Examples = safe };
var validation = new List<ValidationResult>();
Validator.TryValidateObject(invalid, new ValidationContext(invalid), validation, true);
Check(validation.Any(v => v.MemberNames.Contains("LevelId")), "required Level validation");
Check(validation.Any(v => v.MemberNames.Contains("GrammarGroupId")), "required GrammarGroup validation");
Check(typeof(GrammarInputModel).GetProperty("TopicId") == null && typeof(GrammarInputModel).GetProperty("Kind") == null && typeof(GrammarInputModel).GetProperty("IsDeleted") == null, "input excludes persistence/over-posting fields");
Check(typeof(GrammarProgressInputModel).GetProperties().Select(p => p.Name).SequenceEqual(new[] { "IsLearned" }), "progress input excludes UserId");
foreach (var (controller, policy) in new[] { (typeof(GrammarController), AuthorizationPolicies.StudentOnly), (typeof(AdminGrammarController), AuthorizationPolicies.AdminOnly) }) {
    Check(controller.GetCustomAttribute<AuthorizeAttribute>()?.Policy == policy, controller.Name + " policy");
    foreach (var action in controller.GetMethods().Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null))
        Check(action.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() != null, controller.Name + "." + action.Name + " POST antiforgery");
}
var fake = new ProgressProbe();
var studentId = Guid.NewGuid();
var victimId = Guid.NewGuid();
var localizer = new ProbeLocalizer();
var student = new GrammarController(fake, localizer) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
student.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, studentId.ToString()), new Claim(ClaimTypes.Role, "Student") }, "probe"));
fake.Result = GrammarResult.NotFound;
await student.Learned(42, new GrammarProgressInputModel { IsLearned = true }, default);
Check(fake.UserId == studentId && fake.UserId != victimId, "controller uses claims user for progress mutation");
student.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
Check(await student.Learned(42, new GrammarProgressInputModel { IsLearned = true }, default) is ForbidResult, "missing user claim cannot mutate progress");
var detailsSource = File.ReadAllText(Path.Combine(root, "Views/Grammar/Details.cshtml"));
Check(detailsSource.Contains("id=\"grammar-theory\"") && detailsSource.Contains("id=\"grammar-practice\"") && detailsSource.Contains("GrammarPracticePlaceholder"), "Theory/Practice sections and placeholder exist");
Check(!detailsSource.Contains("disabled") && !detailsSource.Contains("fieldset") && !detailsSource.Contains("radio"), "Practice has no progress lock or exercise controls");
foreach (var file in new[] { "SharedResource.resx", "SharedResource.en.resx" }) {
    var resources = XDocument.Load(Path.Combine(root, "Resources", file)).Root!.Elements("data").Select(e => e.Attribute("name")!.Value).ToArray();
    Check(resources.Length == resources.Distinct().Count(), "no duplicate resource keys " + file);
    Check(resources.Contains("Grammar") && resources.Contains("GrammarPracticePlaceholder") && resources.Contains("GrammarRequired"), "Grammar localization " + file);
}
var baseIndex = Array.IndexOf(args, "--base-url");
if (baseIndex >= 0) {
    using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
    foreach (var path in new[] { "/grammar", "/vi/grammar", "/grammar/1", "/AdminGrammar", "/AdminGrammar/Create", "/AdminGrammar/Edit/1", "/AdminGrammar/Delete/1" }) {
        var response = await http.GetAsync(args[baseIndex + 1] + path);
        Check(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location?.ToString().Contains("Account/Login") == true, "anonymous redirect " + path);
    }
}
if (!args.Contains("--database")) { Console.WriteLine("DB checks NOT RUN (use -Database)"); return; }
var config = new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", true).AddEnvironmentVariables().Build();
var connection = new SqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection")) { ConnectTimeout = 15 };
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options;
await using var context = new ApplicationDbContext(options);
try { await context.Database.OpenConnectionAsync(); }
catch (SqlException e) { Console.WriteLine("BLOCKED DB connection: SqlException " + e.Number); Environment.ExitCode = 2; return; }
await context.Database.CloseConnectionAsync();
var fixtureTitle = "P203-" + Guid.NewGuid().ToString("N");
var ids = new List<int>();
var groupId = 0;
var topicId = 0;
var students = new[] { Guid.NewGuid(), Guid.NewGuid() };
var adminId = Guid.NewGuid();
var fixtureUsers = students.Append(adminId).ToArray();
var fixturePassword = Guid.NewGuid().ToString("N") + "aA1!";
var ct = CancellationToken.None;
async Task<GrammarService> Service() { context.ChangeTracker.Clear(); return await Task.FromResult(new GrammarService(context, sanitizer)); }
async Task<int> Create(string title, int level, int group) {
    var s = await Service();
    var result = await s.SaveAsync(null, new GrammarInputModel { Title = title, LevelId = level, GrammarGroupId = group, Formula = safe, Usage = "<p onclick='alert(1)'>Use</p>", Examples = "<p>Example</p><script>alert(1)</script>", Notes = "<p style='color:red'>Note</p>", IsVisible = true }, ct);
    Check(result.Status == GrammarResult.Success, "create " + title);
    var id = await context.LearningItems.Where(x => x.Title == title).Select(x => x.Id).SingleAsync();
    ids.Add(id); return id;
}
try {
    var levels = await context.Levels.AsNoTracking().OrderBy(x => x.SortOrder).Take(2).ToArrayAsync();
    Check(levels.Length == 2, "two existing levels for filters");
    var group = new GrammarGroup { Name = fixtureTitle, Description = "Disposable P2-03 validation" };
    var topic = new Topic { Name = fixtureTitle, Description = "Disposable progress validation" };
    context.GrammarGroups.Add(group);
    context.Topics.Add(topic);
    foreach (var id in fixtureUsers) {
        var user = new AspNetUser { Id = id, Email = id + "@example.invalid", NormalizedEmail = (id + "@example.invalid").ToUpperInvariant(), FullName = "P2-03 disposable user", Role = (byte)(id == adminId ? UserRole.Admin : UserRole.Student), IsActive = true, CreatedAt = DateTimeOffset.UtcNow };
        user.PasswordHash = new PasswordHasher<AspNetUser>().HashPassword(user, fixturePassword);
        context.AspNetUsers.Add(user);
    }
    await context.SaveChangesAsync(); groupId = group.Id; topicId = topic.Id;
    var first = await Create(fixtureTitle + "-one", levels[0].Id, groupId);
    var second = await Create(fixtureTitle + "-two", levels[1].Id, groupId);
    var s = await Service();
    var stored = await context.GrammarLessons.AsNoTracking().SingleAsync(x => x.LearningItemId == first);
    Check(stored.Formula.Contains("<strong>") && !stored.Usage.Contains("onclick") && !stored.Examples.Contains("script") && !stored.Notes!.Contains("style"), "persisted rich text is sanitized");
    var parent = await context.LearningItems.AsNoTracking().SingleAsync(x => x.Id == first);
    Check(parent.Kind == 2 && parent.TopicId == null && parent.GrammarGroupId == groupId, "Grammar classification/shared PK");
    var item = await s.GetAsync(first, true, Guid.Empty, ct);
    GrammarInputModel EditInput(GrammarDetailsViewModel x, bool visible) => new() { Title = x.Title + "-edited", LevelId = x.LevelId, GrammarGroupId = x.GrammarGroupId, Formula = x.Formula, Usage = x.Usage, Examples = x.Examples, Notes = x.Notes, IsVisible = visible, RowVersion = Convert.ToBase64String(x.RowVersion) };
    Check((await s.SaveAsync(first, EditInput(item!, true), ct)).Status == GrammarResult.Success, "edit Grammar");
    s = await Service();
    Check((await s.SaveAsync(first, EditInput(item!, true), ct)).Status == GrammarResult.Conflict, "stale RowVersion rejected");
    s = await Service();
    foreach (var (level, groupValue) in new[] { (0, groupId), (levels[0].Id, 0), (int.MaxValue, groupId), (levels[0].Id, int.MaxValue) })
        Check((await s.SaveAsync(null, new GrammarInputModel { Title = fixtureTitle, Formula = safe, Usage = safe, Examples = safe, LevelId = level, GrammarGroupId = groupValue }, ct)).Status == GrammarResult.Invalid, "server classification validation " + level + "/" + groupValue);
    var list = await s.ListAsync(new GrammarQuery { Search = fixtureTitle + "-one" }, false, students[0], ct);
    Check(list.Items.Count == 1 && list.Items[0].Id == first, "search by title");
    list = await s.ListAsync(new GrammarQuery { Search = fixtureTitle, LevelId = levels[1].Id }, false, students[0], ct);
    Check(list.Items.Count == 1 && list.Items[0].Id == second, "filter Level");
    list = await s.ListAsync(new GrammarQuery { GrammarGroupId = groupId }, false, students[0], ct);
    Check(list.Items.Count == 2, "filter GrammarGroup");
    Check(await s.SetLearnedAsync(first, students[0], true, ct) == GrammarResult.Success, "mark learned");
    s = await Service(); var learned = await context.UserLearningProgresses.AsNoTracking().SingleAsync(p => p.UserId == students[0] && p.LearningItemId == first);
    Check(learned.IsLearned && learned.LearnedAt != null, "mark sets timestamp");
    Check(!await context.UserLearningProgresses.AnyAsync(p => p.UserId == students[1] && p.LearningItemId == first), "other user's progress unchanged");
    Check(await s.SetLearnedAsync(first, students[0], false, ct) == GrammarResult.Success, "unmark learned");
    s = await Service(); var unlearned = await context.UserLearningProgresses.AsNoTracking().SingleAsync(p => p.UserId == students[0] && p.LearningItemId == first);
    Check(!unlearned.IsLearned && unlearned.LearnedAt == null, "unmark clears timestamp");
    Check(await s.SetLearnedAsync(first, students[0], true, ct) == GrammarResult.Success, "mark again");
    s = await Service(); var relearned = await context.UserLearningProgresses.AsNoTracking().SingleAsync(p => p.UserId == students[0] && p.LearningItemId == first);
    Check(relearned.LearnedAt > learned.LearnedAt, "mark again writes new timestamp");
    item = await s.GetAsync(first, true, Guid.Empty, ct);
    Check((await s.SaveAsync(first, EditInput(item!, false), ct)).Status == GrammarResult.Success, "hide Grammar");
    s = await Service(); Check(await s.GetAsync(first, false, students[0], ct) == null, "hidden detail unavailable");
    Check(await s.SetLearnedAsync(first, students[0], false, ct) == GrammarResult.NotFound, "hidden progress mutation denied");
    list = await s.ListAsync(new GrammarQuery { GrammarGroupId = groupId }, false, students[0], ct); Check(list.Items.All(x => x.Id != first), "hidden list unavailable");
    item = await s.GetAsync(second, true, Guid.Empty, ct);
    Check(await s.DeleteAsync(second, Convert.ToBase64String(item!.RowVersion), ct) == GrammarResult.Success, "soft delete Grammar");
    s = await Service(); Check(await s.GetAsync(second, false, students[0], ct) == null, "deleted detail unavailable");
    Check(await s.SetLearnedAsync(second, students[0], true, ct) == GrammarResult.NotFound, "deleted progress mutation denied");
    list = await s.ListAsync(new GrammarQuery { GrammarGroupId = groupId }, false, students[0], ct); Check(list.Items.Count == 0, "deleted list unavailable");
    Check(await context.GrammarLessons.AnyAsync(x => x.LearningItemId == second), "soft delete keeps subtype");
    Check(await context.UserLearningProgresses.AnyAsync(x => x.UserId == students[0] && x.LearningItemId == first), "visibility retains progress");
    context.ChangeTracker.Clear(); var vs = new VocabularyService(context, new MediaUrlValidator());
    Check((await vs.SaveAsync(null, new VocabularyInputModel { Title = fixtureTitle + "-vocab", Word = "test", Pronunciation = "test", PartOfSpeech = "Noun", Example = "Test example", LevelId = levels[0].Id, TopicId = topicId, Meanings = [new VocabularyMeaningInputModel { MeaningVi = "Kiểm tra" }] }, ct)).Status == VocabularyResult.Success, "create disposable Vocabulary for progress validation");
    var vocabulary = await context.LearningItems.Where(x => x.Title == fixtureTitle + "-vocab").Select(x => x.Id).SingleAsync();
    Check(await vs.SetLearnedAsync(vocabulary, students[0], true, ct) == VocabularyResult.Success, "Vocabulary mark with new rule");
    context.ChangeTracker.Clear(); var vocabLearned = await context.UserLearningProgresses.AsNoTracking().SingleAsync(x => x.UserId == students[0] && x.LearningItemId == vocabulary);
    Check(vocabLearned.IsLearned && vocabLearned.LearnedAt != null, "Vocabulary mark sets timestamp");
    Check(await vs.SetLearnedAsync(vocabulary, students[0], false, ct) == VocabularyResult.Success, "Vocabulary unmark with new rule");
    context.ChangeTracker.Clear();
    Check((await context.UserLearningProgresses.AsNoTracking().SingleAsync(x => x.UserId == students[0] && x.LearningItemId == vocabulary)).LearnedAt == null, "Vocabulary unmark clears timestamp");
    Check(await vs.SetLearnedAsync(vocabulary, students[0], true, ct) == VocabularyResult.Success, "Vocabulary re-mark with new rule");
    context.ChangeTracker.Clear();
    Check((await context.UserLearningProgresses.AsNoTracking().SingleAsync(x => x.UserId == students[0] && x.LearningItemId == vocabulary)).LearnedAt > vocabLearned.LearnedAt, "Vocabulary re-mark sets new timestamp");
    if (baseIndex >= 0) {
        var baseUrl = args[baseIndex + 1];
        HttpClient Client() => new(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() }) { BaseAddress = new Uri(baseUrl) };
        async Task<string> Page(HttpClient client, string path) {
            var r = await client.GetAsync(path); Check(r.StatusCode == HttpStatusCode.OK, "authenticated GET " + path); return await r.Content.ReadAsStringAsync();
        }
        string Token(string page) {
            var dom = new HtmlParser().ParseDocument(page);
            return dom.QuerySelector("input[name='__RequestVerificationToken']")?.GetAttribute("value") ?? throw new InvalidOperationException("Antiforgery token missing");
        }
        async Task Login(HttpClient client, Guid user) {
            var page = await Page(client, "/Account/Login");
            var r = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string,string> { ["Email"] = user + "@example.invalid", ["Password"] = fixturePassword, ["__RequestVerificationToken"] = Token(page) }));
            Check(r.StatusCode == HttpStatusCode.Redirect, "fixture login");
        }
        using var admin = Client(); using var learner = Client();
        await Login(admin, adminId); await Login(learner, students[0]);
        var forbidden = await learner.GetAsync("/AdminGrammar/Create");
        Check(forbidden.StatusCode == HttpStatusCode.Redirect && forbidden.Headers.Location!.ToString().Contains("AccessDenied"), "Student cannot access Admin Grammar");
        forbidden = await admin.GetAsync("/grammar");
        Check(forbidden.StatusCode == HttpStatusCode.Redirect && forbidden.Headers.Location!.ToString().Contains("AccessDenied"), "Admin cannot access Student Grammar");
        var studentList = await Page(learner, "/grammar");
        forbidden = await learner.PostAsync("/AdminGrammar/Create", new FormUrlEncodedContent(new Dictionary<string,string> { ["__RequestVerificationToken"] = Token(studentList), ["Input.Title"] = fixtureTitle + "-forbidden" }));
        Check(forbidden.StatusCode == HttpStatusCode.Redirect && forbidden.Headers.Location!.ToString().Contains("AccessDenied"), "Student cannot POST Admin Grammar");
        var createPage = await Page(admin, "/AdminGrammar/Create?culture=en");
        Dictionary<string,string> form = new() { ["Input.Title"] = fixtureTitle + "-http", ["Input.Formula"] = safe, ["Input.Usage"] = "<p onclick='alert(1)'>Usage</p>", ["Input.Examples"] = "<p>Example</p><script>alert(1)</script>", ["Input.LevelId"] = levels[0].Id.ToString(), ["Input.GrammarGroupId"] = groupId.ToString(), ["Input.IsVisible"] = "true", ["Input.Kind"] = "1", ["Input.TopicId"] = topicId.ToString(), ["Input.IsDeleted"] = "true", ["__RequestVerificationToken"] = Token(createPage) };
        var withoutLevel = new Dictionary<string,string>(form); withoutLevel.Remove("Input.LevelId");
        var invalidResponse = await admin.PostAsync("/AdminGrammar/Create?culture=en", new FormUrlEncodedContent(withoutLevel));
        Check(invalidResponse.StatusCode == HttpStatusCode.OK && (await invalidResponse.Content.ReadAsStringAsync()).Contains("Select a valid level and grammar group."), "HTTP required Level localized validation");
        var withoutGroup = new Dictionary<string,string>(form); withoutGroup.Remove("Input.GrammarGroupId");
        invalidResponse = await admin.PostAsync("/AdminGrammar/Create?culture=en", new FormUrlEncodedContent(withoutGroup));
        Check(invalidResponse.StatusCode == HttpStatusCode.OK && (await invalidResponse.Content.ReadAsStringAsync()).Contains("Select a valid level and grammar group."), "HTTP required GrammarGroup localized validation");
        Check((await admin.PostAsync("/AdminGrammar/Create", new FormUrlEncodedContent(form))).StatusCode == HttpStatusCode.Redirect, "HTTP Admin create");
        context.ChangeTracker.Clear(); var httpItem = await context.LearningItems.AsNoTracking().SingleAsync(x => x.Title == fixtureTitle + "-http");
        Check(httpItem.Kind == 2 && httpItem.TopicId == null && !httpItem.IsDeleted, "HTTP over-posted persistence fields ignored");
        var detailPage = await Page(learner, "/grammar/" + httpItem.Id);
        var detailDom = new HtmlParser().ParseDocument(detailPage);
        Check(detailDom.QuerySelectorAll(".grammar-html strong").Length > 0 && detailDom.QuerySelectorAll(".grammar-html table").Length > 0 && detailDom.QuerySelectorAll(".grammar-html [onclick], .grammar-html script").Length == 0, "HTTP detail renders safe bold/table and strips XSS");
        Check(detailPage.Contains("grammar-practice") && !detailPage.Contains("type=\"radio\""), "HTTP Practice placeholder without exercises");
        var progressUrl = "/grammar/" + httpItem.Id + "/learned";
        Check((await learner.PostAsync(progressUrl, new FormUrlEncodedContent(new Dictionary<string,string> { ["IsLearned"] = "true" }))).StatusCode == HttpStatusCode.BadRequest, "HTTP progress missing antiforgery rejected");
        var progressForm = new Dictionary<string,string> { ["IsLearned"] = "true", ["UserId"] = students[1].ToString(), ["__RequestVerificationToken"] = Token(detailPage) };
        Check((await learner.PostAsync(progressUrl, new FormUrlEncodedContent(progressForm))).StatusCode == HttpStatusCode.Redirect, "HTTP mark learned with forged UserId");
        context.ChangeTracker.Clear();
        Check(await context.UserLearningProgresses.AnyAsync(x => x.UserId == students[0] && x.LearningItemId == httpItem.Id && x.IsLearned) && !await context.UserLearningProgresses.AnyAsync(x => x.UserId == students[1] && x.LearningItemId == httpItem.Id), "HTTP Student cannot mutate another user's progress");
        progressForm["IsLearned"] = "false";
        Check((await learner.PostAsync(progressUrl, new FormUrlEncodedContent(progressForm))).StatusCode == HttpStatusCode.Redirect, "HTTP unmark learned");
        context.ChangeTracker.Clear();
        Check((await context.UserLearningProgresses.AsNoTracking().SingleAsync(x => x.UserId == students[0] && x.LearningItemId == httpItem.Id)).LearnedAt == null, "HTTP unmark clears timestamp");
        Check((await learner.GetAsync("/grammar?Search=" + fixtureTitle + "-http&LevelId=" + levels[0].Id + "&GrammarGroupId=" + groupId)).StatusCode == HttpStatusCode.OK, "HTTP search and filters");
        var editPage = await Page(admin, "/AdminGrammar/Edit/" + httpItem.Id);
        var editDom = new HtmlParser().ParseDocument(editPage);
        form["Input.RowVersion"] = editDom.QuerySelector("input[name='Input.RowVersion']")!.GetAttribute("value")!;
        form["Input.Title"] = fixtureTitle + "-http-edited"; form["__RequestVerificationToken"] = Token(editPage);
        Check((await admin.PostAsync("/AdminGrammar/Edit/" + httpItem.Id, new FormUrlEncodedContent(form))).StatusCode == HttpStatusCode.Redirect, "HTTP Admin edit");
        context.ChangeTracker.Clear();
        Check((await context.LearningItems.AsNoTracking().SingleAsync(x => x.Id == httpItem.Id)).Title.EndsWith("-edited"), "HTTP edit persisted");
    }
}
finally {
    context.ChangeTracker.Clear();
    await using var cleanup = await context.Database.BeginTransactionAsync();
    await context.UserLearningProgresses.Where(p => fixtureUsers.Contains(p.UserId)).ExecuteDeleteAsync();
    await context.GrammarLessons.Where(x => x.LearningItem.Title.StartsWith(fixtureTitle) && x.LearningItem.Kind == 2).ExecuteDeleteAsync();
    await context.VocabularyMeanings.Where(x => x.Vocabulary.LearningItem.Title.StartsWith(fixtureTitle)).ExecuteDeleteAsync();
    await context.Vocabularies.Where(x => x.LearningItem.Title.StartsWith(fixtureTitle)).ExecuteDeleteAsync();
    await context.LearningItems.Where(x => x.Title.StartsWith(fixtureTitle) && (x.Kind == 2 || x.Kind == 1)).ExecuteDeleteAsync();
    await context.AspNetUsers.Where(x => fixtureUsers.Contains(x.Id)).ExecuteDeleteAsync();
    await context.GrammarGroups.Where(x => x.Id == groupId && x.Name == fixtureTitle).ExecuteDeleteAsync();
    await context.Topics.Where(x => x.Id == topicId && x.Name == fixtureTitle).ExecuteDeleteAsync();
    await cleanup.CommitAsync();
    Check(!await context.LearningItems.AnyAsync(x => x.Title.StartsWith(fixtureTitle)) && !await context.AspNetUsers.AnyAsync(x => fixtureUsers.Contains(x.Id)), "disposable fixture cleanup");
}

sealed class ProbeLocalizer : IStringLocalizer<SharedResource>
{
    public LocalizedString this[string name] => new(name, name);
    public LocalizedString this[string name, params object[] arguments] => new(name, name);
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
}
sealed class ProgressProbe : IGrammarService
{
    public Guid UserId { get; private set; }
    public GrammarResult Result { get; set; }
    public Task<GrammarResult> SetLearnedAsync(int id, Guid userId, bool learned, CancellationToken cancellationToken) { UserId = userId; return Task.FromResult(Result); }
    public Task<GrammarChoices> GetChoicesAsync(CancellationToken c) => throw new NotSupportedException();
    public Task<GrammarListViewModel> ListAsync(GrammarQuery q, bool a, Guid u, CancellationToken c) => throw new NotSupportedException();
    public Task<GrammarDetailsViewModel?> GetAsync(int id, bool a, Guid u, CancellationToken c) => throw new NotSupportedException();
    public Task<GrammarSaveResult> SaveAsync(int? id, GrammarInputModel i, CancellationToken c) => throw new NotSupportedException();
    public Task<GrammarResult> DeleteAsync(int id, string? v, CancellationToken c) => throw new NotSupportedException();
}
