using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Azprism.Services;

namespace Azprism.Commands.Principals;

public class PrincipalsCommandModule : CommandModuleBase
{
    private readonly Func<Task<bool>> _permissionCheck;

    public PrincipalsCommandModule(IServiceProvider services, Func<Task<bool>> permissionCheck) : base(services)
    {
        _permissionCheck = permissionCheck;
    }

    public override Command BuildCommand()
    {
        var command = new Command("principals", "Manage principal assignments");
        
        command.Subcommands.Add(BuildAddCommand());
        command.Subcommands.Add(BuildRemoveCommand());
        command.Subcommands.Add(BuildSyncCommand());
        command.Subcommands.Add(BuildResetCommand());

        return command;
    }
    
    private Command BuildAddCommand()
    {
        var cmd = new Command("add", "Add missing principals from original to target");
        cmd.Options.Add(CommonOptions.OriginalId);
        cmd.Options.Add(CommonOptions.TargetId);
        cmd.Options.Add(CommonOptions.DryRun);
        
        cmd.SetAction(async parseResult =>
        {
            if (!await _permissionCheck()) return;
            var service = Services.GetRequiredService<IAddPrincipalsService>();
            await service.AddPrincipalsAsync(
                parseResult.GetValue(CommonOptions.OriginalId),
                parseResult.GetValue(CommonOptions.TargetId),
                parseResult.GetValue(CommonOptions.DryRun));
        });
        
        return cmd;
    }
    
    private Command BuildRemoveCommand()
    {
        var cmd = new Command("remove", "Remove principals from target which are not in original");
        cmd.Options.Add(CommonOptions.OriginalId);
        cmd.Options.Add(CommonOptions.TargetId);
        cmd.Options.Add(CommonOptions.DryRun);
        
        cmd.SetAction(async parseResult =>
        {
            if (!await _permissionCheck()) return;
            var service = Services.GetRequiredService<IRemoveRedundantPrincipalsService>();
            await service.RemoveRedundantPrincipalsAsync(
                parseResult.GetValue(CommonOptions.OriginalId),
                parseResult.GetValue(CommonOptions.TargetId),
                parseResult.GetValue(CommonOptions.DryRun));
        });
        
        return cmd;
    }
    
    private Command BuildSyncCommand()
    {
        var cmd = new Command("sync", "Synchronize principals between original and target");
        cmd.Options.Add(CommonOptions.OriginalId);
        cmd.Options.Add(CommonOptions.TargetId);
        cmd.Options.Add(CommonOptions.DryRun);
        
        cmd.SetAction(async parseResult =>
        {
            if (!await _permissionCheck()) return;
            var service = Services.GetRequiredService<ISyncAppRoleAssignmentsService>();
            await service.SyncAppRoleAssignmentsAsync(
                parseResult.GetValue(CommonOptions.OriginalId),
                parseResult.GetValue(CommonOptions.TargetId),
                parseResult.GetValue(CommonOptions.DryRun));
        });
        
        return cmd;
    }
    
    private Command BuildResetCommand()
    {
        var cmd = new Command("reset", "Remove all principals from the target");
        cmd.Options.Add(CommonOptions.TargetId);
        cmd.Options.Add(CommonOptions.DryRun);
        
        cmd.SetAction(async parseResult =>
        {
            if (!await _permissionCheck()) return;
            var service = Services.GetRequiredService<IResetPrincipalsService>();
            await service.ResetPrincipalsAsync(
                parseResult.GetValue(CommonOptions.TargetId),
                parseResult.GetValue(CommonOptions.DryRun));
        });
        
        return cmd;
    }
}