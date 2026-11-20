
using Insurance.AI.Api.Contracts;

namespace Insurance.AI.Api.Services;

public sealed class ContractService : IContractService
{
    public Task<IReadOnlyList<ContractDto>> GetActiveContractsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ContractDto> contracts =
        [
            new ContractDto(
                Guid.NewGuid(),
                "POL-2026-001",
                "Assurance Automobile",
                "ACTIVE",
                DateTime.UtcNow.AddMonths(6)
            )
        ];

        return Task.FromResult(contracts);
    }

    public Task<decimal> GetNextRenewalAmountAsync(
        Guid userId,
        string contractId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(150000m);
    }
}
