1. Project Overview
This repository contains English, an English-learning web application built with ASP.NET Core MVC.
Technology stack
.NET 10
ASP.NET Core MVC
Entity Framework Core 10
SQL Server
Razor Views
JavaScript where needed
Database First development approach
The project should remain simple, clear, and maintainable. Prefer straightforward ASP.NET Core MVC patterns over unnecessary architectural complexity.
2. Core Development Rules
When working in this repository:
Read the relevant existing files before changing code.
Understand the current behavior before implementing a change.
Keep changes inside the requested scope.
Do not rewrite unrelated working code.
Do not change the database schema unless explicitly requested.
Do not add dependencies unless they are necessary.
Preserve existing naming, structure, conventions, and behavior unless the task requires otherwise.
Prefer the smallest correct change that satisfies the task.
Do not silently expand a task into adjacent features.
If a decision materially affects database design, authentication, security, architecture, or scope, ask for confirmation before proceeding.
3. Architecture
Use a simple ASP.NET Core MVC architecture.
Preferred flow:
Controller  ↓
Service   ↓
ApplicationDbContext   ↓
SQL Server
Controllers
Controllers should:
receive HTTP requests;
validate basic request state;

call services;

prepare ViewModels;

return Views, redirects, or HTTP results.

Avoid putting large business rules directly in controllers.

Services

Services should contain application/business logic such as:

authentication;

registration;

profile updates;

learning progress;

exercise submission;

TOEIC attempt handling;

writing versioning;

AI writing evaluation orchestration;

file handling.

Services may use ApplicationDbContext directly.

Data access

Use ApplicationDbContext as the main EF Core data-access layer.

Do not introduce the following unless explicitly requested:

Generic Repository

Unit of Work

MediatR

CQRS

event bus

microservices

EF Core already provides sufficient data-access abstractions for this project.

Views

Razor Views should focus on presentation.

Avoid:

database access inside Razor;

business logic inside Razor;

resolving services directly from Views;

large state-changing logic in JavaScript when it belongs on the server.

Prefer strongly typed ViewModels.

4. Database Strategy

The project uses Database First.

The SQL Server schema is designed manually first. EF Core entities and ApplicationDbContext are generated from the existing database by reverse engineering/scaffolding.

The database is the source of truth for persistence structure.

Database rules

Do not create or run EF Core migrations unless explicitly requested.

Do not run:

dotnet ef migrations add
dotnet ef database update

unless a migration-based workflow has been explicitly approved.

Do not modify the following unless the task explicitly requires a database change:

tables;

columns;

SQL data types;

nullability;

primary keys;

foreign keys;

unique constraints;

check constraints;

indexes;

relationships.

Do not make schema changes only to make application code easier.

5. EF Core Reverse Engineering

Generated entity files are located in:

Models/Entities/

The generated DbContext is located in:

Data/ApplicationDbContext.cs

These files may be regenerated from SQL Server.

Generated file rules

Avoid putting business logic directly inside generated entity files.

Prefer application-specific behavior in:

services;

ViewModels;

enums;

constants;

helper classes;

extension methods;

separate partial-class files when appropriate.

Do not manually change generated relationship mappings unless the corresponding database relationship has been verified.

Re-scaffolding

Do not use --force casually.

Before re-scaffolding:

confirm that the database schema intentionally changed;

inspect git status;

commit or preserve important work;

understand which generated files will be overwritten.

Typical scaffold form:

dotnet ef dbcontext scaffold "<connection-string>" Microsoft.EntityFrameworkCore.SqlServer \
  --context ApplicationDbContext \
  --context-dir Data \
  --output-dir Models/Entities \
  --namespace English.Models.Entities \
  --context-namespace English.Data \
  --no-onconfiguring

Do not place passwords, API keys, or other secrets in scaffold commands committed to source control.

6. Runtime and EF Core Versions

Target framework:

net10.0

Current EF Core tooling/packages are expected to remain aligned:

Microsoft.EntityFrameworkCore.SqlServer   10.0.12
Microsoft.EntityFrameworkCore.Tools       10.0.12
Microsoft.EntityFrameworkCore.Design      10.0.12
dotnet-ef                                 10.0.12

Avoid mixing EF Core 8, 9, and 10 packages.

Do not upgrade framework or package versions during an unrelated feature task.

7. Main Database Domains

The database contains the major application domains, including:

Level

Topic

GrammarGroup

LearningItem

Vocabulary

VocabularyMeaning

GrammarLesson

