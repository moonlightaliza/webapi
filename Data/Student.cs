namespace MarksApi.Data;

public class Student{
    public int Id { get; set; }
    public string RegistrationNo { get; set; }= string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; }= string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public List<Enrollment> Enrollments { get; set; } = new();
}