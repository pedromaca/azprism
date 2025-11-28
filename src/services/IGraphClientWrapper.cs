using Microsoft.Graph.Models;

namespace azprism.Services;

public interface IGraphClientWrapper
{
    Task AddAppRoleAssignmentsAsync(List<AppRoleAssignment> appRoleAssignments, Guid targetObjectId);
    Task RemoveAppRoleAssignmentsAsync(List<AppRoleAssignment> appRoleAssignments, Guid targetObjectId);
    Task<Dictionary<Guid, Guid>> AppRoleAssignmentMappingAsync(string originalObjectId, string targetObjectId);
    Task<List<AppRoleAssignment>> GetAllAssignmentsAsync(Guid objectId);
}