
using System.ComponentModel;
using Insurance.AI.Api.Services;
using Insurance.AI.Api.Security;
using Insurance.AI.Api.Contracts;

namespace Insurance.AI.Api.Agents;

public sealed class InsuranceTools
{
    private readonly IContractService _contractService;
    private readonly ICurrentUser _currentUser;

    public InsuranceTools(
        IContractService contractService,
        ICurrentUser currentUser)
    {
        _contractService = contractService;
        _currentUser = currentUser;
    }

    private Guid GetAuthenticatedUserId()
    {
        var userId = _currentUser.UserId;

        if (userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "Authenticated user ID is missing.");
        }

        return userId;
    }

    [Description(
        "Returns the active insurance contracts belonging to the authenticated user.")]
    public async Task<IReadOnlyList<ContractDto>>
        GetMyActiveContractsAsync()
    {
        var userId = GetAuthenticatedUserId();

        return await _contractService.GetActiveContractsAsync(userId);
    }

    [Description(
        "Returns the next renewal amount for a contract belonging to the authenticated user.")]
    public async Task<decimal> GetNextRenewalAmountAsync(
        [Description("The contract identifier")]
        string contractId)
    {
        if (string.IsNullOrWhiteSpace(contractId))
        {
            throw new ArgumentException(
                "Contract ID is required.",
                nameof(contractId));
        }

        var userId = GetAuthenticatedUserId();

        return await _contractService.GetNextRenewalAmountAsync(
            userId,
            contractId);
    }
}
