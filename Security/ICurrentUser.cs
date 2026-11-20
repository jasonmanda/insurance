
namespace Insurance.AI.Api.Security;

public interface ICurrentUser
{
    Guid UserId { get; }
}
