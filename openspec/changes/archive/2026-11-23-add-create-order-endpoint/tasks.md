# Tasks

## 1. ADRs

- [x] 1.1 Crear `docs/adr/0001-validacion-en-dos-capas.md` … `0006-skus-repetidos-permitidos.md` a partir de la tabla de Decisions de `design.md` (Contexto, Decisión, Alternativas, Consecuencias; estado "Aceptado"). Verificar: existen los 6 ficheros y cada uno tiene las 4 secciones.

## 2. Caso de uso (Application)

- [x] 2.1 Crear `src/Ordering.Application/Orders/CreateOrder/CreateOrderCommand.cs`, con el command (`CustomerId`, `Lines`) y el handler en el mismo fichero: `Order.Create` con `TimeProvider`, `AddLine` por línea, `IOrderRepository.AddAsync` + `SaveChangesAsync`, y devolver `OrderDto.From`. Verificar: `dotnet build` compila.

## 3. Endpoint y errores (Api)

- [x] 3.1 Crear `CreateOrderRequest`/`CreateOrderLineRequest` en Api con DataAnnotations (`Guid?`/`decimal?` + `[Required]`, `[MinLength(1)]`, `[MaxLength(Order.MaxLines)]`, `[MaxLength(64)]` en sku y `[Range]` en quantity/unitPrice), con comentarios en castellano sobre las trampas (ADR-0003, ADR-0004). Verificar: `dotnet build`.
- [x] 3.2 Añadir `[HttpPost]` en `OrdersController` con `[Authorize(Policy = Policies.OrdersWrite)]`: traduce el request a command y devuelve `CreatedAtRoute("GetOrderById", ...)`, con `ProducesResponseType` 201/400. Verificar: tests de integración de 201 + Location y de un GET posterior que devuelve 200.
- [x] 3.3 Añadir `DomainExceptionHandler : IExceptionHandler` que devuelve `400 ProblemDetails`, y registrar `AddProblemDetails()`, `AddExceptionHandler<>()` y `UseExceptionHandler()` en `Program.cs`. Verificar: test de integración con `customerId` = `Guid.Empty` → 400 `application/problem+json`.
- [x] 3.4 Tests de integración en `tests/Ordering.Tests/CreateOrderApiTests.cs` (patrón de `OrdersApiTests`, con `ApiFactory` y `TestTokens.Create`): 401 sin token, 403 con solo `orders:read`, 400 sin líneas, 400 con `quantity` 0, 400 con `unitPrice` ausente y 400 con varios errores a la vez. Verificar: `dotnet test` en verde.

## 4. Integración

- [x] 4.1 Ejecutar `dotnet test` completo (los tests existentes siguen pasando) y hacer una prueba manual de `POST /api/orders` desde Swagger con un token `orders:write`. Verificar: todo en verde y un 201 en Swagger.

## 5. Correcciones de la verificación

- [x] 5.1 Tests de los escenarios sin cobertura en `CreateOrderApiTests`: SKU repetido → 201 con 2 líneas, precio cero → 201, más de 50 líneas → 400 `Lines`, `lines` ausente → 400 `Lines`, `sku` vacío/espacios/65 caracteres → 400 `Lines[0].Sku`, `unitPrice` negativo → 400 `Lines[0].UnitPrice`. Verificar: `dotnet test` en verde.
- [x] 5.2 Escenario "Línea nula" (`lines: [null]` → 400 `Lines`) en el spec y su test. Verificar: `dotnet test` en verde.
- [x] 5.3 El `ProducesResponseType` 400 del POST declara `ProblemDetails` (la base común de `ValidationProblemDetails` y de la respuesta del dominio), con un comentario que lo explique. Verificar: `dotnet build`, y en `/swagger/v1/swagger.json` el 400 del POST referencia `ProblemDetails`.

## Workflow follow-up

- Ejecutar `/openspec-verify-change add-create-order-endpoint`.
- Archivar con `/openspec-archive-change add-create-order-endpoint`, que crea `openspec/specs/order-creation/spec.md`.
