using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Moq;
using azprism.Services;

namespace AzprismTests.ServicesTests;

public class SyncAppRoleAssignmentsServiceTests
{
    private readonly Guid originalObjectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid targetObjectId = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");

    [Fact]
    public async Task CallsAddAndRemoveAsync_WhenNotDryRun()
    {
        // Arrange
        var logger = new Mock<ILogger<SyncAppRoleAssignmentsService>>();
        var graphClientWrapper = new Mock<IGraphClientWrapper>();

        // original has principal A, target has principal B -> will cause one add and one remove
        var originalAssignments = new List<AppRoleAssignment>
        {
            new AppRoleAssignment { 
                PrincipalId = Guid.Parse("11111111-1111-1111-1111-111111111111"), 
                AppRoleId = Guid.NewGuid(), 
                ResourceId = originalObjectId }
        };

        var targetAssignments = new List<AppRoleAssignment>
        {
            new AppRoleAssignment { 
                PrincipalId = Guid.Parse("22222222-2222-2222-2222-222222222222"), 
                AppRoleId = Guid.NewGuid(), 
                ResourceId = targetObjectId }
        };

        graphClientWrapper
            .Setup(g => g.GetAllAssignmentsAsync(originalObjectId))
            .ReturnsAsync(originalAssignments);

        graphClientWrapper
            .Setup(g => g.GetAllAssignmentsAsync(targetObjectId))
            .ReturnsAsync(targetAssignments);

        graphClientWrapper
            .Setup(g => g.AppRoleAssignmentMappingAsync(originalObjectId, targetObjectId))
            .ReturnsAsync(new Dictionary<Guid, Guid>());

        graphClientWrapper
            .Setup(g => g.AddAppRoleAssignmentsAsync(It.IsAny<List<AppRoleAssignment>>(), targetObjectId))
            .Returns(Task.CompletedTask)
            .Verifiable();

        graphClientWrapper
            .Setup(g => g.RemoveAppRoleAssignmentsAsync(It.IsAny<List<AppRoleAssignment>>(), targetObjectId))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Create dependent services with mocked dependencies
        var loggerForAdd = new Mock<ILogger<AddPrincipalsService>>();
        var loggerForRemove = new Mock<ILogger<RemoveRedundantPrincipalsService>>();
        var loggerForCompare = new Mock<ILogger<ComparePrincipalsService>>();

        var compareService = new ComparePrincipalsService(loggerForCompare.Object);
        var appRoleBuilder = new AppRoleAssignmentBuilderService(graphClientWrapper.Object);

        var addService = new AddPrincipalsService(loggerForAdd.Object, graphClientWrapper.Object, compareService, appRoleBuilder);
        var removeService = new RemoveRedundantPrincipalsService(loggerForRemove.Object, graphClientWrapper.Object, compareService);

        var syncService = new SyncAppRoleAssignmentsService(logger.Object, addService, removeService);

        // Act
        await syncService.SyncAppRoleAssignmentsAsync(originalObjectId, targetObjectId, dryRun: false);

        // Assert
        graphClientWrapper.Verify(g => g.GetAllAssignmentsAsync(originalObjectId), Times.AtLeastOnce);
        graphClientWrapper.Verify(g => g.GetAllAssignmentsAsync(targetObjectId), Times.AtLeastOnce);
        graphClientWrapper.Verify(g => g.AddAppRoleAssignmentsAsync(It.IsAny<List<AppRoleAssignment>>(), targetObjectId), Times.Once);
        graphClientWrapper.Verify(g => g.RemoveAppRoleAssignmentsAsync(It.IsAny<List<AppRoleAssignment>>(), targetObjectId), Times.Once);
    }

    [Fact]
    public async Task HandlesForbiddenODataError_WhenAddThrows()
    {
        // Arrange
        var logger = new Mock<ILogger<SyncAppRoleAssignmentsService>>();
        var graphClientWrapper = new Mock<IGraphClientWrapper>();

        // Make the first call (from AddPrincipalsService) throw an ODataError with 403
        graphClientWrapper
            .Setup(g => g.GetAllAssignmentsAsync(originalObjectId))
            .ThrowsAsync(new ODataError { ResponseStatusCode = (int)HttpStatusCode.Forbidden });

        // Ensure remove is not called
        graphClientWrapper
            .Setup(g => g.RemoveAppRoleAssignmentsAsync(It.IsAny<List<AppRoleAssignment>>(), targetObjectId))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var loggerForAdd = new Mock<ILogger<AddPrincipalsService>>();
        var loggerForRemove = new Mock<ILogger<RemoveRedundantPrincipalsService>>();
        var loggerForCompare = new Mock<ILogger<ComparePrincipalsService>>();

        var compareService = new ComparePrincipalsService(loggerForCompare.Object);
        var appRoleBuilder = new AppRoleAssignmentBuilderService(graphClientWrapper.Object);

        var addService = new AddPrincipalsService(loggerForAdd.Object, graphClientWrapper.Object, compareService, appRoleBuilder);
        var removeService = new RemoveRedundantPrincipalsService(loggerForRemove.Object, graphClientWrapper.Object, compareService);

        var syncService = new SyncAppRoleAssignmentsService(logger.Object, addService, removeService);

        // Act & Assert: should not throw even though the underlying call throws ODataError
        await syncService.SyncAppRoleAssignmentsAsync(originalObjectId, targetObjectId, dryRun: false);

        // Verify remove was never called because add threw
        graphClientWrapper.Verify(g => g.RemoveAppRoleAssignmentsAsync(It.IsAny<List<AppRoleAssignment>>(), targetObjectId), Times.Never);
    }
}
