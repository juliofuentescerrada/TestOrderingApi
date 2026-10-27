using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Ordering.Api.Auth;
using Ordering.Application;
using Ordering.Infrastructure;
using Ordering.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Swagger / OpenAPI con soporte para autenticar con un token JWT (botón "Authorize").
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Ordering API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Token JWT. Introduce solo el token (sin el prefijo 'Bearer ').",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// La cadena de conexión se lee de forma diferida para que los tests puedan sustituirla.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlite(sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")));

// Autenticación JWT. La clave de firma se lee de configuración (Jwt:SigningKey).
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, config) =>
    {
        var signingKey = config["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = config["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.OrdersRead, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(ctx => Policies.HasScope(ctx.User, Policies.OrdersRead)));

    options.AddPolicy(Policies.OrdersWrite, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(ctx => Policies.HasScope(ctx.User, Policies.OrdersWrite)));
});

var app = builder.Build();

// Para el ejercicio: se crea el esquema SQLite al arrancar (sin migraciones).
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Ordering API v1"));
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Necesario para WebApplicationFactory<Program> en los tests de integración.
public partial class Program
{
}
