using Microsoft.EntityFrameworkCore;
using Server.Models.Entities;

namespace Server.Data.Configurations;

public static class SystemEntityConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<SystemConfiguration>(entity =>
        {
            entity.ToTable("SYSTEM_CONFIGURATIONS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConfigKey).HasColumnName("config_key").HasMaxLength(100);
            entity.Property(e => e.ConfigValue).HasColumnName("config_value").HasColumnType("text");
            entity.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");

            entity.HasIndex(e => e.ConfigKey).IsUnique();

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AiRequestLog>(entity =>
        {
            entity.ToTable("AI_REQUEST_LOGS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RequestType).HasColumnName("request_type").HasMaxLength(50);
            entity.Property(e => e.Prompt).HasColumnName("prompt").HasColumnType("text");
            entity.Property(e => e.ResponseSummary).HasColumnName("response_summary").HasColumnType("text");
            entity.Property(e => e.RequestedByUserId).HasColumnName("requested_by_user_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.RequestedByUserId).HasDatabaseName("IX_AiLogs_User");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AUDIT_LOGS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(e => e.EntityName).HasColumnName("entity_name").HasMaxLength(100);
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.ActionType).HasColumnName("action_type").HasMaxLength(50);
            entity.Property(e => e.OldValues).HasColumnName("old_values").HasColumnType("text");
            entity.Property(e => e.NewValues).HasColumnName("new_values").HasColumnType("text");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SchedulerJobLog>(entity =>
        {
            entity.ToTable("SCHEDULER_JOB_LOGS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.JobName).HasColumnName("job_name").HasMaxLength(100);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
        });

        builder.Entity<EmailTemplate>(entity =>
        {
            entity.ToTable("EMAIL_TEMPLATES");
            entity.HasKey(e => e.TemplateKey);
            entity.Property(e => e.TemplateKey).HasColumnName("template_key").HasMaxLength(50);
            entity.Property(e => e.SubjectTemplate).HasColumnName("subject_template").HasMaxLength(255).IsRequired();
            entity.Property(e => e.BodyTemplate).HasColumnName("body_template").HasColumnType("text").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        builder.Entity<EmailLog>(entity =>
        {
            entity.ToTable("EMAIL_LOGS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Recipient).HasColumnName("recipient").HasMaxLength(255).IsRequired();
            entity.Property(e => e.EmailType).HasColumnName("email_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Subject).HasColumnName("subject").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.Property(e => e.SentTime).HasColumnName("sent_time").IsRequired();
            entity.Property(e => e.EntityReference).HasColumnName("entity_reference").HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
            entity.Property(e => e.ProcessingDuration).HasColumnName("processing_duration").IsRequired();
            entity.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100).IsRequired();

            entity.HasIndex(e => new { e.Recipient, e.EmailType, e.SentTime });
        });
    }
}
