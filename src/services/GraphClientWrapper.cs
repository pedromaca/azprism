using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace azprism.Services;

public class GraphClientWrapper : IGraphClientWrapper
{

    private readonly GraphServiceClient _graphServiceClient;
    private readonly ILogger<GraphClientWrapper> _logger;
    public GraphClientWrapper(GraphServiceClient graphServiceClient, ILogger<GraphClientWrapper> logger)
    {
        _graphServiceClient = graphServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Adds the specified principals to the target application.
    /// </summary>
    public async Task AddAppRoleAssignmentsAsync(List<AppRoleAssignment> appRoleAssignmentRequestBodies, Guid targetObjectId)
        {
        await Parallel.ForEachAsync(
            appRoleAssignmentRequestBodies,
            new ParallelOptions { MaxDegreeOfParallelism = 10 },
            async (appRoleAssignmentRequestBody, token) =>
            {
                try
                {
                    await _graphServiceClient.ServicePrincipals[targetObjectId.ToString()].AppRoleAssignedTo.PostAsync(appRoleAssignmentRequestBody, cancellationToken: token);
                    _logger.LogInformation($"Added principal with ID {appRoleAssignmentRequestBody.PrincipalId}.");
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error adding principal with ID {appRoleAssignmentRequestBody.PrincipalId}: {e.Message}");
                }
            }
        );
    }

    /// <summary>
    /// Removes the specified appRoleAssignments from the target service principal.
    /// </summary>
    public async Task RemoveAppRoleAssignmentsAsync(List<AppRoleAssignment> appRoleAssignments, Guid targetObjectId)
    {
        await Parallel.ForEachAsync(
            appRoleAssignments,
            new ParallelOptions { MaxDegreeOfParallelism = 10 },
            async (appRoleAssignment, token) =>
            {
                try
                {
                    await _graphServiceClient.ServicePrincipals[targetObjectId.ToString()].AppRoleAssignedTo[appRoleAssignment.Id].DeleteAsync(cancellationToken: token);
                    _logger.LogInformation($"Removed principal with ID {appRoleAssignment.PrincipalId}.");
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error removing principal with ID {appRoleAssignment.PrincipalId}: {e.Message}");
                }
            }
        );
    }

    /// <summary>
    /// Fetches all AppRole assignments for a given service principal.
    /// </summary>
    public async Task<List<AppRoleAssignment>> GetAllAssignmentsAsync(Guid objectId)
    {
        var appRoleAssignments = new List<AppRoleAssignment>();
        var firstPage = await _graphServiceClient.ServicePrincipals[objectId.ToString()].AppRoleAssignedTo.GetAsync();
        
        if (firstPage == null)
        {
            return appRoleAssignments;
        }

        var pageIterator = PageIterator<AppRoleAssignment, AppRoleAssignmentCollectionResponse>
            .CreatePageIterator(_graphServiceClient, firstPage, assignment =>
                {
                    appRoleAssignments.Add(assignment);
                    return true; // Continue iterating
                });

        await pageIterator.IterateAsync();

        if (appRoleAssignments == null)
        {
            _logger.LogError("Failed to fetch assignments.");
            return new List<AppRoleAssignment>();
        }
        return appRoleAssignments;
    }

    /// <summary>
    /// Gets the AppRoles for both Service Principals.
    /// Otherwise, builds an AppRoleIds dictionary which contemplates how the Ids on the original are reflected on the target.
    /// e.g.: [Original AppRole "User" Guid: Target AppRole "User" Guid]
    /// </summary>
    public async Task<Dictionary<Guid, Guid>> AppRoleAssignmentMappingAsync(Guid originalObjectId, Guid targetObjectId)
    {
        try
        {
            // Fetch both service principals
            var originalPrincipal = await _graphServiceClient.ServicePrincipals[originalObjectId.ToString()].GetAsync();
            var targetPrincipal = await _graphServiceClient.ServicePrincipals[targetObjectId.ToString()].GetAsync();
    
            var appRoleIdMappings = new Dictionary<Guid, Guid>();

            if (originalPrincipal == null || targetPrincipal == null)
            {
                _logger.LogError("Could not fetch one or both service principals for AppRole mapping.");
                return appRoleIdMappings;
            }

            var originalAppRoles = originalPrincipal.AppRoles ?? new List<AppRole>();
            var targetAppRoles = targetPrincipal.AppRoles ?? new List<AppRole>();
            
            // If the target service principal has no AppRoles, we resort to the default behavior
            if (targetAppRoles.Count == 0)
            {
                _logger.LogInformation("Target service principal has no AppRoles. Resorting to default.");
                return appRoleIdMappings;
            }

            var defaultTargetRole = targetAppRoles.FirstOrDefault();
            if (defaultTargetRole?.Id == null)
            {
                _logger.LogInformation("No valid default role found in target service principal.");
                return appRoleIdMappings;
            }
    
            // Map empty GUID to the first role in target app as default
            appRoleIdMappings[Guid.Empty] = defaultTargetRole.Id.Value;
    
            if (originalAppRoles.Count == 0)
            {
                _logger.LogInformation("Original service principal has no AppRoles, mapping all to default target role");
                foreach (var targetRole in targetAppRoles)
                {
                    if (targetRole.Id.HasValue)
                    {
                        // Create self-mapping for direct role assignments
                        appRoleIdMappings[targetRole.Id.Value] = targetRole.Id.Value;
                    }
                }
                return appRoleIdMappings;
            }
    
            foreach (var originalRole in originalAppRoles)
            {
                if (!originalRole.Id.HasValue) continue;
    
                var matchingTargetRole = targetAppRoles
                    .FirstOrDefault(r => r.DisplayName?.Equals(originalRole.DisplayName, StringComparison.OrdinalIgnoreCase) == true);
                
                if (matchingTargetRole?.Id.HasValue == true)
                {
                    appRoleIdMappings[originalRole.Id.Value] = matchingTargetRole.Id.Value;
                    _logger.LogInformation($"Mapped role {originalRole.DisplayName} to {matchingTargetRole.DisplayName}");
                }
                else
                {
                    // If no matching role found, map to default target role
                    appRoleIdMappings[originalRole.Id.Value] = defaultTargetRole.Id.Value;
                    _logger.LogInformation($"No matching role found for {originalRole.DisplayName}, mapping to default role {defaultTargetRole.DisplayName}");
                }
            }
            
            return appRoleIdMappings;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to initialize AppRole mappings: {ex.Message}");
            return new Dictionary<Guid, Guid>();
        }
    }
}