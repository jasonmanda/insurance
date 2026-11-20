
namespace Insurance.AI.Api.Contracts;

public sealed record ChatResponse(
    Guid ConversationId,
    string Answer);
