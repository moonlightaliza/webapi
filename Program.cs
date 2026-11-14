using Microsoft.EntityFrameworkCore;
using MarksApi.Data;
using MarksApi.Services;
using MarksApi.Dtos;

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Database"))
        .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
var app = builder.Build();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
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
app.MapPost("/api/students", async (StudentRequest request, IStudentService studservice, CancellationToken ct ) =>
{
    var result = await studservice.CreateAsync(request, ct);
    return Results.Created($"/api/students/{result.Id}", result);
}).RequireAuthorization(p => p.RequireRole("Teacher"));
app.MapGet("/api/students/{id:int}", async(int id, IStudentService studservice, CancellationToken ct ) =>
{
    var result = await studservice.GetAsync(id, ct);
    return result is null ? Results.NotFound(): Results.Ok(result);
}).RequireAuthorization(p => p.RequireRole("Teacher"));

app.Run();