ListeningLesson

TranscriptCue

ReadingLesson

Book

BookChapter

Exercise

Question

AnswerOption

AcceptedAnswer

Attempt

AttemptSnapshot

AttemptQuestion

AttemptAnswer

UserLearningProgress

UserChapterProgress

BookReadingPosition

ToeicFormatProfile

ToeicTest

ToeicSection

ToeicPart

QuestionGroup

GroupStimulus

AttemptSection

UserWriting

WritingVersion

WritingEvaluation

StoredFile

AspNetUsers

Do not redesign these domains unless explicitly requested.

8. LearningItem Rules

LearningItem is the shared parent record for learning content.

Current kinds:

1 = Vocabulary
2 = Grammar
3 = Listening
4 = Reading
5 = Writing

Application code should prefer an enum instead of magic numbers.

Example:

public enum LearningItemKind : byte
{
    Vocabulary = 1,
    Grammar = 2,
    Listening = 3,
    Reading = 4,
    Writing = 5
}

The database contains classification constraints controlling valid TopicId and GrammarGroupId combinations for each kind.

Do not weaken or bypass those rules in application code.

9. Authentication Strategy

The project does not use the full default ASP.NET Core Identity database schema.

The existing AspNetUsers table is the application's user table.

Authentication should use:

Cookie Authentication;

the existing AspNetUsers table;

IPasswordHasher<AspNetUser> or an equivalent secure password hasher;

an application authentication service;

claims for identity and role.

Do not automatically introduce the default ASP.NET Core Identity schema.

Do not add these tables unless explicitly requested:

AspNetRoles

AspNetUserRoles

AspNetUserClaims

AspNetUserLogins

AspNetUserTokens

Do not change the generated user entity to inherit from IdentityUser<Guid> without explicit approval.

That would no longer match the current Database First schema.

10. User Roles

Current database role values:

1 = Student
2 = Admin

Prefer a strongly typed enum in application code.

Example:

public enum UserRole : byte
{
    Student = 1,
    Admin = 2
}

Do not scatter numeric role values throughout controllers, services, and Views.

Use claims, policies, enums, or constants as appropriate.

11. Password and Security Rules

Never store plaintext passwords.

Use a secure ASP.NET Core password hasher.

Never log:

passwords;

password hashes;

authentication cookies;

API keys;

secrets;

sensitive tokens.

State-changing MVC forms should use anti-forgery protection.

Prefer POST for mutations.

Authorization must always be enforced on the server.

Hiding a button in Razor or JavaScript is not sufficient authorization.

Do not expose stack traces, SQL errors, or internal exception details to end users.

12. Cookie Authentication

Authentication should support:

Register

Login

Logout

authenticated user claims

role claims

inactive-user handling

password verification

protected routes

correct redirect behavior for unauthenticated users

Claims should contain only necessary data, for example:

User ID

Email

Full name

Role

Do not put large or sensitive objects inside the authentication cookie.

13. Functional Scope

Account

Register

Login

Logout

Profile

Change password

Dashboard and roadmap

Dashboard

CEFR levels A1–C2

User-selected level

Vocabulary

Topic + Level

Word

Pronunciation

One part of speech

Multiple Vietnamese meanings

Example

Optional audio URL

Optional image URL

Learned state/progress

Grammar

Grammar group

Level

Theory

Exercises

Listening

Topic

Level

Audio

SRT/VTT-derived transcript data

Synced transcript cues

Exercises

Reading

Level required

Topic optional

English content

Optional Vietnamese translation

Vocabulary notes

Exercises

Books

Topic based

No required CEFR level

Chapters

Optional translation

Vocabulary notes

Mark chapter read

Last-opened chapter

No PDF/EPUB reader is required unless explicitly added to scope.

Writing

Topic + Level

Multiple writings per prompt

Manual save

Edit

Delete

Version history

Text length limited by database rules

AI evaluation stored against immutable writing versions

AI scoring dimensions:

Task Relevance

Grammar

Vocabulary

Organization/Cohesion

Each dimension uses a 0–10 scale.

The intended overall score uses equal weighting of all four dimensions.

AI feedback should not silently rewrite the learner's text unless a separate rewrite feature is explicitly requested.

TOEIC

Supported scope includes:

one Part practice;

all Listening;

all Reading;

full 7-part / 200-question test.

Attempt history is snapshot-based so historical results remain stable even if source questions change later.

Admin

Admin features may manage:

users;

vocabulary;

grammar;

listening;

reading;

