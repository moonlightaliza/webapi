namespace MarksApi.Dtos;

public class TranscriptCourseResponse
{
    public string Code { get; set; } = "";
    public int CreditHours { get; set; }
    public int? Mark { get; set; }
    public bool Passed { get; set; }
}