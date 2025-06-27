using System.Security.Claims;
using EnterpriseDirect.Data;
using EnterpriseDirect.Shared.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDirect.Services;

public class EmployeeService
{
    private readonly ILogger<EmployeeService> _logger;
    private readonly ApplicationDbContext _context;
    private readonly AuthenticationStateProvider _authProvider;

    public EmployeeService(ApplicationDbContext context, ILogger<EmployeeService> logger, AuthenticationStateProvider authProvider)
    {
        _context = context;
        _logger = logger;
        _authProvider = authProvider;
    }
    
    /// <summary>
    /// Helper method to get the current user from the authentication state.
    /// </summary>
    private async Task<ClaimsPrincipal> GetCurrentUserAsync()
    {
        var authState = await _authProvider.GetAuthenticationStateAsync();
        return authState.User;
    }
    
    /// <summary>
    /// Returns a list of all employees in the system.
    /// </summary>
    public async Task<List<EmployeeModel>> GetAllEmployeesAsync()
    {
        var employees = await _context.Employees.ToListAsync();
        return ToModels(employees);
    }

    /// <summary>
    /// Converts a single Employee entity (from DB) to an EmployeeModel
    /// </summary>
    private EmployeeModel? ToModel(Data.Employee? employee)
    {
        if (employee == null) return null;

        var model = new EmployeeModel
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            Department = employee.Department,
            JobTitle = employee.JobTitle,
            Status = employee.Status,
            HireDate = employee.HireDate,
            // Initialize rate/salary to null
            HourlyRate = null,
            Salary = null
        };

        // Populate specific properties based on the actual type of the EF entity
        if (employee is Data.PartTimeEmployee pt)
        {
            model.HourlyRate = pt.HourlyRate;
            model.EmploymentType = "PartTime";
        }
        else if (employee is Data.FullTimeEmployee ft)
        {
            model.Salary = ft.Salary;
            model.EmploymentType = "FullTime";
        }
        else
        {
            model.EmploymentType = "Unknown"; // Fallback
            _logger.LogWarning("Unknown employee type for employee ID {Id}", employee.Id);
        }

