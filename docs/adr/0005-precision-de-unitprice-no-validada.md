# ADR-0005: La precisión de `unitPrice` no se valida

- **Estado:** Aceptado
- **Cambio:** `add-create-order-endpoint`

## Contexto

EF Core configura `UnitPrice` con `HasPrecision(18, 2)`, pero SQLite guarda los `decimal` como TEXT e ignora la precisión: `10.999` se guarda tal cual. El enunciado solo exige `unitPrice >= 0`.

## Decisión

No validar la escala decimal en este cambio. Se documenta como deuda conocida.

## Alternativas

- **Rechazar más de 2 decimales:** es una regla de negocio que nadie ha pedido (¿y si hay precios unitarios con fracciones de céntimo?).
- **Redondear:** alteraría en silencio lo que envió el cliente.

## Consecuencias

- Pueden persistirse precios con más de 2 decimales.
- Si negocio confirma la regla, se añade un validador en el request y un test.
