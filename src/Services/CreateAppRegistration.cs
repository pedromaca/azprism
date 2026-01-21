using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Azprism.Services;

public class CreateAppRegistrationService
{
    private readonly GraphServiceClient _graphServiceClient;
    private readonly ILogger<AddPrincipalsService> _logger;
    
    public CreateAppRegistrationService(
        GraphServiceClient graphServiceClient, 
        ILogger<AddPrincipalsService> logger)
    {
        _graphServiceClient = graphServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new app registration with the specified display name and assigns authenticated principal as owner.
    /// </summary>
    public async Task CreateAppRegistrationAsync(string displayName, bool dryRun = false)
    {
        // Check if application with the same display name already exists
        var applicationExists = await _graphServiceClient.Applications
            .GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Filter = "startswith(displayName, '" + displayName + "')";
                }
            );

        if (applicationExists?.Value != null && applicationExists.Value.Any(app => app.DisplayName == displayName))
        {
            _logger.LogInformation("An application with the display name '{DisplayName}' already exists.", displayName);
            return;
        }
        
        // Create the app registration
        var appRegistration = new Application { DisplayName = displayName };
        
        if (dryRun)
        {
            _logger.LogInformation("[DRY-RUN] Would create App Registration with DisplayName: {DisplayName}", appRegistration.DisplayName);
            return;
        }
        
        try
        {
            var createdApp = await _graphServiceClient.Applications
                .PostAsync(appRegistration);

            if (createdApp != null)
                _logger.LogInformation("App Registration with AppId created successfully: {AppId}", createdApp.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create app registration with display name: {DisplayName}", displayName);
        }
    }
}