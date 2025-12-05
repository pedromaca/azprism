using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Moq;
using Azprism.Services;

namespace AzprismTests.ServicesTests;

public class AddPrincipalsServiceTests
{
    public Guid originalObjectId = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
    public Guid targetObjectId = Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210");

    public AppRoleAssignment originalAssignmentA = new AppRoleAssignment{
        AppRoleId   = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        PrincipalId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        ResourceId  = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
    };

    public AppRoleAssignment originalAssignmentB = new AppRoleAssignment{
        AppRoleId   = Guid.Parse("11111111-2222-3333-4444-555555555555"),
        PrincipalId = Guid.Parse("66666666-7777-8888-9999-000000000000"),
        ResourceId  = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")
    };

    [Fact]
    public async Task DryRun_WithAssignments_DoesNotCallAdd()
    {
        // Arrange
        var logger = new Mock<ILogger<AddPrincipalsService>>();
        var graphClientWrapper = new Mock<IGraphClientWrapper>();
        var appRoleMapping = new Mock<IAppRoleAssignmentMapping>();

        var allOriginal = new List<AppRoleAssignment> { originalAssignmentA, originalAssignmentB };
        var allTarget = new List<AppRoleAssignment> { };

        graphClientWrapper
            .Setup(g => g.GetAllAssignmentsAsync(originalObjectId))
            .ReturnsAsync(allOriginal);

        graphClientWrapper
            .Setup(g => g.GetAllAssignmentsAsync(targetObjectId))
            .ReturnsAsync(allTarget);

        var compareLogger = new Mock<ILogger<ComparePrincipalsService>>();
        var compareService = new ComparePrincipalsService(compareLogger.Object);

        var appRoleBuilder = new AppRoleAssignmentBuilderService(appRoleMapping.Object);

        var service = new AddPrincipalsService(logger.Object, graphClientWrapper.Object, compareService, appRoleBuilder);

        // Act
        await service.AddPrincipalsAsync(originalObjectId, targetObjectId, dryRun: true);

        // Assert
        graphClientWrapper.Verify(g => g.GetAllAssignmentsAsync(originalObjectId), Times.Once);
        graphClientWrapper.Verify(g => g.GetAllAssignmentsAsync(targetObjectId), Times.Once);
        graphClientWrapper.Verify(g => g.AddAppRoleAssignmentsAsync(It.IsAny<List<AppRoleAssignment>>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task AddsAssignments_WhenMissingAndNotDryRun()
    {
        // Arrange
        var logger = new Mock<ILogger<AddPrincipalsService>>();
        var graphClientWrapper = new Mock<IGraphClientWrapper>();
        var appRoleMapping = new Mock<IAppRoleAssignmentMapping>();

        var allOriginal = new List<AppRoleAssignment> { originalAssignmentA, originalAssignmentB };
        var allTarget = new List<AppRoleAssignment> { };

        graphClientWrapper
            .Setup(g => g.GetAllAssignmentsAsync(originalObjectId))
            .ReturnsAsync(allOriginal);

        graphClientWrapper
            .Setup(g => g.GetAllAssignmentsAsync(targetObjectId))
            .ReturnsAsync(allTarget);

        // AppRoleAssignmentMappingAsync used by builder - return empty mapping
        appRoleMapping
            .Setup(g => g.AppRoleAssignmentMappingAsync(originalObjectId, targetObjectId))
            .ReturnsAsync(new Dictionary<Guid, Guid>());

        graphClientWrapper
            .Setup(g => g.AddAppRoleAssignmentsAsync(It.IsAny<List<AppRoleAssignment>>(), targetObjectId))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var compareLogger = new Mock<ILogger<ComparePrincipalsService>>();
        var compareService = new ComparePrincipalsService(compareLogger.Object);

        var appRoleBuilder = new AppRoleAssignmentBuilderService(appRoleMapping.Object);

        var service = new AddPrincipalsService(logger.Object, graphClientWrapper.Object, compareService, appRoleBuilder);

        // Act
        await service.AddPrincipalsAsync(originalObjectId, targetObjectId, dryRun: false);

        // Assert
        graphClientWrapper.Verify(g => g.GetAllAssignmentsAsync(originalObjectId), Times.Once);
        graphClientWrapper.Verify(g => g.GetAllAssignmentsAsync(targetObjectId), Times.Once);
        appRoleMapping.Verify(g => g.AppRoleAssignmentMappingAsync(originalObjectId, targetObjectId), Times.Once);
        graphClientWrapper.Verify(g => g.AddAppRoleAssignmentsAsync(It.Is<List<AppRoleAssignment>>(l => l.Count == 2 && l[0].PrincipalId == originalAssignmentA.PrincipalId && l[1].PrincipalId == originalAssignmentB.PrincipalId), targetObjectId), Times.Once);
    }

    [Fact]
    public async Task DoesNotCallAdd_WhenThereAreNoPrincipalsToAdd()
    {
        // Arrange
        var logger = new Mock<ILogger<AddPrincipalsService>>();
        var graphClientWrapper = new Mock<IGraphClientWrapper>();
        var appRoleMapping = new Mock<IAppRoleAssignmentMapping>();

        var allOriginal = new List<AppRoleAssignment> { };
        var allTarget = new List<AppRoleAssignment> { };

        graphClientWrapper
            .Setup(g => g.GetAllAssignmentsAsync(originalObjectId))
            .ReturnsAsync(allOriginal);

        graphClientWrapper
            .Setup(g => g.GetAllAssignmentsAsync(targetObjectId))
            .ReturnsAsync(allTarget);

        graphClientWrapper
            .Setup(g => g.AddAppRoleAssignmentsAsync(It.IsAny<List<AppRoleAssignment>>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var compareLogger = new Mock<ILogger<ComparePrincipalsService>>();
        var compareService = new ComparePrincipalsService(compareLogger.Object);

        var appRoleBuilder = new AppRoleAssignmentBuilderService(appRoleMapping.Object);

        var service = new AddPrincipalsService(logger.Object, graphClientWrapper.Object, compareService, appRoleBuilder);

        // Act
        await service.AddPrincipalsAsync(originalObjectId, targetObjectId, dryRun: false);

        // Assert
        graphClientWrapper.Verify(g => g.GetAllAssignmentsAsync(originalObjectId), Times.Once);
        graphClientWrapper.Verify(g => g.GetAllAssignmentsAsync(targetObjectId), Times.Once);
        graphClientWrapper.Verify(g => g.AddAppRoleAssignmentsAsync(It.IsAny<List<AppRoleAssignment>>(), It.IsAny<Guid>()), Times.Never);
    }
}
