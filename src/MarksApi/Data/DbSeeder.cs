using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MarksApi.Data;


public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Students.AnyAsync()) return;   // already seeded
        var st1 = new Student { RegistrationNo = "005711", FullName = "Sara Ahmed", Email = "saraahmed23@gmail.com", CreatedAtUtc = DateTime.UtcNow};
        var st2 = new Student { RegistrationNo = "005712", FullName = "Areesha Saqib", Email = "areeshasaqib45@gmail.com", CreatedAtUtc = DateTime.UtcNow};
        var su1 = new User { Username = "saraahmed1", Role = "Student", Student = st1};
        var su2 = new User { Username = "areeshasaqib23", Role = "Student", Student = st2};
        var tu1 = new User { Username = "javeria56", Role = "Teacher"};
        var co1 = new Course{Code = "CS231222", Title = "Advanced Programming", CreditHours = 3, CreatedAtUtc = DateTime.UtcNow};
        var co2 = new Course{Code = "AI431455", Title = "Computer Vision", CreditHours = 4, CreatedAtUtc = DateTime.UtcNow};
        var co3 = new Course{Code = "SS337133", Title = "French Language", CreditHours = 3, CreatedAtUtc = DateTime.UtcNow};
        var e1 = new Enrollment{Student = st1, Course = co1, Mark = 78, EnrolledAtUtc = DateTime.UtcNow};
        var e2 = new Enrollment{Student = st1, Course = co2, EnrolledAtUtc = DateTime.UtcNow};
        var e3 = new Enrollment{Student = st1, Course = co3, Mark = 90, EnrolledAtUtc = DateTime.UtcNow};
        var e4 = new Enrollment{Student = st2, Course = co1, Mark = 79, EnrolledAtUtc = DateTime.UtcNow};
        var e5 = new Enrollment{Student = st2, Course = co2, Mark = 67, EnrolledAtUtc = DateTime.UtcNow};
        var e6 = new Enrollment{Student = st2, Course = co3, Mark = 91, EnrolledAtUtc = DateTime.UtcNow};

        var hasher = new PasswordHasher<User>();
        su1.PasswordHash = hasher.HashPassword(su1, "Adscxx234_");
        su2.PasswordHash = hasher.HashPassword(su2, "DfseE45_3");
        tu1.PasswordHash = hasher.HashPassword(tu1, "oiYwe32_09");
        db.Students.AddRange(st1, st2);
        db.Users.AddRange(su1, su2, tu1);
        db.Courses.AddRange(co1,co2,co3);
        db.Enrollments.AddRange(e1,e2,e3,e4,e5,e6);
        await db.SaveChangesAsync();

        
    }
}