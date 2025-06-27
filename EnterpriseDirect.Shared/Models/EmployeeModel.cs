using System.ComponentModel.DataAnnotations;

namespace EnterpriseDirect.Shared.Models;

public class EmployeeModel : IValidatableObject
{
    /// <summary>
    /// The internal ID of the employee.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The first name of the employee.
    /// </summary>
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
    public string FirstName { get; set; } = default!;
    
    /// <summary>
    /// The last name of the employee.
    /// </summary>
    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
    public string LastName { get; set; } = default!;
    
    /// <summary>
    /// The email address of the employee.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    public string Email { get; set; } = default!;
    
    /// <summary>
    /// The department the employee belongs to.
    /// </summary>
    [Required(ErrorMessage = "Department is required.")]
    [StringLength(50, ErrorMessage = "Department cannot exceed 50 characters.")]
    public string Department { get; set; } = default!;
    
    /// <summary>
    /// The job title of the employee.
    /// </summary>
    [Required(ErrorMessage = "Job Title is required.")]
    [StringLength(50, ErrorMessage = "Job Title cannot exceed 50 characters.")]
    public string JobTitle { get; set; } = default!;
    
    /// <summary>
    /// The employment status of the employee.
    /// </summary>
    [Required(ErrorMessage = "Status is required.")]
    [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters.")]
    public string Status { get; set; } = default!;
    
    /// <summary>
    /// The date the employee was hired.
    /// </summary>
    [Required(ErrorMessage = "Hire date is required.")]
    public DateTime HireDate { get; set; }
    
    // Subclass properties
    
    [Required(ErrorMessage = "Employee type is required.")]
    public string EmploymentType { get; set; } = default!; // "FullTime", "PartTime"
    
    // Full time
    /// <summary>
    /// The annual salary of the employee, if applicable.
    /// </summary>
    public decimal? Salary { get; set; }
    
    // Part time
    /// <summary>
    /// The hourly rate of the employee, if applicable.
    /// </summary>
    public decimal? HourlyRate { get; set; }
    
    /// <summary>
    /// The date and time when the employee record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// The date and time when the employee record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Performs custom validation based on the employment type.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Validation for Full-Time employees
        if (EmploymentType == "FullTime")
        {
            if (!Salary.HasValue || Salary.Value <= 0)
            {
                yield return new ValidationResult(
                    "Salary is required for Full-Time employees and must be a positive value.",
                    new[] { nameof(Salary) }); // Associate with Salary property
            }
            // Ensure HourlyRate is not set for FullTime
            if (HourlyRate.HasValue)
            {
                yield return new ValidationResult(
                    "Hourly Rate should not be set for Full-Time employees.",
                    new[] { nameof(HourlyRate) }); // Associate with HourlyRate property
            }
        }
        // Validation for Part-Time employees
        else if (EmploymentType == "PartTime")
        {
            if (!HourlyRate.HasValue || HourlyRate.Value <= 0)
            {
                yield return new ValidationResult(
                    "Hourly Rate is required for Part-Time employees and must be a positive value.",
                    new[] { nameof(HourlyRate) }); // Associate with HourlyRate property
            }
            // Ensure Salary is not set for PartTime
            if (Salary.HasValue)
            {
                yield return new ValidationResult(
                    "Salary should not be set for Part-Time employees.",
                    new[] { nameof(Salary) }); // Associate with Salary property
            }
        }
    }
}