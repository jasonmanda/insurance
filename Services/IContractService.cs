
using Insurance.AI.Api.Contracts;

namespace Insurance.AI.Api.Services;

public interface IContractService
{
    Task<IReadOnlyList<ContractDto>> GetActiveContractsAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<decimal> GetNextRenewalAmountAsync(
        Guid userId,
        string contractId,
        CancellationToken cancellationToken = default
    );
}
