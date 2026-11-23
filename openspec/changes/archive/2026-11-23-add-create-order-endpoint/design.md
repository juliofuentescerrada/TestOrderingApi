# Design

## Context

- La referencia `GET /api/orders/{id}` define el patrón: el controller fino hace `ISender.Send(...)`, la query y el handler van en un mismo fichero, y la política se llama igual que el scope.
- El dominio (`Order.Create`, `Order.AddLine`) ya protege todas las reglas del enunciado salvo "al menos una línea", y lanza `DomainException`, que hoy no se traduce a HTTP (acaba en `500`).
- La política `orders:write` y la ruta con nombre `GetOrderById` ya existen.
- No hay librería de validación (solo MediatR) y SQLite no aplica `HasMaxLength` ni `HasPrecision`.

## Goals / Non-Goals

**Goals:**
- Cumplir `specs/order-creation/spec.md` sin dependencias NuGet nuevas.
- Dejar cada decisión registrada como ADR en `docs/adr/`.

**Non-Goals:**
- Idempotencia (cambio aparte: `add-create-order-idempotency`).
- Validar que el cliente exista o la precisión decimal de `unitPrice` (ver ADR-0005).
- Migraciones de EF Core (se mantiene `EnsureCreated`).

## Decisions

Cada decisión se registra como ADR en `docs/adr/` (formato: Contexto, Decisión, Alternativas, Consecuencias). Este es el resumen:

| ADR | Decisión | Alternativa descartada |
|---|---|---|
| 0001 | **Validación en dos capas.** (1) DataAnnotations en el request: `[ApiController]` devuelve automáticamente un `400 ValidationProblemDetails` con todos los errores por campo. (2) `IExceptionHandler` + `AddProblemDetails()` traduce `DomainException` → `400 ProblemDetails` como red de seguridad. | Solo el dominio: devuelve un único error sin campo y no cubre los campos ausentes. FluentValidation o un pipeline behavior de MediatR: una dependencia o más código del necesario para este tamaño. |
| 0002 | **`CreateOrderRequest` propio en la capa Api**, que el controller traduce a un `CreateOrderCommand` con tipos no nulables. | Bindear el command directamente: se ahorra el mapeo, pero los nullables y los atributos de validación entrarían en Application. |
| 0003 | **`Guid? CustomerId` y `decimal? UnitPrice` con `[Required]`** en el request. Sin esto, un campo ausente se deserializa como `Guid.Empty`/`0`, y un `unitPrice` omitido se aceptaría como precio 0. `quantity` puede ser `int`, porque el 0 ya lo rechaza `[Range(1, int.MaxValue)]`. Un `customerId` = `Guid.Empty` explícito lo rechaza el dominio (capa 2). | Tipos no nulables: los campos ausentes pasarían en silencio. |
| 0004 | **`[MaxLength(64)]` en `sku`**, igual que `HasMaxLength(64)` de EF, porque SQLite no aplica longitudes. `[MaxLength(Order.MaxLines)]` y `[MinLength(1)]` en `lines`, reutilizando la constante del dominio. | Confiar en el esquema: SQLite guardaría SKUs de cualquier longitud. |
| 0005 | **No se valida la precisión de `unitPrice`** (SQLite guarda los decimales como TEXT e ignora `HasPrecision(18,2)`). Queda fuera de las reglas del enunciado; se documenta como deuda. | Un validador de escala ≤ 2: es una regla que el enunciado no pide. |
| 0006 | **Se permiten SKUs repetidos en un pedido**, como ya hace el dominio. | Rechazarlos o agruparlos: sería cambiar el dominio sin que nadie lo haya pedido. |

Detalles de implementación:
- El handler usa el `TimeProvider` ya registrado para `CreatedAtUtc`, crea el pedido con el dominio, lo persiste con `IOrderRepository.AddAsync` + `SaveChangesAsync` y devuelve `OrderDto.From(order)`.
- El controller responde con `CreatedAtRoute("GetOrderById", new { id }, dto)`.
- El `IExceptionHandler` solo trata `DomainException`. El resto sigue el flujo por defecto (`500`).
- Cada trampa no obvia (nullables, MaxLength por SQLite) lleva un comentario en castellano en el código.

## Risks / Trade-offs

- [Las reglas aparecen en el request y en el dominio] → El máximo de líneas sale de `Order.MaxLines`, y el dominio sigue siendo la garantía final si las dos capas divergen.
- [El 400 tiene dos formas: `errors` por campo (entrada) o `detail` (dominio)] → Ambas son `ProblemDetails`. Se documenta en ADR-0001.
- [El `IExceptionHandler` es global: cualquier `DomainException` de cualquier endpoint pasa de `500` a `400`] → Es el comportamiento deseado, porque una invariante violada es un error del cliente.

## Migration Plan

Sin cambios de esquema. Se despliega sin pasos adicionales.
