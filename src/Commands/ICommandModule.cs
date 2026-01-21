using System.CommandLine;

namespace Azprism.Commands;

public interface ICommandModule
{
    Command BuildCommand();
}