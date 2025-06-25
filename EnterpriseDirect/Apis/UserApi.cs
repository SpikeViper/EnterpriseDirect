using EnterpriseDirect.Services;
using EnterpriseDirect.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseDirect.Apis;

public class UserApi
{
    /// <summary>
    /// Registers the routes for the User API.
    /// </summary>
    public static void RegisterRoutes(WebApplication app)
    {
        app.MapGet("/api/users", GetUsersAsync);
        app.MapPost("api/users/{userId}/admin/{isAdmin}", SetUserIsAdminAsync);
    }
    
    /// <summary>
    /// Returns all users from the UserService as UserModel objects.
    /// Restricted to Admin users only.
    /// </summary>
    [Authorize("Admin")]
    public static async Task<List<UserModel>> GetUsersAsync(UserService userService)
    {
        return await userService.GetAllUsersAsync();
    }
    
    /// <summary>
    /// Sets the specified user as an admin or not.
    /// Restricted to Admin users only.
    /// </summary>
    [Authorize("Admin")]
    public static async Task SetUserIsAdminAsync(UserService userService, string userId, bool isAdmin)
    {
        await userService.SetIsAdminAsync(userId, isAdmin);
    }
}