# ADR-0010: Huella del cuerpo de la petición

- **Estado:** Aceptado
- **Cambio:** `add-create-order-idempotency`

## Contexto

Para distinguir "mismo cuerpo" de "cuerpo distinto" se guarda una huella. El mismo pedido puede escribirse de varias formas: otro orden de propiedades, espacios, un GUID en mayúsculas o `10` en lugar de `10.00`.

## Decisión

Huella = SHA-256 del `CreateOrderCommand` ya bindeado, serializado a JSON con la **escala de los `decimal` normalizada** (`decimal` conserva la escala: `10.00m` se serializaría como `"10.00"` y `10m` como `"10"`). Los SKU no se recortan y el orden de las líneas cuenta.

## Alternativas

- **Bytes crudos del cuerpo:** las diferencias de formato darían `422` falsos, y habría que leer el cuerpo dos veces.
- **Ignorar el orden de las líneas:** cambiaría el pedido devuelto, que conserva ese orden.

## Consecuencias

- Las diferencias de formato no cuentan; las de contenido sí.
- Un cambio en el command o en el serializador cambia la huella: en producción convendría versionarla.
