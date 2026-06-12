using Microsoft.EntityFrameworkCore;
using Server.Models.Entities;

namespace Server.Data.Configurations;

public static class ProjectEntityConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<Project>(entity =>
        {
            entity.ToTable("PROJECTS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProjectCode).HasColumnName("project_code").HasMaxLength(50);
            entity.Property(e => e.ProjectName).HasColumnName("project_name").HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
            entity.Property(e => e.StartDate).HasColumnName("start_date").HasColumnType("date");
            entity.Property(e => e.EndDate).HasColumnName("end_date").HasColumnType("date");
            entity.Property(e => e.ProjectStatus).HasColumnName("project_status").HasMaxLength(20);
            entity.Property(e => e.HealthStatus).HasColumnName("health_status").HasMaxLength(20);
            entity.Property(e => e.TotalStoryPoints).HasColumnName("total_story_points");
            entity.Property(e => e.ManagerUserId).HasColumnName("manager_user_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.ProjectCode).IsUnique();
            entity.HasIndex(e => e.ManagerUserId).HasDatabaseName("IX_Projects_Manager");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.ManagerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProjectMilestone>(entity =>
        {
            entity.ToTable("PROJECT_MILESTONES");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.MilestoneTitle).HasColumnName("milestone_title").HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
            entity.Property(e => e.DueDate).HasColumnName("due_date").HasColumnType("date");
            entity.Property(e => e.MilestoneStatus).HasColumnName("milestone_status").HasMaxLength(20);
            entity.Property(e => e.StoryPoints).HasColumnName("story_points");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => new { e.ProjectId, e.DueDate }).HasDatabaseName("IX_Milestones_Project");

            entity.HasOne<Project>()
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProjectAllocation>(entity =>
        {
            entity.ToTable("PROJECT_ALLOCATIONS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ResourceProfileId).HasColumnName("resource_profile_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.AllocationPercentage).HasColumnName("allocation_percentage").HasPrecision(5, 2);
            entity.Property(e => e.AllocationStartDate).HasColumnName("allocation_start_date").HasColumnType("date");
            entity.Property(e => e.AllocationEndDate).HasColumnName("allocation_end_date").HasColumnType("date");
            entity.Property(e => e.AllocationStatus).HasColumnName("allocation_status").HasMaxLength(20);
            entity.Property(e => e.AllocatedByUserId).HasColumnName("allocated_by_user_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => new { e.ResourceProfileId, e.AllocationStatus, e.AllocationStartDate, e.AllocationEndDate })
                .HasDatabaseName("IX_Allocations_ResourceProfile");
            entity.HasIndex(e => e.ProjectId).HasDatabaseName("IX_Allocations_Project");

            entity.HasOne<ResourceProfile>()
                .WithMany()
                .HasForeignKey(e => e.ResourceProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Project>()
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.AllocatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
