using Microsoft.EntityFrameworkCore;
namespace MarksApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Student> Students { get; set; } = null!;
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>(e=>{
            e.Property(s => s.RegistrationNo).HasMaxLength(20).IsRequired();
            e.Property(s => s.FullName).HasMaxLength(100).IsRequired();
            e.Property(s=> s.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(s => s.RegistrationNo).IsUnique();
            e.HasIndex(s => s.Email).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("ck_students_email_lower", "email = lower(email)"));
        });

        modelBuilder.Entity<Course>(e=>
        {
            e.Property(c=>c.Code).HasMaxLength(10).IsRequired();
            e.HasIndex(c=>c.Code).IsUnique();
            e.Property(c=>c.Title).HasMaxLength(150).IsRequired();
            e.ToTable(t => t.HasCheckConstraint(
                "ck_courses_credit_hours",
                "credit_hours BETWEEN 1 AND 4"));


        });

        modelBuilder.Entity<Enrollment>(e=>
        {
            e.ToTable(t=>t.HasCheckConstraint(
                "ck_enrollment_marks",
                "mark IS NULL or (mark BETWEEN 0 and 100 )"
            ));
            
            e.HasOne(x => x.Student).WithMany(y => y.Enrollments).HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Course).WithMany(y => y.Enrollments).HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new {x.StudentId, x.CourseId}).IsUnique();
    });

        modelBuilder.Entity<User>(e=>
        {
            e.Property(u=>u.Username).HasMaxLength(50).IsRequired();
            e.Property(u=>u.Role).HasMaxLength(20).IsRequired();
            e.HasOne(u => u.Student).WithMany().HasForeignKey(u => u.StudentId).OnDelete(DeleteBehavior.Restrict);
            e.Property(u => u.PasswordHash).IsRequired();
            e.HasIndex(u=>u.Username).IsUnique();
        });
        
        
        }
        
}