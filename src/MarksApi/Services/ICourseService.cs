using MarksApi.Dtos;

namespace MarksApi.Services;

public interface ICourseService
{
    Task<PagedResult<CourseResponse>> ListAsync(int page, int pageSize, string? search, string? sort, CancellationToken ct);
    Task<CourseResponse?> GetAsync(int id, CancellationToken ct);
    Task<CourseResponse> CreateAsync(CourseRequest request, CancellationToken ct);
    Task<CourseResponse?> UpdateAsync(int id, CourseRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}