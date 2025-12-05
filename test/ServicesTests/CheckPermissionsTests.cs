using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Moq;
using Azprism.Services;

namespace AzprismTests.ServicesTests;

public class PrincipalPermissionsTests
{
    public Guid principalId = Guid.NewGuid();
    public List<AppRoleAssignment> acceptedAppRoleAssignments = new List<AppRoleAssignment>
    {
        new AppRoleAssignment { AppRoleId = Guid.Parse("1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9") },
        new AppRoleAssignment { AppRoleId = Guid.Parse("18a4783c-866b-4cc7-a460-3d5e5662c884") },
        new AppRoleAssignment { AppRoleId = Guid.Parse("06b708a9-e830-4db3-a914-8e69da51d44f") }
    };
    
    public List<AppRoleAssignment> unacceptedAppRoleAssignment = new List<AppRoleAssignment>
    {
        new AppRoleAssignment { AppRoleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")}
    };
    
    [Fact]
    public async Task PrincipalHasAllPermissions_ReturnsTrue()
    {
        // Arrange
        var logger = new Mock<ILogger<CheckPermissions>>();
        var graphClientWrapper = new Mock<IGraphClientWrapper>();
        
        graphClientWrapper.Setup(g => g.GetAppRoleAssignments(It.IsAny<Guid>()))
            .ReturnsAsync(acceptedAppRoleAssignments);
        
        var service = new CheckPermissions(logger.Object, graphClientWrapper.Object);
        
        // Act
        var hasPermissions = await service.PrincipalHasPermissions(principalId);
        
        // Assert
        Assert.True(hasPermissions);
    }
    
    [Fact]
    public async Task PrincipalHasOneAcceptedPermission_ReturnsTrue()
    {
        // Arrange
        var logger = new Mock<ILogger<CheckPermissions>>();
        var graphClientWrapper = new Mock<IGraphClientWrapper>();
        
        graphClientWrapper.Setup(g => g.GetAppRoleAssignments(It.IsAny<Guid>()))
            .ReturnsAsync(new List<AppRoleAssignment>
            {
                new AppRoleAssignment { AppRoleId = Guid.Parse("18a4783c-866b-4cc7-a460-3d5e5662c884") }
            });
        
        var service = new CheckPermissions(logger.Object, graphClientWrapper.Object);
        
        // Act
        var hasPermissions = await service.PrincipalHasPermissions(principalId);
        
        // Assert
        Assert.True(hasPermissions);
    }
    
    [Fact]
    public async Task PrincipalHasOneUnacceptedPermission_ReturnsFalse()
    {
        // Arrange
        var logger = new Mock<ILogger<CheckPermissions>>();
        var graphClientWrapper = new Mock<IGraphClientWrapper>();

        graphClientWrapper.Setup(g => g.GetAppRoleAssignments(It.IsAny<Guid>()))
            .ReturnsAsync(unacceptedAppRoleAssignment);
        
        var service = new CheckPermissions(logger.Object, graphClientWrapper.Object);
        
        // Act
        var hasPermissions = await service.PrincipalHasPermissions(principalId);
        
        // Assert
        Assert.False(hasPermissions);
    }
    
    [Fact]
    public async Task PrincipalHasNoPermissions_ReturnsFalse()
    {
        // Arrange
        var logger = new Mock<ILogger<CheckPermissions>>();
        var graphClientWrapper = new Mock<IGraphClientWrapper>();
        
        graphClientWrapper.Setup(g => g.GetAppRoleAssignments(It.IsAny<Guid>()))
            .ReturnsAsync(new List<AppRoleAssignment>());
        
        var service = new CheckPermissions(logger.Object, graphClientWrapper.Object);
        
        // Act
        var hasPermissions = await service.PrincipalHasPermissions(principalId);
        
        // Assert
        Assert.False(hasPermissions);
    }
}
