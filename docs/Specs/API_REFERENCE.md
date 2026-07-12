# PRM Platform — API Reference

Base URL (local): `http://localhost:5000`

Interactive docs: `http://localhost:5000/swagger` (Development only)

---

## Authentication

All protected endpoints require a JWT Bearer token:

```
Authorization: Bearer <token>
```

Obtain a token via `POST /api/auth/login`. Token expiry is configured in `JwtSettings:ExpiryHours` (default: 8 hours).

Employee-scoped endpoints also require an `employee_id` claim in the token (set automatically for Employee role accounts).

---

## Response Envelope

Every API returns `ApiResponse<T>`:

```json
{
  "success": true,
  "data": { },
  "message": "Operation completed.",
  "error": null,
  "errorCode": null,
  "details": null
}
```

Error example:

```json
{
  "success": false,
  "data": null,
  "message": null,
  "error": "Validation failed.",
  "errorCode": "VALIDATION_FAILED",
  "details": ["Username is required."]
}
```

| HTTP Status | When |
|-------------|------|
| `200 OK` | Successful read or update |
| `201 Created` | Successful create |
| `400 Bad Request` | Validation failure |
| `401 Unauthorized` | Missing, invalid, or expired token |
| `403 Forbidden` | Wrong role or insufficient permission |
| `404 Not Found` | Entity not found |
| `409 Conflict` | Business rule conflict (e.g. over-allocation) |
| `500 Internal Server Error` | Unexpected server error |

---

## Reference Values

| Domain | Allowed Values |
|--------|----------------|
| **Roles** | `ADMIN`, `MANAGER`, `EMPLOYEE` |
| **Resource status** | `BENCH`, `PARTIALLY_ALLOCATED`, `ALLOCATED` |
| **Allocation status** | `ACTIVE`, `ENDED` |
| **Project status** | `PLANNED`, `ACTIVE`, `ON_HOLD`, `COMPLETED` |
| **Milestone status** | `NOT_STARTED`, `IN_PROGRESS`, `DONE` |
| **Health status** | `GREEN`, `AMBER`, `RED` |
| **Timesheet status** | `SUBMITTED`, `MISSED` |
| **Skill proficiency** | `BEGINNER`, `INTERMEDIATE`, `ADVANCED` |
| **Skill category** | `BACKEND`, `FRONTEND`, `DEVOPS`, `QA`, `OTHER` |
| **LLM provider** | `GEMINI`, `GROQ`, `GEMMA` |

---

## Endpoint Index

| # | Method | Endpoint | Auth |
|---|--------|----------|------|
| | **Health** | | |
| 1 | GET | `/health` | Anonymous |
| | **Auth** | | |
| 2 | POST | `/api/auth/login` | Anonymous |
| 3 | POST | `/api/auth/change-password` | JWT |
| | **Users** | | |
| 4 | POST | `/api/users` | Admin |
| 5 | GET | `/api/users` | Admin |
| 6 | PUT | `/api/users/{id}` | Admin |
| 7 | PUT | `/api/users/{id}/roles` | Admin |
| 8 | PUT | `/api/users/{id}/reset-password` | Admin |
| 9 | PUT | `/api/users/{id}/deactivate` | Admin |
| 10 | PUT | `/api/users/{id}/reactivate` | Admin |
| | **Employees** | | |
| 11 | GET | `/api/employees` | Admin |
| 12 | GET | `/api/employees/{id}` | Admin |
| 13 | PUT | `/api/employees/{id}` | Admin |
| 14 | PUT | `/api/employees/{id}/deactivate` | Admin |
| 15 | PUT | `/api/employees/{id}/manager` | Admin |
| 16 | POST | `/api/employees/{id}/skills` | Admin |
| 17 | PUT | `/api/employees/{id}/skills/{skillId}` | Admin |
| 18 | DELETE | `/api/employees/{id}/skills/{skillId}` | Admin |
| 19 | GET | `/api/employees/my-team` | Manager |
| 20 | GET | `/api/employees/my-team/{id}` | Manager |
| 21 | PUT | `/api/employees/my-team/{id}/restore-timesheet-access` | Manager |
| | **Projects** | | |
| 22 | POST | `/api/projects` | Admin |
| 23 | GET | `/api/projects` | Admin |
| 24 | GET | `/api/projects/{id}` | Admin |
| 25 | PUT | `/api/projects/{id}` | Admin |
| 26 | PUT | `/api/projects/{id}/archive` | Admin |
| 27 | GET | `/api/projects/{id}/milestones` | Admin |
| 28 | POST | `/api/projects/{id}/milestones` | Admin |
| 29 | PUT | `/api/projects/{id}/milestones/{milestoneId}` | Admin |
| 30 | PUT | `/api/projects/{id}/milestones/{milestoneId}/status` | Admin |
| 31 | GET | `/api/projects/my` | Manager |
| 32 | GET | `/api/projects/{id}/manager` | Manager |
| | **Allocations** | | |
| 33 | POST | `/api/allocations` | Manager |
| 34 | PUT | `/api/allocations/{id}` | Manager |
| 35 | PUT | `/api/allocations/{id}/end` | Manager |
| 36 | GET | `/api/allocations` | Admin |
| 37 | GET | `/api/allocations/my` | Employee |
| | **Timesheets** | | |
| 38 | POST | `/api/timesheets` | Employee |
| 39 | GET | `/api/timesheets/my` | Employee |
| 40 | GET | `/api/timesheets/my/{id}` | Employee |
| 41 | GET | `/api/timesheets/week-allocations` | Employee |
| 42 | GET | `/api/timesheets/reminder` | Employee |
| 43 | GET | `/api/timesheets/team` | Manager |
| 44 | GET | `/api/timesheets/{id}` | Manager |
| | **Activity Tags** | | |
| 45 | GET | `/api/activity-tags` | Employee |
| | **System Config** | | |
| 46 | GET | `/api/system-config` | Admin |
| 47 | PUT | `/api/system-config` | Admin |
| | **AI Insights** | | |
| 48 | GET | `/api/ai/skill-match` | Manager |
| 49 | GET | `/api/ai/projects/{projectId}/skill-match` | Manager |
| 50 | GET | `/api/ai/projects/{projectId}/risk-summary` | Manager |
| 51 | GET | `/api/ai/team-builder` | Manager |
| | **Audit Logs** | | |
| 52 | GET | `/api/audit-logs` | Admin |
| | **Roles & Permissions** | | |
| 53 | GET | `/api/roles` | Admin |
| 54 | GET | `/api/roles/{roleName}/permissions` | Admin |