        return model;
    }
    
    /// <summary>
    /// Converts an EmployeeModel (from UI) to the correct Employee entity type
    /// </summary>
    private Data.Employee ToEntity(EmployeeModel model, Data.Employee? existingEntity = null)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        Data.Employee entity;

        // Determine the type of entity to create/update
        if (model.EmploymentType == "PartTime")
        {
            var ptEntity = existingEntity as Data.PartTimeEmployee ?? new Data.PartTimeEmployee();
            ptEntity.HourlyRate = model.HourlyRate ?? throw new ArgumentException("Hourly Rate is required for Part-Time employees.");
            entity = ptEntity;
        }
        else if (model.EmploymentType == "FullTime")
        {
            var ftEntity = existingEntity as Data.FullTimeEmployee ?? new Data.FullTimeEmployee();
            ftEntity.Salary = model.Salary ?? throw new ArgumentException("Salary is required for Full-Time employees.");
            entity = ftEntity;
        }
        else
        {
            throw new ArgumentException($"Invalid EmployeeType '{model.EmploymentType}' specified.");
        }

        // Map common properties
        entity.Id = model.Id; // ID is usually set by DB on Add, but needed for updates
        entity.FirstName = model.FirstName;
        entity.LastName = model.LastName;
        entity.Email = model.Email;
        entity.Department = model.Department;
        entity.JobTitle = model.JobTitle;
        entity.Status = model.Status;
        entity.HireDate = model.HireDate.ToUniversalTime();
        entity.CreatedAt = existingEntity?.CreatedAt ?? DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        return entity;
    }

    /// <summary>
    /// Converts a collection of Employee entities to EmployeeModel objects.
    /// </summary>
    private List<EmployeeModel> ToModels(IEnumerable<Data.Employee>? employees)
    {
        if (employees == null) return new List<EmployeeModel>();
        return employees.Where(x => x != null).Select(ToModel).ToList();
    }

    /// <summary>
    /// Returns an employee by their ID.
    /// </summary>
    public async Task<EmployeeModel?> GetEmployeeByIdAsync(int id)
    {
        return ToModel(await _context.Employees.FindAsync(id));
    }

    /// <summary>
    /// Adds a new employee to the system.
    /// </summary>
    public async Task AddEmployeeAsync(EmployeeModel employee)
    {
        var currentUser = await GetCurrentUserAsync();

        // Check if the current user is in the "Admin" role
        if (!currentUser.IsInRole("Admin"))
        {
            throw new UnauthorizedAccessException("Only administrators can add new employees.");
        }
        
        var entity = ToEntity(employee);
        _context.Employees.Add(entity);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates an existing employee in the system.
    /// </summary>
    public async Task UpdateEmployeeAsync(EmployeeModel employee)
    {
        var currentUser = await GetCurrentUserAsync();

        // Check if the current user is in the "Admin" role
        if (!currentUser.IsInRole("Admin"))
        {
            throw new UnauthorizedAccessException("Only administrators can update employees.");
        }
        
        // Convert the model to the entity type
        var existingEntity = await _context.Employees.FindAsync(employee.Id);
        
        if (existingEntity == null)
        {
            _logger.LogWarning("Attempted to update non-existing employee with ID {Id}", employee.Id);
            return;
        }
        
        var entity = ToEntity(employee, existingEntity);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes an employee from the system.
    /// </summary>
    public async Task DeleteEmployeeAsync(int id)
    {
        var currentUser = await GetCurrentUserAsync();

        // Check if the current user is in the "Admin" role
        if (!currentUser.IsInRole("Admin"))
        {
            throw new UnauthorizedAccessException("Only administrators can delete employees.");
        }
        
        var employee = await _context.Employees.FindAsync(id);
        if (employee != null)
        {
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
        } else
        {
            _logger.LogWarning("Attempted to delete non-existing employee with ID {Id}", id);
        }
    }
    
    public async Task CreateExampleEmployeesAsync()
    {
        try
        {
            // Check if employees already exist
            var existingEmployees = await _context.Employees.CountAsync();
            if (existingEmployees > 0)
            {
                Console.WriteLine("Database already contains employees. Skipping seed.");
                return;
            }

            var seedEmployees = new List<Employee>
            {
                new FullTimeEmployee
                {
                    FirstName = "John",
                    LastName = "Smith",
                    Email = "john.smith@company.com",
                    Department = "Engineering",
                    JobTitle = "Senior Software Engineer",
                    Status = "Active",
                    HireDate = new DateTime(2022, 3, 15).ToUniversalTime(),
                    Salary = 95000,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new FullTimeEmployee
                {
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    Email = "sarah.johnson@company.com",
                    Department = "HR",
                    JobTitle = "HR Manager",
                    Status = "Active",
                    HireDate = new DateTime(2021, 8, 10).ToUniversalTime(),
                    Salary = 75000,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new FullTimeEmployee
                {
                    FirstName = "Michael",
                    LastName = "Brown",
                    Email = "michael.brown@company.com",
                    Department = "Sales",
                    JobTitle = "Sales Representative",
                    Status = "Active",
                    HireDate = new DateTime(2023, 1, 20).ToUniversalTime(),
                    Salary = 55000,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PartTimeEmployee
                {
                    FirstName = "Emily",
                    LastName = "Davis",
                    Email = "emily.davis@company.com",
                    Department = "Marketing",
                    JobTitle = "Marketing Coordinator",
                    Status = "Active",
                    HireDate = new DateTime(2022, 11, 5).ToUniversalTime(),
                    HourlyRate = 28.50m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new FullTimeEmployee
                {
                    FirstName = "David",
                    LastName = "Wilson",
                    Email = "david.wilson@company.com",
                    Department = "Engineering",
                    JobTitle = "DevOps Engineer",
                    Status = "Active",
                    HireDate = new DateTime(2021, 6, 12).ToUniversalTime(),
                    Salary = 88000,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new FullTimeEmployee
                {
                    FirstName = "Lisa",
                    LastName = "Anderson",
                    Email = "lisa.anderson@company.com",
                    Department = "Finance",
                    JobTitle = "Financial Analyst",
                    Status = "On Leave",
                    HireDate = new DateTime(2020, 4, 8).ToUniversalTime(),
                    Salary = 68000,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new FullTimeEmployee
                {
                    FirstName = "Robert",
                    LastName = "Taylor",
                    Email = "robert.taylor@company.com",
                    Department = "IT Support",
                    JobTitle = "IT Specialist",
                    Status = "Active",
                    HireDate = new DateTime(2023, 5, 18).ToUniversalTime(),
                    Salary = 52000,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PartTimeEmployee
                {
                    FirstName = "Jennifer",
                    LastName = "Martinez",
                    Email = "jennifer.martinez@company.com",
                    Department = "Design",
                    JobTitle = "UX Designer",
                    Status = "Active",
                    HireDate = new DateTime(2022, 9, 25).ToUniversalTime(),
                    HourlyRate = 35.00m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new FullTimeEmployee
                {
                    FirstName = "Christopher",
                    LastName = "Garcia",
                    Email = "christopher.garcia@company.com",
                    Department = "Operations",
                    JobTitle = "Operations Manager",
                    Status = "Active",
                    HireDate = new DateTime(2019, 12, 3).ToUniversalTime(),
                    Salary = 82000,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PartTimeEmployee
                {
                    FirstName = "Amanda",
                    LastName = "Rodriguez",
                    Email = "amanda.rodriguez@company.com",
                    Department = "Customer Service",
                    JobTitle = "Customer Support Specialist",
                    Status = "Terminated",
                    HireDate = new DateTime(2021, 2, 14).ToUniversalTime(),
                    HourlyRate = 18.75m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            // Add all employees to the context
            await _context.Employees.AddRangeAsync(seedEmployees);
            
            // Save changes to the database
            await _context.SaveChangesAsync();

            Console.WriteLine($"Successfully seeded database with {seedEmployees.Count} employees.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error seeding database: {ex.Message}");
            throw;
        }
    }
}