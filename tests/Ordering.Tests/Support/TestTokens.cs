using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ordering.Tests.Support;

/// <summary>Genera JWT de prueba firmados con la misma clave que usa ApiFactory.</summary>
public static class TestTokens
{
    public const string Issuer = "ordering-api";
    public const string Audience = "ordering-api";
    public const string SigningKey = "test-only-signing-key-for-the-exercise-0123456789";

    public static string Create(params string[] scopes)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", "test-user"),
                new Claim("scope", string.Join(' ', scopes))
            }),
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
