# Phase P2 review — 2026-10-06

PHASE P2 REVIEW: PASS

Scope: P2-01, P2-02, P2-03 and Gate P2. Read AGENTS.md and docs/TASKS.md.
No P3 implementation. Gate P2 is marked PASS in docs/TASKS.md on the verified
P2 acceptance evidence below. Exercise snapshot integration remains a P4 concern.

## Resolved regression and remaining issues

P2-01 regression is fixed with the user's explicit approval. Only the two VI/EN
anchors in `Views/Shared/_AdminLayout.cshtml` changed: they preserve the current
request's kind for AdminClassifications except Levels, and the current route ID.
No classification kind is hard-coded; controller/action continue to use ambient
route values. UI, business logic and other application functionality are unchanged.

Authenticated HTTP follows EN then VI links on Topic and GrammarGroup
Index/Create/Edit/Delete. Each keeps its controller/action, kind and ID; Edit also
checks the fixture name remains the correct record. Levels, AdminUsers,
AdminGrammar list/create, AdminVocabulary list/create/edit/delete and Admin account
profile/password pages keep their routes without receiving a classification kind.
All round-trip checks PASS. Remaining mandatory P2 issues: None.

## P2-01

- SQL and authenticated Admin HTTP: create/edit Topic and GrammarGroup,
  blank/overlong name validation, HTML encoding and stale RowVersion rejection PASS.
- Level page is read-only; existing A1–C2 metadata verified.
- Referenced Topic/GrammarGroup deletion is blocked PASS. Unreferenced concurrent
  deletion produces exactly one success for each kind PASS.
- AdminOnly, Guest/Student denial and create/edit/delete POST without antiforgery
  returning HTTP 400 PASS.
- Review of ClassificationService confirms all LearningItem references, including
  hidden/deleted items, are checked; Topic also checks Book/Exercise. The earlier
  Book/Exercise and stale-delete runtime evidence is recorded in docs/TASKS.md;
  those additional scenarios were not repeated in this run.
- Classification language links and other Admin page round trips PASS as described above.

## P2-02

- SQL + authenticated HTTP create/edit/reorder/hide/soft delete PASS.
- Multiple meanings, add/remove/reorder with retained IDs and positive sequential
  order PASS. Empty/blank meanings and foreign meaning ownership are rejected.
- An interceptor throws DbUpdateConcurrencyException on the second SaveChanges
  after the first SQL write: original title, RowVersion and meanings are preserved
  by the transaction rollback PASS.
- Word/Title/second-meaning search and several matching meanings without duplicate
  vocabulary rows PASS; Level/Topic/learned filters, empty state and page clamp PASS.
- Unsafe media URLs are rejected; absent audio is not rendered PASS.
- Mark/unmark/re-mark timestamps, independent user progress, forged UserId rejection
  and antiforgery PASS. Hidden/deleted items are excluded and cannot receive progress.
- Soft delete retains subtype, meanings and progress PASS.
- Browser Admin list/form and client meaning add/remove/reorder checked. The copied
  background contains a deleted Vocabulary; positive Student data assertions use
  disposable fixtures through authenticated HTTP rather than treating an empty list
  as acceptance evidence.

## P2-03

- Grammar SQL and authenticated HTTP harness exits 0: create/edit/stale version,
  visibility/soft delete, required/nonexistent Level/GrammarGroup, filters/search,
  progress ownership and timestamps PASS.
- Sanitizer allowlist, seven XSS payload families, persisted sanitization and safe
  bold/table rendering PASS. Hidden/deleted list/detail/progress are blocked.
- Admin create/edit/delete and Student progress missing antiforgery return HTTP 400.
- Theory/Practice tabs work while unlearned; Practice is only the P4 placeholder.
- Grammar VI/EN labels, accessible catalog/font labels and tab text verified through
  authenticated HTTP and browser. Font toggle changes its EN accessible label.
- Browser renders the five copied visible Grammar lessons from DB, grouped counts
  1/1/3 and selected rich-text content. No sample lesson fallback.

## Gate P2 evidence

- Build after the two-link correction: `dotnet build --output C:/Windows/Temp/english-p2-link-build -p:UseAppHost=false --no-restore`
  PASS, 0 warnings/errors.
- Grammar: `powershell -NoProfile -File scripts/check-grammar.ps1 -Database -BaseUrl http://127.0.0.1:5112`
  PASS with temporary test DB environment override.
- Classification/Vocabulary: `node scripts/check-vocabulary.mjs http://127.0.0.1:5112`
  runs the new SQL/HTTP harness. Functional and language-link regression checks
  PASS, final exit 0. Fixture cleanup and source/background hash comparisons PASS.
- Browser: desktop Admin/Student pages inspected; Grammar VI/EN, tab switching,
  font toggle and landing rendering checked. No browser outage. Mobile screenshot
  capture was insufficient for a full visual conclusion; no app changes were made
  to work around that tool limitation.
