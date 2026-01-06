using Azure.Identity;
using Microsoft.Graph;

namespace Azprism.Services;

public class GraphClientFactoryResult
{
    public bool Success { get; init; }
    public GraphServiceClient? Client { get; init; }
    public Guid ClientId { get; init; }
    public string? ErrorMessage { get; init; }
}

public static class GraphClientFactory
{
    public static async Task<GraphClientFactoryResult> CreateAsync()
    {
        // Validate required environment variables are present
        var tenantIdEnv = Environment.GetEnvironmentVariable("TENANT_ID");
        var clientIdEnv = Environment.GetEnvironmentVariable("CLIENT_ID");
        var clientSecretEnv = Environment.GetEnvironmentVariable("CLIENT_SECRET");

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(tenantIdEnv)) missing.Add("TENANT_ID");
        if (string.IsNullOrWhiteSpace(clientIdEnv)) missing.Add("CLIENT_ID");
        if (string.IsNullOrWhiteSpace(clientSecretEnv)) missing.Add("CLIENT_SECRET");

        if (missing.Count > 0)
        {
            return new GraphClientFactoryResult
            {
                Success = false,
                ErrorMessage = "Missing required environment variables: " + string.Join(", ", missing)
            };
        }

        // Validate TENANT_ID and CLIENT_ID are valid GUIDs
        var validationErrors = new List<string>();
        if (!Guid.TryParse(tenantIdEnv, out var tenantId))
        {
            validationErrors.Add($"TENANT_ID '{tenantIdEnv}' is not a valid GUID format.");
        }
        if (!Guid.TryParse(clientIdEnv, out var clientId))
        {
            validationErrors.Add($"CLIENT_ID '{clientIdEnv}' is not a valid GUID format.");
        }

        if (validationErrors.Count > 0)
        {
            return new GraphClientFactoryResult
            {
                Success = false,
                ErrorMessage = string.Join(" ", validationErrors)
            };
        }

        // Validate credentials by attempting to authenticate
        try
        {
            var credentials = new ClientSecretCredential(tenantId.ToString(), clientId.ToString(), clientSecretEnv);
            string[] scopes = ["https://graph.microsoft.com/.default"];
            var graphClient = new GraphServiceClient(credentials, scopes);

            // Test the credentials by making a simple request
            // This will throw if the client doesn't exist or the secret is invalid
            await graphClient.ServicePrincipalsWithAppId(clientId.ToString()).GetAsync();

            return new GraphClientFactoryResult
            {
                Success = true,
                Client = graphClient,
                ClientId = clientId
            };
        }
        catch (Exception ex)
        {
            return new GraphClientFactoryResult
            {
                Success = false,
                ErrorMessage = $"Failed to authenticate with Microsoft Graph: {ex.Message}"
            };
        }
    }
}
