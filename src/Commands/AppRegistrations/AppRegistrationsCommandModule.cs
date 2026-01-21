using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Azprism.Services;

namespace Azprism.Commands.AppRegistrations;

public class AppRegistrationsCommandModule : CommandModuleBase
{
    public AppRegistrationsCommandModule(IServiceProvider services) : base(services)
    { }

    public override Command BuildCommand()
    {
        var command = new Command("appRegistration", "Manage app registrations");
        
        command.Subcommands.Add(BuildCreateCommand());

        return command;
    }

    private Command BuildCreateCommand()
    {
        var cmd = new Command("create", "Create a new app registration with the specified display name");
        cmd.Options.Add(CommonOptions.DisplayName);
        cmd.Options.Add(CommonOptions.DryRun);
        
        cmd.SetAction(async parseResult => 
        {
            var service = Services.GetRequiredService<CreateAppRegistrationService>();
            await service.CreateAppRegistrationAsync(
                parseResult.GetValue(CommonOptions.DisplayName) ?? throw new ArgumentException("Display name is required"),
                parseResult.GetValue(CommonOptions.DryRun)
            );
        });
        
        return cmd;
    }
}