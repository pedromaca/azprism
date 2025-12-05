using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Azprism.Services;

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
    /// Check if the service principal has the necessary permissions.
    /// </summary>
    public async Task<List<AppRoleAssignment>> GetAppRoleAssignments(Guid principalId)
    {
        // Fetch the service principal by AppId to get its ObjectId
        var principal = await _graphServiceClient.ServicePrincipalsWithAppId(principalId.ToString()).GetAsync();
        if (principal == null)
        {
            _logger.LogError($"Service principal with AppId {principalId} not found.");
            return new List<AppRoleAssignment>();
        }
        
        var objectId = principal.Id;
        var appRoleAssignments = await _graphServiceClient.ServicePrincipals[objectId].AppRoleAssignments.GetAsync();
        if (appRoleAssignments?.Value == null)
        {
            _logger.LogError($"No AppRole assignments found for principal with ObjectId {objectId}.");
            return new List<AppRoleAssignment>();
        }

        return appRoleAssignments.Value;
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
        
        return appRoleAssignments;
    }
    
    /// <summary>
    /// Fetches all AppRoles for a given service principal.
    /// </summary>
    public async Task<List<AppRole>> GetAppRolesAsync(Guid objectId)
    {
        var servicePrincipal = await _graphServiceClient.ServicePrincipals[objectId.ToString()].GetAsync();
        return servicePrincipal?.AppRoles?.ToList() ?? new List<AppRole>();
    }
}