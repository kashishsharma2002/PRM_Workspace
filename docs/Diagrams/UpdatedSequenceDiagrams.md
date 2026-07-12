# PRM Platform — Sequence Diagrams (Critical Flows)

This document describes the six critical end-to-end flows implemented in PRM Platform Milestone 2. Each diagram maps to real code in `Server/` and `Client/`.

For the full endpoint inventory see [API_ENDPOINT_STATUS.md](../API_ENDPOINT_STATUS.md). For the data model see [erDiagram.md](./erDiagram.md).

---

## Legend

| Layer | Responsibility |
|-------|----------------|
| **Client** | Console app (`PRMPlatform/Client`) — sends HTTP requests with JWT |
| **Middleware** | JWT authentication, force-password gate, exception handling |
| **Controller** | HTTP routing, auth role checks, request/response mapping |
| **Service** | Business rules, transactions, audit logging |
| **Repository** | EF Core data access |
| **DB** | MSSQL (`prm_platform_db`) |

**Architecture:** `Client → Middleware → Controller → Service → Repository → DB`

---

## 1. Login & JWT Authentication

| | |
|---|---|
| **Trigger** | User enters credentials on `LoginScreen` |
| **Endpoint** | `POST /api/auth/login` |
| **Auth** | Anonymous |
| **Outcome** | JWT returned with claims: `sub`, `role`, `name`, `force_password_change`, `employee_id`, `manager_id` |

```mermaid
sequenceDiagram
    participant Client as ConsoleClient
    participant Controller as AuthController
    participant Service as AuthService
    participant UserRepo as UserRepository
    participant RoleRepo as RoleRepository
    participant Jwt as JwtTokenService
    participant DB as MSSQL

    Client->>Controller: POST /api/auth/login {username, password}
    Controller->>Service: LoginAsync(request)

    alt Missing username or password
        Service-->>Controller: ValidationAppException
        Controller-->>Client: 400 Bad Request
    else User not found or inactive
        Service->>UserRepo: GetByUsernameAsync(username)
        UserRepo->>DB: SELECT Users
        DB-->>UserRepo: null or inactive row
        Service-->>Controller: UnauthorizedAppException
        Controller-->>Client: 401 Unauthorized
    else Invalid password
        Service->>UserRepo: GetByUsernameAsync(username)
        UserRepo->>DB: SELECT Users
        DB-->>UserRepo: user row
        Service->>Service: BCrypt.Verify(password, hash)
        Service-->>Controller: UnauthorizedAppException
        Controller-->>Client: 401 Unauthorized
    else Valid credentials
        Service->>UserRepo: GetByUsernameAsync(username)
        UserRepo->>DB: SELECT Users
        DB-->>UserRepo: user row
        Service->>Service: BCrypt.Verify(password, hash)
        Service->>RoleRepo: GetRoleNameForUserAsync(userId)
        RoleRepo->>DB: SELECT UserRoles, Roles
        DB-->>RoleRepo: role name
        Service->>UserRepo: GetResourceProfileByUserIdAsync(userId)
        UserRepo->>DB: SELECT ResourceProfiles
        DB-->>UserRepo: profile or null
        Service->>UserRepo: UpdateAsync + SaveChanges (LastLoginAt)
        UserRepo->>DB: UPDATE Users
        Service->>Jwt: CreateToken(user, role, profile)
        Jwt-->>Service: LoginResponseDto + JWT
        Service-->>Controller: LoginResponseDto
        Controller-->>Client: 200 OK {token, role, forcePasswordChange, ...}
    end
```

**Key files:** `Server/Controllers/Auth/AuthController.cs`, `Server/Services/Auth/AuthService.cs`, `Server/Program.cs`

---

## 2. Force Password Change

| | |
|---|---|
| **Trigger** | User logs in with `IsTemporaryPassword = true` (new account or admin reset) |
| **Endpoint** | `POST /api/auth/change-password` (exempt from middleware block) |
| **Auth** | JWT required |
| **Outcome** | New JWT issued with `force_password_change = false`; user can access protected endpoints |

