# ADR-0011: Idempotencia en el handler, en una sola transacción

- **Estado:** Aceptado
- **Cambio:** `add-create-order-idempotency`

## Contexto

La garantía de "un solo pedido por clave" tiene que mantenerse aunque lleguen peticiones simultáneas y aunque el proceso caiga a mitad.

## Decisión

- El handler de `CreateOrderCommand` añade el pedido y un `IdempotencyRecord(Subject, Key, RequestHash, OrderId, CreatedAtUtc)` y guarda ambos con **un único** `SaveChanges`.
- La PK `(Subject, Key)` resuelve las carreras: el repositorio captura la `DbUpdateException` de unicidad (SQLite error 19), limpia el `ChangeTracker` y lo indica; el handler relee el registro y decide entre repetición y `422`.
- El handler devuelve un `CreateOrderResult` (`Created` / `Replayed` / `KeyReusedWithDifferentBody`) y el controller lo traduce a HTTP.

## Alternativas

- **Filtro o middleware de caché en la API:** el pedido y la clave irían en dos transacciones; una caída entre ambas duplicaría el pedido en el reintento.
- **Excepción para el `422`:** es un resultado esperado del caso de uso, no un error.

## Consecuencias

- La garantía la da la base de datos, no la memoria del proceso.
- `IOrderRepository` crece con tres operaciones de idempotencia; EF Core no se filtra a Application.
