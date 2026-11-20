
namespace Insurance.AI.Api.Contracts;

public sealed record ChatRequest(
    string Message,
    Guid? ConversationId = null);
