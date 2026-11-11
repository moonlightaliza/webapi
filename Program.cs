using Microsoft.EntityFrameworkCore;
using MarksApi.Data;
using MarksApi.Services;
using MarksApi.Dtos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Database"))
        .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IAuthService, AuthService>();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

app.MapGet("/", () => "Hello");
app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService auth, CancellationToken ct) =>
{
    var result = await auth.LoginAsync(request, ct);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
});

app.Run();
