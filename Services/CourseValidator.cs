using MarksApi.Dtos;
namespace MarksApi.Services;

public static class CourseValidator
{
    public static Dictionary<string, string[]> Validate(CourseRequest c)
    {
        var errors = new Dictionary<string, string[]>();
        if(string.IsNullOrWhiteSpace(c.Code))
            errors["code"] = new[] {"Course code is required."};
        else if(c.Code.Trim().Length > 10)
            errors["code"] = new[] {"Course code must be at most 10 characters."};

        if (string.IsNullOrWhiteSpace(c.Title))
            errors["title"] = new[] { "Title is required." };
        else if (c.Title.Trim().Length > 150)
            errors["title"] = new[] { "Title must be at most 150 characters." };

        if (c.CreditHours < 1 || c.CreditHours > 4)
            errors["creditHours"] = new[] { "Credit hours must be between 1 and 4." };

        return errors;
    }
}