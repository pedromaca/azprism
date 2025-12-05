using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ODataErrors;

namespace Azprism.Services;

public class SyncAppRoleAssignmentsService : ISyncAppRoleAssignmentsService
{
    private readonly ILogger<SyncAppRoleAssignmentsService> _logger;
    private readonly IAddPrincipalsService _addPrincipalsService;
    private readonly IRemoveRedundantPrincipalsService _removeRedundantPrincipalsService;

    public SyncAppRoleAssignmentsService(
        ILogger<SyncAppRoleAssignmentsService> logger,
        IAddPrincipalsService addPrincipalsService,
        IRemoveRedundantPrincipalsService removeRedundantPrincipalsService)
    {
        _logger = logger;
        _addPrincipalsService = addPrincipalsService;
        _removeRedundantPrincipalsService = removeRedundantPrincipalsService;
    }

    /// <summary>
    /// Replicates the AppRole assignments from an original service principal into a target service principal.
    /// It fetches the AppRole assignments of a service principal and attempts to assign them to a target service principal.
    /// If the AppRole assignment is not in the original service principal it gets removed from the target service principals.
    /// </summary>
    public async Task SyncAppRoleAssignmentsAsync(Guid originalObjectId, Guid targetObjectId, bool dryRun = false)
    {
        try
        {
            await _addPrincipalsService.AddPrincipalsAsync(originalObjectId, targetObjectId, dryRun);
            await _removeRedundantPrincipalsService.RemoveRedundantPrincipalsAsync(originalObjectId, targetObjectId, dryRun);
        }
        catch (ODataError odataError) when (odataError.ResponseStatusCode == (int)HttpStatusCode.Forbidden)
        {
            _logger.LogError($"Service principal does not have the required API permissions: {odataError.Error?.Message}");
        }
    }
}
