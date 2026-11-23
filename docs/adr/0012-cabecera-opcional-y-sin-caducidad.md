# ADR-0012: Cabecera opcional, validada y sin caducidad

- **Estado:** Aceptado
- **Cambio:** `add-create-order-idempotency`

## Contexto

El enunciado define la cabecera como "texto, máximo 64 caracteres". Los clientes actuales no la envían. El esquema se crea con `EnsureCreated`, sin migraciones.

## Decisión

- La cabecera es opcional: si falta, el comportamiento es el de antes.
- Vacía, solo espacios o más de 64 caracteres → `400`. No se restringen los caracteres.
- Un `400` (de validación o del dominio) no guarda la clave: hay rollback junto con el pedido.
- Las claves no caducan; se guarda `CreatedAtUtc` para una limpieza futura.
- **Hay que borrar el `ordering.db` local** tras este cambio: `EnsureCreated` no añade tablas a una base existente. Con `dotnet run --project src/Ordering.Api` el fichero está en `src/Ordering.Api/ordering.db` (junto con `-shm`/`-wal`), no en la raíz: el directorio de trabajo es el del proyecto.

## Alternativas

- **Cabecera obligatoria:** rompería a los clientes actuales.
- **Caducidad (p. ej. 24 h):** fuera del alcance del enunciado.

## Consecuencias

- La tabla crece sin límite: es deuda conocida.
- El cliente puede corregir un cuerpo inválido y reintentar con la misma clave.
