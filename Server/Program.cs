using System.Text;
using System.Text.Json;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Common;
using Server.Data;
using Server.Middleware;
using Server.Repositories;
using Server.Repositories.Interfaces;
using Server.Seed;
using Server.Services;
using Server.Services.Interfaces;
using Server.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddDbContext<PrmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<ISkillRepository, SkillRepository>();
builder.Services.AddScoped<IEmployeeSkillRepository, EmployeeSkillRepository>();
builder.Services.AddScoped<IAllocationRepository, AllocationRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IMilestoneRepository, MilestoneRepository>();
builder.Services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IAllocationService, AllocationService>();
builder.Services.AddScoped<ITimesheetService, TimesheetService>();
builder.Services.AddScoped<ITimesheetRepository, TimesheetRepository>();
builder.Services.AddScoped<IActivityTagRepository, ActivityTagRepository>();
builder.Services.AddScoped<ISystemConfigService, SystemConfigService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddDataProtection();
builder.Services.AddSingleton<ConfigEncryptionHelper>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings configuration is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return new BadRequestObjectResult(ApiResponse<object>.Fail("Validation failed.", errors));
        };
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
    await DatabaseSeeder.SeedAsync(db);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<ForcePasswordChangeMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "PRM.Server",
    phase = "5"
}));

app.Run();
