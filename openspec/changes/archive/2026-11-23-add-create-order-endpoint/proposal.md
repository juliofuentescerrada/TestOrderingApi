# Proposal

## Why

La API solo permite consultar pedidos (`GET /api/orders/{id}`); no hay forma de crearlos. Además, hoy cualquier `DomainException` acaba en un `500`, así que una entrada inválida no recibe una respuesta útil.

## What Changes

- Nuevo endpoint `POST /api/orders` protegido con la política `orders:write` (401 sin token, 403 sin el scope).
- Caso de uso MediatR `CreateOrderCommand` + handler que crea el `Order` mediante el dominio y lo persiste con `IOrderRepository`.
- Validación de entrada en dos capas, que responde `400` con `ProblemDetails`:
  - DataAnnotations en un `CreateOrderRequest` propio de la capa Api (todos los errores por campo a la vez).
  - Traducción global de `DomainException` → `400 ProblemDetails` como red de seguridad (elimina el `500` actual).
- Respuesta `201 Created` con cabecera `Location` hacia `GET /api/orders/{id}` y el `OrderDto` en el cuerpo.
- Registro de las decisiones de diseño como ADRs en `docs/adr/`.
- Tests de integración del endpoint.

## Capabilities

### New Capabilities
- `order-creation`: alta de pedidos vía API (autorización, reglas de validación de la entrada y contrato de respuesta).

### Modified Capabilities

## Impact

- **Api**: `OrdersController` (nueva acción), nuevo `CreateOrderRequest`, nuevo `IExceptionHandler` y registro de `AddProblemDetails()`/`UseExceptionHandler()` en `Program.cs`. Afecta a todos los endpoints: un `DomainException` pasa de `500` a `400`.
- **Application**: nuevo `Orders/CreateOrder/CreateOrderCommand.cs`.
- **Domain / Infrastructure**: sin cambios (se reutilizan `Order.Create`, `AddLine`, `IOrderRepository` y `TimeProvider`).
- **Docs**: nueva carpeta `docs/adr/`.
- **Dependencias**: ninguna nueva.