---

## Health

### GET `/health`

No authentication required.

**Response `200`**

```json
{
  "status": "healthy",
  "service": "PRM.Server",
  "phase": "7"
}
```

---

## Auth

### POST `/api/auth/login`

**Auth:** Anonymous

**Request body**

```json
{
  "username": "admin",
  "password": "Admin@1234"
}
```

**Response `200`**

```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "expiresAt": "2026-06-21T18:00:00Z",
    "userId": 1,
    "employeeId": null,
    "managerId": null,
    "role": "ADMIN",
    "fullName": "System Administrator",
    "forcePasswordChange": true
  },
  "message": "Login successful."
}
```

---

### POST `/api/auth/change-password`

**Auth:** JWT (any role)

**Request body**

```json
{
  "currentPassword": "Admin@1234",
  "newPassword": "NewSecure@Pass1"
}
```

**Response `200`** — Returns a new `LoginResponseDto` with a refreshed token and `forcePasswordChange: false`.

---

## Users

All endpoints require **Admin** role.

### POST `/api/users`

Create a user account. User must change password on first login.

**Request body**

```json
{
  "fullName": "Jane Manager",
  "email": "jane@techserve.com",
  "username": "jmanager",
  "temporaryPassword": "Temp@1234",
  "role": "MANAGER",
  "department": "DELIVERY",
  "designation": "DELIVERY_MANAGER"
}
```

**Response `201`** — `CreateUserResponseDto` with `userId`, `username`, `role`.

---

### GET `/api/users`

List all user accounts.

**Response `200`** — `UserListResponseDto` with `users[]`, `total`, `activeCount`, `inactiveCount`.

---

### PUT `/api/users/{id}`

Update user full name and email.

**Request body**

```json
{
  "fullName": "Jane Manager Updated",
  "email": "jane.updated@techserve.com"
}
```

**Response `200`**

---

### PUT `/api/users/{id}/roles`

Replace the user's role (single-role model).

**Request body**

```json
{
  "role": "EMPLOYEE"
}
```

**Response `200`**

---

### PUT `/api/users/{id}/reset-password`

Reset password and force change on next login.

**Request body**

```json
{
  "newTemporaryPassword": "Reset@1234"
}
```

**Response `200`**

---

### PUT `/api/users/{id}/deactivate`

Deactivate a user account.

**Response `200`**

---

### PUT `/api/users/{id}/reactivate`

Reactivate a user account. Previous allocations are **not** restored.

**Response `200`**

---

## Employees

### GET `/api/employees`

