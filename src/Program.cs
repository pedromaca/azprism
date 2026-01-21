using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Azprism.Commands.AppRegistrations;
using Azprism.Commands.Principals;
using Azprism.Services;

// Initialize Graph client with validation
var clientResult = await GraphClientFactory.CreateAsync();
if (!clientResult.Success)
{
    Console.Error.WriteLine(clientResult.ErrorMessage);
    return 1;
}
var graphClient = clientResult.Client!;

// Host builder function
IHost BuildHost() =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((services) =>
        {
            services.AddSingleton(_ => graphClient);
            services.AddLogging(options => 
                options.AddSimpleConsole(s => {
                    s.UseUtcTimestamp = true;
                    s.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                    s.SingleLine = true;
                }));
            services.AddSingleton<IGraphClientWrapper, GraphClientWrapper>();
            services.AddTransient<ICheckPermissions, CheckPermissions>();
            services.AddTransient<IComparePrincipals, ComparePrincipalsService>();
            services.AddTransient<IAppRoleAssignmentBuilder, AppRoleAssignmentBuilderService>();
            services.AddTransient<IAppRoleAssignmentMapping, AppRoleAssignmentMapping>();
            services.AddTransient<IAddPrincipalsService, AddPrincipalsService>();
            services.AddTransient<IRemoveRedundantPrincipalsService, RemoveRedundantPrincipalsService>();
            services.AddTransient<ISyncAppRoleAssignmentsService, SyncAppRoleAssignmentsService>();
            services.AddTransient<IResetPrincipalsService, ResetPrincipalsService>();
            services.AddTransient<CreateAppRegistrationService>();
        })
        .ConfigureLogging(logging =>
        {
            logging.AddFilter("Microsoft", LogLevel.Warning);
        })
        .Build();

var host = BuildHost();

// Ensure SP has necessary permissions
var checkService = host.Services.GetRequiredService<ICheckPermissions>();
async Task<bool> PermissionCheck() => await checkService.PrincipalHasPermissions(clientResult.ClientId);

// Register root command
var rootCommand = new RootCommand("Azure Principal Sync Mechanism (Azprism)");

// Register command modules
rootCommand.Subcommands.Add(new PrincipalsCommandModule(host.Services, PermissionCheck).BuildCommand());
rootCommand.Subcommands.Add(new AppRegistrationsCommandModule(host.Services).BuildCommand());

// start host so logging providers are active
await host.StartAsync();

// run the command
var exitCode = await rootCommand.Parse(args).InvokeAsync();

// stop host (this lets logging providers flush) and dispose
await host.StopAsync();
await host.WaitForShutdownAsync();
return exitCode;
