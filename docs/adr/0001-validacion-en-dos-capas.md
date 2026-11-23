# ADR-0001: Validación de la entrada en dos capas

- **Estado:** Aceptado
- **Cambio:** `add-create-order-endpoint`

## Contexto

`POST /api/orders` debe responder `400` con un `ProblemDetails` claro ante una entrada inválida. El dominio (`Order.Create`, `Order.AddLine`) ya protege casi todas las reglas, pero lanza `DomainException` con un único mensaje y sin indicar el campo. Además, nada traduce esa excepción a HTTP: hoy acaba en `500`. No hay librería de validación instalada.

## Decisión

1. **Borde HTTP:** DataAnnotations en el request de la API. `[ApiController]` responde automáticamente `400 ValidationProblemDetails` con todos los errores agrupados por campo.
2. **Red de seguridad:** un `IExceptionHandler` registrado con `AddProblemDetails()` traduce `DomainException` → `400 ProblemDetails` (`detail` = mensaje del dominio) para cualquier endpoint.

## Alternativas

- **Solo el dominio + exception handler:** menos código, pero devuelve un único error sin campo y no detecta los campos ausentes (ver ADR-0003).
- **FluentValidation:** añade una dependencia NuGet para unas pocas reglas.
- **Pipeline behavior de MediatR:** más infraestructura de la necesaria para un solo comando.

## Consecuencias

- El cliente ve todos los errores de entrada de una vez.
- Algunas reglas aparecen en el request y en el dominio; el dominio sigue siendo la garantía final.
- El `400` tiene dos formas (`errors` por campo o `detail`), ambas `application/problem+json`.
- Cualquier `DomainException` de la API pasa de `500` a `400`.
