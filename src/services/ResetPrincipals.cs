using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

namespace azprism.Services;

public class ResetPrincipalsService : IResetPrincipalsService
{
    private readonly IGraphClientWrapper _graphClientWrapper;
    private readonly ILogger<ResetPrincipalsService> _logger;

    public ResetPrincipalsService(IGraphClientWrapper graphClientWrapper, ILogger<ResetPrincipalsService> logger)
    {
        _graphClientWrapper = graphClientWrapper;
        _logger = logger;
    }

    /// <summary>
    /// Removes all principals from the target service principal.
    /// </summary>
    public async Task ResetPrincipalsAsync(Guid targetObjectId, bool dryRun = false)
    {
        var targetAssignments = await _graphClientWrapper.GetAllAssignmentsAsync(targetObjectId) ?? new List<AppRoleAssignment>();

        if (targetAssignments.Count == 0)
        {
            _logger.LogInformation($"There are no principals to remove from target {targetObjectId}.");
            return;
        }

        _logger.LogInformation($"{(dryRun ? "[DRY RUN] " : "")}azprism will remove {targetAssignments.Count} principals from target {targetObjectId}.");

        if (dryRun) return;
        
        // Remove all principals from the target
        await _graphClientWrapper.RemoveAppRoleAssignmentsAsync(targetAssignments, targetObjectId);
    }
}