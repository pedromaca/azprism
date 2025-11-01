using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace azprism.Services;

public class ResetPrincipalsService
{
    private readonly GraphServiceClient _graphServiceClient;
    private readonly ILogger<ResetPrincipalsService> _logger;
    private readonly GetAssignmentsService _getAssignmentsService;

    public ResetPrincipalsService(GraphServiceClient graphServiceClient, ILogger<ResetPrincipalsService> logger, GetAssignmentsService getAssignmentsService)
    {
        _graphServiceClient = graphServiceClient;
        _logger = logger;
        _getAssignmentsService = getAssignmentsService;
    }

    /// <summary>
    /// Removes all principals from the target service principal.
    /// </summary>
    public async Task ResetPrincipalsAsync(Guid targetObjectId, bool dryRun = false)
    {
        var targetAssignments = await _getAssignmentsService.GetAllAssignmentsAsync(targetObjectId);

        if (targetAssignments == null)
        {
            _logger.LogError("{Timestamp} - Failed to fetch assignments for target {TargetObjectId}", DateTime.UtcNow, targetObjectId);
            return;
        }

        if (targetAssignments.Count == 0)
        {
            _logger.LogInformation("There are no principals to remove from target {TargetObjectId}.", targetObjectId);
            return;
        }

        _logger.LogInformation("{Prefix}azprism will remove {PrincipalCount} principals from target {TargetObjectId}.",
            dryRun ? "[DRY RUN] " : "", targetAssignments.Count, targetObjectId);

        // Remove all principals from the target
        if (!dryRun)
        {
            await Parallel.ForEachAsync(targetAssignments,
                new ParallelOptions { MaxDegreeOfParallelism = 10 },
                async (assignment, token) =>
                {
                    try
                    {
                        await _graphServiceClient.ServicePrincipals[targetObjectId.ToString()].AppRoleAssignedTo[assignment.Id]
                            .DeleteAsync(cancellationToken: token);

                        _logger.LogInformation("{Timestamp} - Principal {PrincipalDisplayName} removed from target",
                            DateTime.UtcNow, assignment.PrincipalDisplayName);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(
                            "{Timestamp} - Exception removing principal {PrincipalDisplayName} from target {TargetObjectId}: {ErrorMessage}",
                            DateTime.UtcNow, assignment.PrincipalDisplayName, targetObjectId, e.Message);
                    }
                });
        }
    }
}
