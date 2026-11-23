# ADR-0004: Límites de longitud validados en la API

- **Estado:** Aceptado
- **Cambio:** `add-create-order-endpoint`

## Contexto

EF Core configura `Sku` con `HasMaxLength(64)`, pero **SQLite no aplica longitudes**: un SKU de 1.000 caracteres se guardaría sin error. El máximo de líneas (50) vive en `Order.MaxLines`.

## Decisión

- `[MaxLength(64)]` en `sku`, la misma regla que ya expresa el esquema.
- `[MinLength(1)]` y `[MaxLength(Order.MaxLines)]` en `lines`, reutilizando la constante del dominio para no duplicar el número.

## Alternativas

- **Confiar en el esquema:** no funciona con SQLite.
- **Validar la longitud en el dominio:** sería un cambio del dominio fuera del alcance del enunciado.

## Consecuencias

- El límite de 64 está en dos sitios (configuración de EF y request); si cambia, hay que actualizar ambos.
