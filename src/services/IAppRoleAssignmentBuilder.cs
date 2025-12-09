using Microsoft.Graph.Models;

namespace Azprism.Services;

public interface IAppRoleAssignmentBuilder
{
    Task<List<AppRoleAssignment>> BuildAppRoleAssignment(List<AppRoleAssignment> principalsToAdd, Guid originalObjectId,
        Guid targetObjectId);
}