# ADR-0003: Tipos nulables en el request para detectar campos ausentes

- **Estado:** Aceptado
- **Cambio:** `add-create-order-endpoint`

## Contexto

System.Text.Json deja a su valor por defecto los campos que faltan en el JSON. Con tipos no nulables:

- `customerId` ausente → `Guid.Empty`, y `[Required]` no lo detecta (un `Guid` nunca es `null`).
- `unitPrice` ausente → `0`, que **es válido** (`>= 0`): un precio omitido se aceptaría en silencio.
- `quantity` ausente → `0`, que ya rechaza `[Range(1, int.MaxValue)]`.

## Decisión

`Guid? CustomerId` y `decimal? UnitPrice` con `[Required]`. `Quantity` se queda como `int` con `[Range]`. Un `customerId` = `00000000-…` **explícito** lo rechaza el dominio (`Order.Create`) a través de la red de seguridad de ADR-0001.

## Alternativas

- **Tipos no nulables:** los campos ausentes pasarían sin error.
- **`[JsonRequired]`:** el fallo se produce en la deserialización y su mensaje es menos claro para el cliente.

## Consecuencias

- Los campos ausentes se informan como errores del campo correspondiente.
- El request lleva comentarios en castellano que explican la trampa.
