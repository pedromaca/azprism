using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Moq;
using azprism.Services;

namespace AzprismTests.ServicesTests;

public class ComparePrincipalsServiceTests
{
    public List<AppRoleAssignment> originalARA = new List<AppRoleAssignment>{
            new AppRoleAssignment{
                AppRoleId   = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
                PrincipalId = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
                ResourceId  = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef")
            }
        };
    public List<AppRoleAssignment> targetARA = new();

    [Fact]
    public void ComparePrincipals_ReturnsExtraInTarget()
    {
        // Arrange
        var logger  = new Mock<ILogger<ComparePrincipalsService>>();
        var service = new ComparePrincipalsService(logger.Object);

        var expect = new List<AppRoleAssignment>(); // Expecting no extra assignments in target

        // Act
        var (extraInTarget, _) = service.ComparePrincipals(originalARA, targetARA);

        // Assert
        Assert.Equal(expect, extraInTarget);
    }

    [Fact]
    public void ComparePrincipals_ReturnsMissingInTarget()
    {
        // Arrange
        var logger  = new Mock<ILogger<ComparePrincipalsService>>();
        var service = new ComparePrincipalsService(logger.Object);

        targetARA.Add(new AppRoleAssignment{
            AppRoleId   = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
            PrincipalId = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
            ResourceId  = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef")
        });

        var expect = new List<AppRoleAssignment>(); // Expecting no missing assignments in target

        // Act
        var (_, missingInTarget) = service.ComparePrincipals(originalARA, targetARA);

        // Assert
        Assert.Equal(expect, missingInTarget);
    }

    [Fact]
    public void ComparePrincipals_ReturnsNoDifference()
    {
        // Arrange
        var logger  = new Mock<ILogger<ComparePrincipalsService>>();
        var service = new ComparePrincipalsService(logger.Object);

        targetARA.Add(new AppRoleAssignment{
            AppRoleId   = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
            PrincipalId = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
            ResourceId  = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef")
        });

        // Act
        var (extraInTarget, missingInTarget) = service.ComparePrincipals(originalARA, targetARA);

        // Assert
        Assert.Equal(extraInTarget, missingInTarget); // Both should be empty
    }

    [Fact]
    public void ComparePrincipals_DetectsDifferentGuids()
    {
        var logger  = new Mock<ILogger<ComparePrincipalsService>>();
        var service = new ComparePrincipalsService(logger.Object);

        // Same appRoleId and resourceId but different principalId
        targetARA.Add(new AppRoleAssignment{
            AppRoleId   = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
            PrincipalId = Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210"),
            ResourceId  = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef")
        });

        var (_, missingInTarget) = service.ComparePrincipals(originalARA, targetARA);

        Assert.Single(missingInTarget);
    }
}
