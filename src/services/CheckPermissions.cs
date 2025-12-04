using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

namespace azprism.Services;

public class CheckPermissions
{
    private readonly ILogger<CheckPermissions> _logger;
    private readonly IGraphClientWrapper _graphClientWrapper;
    
    public CheckPermissions(ILogger<CheckPermissions> logger, IGraphClientWrapper graphClientWrapper)
    {
        _logger = logger;
        _graphClientWrapper = graphClientWrapper;
    }
    
    private readonly List<AppRoleAssignment> _acceptedAppRoleAssignments = new List<AppRoleAssignment>
    {
        new AppRoleAssignment { AppRoleId = Guid.Parse("1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9") },
        new AppRoleAssignment { AppRoleId = Guid.Parse("18a4783c-866b-4cc7-a460-3d5e5662c884") },
        new AppRoleAssignment { AppRoleId = Guid.Parse("06b708a9-e830-4db3-a914-8e69da51d44f") }
    };
    
    public async Task<bool> PrincipalHasPermissions(Guid principalId)
    {
        var principalAppRoleAssignments = await _graphClientWrapper.GetAppRoleAssignments(principalId);
        
        foreach (var appRoleAssignment in principalAppRoleAssignments)
        {
            if (_acceptedAppRoleAssignments.Any(accepted => accepted.AppRoleId == appRoleAssignment.AppRoleId))
                return true;
        }
        
        _logger.LogWarning("Principal has no permissions");
        return false;
    }
}
