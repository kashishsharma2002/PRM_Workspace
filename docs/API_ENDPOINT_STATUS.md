# API Endpoint Status — Milestone 2

| Module | Endpoint | Auth | Status | Changes Required |
|--------|----------|------|--------|------------------|
| Auth | POST `/api/auth/login` | Anonymous | Complete | — |
| Auth | POST `/api/auth/change-password` | JWT | Complete | — |
| Users | POST `/api/users` | Admin | Complete | — |
| Users | GET `/api/users` | Admin | Complete | — |
| Users | PUT `/api/users/{id}` | Admin | **New** | Update full name and email |
| Users | PUT `/api/users/{id}/roles` | Admin | **New** | Replace user role (single role model) |
| Users | PUT `/api/users/{id}/reset-password` | Admin | Complete | Sets `is_temporary_password` (force change) |
| Users | PUT `/api/users/{id}/deactivate` | Admin | Complete | — |
| Users | PUT `/api/users/{id}/reactivate` | Admin | Complete | — |
| Employees | GET `/api/employees` | Admin | Complete | — |
| Employees | GET `/api/employees/{id}` | Admin | Complete | — |
| Employees | PUT `/api/employees/{id}` | Admin | Complete | — |
| Employees | PUT `/api/employees/{id}/deactivate` | Admin | Complete | — |
| Employees | PUT `/api/employees/{id}/manager` | Admin | Complete | — |
| Employees | POST/PUT/DELETE skills | Admin | Complete | — |
| Employees | GET `/api/employees/my-team` | Manager | Complete | — |
| Employees | GET `/api/employees/my-team/{id}` | Manager | Complete | — |
| Projects | POST `/api/projects` | Admin | Complete | — |
| Projects | GET `/api/projects` | Admin | **Enhanced** | Returns `healthStatus` on list items |
| Projects | GET `/api/projects/{id}` | Admin | Complete | — |
| Projects | PUT `/api/projects/{id}` | Admin | Complete | — |
| Projects | PUT `/api/projects/{id}/archive` | Admin | **New** | Sets COMPLETED + `is_active=false` |
| Projects | Milestone CRUD | Admin | Complete | — |
| Projects | GET `/api/projects/my` | Manager | Complete | — |
| Projects | GET `/api/projects/{id}/manager` | Manager | Complete | Uses configurable health thresholds |
| Allocations | POST `/api/allocations` | Manager | **Enhanced** | Status: BENCH / PARTIALLY_ALLOCATED / ALLOCATED |
| Allocations | PUT `/api/allocations/{id}` | Manager | **New** | Update % or dates with utilization validation |
| Allocations | PUT `/api/allocations/{id}/end` | Manager | **Enhanced** | Reconciles resource status after end |
| Allocations | GET `/api/allocations` | Admin | Complete | — |
| Allocations | GET `/api/allocations/my` | Employee | Complete | — |
| Timesheets | Employee + Manager endpoints | Mixed | Complete | — |
| System Config | GET/PUT `/api/system-config` | Admin | **Enhanced** | Health threshold settings added |
| AI | GET `/api/ai/projects/{id}/*` | Manager | Deferred (AI) | Phase 8 — LLM not in M2 scope |
| Scheduler | Background job | Internal | **Enhanced** | + resource status reconciliation phase |

## Final Checklist

- [x] Schema alignment verified (`USER_SKILLS`, `manager_id` → `USERS`, `completed_at`)
- [x] Non-AI console screens functional (health + config UI updates)
- [x] Scheduler: health evaluation + missed timesheets + status reconciliation
- [x] Health thresholds configurable via system config
- [x] JWT + role authorization on new endpoints
- [x] Swagger OpenAPI + JWT bearer configured (no XML doc comments)
- [x] Unit tests for changed areas (70 passing; no integration tests)
- [x] No magic strings/numbers in touched code — constants in `Server/Common/*`
- [x] AI endpoints explicitly deferred

## Breaking Changes

None. All M2 endpoints are additive. Allocation status may now return `PARTIALLY_ALLOCATED` where previously only `BENCH`/`ALLOCATED` were used.
