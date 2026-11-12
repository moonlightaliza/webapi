using MarksApi.Data;
using MarksApi.Dtos;
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
                FullName = s.FullName
            })
            .FirstOrDefaultAsync(ct);
    }

    public Task<PagedResult<StudentResponse>> ListAsync(int page, int pageSize, string? search, string? sort, CancellationToken ct)
        => throw new NotImplementedException();

    public Task<StudentResponse> CreateAsync(StudentRequest request, CancellationToken ct)
        => throw new NotImplementedException();

    public Task<StudentResponse?> UpdateAsync(int id, StudentRequest request, CancellationToken ct)
        => throw new NotImplementedException();

    public Task<bool> DeleteAsync(int id, CancellationToken ct)
        => throw new NotImplementedException();
}