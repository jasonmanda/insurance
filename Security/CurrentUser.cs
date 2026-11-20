
using System.Security.Claims;

namespace Insurance.AI.Api.Security;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?
                .User.FindFirstValue("sub");

            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                throw new UnauthorizedAccessException(
                    "Authenticated user ID is missing or invalid.");
            }

            return parsedUserId;
        }
    }
}
