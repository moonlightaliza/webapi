namespace MarksApi.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        ProblemDetails problem;

        if (exception is ConflictException conflict)
        {
            problem = new ProblemDetails { Status = 409, Title = conflict.Title, Detail = conflict.Message };
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception");
            problem = new ProblemDetails { Status = 500, Title = "An unexpected error occurred." };
        }

        context.Response.StatusCode = problem.Status!.Value;
        await context.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}