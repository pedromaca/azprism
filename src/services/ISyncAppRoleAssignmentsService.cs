namespace azprism.Services
{
    public interface ISyncAppRoleAssignmentsService
    {
        Task SyncAppRoleAssignmentsAsync(Guid originalObjectId, Guid targetObjectId, bool dryRun = false);
    }
}

