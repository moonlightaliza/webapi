using MarksApi.Dtos;

namespace MarksApi.Services;

public interface IStudentService
{
    Task<PagedResult<StudentResponse>> ListAsync(int page, int pageSize, string? search, string? sort, CancellationToken ct);
    Task<StudentResponse?> GetAsync(int id, CancellationToken ct);
    Task<StudentResponse> CreateAsync(StudentRequest request, CancellationToken ct);
    Task<StudentResponse?> UpdateAsync(int id, StudentRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}