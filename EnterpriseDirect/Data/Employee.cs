using Microsoft.EntityFrameworkCore;

namespace EnterpriseDirect.Data;

/// <summary>
/// The base class for all employee types in the system.
/// </summary>
public abstract class Employee
{
    /// <summary>
    /// The internal ID of the employee.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The first name of the employee.
    /// </summary>
    public string FirstName { get; set; } = default!;
    
    /// <summary>
    /// The last name of the employee.
    /// </summary>
    public string LastName { get; set; } = default!;
    
    /// <summary>
    /// The email address of the employee.
    /// </summary>
    public string Email { get; set; } = default!;
    
    /// <summary>
    /// The department the employee belongs to.
    /// </summary>
    public string Department { get; set; } = default!;
    
    /// <summary>
    /// The job title of the employee.
    /// </summary>
    public string JobTitle { get; set; } = default!;
    
    /// <summary>
    /// The employment status of the employee.
    /// </summary>
    public string Status { get; set; } = default!; // "Active", "Inactive", "On Leave"
    
    /// <summary>
    /// The date the employee was hired.
    /// </summary>
    public DateTime HireDate { get; set; }

    /// <summary>
    /// Builds the database model for the Employee entity and its derived types.
    /// </summary>
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