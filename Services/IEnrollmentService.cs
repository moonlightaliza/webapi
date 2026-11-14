using MarksApi.Dtos;

namespace MarksApi.Services;

public interface IEnrollmentService
{
    
    Task<EnrollmentResponse?> CreateAsync(int courseId, EnrollmentRequest request, CancellationToken ct);

    
    Task<PagedResult<EnrollmentResponse>?> ListByCourseAsync(int courseId, int page, int pageSize, CancellationToken ct);

    
    Task<EnrollmentResponse?> SetMarkAsync(int id, MarkRequest request, CancellationToken ct);

    Task<bool> DeleteAsync(int id, CancellationToken ct);
}