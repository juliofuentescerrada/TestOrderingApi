using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Ordering.Tests.Support;

/// <summary>Arranca la API real con una base SQLite temporal propia y una clave JWT de pruebas.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"ordering-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
        builder.UseSetting("Jwt:Issuer", TestTokens.Issuer);
        builder.UseSetting("Jwt:Audience", TestTokens.Audience);
        builder.UseSetting("Jwt:SigningKey", TestTokens.SigningKey);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        try
        {
            SqliteConnection.ClearAllPools();
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
            // Fichero temporal: si no se puede borrar ahora, lo hará el sistema.
        }
    }
}