writing topics;

exercises;

questions/answers;

TOEIC content;

results and related learning data.

14. Preferred Implementation Order

Unless a task explicitly requires otherwise, use this implementation order:

Foundation
→ Authentication
→ Profile
→ Dashboard
→ Vocabulary
→ Grammar
→ Listening
→ Reading
→ Books
→ Exercises
→ Learning Progress
→ Writing
→ TOEIC
→ Admin

Do not start later high-complexity modules such as TOEIC or AI Writing before required foundation work is stable, unless explicitly requested.

15. ViewModels

Prefer dedicated ViewModels for user-facing forms and pages.

Use ViewModels for areas such as:

Login

Register

Profile editing

Change password

Create/Edit forms

Search/filter forms

Exercise submissions

Writing submissions

Admin forms

Do not bind database entities directly to public forms when that creates over-posting or persistence coupling.

16. Validation

Use validation at the appropriate layers.

ViewModel validation

Use Data Annotations or equivalent mechanisms for:

required fields;

string length;

email format;

password confirmation;

valid form values.

Service validation

Business rules that depend on current state belong in services, including:

duplicate email checks;

ownership;

account state;

current attempt state;

content state;

allowed transitions.

Database validation

The database remains the final integrity boundary for:

primary keys;

foreign keys;

uniqueness;

check constraints;

nullability;

row versioning.

Do not assume browser or ViewModel validation replaces database integrity.

17. EF Core Query Rules

Use asynchronous EF Core methods for database I/O.

Prefer:

await context.Levels.ToListAsync();
await context.AspNetUsers.FirstOrDefaultAsync(...);
await context.SaveChangesAsync();

Avoid blocking database calls inside HTTP request handling.

Use AsNoTracking() for read-only queries when tracking is unnecessary.

Avoid loading full tables when only a subset is needed.

Prefer projection for display models:

.Select(x => new SomeViewModel
{
    Id = x.Id,
    Name = x.Name
})

Avoid N+1 query patterns.

Use Include only when related data is actually needed.

18. RowVersion and Concurrency

Some tables use SQL Server rowversion.

RowVersion is not a date/time.

It is used for optimistic concurrency.

Do not ignore concurrency tokens on edit workflows that depend on them.

If EF Core throws DbUpdateConcurrencyException, handle the conflict intentionally rather than blindly retrying.

19. Soft Delete

Several tables use:

IsDeleted
DeletedAt

Respect existing soft-delete behavior.

Do not replace soft delete with physical deletion unless explicitly requested.

Normal user-facing queries should generally exclude soft-deleted records.

20. Date and Time

The database uses DATETIMEOFFSET in many places.

Use DateTimeOffset in C# for matching columns.

For fields explicitly named with Utc, preserve UTC semantics.

Do not arbitrarily mix server local time, UTC, and offset-aware values.

21. File Handling

Stored-file metadata exists in the database.

When handling uploads:

validate allowed content type;

validate file size;

generate a safe storage key;

check ownership;

do not trust user-provided file names as storage paths.

Do not store large binary file content in the database unless the schema is intentionally changed.

22. TOEIC Attempt Integrity

TOEIC attempts use snapshot-related tables.

Snapshots preserve what the learner actually saw during the attempt.

Do not calculate historical results from mutable live question data when attempt snapshots exist.

Preserve:

question order;

section/part scope;

answer state;

timing;

result state;

snapshot content.

Do not casually replace snapshot-based logic with direct live-question references.

23. Writing Version Integrity

Writing uses version history.

Treat prior writing versions as immutable historical records.

A new saved revision should create a new version according to application rules rather than modifying a historical version.

Writing evaluations must reference the version that was actually evaluated.

Do not attach an evaluation for one writing version to another version.

24. AI Writing Integration

AI evaluation should be behind a service/interface abstraction.

Do not call an AI provider directly from a controller.

Prefer an abstraction such as:

IWritingEvaluationService
IAiWritingClient

Provider-specific API keys must never be committed to Git.

AI failures must not destroy user writing.

Persist evaluation status and error information according to the existing database model.

25. Configuration and Secrets

Use configuration for:

SQL Server connection strings;

API keys;

external endpoints;

environment-dependent settings.

Do not hardcode secrets into:

controllers;

services;

entity classes;

Razor Views;

JavaScript;

source-controlled documentation;

committed settings files.

Use user secrets or environment variables for sensitive development values.

Machine-specific credentials should not be committed.

26. Git Workflow

Before starting meaningful work:

