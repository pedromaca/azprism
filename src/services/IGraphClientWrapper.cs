using Microsoft.Graph.Models;

namespace Azprism.Services;

public interface IGraphClientWrapper
{
    Task AddAppRoleAssignmentsAsync(List<AppRoleAssignment> appRoleAssignments, Guid targetObjectId);
    Task RemoveAppRoleAssignmentsAsync(List<AppRoleAssignment> appRoleAssignments, Guid targetObjectId);
    Task<List<AppRoleAssignment>> GetAllAssignmentsAsync(Guid objectId);
    Task<List<AppRoleAssignment>> GetAppRoleAssignments(Guid principalId);
    Task<List<AppRole>> GetAppRolesAsync(Guid objectId);
}