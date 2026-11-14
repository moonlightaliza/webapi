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
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
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

// Students
app.MapPost("/api/students", async (StudentRequest request, IStudentService studservice, CancellationToken ct) =>
{
    var errors = StudentValidator.Validate(request);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var result = await studservice.CreateAsync(request, ct);
    return Results.Created($"/api/students/{result.Id}", result);
}).RequireAuthorization(p => p.RequireRole("Teacher"));

app.MapGet("/api/students/{id:int}", async (int id, IStudentService studservice, CancellationToken ct) =>
{
    var result = await studservice.GetAsync(id, ct);
    return result is null ? Results.NotFound() : Results.Ok(result);
}).RequireAuthorization(p => p.RequireRole("Teacher"));

app.MapGet("/api/students", async (IStudentService studservice, CancellationToken ct,
    int page = 1, int pageSize = 10, string? search = null, string? sort = null) =>
    Results.Ok(await studservice.ListAsync(page, pageSize, search, sort, ct)))
    .RequireAuthorization(p => p.RequireRole("Teacher"));

app.MapPut("/api/students/{id:int}", async (int id, StudentRequest request, IStudentService studservice, CancellationToken ct) =>
{
    var errors = StudentValidator.Validate(request);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var updated = await studservice.UpdateAsync(id, request, ct);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
}).RequireAuthorization(p => p.RequireRole("Teacher"));

app.MapDelete("/api/students/{id:int}", async (int id, IStudentService studservice, CancellationToken ct) =>
{
    return await studservice.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization(p => p.RequireRole("Teacher"));

// Courses
app.MapPost("/api/courses", async (CourseRequest request, ICourseService courseService, CancellationToken ct) =>
{
    var errors = CourseValidator.Validate(request);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var result = await courseService.CreateAsync(request, ct);
    return Results.Created($"/api/courses/{result.Id}", result);
}).RequireAuthorization(p => p.RequireRole("Teacher"));

app.MapGet("/api/courses/{id:int}", async (int id, ICourseService courseService, CancellationToken ct) =>
{
    var result = await courseService.GetAsync(id, ct);
    return result is null ? Results.NotFound() : Results.Ok(result);
}).RequireAuthorization();

app.MapGet("/api/courses", async (ICourseService courseService, CancellationToken ct,
    int page = 1, int pageSize = 10, string? search = null, string? sort = null) =>
    Results.Ok(await courseService.ListAsync(page, pageSize, search, sort, ct)))
    .RequireAuthorization();

app.MapPut("/api/courses/{id:int}", async (int id, CourseRequest request, ICourseService courseService, CancellationToken ct) =>
{
    var errors = CourseValidator.Validate(request);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var updated = await courseService.UpdateAsync(id, request, ct);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
}).RequireAuthorization(p => p.RequireRole("Teacher"));

app.MapDelete("/api/courses/{id:int}", async (int id, ICourseService courseService, CancellationToken ct) =>
{
    return await courseService.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization(p => p.RequireRole("Teacher"));

// Enrollments
app.MapPost("/api/courses/{courseId:int}/enrollments", async (int courseId, EnrollmentRequest request, IEnrollmentService enrollmentService, CancellationToken ct) =>
{
    var result = await enrollmentService.CreateAsync(courseId, request, ct);
    return result is null
        ? Results.NotFound()
        : Results.Created($"/api/courses/{courseId}/enrollments", result);
}).RequireAuthorization(p => p.RequireRole("Teacher"));

app.MapGet("/api/courses/{courseId:int}/enrollments", async (int courseId, IEnrollmentService enrollmentService, CancellationToken ct,
    int page = 1, int pageSize = 10) =>
{
    var result = await enrollmentService.ListByCourseAsync(courseId, page, pageSize, ct);
    return result is null ? Results.NotFound() : Results.Ok(result);
}).RequireAuthorization(p => p.RequireRole("Teacher"));

app.MapPut("/api/enrollments/{id:int}/mark", async (int id, MarkRequest request, IEnrollmentService enrollmentService, CancellationToken ct) =>
{
    var errors = MarkValidator.Validate(request);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var updated = await enrollmentService.SetMarkAsync(id, request, ct);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
}).RequireAuthorization(p => p.RequireRole("Teacher"));

app.MapDelete("/api/enrollments/{id:int}", async (int id, IEnrollmentService enrollmentService, CancellationToken ct) =>
{
    return await enrollmentService.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization(p => p.RequireRole("Teacher"));

app.Run();