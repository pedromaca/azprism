namespace Azprism.Services;

public interface IAppRoleAssignmentMapping
{
    Task<Dictionary<Guid, Guid>> AppRoleAssignmentMappingAsync(Guid originalObjectId, Guid targetObjectId);
}