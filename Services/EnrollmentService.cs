using MarksApi.Data;
using MarksApi.Dtos;
using Npgsql;
using Microsoft.EntityFrameworkCore;

namespace MarksApi.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly AppDbContext _db;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(AppDbContext db, ILogger<EnrollmentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<EnrollmentResponse?> CreateAsync(int courseId, EnrollmentRequest request, CancellationToken ct)
    {
        
        var courseExists = await _db.Courses.AnyAsync(c => c.Id == courseId, ct);
        if (!courseExists) return null;

        var studentExists = await _db.Students.AnyAsync(s => s.Id == request.StudentId, ct);
        if (!studentExists) return null;

        var enrollment = new Enrollment
        {
            StudentId = request.StudentId,
            CourseId = courseId,
            EnrolledAtUtc = DateTime.UtcNow
        };
        _db.Enrollments.Add(enrollment);

        
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            _logger.LogWarning("Create enrollment failed: student {StudentId} already enrolled in course {CourseId}", request.StudentId, courseId);
            throw new ConflictException("Duplicate enrollment", "This student is already enrolled in this course.");
        }

        _logger.LogInformation("Enrollment created with id {Id} (student {StudentId}, course {CourseId})", enrollment.Id, enrollment.StudentId, enrollment.CourseId);

        return new EnrollmentResponse { Id = enrollment.Id, StudentId = enrollment.StudentId, CourseId = enrollment.CourseId, Mark = enrollment.Mark };
    }

    public async Task<PagedResult<EnrollmentResponse>?> ListByCourseAsync(int courseId, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var courseExists = await _db.Courses.AnyAsync(c => c.Id == courseId, ct);
        if (!courseExists) return null;

        var query = _db.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .OrderBy(e => e.Id); 

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EnrollmentResponse { Id = e.Id, StudentId = e.StudentId, CourseId = e.CourseId, Mark = e.Mark })
            .ToListAsync(ct);

        return new PagedResult<EnrollmentResponse> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<EnrollmentResponse?> SetMarkAsync(int id, MarkRequest request, CancellationToken ct)
    {
        var enrollment = await _db.Enrollments.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (enrollment is null) return null;

        var oldMark = enrollment.Mark;
        enrollment.Mark = request.Mark; 

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Enrollment {Id} mark changed from {OldMark} to {NewMark}", id, oldMark, enrollment.Mark);

        return new EnrollmentResponse { Id = enrollment.Id, StudentId = enrollment.StudentId, CourseId = enrollment.CourseId, Mark = enrollment.Mark };
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var enrollment = await _db.Enrollments.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (enrollment is null) return false;

        _db.Enrollments.Remove(enrollment);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Enrollment {Id} deleted", id);
        return true;
    }
}