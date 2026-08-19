using SmartExpense.Application.Abstractions.Authentication;

namespace SmartExpense.Api.Authentication;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;

            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var subject = principal.FindFirst("sub")?.Value;

            return Guid.TryParse(subject, out var userId)
                ? userId
                : null;
        }
    }
}
