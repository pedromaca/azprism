using Microsoft.Extensions.Logging;

namespace azprism.Services;

public class RemoveRedundantPrincipalsService : IRemoveRedundantPrincipalsService
{
    private readonly ILogger<RemoveRedundantPrincipalsService> _logger;
    private readonly IGraphClientWrapper _graphClientWrapper;
    private readonly ComparePrincipalsService _comparePrincipalsService;

    public RemoveRedundantPrincipalsService(ILogger<RemoveRedundantPrincipalsService> logger, IGraphClientWrapper graphClientWrapper, ComparePrincipalsService comparePrincipalsService)
    {
        _logger = logger;
        _graphClientWrapper = graphClientWrapper;
        _comparePrincipalsService = comparePrincipalsService;
    }

    /// <summary>
    /// Removes the specified principals from the target service principal.
    /// </summary>
    public async Task RemoveRedundantPrincipalsAsync(Guid originalObjectId, Guid targetObjectId, bool dryRun = false)
    {
        // Fetch assignments for both original and target service principals
        var originalAssignments = await _graphClientWrapper.GetAllAssignmentsAsync(originalObjectId);
        var targetAssignments = await _graphClientWrapper.GetAllAssignmentsAsync(targetObjectId);

        // Compare principals to identify which ones need to be removed
        var (principalsToRemove, _) = _comparePrincipalsService.ComparePrincipals(originalAssignments, targetAssignments);

        _logger.LogInformation($"{(dryRun ? "[DRY RUN] " : "")}azprism will remove {principalsToRemove.Count} principals.");

        if (dryRun) return;

        if (principalsToRemove.Count == 0) return;

        // Remove the extra principals
        await _graphClientWrapper.RemoveAppRoleAssignmentsAsync(principalsToRemove, targetObjectId);
    }
}
