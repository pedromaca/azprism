namespace Azprism.Services;

public interface ICheckPermissions
{
    Task<bool> PrincipalHasPermissions(Guid principalId);
}