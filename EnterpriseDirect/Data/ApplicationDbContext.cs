using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDirect.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<Employee> Employees { get; set; }
    public DbSet<FullTimeEmployee> FullTimeEmployees { get; set; }
    public DbSet<PartTimeEmployee> PartTimeEmployees { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        Employee.SetupDbModel(builder);
    }
}