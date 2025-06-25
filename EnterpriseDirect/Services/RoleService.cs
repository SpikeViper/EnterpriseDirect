using EnterpriseDirect.Data;
using Microsoft.AspNetCore.Identity;

namespace EnterpriseDirect.Services;

/// <summary>
/// The role service is a wrapper around the ASP.NET Core Identity RoleManager and UserManager.
/// It exposes high-level methods for creating and managing roles and users.
/// </summary>
public class RoleService
{
    private readonly IConfiguration _configuration;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager, 
        IConfiguration configuration, 
        ILogger<RoleService> logger)
    {
        this._roleManager = roleManager;
        this._userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task EnsureRolesAsync()
    {
        var roles = new[] { "Admin", "ReadOnly" };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    public async Task CreateExampleUsersAsync()
    {
        // Create a default admin user
        var adminEmail = _configuration["DefaultUsers:AdminEmail"];
        var adminPassword = _configuration["DefaultUsers:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            _logger.LogWarning("Default admin email or password is not configured. Skipping admin user creation.");
        }
        else
        {
            var adminUser = await _userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                await _userManager.CreateAsync(adminUser, adminPassword);
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                
                _logger.LogInformation("Admin user created with email: {Email}", adminEmail);
            }
        }

        // Create a default read-only user
        var readOnlyEmail = _configuration["DefaultUsers:ReadOnlyEmail"];
        var readOnlyPassword = _configuration["DefaultUsers:ReadOnlyPassword"];
    
        if (string.IsNullOrWhiteSpace(readOnlyEmail) || string.IsNullOrWhiteSpace(readOnlyPassword))
        {
            _logger.LogWarning("Default read-only email or password is not configured. Skipping read-only user creation.");
        }
        else
        {
            var readOnlyUser = await _userManager.FindByEmailAsync(readOnlyEmail);

            if (readOnlyUser == null)
            {
                readOnlyUser = new ApplicationUser
                    { UserName = readOnlyEmail, Email = readOnlyEmail, EmailConfirmed = true };
                await _userManager.CreateAsync(readOnlyUser, readOnlyPassword);
                await _userManager.AddToRoleAsync(readOnlyUser, "ReadOnly");
                
                _logger.LogInformation("Read-only user created with email: {Email}", readOnlyEmail);
            }
        }
    }
    
}