```mermaid
sequenceDiagram
    participant Client as ConsoleClient
    participant JwtMw as JwtAuthentication
    participant ForcePw as ForcePasswordChangeMiddleware
    participant Controller as AuthController
    participant Service as AuthService
    participant UserRepo as UserRepository
    participant RoleRepo as RoleRepository
    participant Jwt as JwtTokenService
    participant DB as MSSQL

    Note over Client,DB: Step 1 — blocked access until password changed

    Client->>JwtMw: GET /api/employees (JWT with force_password_change=true)
    JwtMw->>ForcePw: Authenticated request
    ForcePw->>ForcePw: IsChangePasswordEndpoint? No
    ForcePw-->>Client: 403 Forbidden "Password change required."

    Note over Client,DB: Step 2 — change password (endpoint exempt)

    Client->>JwtMw: POST /api/auth/change-password {currentPassword, newPassword}
    JwtMw->>ForcePw: Authenticated request
    ForcePw->>ForcePw: IsChangePasswordEndpoint? Yes — pass through
    ForcePw->>Controller: Invoke next
    Controller->>Service: ChangePasswordAsync(userId, request)

    alt Invalid current password or weak new password
        Service-->>Controller: ValidationAppException
        Controller-->>Client: 400 Bad Request
    else Success
        Service->>UserRepo: GetByIdAsync(userId)
        UserRepo->>DB: SELECT Users
        DB-->>UserRepo: user row
        Service->>Service: BCrypt hash new password, IsTemporaryPassword=false
        Service->>UserRepo: UpdateAsync + SaveChanges
        UserRepo->>DB: UPDATE Users
        Service->>RoleRepo: GetRoleNameForUserAsync(userId)
        RoleRepo->>DB: SELECT Roles
        Service->>UserRepo: GetResourceProfileByUserIdAsync(userId)
        UserRepo->>DB: SELECT ResourceProfiles
        Service->>Jwt: CreateToken(user, role, profile)
        Jwt-->>Service: new JWT (force_password_change=false)
        Service-->>Controller: LoginResponseDto
        Controller-->>Client: 200 OK {new token}
    end
```

**Key files:** `Server/Middleware/ForcePasswordChangeMiddleware.cs`, `Client/Screens/ChangePasswordScreen.cs`

---

## 3. Create Allocation (Manager)

| | |
|---|---|
| **Trigger** | Manager allocates a bench/team employee to a project via `AllocateResourceScreen` |
| **Endpoint** | `POST /api/allocations` |
| **Auth** | Manager role |
| **Outcome** | Active allocation created; employee resource status recalculated (BENCH / PARTIALLY_ALLOCATED / ALLOCATED) |

```mermaid
sequenceDiagram
    participant Client as ManagerClient
    participant Controller as AllocationController
    participant Service as AllocationService
    participant EmpRepo as EmployeeRepository
    participant UserRepo as UserRepository
    participant ProjRepo as ProjectRepository
    participant AllocRepo as AllocationRepository
    participant StatusSvc as ResourceStatusService
    participant Audit as AuditService
    participant DB as MSSQL

    Client->>Controller: POST /api/allocations {employeeId, projectId, %, dates}
    Controller->>Service: CreateAllocationAsync(managerUserId, request)

    Service->>EmpRepo: GetByIdAsync(employeeId)
    EmpRepo->>DB: SELECT ResourceProfiles
    DB-->>EmpRepo: profile

    alt Employee not on manager team
        Service-->>Controller: ForbiddenAppException
        Controller-->>Client: 403 Forbidden
    else Project not owned by manager
        Service->>ProjRepo: GetByIdAsync(projectId)
        ProjRepo->>DB: SELECT Projects
        Service-->>Controller: ForbiddenAppException
        Controller-->>Client: 403 Forbidden
    else Project not ACTIVE or PLANNED
        Service-->>Controller: ValidationAppException
        Controller-->>Client: 400 Bad Request
    else Dates outside project range
        Service-->>Controller: ValidationAppException
        Controller-->>Client: 400 Bad Request
    else Utilization would exceed 100%
        Service->>AllocRepo: GetActiveByEmployeeIdAsync(employeeId)
        AllocRepo->>DB: SELECT ProjectAllocations
        Service->>Service: ValidateUtilization(overlapping allocations)
        Service-->>Controller: ValidationAppException
        Controller-->>Client: 400 Bad Request
    else All validations pass
        Service->>Service: BeginTransaction
        Service->>AllocRepo: AddAsync(ProjectAllocation ACTIVE)
        AllocRepo->>DB: INSERT ProjectAllocations
        Service->>StatusSvc: ApplyStatusFromActiveAllocationsAsync(profileId)
        StatusSvc->>AllocRepo: GetActiveByEmployeeIdAsync(profileId)
        AllocRepo->>DB: SELECT active allocations
        StatusSvc->>StatusSvc: Sum allocation % → BENCH/PARTIAL/ALLOCATED
        StatusSvc->>EmpRepo: Update ResourceProfile.ResourceStatus
        EmpRepo->>DB: UPDATE ResourceProfiles
        Service->>Audit: LogCreateAsync(allocation)
        Audit->>DB: INSERT AuditLogs
        Service->>Service: CommitTransaction
        Service-->>Controller: CreateAllocationResponseDto
        Controller-->>Client: 200 OK {allocationId, employmentStatus}
    end
```

