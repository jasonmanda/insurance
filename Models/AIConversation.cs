
namespace Insurance.AI.Api.Models;

public sealed class AIConversation
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string? Title { get; set; }

    public string SessionData { get; set; } = "{}";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
