using System.Security.Claims;
using EnterpriseDirect.Data;
using EnterpriseDirect.Services;
using EnterpriseDirect.Shared.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EnterpriseDirect.Tests;

public class EmployeeServiceTests
{
    private readonly Mock<ILogger<EmployeeService>> _loggerMock;
    private readonly Mock<AuthenticationStateProvider> _authProviderMock;
    private ApplicationDbContext _context;
    private EmployeeService _employeeService;

    public EmployeeServiceTests()
    {
        _loggerMock = new Mock<ILogger<EmployeeService>>();
        _authProviderMock = new Mock<AuthenticationStateProvider>();

        // Setup a fresh in-memory database for each test
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _employeeService = new EmployeeService(
            _context,
            _loggerMock.Object,
            _authProviderMock.Object
        );
    }

    // Creates a valid database entity for a Full-Time Employee
    private static FullTimeEmployee CreateValidFullTimeEntity() =>
        new()
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@test.com",
            Department = "Engineering",
            JobTitle = "Developer",
            Status = "Active",
            HireDate = new DateTime(2023, 1, 1),
            Salary = 90000
        };

    // Creates a valid database entity for a Part-Time Employee
    private static PartTimeEmployee CreateValidPartTimeEntity() =>
        new()
        {
            Id = 2,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@test.com",
            Department = "Marketing",
            JobTitle = "Coordinator",
            Status = "Active",
            HireDate = new DateTime(2023, 2, 1),
            HourlyRate = 35.50m
        };

    // Creates a valid UI model for a Full-Time Employee
    private static EmployeeModel CreateValidFullTimeModel() =>
        new()
        {
            FirstName = "Peter",
            LastName = "Jones",
            Email = "peter.jones@test.com",
            Department = "Sales",
            JobTitle = "Representative",
            Status = "Active",
            HireDate = new DateTime(2024, 1, 1),
            EmploymentType = "FullTime",
            Salary = 65000
        };


    private void SetupAuthentication(string? role = null)
    {
        var identity = role != null
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "TestAuth")
            : new ClaimsIdentity();
        var user = new ClaimsPrincipal(identity);
        var authState = Task.FromResult(new AuthenticationState(user));
        _authProviderMock.Setup(x => x.GetAuthenticationStateAsync()).Returns(authState);
    }

    [Fact]
    public async Task GetAllEmployeesAsync_WhenEmployeesExist_ReturnsAllEmployeeModels()
    {
        // Arrange
        await _context.Employees.AddRangeAsync(
            CreateValidFullTimeEntity(),
            CreateValidPartTimeEntity()
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _employeeService.GetAllEmployeesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.FirstName == "John" && e.EmploymentType == "FullTime");
        Assert.Contains(result, e => e.FirstName == "Jane" && e.EmploymentType == "PartTime");
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_WhenEmployeeExists_ReturnsCorrectModel()
    {
        // Arrange
        var ftEmployee = CreateValidFullTimeEntity();
        await _context.Employees.AddAsync(ftEmployee);
        await _context.SaveChangesAsync();

        // Act
        var result = await _employeeService.GetEmployeeByIdAsync(ftEmployee.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ftEmployee.Id, result.Id);
        Assert.Equal(ftEmployee.FirstName, result.FirstName);
        Assert.Equal(ftEmployee.Salary, result.Salary);
        Assert.Null(result.HourlyRate);
        Assert.Equal("FullTime", result.EmploymentType);
    }

    [Fact]
    public async Task AddEmployeeAsync_WhenUserIsAdmin_AddsEmployee()
    {
        // Arrange
        SetupAuthentication("Admin");
        var newEmployeeModel = CreateValidFullTimeModel();

        // Act
        await _employeeService.AddEmployeeAsync(newEmployeeModel);

        // Assert
        var employeeInDb = await _context.Employees.FirstOrDefaultAsync(e => e.Email == newEmployeeModel.Email);
        Assert.NotNull(employeeInDb);
        Assert.IsType<FullTimeEmployee>(employeeInDb);
        Assert.Equal(newEmployeeModel.Salary, ((FullTimeEmployee)employeeInDb).Salary);
        Assert.Equal(newEmployeeModel.FirstName, employeeInDb.FirstName);
    }

    [Fact]
    public async Task AddEmployeeAsync_WithMissingRequiredTypeSpecificData_ThrowsArgumentException()
    {
        // Arrange
        SetupAuthentication("Admin");
        var invalidModel = CreateValidFullTimeModel();
        invalidModel.Salary = null; // This makes the model invalid for a FullTime employee

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _employeeService.AddEmployeeAsync(invalidModel)
        );
        Assert.Contains("Salary is required for Full-Time employees", exception.Message);
    }

    [Fact]
    public async Task AddEmployeeAsync_WhenUserIsNotAdmin_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        SetupAuthentication("ReadOnly");
        var newEmployee = CreateValidFullTimeModel();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _employeeService.AddEmployeeAsync(newEmployee)
        );
    }

    [Fact]
    public async Task UpdateEmployeeAsync_WhenUserIsAdminAndEmployeeExists_UpdatesEmployee()
    {
        // Arrange
        SetupAuthentication("Admin");
        var existingEmployee = CreateValidFullTimeEntity();
        await _context.AddAsync(existingEmployee);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear(); // Detach to simulate a new request

        var updatedModel = new EmployeeModel
        {
            Id = existingEmployee.Id,
            FirstName = "John-Updated",
            LastName = existingEmployee.LastName,
            Email = existingEmployee.Email,
            Department = "Management",
            JobTitle = existingEmployee.JobTitle,
            Status = existingEmployee.Status,
            HireDate = existingEmployee.HireDate,
            EmploymentType = "FullTime",
            Salary = 95000
        };

        // Act
        await _employeeService.UpdateEmployeeAsync(updatedModel);

        // Assert
        var employeeInDb = (FullTimeEmployee)await _context.Employees.FindAsync(existingEmployee.Id);
        Assert.NotNull(employeeInDb);
        Assert.Equal("John-Updated", employeeInDb.FirstName);
        Assert.Equal("Management", employeeInDb.Department);
        Assert.Equal(95000, employeeInDb.Salary);
    }

    [Fact]
    public async Task DeleteEmployeeAsync_WhenUserIsAdminAndEmployeeExists_DeletesEmployee()
    {
        // Arrange
        SetupAuthentication("Admin");
        var employeeToDelete = CreateValidPartTimeEntity();
        await _context.AddAsync(employeeToDelete);
        await _context.SaveChangesAsync();

        // Act
        await _employeeService.DeleteEmployeeAsync(employeeToDelete.Id);

        // Assert
        var employeeInDb = await _context.Employees.FindAsync(employeeToDelete.Id);
        Assert.Null(employeeInDb);
    }
}