**Key files:** `Server/Services/Allocations/AllocationService.cs`, `Server/Services/Employees/ResourceStatusService.cs`

---

## 4. End Allocation & Resource Status Reconcile

| | |
|---|---|
| **Trigger** | Manager ends an active allocation via `AllocateResourceScreen` |
| **Endpoint** | `PUT /api/allocations/{id}/end` |
| **Auth** | Manager role |
| **Outcome** | Allocation status set to ENDED; employee resource status recalculated from remaining active allocations |

```mermaid
sequenceDiagram
    participant Client as ManagerClient
    participant Controller as AllocationController
    participant Service as AllocationService
    participant AllocRepo as AllocationRepository
    participant ProjRepo as ProjectRepository
    participant EmpRepo as EmployeeRepository
    participant StatusSvc as ResourceStatusService
    participant Audit as AuditService
    participant DB as MSSQL

    Client->>Controller: PUT /api/allocations/{id}/end
    Controller->>Service: EndAllocationAsync(managerUserId, allocationId)

    Service->>AllocRepo: GetByIdAsync(allocationId)
    AllocRepo->>DB: SELECT ProjectAllocations
    DB-->>AllocRepo: allocation row

    alt Allocation not found or not ACTIVE
        Service-->>Controller: NotFoundAppException or ValidationAppException
        Controller-->>Client: 404 or 400
    else Project not owned by manager
        Service->>ProjRepo: GetByIdAsync(projectId)
        ProjRepo->>DB: SELECT Projects
        Service-->>Controller: ForbiddenAppException
        Controller-->>Client: 403 Forbidden
    else Employee not on manager team
        Service->>EmpRepo: GetByIdAsync(resourceProfileId)
        EmpRepo->>DB: SELECT ResourceProfiles
        Service-->>Controller: ForbiddenAppException
        Controller-->>Client: 403 Forbidden
    else Valid end request
        Service->>Service: BeginTransaction
        Service->>Service: Set EndDate=today, Status=ENDED
        Service->>AllocRepo: UpdateAsync(allocation)
        AllocRepo->>DB: UPDATE ProjectAllocations
        Service->>StatusSvc: ApplyStatusFromActiveAllocationsAsync(profileId)
        StatusSvc->>AllocRepo: GetActiveByEmployeeIdAsync(profileId)
        AllocRepo->>DB: SELECT remaining active allocations
        StatusSvc->>StatusSvc: Recalculate BENCH/PARTIAL/ALLOCATED
        StatusSvc->>EmpRepo: Update ResourceProfile.ResourceStatus
        EmpRepo->>DB: UPDATE ResourceProfiles
        Service->>Audit: LogEndAsync(allocation)
        Audit->>DB: INSERT AuditLogs
        Service->>Service: CommitTransaction
        Service-->>Controller: EndAllocationResponseDto
        Controller-->>Client: 200 OK {allocationId, employmentStatus, endDate}
    end
```

