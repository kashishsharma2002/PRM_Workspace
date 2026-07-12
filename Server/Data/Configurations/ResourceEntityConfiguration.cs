using Microsoft.EntityFrameworkCore;
using Server.Models.Entities;

namespace Server.Data.Configurations;

public static class ResourceEntityConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<ResourceProfile>(entity =>
        {
            entity.ToTable("RESOURCE_PROFILES");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ManagerId).HasColumnName("manager_id");
            entity.Property(e => e.ResourceStatus).HasColumnName("resource_status").HasMaxLength(20);
            entity.Property(e => e.IsTimesheetFrozen).HasColumnName("is_timesheet_frozen");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.ManagerId).HasDatabaseName("IX_ResourceProfiles_Manager");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Skill>(entity =>
        {
            entity.ToTable("SKILLS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SkillName).HasColumnName("skill_name").HasMaxLength(100);
            entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(50);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.SkillName).IsUnique();
        });

        builder.Entity<UserSkill>(entity =>
        {
            entity.ToTable("USER_SKILLS");
            entity.HasKey(e => new { e.UserId, e.SkillId });
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.ProficiencyLevel).HasColumnName("proficiency_level").HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Skill>()
                .WithMany()
                .HasForeignKey(e => e.SkillId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
