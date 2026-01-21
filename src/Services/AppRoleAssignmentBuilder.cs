using Microsoft.Graph.Models;

namespace Azprism.Services;

public class AppRoleAssignmentBuilderService : IAppRoleAssignmentBuilder
{
    private readonly IAppRoleAssignmentMapping _appRoleAssignmentMapping;
    public AppRoleAssignmentBuilderService(IAppRoleAssignmentMapping appRoleAssignmentMapping)
    {
        _appRoleAssignmentMapping = appRoleAssignmentMapping;
    }
    
    public async Task<List<AppRoleAssignment>> BuildAppRoleAssignment(List<AppRoleAssignment> principalsToAdd, Guid originalObjectId, Guid targetObjectId)
    {
        // Initialize the AppRole mappings using the dedicated service
        var appRoleIdMappings = await _appRoleAssignmentMapping.AppRoleAssignmentMappingAsync(originalObjectId, targetObjectId);

        // Will hold the final request bodies
        var appRoleAssignmentRequestBodies = new List<AppRoleAssignment>();

        foreach (var assignment in principalsToAdd)
        {
            // targetAppRoleId is the AppRoleId to be assigned to the principal
            var targetAppRoleId = Guid.Empty;
            
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
        }

        return appRoleAssignmentRequestBodies;
    }
}