**Auth:** Admin

**Query parameters**

| Param | Type | Description |
|-------|------|-------------|
| `status` | string | Filter by resource status (`BENCH`, `PARTIALLY_ALLOCATED`, `ALLOCATED`) |
| `department` | string | Filter by department |

**Response `200`** — `EmployeeListResponseDto` with `employees[]`, `total`, `allocatedCount`, `benchCount`.

---

### GET `/api/employees/{id}`

**Auth:** Admin

**Response `200`** — `EmployeeDetailDto` (profile, skills, allocations, manager info).

---

### PUT `/api/employees/{id}`

**Auth:** Admin

**Request body**

```json
{
  "department": "DELIVERY",
  "designation": "SENIOR_DEVELOPER"
}
```

**Response `200`**

---

### PUT `/api/employees/{id}/deactivate`

**Auth:** Admin — Deactivates employee and ends active allocations.

**Response `200`**

---

### PUT `/api/employees/{id}/manager`

**Auth:** Admin

**Request body**

```json
{
  "managerUserId": 5
}
```

**Response `200`**

---

### POST `/api/employees/{id}/skills`

**Auth:** Admin

**Request body**

```json
{
  "skillName": "Java",
  "category": "BACKEND",
  "proficiencyLevel": "ADVANCED"
}
```

**Response `200`**

---

### PUT `/api/employees/{id}/skills/{skillId}`

**Auth:** Admin

**Request body**

```json
{
  "proficiencyLevel": "INTERMEDIATE"
}
```

**Response `200`**

---

### DELETE `/api/employees/{id}/skills/{skillId}`

**Auth:** Admin

**Response `200`**

---

### GET `/api/employees/my-team`

**Auth:** Manager — Team dashboard for the logged-in manager.

**Response `200`** — `TeamDashboardDto` with team members, utilization, and status summary.

---

### GET `/api/employees/my-team/{id}`

**Auth:** Manager — Detail for a team member on the manager's team.

**Response `200`** — `TeamMemberDetailDto`

---

### PUT `/api/employees/my-team/{id}/restore-timesheet-access`

**Auth:** Manager — Restores timesheet submission access for a team member.

**Response `200`**

---

## Projects

### POST `/api/projects`

**Auth:** Admin

**Request body**

```json
{
  "projectName": "Alpha Portal",
  "description": "Customer portal redesign",
  "startDate": "2026-01-06",
  "endDate": "2026-06-30",
  "projectStatus": "ACTIVE",
  "managerUserId": 5,
  "totalStoryPoints": 120
}
```

**Response `201`** — `CreateProjectResponseDto` with `projectId`.

---

### GET `/api/projects`

**Auth:** Admin — All projects with `healthStatus` on list items.

**Response `200`** — `ProjectListResponseDto`

---

### GET `/api/projects/{id}`

**Auth:** Admin

**Response `200`** — `ProjectDetailDto` with milestones and allocations.

---

### PUT `/api/projects/{id}`

**Auth:** Admin

**Request body** — Same shape as create (`UpdateProjectRequestDto`).

**Response `200`**

---

### PUT `/api/projects/{id}/archive`

**Auth:** Admin — Sets status to `COMPLETED` and `is_active = false`.

**Response `200`**

---

### GET `/api/projects/{id}/milestones`

**Auth:** Admin

**Response `200`** — `MilestoneListResponseDto`

---

### POST `/api/projects/{id}/milestones`

**Auth:** Admin

**Request body**

```json
{
  "milestoneTitle": "Phase 1 — Auth Module",
  "dueDate": "2026-03-15",
  "storyPoints": 30,
  "sortOrder": 1
}
```

**Response `201`**

---

### PUT `/api/projects/{id}/milestones/{milestoneId}`

### PUT `/api/projects/{id}/milestones/{milestoneId}/status`

**Auth:** Admin — Both routes update milestone status.

**Request body**

```json
{
  "milestoneStatus": "IN_PROGRESS"
}
```

**Response `200`**

---

### GET `/api/projects/my`

**Auth:** Manager — Projects managed by the logged-in manager.

**Response `200`** — `ManagerProjectListResponseDto` with health status per project.

---

### GET `/api/projects/{id}/manager`

**Auth:** Manager — Project detail scoped to the manager's own project, including health evaluation.

**Response `200`** — `ManagerProjectDetailDto`

---

## Allocations

### POST `/api/allocations`

**Auth:** Manager

**Request body**

```json
{
  "employeeId": 12,
  "projectId": 3,
  "allocationPercentage": 80,
  "allocationStartDate": "2026-06-01",
  "allocationEndDate": "2026-09-30"
}
```

