namespace MarksApi.Dtos;

public static class MarkValidator
{
    public static Dictionary<string, string[]> Validate(MarkRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Mark is null)
        {
            errors["mark"] = new[] { "Mark is required." };
        }
        else if (request.Mark < 0 || request.Mark > 100)
        {
            errors["mark"] = new[] { "Mark must be between 0 and 100." };
        }

        return errors;
    }
}