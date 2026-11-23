# ADR-0009: Respuesta de una repetición

- **Estado:** Aceptado
- **Cambio:** `add-create-order-idempotency`

## Contexto

Con la misma clave y el mismo cuerpo hay que devolver "el mismo pedido". El cliente que reintenta tras un timeout no sabe si la primera petición llegó.

## Decisión

Responder `201 Created` con la misma `Location` y el `OrderDto` leído por `OrderId`, más la cabecera `Idempotent-Replayed: true`. Solo se guarda el `OrderId`.

## Alternativas

- **`200 OK`:** más fiel a "no he creado nada ahora", pero el cliente tendría que manejar dos códigos de éxito para la misma operación.
- **Guardar y repetir la respuesta serializada:** es lo más estricto, pero innecesario aquí: un pedido no cambia después de crearse (no hay endpoints de modificación).

## Consecuencias

- La repetición es indistinguible para el cliente; la cabecera solo sirve para depurar.
- Si en el futuro los pedidos cambian de estado, la repetición devolverá el estado actual, no el original.
