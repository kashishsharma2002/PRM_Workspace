using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
using Server.Services.Users;
using Server.Validators.Users;

namespace Server.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPrmServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PrmDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

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
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IAllocationService, AllocationService>();
        services.AddScoped<ITimesheetService, TimesheetService>();
        services.AddScoped<ITimesheetRepository, TimesheetRepository>();
        services.AddScoped<IActivityTagRepository, ActivityTagRepository>();
        services.AddScoped<ISystemConfigService, SystemConfigService>();
        services.AddScoped<IResourceStatusService, ResourceStatusService>();
        services.AddScoped<ISchedulerJobLogRepository, SchedulerJobLogRepository>();
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
