namespace EnterpriseDirect.Shared.Models;

public class EmployeeModel
{
    public int Id { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Department { get; set; } = default!;
    public string JobTitle { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime HireDate { get; set; }
    
    // Subclass properties
    
    public string EmploymentType { get; set; } = default!; // "FullTime", "PartTime"
    
    // Full time
    public decimal? Salary { get; set; }
    
    // Part time
    public decimal? HourlyRate { get; set; }
}