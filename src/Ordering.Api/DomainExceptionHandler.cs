using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Ordering.Domain;

namespace Ordering.Api;

/// <summary>
/// Red de seguridad (ADR-0001): una invariante del dominio violada es un error del cliente → 400 ProblemDetails.
/// Cualquier otra excepción sigue el flujo por defecto (500).
/// </summary>
public sealed class DomainExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;

    public DomainExceptionHandler(IProblemDetailsService problemDetails)
    {
        _problemDetails = problemDetails;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DomainException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The order violates a business rule.",
                Detail = exception.Message
            }
        });
    }
}