- Landing: existing layout and scene-motion button remain functional. Grammar
  shell localization is conditional on grammar-page; landing text/CSS is preserved.
- JavaScript syntax: check-vocabulary.mjs, grammar.js, site.js and
  vocabulary-meanings.js PASS with node --check.
- Final diff checked; git diff --check PASS. Controllers/services use typed
  ViewModels/InputModels, server policies and antiforgery. No GenericRepository or
  UnitOfWork introduced/found in application C# searches.

## Database protection and cleanup

- Server: LAPTOP-8RQB10J4. New English_P2_Test restored from a COPY_ONLY backup of
  English with checksum/restore verification and distinct data/log files.
- Test app ran on dedicated port 5112 with temporary
  ConnectionStrings__DefaultConnection environment override. No repository
  connection-string/configuration changes. SQL DB_NAME()/ServerName guards verify
  the exact target before fixtures and cleanup; unique test-only account logins
  also verify that HTTP writes reach the test DB.
- Fixture markers: unique P2Gate- and P203- prefixes; generated Admin/Student
  accounts, classifications/content/progress cleaned. Copied background retained.
- Hash comparisons of all rows and column names/types in 37 user tables verify
  copied background unchanged after cleanup and source English unchanged. An earlier
  source metadata read timed out; the completed reruns verify both comparisons.
- English_P2_Test is retained. The dedicated test app is stopped after checks.

source DB English modified: NO

test DB used: English_P2_Test

test fixtures cleaned: YES

schema/migration/scaffold changes to source DB: NO

Dependency changes: NO. No migration, re-scaffold, ALTER or source DML was run.

## Changed files

Student navigation follow-up (2026-10-06): Student default login now opens the
Home desk instead of Profile; a Student return URL pointing to an Admin path falls
back to the desk, while valid local learning return URLs are retained. Admin
default redirect remains AdminUsers. Home/rail skill links preserve culture and
open the existing Vocabulary/Grammar/Listening routes; the Student sidebar also
includes Listening. No P3 authoring, audio storage or listening DB work added.

Student Vocabulary now renders Views/Vocabulary/Index.cshtml with the real P2
ViewModel and existing card-grid CSS; Admin still renders the management List view.
Student detail uses the learning layout. Search/Level/Topic/learned filters,
pagination, meaning order, optional audio and POST progress remain in the existing
service/controller flow. Sample catalog, simulated progress and browser TTS are
not used by the Student DB page. Two CSS rules accommodate real titles/meaning lists.

Follow-up validation: build english-student-ui-final-build PASS, 0 warning/error;
check-vocabulary.mjs exits 0 including role redirects, local/external/Admin return
URLs, Student skill links, separate learning views and all prior P2 SQL/HTTP checks.
Source/background hash comparisons and fixture cleanup PASS. Browser verifies
Student desk → Listening and the Vocabulary learning empty state; positive fixture
cards/detail are verified through authenticated HTTP. Rebuilt static assets load
correctly (Vocabulary CSS HTTP 200). Source DB/schema/dependencies are unchanged.

Live-app follow-up: localhost:5096 was still running the older binary and displayed
the shared Vocabulary table. Stopped that exact English process, ran dotnet build
PASS (0 warnings/errors), and restarted the http profile with the current build.
Browser followed Student desk > Luyen tu > /vi/vocabulary?culture=vi and verified
the separate learning layout, filters and real DB empty state. Only GET navigation
was performed against English; no source data mutations or configuration changes.

- Resources/SharedResource.resx, Resources/SharedResource.en.resx: Grammar VI/EN labels.
- Views/Grammar/Index.cshtml, Views/Shared/_Layout.cshtml,
  Views/Shared/_LandingNavigation.cshtml: localized Grammar workspace/shell.
- Views/Shared/_AdminLayout.cshtml: only two VI/EN links preserve kind and route ID.
- wwwroot/js/grammar.js, wwwroot/js/site.js: use localized labels supplied by views.
- scripts/check-grammar.csx: test DB guard, VI/EN and additional HTTP security checks.
- scripts/check-vocabulary.mjs, scripts/check-p2.ps1, scripts/check-p2.csx:
  replace mock acceptance with guarded SQL/HTTP fixtures, rollback and cleanup checks.
- docs/P2-REVIEW.md, docs/TASKS.md: verified review, P2-02 acceptance and Gate P2 PASS.
- Controllers/AccountController.cs, Controllers/VocabularyController.cs,
  Views/Home/Index.cshtml, Views/Shared/_StudentLayout.cshtml,
  Views/Vocabulary/Index.cshtml, Views/Vocabulary/Details.cshtml,
  wwwroot/css/vocabulary.css: Student navigation and existing learning UI connection.

Out-of-scope findings: landing still uses its existing Vietnamese text at culture=en;
no broad landing localization is included in this P2 correction.
