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

    public Task<PagedResult<StudentResponse>> ListAsync(int page, int pageSize, string? search, string? sort, CancellationToken ct)
        => throw new NotImplementedException();

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
        

    public Task<StudentResponse?> UpdateAsync(int id, StudentRequest request, CancellationToken ct)
        => throw new NotImplementedException();

    public Task<bool> DeleteAsync(int id, CancellationToken ct)
        => throw new NotImplementedException();
}