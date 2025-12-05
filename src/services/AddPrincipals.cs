using Microsoft.Extensions.Logging;

namespace Azprism.Services;

public class AddPrincipalsService : IAddPrincipalsService
{
    private readonly ILogger<AddPrincipalsService> _logger;
    private readonly IGraphClientWrapper _graphClientWrapper;
    private readonly ComparePrincipalsService _comparePrincipalsService;
    private readonly AppRoleAssignmentBuilderService _appRoleAssignmentBuilderService;

    public AddPrincipalsService(
        ILogger<AddPrincipalsService> logger,
        IGraphClientWrapper graphClientWrapper, 
        ComparePrincipalsService comparePrincipalsService,
        AppRoleAssignmentBuilderService appRoleAssignmentBuilderService)
    {
        _graphClientWrapper = graphClientWrapper;
        _logger = logger;
        _comparePrincipalsService = comparePrincipalsService;
        _appRoleAssignmentBuilderService = appRoleAssignmentBuilderService;
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
        var (_, principalsToAdd) = _comparePrincipalsService.ComparePrincipals(originalAssignments, targetAssignments);

        _logger.LogInformation($"{(dryRun ? "[DRY RUN] " : "")}azprism will add {principalsToAdd.Count} principals.");

        if (dryRun) return;

        if (principalsToAdd.Count == 0) return;

        // Determine appRoleAssignment request bodies
        var appRoleAssignmentRequestBodies = await _appRoleAssignmentBuilderService.BuildAppRoleAssignment(principalsToAdd, originalObjectId, targetObjectId);

        // Add the missing principals
        await _graphClientWrapper.AddAppRoleAssignmentsAsync(appRoleAssignmentRequestBodies, targetObjectId);
    }
}
