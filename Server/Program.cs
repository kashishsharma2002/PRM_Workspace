using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DependencyInjection;
using Server.Middleware;
using Server.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrmServices(builder.Configuration);
builder.Services.AddPrmAi(builder.Configuration);
builder.Services.AddPrmAuthentication(builder.Configuration);

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(new
        {
            status = "healthy",
            service = "PRM.Server",
            phase = "7"
        });
        return;
    }

    await next(context);
});

if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
        await db.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(db);
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.EnablePersistAuthorization());
}

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<ForcePasswordChangeMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
