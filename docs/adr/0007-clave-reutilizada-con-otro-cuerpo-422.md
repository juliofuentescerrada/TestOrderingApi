# ADR-0007: Misma clave con cuerpo distinto → 422

- **Estado:** Aceptado
- **Cambio:** `add-create-order-idempotency`

## Contexto

El enunciado pide decidir y justificar qué responde `POST /api/orders` cuando llega una `Idempotency-Key` ya usada con un cuerpo distinto.

## Decisión

Responder `422 Unprocessable Content` con un `ProblemDetails` que explica que la clave ya se usó con otro cuerpo. No se crea nada.

## Alternativas

- **`409 Conflict`:** defendible, pero `draft-ietf-httpapi-idempotency-key-header` lo reserva para "la petición original con esa clave aún está en curso"; usarlo aquí mezcla dos situaciones.
- **Devolver el pedido original:** ocultaría un error del cliente y respondería con un pedido que no corresponde al cuerpo enviado.

## Consecuencias

- Es la misma semántica que el borrador IETF y que Stripe: reutilizar una clave para otra operación es un error del cliente.
- El cliente tiene que generar una clave nueva por cada operación distinta.
