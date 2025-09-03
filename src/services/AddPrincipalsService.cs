using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace azprism.Services;

public class AddPrincipalsService
{
    private readonly GraphServiceClient _graphServiceClient;
    private readonly ILogger<AddPrincipalsService> _logger;
    private readonly AppRoleMappingsService _appRoleMappingsService;
    private readonly GetAssignmentsService _getAssignmentsService;

    public AddPrincipalsService(
        GraphServiceClient graphServiceClient, 
        ILogger<AddPrincipalsService> logger,
        AppRoleMappingsService appRoleMappingsService,
        GetAssignmentsService getAssignmentsService)
    {
        _graphServiceClient = graphServiceClient;
        _logger = logger;
        _appRoleMappingsService = appRoleMappingsService;
        _getAssignmentsService = getAssignmentsService;
    }

    /// <summary>
    /// Adds the specified principals to the target application.
    /// </summary>
    public async Task AddPrincipalsAsync(Guid originalObjectId, Guid targetObjectId, bool dryRun = false)
    {
        var originalAssignments = await _getAssignmentsService.GetAllAssignmentsAsync(originalObjectId);
        var targetAssignments = await _getAssignmentsService.GetAllAssignmentsAsync(targetObjectId);

        if (originalAssignments == null || targetAssignments == null)
        {
            _logger.LogError("{Timestamp} - Failed to fetch assignments", DateTime.UtcNow);
            return;
        }

        var targetPrincipalIds = targetAssignments.Select(a => a.PrincipalId).ToHashSet();

        // Handle principals to add
        var principalsToAdd = originalAssignments
            .Where(a => !targetPrincipalIds.Contains(a.PrincipalId))
            .ToList();
        
        if (principalsToAdd.Count == 0)
        {
            _logger.LogInformation("There are no principals to add.");
            return;
        }
        _logger.LogInformation("{Prefix}azprism will add {PrincipalCount} principals.", dryRun ? "[DRY RUN] " : "", principalsToAdd.Count);

        if (!dryRun)
        {
            // Initialize the AppRole mappings using the dedicated service
            var appRoleIdMappings =
                await _appRoleMappingsService.InitializeAppRoleMappingsAsync(originalObjectId.ToString(), targetObjectId.ToString());

            // Add the principals asynchronously
            await Parallel.ForEachAsync(principalsToAdd,
                new ParallelOptions { MaxDegreeOfParallelism = 10 },
                async (assignment, token) =>
                {
                    try
                    {
                        // targetAppRoleId is the AppRoleId to be assigned to the principal
                        Guid targetAppRoleId;

                        // If the AppRoleId is found in the mappings, use the mapped id
                        if (assignment.AppRoleId != null &&
                            appRoleIdMappings.TryGetValue(assignment.AppRoleId.Value, out var mappedId))
                        {
                            targetAppRoleId = mappedId;
                        }
                        // If the AppRoleId is not found in the mappings, use Guid.Empty
                        else
                        {
                            targetAppRoleId = Guid.Empty;
                        }

                        // Finally, build the request body
                        var requestBody = new AppRoleAssignment
                        {
                            PrincipalId = assignment.PrincipalId,
                            ResourceId = Guid.Parse(targetObjectId.ToString()),
                            AppRoleId = targetAppRoleId
                        };

                        await _graphServiceClient.ServicePrincipals[targetObjectId.ToString()].AppRoleAssignedTo
                            .PostAsync(requestBody, cancellationToken: token);

                        _logger.LogInformation(
                            "{Timestamp} - Principal {PrincipalDisplayName} assigned with AppRoleId {AppRoleId}",
                            DateTime.UtcNow, assignment.PrincipalDisplayName, requestBody.AppRoleId);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(
                            "{Timestamp} - Exception assigning principal {PrincipalDisplayName} AppRoleId {AppRoleId}: {ErrorMessage}",
                            DateTime.UtcNow, assignment.PrincipalDisplayName, assignment.AppRoleId, e.Message);
                    }
                });
        }
    }
}
