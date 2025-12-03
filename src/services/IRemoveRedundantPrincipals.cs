namespace azprism.Services
{
    public interface IRemoveRedundantPrincipalsService
    {
        Task RemoveRedundantPrincipalsAsync(Guid originalObjectId, Guid targetObjectId, bool dryRun = false);
    }
}