namespace azprism.Services
{
    public interface IResetPrincipalsService
    {
        Task ResetPrincipalsAsync(Guid targetObjectId, bool dryRun = false);
    }
}

