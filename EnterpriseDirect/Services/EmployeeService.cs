using EnterpriseDirect.Data;
using EnterpriseDirect.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDirect.Services;

public class EmployeeService
{
    private readonly ApplicationDbContext _context; 

    public EmployeeService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    // Get all employees
    public async Task<List<EmployeeModel>> GetAllEmployeesAsync()
    {
        var employees = await _context.Employees.ToListAsync();

        
    }

    /// <summary>
    /// Converts a collection of Employee entities to EmployeeModel objects.
    /// </summary>
    public List<EmployeeModel> ToModels(IEnumerable<Employee> employees)
    {
        var models = new List<EmployeeModel>();
        foreach (var employee in employees)
        {
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
            };

            if (employee is PartTimeEmployee pt)
            {
                model.HourlyRate = pt.HourlyRate;
            }
            
            if (employee is FullTimeEmployee ft)
            {
                model.Salary = ft.Salary;
            }
            
            models.Add(model);
        }
        
        return models;
    }

    // Get employee by Id
    public async Task<EmployeeModel?> GetEmployeeByIdAsync(int id)
    {
        return await _context.Employees.FindAsync(id);
    }

    // Add a new employee
    public async Task AddEmployeeAsync(EmployeeModel employee)
    {
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
    }

    // Update an existing employee
    public async Task UpdateEmployeeAsync(EmployeeModel employee)
    {
        // Attach and mark as modified if you're not tracking the entity
        _context.Entry(employee).State = EntityState.Modified;
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
        }
    }
}