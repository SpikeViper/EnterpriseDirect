namespace EnterpriseDirect.Data;

/// <summary>
/// The PartTimeEmployee class represents an employee who works part-time.
/// </summary>
public class PartTimeEmployee : Employee
{
    /// <summary>
    /// The hourly rate for the part-time employee.
    /// </summary>
    public decimal HourlyRate { get; set; }
}