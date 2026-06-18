using Microsoft.EntityFrameworkCore;
using Server.Data.Configurations;
using Server.Models.Entities;

namespace Server.Data;

public class PrmDbContext(DbContextOptions<PrmDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<ResourceProfile> ResourceProfiles => Set<ResourceProfile>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMilestone> ProjectMilestones => Set<ProjectMilestone>();
    public DbSet<ProjectAllocation> ProjectAllocations => Set<ProjectAllocation>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<TimesheetLineItem> TimesheetLineItems => Set<TimesheetLineItem>();
    public DbSet<ActivityTag> ActivityTags => Set<ActivityTag>();
    public DbSet<TimesheetLineItemActivityTag> TimesheetLineItemActivityTags => Set<TimesheetLineItemActivityTag>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
    public DbSet<AiRequestLog> AiRequestLogs => Set<AiRequestLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SchedulerJobLog> SchedulerJobLogs => Set<SchedulerJobLog>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        UserAndAuthEntityConfiguration.Configure(builder);
        ResourceEntityConfiguration.Configure(builder);
        ProjectEntityConfiguration.Configure(builder);
        TimesheetEntityConfiguration.Configure(builder);
        SystemEntityConfiguration.Configure(builder);
    }
}
