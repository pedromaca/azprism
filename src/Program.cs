using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Azure.Identity;
using azprism.Services;

var missing = new List<string>();
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TENANT_ID"))) missing.Add("TENANT_ID");
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CLIENT_ID"))) missing.Add("CLIENT_ID");
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CLIENT_SECRET"))) missing.Add("CLIENT_SECRET");

if (missing.Count > 0) { 
	Console.Error.WriteLine("Missing required environment variables: " + string.Join(", ", missing)); 
	return 1;
};

// Host builder function
IHost BuildHost() =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((services) =>
        {
            services.AddSingleton(_ => {
                var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                var credentials = new ClientSecretCredential(tenantId, clientId, clientSecret);
                string[] scopes = ["https://graph.microsoft.com/.default"];
                return new GraphServiceClient(credentials, scopes);
            });

            services.AddLogging(options => 
                options.AddSimpleConsole(s => {
                    s.UseUtcTimestamp = true;
                    s.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                    s.SingleLine = true;
                }));
            services.AddTransient<IGraphClientWrapper, GraphClientWrapper>();
            services.AddTransient<ComparePrincipalsService>();
            services.AddTransient<AppRoleAssignmentBuilderService>();
            services.AddTransient<RemoveExtraPrincipalsService>();
            services.AddTransient<AddPrincipalsService>();
            services.AddTransient<ReplicateAppRoleAssignmentsService>();
            services.AddTransient<ResetPrincipalsService>();
            services.AddTransient<CreateAppRegistrationService>();
        })
        .Build();

var host = BuildHost();

var rootCommand = new RootCommand("Azure Principal Sync Mechanism (Azprism)");

var principalsCommand = new Command("principals", "Manage principal assignments");
var principalsAddCommand = new Command("add", "Add missing principals from original to target");
var principalsRemoveCommand = new Command("remove", "Remove principals from target which are not in original");
var principalsSyncCommand = new Command("sync", "Synchronize adds missing principals from original to target and removes principals from target which are not in original");
var principalsResetCommand = new Command("reset", "Remove all principals from the target");

// CLI Flags
var originalIdOption = new Option<Guid>("--original-id") {
    Description = "The original object ID to sync from", 
    Required = true 
};

var targetIdOption = new Option<Guid>("--target-id") {
    Description = "The target object ID to sync to",
    Required = true 
};
    
var displayNameOption = new Option<string>("--display-name") {
    Description = "The display name for the app registration",
    Required = true
};
    
var dryRunOption = new Option<bool>("--dry-run") {
    Description = "Perform a dry run without making changes",
    Required = false,
    DefaultValueFactory = _ => false
};

// PrincipalsAddCommand
principalsAddCommand.Options.Add(originalIdOption);
principalsAddCommand.Options.Add(targetIdOption);
principalsAddCommand.Options.Add(dryRunOption);
principalsCommand.Subcommands.Add(principalsAddCommand);
principalsAddCommand.SetAction(async parseResult => {
    var addService = host.Services.GetRequiredService<AddPrincipalsService>();
    await addService.AddPrincipalsAsync(
        parseResult.GetValue(originalIdOption),
        parseResult.GetValue(targetIdOption),
        parseResult.GetValue(dryRunOption)
    );
});

// PrincipalsRemoveCommand options
principalsRemoveCommand.Options.Add(originalIdOption);
principalsRemoveCommand.Options.Add(targetIdOption);
principalsRemoveCommand.Options.Add(dryRunOption);
principalsCommand.Subcommands.Add(principalsRemoveCommand);
principalsRemoveCommand.SetAction(async parseResult => {
    var removeService = host.Services.GetRequiredService<RemoveExtraPrincipalsService>();
    await removeService.RemoveExtraPrincipalsAsync(
        parseResult.GetValue(originalIdOption),
        parseResult.GetValue(targetIdOption),
        parseResult.GetValue(dryRunOption)
    );
});

// PrincipalsSyncCommand options
principalsSyncCommand.Options.Add(originalIdOption);
principalsSyncCommand.Options.Add(targetIdOption);
principalsSyncCommand.Options.Add(dryRunOption);
principalsCommand.Subcommands.Add(principalsSyncCommand);
principalsSyncCommand.SetAction(async parseResult => {
    var replicateService = host.Services.GetRequiredService<ReplicateAppRoleAssignmentsService>();
    await replicateService.ReplicateAppRoleAssignmentsAsync(
        parseResult.GetValue(originalIdOption),
        parseResult.GetValue(targetIdOption),
        parseResult.GetValue(dryRunOption)
    );
});

// PrincipalsResetCommand options
principalsResetCommand.Options.Add(targetIdOption);
principalsResetCommand.Options.Add(dryRunOption);
principalsCommand.Subcommands.Add(principalsResetCommand);
principalsResetCommand.SetAction(async parseResult => {
    var resetService = host.Services.GetRequiredService<ResetPrincipalsService>();
    await resetService.ResetPrincipalsAsync(
        parseResult.GetValue(targetIdOption),
        parseResult.GetValue(dryRunOption)
    );
});

// appRegistrationCommand
var appRegistrationCommand = new Command("appRegistration", "Manage app registrations");
var appRegistrationCreateCommand = new Command("create", "Create a new app registration with the specified display name");
appRegistrationCreateCommand.Options.Add(displayNameOption);
appRegistrationCreateCommand.Options.Add(dryRunOption);
appRegistrationCommand.Subcommands.Add(appRegistrationCreateCommand);
appRegistrationCreateCommand.SetAction(async parseResult => {
    var createService = host.Services.GetRequiredService<CreateAppRegistrationService>();
    await createService.CreateAppRegistrationAsync(
        parseResult.GetValue(displayNameOption) ?? throw new ArgumentException("Display name is required"),
        parseResult.GetValue(dryRunOption)
    );
});

// Attach subcommands to root command
rootCommand.Subcommands.Add(principalsCommand);
rootCommand.Subcommands.Add(appRegistrationCommand);

// start host so logging providers are active
await host.StartAsync();

// run the command
var exitCode = await rootCommand.Parse(args).InvokeAsync();

// stop host (this lets logging providers flush) and dispose
await host.StopAsync();
await host.WaitForShutdownAsync();

return exitCode;
