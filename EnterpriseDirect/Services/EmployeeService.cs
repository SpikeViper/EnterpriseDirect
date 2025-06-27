using EnterpriseDirect.Data;
using EnterpriseDirect.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDirect.Services;

public class EmployeeService
{
    private readonly ILogger<EmployeeService> _logger;
    private readonly ApplicationDbContext _context; 

    public EmployeeService(ApplicationDbContext context, ILogger<EmployeeService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    // Get all employees
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
        entity.HireDate = model.HireDate;

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

    // Get employee by Id
    public async Task<EmployeeModel?> GetEmployeeByIdAsync(string id)
    {
        return ToModel(await _context.Employees.FindAsync(id));
    }

    // Add a new employee
    public async Task AddEmployeeAsync(EmployeeModel employee)
    {
        var entity = ToEntity(employee);
        _context.Employees.Add(entity);
        await _context.SaveChangesAsync();
    }

    // Update an existing employee
    public async Task UpdateEmployeeAsync(EmployeeModel employee)
    {
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

    // Delete an employee
    public async Task DeleteEmployeeAsync(int id)
    {
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
}