namespace MarksApi.Data;

public class Course{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<Enrollment> Enrollments { get; set; } = new();
}