**Key files:** `Server/Services/Allocations/AllocationService.cs`, `Server/Services/Employees/ResourceStatusService.cs`

---

## 5. Submit Timesheet (Employee)

| | |
|---|---|
| **Trigger** | Employee submits weekly hours on `SubmitTimesheetScreen` |
| **Endpoints** | Pre-load: `GET /api/timesheets/week-allocations`, `GET /api/activity-tags` · Submit: `POST /api/timesheets` |
| **Auth** | Employee role |
| **Outcome** | Timesheet saved with status SUBMITTED; line items and activity tags persisted |

```mermaid
sequenceDiagram
    participant Client as EmployeeClient
    participant Controller as TimesheetController
    participant Service as TimesheetService
    participant AllocRepo as AllocationRepository
    participant ConfigRepo as SystemConfigRepository
    participant TagRepo as ActivityTagRepository
    participant TsRepo as TimesheetRepository
    participant Audit as AuditService
    participant DB as MSSQL

    Note over Client,DB: Pre-load (SubmitTimesheetScreen)

    Client->>Controller: GET /api/timesheets/week-allocations?weekStart=
    Controller->>Service: GetWeekAllocationsAsync(employeeId, weekStart)
    Service->>AllocRepo: GetActiveByEmployeeIdForWeekAsync
    AllocRepo->>DB: SELECT ProjectAllocations
    Service-->>Controller: allocations + maxHours per project
    Controller-->>Client: 200 OK

    Client->>Controller: GET /api/activity-tags
    Controller-->>Client: 200 OK (11 reference tags)

    Note over Client,DB: Submit timesheet

    Client->>Controller: POST /api/timesheets {weekStartDate, lineItems[], remarks}
    Controller->>Service: SubmitTimesheetAsync(employeeId, actorUserId, request)

    alt Future week
        Service-->>Controller: ValidationAppException
        Controller-->>Client: 400 Bad Request
    else Duplicate week
        Service->>TsRepo: ExistsForWeekAsync(employeeId, weekStart)
        TsRepo->>DB: SELECT Timesheets
        Service-->>Controller: ConflictAppException
        Controller-->>Client: 409 Conflict
    else No active allocations for week
        Service->>AllocRepo: GetActiveByEmployeeIdForWeekAsync
        AllocRepo->>DB: SELECT ProjectAllocations
        Service-->>Controller: ValidationAppException
        Controller-->>Client: 400 Bad Request
    else Hours exceed limits
        Service->>ConfigRepo: GetByKeyAsync(max_weekly_hours)
        ConfigRepo->>DB: SELECT SystemConfigurations
        Service->>Service: Validate totalHours and per-project caps
        Service-->>Controller: ValidationAppException
        Controller-->>Client: 400 Bad Request
    else Valid submission
        Service->>Service: BeginTransaction
        Service->>TsRepo: AddAsync(Timesheet SUBMITTED)
        TsRepo->>DB: INSERT Timesheets
        loop Each line item
            Service->>TsRepo: AddLineItemAsync(projectId, hoursLogged)
            TsRepo->>DB: INSERT TimesheetLineItems
            Service->>TagRepo: GetByIdsAsync(activityTagIds)
            TagRepo->>DB: SELECT ActivityTags
            Service->>TsRepo: AddLineItemTagAsync
            TsRepo->>DB: INSERT TimesheetLineItemActivityTags
        end
        Service->>Audit: LogCreateAsync(timesheet)
        Audit->>DB: INSERT AuditLogs
        Service->>Service: CommitTransaction
        Service-->>Controller: TimesheetSubmitResponseDto
        Controller-->>Client: 200 OK {timesheetId, status=SUBMITTED, totalHours}
    end
```

**Key files:** `Server/Services/Timesheets/TimesheetService.cs`, `Client/Screens/Employee/SubmitTimesheetScreen.cs`

---

## 6. Background Scheduler Tick

| | |
|---|---|
| **Trigger** | Server starts (`dotnet run --project Server`); repeats every `scheduler_interval_hours` (default 4) |
| **Endpoint** | None — internal `IHostedService` |
| **Auth** | N/A (server process) |
| **Outcome** | Project health updated, missed timesheets created, resource statuses reconciled, run logged to `SCHEDULER_JOB_LOGS` |

