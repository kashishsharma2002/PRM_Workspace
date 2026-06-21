erDiagram



    USERS {

        BIGINT id PK

        VARCHAR username UK

        VARCHAR email UK

        VARCHAR full_name

        VARCHAR password_hash

        VARCHAR department "enum:DepartmentConstants"

        VARCHAR designation "enum:DesignationConstants"

        BOOLEAN is_active

        BOOLEAN is_temporary_password

        DATETIME last_login_at

        DATE joined_at

        DATETIME created_at

        DATETIME updated_at

    }



    RESOURCE_PROFILES {

        BIGINT id PK

        BIGINT user_id FK UK

        BIGINT manager_id FK

        VARCHAR resource_status

        DATETIME created_at

        DATETIME updated_at

    }



    ROLES {

        BIGINT id PK

        VARCHAR role_name UK

        DATETIME created_at

    }



    PERMISSIONS {

        BIGINT id PK

        VARCHAR resource "enum:PermissionResource"

        VARCHAR action "enum:PermissionAction"

        VARCHAR description

    }



    ROLE_PERMISSIONS {

        BIGINT role_id PK_FK

        BIGINT permission_id PK_FK

    }



    USER_ROLES {

        BIGINT user_id PK_FK

        BIGINT role_id PK_FK

        BIGINT assigned_by_user_id FK

        DATETIME assigned_at

    }



    SKILLS {

        BIGINT id PK

        VARCHAR skill_name UK

        VARCHAR category "enum:SkillCategoryConstants"

        BOOLEAN is_active

        DATETIME created_at

    }



    USER_SKILLS {

        BIGINT user_id PK_FK

        BIGINT skill_id PK_FK

        VARCHAR proficiency_level "enum:ProficiencyConstants"

        DATETIME created_at

    }



    PROJECTS {

        BIGINT id PK

        VARCHAR project_code UK

        VARCHAR project_name

        TEXT description

        DATE start_date

        DATE end_date

        VARCHAR project_status "enum:ProjectStatusConstants"

        VARCHAR health_status "enum:HealthStatusConstants"

        INT total_story_points

        BIGINT manager_user_id FK

        BOOLEAN is_active

        DATETIME created_at

        DATETIME updated_at

    }



    PROJECT_MILESTONES {

        BIGINT id PK

        BIGINT project_id FK

        VARCHAR milestone_title

        TEXT description

        DATE due_date

        VARCHAR milestone_status "enum:MilestoneStatusConstants"

        INT story_points

        SMALLINT sort_order

        DATETIME completed_at

        DATETIME created_at

        DATETIME updated_at

    }



    PROJECT_ALLOCATIONS {

        BIGINT id PK

        BIGINT resource_profile_id FK

        BIGINT project_id FK

        BIGINT allocated_by_user_id FK

        DECIMAL allocation_percentage

        DATE allocation_start_date

        DATE allocation_end_date

        VARCHAR allocation_status "enum:AllocationStatusConstants"

        DATETIME created_at

        DATETIME updated_at

    }



    TIMESHEETS {

        BIGINT id PK

        BIGINT resource_profile_id FK

        DATE week_start_date

        VARCHAR status "enum:TimesheetStatusConstants"

        DECIMAL total_hours

        TEXT remarks

        DATETIME submitted_at

        DATETIME created_at

        DATETIME updated_at

    }



    TIMESHEET_LINE_ITEMS {

        BIGINT id PK

        BIGINT timesheet_id FK

        BIGINT project_id FK

        DECIMAL hours_logged

        TEXT work_notes

        DATE work_date

        DATETIME created_at

        DATETIME updated_at

    }



    ACTIVITY_TAGS {

        BIGINT id PK

        VARCHAR tag_code UK

        VARCHAR tag_name

        VARCHAR tag_category "enum:ActivityTagCategoryConstants"

        BOOLEAN is_active

        DATETIME created_at

    }



    TIMESHEET_LINE_ITEM_ACTIVITY_TAGS {

        BIGINT timesheet_line_item_id PK_FK

        BIGINT activity_tag_id PK_FK

        VARCHAR custom_tag_text

    }



    AUDIT_LOGS {

        BIGINT id PK

        BIGINT actor_user_id FK

        VARCHAR entity_name "enum:AuditEntityConstants"

        BIGINT entity_id

        VARCHAR action_type "enum:AuditActionConstants"

        TEXT old_values

        TEXT new_values

        DATETIME created_at

        VARCHAR correlation_id

    }



    AI_REQUEST_LOGS {

        BIGINT id PK

        BIGINT requested_by_user_id FK

        VARCHAR request_type "enum:AiRequestTypeConstants"

        TEXT prompt

        TEXT response_summary

        DATETIME created_at

    }



    SYSTEM_CONFIGURATIONS {

        BIGINT id PK

        VARCHAR config_key UK

        TEXT config_value

        TEXT description

        DATETIME updated_at

        BIGINT updated_by_user_id FK

    }



    SCHEDULER_JOB_LOGS {

        BIGINT id PK

        VARCHAR job_name "enum:SchedulerJobNameConstants"

        VARCHAR status "enum:SchedulerJobStatusConstants"

        DATETIME started_at

        DATETIME completed_at

        TEXT error_message

    }



    USERS ||--o| RESOURCE_PROFILES : owns

    USERS ||--o{ RESOURCE_PROFILES : manages

    USERS ||--o{ USER_ROLES : assigned

    ROLES ||--o{ USER_ROLES : contains

    ROLES ||--o{ ROLE_PERMISSIONS : grants

    PERMISSIONS ||--o{ ROLE_PERMISSIONS : includes



    USERS ||--o{ USER_SKILLS : possesses

    SKILLS ||--o{ USER_SKILLS : mapped_to



    USERS ||--o{ PROJECTS : manages

    PROJECTS ||--o{ PROJECT_MILESTONES : contains



    RESOURCE_PROFILES ||--o{ PROJECT_ALLOCATIONS : assigned

    PROJECTS ||--o{ PROJECT_ALLOCATIONS : receives

    USERS ||--o{ PROJECT_ALLOCATIONS : allocated_by



    RESOURCE_PROFILES ||--o{ TIMESHEETS : submits

    TIMESHEETS ||--o{ TIMESHEET_LINE_ITEMS : contains

    PROJECTS ||--o{ TIMESHEET_LINE_ITEMS : effort_logged



    TIMESHEET_LINE_ITEMS ||--o{ TIMESHEET_LINE_ITEM_ACTIVITY_TAGS : tagged_with

    ACTIVITY_TAGS ||--o{ TIMESHEET_LINE_ITEM_ACTIVITY_TAGS : assigned



    USERS ||--o{ AUDIT_LOGS : performs

    USERS ||--o{ AI_REQUEST_LOGS : requests

    USERS ||--o{ SYSTEM_CONFIGURATIONS : updates

