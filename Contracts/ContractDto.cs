
namespace Insurance.AI.Api.Contracts;

public sealed record ContractDto(
    Guid Id,
    string ContractNumber,
    string ProductName,
    string Status,
    DateTime? ExpirationDate
);