**Response `201`** — `CreateAllocationResponseDto`

```json
{
  "allocationId": 45,
  "employeeId": 12,
  "projectId": 3,
  "allocationPercentage": 80,
  "allocationStatus": "ACTIVE",
  "employmentStatus": "PARTIALLY_ALLOCATED"
}
```

Validates total utilization against `max_weekly_hours` system config. Returns `409 Conflict` on over-allocation.

---

### PUT `/api/allocations/{id}`

**Auth:** Manager — Update percentage and/or dates.

**Request body** (all fields optional)

```json
{
  "allocationPercentage": 100,
  "allocationStartDate": "2026-06-01",
  "allocationEndDate": "2026-12-31"
}
```

**Response `200`** — `UpdateAllocationResponseDto`

---

### PUT `/api/allocations/{id}/end`

**Auth:** Manager — End an active allocation and reconcile resource status.

**Response `200`** — `EndAllocationResponseDto`

---

### GET `/api/allocations`

**Auth:** Admin — Company-wide allocation matrix.

**Query parameters**

| Param | Type | Description |
|-------|------|-------------|
| `employeeId` | long | Filter by employee |
| `projectId` | long | Filter by project |
| `status` | string | `ACTIVE` or `ENDED` |

**Response `200`** — `AllocationListResponseDto`

---

### GET `/api/allocations/my`

**Auth:** Employee — Own allocation history.

**Response `200`** — `EmployeeAllocationListResponseDto`

---

## Timesheets

### POST `/api/timesheets`

**Auth:** Employee

**Request body**

```json
{
  "weekStartDate": "2026-06-16",
  "lineItems": [
    {
      "projectId": 3,
      "hoursLogged": 20,
      "activityTagIds": [1, 5],
      "customTagText": null
    },
    {
      "projectId": 7,
      "hoursLogged": 20,
      "activityTagIds": [2],
      "customTagText": "WebSocket integration"
    }
  ],
  "remarks": "Normal week"
}
```

**Response `201`** — `TimesheetSubmitResponseDto`

---

### GET `/api/timesheets/my`

**Auth:** Employee — Timesheet history (`SUBMITTED` and `MISSED` weeks).

**Response `200`** — `TimesheetHistoryItemDto[]`

---

### GET `/api/timesheets/my/{id}`

**Auth:** Employee — Detail for own timesheet.

**Response `200`** — `TimesheetDetailDto` with line items and activity tags.

---

### GET `/api/timesheets/week-allocations`

**Auth:** Employee

**Query parameters**

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `weekStart` | DateOnly (`YYYY-MM-DD`) | Yes | Monday of the target week |

