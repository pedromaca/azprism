using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace azprism.Services;

public class AppRoleMappingsService
{
    private readonly GraphServiceClient _graphServiceClient;
    private readonly ILogger<AppRoleMappingsService> _logger;

    public AppRoleMappingsService(GraphServiceClient graphServiceClient, ILogger<AppRoleMappingsService> logger)
    {
        _graphServiceClient = graphServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets the AppRoles for both Service Principals.
    /// Otherwise, builds an AppRoleIds dictionary which contemplates how the Ids on the original are reflected on the target.
    /// e.g.: [Original AppRole "User" Guid: Target AppRole "User" Guid]
    /// </summary>
    public async Task<Dictionary<Guid, Guid>> InitializeAppRoleMappingsAsync(string originalObjectId, string targetObjectId)
    {
        try
        {
            var originalSp = await _graphServiceClient.ServicePrincipals[originalObjectId].GetAsync();
            var targetSp = await _graphServiceClient.ServicePrincipals[targetObjectId].GetAsync();
    
            var appRoleIdMappings = new Dictionary<Guid, Guid>();
            
            if (targetSp?.AppRoles == null)
            {
                _logger.LogInformation("Could not fetch AppRoles for target service principal");
                return appRoleIdMappings;
            }
            
            // If the target service principal has no AppRoles, we resort to the default behavior
            if (targetSp.AppRoles.Count == 0)
            {
                _logger.LogInformation("Target service principal has no AppRoles. Resorting to default.");
                return appRoleIdMappings;
            }
    
            var defaultTargetRole = targetSp.AppRoles.FirstOrDefault();
            if (defaultTargetRole?.Id == null)
            {
                _logger.LogInformation("No valid default role found in target service principal");
                return appRoleIdMappings;
            }
    
            // Map empty GUID to the first role in target app as default
            appRoleIdMappings[Guid.Empty] = defaultTargetRole.Id.Value;
    
            if (originalSp?.AppRoles == null || originalSp.AppRoles.Count == 0)
            {
                _logger.LogInformation("Original service principal has no AppRoles, mapping all to default target role");
                foreach (var targetRole in targetSp.AppRoles)
                {
                    if (targetRole.Id.HasValue)
                    {
                        // Create self-mapping for direct role assignments
                        appRoleIdMappings[targetRole.Id.Value] = targetRole.Id.Value;
                    }
                }
                return appRoleIdMappings;
            }
    
            foreach (var originalRole in originalSp.AppRoles)
            {
                if (!originalRole.Id.HasValue) continue;
    
                var matchingTargetRole = targetSp.AppRoles
                    .FirstOrDefault(r => r.DisplayName?.Equals(originalRole.DisplayName, StringComparison.OrdinalIgnoreCase) == true);
                
                if (matchingTargetRole?.Id.HasValue == true)
                {
                    appRoleIdMappings[originalRole.Id.Value] = matchingTargetRole.Id.Value;
                    _logger.LogInformation("Mapped role {OriginalRole} to {TargetRole}", 
                        originalRole.DisplayName, matchingTargetRole.DisplayName);
                }
                else
                {
                    // If no matching role found, map to default target role
                    appRoleIdMappings[originalRole.Id.Value] = defaultTargetRole.Id.Value;
                    _logger.LogInformation("No matching role found for {OriginalRole}, mapping to default role {DefaultRole}", 
                        originalRole.DisplayName, defaultTargetRole.DisplayName);
                }
            }
            
            return appRoleIdMappings;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to initialize AppRole mappings: {Message}", ex.Message);
            return new Dictionary<Guid, Guid>();
        }
    }
}
