namespace azprism.Services
{
    public interface IAddPrincipalsService
    {
        Task AddPrincipalsAsync(Guid originalObjectId, Guid targetObjectId, bool dryRun = false);
    }
}