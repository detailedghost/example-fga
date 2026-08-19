using FgaPoc.Authorization;

namespace FgaPoc.Tests;

public sealed class BlogAuthorizationModelTests
{
    [Theory]
    [InlineData("admin", 5)]
    [InlineData("editor", 4)]
    [InlineData("writer", 2)]
    [InlineData("reader", 1)]
    [InlineData(null, 0)]
    public void PermissionsForRole_ReturnsExpectedPermissionCount(string? role, int expectedCount)
    {
        Assert.Equal(expectedCount, BlogAuthorizationModel.PermissionsForRole(role).Count);
    }

    [Fact]
    public void PermissionsForRole_AdminIncludesManageAccess()
    {
        Assert.Contains(
            PermissionNames.ManageAccess,
            BlogAuthorizationModel.PermissionsForRole("admin")
        );
    }
}
