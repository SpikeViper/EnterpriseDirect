using System.Security.Claims;
using EnterpriseDirect.Data;
using EnterpriseDirect.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EnterpriseDirect.Tests;

public class UserServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly Mock<AuthenticationStateProvider> _authProviderMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        // Mock the stores required by UserManager and RoleManager
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();

        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null
        );
        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object, null, null, null, null
        );
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _authProviderMock = new Mock<AuthenticationStateProvider>();

        _userService = new UserService(
            _roleManagerMock.Object,
            _userManagerMock.Object,
            _configMock.Object,
            _loggerMock.Object,
            _authProviderMock.Object
        );
    }

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
    public async Task EnsureRolesAsync_WhenRolesDoNotExist_CreatesRoles()
    {
        // Arrange
        _roleManagerMock.Setup(r => r.RoleExistsAsync("Admin")).ReturnsAsync(false);
        _roleManagerMock.Setup(r => r.RoleExistsAsync("ReadOnly")).ReturnsAsync(false);
        _roleManagerMock.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>())).ReturnsAsync(IdentityResult.Success);

        // Act
        await _userService.EnsureRolesAsync();

        // Assert
        _roleManagerMock.Verify(r => r.CreateAsync(It.Is<IdentityRole>(role => role.Name == "Admin")), Times.Once);
        _roleManagerMock.Verify(r => r.CreateAsync(It.Is<IdentityRole>(role => role.Name == "ReadOnly")), Times.Once);
    }

    [Fact]
    public async Task CreateExampleUsersAsync_WhenConfiguredAndUsersDoNotExist_CreatesUsers()
    {
        // Arrange
        _configMock.Setup(c => c["DefaultUsers:AdminEmail"]).Returns("admin@test.com");
        _configMock.Setup(c => c["DefaultUsers:AdminPassword"]).Returns("Password123!");

        _userManagerMock.Setup(u => u.FindByEmailAsync("admin@test.com")).ReturnsAsync((ApplicationUser)null!);
        _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin")).ReturnsAsync(IdentityResult.Success);

        // Act
        await _userService.CreateExampleUsersAsync();

        // Assert
        _userManagerMock.Verify(u => u.CreateAsync(It.Is<ApplicationUser>(user => user.Email == "admin@test.com"), "Password123!"), Times.Once);
        _userManagerMock.Verify(u => u.AddToRoleAsync(It.Is<ApplicationUser>(user => user.Email == "admin@test.com"), "Admin"), Times.Once);
    }

    [Fact]
    public async Task CreateExampleUsersAsync_WhenConfigIsMissing_LogsWarningAndSkipsCreation()
    {
        // Arrange
        _configMock.Setup(c => c["DefaultUsers:AdminEmail"]).Returns(""); // Empty config
        _configMock.Setup(c => c["DefaultUsers:AdminPassword"]).Returns("");

        // Act
        await _userService.CreateExampleUsersAsync();

        // Assert
        _userManagerMock.Verify(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("is not configured")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce
        );
    }

    [Fact]
    public async Task SetIsAdminAsync_WhenCurrentUserIsAdmin_GrantsAdminRole()
    {
        // Arrange
        SetupAuthentication("Admin");
        var targetUser = new ApplicationUser { Id = "targetUserId", Email = "target@test.com" };
        _userManagerMock.Setup(u => u.FindByIdAsync("targetUserId")).ReturnsAsync(targetUser);
        _userManagerMock.Setup(u => u.IsInRoleAsync(targetUser, "Admin")).ReturnsAsync(false);
        _userManagerMock.Setup(u => u.AddToRoleAsync(targetUser, "Admin")).ReturnsAsync(IdentityResult.Success);

        // Act
        await _userService.SetIsAdminAsync("targetUserId", true);

        // Assert
        _userManagerMock.Verify(u => u.AddToRoleAsync(targetUser, "Admin"), Times.Once);
    }

    [Fact]
    public async Task SetIsAdminAsync_WhenCurrentUserIsAdmin_RevokesAdminRole()
    {
        // Arrange
        SetupAuthentication("Admin");
        var targetUser = new ApplicationUser { Id = "targetUserId", Email = "target@test.com" };
        _userManagerMock.Setup(u => u.FindByIdAsync("targetUserId")).ReturnsAsync(targetUser);
        _userManagerMock.Setup(u => u.IsInRoleAsync(targetUser, "Admin")).ReturnsAsync(true);
        _userManagerMock.Setup(u => u.RemoveFromRoleAsync(targetUser, "Admin")).ReturnsAsync(IdentityResult.Success);

        // Act
        await _userService.SetIsAdminAsync("targetUserId", false);

        // Assert
        _userManagerMock.Verify(u => u.RemoveFromRoleAsync(targetUser, "Admin"), Times.Once);
    }

    [Fact]
    public async Task SetIsAdminAsync_WhenCurrentUserIsNotAdmin_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        SetupAuthentication("ReadOnly"); // A non-admin user

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _userService.SetIsAdminAsync("anyId", true)
        );
    }
}