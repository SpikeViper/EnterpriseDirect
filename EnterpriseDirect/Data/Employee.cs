using Microsoft.EntityFrameworkCore;

namespace EnterpriseDirect.Data;

public abstract class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Department { get; set; } = default!;
    public string JobTitle { get; set; } = default!;
    public string Status { get; set; } = default!; // "Active", "Inactive", "On Leave"
    public DateTime HireDate { get; set; }

    public static void SetupDbModel(ModelBuilder builder)
    {
        builder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Department).IsRequired().HasMaxLength(50);
            entity.Property(e => e.JobTitle).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.HireDate).IsRequired();
        });

        builder.Entity<PartTimeEmployee>(entity =>
        {
            entity.Property(e => e.HourlyRate).IsRequired();
        });
        
        builder.Entity<FullTimeEmployee>(entity =>
        {
            entity.Property(e => e.Salary).IsRequired();
        });

        // Share table but allow for different employee types via OO inheritance
        builder.Entity<Employee>()
            .HasDiscriminator<string>("EmployeeType")
            .HasValue<Employee>("Employee")
            .HasValue<FullTimeEmployee>("FullTime")
            .HasValue<PartTimeEmployee>("PartTime");
    }
}