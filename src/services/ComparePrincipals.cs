using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

namespace Azprism.Services;

public class ComparePrincipalsService : IComparePrincipals
{
    private readonly ILogger<ComparePrincipalsService> _logger;
    public ComparePrincipalsService(ILogger<ComparePrincipalsService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Compares principals between the original and target service principals.
    /// </summary>
    public (List<AppRoleAssignment> ExtraInTarget, List<AppRoleAssignment> MissingInTarget) ComparePrincipals(List<AppRoleAssignment> originalAssignments, List<AppRoleAssignment> targetAssignments)
    {
        // Principals to remove from target (extra in target)
        var originalPrincipalIds = originalAssignments.Select(a => a.PrincipalId).ToHashSet();
        var extraInTarget = targetAssignments
            .Where(a => !originalPrincipalIds.Contains(a.PrincipalId))
            .ToList();

        // Principals to add to target (missing in target)
        var targetPrincipalIds = targetAssignments.Select(a => a.PrincipalId).ToHashSet();
        var missingInTarget = originalAssignments
            .Where(a => !targetPrincipalIds.Contains(a.PrincipalId))
            .ToList();

        if (missingInTarget.Count == 0 && extraInTarget.Count == 0)
        {
            _logger.LogInformation("There is no difference in assignments between the service principals.");
        }

        return (extraInTarget, missingInTarget);
    }
}