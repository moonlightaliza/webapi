namespace MarksApi.Dtos;

public class TranscriptResponse
{
    public int StudentId { get; set; }
    public string RegistrationNo { get; set; } = "";
    public string FullName { get; set; } = "";
    public List<TranscriptCourseResponse> Courses { get; set; } = new();
    public double? WeightedAverage { get; set; }
}