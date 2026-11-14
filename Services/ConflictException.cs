namespace MarksApi.Services;

public class ConflictException : Exception
{
    public string Title { get; }

    public ConflictException(string title, string detail) : base(detail)
    {
        Title = title;
    }
}