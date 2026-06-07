using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PrmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
    await DatabaseSeeder.SeedAsync(db);
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "PRM.Server",
    phase = "0"
}));

app.Run();