git status

Do not overwrite unrelated uncommitted changes.

Keep commits focused.

Recommended commit examples:

chore: initialize project foundation
feat: add cookie authentication
feat: implement user registration
feat: add vocabulary learning flow
fix: handle duplicate exercise submission
refactor: extract authentication service

Do not commit:

bin/

obj/

.vs/

IDE user state;

API keys;

passwords;

secrets;

generated temporary files;

unrelated local database backups.

27. Scope Control

If asked to implement one subsystem, do not modify unrelated subsystems for cleanup.

Example: when implementing Login, do not also:

redesign Vocabulary;

rewrite the database;

add TOEIC logic;

replace MVC architecture;

introduce new infrastructure without need.

If a task requires an out-of-scope dependency change, explain why before doing it.

Do not change working code only for stylistic preference.

28. Dependency Rules

Do not add NuGet packages simply to reduce a small amount of code.

First check whether the capability already exists in:

.NET;

ASP.NET Core;

EF Core.

Every new dependency should have a clear purpose.

Do not perform package upgrades during unrelated feature work.

29. Coding Style

Follow modern C# conventions.

Prefer:

clear naming;

dependency injection;

async/await;

nullable reference types;

file-scoped namespaces when consistent;

focused methods;

ViewModels for forms;

services for business logic;

enums/constants instead of magic values.

Avoid:

large controllers;

global mutable state;

service locator patterns;

.Result;

.Wait();

duplicated business logic;

unexplained magic numbers;

unnecessary comments;

direct SQL when EF Core can reasonably perform the operation.

30. Error Handling

Handle expected errors intentionally.

Examples include:

duplicate email;

invalid login;

inactive account;

missing content;

invalid attempt state;

closed/expired attempt;

invalid upload;

concurrency conflicts;

AI provider failure.

Do not catch every exception and silently ignore it.

Unexpected errors should be logged.

User-facing errors must not expose database details, stack traces, or secrets.

31. Logging

Use ILogger<T> when logging is needed.

Useful operational context may include:

failed-login category without passwords;

missing entity IDs;

invalid state transitions;

external AI failures;

concurrency conflicts.

Never log sensitive credentials.

Avoid noisy logs inside tight loops.

32. Validation After Code Changes

After modifying C# code, run:

dotnet build

A task is not complete if the project does not build.

If relevant automated tests exist, run the affected tests.

If a user workflow changes, perform focused manual validation where practical.

Examples:

Authentication

register
→ login
→ authenticated page
→ logout
→ protected page redirects correctly

Vocabulary

choose topic/level
→ view words
→ mark learned
→ progress persists

Exercise

start
→ answer
→ submit
→ score calculated
→ attempt/history saved

Do not claim a test passed unless it was actually run.

33. Completion Report

After implementing a task, report:

files changed;

what was changed;

whether database schema changed;

whether dependencies changed;

build result;

test/manual validation result;

remaining issue, assumption, or blocker.

Example:

TASK COMPLETE

Changed:
- Services/AuthService.cs
- Controllers/AccountController.cs
- Program.cs

Database:
- No schema changes

Dependencies:
- No new packages

Validation:
- dotnet build: PASS
- Register/Login/Logout manual flow: PASS

Remaining:
- Change Password not included in this task

34. Stop Conditions

Stop and ask for confirmation before doing any of the following unless explicitly requested:

changing database schema;

deleting data;

creating EF Core migrations;

changing authentication architecture;

introducing full ASP.NET Core Identity;

adding major dependencies;

replacing MVC with a SPA architecture;

introducing Repository/UoW/CQRS/MediatR;

running scaffold with --force;

rewriting a working subsystem outside task scope;

making a breaking data contract or persistence change.

35. Current Project State

Current known state:

SQL Server database design is complete enough for implementation.

The project targets .NET 10.

EF Core 10.0.12 is installed.

Database First reverse engineering has been performed.

Entity classes have been scaffolded.

ApplicationDbContext has been scaffolded.

ASP.NET Core successfully connects to SQL Server.

Reading Level data from the database has been manually verified.

Authentication implementation has not yet been completed.

The recommended next implementation sequence is:

Cookie Authentication
→ Register
→ Login
→ Logout
→ Authorization
→ Profile
→ Change Password

36. Final Rule

Make the smallest correct change that satisfies the current task while preserving the existing database, architecture, and working behavior.

When in doubt:

read existing code
→ understand current behavior
→ make a scoped change
→ build
→ test
→ report