```mermaid
sequenceDiagram
    participant Host as ASPNETCoreHost
    participant Scheduler as BackgroundScheduler
    participant ProjSvc as ProjectService
    participant TsSvc as TimesheetService
    participant StatusSvc as ResourceStatusService
    participant Evaluator as ProjectHealthFlagEvaluator
    participant Threshold as HealthThresholdProvider
    participant ProjRepo as ProjectRepository
    participant TsRepo as TimesheetRepository
    participant EmpRepo as EmployeeRepository
    participant JobLog as SchedulerJobLogRepository
    participant ConfigRepo as SystemConfigRepository
    participant DB as MSSQL

    Host->>Scheduler: ExecuteAsync (on startup)
    Scheduler->>Scheduler: CreateScope

    Note over Scheduler,DB: Phase 1 — Project health evaluation

    Scheduler->>ProjSvc: EvaluateAllProjectsHealthAsync
    ProjSvc->>ProjRepo: GetActiveAsync
    ProjRepo->>DB: SELECT Projects WHERE is_active
    ProjSvc->>Threshold: GetThresholdsAsync
    Threshold->>ConfigRepo: health_low_hours_threshold (0.6), health_approaching_deadline_days (28)
    ConfigRepo->>DB: SELECT SystemConfigurations
    loop Each active project
        ProjSvc->>ProjSvc: Compute expectedHours vs loggedHours
        ProjSvc->>Evaluator: EvaluateFlags(milestones, hours, thresholds)
        Evaluator-->>ProjSvc: flags OVERDUE_MILESTONE / LOW_HOURS / APPROACHING_DEADLINE
        ProjSvc->>Evaluator: MapToHealthStatus(flags)
        Evaluator-->>ProjSvc: GREEN / AMBER / RED
        ProjSvc->>ProjRepo: UpdateHealthStatusAsync
        ProjRepo->>DB: UPDATE Projects
    end
    ProjSvc-->>Scheduler: SchedulerHealthResultDto

    Note over Scheduler,DB: Phase 2 — Missed timesheets

    Scheduler->>TsSvc: MarkMissedTimesheetsAsync
    TsSvc->>EmpRepo: GetAllActiveEmployees
    EmpRepo->>DB: SELECT active employees
    loop Each employee without last-week timesheet
        TsSvc->>TsRepo: AddAsync(Timesheet MISSED)
        TsRepo->>DB: INSERT Timesheets
    end
    TsSvc-->>Scheduler: missedCreated count

    Note over Scheduler,DB: Phase 3 — Resource status reconciliation

    Scheduler->>StatusSvc: ReconcileAllResourceStatusesAsync
    StatusSvc->>EmpRepo: GetAll profiles
    EmpRepo->>DB: SELECT ResourceProfiles
    loop Each profile
        StatusSvc->>StatusSvc: ApplyStatusFromActiveAllocationsAsync
        StatusSvc->>EmpRepo: Update ResourceStatus
        EmpRepo->>DB: UPDATE ResourceProfiles
    end
    StatusSvc-->>Scheduler: statusesUpdated count

    Scheduler->>JobLog: LogAsync(BackgroundScheduler, SUCCESS)
    JobLog->>DB: INSERT SCHEDULER_JOB_LOGS
    Scheduler->>ConfigRepo: GetSchedulerIntervalHours (default 4)
    ConfigRepo->>DB: SELECT scheduler_interval_hours
    Scheduler->>Scheduler: Task.Delay(intervalHours)

    alt Run failed
        Scheduler->>JobLog: LogAsync(BackgroundScheduler, FAILED, error)
        JobLog->>DB: INSERT SCHEDULER_JOB_LOGS
        Scheduler->>Scheduler: Task.Delay(intervalHours)
    end
```

**Key files:** `Server/Scheduler/BackgroundScheduler.cs`, `Server/Services/Projects/ProjectHealthFlagEvaluator.cs`, `Server/Common/HealthThresholdDefaults.cs`

