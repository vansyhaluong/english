// SQL Server integration checks. Only the explicitly authorized copy may be mutated.
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AngleSharp.Html.Parser;
using English.Data;
using English.Interfaces;
using English.Models;
using English.Models.Entities;
using English.Models.ViewModels.Vocabulary;
using English.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

void Check(bool ok,string label) { if(!ok)throw new InvalidOperationException(label);Console.WriteLine("PASS "+label); }
var baseUri=new Uri(args[1]);
Check(baseUri.IsLoopback&&baseUri.Port==5112,"dedicated test app on port 5112");
var env=Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
var cs=new SqlConnectionStringBuilder(env??""){ConnectTimeout=15};
Check(!string.IsNullOrWhiteSpace(env)&&cs.DataSource=="LAPTOP-8RQB10J4"&&cs.InitialCatalog=="English_P2_Test","explicit test DB override");
var options=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs.ConnectionString).Options;
ApplicationDbContext Db()=>new(options);
await using var db=Db();
async Task Guard() {
    await using var con=new SqlConnection(cs.ConnectionString);await con.OpenAsync();await using var cmd=con.CreateCommand();
    cmd.CommandText="SELECT DB_NAME(),CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))";
    await using var r=await cmd.ExecuteReaderAsync();await r.ReadAsync();
    Check(r.GetString(0)=="English_P2_Test"&&r.GetString(1)=="LAPTOP-8RQB10J4","actual SQL identity before mutation");
}
// Only hashes are emitted; row values (including user credentials) stay in memory.
async Task<string> Snapshot(string catalog) {
    var readCs=new SqlConnectionStringBuilder(cs.ConnectionString){InitialCatalog=catalog};
    await using var con=new SqlConnection(readCs.ConnectionString);await con.OpenAsync();var tables=new List<string>();
    await using(var cmd=con.CreateCommand()){cmd.CommandTimeout=120;cmd.CommandText="SELECT QUOTENAME(SCHEMA_NAME(schema_id))+'.'+QUOTENAME(name) FROM sys.tables WHERE is_ms_shipped=0 ORDER BY schema_id,name";await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync())tables.Add(r.GetString(0));}
    var all=new StringBuilder();
    foreach(var table in tables){var hashes=new List<string>();await using var cmd=con.CreateCommand();cmd.CommandTimeout=120;cmd.CommandText="SELECT * FROM "+table;await using var r=await cmd.ExecuteReaderAsync();all.Append(table);
        for(var i=0;i<r.FieldCount;i++)all.Append(r.GetName(i)).Append(r.GetDataTypeName(i));
        while(await r.ReadAsync()){var values=new string?[r.FieldCount];for(var i=0;i<values.Length;i++)values[i]=r.IsDBNull(i)?null:r.GetValue(i) switch {byte[] b=>Convert.ToBase64String(b),DateTimeOffset d=>d.ToString("O"),DateTime d=>d.ToString("O"),IFormattable v=>v.ToString(null,CultureInfo.InvariantCulture),var v=>v.ToString()};hashes.Add(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(values)))));}
        hashes.Sort(StringComparer.Ordinal);foreach(var h in hashes)all.Append(h);
    }
    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(all.ToString())));
}
await Guard();var sourceBefore=await Snapshot("English");var baseline=await Snapshot("English_P2_Test");
Check(sourceBefore==baseline,"copied background rows equal source");
var marker="P2Gate-"+Guid.NewGuid().ToString("N");var users=new[]{Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid()};var password=Guid.NewGuid().ToString("N")+"aA1!";
var levels=await db.Levels.AsNoTracking().OrderBy(x=>x.SortOrder).Take(2).ToArrayAsync();Check(levels.Length==2,"existing levels");
int topic=0,group=0;
VocabularyService V(ApplicationDbContext c)=>new(c,new MediaUrlValidator());
ClassificationService C(ApplicationDbContext c)=>new(c);
HttpClient Client()=>new(new HttpClientHandler{AllowAutoRedirect=false,CookieContainer=new CookieContainer()}){BaseAddress=baseUri};
using var admin=Client();using var student=Client();using var guest=Client();
string Token(string html)=>new HtmlParser().ParseDocument(html).QuerySelector("input[name='__RequestVerificationToken']")?.GetAttribute("value")??throw new Exception("No antiforgery token");
string Version(string html)=>new HtmlParser().ParseDocument(html).QuerySelector("input[name='Input.RowVersion']")?.GetAttribute("value")??throw new Exception("No RowVersion");
async Task<string> Page(HttpClient client,string path){var r=await client.GetAsync(path);Check(r.StatusCode==HttpStatusCode.OK,"GET "+path);return await r.Content.ReadAsStringAsync();}
async Task<HttpResponseMessage> Post(HttpClient client,string path,Dictionary<string,string> form)=>await client.PostAsync(path,new FormUrlEncodedContent(form));
async Task Login(HttpClient client,Guid user,string? returnUrl=null,string? expectedPath=null){var html=await Page(client,"/Account/Login");var r=await Post(client,"/Account/Login",new(){["Email"]=user+"@example.invalid",["Password"]=password,["ReturnUrl"]=returnUrl??"",["__RequestVerificationToken"]=Token(html)});Check(r.StatusCode==HttpStatusCode.Redirect,"fixture login on test app");var location=new Uri(baseUri,r.Headers.Location!);Check(location.AbsolutePath==(expectedPath??(user==users[0]?"/AdminUsers":"/")),"role-appropriate login redirect");}
VocabularyInputModel Input(string suffix)=>new(){Title=marker+suffix,Word=marker+suffix,Pronunciation="test",PartOfSpeech="Noun",Example="A test example.",LevelId=levels[0].Id,TopicId=topic,Meanings=[new(){MeaningVi=marker+" common first"},new(){MeaningVi=marker+" common second"},new(){MeaningVi=marker+" common third"}]};
VocabularyInputModel Edit(VocabularyDetailsViewModel x)=>new(){Title=x.Title,Word=x.Word,Pronunciation=x.Pronunciation,PartOfSpeech=x.PartOfSpeech,Example=x.Example,LevelId=x.LevelId,TopicId=x.TopicId,IsVisible=x.IsVisible,RowVersion=Convert.ToBase64String(x.RowVersion),Meanings=x.Meanings.Select(m=>new VocabularyMeaningInputModel{Id=m.Id,MeaningVi=m.MeaningVi}).ToList()};
async Task<VocabularyDetailsViewModel> Detail(int id){db.ChangeTracker.Clear();return await V(db).GetAsync(id,true,Guid.Empty,default)??throw new Exception("Missing fixture");}
try {
    await Guard();
    foreach(var (userId,i) in users.Select((id,i)=>(id,i))){var u=new AspNetUser{Id=userId,Email=userId+"@example.invalid",NormalizedEmail=(userId+"@example.invalid").ToUpperInvariant(),FullName=marker,Role=(byte)(i==0?UserRole.Admin:UserRole.Student),IsActive=true,CreatedAt=DateTimeOffset.UtcNow};u.PasswordHash=new PasswordHasher<AspNetUser>().HashPassword(u,password);db.AspNetUsers.Add(u);}
    await db.SaveChangesAsync();await Login(admin,users[0]);await Login(student,users[1]);
    foreach(var (returnUrl,expected) in new[]{("/grammar","/grammar"),("/vocabulary","/vocabulary"),("/listening","/listening"),("/AdminGrammar","/"),("~/AdminVocabulary","/"),("https://example.invalid/","/")}) {
        using var session=Client();await Login(session,users[1],returnUrl,expected);
    }
    var studentHome=new HtmlParser().ParseDocument(await Page(student,"/?culture=en"));
    foreach(var skill in new[]{"vocabulary","grammar","listening"}) {
        var link=studentHome.QuerySelectorAll(".desk-shortcuts a").Single(a=>new Uri(baseUri,a.GetAttribute("href")!).AbsolutePath.EndsWith("/"+skill,StringComparison.OrdinalIgnoreCase));
        Check(new Uri(baseUri,link.GetAttribute("href")!).Query.Contains("culture=en"),"Student desk skill link preserves culture: "+skill);
        Check((await student.GetAsync(link.GetAttribute("href"))).StatusCode==HttpStatusCode.OK,"Student desk opens own skill page: "+skill);
    }
    var profile=new HtmlParser().ParseDocument(await Page(student,"/Account/Profile?culture=en"));
    Check(profile.QuerySelector(".admin-shell")==null&&profile.QuerySelector(".student-shell")!=null,"Student profile uses Student layout");
    foreach(var skill in new[]{"vocabulary","grammar","listening"})Check(profile.QuerySelectorAll(".student-nav a").Any(a=>new Uri(baseUri,a.GetAttribute("href")!).AbsolutePath.EndsWith("/"+skill,StringComparison.OrdinalIgnoreCase)),"Student sidebar skill link: "+skill);
    Check(!profile.QuerySelectorAll(".student-nav a").Any(a=>new Uri(baseUri,a.GetAttribute("href")!).AbsolutePath.StartsWith("/Admin",StringComparison.OrdinalIgnoreCase)),"Student sidebar excludes Admin routes");
    // Unique random users exist only in the test DB; successful HTTP logins verify app target.
    foreach(var kind in new[]{ClassificationKind.Topic,ClassificationKind.GrammarGroup}){
        var create="/AdminClassifications/Create?kind="+kind;var p=await Page(admin,create);
        var form=new Dictionary<string,string>{["Input.Name"]=marker+kind,["Input.Description"]="<script>encoded description</script>",["__RequestVerificationToken"]=Token(p)};
        Check((await Post(admin,create,form)).StatusCode==HttpStatusCode.Redirect,"HTTP classification create "+kind);
        db.ChangeTracker.Clear();var row=(await C(db).GetAllAsync(kind,default)).Single(x=>x.Name==marker+kind);if(kind==ClassificationKind.Topic)topic=row.Id;else group=row.Id;
        var path="/AdminClassifications/Edit/"+row.Id+"?kind="+kind;p=await Page(admin,path);
        Check(!p.Contains("<script>encoded description</script>"),"classification HTML encoded");
        form["Input.RowVersion"]=Version(p);form["Input.Name"]=marker+kind+"-edited";
        Check((await Post(admin,path,form)).StatusCode==HttpStatusCode.Redirect,"classification edit");
        Check((await Post(admin,path,form)).StatusCode==HttpStatusCode.OK,"classification stale edit blocked");
        db.ChangeTracker.Clear();Check((await C(db).GetAsync(kind,row.Id,default))!.Name==form["Input.Name"],"classification edit persisted");
        form["Input.Name"]=" ";form["Input.RowVersion"]=Version(await Page(admin,path));Check((await Post(admin,path,form)).StatusCode==HttpStatusCode.OK,"blank name blocked");
        form["Input.Name"]=new string('x',201);Check((await Post(admin,path,form)).StatusCode==HttpStatusCode.OK,"name length blocked");
        foreach(var action in new[]{create,path,"/AdminClassifications/Delete/"+row.Id+"?kind="+kind})Check((await Post(admin,action,new())).StatusCode==HttpStatusCode.BadRequest,"classification CSRF "+kind);
    }
    Check(new HtmlParser().ParseDocument(await Page(admin,"/AdminClassifications/Levels")).QuerySelector("#main-content form")==null,"Level read-only");
    await Guard();db.ChangeTracker.Clear();
    Check((await V(db).SaveAsync(null,Input("-one"),default)).Status==VocabularyResult.Success,"Vocabulary create with multiple meanings");
    var first=await db.LearningItems.Where(x=>x.Title==marker+"-one").Select(x=>x.Id).SingleAsync();
    Check((await V(db).SaveAsync(null,Input("-other"),default)).Status==VocabularyResult.Success,"second Vocabulary fixture");
    var other=await db.LearningItems.Where(x=>x.Title==marker+"-other").Select(x=>x.Id).SingleAsync();
    var initial=await Detail(first);var edit=Edit(initial);edit.Meanings=[edit.Meanings[2],new(){MeaningVi=marker+" common new"},edit.Meanings[0]];
    Check((await V(db).SaveAsync(first,edit,default)).Status==VocabularyResult.Success,"atomic meaning add/remove/reorder");
    var reordered=await Detail(first);Check(reordered.Meanings.Select(m=>m.DisplayOrder).SequenceEqual(new[]{1,2,3})&&reordered.Meanings[0].Id==initial.Meanings[2].Id&&reordered.Meanings[2].Id==initial.Meanings[0].Id&&!reordered.Meanings.Any(m=>m.Id==initial.Meanings[1].Id),"persisted order and retained IDs");
    Check((await V(db).SaveAsync(first,edit,default)).Status==VocabularyResult.Conflict,"stale Vocabulary edit blocked");
    var invalid=Edit(reordered);invalid.Meanings[0].Id=(await Detail(other)).Meanings[0].Id;Check((await V(db).SaveAsync(first,invalid,default)).Status==VocabularyResult.Invalid,"foreign meaning ownership blocked");
    invalid=Edit(await Detail(first));invalid.Meanings=[];Check((await V(db).SaveAsync(first,invalid,default)).Status==VocabularyResult.Invalid,"last meaning removal blocked");
    invalid=Edit(await Detail(first));invalid.Meanings[0].MeaningVi=" ";Check((await V(db).SaveAsync(first,invalid,default)).Status==VocabularyResult.Invalid,"blank meaning blocked");
    foreach(var url in new[]{"javascript:alert(1)","data:text/html,unsafe","/relative"}){invalid=Edit(await Detail(first));invalid.AudioUrl=url;Check((await V(db).SaveAsync(first,invalid,default)).Status==VocabularyResult.Invalid,"unsafe media blocked");}
    var before=await Detail(first);var failure=Edit(before);failure.Title=marker+"-rollback";failure.Meanings.Reverse();
    var failingOptions=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs.ConnectionString).AddInterceptors(new FailSecondSave()).Options;
    await using(var failing=new ApplicationDbContext(failingOptions))Check((await V(failing).SaveAsync(first,failure,default)).Status==VocabularyResult.Conflict,"second save failure handled");
    var after=await Detail(first);Check(after.Title==before.Title&&after.RowVersion.SequenceEqual(before.RowVersion)&&after.Meanings.SequenceEqual(before.Meanings),"real SQL rollback preserves original title/version/meanings");
    foreach(var search in new[]{after.Word,after.Title,after.Meanings[1].MeaningVi,marker+" common"}){var rows=await V(db).ListAsync(new(){Search=search},false,users[1],default);Check(rows.Items.Any(x=>x.Id==first)&&rows.Items.Select(x=>x.Id).Distinct().Count()==rows.Items.Count,"Word/Title/any meaning search without duplicates");}
    Check((await V(db).ListAsync(new(){Search=marker,LevelId=levels[1].Id,TopicId=topic},false,users[1],default)).Items.Count==0,"Level/Topic filter empty state");
    Check((await V(db).ListAsync(new(){Search=marker,Page=999},false,users[1],default)).Query.Page==1,"page clamp");
    DateTimeOffset? firstLearned=null;
    foreach(var learned in new[]{true,false,true}){db.ChangeTracker.Clear();Check(await V(db).SetLearnedAsync(first,users[1],learned,default)==VocabularyResult.Success,"Vocabulary mark/unmark/re-mark");db.ChangeTracker.Clear();var p=await db.UserLearningProgresses.AsNoTracking().SingleAsync(x=>x.UserId==users[1]&&x.LearningItemId==first);Check(p.IsLearned==learned&&(p.LearnedAt!=null)==learned,"learned timestamp state");if(learned){if(firstLearned!=null)Check(p.LearnedAt>firstLearned,"re-mark timestamp updated");firstLearned=p.LearnedAt;}Check(!await db.UserLearningProgresses.AnyAsync(x=>x.UserId==users[2]&&x.LearningItemId==first),"other user progress unchanged");}
    Check((await V(db).ListAsync(new(){Search=marker,Learned=true},false,users[1],default)).Items.Select(x=>x.Id).SequenceEqual(new[]{first}),"learned filter");
    Check((await V(db).ListAsync(new(){Search=marker,Learned=false},false,users[1],default)).Items.Select(x=>x.Id).SequenceEqual(new[]{other}),"unlearned filter");
    db.ChangeTracker.Clear();var t=await C(db).GetAsync(ClassificationKind.Topic,topic,default);Check(await C(db).DeleteAsync(ClassificationKind.Topic,topic,t!.RowVersion,default)==ClassificationResult.Referenced,"referenced Topic delete blocked");
    var gs=new GrammarService(db,new GrammarHtmlSanitizer());Check((await gs.SaveAsync(null,new(){Title=marker+"-grammar",LevelId=levels[0].Id,GrammarGroupId=group,Formula="<p><strong>Formula</strong></p>",Usage="<p>Usage</p>",Examples="<p>Example</p>"},default)).Status==GrammarResult.Success,"Grammar group reference fixture");
    db.ChangeTracker.Clear();var g=await C(db).GetAsync(ClassificationKind.GrammarGroup,group,default);Check(await C(db).DeleteAsync(ClassificationKind.GrammarGroup,group,g!.RowVersion,default)==ClassificationResult.Referenced,"referenced GrammarGroup delete blocked");
    foreach(var kind in new[]{ClassificationKind.Topic,ClassificationKind.GrammarGroup}){db.ChangeTracker.Clear();Check(await C(db).CreateAsync(kind,marker+"-race-"+kind,null,default)==ClassificationResult.Success,"unreferenced race fixture");var row=(await C(db).GetAllAsync(kind,default)).Single(x=>x.Name==marker+"-race-"+kind);var rv=(await C(db).GetAsync(kind,row.Id,default))!.RowVersion;await using var a=Db();await using var b=Db();var results=await Task.WhenAll(C(a).DeleteAsync(kind,row.Id,rv,default),C(b).DeleteAsync(kind,row.Id,rv,default));Check(results.Count(x=>x==ClassificationResult.Success)==1&&results.All(x=>x is ClassificationResult.Success or ClassificationResult.Conflict or ClassificationResult.NotFound),"concurrent delete safe "+kind);}
    await Guard();
    foreach(var path in new[]{"/AdminClassifications?kind=Topic","/AdminVocabulary","/AdminGrammar"}){var r=await student.GetAsync(path);Check(r.StatusCode==HttpStatusCode.Redirect&&r.Headers.Location!.ToString().Contains("AccessDenied"),"Student denied "+path);r=await guest.GetAsync(path);Check(r.StatusCode==HttpStatusCode.Redirect&&r.Headers.Location!.ToString().Contains("Account/Login"),"Guest denied "+path);}
    foreach(var path in new[]{"/vocabulary","/grammar"}){var r=await admin.GetAsync(path);Check(r.StatusCode==HttpStatusCode.Redirect&&r.Headers.Location!.ToString().Contains("AccessDenied"),"Admin denied Student "+path);}
    var createPage=await Page(admin,"/AdminVocabulary/Create?culture=en");
    var vf=new Dictionary<string,string>{["Input.Title"]=marker+"-http",["Input.Word"]=marker+"-http-word",["Input.Pronunciation"]="test",["Input.PartOfSpeech"]="Verb",["Input.Example"]="Test.",["Input.LevelId"]=levels[0].Id.ToString(),["Input.TopicId"]=topic.ToString(),["Input.IsVisible"]="true",["Input.Meanings[0].MeaningVi"]=marker+" HTTP common first",["Input.Meanings[1].MeaningVi"]=marker+" HTTP common second",["Input.Kind"]="2",["Input.IsDeleted"]="true",["__RequestVerificationToken"]=Token(createPage)};
    Check((await Post(admin,"/AdminVocabulary/Create",vf)).StatusCode==HttpStatusCode.Redirect,"HTTP Vocabulary create");
    db.ChangeTracker.Clear();var item=await db.LearningItems.AsNoTracking().SingleAsync(x=>x.Title==marker+"-http");var id=item.Id;Check(item.Kind==1&&!item.IsDeleted,"overposted persistence fields ignored");
    var page=await Page(student,"/vocabulary/"+id+"?culture=en");var dom=new HtmlParser().ParseDocument(page);Check(dom.QuerySelectorAll("[data-meaning-id]").Length==2&&dom.QuerySelector("audio")==null&&page.Contains("Mark learned"),"HTTP detail/optional audio/EN");
    var listing=await Page(student,"/vocabulary?Search="+Uri.EscapeDataString(marker+" HTTP common"));var studentCatalog=new HtmlParser().ParseDocument(listing);Check(studentCatalog.QuerySelectorAll("article[data-vocabulary-id]").Length==1,"HTTP any-meaning search no duplicate");
    Check(studentCatalog.QuerySelector(".vocab-grid")!=null&&studentCatalog.QuerySelector(".management-table, .admin-shell, .student-shell, [data-speak]")==null,"Student Vocabulary uses own learning UI without Admin table or TTS");
    Check(dom.QuerySelector(".vocab-deck")!=null&&dom.QuerySelector(".admin-shell, .student-shell, .management-card")==null,"Student Vocabulary detail uses learning layout");
    foreach(var action in new[]{"Create","Edit/"+id,"Delete/"+id})Check((await Post(admin,"/AdminVocabulary/"+action,new())).StatusCode==HttpStatusCode.BadRequest,"Vocabulary CSRF "+action);
    Check((await Post(student,"/vocabulary/"+id+"/learned",new(){["IsLearned"]="true"})).StatusCode==HttpStatusCode.BadRequest,"progress CSRF");
    var pf=new Dictionary<string,string>{["IsLearned"]="true",["UserId"]=users[2].ToString(),["__RequestVerificationToken"]=Token(page)};
    Check((await Post(student,"/vocabulary/"+id+"/learned",pf)).StatusCode==HttpStatusCode.Redirect,"HTTP mark");db.ChangeTracker.Clear();Check(await db.UserLearningProgresses.AnyAsync(x=>x.UserId==users[1]&&x.LearningItemId==id&&x.IsLearned)&&!await db.UserLearningProgresses.AnyAsync(x=>x.UserId==users[2]&&x.LearningItemId==id),"HTTP forged UserId ignored");
    pf["IsLearned"]="false";Check((await Post(student,"/vocabulary/"+id+"/learned",pf)).StatusCode==HttpStatusCode.Redirect,"HTTP unmark");
    var ep=await Page(admin,"/AdminVocabulary/Edit/"+id);vf["Input.RowVersion"]=Version(ep);vf["__RequestVerificationToken"]=Token(ep);var detail=await Detail(id);
    vf["Input.Meanings[0].Id"]=detail.Meanings[1].Id.ToString();vf["Input.Meanings[1].Id"]=detail.Meanings[0].Id.ToString();vf["Input.Meanings[0].MeaningVi"]=detail.Meanings[1].MeaningVi;vf["Input.Meanings[1].MeaningVi"]=detail.Meanings[0].MeaningVi;
    Check((await Post(admin,"/AdminVocabulary/Edit/"+id,vf)).StatusCode==HttpStatusCode.Redirect,"HTTP reorder");Check((await Detail(id)).Meanings[0].Id==detail.Meanings[1].Id,"HTTP reorder persisted");Check((await Post(admin,"/AdminVocabulary/Edit/"+id,vf)).StatusCode==HttpStatusCode.OK,"HTTP stale edit blocked");
    vf["Input.RowVersion"]=Version(await Page(admin,"/AdminVocabulary/Edit/"+id));vf["Input.IsVisible"]="false";Check((await Post(admin,"/AdminVocabulary/Edit/"+id,vf)).StatusCode==HttpStatusCode.Redirect,"HTTP hide");
    Check((await student.GetAsync("/vocabulary/"+id)).StatusCode==HttpStatusCode.NotFound,"hidden detail blocked");Check((await Post(student,"/vocabulary/"+id+"/learned",pf)).StatusCode==HttpStatusCode.NotFound,"hidden progress blocked");
    var dp=await Page(admin,"/AdminVocabulary/Delete/"+id);Check((await Post(admin,"/AdminVocabulary/Delete/"+id,new(){["Input.RowVersion"]=Version(dp),["__RequestVerificationToken"]=Token(dp)})).StatusCode==HttpStatusCode.Redirect,"HTTP soft delete");
    db.ChangeTracker.Clear();Check(await db.Vocabularies.AnyAsync(x=>x.LearningItemId==id)&&await db.VocabularyMeanings.CountAsync(x=>x.VocabularyId==id)==2&&await db.UserLearningProgresses.AnyAsync(x=>x.LearningItemId==id),"soft delete keeps subtype/meanings/progress");
    var hide=Edit(await Detail(first));hide.IsVisible=false;Check((await V(db).SaveAsync(first,hide,default)).Status==VocabularyResult.Success,"service hide");db.ChangeTracker.Clear();Check(await V(db).GetAsync(first,false,users[1],default)==null&&await V(db).SetLearnedAsync(first,users[1],false,default)==VocabularyResult.NotFound,"hidden service guards");
    var d=await Detail(other);Check(await V(db).DeleteAsync(other,Convert.ToBase64String(d.RowVersion),default)==VocabularyResult.Success,"service soft delete");db.ChangeTracker.Clear();Check(await V(db).GetAsync(other,false,users[1],default)==null&&await V(db).SetLearnedAsync(other,users[1],true,default)==VocabularyResult.NotFound,"deleted service guards");
    Check((await V(db).ListAsync(new(){Search=marker},false,users[1],default)).Items.Count==0,"hidden/deleted list excluded");
    Console.WriteLine("PASS Classification/Vocabulary SQL + authenticated HTTP checks");
    async Task LanguageRoundTrip(string path,string? kind=null,string? expectedName=null) {
        string RoutePath(string value)=>value.EndsWith("/Index",StringComparison.OrdinalIgnoreCase)?value[..^6]:value;
        var original = new Uri(baseUri,path);
        var languagePage = new HtmlParser().ParseDocument(await Page(admin,path));
        foreach (var language in new[]{"en","vi"}) {
            var link = languagePage.QuerySelectorAll(".admin-language a").Single(a=>a.TextContent.Trim()==language.ToUpperInvariant());
            var target = new Uri(baseUri,link.GetAttribute("href")!);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(target.Query);
            Check(RoutePath(target.AbsolutePath)==RoutePath(original.AbsolutePath)&&query["culture"]==language,"VI/EN preserves controller/action/ID "+original.AbsolutePath);
            Check(kind is null ? !query.ContainsKey("kind") : query["kind"]==kind,"VI/EN preserves classification kind only where needed");
            languagePage = new HtmlParser().ParseDocument(await Page(admin,target.PathAndQuery));
            Check(languagePage.DocumentElement.GetAttribute("lang")==language,"requested language rendered");
            if(expectedName is not null)Check(languagePage.QuerySelector("input[name='Input.Name']")!.GetAttribute("value")==expectedName,"VI/EN keeps correct classification record");
        }
    }
    foreach (var (kind,recordId) in new[]{("Topic",topic),("GrammarGroup",group)}) {
        foreach(var action in new[]{"Index","Create","Edit/"+recordId,"Delete/"+recordId})
            await LanguageRoundTrip("/AdminClassifications/"+action+"?kind="+kind+"&culture=vi",kind,action.StartsWith("Edit/")?marker+kind+"-edited":null);
    }
    foreach(var path in new[]{"/AdminClassifications/Levels","/AdminUsers","/AdminGrammar","/AdminGrammar/Create","/AdminVocabulary","/AdminVocabulary/Create","/AdminVocabulary/Edit/"+first,"/AdminVocabulary/Delete/"+first,"/Account/Profile","/Account/ChangePassword"})
        await LanguageRoundTrip(path+"?culture=vi");
}
finally {
    await Guard();db.ChangeTracker.Clear();await using var tx=await db.Database.BeginTransactionAsync();
    await db.UserLearningProgresses.Where(x=>users.Contains(x.UserId)).ExecuteDeleteAsync();
    await db.GrammarLessons.Where(x=>x.LearningItem.Title.StartsWith(marker)).ExecuteDeleteAsync();
    await db.VocabularyMeanings.Where(x=>x.Vocabulary.LearningItem.Title.StartsWith(marker)).ExecuteDeleteAsync();
    await db.Vocabularies.Where(x=>x.LearningItem.Title.StartsWith(marker)).ExecuteDeleteAsync();
    await db.LearningItems.Where(x=>x.Title.StartsWith(marker)).ExecuteDeleteAsync();
    await db.GrammarGroups.Where(x=>x.Name.StartsWith(marker)).ExecuteDeleteAsync();await db.Topics.Where(x=>x.Name.StartsWith(marker)).ExecuteDeleteAsync();
    await db.AspNetUsers.Where(x=>users.Contains(x.Id)&&x.FullName==marker).ExecuteDeleteAsync();await tx.CommitAsync();
    Check(await Snapshot("English_P2_Test")==baseline,"fixtures cleaned; copied background rows unchanged");
    Check(await Snapshot("English")==sourceBefore,"source English rows unchanged");
}
sealed class FailSecondSave:SaveChangesInterceptor {
    private int saves;
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,InterceptionResult<int> result,CancellationToken ct=default){if(++saves==2)throw new DbUpdateConcurrencyException("Injected second-save failure");return ValueTask.FromResult(result);}
}
