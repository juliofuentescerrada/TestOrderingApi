# Design

## Context

- `POST /api/orders` ya existe (cambio `add-create-order-endpoint`): `CreateOrderRequest` → `CreateOrderCommand` → handler → `IOrderRepository.AddAsync` + `SaveChangesAsync`, con validación en dos capas (ADR-0001).
- `IOrderRepository` es la unidad de trabajo: un único `AppDbContext` por petición y un único `SaveChangesAsync`.
- El JWT se valida con `MapInboundClaims = false`, así que el sujeto llega como claim `sub`, que no es obligatorio. Los tokens de test lo incluyen.
- Esquema con `EnsureCreated`, sin migraciones. SQLite.

## Goals / Non-Goals

**Goals:**
- Cumplir el requisito "Idempotencia del alta" de `specs/order-creation/spec.md` sin dependencias nuevas.
- Garantizar en la base de datos (no en memoria) que una clave no crea dos pedidos, incluso con peticiones simultáneas.

**Non-Goals:**
- Caducidad o limpieza de claves (solo se guarda `CreatedAtUtc`).
- Idempotencia genérica para otros endpoints.
- Un estado "en curso" con `409`: no hace falta, porque la transacción única no deja estados intermedios visibles.

## Decisions

Cada decisión se registra como ADR en `docs/adr/`. Este es el resumen:

| ADR | Decisión | Alternativa descartada |
|---|---|---|
| 0007 | **Misma clave + cuerpo distinto → `422`** con `ProblemDetails`. Es un error del cliente (ha reutilizado la clave para otra operación), como propone `draft-ietf-httpapi-idempotency-key-header` y como hace Stripe. | `409`: el borrador lo reserva para "petición original aún en curso". Devolver el pedido original ocultaría el error. |
| 0008 | **Clave por cliente: `(Subject, Key)`**, con `Subject` = claim `sub`. Si llega la cabecera y el token no trae `sub` → `400`. | Clave global: un cliente que repita la clave y el cuerpo de otro recibiría **su** pedido (fuga de datos). |
| 0009 | **Repetición → `201`** con la misma `Location` y el `OrderDto` leído por `OrderId`, más la cabecera `Idempotent-Replayed: true`. Solo se guarda `OrderId`. | `200`: obliga al cliente a manejar dos códigos de éxito. Guardar la respuesta serializada: innecesario, porque un pedido no cambia tras crearse. |
| 0010 | **Huella = SHA-256 del `CreateOrderCommand` ya bindeado**, serializado con la escala de los `decimal` normalizada (`10.00m` y `10m` dan la misma huella). Los SKU no se recortan. El orden de las líneas cuenta. | Bytes crudos: el orden de las propiedades, los espacios o las mayúsculas del GUID darían `422` falsos, y habría que leer el cuerpo dos veces. Ignorar el orden de las líneas: cambia el pedido devuelto. |
| 0011 | **La lógica va en el handler, en una sola transacción.** Se añaden el pedido y el `IdempotencyRecord(Subject, Key, RequestHash, OrderId, CreatedAtUtc)` y se guardan con **un** `SaveChanges`. La PK `(Subject, Key)` resuelve las carreras: el repositorio captura `DbUpdateException` de unicidad (SQLite error 19), limpia el `ChangeTracker` y lo indica; el handler relee el registro y decide entre repetición y `422`. El handler devuelve un `CreateOrderResult` (`Created` / `Replayed` / `KeyReusedWithDifferentBody`) y el controller lo traduce a HTTP. | Un filtro o middleware de caché en Api: el pedido y la clave irían en dos transacciones, y una caída entre ambas duplicaría el pedido en el reintento. Excepciones para el `422`: es un resultado esperado, no un error. |
| 0012 | **Cabecera opcional, de 1 a 64 caracteres** (sin restricción de caracteres). Vacía, solo espacios o más de 64 → `400`. Un `400` no guarda la clave (rollback). Sin caducidad. **Tras desplegar hay que borrar el `ordering.db` local** (`EnsureCreated` no añade tablas a una BD existente). | Exigir la cabecera: rompería a los clientes actuales. Caducidad: fuera del alcance del enunciado. |

Detalles de implementación:
- La validación de la cabecera y la lectura de `sub` se hacen en el controller (borde HTTP), que los pasa al command como `IdempotencyKey?` y `Subject?`.
- `IdempotencyRecord` es un detalle de persistencia, no del dominio: su tipo vive en Application (lo usa el contrato del repositorio) y su configuración EF en Infrastructure.
- Las trampas no obvias (escala de los decimales, limpieza del `ChangeTracker`, captura de la unicidad en el repositorio) llevan un comentario en castellano.

## Risks / Trade-offs

- [En SQLite, las peticiones simultáneas se serializan por el bloqueo de escritura; la carrera real se ve menos que en un servidor SQL] → El test de concurrencia lanza peticiones en paralelo y comprueba que hay un solo pedido. La garantía la sigue dando la PK.
- [Un cambio en el serializador o en el command cambia la huella] → Las claves anteriores darían `422` con el mismo cuerpo. Es aceptable sin caducidad en un ejercicio; en producción la huella llevaría una versión.
- [La tabla crece sin límite] → Deuda documentada en ADR-0012; `CreatedAtUtc` permite una limpieza posterior.

## Migration Plan

Borrar `ordering.db` en los entornos locales tras desplegar (no hay migraciones). Rollback: volver a la versión anterior; la tabla sobrante no molesta.
