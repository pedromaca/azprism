using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace azprism.Services;

public class GetAssignmentsService
{
    private readonly GraphServiceClient _graphServiceClient;

    public GetAssignmentsService(GraphServiceClient graphServiceClient)
    {
        _graphServiceClient = graphServiceClient;
    }

    /// <summary>
    /// Fetches all AppRole assignments for a given service principal using proper pagination.
    /// </summary>
    public async Task<List<AppRoleAssignment>> GetAllAssignmentsAsync(Guid objectId)
    {
        var allAssignments = new List<AppRoleAssignment>();
        var firstPage = await _graphServiceClient.ServicePrincipals[objectId.ToString()].AppRoleAssignedTo.GetAsync();
        
        if (firstPage == null)
        {
            return allAssignments;
        }

        var pageIterator = PageIterator<AppRoleAssignment, AppRoleAssignmentCollectionResponse>
            .CreatePageIterator(
                _graphServiceClient,
                firstPage,
                (assignment) =>
                {
                    allAssignments.Add(assignment);
                    return true; // Continue iterating
                });

        await pageIterator.IterateAsync();
        return allAssignments;
    }
}
