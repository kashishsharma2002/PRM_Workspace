using Microsoft.EntityFrameworkCore;
using Server.Models.Entities;

namespace Server.Data.Configurations;

public static class TimesheetEntityConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<Timesheet>(entity =>
        {
            entity.ToTable("TIMESHEETS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ResourceProfileId).HasColumnName("resource_profile_id");
            entity.Property(e => e.WeekStartDate).HasColumnName("week_start_date").HasColumnType("date");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.Property(e => e.TotalHours).HasColumnName("total_hours").HasPrecision(5, 2);
            entity.Property(e => e.Remarks).HasColumnName("remarks").HasColumnType("text");
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => new { e.ResourceProfileId, e.WeekStartDate })
                .IsUnique()
                .HasDatabaseName("IX_Timesheets_ResourceProfile_Week");

            entity.HasOne<ResourceProfile>()
                .WithMany()
                .HasForeignKey(e => e.ResourceProfileId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TimesheetLineItem>(entity =>
        {
            entity.ToTable("TIMESHEET_LINE_ITEMS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TimesheetId).HasColumnName("timesheet_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.HoursLogged).HasColumnName("hours_logged").HasPrecision(5, 2);
            entity.Property(e => e.WorkNotes).HasColumnName("work_notes").HasColumnType("text");
            entity.Property(e => e.WorkDate).HasColumnName("work_date").HasColumnType("date");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.TimesheetId).HasDatabaseName("IX_LineItems_Timesheet");

            entity.HasOne<Timesheet>()
                .WithMany()
                .HasForeignKey(e => e.TimesheetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Project>()
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ActivityTag>(entity =>
        {
            entity.ToTable("ACTIVITY_TAGS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TagCode).HasColumnName("tag_code").HasMaxLength(50);
            entity.Property(e => e.TagName).HasColumnName("tag_name").HasMaxLength(100);
            entity.Property(e => e.TagCategory).HasColumnName("tag_category").HasMaxLength(50);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.TagCode).IsUnique();
        });

        builder.Entity<TimesheetLineItemActivityTag>(entity =>
        {
            entity.ToTable("TIMESHEET_LINE_ITEM_ACTIVITY_TAGS");
            entity.HasKey(e => new { e.TimesheetLineItemId, e.ActivityTagId });
            entity.Property(e => e.TimesheetLineItemId).HasColumnName("timesheet_line_item_id");
            entity.Property(e => e.ActivityTagId).HasColumnName("activity_tag_id");
            entity.Property(e => e.CustomTagText).HasColumnName("custom_tag_text").HasMaxLength(200);

            entity.HasOne<TimesheetLineItem>()
                .WithMany()
                .HasForeignKey(e => e.TimesheetLineItemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ActivityTag>()
                .WithMany()
                .HasForeignKey(e => e.ActivityTagId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
