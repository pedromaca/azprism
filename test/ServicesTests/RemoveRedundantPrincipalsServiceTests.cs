using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Moq;
using Azprism.Services;

namespace AzprismTests.ServicesTests;

public class RemoveRedundantPrincipalsServiceTests
{
    public Guid originalObjectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
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
    public async Task RemovesRedundantPrincipalsAsync_WhenNotDryRun()
    {
        // Arrange
        var logger = new Mock<ILogger<RemoveRedundantPrincipalsService>>();
        var graphClientWrapper = new Mock<IGraphClientWrapper>();
        var compareLogger = new Mock<ILogger<ComparePrincipalsService>>();
        var compareService = new ComparePrincipalsService(compareLogger.Object);

        var originalAssignments = new List<AppRoleAssignment> { appRoleAssignmentA };
        var targetAssignments = new List<AppRoleAssignment> { appRoleAssignmentA, appRoleAssignmentB };

        // principalsToRemove should be appRoleAssignmentB
        var principalsToRemove = new List<AppRoleAssignment> { appRoleAssignmentB };

        graphClientWrapper
            .Setup(g => g.GetAllAssignmentsAsync(originalObjectId))
            .ReturnsAsync(originalAssignments);

        graphClientWrapper
            .Setup(g => g.GetAllAssignmentsAsync(targetObjectId))
            .ReturnsAsync(targetAssignments);

        graphClientWrapper
            .Setup(g => g.RemoveAppRoleAssignmentsAsync(It.Is<List<AppRoleAssignment>>(l => l.Count == 1 && l[0].PrincipalId == appRoleAssignmentB.PrincipalId), targetObjectId))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var service = new RemoveRedundantPrincipalsService(logger.Object, graphClientWrapper.Object, compareService);

        // Act
        await service.RemoveRedundantPrincipalsAsync(originalObjectId, targetObjectId);

        // Assert
        graphClientWrapper.Verify(g => g.GetAllAssignmentsAsync(originalObjectId), Times.Once);
        graphClientWrapper.Verify(g => g.GetAllAssignmentsAsync(targetObjectId), Times.Once);
        graphClientWrapper.Verify(g => g.RemoveAppRoleAssignmentsAsync(It.IsAny<List<AppRoleAssignment>>(), targetObjectId), Times.Once);
    }

    [Fact]
    public async Task RemovesNoAssignmentsAsync_WhenThereAreNoPrincipalsToRemove_WhenNotDryRun()
    {
        // Arrange
        var logger = new Mock<ILogger<RemoveRedundantPrincipalsService>>();
        var graphClientWrapper = new Mock<IGraphClientWrapper>();
        var compareLogger = new Mock<ILogger<ComparePrincipalsService>>();
        var compareService = new ComparePrincipalsService(compareLogger.Object);

        var originalAssignments = new List<AppRoleAssignment> { appRoleAssignmentA, appRoleAssignmentB };
        var targetAssignments = new List<AppRoleAssignment> { appRoleAssignmentA, appRoleAssignmentB };

        graphClientWrapper
            .Setup(g => g.GetAllAssignmentsAsync(originalObjectId))
            .ReturnsAsync(originalAssignments);

        graphClientWrapper
            .Setup(g => g.GetAllAssignmentsAsync(targetObjectId))
            .ReturnsAsync(targetAssignments);

        graphClientWrapper
            .Setup(g => g.RemoveAppRoleAssignmentsAsync(It.IsAny<List<AppRoleAssignment>>(), targetObjectId))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var service = new RemoveRedundantPrincipalsService(logger.Object, graphClientWrapper.Object, compareService);

        // Act
        await service.RemoveRedundantPrincipalsAsync(originalObjectId, targetObjectId);

        // Assert
        graphClientWrapper.Verify(g => g.GetAllAssignmentsAsync(originalObjectId), Times.Once);
        graphClientWrapper.Verify(g => g.GetAllAssignmentsAsync(targetObjectId), Times.Once);
        graphClientWrapper.Verify(g => g.RemoveAppRoleAssignmentsAsync(It.IsAny<List<AppRoleAssignment>>(), targetObjectId), Times.Never);
    }

    [Fact]
    public async Task DryRun_WithPrincipals_DoesNotCallRemove()
    {
        // Arrange
        var logger = new Mock<ILogger<RemoveRedundantPrincipalsService>>();
        var graphClientWrapper = new Mock<IGraphClientWrapper>();
        var compareLogger = new Mock<ILogger<ComparePrincipalsService>>();
        var compareService = new ComparePrincipalsService(compareLogger.Object);

        var originalAssignments = new List<AppRoleAssignment> { appRoleAssignmentA };
        var targetAssignments = new List<AppRoleAssignment> { appRoleAssignmentA, appRoleAssignmentB };

        graphClientWrapper
            .Setup(g => g.GetAllAssignmentsAsync(originalObjectId))
            .ReturnsAsync(originalAssignments);

        graphClientWrapper
            .Setup(g => g.GetAllAssignmentsAsync(targetObjectId))
            .ReturnsAsync(targetAssignments);

        var service = new RemoveRedundantPrincipalsService(logger.Object, graphClientWrapper.Object, compareService);

        // Act
        await service.RemoveRedundantPrincipalsAsync(originalObjectId, targetObjectId, dryRun: true);

        // Assert
        graphClientWrapper.Verify(g => g.GetAllAssignmentsAsync(originalObjectId), Times.Once);
        graphClientWrapper.Verify(g => g.GetAllAssignmentsAsync(targetObjectId), Times.Once);
        graphClientWrapper.Verify(g => g.RemoveAppRoleAssignmentsAsync(It.IsAny<List<AppRoleAssignment>>(), targetObjectId), Times.Never);
    }
}
