# Proposal

## Why

Si un cliente reintenta `POST /api/orders` tras un timeout o un error de red, hoy se crea un pedido duplicado. El enunciado pide, como ampliación, que una cabecera `Idempotency-Key` haga los reintentos seguros.

## What Changes

- `POST /api/orders` acepta la cabecera opcional `Idempotency-Key` (texto de 1 a 64 caracteres). Si no se envía, el comportamiento no cambia.
- La clave se asocia al cliente autenticado (claim `sub`): misma clave y mismo cuerpo → se devuelve el pedido original sin crear otro (`201`, misma `Location`, cabecera `Idempotent-Replayed: true`).
- Misma clave y cuerpo distinto → `422 Unprocessable Content` con `ProblemDetails`.
- Una cabecera inválida, o un token sin `sub` junto con la cabecera → `400`.
- Nueva tabla de registros de idempotencia, guardada en la misma transacción que el pedido.
- Las decisiones se registran como ADRs 0007–0012 en `docs/adr/`.

## Capabilities

### New Capabilities

### Modified Capabilities
- `order-creation`: se añade el requisito de idempotencia del alta. Los requisitos existentes no cambian.

## Impact

- **Api**: `OrdersController.Create` lee la cabecera y el `sub`, y traduce el resultado a `201`/`422`.
- **Application**: `CreateOrderCommand` recibe la clave y el sujeto opcionales; el handler devuelve un `CreateOrderResult`; `IOrderRepository` se amplía.
- **Infrastructure**: nueva entidad y configuración EF `IdempotencyRecord`, y detección de la violación de unicidad en el repositorio.
- **Esquema**: tabla nueva. Con `EnsureCreated` hay que borrar el `ordering.db` local; los tests usan una BD temporal.
- **Dependencias**: ninguna nueva.
