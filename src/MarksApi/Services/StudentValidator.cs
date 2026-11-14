using MarksApi.Dtos;
namespace MarksApi.Services;

public static class StudentValidator
{
    public static Dictionary<string, string[]> Validate(StudentRequest s)
    {
        var errors = new Dictionary<string, string[]>();
        if(string.IsNullOrWhiteSpace(s.RegistrationNo))
            errors["registrationNo"] = new[] {"Registration number is required."};
        else if(s.RegistrationNo.Trim().Length > 20)
            errors["registrationNo"] = new[] {"Registration number must be at most 20 characters."};

        if (string.IsNullOrWhiteSpace(s.FullName))
            errors["fullName"] = new[] { "Full Name is required." };
        else if (s.FullName.Trim().Length > 100)
            errors["fullName"] = new[] { "Full Name must be at most 100 characters." };

        if (string.IsNullOrWhiteSpace(s.Email))
            errors["email"] = new[] { "Email is required." };
        else if (!s.Email.Contains('@'))
            errors["email"] = new[] { "Email is not valid." };
        else if (s.Email.Trim().Length > 150)
            errors["email"] = new[] { "Email must be at most 150 characters." };

        return errors;
    }
}