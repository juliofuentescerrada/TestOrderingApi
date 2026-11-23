# ADR-0008: La clave de idempotencia es por cliente

- **Estado:** Aceptado
- **Cambio:** `add-create-order-idempotency`

## Contexto

Si la clave fuese global, dos clientes podrían usar la misma. Si además el cuerpo coincide, el segundo recibiría el pedido del primero: una fuga de datos entre clientes. El JWT trae el sujeto en el claim `sub` (`MapInboundClaims = false`), aunque la validación no lo exige.

## Decisión

La identidad de una clave es `(Subject, Key)`, con `Subject` = claim `sub`. Si llega `Idempotency-Key` con un token sin `sub` → `400`.

## Alternativas

- **Clave global:** más simple, pero con la fuga descrita.
- **Espacio compartido para tokens sin `sub`:** reintroduce la fuga entre esos clientes.

## Consecuencias

- Cada cliente tiene su propio espacio de claves.
- Los clientes sin `sub` no pueden usar idempotencia (pero sí crear pedidos sin la cabecera).
