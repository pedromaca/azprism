using System.CommandLine;

namespace Azprism.Commands;

public static class CommonOptions
{
    public static Option<Guid> OriginalId { get; } = new("--original-id")
    {
        Description = "The original object ID to sync from",
        Required = true
    };
    
    public static Option<Guid> TargetId { get; } = new("--target-id")
    {
        Description = "The target object ID to sync to",
        Required = true
    };
    
    public static Option<string> DisplayName { get; } = new("--display-name")
    {
        Description = "The display name for the app registration",
        Required = true
    };
    
    public static Option<bool> DryRun { get; } = new("--dry-run")
    {
        Description = "Perform a dry run without making changes",
        DefaultValueFactory = _ => false
    };
}