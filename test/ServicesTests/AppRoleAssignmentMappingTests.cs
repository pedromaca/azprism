using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Moq;
using Azprism.Services;

namespace AzprismTests.ServicesTests;

public class AppRoleAssignmentMappingTests
{
    [Fact]
    public async Task PrincipalsHaveNoAppRoles_ReturnsEmptyMapping()
    {
        // Arrange
        var logger = new Mock<ILogger<AppRoleAssignmentMapping>>();
        var graphClientWrapper = new Mock<IGraphClientWrapper>();
        
        var originalPrincipalId = Guid.NewGuid();
        var targetPrincipalId = Guid.NewGuid();
        
        graphClientWrapper.Setup(g => g.GetAppRolesAsync(originalPrincipalId))
            .ReturnsAsync(new List<AppRole>());
        
        graphClientWrapper.Setup(g => g.GetAppRolesAsync(targetPrincipalId))
            .ReturnsAsync(new List<AppRole>());
        
        var service = new AppRoleAssignmentMapping(logger.Object, graphClientWrapper.Object);
        
        // Act
        var mapping = await service.AppRoleAssignmentMappingAsync(originalPrincipalId, targetPrincipalId);
        
        // Assert
        Assert.Empty(mapping);
    }

    [Fact]
    public async Task PrincipalsHaveDifferentAppRoles_ReturnsMappingToDefaultRole()
    {
        // Arrange
        var logger = new Mock<ILogger<AppRoleAssignmentMapping>>();
        var graphClientWrapper = new Mock<IGraphClientWrapper>();

        var originalPrincipalId = Guid.NewGuid();
        var targetPrincipalId = Guid.NewGuid();

        graphClientWrapper.Setup(g => g.GetAppRolesAsync(originalPrincipalId))
            .ReturnsAsync(new List<AppRole>
            {
                new AppRole { DisplayName = "SampleAppRoleA", Id = Guid.Parse("1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9") },
                new AppRole { DisplayName = "SampleAppRoleA", Id = Guid.Parse("18a4783c-866b-4cc7-a460-3d5e5662c884") }
            });

        graphClientWrapper.Setup(g => g.GetAppRolesAsync(targetPrincipalId))
            .ReturnsAsync(new List<AppRole>
            {
                new AppRole { DisplayName = "DefaultRole", Id = Guid.Empty }
            });

        var service = new AppRoleAssignmentMapping(logger.Object, graphClientWrapper.Object);
        
        var expect = new Dictionary<Guid, Guid>
        {
            { Guid.Empty, Guid.Empty },
            { Guid.Parse("1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9"), Guid.Empty },
            { Guid.Parse("18a4783c-866b-4cc7-a460-3d5e5662c884"), Guid.Empty },
        };

        // Act
        var mapping = await service.AppRoleAssignmentMappingAsync(originalPrincipalId, targetPrincipalId);

        // Assert
        Assert.Equal(expect, mapping);
    }
    
    [Fact]
    public async Task PrincipalsHaveMatchingAppRoles_ReturnsCorrectMapping()
    {
        // Arrange
        var logger = new Mock<ILogger<AppRoleAssignmentMapping>>();
        var graphClientWrapper = new Mock<IGraphClientWrapper>();

        var originalPrincipalId = Guid.NewGuid();
        var targetPrincipalId = Guid.NewGuid();

        graphClientWrapper.Setup(g => g.GetAppRolesAsync(originalPrincipalId))
            .ReturnsAsync(new List<AppRole>
            {
                new AppRole { DisplayName = "SampleAppRoleA", Id = Guid.Parse("1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9") },
                new AppRole { DisplayName = "SampleAppRoleA", Id = Guid.Parse("18a4783c-866b-4cc7-a460-3d5e5662c884") }
            });

        graphClientWrapper.Setup(g => g.GetAppRolesAsync(targetPrincipalId))
            .ReturnsAsync(new List<AppRole>
            {
                new AppRole { DisplayName = "SampleAppRoleA", Id = Guid.Parse("1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9") },
                new AppRole { DisplayName = "SampleAppRoleA", Id = Guid.Parse("18a4783c-866b-4cc7-a460-3d5e5662c884") }
            });

        var service = new AppRoleAssignmentMapping(logger.Object, graphClientWrapper.Object);
        
        var expect = new Dictionary<Guid, Guid>
        {
            { Guid.Empty, Guid.Parse("1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9") },
            { Guid.Parse("1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9"), Guid.Parse("1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9") },
            { Guid.Parse("18a4783c-866b-4cc7-a460-3d5e5662c884"), Guid.Parse("1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9") },
        };

        // Act
        var mapping = await service.AppRoleAssignmentMappingAsync(originalPrincipalId, targetPrincipalId);

        // Assert
        Assert.Equal(expect, mapping);
    }

    [Fact]
    public async Task OriginalPrincipalHasNoRoles_ReturnsSelfMapping()
    {
        {
            // Arrange
            var logger = new Mock<ILogger<AppRoleAssignmentMapping>>();
            var graphClientWrapper = new Mock<IGraphClientWrapper>();

            var originalPrincipalId = Guid.NewGuid();
            var targetPrincipalId = Guid.NewGuid();

            graphClientWrapper.Setup(g => g.GetAppRolesAsync(originalPrincipalId))
                .ReturnsAsync(new List<AppRole>{});

            graphClientWrapper.Setup(g => g.GetAppRolesAsync(targetPrincipalId))
                .ReturnsAsync(new List<AppRole>
                {
                    new AppRole { DisplayName = "SampleAppRoleA", Id = Guid.Parse("1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9") },
                    new AppRole { DisplayName = "SampleAppRoleA", Id = Guid.Parse("18a4783c-866b-4cc7-a460-3d5e5662c884") }
                });

            var service = new AppRoleAssignmentMapping(logger.Object, graphClientWrapper.Object);
        
            var expect = new Dictionary<Guid, Guid>
            {
                { Guid.Empty, Guid.Parse("1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9") },
                { Guid.Parse("1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9"), Guid.Parse("1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9") },
                { Guid.Parse("18a4783c-866b-4cc7-a460-3d5e5662c884"), Guid.Parse("18a4783c-866b-4cc7-a460-3d5e5662c884") },
            };

            // Act
            var mapping = await service.AppRoleAssignmentMappingAsync(originalPrincipalId, targetPrincipalId);

            // Assert
            Assert.Equal(expect, mapping);
        }
    }
}