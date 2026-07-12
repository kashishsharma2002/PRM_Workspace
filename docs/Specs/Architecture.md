# Architecture

## Principles

- **Controllers** handle HTTP only — no business logic.
- **Services** own business rules and orchestration.
- **Repositories** own queries and persistence.
- **Validators** (FluentValidation) enforce input rules before service execution.
- **Constants** live in `Server/Common/` and `Client/Common/` — no magic strings.

Request flow: `Controller → Service → Repository → PrmDbContext → SQL Server`

## Server Structure

| Folder | Contents |
|--------|----------|
| `Controllers/` | HTTP endpoints grouped by domain |
| `Services/` | Business logic; domain subfolders (see below) |
| `Repositories/` | Data access interfaces and EF implementations |
| `Models/Entities/` | Database models |
| `Models/DTOs/` | API contracts |
| `Validators/` | FluentValidation rules per request DTO |
| `Data/` | `PrmDbContext`, entity configurations |
| `Migrations/` | EF Core schema migrations |
| `Scheduler/` | `BackgroundScheduler`, `SchedulerRunner` |
| `AI/` | LLM providers (`Providers/`), factory and key resolver (`Infrastructure/`) |
| `Common/` | Constants, defaults, error codes, role/status enums |
| `Configuration/` | Options classes (JWT, SMTP, LLM settings) |
| `Exceptions/` | `AppException`, `ValidationAppException`, domain exceptions |
| `DependencyInjection/` | `ServiceCollectionExtensions.cs`, `AuthServiceCollectionExtensions.cs`, `AiServiceCollectionExtensions.cs` |
| `Middleware/` | `ExceptionHandlingMiddleware`, `ForcePasswordChangeMiddleware` |
| `Seed/` | Bootstrap roles and default admin |

### Service subfolders

| Subfolder | Responsibility |
|-----------|----------------|
| `Auth/` | Login, password change, JWT token service |
| `Users/` | User account CRUD, role assignment, password reset |
| `Employees/` | Profiles, skills, manager assignment, resource status |
| `Projects/` | Project CRUD, milestones, health evaluation |
| `Allocations/` | Create, update, end allocations; utilization validation |
| `Timesheets/` | Submission, history, manager views, scheduler missed-timesheet work |
| `Compliance/` | Timesheet compliance checks and reminder emails |
| `Ai/` | Skill match, team builder, risk summary orchestration |
| `Emails/` | SMTP dispatch, template rendering, notification logging |
| `SystemConfig/` | Runtime settings (LLM, scheduler interval, max hours) |
| `Permissions/` | Role capability catalog (read-only API) |
| `Audit/` | Activity log queries |
| `Shared/` | Cross-cutting audit write service |

## Client Structure

| Folder | Contents |
|--------|----------|
| `Screens/` | Role-based console UI (`Admin/`, `Manager/`, `Employee/`) |
| `HttpClients/` | Typed REST clients per domain |
| `Models/` | Client-side DTOs mirroring API responses |
| `Helpers/` | Input validation, screen runners, API load helpers |
| `Common/` | Menu choices, API routes, display constants |
| `Exceptions/` | Client-side error display helpers |

The client reads `ClientSettings:ServerBaseUrl` from `appsettings.json` and calls the server REST API with the JWT from login.

## Authorization

Endpoint access is enforced with JWT role claims via `[Authorize(Roles = ...)]` (`ADMIN`, `MANAGER`, `EMPLOYEE`).

Role capabilities exposed by `GET /api/roles/{roleName}/permissions` are defined in code (`PermissionSeedData` in `Server/Common/Permissions/`) — not stored in database tables. The `PERMISSIONS` and `ROLE_PERMISSIONS` tables were removed; authorization at runtime is role-based only.

## Design Patterns

| Pattern | Where |
|---------|-------|
| Repository | `Server/Repositories/` |
| Strategy | `Server/AI/Providers/` via `ILlmClient` |
| Factory | `Server/AI/Infrastructure/LlmClientFactory.cs` |
| Adapter | `Server/Services/Emails/Providers/SmtpProvider.cs` |

Adding a new LLM provider: implement `ILlmClient` and register it in DI — no changes to `AiIntegrationService`.

## Background Scheduler

`BackgroundScheduler` (`IHostedService`) loops on the interval set by `scheduler_interval_hours` (default: 4 hours). Each run executes `SchedulerRunner`:

| Phase | What it does |
|-------|--------------|
| Project health | Evaluates milestones and effort against hardcoded thresholds (`HealthThresholdDefaults`); sends health alert emails when status turns `RED`; optional AI risk summary in notification |
| Timesheet compliance | Identifies overdue submissions; sends reminder emails |
| Missed timesheets | Creates `MISSED` records for unsubmitted past weeks |
| Resource status | Updates employee status: `BENCH`, `PARTIALLY_ALLOCATED`, `ALLOCATED` |

Job outcomes are written to `SchedulerJobLogs`. On failure, the scheduler waits for the next interval before retrying.

## Database Tables

| Area | Tables |
|------|--------|
| Auth & Users | `Users`, `Roles`, `UserRoles` |
| Resources | `ResourceProfiles`, `Skills`, `UserSkills` |
| Projects | `Projects`, `ProjectMilestones`, `ProjectAllocations` |
| Timesheets | `Timesheets`, `TimesheetLineItems`, `ActivityTags`, `TimesheetLineItemActivityTags` |
| System | `SystemConfigurations`, `AuditLogs`, `SchedulerJobLogs`, `EmailTemplates`, `EmailLogs`, `AiRequestLogs` |

Migrations run automatically on server startup (non-Testing environments). Seed data creates default roles and the bootstrap admin account.
