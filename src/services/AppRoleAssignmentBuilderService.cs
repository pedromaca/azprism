

using Microsoft.Graph.Models;

namespace azprism.Services;

public class AppRoleAssignmentBuilderService
{
    private readonly IGraphClientWrapper _graphClientWrapper;
    public AppRoleAssignmentBuilderService(IGraphClientWrapper graphClientWrapper)
    {
        _graphClientWrapper = graphClientWrapper;
    }
    public async Task<List<AppRoleAssignment>> BuildAppRoleAssignment(List<AppRoleAssignment> principalsToAdd, Guid originalObjectId, Guid targetObjectId)
    {
        // Initialize the AppRole mappings using the dedicated service
        var appRoleIdMappings = await _graphClientWrapper.AppRoleAssignmentMappingAsync(originalObjectId.ToString(), targetObjectId.ToString());

        // Will hold the final request bodies
        var appRoleAssignmentRequestBodies = new List<AppRoleAssignment>();

        await Parallel.ForEachAsync(principalsToAdd,
                new ParallelOptions { MaxDegreeOfParallelism = 10 },
                async (assignment, token) =>
                {
                    // targetAppRoleId is the AppRoleId to be assigned to the principal
                    Guid targetAppRoleId = Guid.Empty;
                    
                    // If the AppRoleId is found in the mappings, use the mapped id. Otherwise, use empty Guid
                    if (assignment.AppRoleId != null && appRoleIdMappings.TryGetValue(assignment.AppRoleId.Value, out var mappedId))
                    {
                        targetAppRoleId = mappedId;
                    }
                    
                    // Finally, build the request body
                    var requestBody = new AppRoleAssignment
                    {
                        PrincipalId = assignment.PrincipalId,
                        ResourceId = Guid.Parse(targetObjectId.ToString()),
                        AppRoleId = targetAppRoleId
                    };
                    
                    appRoleAssignmentRequestBodies.Add(requestBody);
                });

        return appRoleAssignmentRequestBodies;
    }
}