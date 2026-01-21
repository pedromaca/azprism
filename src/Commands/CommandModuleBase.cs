using System.CommandLine;

namespace Azprism.Commands;

public abstract class CommandModuleBase : ICommandModule
{
    protected IServiceProvider Services { get; }
    
    protected CommandModuleBase(IServiceProvider services)
    {
        Services = services;
    }
    
    public abstract Command BuildCommand();
}