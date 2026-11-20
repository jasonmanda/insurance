
using Insurance.AI.Api.Models;

namespace Insurance.AI.Api.Repositories;

public interface IConversationRepository
{
    Task<AIConversation?> GetByIdAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task CreateAsync(
        AIConversation conversation,
        CancellationToken cancellationToken = default);

    Task UpdateSessionAsync(
        Guid conversationId,
        Guid userId,
        string sessionData,
        CancellationToken cancellationToken = default);
}
