
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

    [Description("Returns the active insurance contracts belonging to the authenticated user.")]
    public async Task<IReadOnlyList<ContractDto>>
        GetMyActiveContractsAsync()
    {
        return await _contractService.GetActiveContractsAsync(
            _currentUser?.UserId ?? Guid.CreateVersion7());
    }

    [Description("Returns the next renewal amount for a contract belonging to the authenticated user.")]
    public async Task<decimal> GetNextRenewalAmountAsync(
        [Description("The contract identifier")]
        string contractId)
    {
        return await _contractService.GetNextRenewalAmountAsync(
            _currentUser.UserId,
            contractId);
    }
}
