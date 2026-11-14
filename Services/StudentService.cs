using MarksApi.Data;
using MarksApi.Dtos;
using Npgsql;
using Microsoft.EntityFrameworkCore;

namespace MarksApi.Services;

public class StudentService : IStudentService
{
    private readonly AppDbContext _db;
    private readonly ILogger<StudentService> _logger;

    public StudentService(AppDbContext db, ILogger<StudentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<StudentResponse?> GetAsync(int id, CancellationToken ct)
    {
        return await _db.Students
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new StudentResponse
            {
                Id = s.Id,
                RegistrationNo = s.RegistrationNo,
                FullName = s.FullName,
                Email = s.Email
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResult<StudentResponse>> ListAsync(int page, int pageSize, string? search, string? sort, CancellationToken ct)
    {

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _db.Students.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var x = search.Trim().ToLower();
            query = query.Where(s => s.FullName.ToLower().Contains(x) || s.RegistrationNo.ToLower().Contains(x));
        }

        query = sort?.ToLower() switch
        {
            "name_desc" => query.OrderByDescending(s => s.FullName),
            "regno"    => query.OrderBy(s => s.RegistrationNo),
        _           => query.OrderBy(s => s.FullName)
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StudentResponse { Id = s.Id, RegistrationNo = s.RegistrationNo, FullName = s.FullName, Email = s.Email })
            .ToListAsync(ct);

        return new PagedResult<StudentResponse> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<StudentResponse> CreateAsync(StudentRequest request, CancellationToken ct)
    {
        var student = new Student
        {
            RegistrationNo = request.RegistrationNo.Trim(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Students.Add(student);

        try 
        {
           await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            _logger.LogWarning("Create student failed: duplicate registration number or email {RegistrationNo}", request.RegistrationNo);
            throw new ConflictException("Duplicate student", "A student with this registration number or email already exists.");

        }
        _logger.LogInformation("Student created with id {Id}", student.Id);

        return new StudentResponse { Id = student.Id, RegistrationNo = student.RegistrationNo, FullName = student.FullName, Email = student.Email };

    }
        

    public async Task<StudentResponse?> UpdateAsync(int id, StudentRequest request, CancellationToken ct)
    {
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (student is null) return null;

        student.RegistrationNo = request.RegistrationNo.Trim();
        student.FullName = request.FullName.Trim();
        student.Email = request.Email.Trim().ToLowerInvariant();

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            _logger.LogWarning("Update student {Id} failed: duplicate", id);
            throw new ConflictException("Duplicate student", "A student with this registration number or email already exists.");
        }

        _logger.LogInformation("Student {Id} updated", id);
        return new StudentResponse { Id = student.Id, RegistrationNo = student.RegistrationNo, FullName = student.FullName, Email = student.Email };
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
    
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (student is null) return false;

        _db.Students.Remove(student);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23503" })
        {
            _logger.LogWarning("Delete student {Id} blocked: still has enrollments", id);
            throw new ConflictException("Student has enrollments", "A student that still has enrollments cannot be deleted.");
        }

        _logger.LogInformation("Student {Id} deleted", id);
        return true;
    }
}