namespace MarksApi.Dtos;


public class CourseStatsResponse
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; }
    public int Enrolled { get; set; }
    public int Graded { get; set; }
    public double? Average { get; set; }
    public int? Highest { get; set; }
    public int? Lowest { get; set; }
    public int Passed { get; set;}
}