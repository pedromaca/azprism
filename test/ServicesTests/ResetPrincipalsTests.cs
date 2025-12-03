using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Moq;
using azprism.Services;

namespace AzprismTests.ServicesTests;

public class ResetPrincipalsServiceTests
{

    public Guid targetObjectId = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
    public AppRoleAssignment appRoleAssignmentA = new AppRoleAssignment{
        AppRoleId   = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
        PrincipalId = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
        ResourceId  = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef")
    };
    public AppRoleAssignment appRoleAssignmentB = new AppRoleAssignment{
        AppRoleId   = Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210"),
        PrincipalId = Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210"),
        ResourceId  = Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210")
    };
    
    [Fact]
    public async Task RemovesAllAssignmentsAsync_WhenNotDryRun()
    {
        // Arrange
        var logger = new Mock<ILogger<ResetPrincipalsService>>();
        var graphClientWrapper = new Mock<IGraphClientWrapper>();
        
        var allAssignments = new List<AppRoleAssignment> {
            appRoleAssignmentA,
            appRoleAssignmentB
        };

        graphClientWrapper
            .Setup(g => g.GetAllAssignmentsAsync(targetObjectId))
            .ReturnsAsync(allAssignments);

        graphClientWrapper
            .Setup(g => g.RemoveAppRoleAssignmentsAsync(allAssignments, targetObjectId))
            .Returns(Task.CompletedTask)
            .Verifiable();
            
        var service = new ResetPrincipalsService(graphClientWrapper.Object, logger.Object);

        // Act
        await service.ResetPrincipalsAsync(targetObjectId);

        // Assert
        graphClientWrapper.Verify(g => g.GetAllAssignmentsAsync(targetObjectId), Times.Once);
        graphClientWrapper.Verify(g => g.RemoveAppRoleAssignmentsAsync(allAssignments, targetObjectId), Times.Once);
    }

    [Fact]
    public async Task RemovesNoAssignmentsAsync_WhenThereAreNoAssignmentsToRemove_WhenNotDryRun()
    {
        // Arrange
        var logger = new Mock<ILogger<ResetPrincipalsService>>();
        var graphClientWrapper = new Mock<IGraphClientWrapper>();
        
        var allAssignments = new List<AppRoleAssignment> { };

        graphClientWrapper
            .Setup(g => g.GetAllAssignmentsAsync(targetObjectId))
            .ReturnsAsync(allAssignments);

        graphClientWrapper
            .Setup(g => g.RemoveAppRoleAssignmentsAsync(allAssignments, targetObjectId))
            .Returns(Task.CompletedTask)
            .Verifiable();
            
        var service = new ResetPrincipalsService(graphClientWrapper.Object, logger.Object);

        // Act
        await service.ResetPrincipalsAsync(targetObjectId);

        // Assert
        graphClientWrapper.Verify(g => g.GetAllAssignmentsAsync(targetObjectId), Times.Once);
        graphClientWrapper.Verify(g => g.RemoveAppRoleAssignmentsAsync(allAssignments, targetObjectId), Times.Never);
    }

    [Fact]
    public async Task DryRun_WithAssignments_DoesNotCallRemove()
    {
        // Arrange
        var logger = new Mock<ILogger<ResetPrincipalsService>>();
        var graphClientWrapper = new Mock<IGraphClientWrapper>();
        var allAssignments = new List<AppRoleAssignment> { new AppRoleAssignment() };
        
        graphClientWrapper
            .Setup(g => g.GetAllAssignmentsAsync(targetObjectId))
            .ReturnsAsync(allAssignments);
        
        var service = new ResetPrincipalsService(graphClientWrapper.Object, logger.Object);
        
        // Act
        await service.ResetPrincipalsAsync(targetObjectId, dryRun: true);
        
        // Assert
        graphClientWrapper.Verify(g => g.GetAllAssignmentsAsync(targetObjectId), Times.Once);
        graphClientWrapper.Verify(g => g.RemoveAppRoleAssignmentsAsync(allAssignments, targetObjectId), Times.Never);
    }
}
