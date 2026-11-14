using MarksApi.Data;
using MarksApi.Dtos;
using Npgsql;
using Microsoft.EntityFrameworkCore;

namespace MarksApi.Services;

public class CourseService : ICourseService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CourseService> _logger;

    public CourseService(AppDbContext db, ILogger<CourseService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CourseResponse?> GetAsync(int id, CancellationToken ct)
    {
        return await _db.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseResponse
            {
                Id = c.Id,
                Code = c.Code,
                Title = c.Title,
                CreditHours = c.CreditHours
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResult<CourseResponse>> ListAsync(int page, int pageSize, string? search, string? sort, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _db.Courses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var x = search.Trim().ToLower();
            query = query.Where(c => c.Title.ToLower().Contains(x) || c.Code.ToLower().Contains(x));
        }

        query = sort?.ToLower() switch
        {
            "title_desc" => query.OrderByDescending(c => c.Title),
            "code"       => query.OrderBy(c => c.Code),
            _            => query.OrderBy(c => c.Title)
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CourseResponse { Id = c.Id, Code = c.Code, Title = c.Title, CreditHours = c.CreditHours })
            .ToListAsync(ct);

        return new PagedResult<CourseResponse> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<CourseResponse> CreateAsync(CourseRequest request, CancellationToken ct)
    {
        var course = new Course
        {
            Code = request.Code.Trim(),
            Title = request.Title.Trim(),
            CreditHours = request.CreditHours,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Courses.Add(course);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            _logger.LogWarning("Create course failed: duplicate course code {Code}", request.Code);
            throw new ConflictException("Duplicate course", "A course with this code already exists.");
        }

        _logger.LogInformation("Course created with id {Id}", course.Id);

        return new CourseResponse { Id = course.Id, Code = course.Code, Title = course.Title, CreditHours = course.CreditHours };
    }

    public async Task<CourseResponse?> UpdateAsync(int id, CourseRequest request, CancellationToken ct)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (course is null) return null;

        course.Code = request.Code.Trim();
        course.Title = request.Title.Trim();
        course.CreditHours = request.CreditHours;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            _logger.LogWarning("Update course {Id} failed: duplicate", id);
            throw new ConflictException("Duplicate course", "A course with this code already exists.");
        }

        _logger.LogInformation("Course {Id} updated", id);
        return new CourseResponse { Id = course.Id, Code = course.Code, Title = course.Title, CreditHours = course.CreditHours };
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (course is null) return false;

        _db.Courses.Remove(course);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23503" or "23001" })
        {
            _logger.LogWarning("Delete course {Id} blocked: still has enrollments", id);
            throw new ConflictException("Course has enrollments", "A course that still has enrollments cannot be deleted.");
        }

        _logger.LogInformation("Course {Id} deleted", id);
        return true;
    }
}