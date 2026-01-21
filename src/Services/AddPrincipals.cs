using Microsoft.Extensions.Logging;

namespace Azprism.Services;

public class AddPrincipalsService : IAddPrincipalsService
{
    private readonly ILogger<AddPrincipalsService> _logger;
    private readonly IGraphClientWrapper _graphClientWrapper;
    private readonly IComparePrincipals _comparePrincipals;
    private readonly IAppRoleAssignmentBuilder _appRoleAssignmentBuilder;

    public AddPrincipalsService(
        ILogger<AddPrincipalsService> logger,
        IGraphClientWrapper graphClientWrapper, 
        IComparePrincipals comparePrincipals,
        IAppRoleAssignmentBuilder appRoleAssignmentBuilder)
    {
        _graphClientWrapper = graphClientWrapper;
        _logger = logger;
        _comparePrincipals = comparePrincipals;
        _appRoleAssignmentBuilder = appRoleAssignmentBuilder;
    }

    /// <summary>
    /// Adds the missing principals to the target application.
    /// </summary>
    public async Task AddPrincipalsAsync(Guid originalObjectId, Guid targetObjectId, bool dryRun = false)
    {
        // Fetch assignments for both original and target service principals
        var originalAssignments = await _graphClientWrapper.GetAllAssignmentsAsync(originalObjectId);
        var targetAssignments = await _graphClientWrapper.GetAllAssignmentsAsync(targetObjectId);

        // Compare principals to identify which ones need to be added
        var (_, principalsToAdd) = _comparePrincipals.ComparePrincipals(originalAssignments, targetAssignments);

        _logger.LogInformation($"{(dryRun ? "[DRY RUN] " : "")}azprism will add {principalsToAdd.Count} principals.");

        if (dryRun) return;

        if (principalsToAdd.Count == 0) return;

        // Determine appRoleAssignment request bodies
        var appRoleAssignmentRequestBodies = await _appRoleAssignmentBuilder.BuildAppRoleAssignment(principalsToAdd, originalObjectId, targetObjectId);

        // Add the missing principals
        await _graphClientWrapper.AddAppRoleAssignmentsAsync(appRoleAssignmentRequestBodies, targetObjectId);
    }
}
