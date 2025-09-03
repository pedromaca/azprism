using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace azprism.Services;

public class RemovePrincipalsService
{
    private readonly GraphServiceClient _graphServiceClient;
    private readonly ILogger<RemovePrincipalsService> _logger;
    private readonly GetAssignmentsService _getAssignmentsService;

    public RemovePrincipalsService(GraphServiceClient graphServiceClient, ILogger<RemovePrincipalsService> logger, GetAssignmentsService getAssignmentsService)
    {
        _graphServiceClient = graphServiceClient;
        _logger = logger;
        _getAssignmentsService = getAssignmentsService;
    }

    /// <summary>
    /// Removes the specified principals from the target service principal.
    /// </summary>
    public async Task RemovePrincipalsAsync(Guid originalObjectId, Guid targetObjectId, bool dryRun = false)
    {
        var originalAssignments = await _getAssignmentsService.GetAllAssignmentsAsync(originalObjectId);
        var targetAssignments = await _getAssignmentsService.GetAllAssignmentsAsync(targetObjectId);

        if (originalAssignments == null || targetAssignments == null)
        {
            _logger.LogError("{Timestamp} - Failed to fetch assignments", DateTime.UtcNow);
            return;
        }

        var originalPrincipalIds = originalAssignments.Select(a => a.PrincipalId).ToHashSet();

        // Handle principals to remove
        var principalsToRemove = targetAssignments
            .Where(a => !originalPrincipalIds.Contains(a.PrincipalId))
            .ToList();
            
        if (principalsToRemove.Count == 0)
        {
            _logger.LogInformation("There are no principals to remove.");
            return;
        }
        else
        {
            _logger.LogInformation("{Prefix}azprism will remove {PrincipalCount} principals.",
                dryRun ? "[DRY RUN] " : "", principalsToRemove.Count);
        }

        // Remove the principals using the dedicated service
        if (!dryRun)
        {
            await Parallel.ForEachAsync(principalsToRemove,
                new ParallelOptions { MaxDegreeOfParallelism = 10 },
                async (assignment, token) =>
                {
                    try
                    {
                        await _graphServiceClient.ServicePrincipals[targetObjectId.ToString()].AppRoleAssignedTo[assignment.Id]
                            .DeleteAsync(cancellationToken: token);

                        _logger.LogInformation("{Timestamp} - Principal {PrincipalDisplayName} removed",
                            DateTime.UtcNow, assignment.PrincipalDisplayName);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(
                            "{Timestamp} - Exception removing principal {PrincipalDisplayName}: {ErrorMessage}",
                            DateTime.UtcNow, assignment.PrincipalDisplayName, e.Message);
                    }
                });
        }
    }
}
