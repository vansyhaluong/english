# AGENTS.md

## 1. Project

English is an English-learning web application built with:

- .NET 10;
- ASP.NET Core MVC;
- EF Core 10;
- SQL Server;
- Razor Views;
- JavaScript only where needed;
- Database First development.

Keep the project simple, clear, and maintainable.

Use `docs/TASKS.md` for phase, feature, and acceptance details. Do not duplicate
those details here.

## 2. Architecture

Use the existing MVC flow:

```text
Controller
  -> Service
  -> ApplicationDbContext
  -> SQL Server
```

- Controllers handle HTTP concerns, basic request validation, ViewModels, and
  responses.
- Services contain application and business logic.
- Services may use `ApplicationDbContext` directly.
- Razor Views focus on presentation and must not query the database.
- Prefer strongly typed ViewModels.
- Use ViewModels/InputModels/request DTOs for forms and mutations.
- Do not bind database entities directly to public forms when that creates
  over-posting or persistence coupling.

Do not introduce these patterns unless explicitly requested:

- Generic Repository;
- Unit of Work;
- CQRS;
- MediatR;
- event bus;
- microservices.

## 3. Database / EF Core

- SQL Server is the persistence store.
- The database schema is the source of truth.
- The project uses Database First.
- Use `Data/ApplicationDbContext.cs` as the EF Core data-access layer.
- Scaffolded entities are under `Models/Entities/`.
- Generated files may be overwritten by future reverse engineering; keep
  application behavior outside generated entities when practical.
- Do not change tables, columns, keys, constraints, indexes, relationships,
  nullability, or SQL types unless explicitly requested.
- Do not create or run EF Core migrations unless explicitly requested.
- Do not run `dotnet ef migrations add` or `dotnet ef database update` without
  explicit approval.
- Do not re-scaffold with `--force` without explicit approval.
- Do not modify generated relationship mappings without verifying the database.
- Use asynchronous EF Core APIs for database I/O.
- Use `AsNoTracking()` for read-only queries when tracking is unnecessary.
- Project only the data needed and avoid N+1 queries.
- Respect existing rowversion/concurrency and soft-delete behavior.
- Do not change the schema merely to simplify application code.

## 4. Authentication

- The existing `AspNetUsers` table is the application user store.
- Future authentication uses Cookie Authentication.
- Use `IPasswordHasher<AspNetUser>` for password hashing and verification.
- Put authentication logic in a custom `AuthService` or equivalent application
  service.
- Use claims for necessary user identity and role data.
- Do not introduce the full default ASP.NET Core Identity schema unless
  explicitly requested.
- Do not introduce `IdentityDbContext` or make `AspNetUser` inherit from
  `IdentityUser<Guid>` without explicit approval.
- Do not add default Identity tables automatically.
- Authorization must be enforced on the server.

## 5. Development Rules

- Read relevant existing files before editing.
- Understand current behavior before changing it.
- Preserve existing naming, structure, conventions, and behavior unless the task
  requires a change.
- Prefer the smallest correct implementation.
- Do not spend time on broad repository cleanup unless explicitly asked.
- Keep controllers focused and business logic in services.
- Use dependency injection and async/await.
- Do not use `.Result` or `.Wait()` in request handling.
- Use clear types, enums, or constants instead of unexplained magic values.
- Validate request shape in ViewModels/InputModels and state-dependent rules in
  services.
- Treat database constraints as the final integrity boundary.
- Do not add or upgrade dependencies unless strictly required by the task.
- Do not overwrite unrelated uncommitted work.
- Do not modify working code solely for stylistic preference.

## 6. Task Scope Lock

Treat the user's explicit task as a hard boundary.

Do not implement:

- adjacent tasks;
- future phase work;
- optional cleanup;
- unrelated bug fixes;
- stylistic refactors;
- UI redesigns;
- dependency upgrades;
- database/schema changes;
- "nice-to-have" improvements;

unless they are strictly required to complete the assigned task.

If an out-of-scope change is genuinely required:

1. Stop.
2. Explain why.
3. Ask for confirmation before editing it.

If an unrelated issue is discovered:

- report it under `Out-of-scope findings`;
- do not fix it.

Before finishing:

- inspect `git diff`;
- ensure every file changed for the current task is necessary;
- revert only unrelated changes introduced by the current task;
- preserve unrelated changes that existed before the task.

## 7. Security

- Never store plaintext passwords.
- Never hardcode or commit passwords, API keys, connection-string credentials,
  authentication cookies, or sensitive tokens.
- Use configuration, user secrets, or environment variables for secrets.
- Never log passwords, password hashes, secrets, cookies, or sensitive tokens.
- Use POST for state-changing MVC actions and apply anti-forgery protection.
- Validate authorization, ownership, and account state on the server.
- Do not expose stack traces, SQL errors, exception details, or secrets to users.
- Validate uploaded file type, size, safe storage key, and ownership when file
  handling is in scope.
- Do not trust user-provided file names as storage paths.

## 8. Validation

After code changes:

- run `dotnet build`;
- run relevant automated tests when they exist;
- perform focused manual validation when the changed workflow requires it;
- run `git diff --check`;
- inspect the final diff and changed-file list.

Do not claim a build, test, or manual check passed unless it was actually run.

For documentation-only tasks, run the validation explicitly requested by the
user and do not add unrelated code validation.

## 9. Stop Conditions

Stop and ask for confirmation before any of these actions unless explicitly
requested:

- changing the database schema;
- creating or running migrations;
- running database updates or deleting data;
- changing the authentication architecture;
- introducing full ASP.NET Core Identity;
- adding a major dependency;
- replacing MVC with a SPA or another architecture;
- introducing Generic Repository, Unit of Work, CQRS, or MediatR;
- re-scaffolding with `--force`;
- making a breaking data contract or persistence change;
- rewriting a working subsystem outside the assigned task.

## 10. Completion Report

Report only verified results:

- changed files;
- concise description of changes;
- database/schema impact;
- dependency impact;
- build result when code changed;
- test and manual-validation results;
- remaining blockers or assumptions;
- `Out-of-scope findings`, if any.

If nothing was found outside scope, report `Out-of-scope findings: None`.