**Health threshold defaults:**

| Config key | Default | Purpose |
|------------|---------|---------|
| `health_low_hours_threshold` | `0.6` | Flag LOW_HOURS when logged &lt; 60% of expected |
| `health_approaching_deadline_days` | `28` | Flag APPROACHING_DEADLINE when end date within 28 days |

---

## Appendix — Other Implemented Endpoints (Not Diagrammed)

These flows follow the same layered pattern (`Controller → Service → Repository → DB`) but are simple CRUD or read operations.

| Module | Method | Path | Auth | Status |
|--------|--------|------|------|--------|
| **Users** | POST | `/api/users` | Admin | Create user + optional employee profile |
| **Users** | GET | `/api/users` | Admin | List all users |
| **Users** | PUT | `/api/users/{id}` | Admin | Update name/email |
| **Users** | PUT | `/api/users/{id}/roles` | Admin | Replace user role |
| **Users** | PUT | `/api/users/{id}/reset-password` | Admin | Reset temp password |
| **Users** | PUT | `/api/users/{id}/deactivate` | Admin | Deactivate + end allocations |
| **Users** | PUT | `/api/users/{id}/reactivate` | Admin | Reactivate user |
| **Employees** | GET | `/api/employees` | Admin | List employees |
| **Employees** | GET | `/api/employees/{id}` | Admin | Employee detail |
| **Employees** | PUT | `/api/employees/{id}` | Admin | Update employee |
| **Employees** | PUT | `/api/employees/{id}/deactivate` | Admin | Deactivate employee |
| **Employees** | PUT | `/api/employees/{id}/manager` | Admin | Assign manager |
| **Employees** | POST/PUT/DELETE | `/api/employees/{id}/skills[...]` | Admin | Skill CRUD |
| **Employees** | GET | `/api/employees/my-team` | Manager | Team dashboard |
| **Employees** | GET | `/api/employees/my-team/{id}` | Manager | Team member detail |
| **Projects** | POST | `/api/projects` | Admin | Create project |
| **Projects** | GET | `/api/projects` | Admin | List projects (with healthStatus) |
| **Projects** | GET | `/api/projects/{id}` | Admin | Project detail |
| **Projects** | PUT | `/api/projects/{id}` | Admin | Update project |
| **Projects** | PUT | `/api/projects/{id}/archive` | Admin | Archive project |
| **Projects** | GET/POST/PUT | `/api/projects/{id}/milestones[...]` | Admin | Milestone CRUD |
| **Projects** | GET | `/api/projects/my` | Manager | Manager project list |
| **Projects** | GET | `/api/projects/{id}/manager` | Manager | Manager project detail + health flags |
| **Allocations** | PUT | `/api/allocations/{id}` | Manager | Update allocation %/dates |
| **Allocations** | GET | `/api/allocations` | Admin | List allocations |
| **Allocations** | GET | `/api/allocations/my` | Employee | My allocations |
| **Timesheets** | GET | `/api/timesheets/my` | Employee | Timesheet history |
| **Timesheets** | GET | `/api/timesheets/my/{id}` | Employee | Timesheet detail |
| **Timesheets** | GET | `/api/timesheets/reminder` | Employee | Missed timesheet reminder |
| **Timesheets** | GET | `/api/timesheets/team` | Manager | Team timesheets by week |
| **Timesheets** | GET | `/api/timesheets/{id}` | Manager | Timesheet detail |
| **System Config** | GET | `/api/system-config` | Admin | Read all config (API key masked) |
| **System Config** | PUT | `/api/system-config` | Admin | Partial config update |
| **AI** | GET | `/api/ai/projects/{id}/risk-summary` | Manager | **Phase 8 — NotImplementedException** |
| **AI** | GET | `/api/ai/projects/{id}/skill-match` | Manager | **Phase 8 — NotImplementedException** |

---

## Out of Scope

- **AI integration** — endpoints and LLM clients exist but throw `NotImplementedException` (Phase 8)
- **Console menu navigation** — client-side routing only, no server interaction
- **Simple GET flows** — listed in appendix; same request/response pattern as diagrammed flows
