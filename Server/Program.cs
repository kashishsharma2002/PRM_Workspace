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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.EnablePersistAuthorization());
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
    phase = "7"
}));

app.Run();
