using System.Security.Claims;

namespace Ordering.Api.Auth;

public static class Policies
{
    public const string OrdersRead = "orders:read";
    public const string OrdersWrite = "orders:write";

    /// <summary>Los permisos viajan en el claim "scope", separados por espacios (estilo OAuth 2.0).</summary>
    public static bool HasScope(ClaimsPrincipal user, string scope) =>
        user.FindAll("scope")
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(scope);
}
