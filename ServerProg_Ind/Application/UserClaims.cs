using System.Security.Claims;
using ServerProg_Ind.Domain;

namespace ServerProg_Ind.Application;

public static class UserClaims
{
    public static Guid GetRequiredUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue("sub")
                    ?? user.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

        if (value is null || !Guid.TryParse(value, out var userId))
        {
            throw new AppException("unauthorized", "Authentication is required.", StatusCodes.Status401Unauthorized);
        }

        return userId;
    }
}
