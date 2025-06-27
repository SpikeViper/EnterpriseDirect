using System.Security.Claims;
using EnterpriseDirect.Data;
using EnterpriseDirect.Shared.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDirect.Services;

/// <summary>
/// The user service is a wrapper around the ASP.NET Core Identity RoleManager and UserManager.
/// It exposes high-level methods for creating and managing roles and users.
/// </summary>
public class UserService
{
    private readonly IConfiguration _configuration;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserService> _logger;
    private readonly AuthenticationStateProvider _authProvider;

    public UserService(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager, 
        IConfiguration configuration, 
        ILogger<UserService> logger, 
        AuthenticationStateProvider authProvider)
    {
        this._roleManager = roleManager;
        this._userManager = userManager;
        _configuration = configuration;
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
    
    /// <summary>
    /// Returns all users in the system, including their role.
    /// </summary>
    public async Task<List<UserModel>> GetAllUsersAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        var userModels = new List<UserModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userModels.Add(new UserModel
            {
                Id = user.Id,
                Email = user.Email,
                IsAdmin = roles.Contains("Admin"),
            });
        }

        return userModels;
    }
    
    /// <summary>
    /// Sets the admin state of a user. Only users in the "Admin" role can change the admin state of other users.
    /// </summary>
    public async Task SetIsAdminAsync(string id, bool isAdmin)
    {
        var currentUser = await GetCurrentUserAsync();

        // Check if the current user is in the "Admin" role
        if (!currentUser.IsInRole("Admin"))
        {
            throw new UnauthorizedAccessException("Only administrators can change admin state.");
        }
        
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User with email {Email} not found.", id);
            return;
        }

        if (isAdmin)
        {
            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.AddToRoleAsync(user, "Admin");
                _logger.LogInformation("User {Email} added to Admin role.", id);
            }
        }
        else
        {
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
                _logger.LogInformation("User {Email} removed from Admin role.", id);
            }
        }
    }
    
}