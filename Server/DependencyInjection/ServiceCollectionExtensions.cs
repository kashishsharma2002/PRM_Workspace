using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Configuration;
using Server.Common;
using Server.Common.Errors;
using Server.Data;
using Server.Scheduler;
using Server.Services.Allocations;
using Server.Services.Auth;
using Server.Services.Employees;
using Server.Services.Projects;
using Server.Services.Shared;
using Server.Services.SystemConfig;
using Server.Services.Timesheets;
using Server.Services.Compliance;
using Server.Services.Emails;
using Server.Services.Emails.Infrastructure;
using Server.Services.Emails.Providers;
using Server.Services.Emails.Templates;
using Server.Services.Audit;
using Server.Services.Permissions;
using Server.Validators.Users;

namespace Server.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPrmServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpSettingsOptions>(configuration.GetSection(SmtpSettingsOptions.SectionName));

        services.AddDbContext<PrmDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<IEmployeeSkillRepository, EmployeeSkillRepository>();
        services.AddScoped<IAllocationRepository, AllocationRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IMilestoneRepository, MilestoneRepository>();
        services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IEmployeeTeamService, EmployeeTeamService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IAllocationService, AllocationService>();
        services.AddScoped<ISchedulerTimesheetService, SchedulerTimesheetService>();
        services.AddScoped<ITimesheetService, TimesheetService>();
        services.AddScoped<ITimesheetRepository, TimesheetRepository>();
        services.AddScoped<IActivityTagRepository, ActivityTagRepository>();
        services.AddScoped<ISystemConfigService, SystemConfigService>();
        services.AddScoped<IResourceStatusService, ResourceStatusService>();
        services.AddScoped<ISchedulerJobLogRepository, SchedulerJobLogRepository>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IEmailLogRepository, EmailLogRepository>();
        services.AddScoped<EmailConfigResolver>();
        services.AddScoped<IEmailProvider, SmtpProvider>();
        services.AddScoped<ITemplateRenderingService, TemplateRenderingService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ITimesheetComplianceService, TimesheetComplianceService>();
        services.AddScoped<IProjectHealthService, ProjectHealthService>();
        services.AddScoped<ISchedulerRunner, SchedulerRunner>();
        services.AddHostedService<BackgroundScheduler>();
        services.AddMemoryCache();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddDataProtection();
        services.AddSingleton<IConfigEncryptionHelper, ConfigEncryptionHelper>();
        services.AddScoped<IDbTransactionManager, EfDbTransactionManager>();

        services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();
        services.AddFluentValidationAutoValidation();
        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return new BadRequestObjectResult(
                        ApiResponse<object>.Fail("Validation failed.", ErrorCodes.ValidationFailed, errors));
                };
            })
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerConfiguration();

        return services;
    }
}