**Response `200`** — `EmployeeWeekAllocationDto[]` (projects available for that week's timesheet).

---

### GET `/api/timesheets/reminder`

**Auth:** Employee — Missed timesheet reminder status.

**Response `200`** — `TimesheetReminderResponseDto`

---

### GET `/api/timesheets/team`

**Auth:** Manager — Team timesheet status for a week.

**Query parameters**

| Param | Type | Description |
|-------|------|-------------|
| `week` | DateOnly | Week start date (defaults to current week) |

**Response `200`** — `TeamTimesheetListResponseDto`

---

### GET `/api/timesheets/{id}`

**Auth:** Manager — Read-only detail for a team member's submitted timesheet.

**Response `200`** — `ManagerTimesheetDetailDto`

---

## Activity Tags

### GET `/api/activity-tags`

**Auth:** Employee — List all activity tags for timesheet submission.

**Response `200`** — `ActivityTagDto[]`

```json
{
  "success": true,
  "data": [
    { "id": 1, "tagName": "Microservices", "tagCode": "MICROSERVICES" },
    { "id": 2, "tagName": "Database Design", "tagCode": "DB_DESIGN" }
  ],
  "message": "Activity tags retrieved."
}
```

---

## System Config

All endpoints require **Admin** role.

### GET `/api/system-config`

**Response `200`**

```json
{
  "success": true,
  "data": {
    "llmProvider": "GEMINI",
    "llmApiKeyMasked": "****************************",
    "schedulerIntervalHours": 4,
    "maxWeeklyHours": 40,
    "timesheetDeadlineWorkingDaysAfterWeekEnd": 3
  },
  "message": "Configuration retrieved."
}
```

---

### PUT `/api/system-config`

At least one field must be provided.

**Request body** (all fields optional)

```json
{
  "llmProvider": "GROQ",
  "llmApiKey": "gsk_xxxxxxxx",
  "schedulerIntervalHours": 4,
  "maxWeeklyHours": 40,
  "timesheetDeadlineWorkingDaysAfterWeekEnd": 3
}
```

**Response `200`**

Notes:
- API key is encrypted at rest.
- Changing LLM provider requires a new API key (except `GEMMA`).
- Scheduler interval takes effect on the next scheduler cycle.

---

## AI Insights

All endpoints require **Manager** role and a configured LLM provider/API key.

### GET `/api/ai/skill-match`

Organization-wide skill match (not scoped to a project).

**Query parameters**

| Param | Type | Required |
|-------|------|----------|
| `requirement` | string | Yes — natural language requirement |

**Response `200`** — `AiSkillMatchResponseDto`

```json
{
  "success": true,
  "data": {
    "projectId": 0,
    "matches": [
      {
        "employeeName": "John Doe",
        "skillName": "Java",
        "matchScore": 92,
        "reason": "Advanced Java with recent Microservices activity tags",
        "remainingCapacityPercentage": 20
      }
    ]
  },
  "message": "Skill match generated."
}
```

---

### GET `/api/ai/projects/{projectId}/skill-match`

Project-scoped skill match.

**Query parameters**

| Param | Type | Required |
|-------|------|----------|
| `requirement` | string | Yes |

**Response `200`** — `AiSkillMatchResponseDto`

---

### GET `/api/ai/projects/{projectId}/risk-summary`

Plain-English project health risk summary.

**Response `200`** — `AiRiskSummaryResponseDto`

```json
{
  "success": true,
  "data": {
    "projectId": 3,
    "summary": "Project Alpha is at moderate risk due to...",
    "recommendations": [
      "Review milestone 2 deadline",
      "Consider additional backend allocation"
    ]
  },
  "message": "Risk summary generated."
}
```

---

### GET `/api/ai/team-builder`

Build a multi-role team recommendation from a natural language requirement.

**Query parameters**

| Param | Type | Required |
|-------|------|----------|
| `requirement` | string | Yes |

**Response `200`** — `TeamBuilderResponseDto` with `roles[]` (each containing skill requirements, matched employees, and gaps).

---

## Audit Logs

### GET `/api/audit-logs`

**Auth:** Admin

**Query parameters**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `from` | DateTime | — | Start of date range (UTC) |
| `to` | DateTime | — | End of date range (UTC) |
| `actorUserId` | long | — | Filter by user who performed the action |
| `entityName` | string | — | e.g. `Users`, `Projects` |
| `actionType` | string | — | e.g. `CREATE`, `UPDATE`, `DELETE` |
| `search` | string | — | Free-text search |
| `page` | int | `1` | Page number |
| `pageSize` | int | `20` | Items per page |

**Response `200`** — `AuditLogListResponseDto` with paginated entries.

---

## Roles & Permissions

### GET `/api/roles`

**Auth:** Admin — List all roles.

**Response `200`** — `RoleListResponseDto`

---

### GET `/api/roles/{roleName}/permissions`

**Auth:** Admin — Capabilities assigned to a role.

**Path parameter:** `roleName` — `ADMIN`, `MANAGER`, or `EMPLOYEE`

**Response `200`** — `RolePermissionsResponseDto`

---

## Error Codes

| Code | Description |
|------|-------------|
| `AUTH_INVALID_CREDENTIALS` | Wrong username or password |
| `AUTH_SESSION_EXPIRED` | Token expired |
| `USER_NOT_FOUND` | User does not exist |
| `EMPLOYEE_NOT_FOUND` | Employee does not exist |
| `EMPLOYEE_INACTIVE` | Employee is deactivated |
| `EMPLOYEE_NOT_ON_TEAM` | Employee not on manager's team |
| `PROJECT_NOT_FOUND` | Project does not exist |
| `MILESTONE_NOT_FOUND` | Milestone does not exist |
| `ALLOCATION_NOT_FOUND` | Allocation does not exist |
| `VALIDATION_FAILED` | Request validation failed |
| `FORBIDDEN` | Action not permitted for role |
| `CONFLICT` | Business rule conflict |
| `NOT_FOUND` | Generic not found |
| `LLM_NOT_CONFIGURED` | LLM provider or API key missing |
| `LLM_REQUEST_FAILED` | LLM provider call failed |
| `LLM_RESPONSE_INVALID` | LLM returned unparseable response |
| `UNEXPECTED_ERROR` | Unhandled server error |

---

*Generated from `Server/Controllers/*` — 54 REST endpoints + 1 health endpoint.*
