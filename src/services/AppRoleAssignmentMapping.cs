using Microsoft.Extensions.Logging;

namespace Azprism.Services;

public class AppRoleAssignmentMapping : IAppRoleAssignmentMapping
{
    private readonly ILogger<AppRoleAssignmentMapping> _logger;
    private readonly IGraphClientWrapper _graphClientWrapper;
    
    public AppRoleAssignmentMapping(ILogger<AppRoleAssignmentMapping> logger, IGraphClientWrapper graphClientWrapper)
    {
        _logger = logger;
        _graphClientWrapper = graphClientWrapper;
    }
    
    /// <summary>
    /// Gets the AppRoles for both Service Principals.
    /// Otherwise, builds an AppRoleIds dictionary which contemplates how the Ids on the original are reflected on the target.
    /// e.g.: [Original AppRole "User" Guid: Target AppRole "User" Guid]
    /// </summary>
    public async Task<Dictionary<Guid, Guid>> AppRoleAssignmentMappingAsync(Guid originalObjectId, Guid targetObjectId)
    {
        // Fetch app roles for both service principals
        var originalAppRoles = await _graphClientWrapper.GetAppRolesAsync(originalObjectId);
        var targetAppRoles = await _graphClientWrapper.GetAppRolesAsync(targetObjectId);
        
        var appRoleIdMappings = new Dictionary<Guid, Guid>();
        
        // If the target service principal has no AppRoles, we resort to the default behavior
        if (targetAppRoles.Count == 0)
        {
            _logger.LogInformation("Target service principal has no AppRoles. Resorting to default.");
            return appRoleIdMappings;
        }
        
        // Determine default target role
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
}