using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ODataErrors;

namespace azprism.Services;

public class ReplicateAppRoleAssignmentsService
{
    private readonly ILogger<ReplicateAppRoleAssignmentsService> _logger;
    private readonly AddPrincipalsService _addPrincipalsService;
    private readonly RemovePrincipalsService _removePrincipalsService;

    public ReplicateAppRoleAssignmentsService(
        ILogger<ReplicateAppRoleAssignmentsService> logger,
        AddPrincipalsService addPrincipalsService,
        RemovePrincipalsService removePrincipalsService)
    {
        _logger = logger;
        _addPrincipalsService = addPrincipalsService;
        _removePrincipalsService = removePrincipalsService;
    }

    /// <summary>
    /// Replicates the AppRole assignments from an original service principal into a target service principal.
    /// It fetches the AppRole assignments of a service principal and attempts to assign them to a target service principal.
    /// If the AppRole assignment is not in the original service principal it gets removed from the target service principals.
    /// </summary>
    public async Task ReplicateAppRoleAssignmentsAsync(Guid originalObjectId, Guid targetObjectId, bool dryRun = false)
    {
        try
        {
            await _addPrincipalsService.AddPrincipalsAsync(originalObjectId, targetObjectId, dryRun);
            await _removePrincipalsService.RemovePrincipalsAsync(originalObjectId, targetObjectId, dryRun);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("{Timestamp} - Configuration error: {ErrorMessage}", DateTime.UtcNow, ex.Message);
            throw;
        }
        catch (ODataError odataError) when (odataError.ResponseStatusCode == (int)HttpStatusCode.Forbidden)
        {
            _logger.LogError("{Timestamp} - Service principal does not have the required API permissions: {ErrorMessage}", DateTime.UtcNow, odataError.Error?.Message);
        }
    }
}
