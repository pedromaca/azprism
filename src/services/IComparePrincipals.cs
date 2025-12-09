using Microsoft.Graph.Models;

namespace Azprism.Services;

public interface IComparePrincipals
{
    (List<AppRoleAssignment> ExtraInTarget, List<AppRoleAssignment> MissingInTarget) ComparePrincipals(
        List<AppRoleAssignment> originalAssignments, List<AppRoleAssignment> targetAssignments);
}