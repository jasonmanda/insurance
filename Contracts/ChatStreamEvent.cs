
namespace Insurance.AI.Api.Contracts;

public sealed record ChatStreamEvent(
    string Type,
    Guid? ConversationId = null,
    string? Content = null);
