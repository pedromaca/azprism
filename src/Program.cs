using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using Azure.Identity;
using Microsoft.Graph;
using azprism.Services;

// CLI Flags
var originalIdOption = new Option<Guid>(
    name: "--original-id",
    description: "The original object ID to sync from")
    { IsRequired = true };

var targetIdOption = new Option<Guid>(
    name: "--target-id",
    description: "The target object ID to sync to")
    { IsRequired = true };

var displayNameOption = new Option<string>(
        name: "--display-name",
        description: "The display name for the app registration")
    { IsRequired = true, ArgumentHelpName = "App Registration Display Name" };

var dryRunOption = new Option<bool>(
    name: "--dry-run",
    description: "Perform a dry run without making changes")
    { IsRequired = false, ArgumentHelpName = "false" };

// Host builder function
IHost BuildHost() =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((services) =>
        {
            services.AddSingleton(_ => {
                var tenantId = GetRequiredEnvVar("TENANT_ID");
                var clientId = GetRequiredEnvVar("CLIENT_ID");
                var clientSecret = GetRequiredEnvVar("CLIENT_SECRET");
                var credentials = new ClientSecretCredential(tenantId, clientId, clientSecret);
                string[] scopes = ["https://graph.microsoft.com/.default"];
                return new GraphServiceClient(credentials, scopes);

                string GetRequiredEnvVar(string name)
                {
                    var value = Environment.GetEnvironmentVariable(name);
                    if (string.IsNullOrEmpty(value))
                        throw new ArgumentException($"Required environment variable '{name}' is not defined or empty.");
                    return value;
                }
            });

            services.AddTransient<RemovePrincipalsService>();
            services.AddTransient<ResetPrincipalsService>();
            services.AddTransient<AppRoleMappingsService>();
            services.AddTransient<AddPrincipalsService>();
            services.AddTransient<GetAssignmentsService>();
            services.AddTransient<ReplicateAppRoleAssignmentsService>();
            services.AddTransient<CreateAppRegistrationService>();
            services.AddLogging();
        })
        .Build();

var host = BuildHost();

// Command definitions
// Parent command: principals
var principalsCommand = new Command("principals", "Manage principal assignments across origin/target objects");

// Subcommand: add
var addPrincipalsCommand = new Command("add", "Add missing principals from original to target");
addPrincipalsCommand.AddOption(originalIdOption);
addPrincipalsCommand.AddOption(targetIdOption);
addPrincipalsCommand.AddOption(dryRunOption);
addPrincipalsCommand.SetHandler(async (originalId, targetId, dryRun) =>
    {
        var addService = host.Services.GetRequiredService<AddPrincipalsService>();
        await addService.AddPrincipalsAsync(originalId, targetId, dryRun);
    }, originalIdOption, targetIdOption, dryRunOption);

// Subcommand: remove
var removePrincipalsCommand = new Command("remove", "Remove principals from target which are not in original");
removePrincipalsCommand.AddOption(originalIdOption);
removePrincipalsCommand.AddOption(targetIdOption);
removePrincipalsCommand.AddOption(dryRunOption);
removePrincipalsCommand.SetHandler(async (originalId, targetId, dryRun) =>
    {
        var removeService = host.Services.GetRequiredService<RemovePrincipalsService>();
        await removeService.RemovePrincipalsAsync(originalId, targetId, dryRun);
    }, originalIdOption, targetIdOption, dryRunOption);

// Subcommand: sync
var syncPrincipalsCommand = new Command("sync", "Synchronize adds missing principals from original to target and removes principals from target which are not in original");
syncPrincipalsCommand.AddOption(originalIdOption);
syncPrincipalsCommand.AddOption(targetIdOption);
syncPrincipalsCommand.AddOption(dryRunOption);
syncPrincipalsCommand.SetHandler(async (originalId, targetId, dryRun) =>
    {
        var replicateService = host.Services.GetRequiredService<ReplicateAppRoleAssignmentsService>();
        await replicateService.ReplicateAppRoleAssignmentsAsync(originalId, targetId, dryRun);
    }, originalIdOption, targetIdOption, dryRunOption);

// Subcommand: reset
var resetPrincipalsCommand = new Command("reset", "Remove all principals from the target");
resetPrincipalsCommand.AddOption(targetIdOption);
resetPrincipalsCommand.AddOption(dryRunOption);
resetPrincipalsCommand.SetHandler(async (targetId, dryRun) =>
    {
        var resetService = host.Services.GetRequiredService<ResetPrincipalsService>();
        await resetService.ResetPrincipalsAsync(targetId, dryRun);
    }, targetIdOption, dryRunOption);

// Attach subcommands to principals command
principalsCommand.AddCommand(addPrincipalsCommand);
principalsCommand.AddCommand(removePrincipalsCommand);
principalsCommand.AddCommand(syncPrincipalsCommand);
principalsCommand.AddCommand(resetPrincipalsCommand);

// App Registration command
var appRegistrationCommand = new Command("appRegistration", "Manage app registrations");

// Subcommand: create
var createAppRegistrationCommand = new Command("create", "Create a new app registration with the specified display name");
createAppRegistrationCommand.AddOption(displayNameOption);
createAppRegistrationCommand.AddOption(dryRunOption);
createAppRegistrationCommand.SetHandler(async (displayName, dryRun) =>
    {
        var createService = host.Services.GetRequiredService<CreateAppRegistrationService>();
        await createService.CreateAppRegistrationAsync(displayName, dryRun);
    }, displayNameOption, dryRunOption);

// Attach subcommands to appRegistration command
appRegistrationCommand.AddCommand(createAppRegistrationCommand);

// Root command
var rootCommand = new RootCommand("Azure PRiSM Tool");
rootCommand.AddCommand(principalsCommand);
rootCommand.AddCommand(appRegistrationCommand);

return await rootCommand.InvokeAsync(args);