# Tasks

## 1. ADRs

- [x] 1.1 Crear `docs/adr/0007` … `0012` a partir de la tabla de Decisions de `design.md` (Contexto, Decisión, Alternativas, Consecuencias; estado "Aceptado"; cambio `add-create-order-idempotency`). Verificar: existen los 6 ficheros y cada uno tiene las 4 secciones.

## 2. Persistencia

- [x] 2.1 Añadir el tipo `IdempotencyRecord` (Application) y ampliar `IOrderRepository` con la búsqueda por `(Subject, Key)`, el alta del registro y un guardado que indique una violación de unicidad. Verificar: `dotnet build`.
- [x] 2.2 Implementarlo en Infrastructure: `DbSet` + configuración EF (PK `(Subject, Key)`, longitudes) y, en `OrderRepository`, captura de `DbUpdateException` de unicidad (SQLite error 19) con limpieza del `ChangeTracker`, comentada en castellano. Verificar: `dotnet build` y los tests existentes en verde.

## 3. Caso de uso

- [x] 3.1 Ampliar `CreateOrderCommand` con `IdempotencyKey?` y `Subject?`, y hacer que el handler devuelva `CreateOrderResult` (`Created` / `Replayed` / `KeyReusedWithDifferentBody`), con la huella SHA-256 del command y la escala de los decimales normalizada (ADR-0010). Verificar: test unitario de la huella (`10m` y `10.00m` → misma huella; orden de líneas distinto → huella distinta).

## 4. Endpoint

- [x] 4.1 En `OrdersController.Create`: leer `[FromHeader] Idempotency-Key` y `sub`, validar la cabecera (1–64, no solo espacios) y el `sub` (`400 ProblemDetails`), y traducir `CreateOrderResult` a `201`, `201` + `Idempotent-Replayed: true` o `422 ProblemDetails`. Añadir `ProducesResponseType` 422. Verificar: los tests de la 4.2.
- [x] 4.2 Tests de integración en `tests/Ordering.Tests/CreateOrderIdempotencyApiTests.cs`, uno por escenario del requisito "Idempotencia del alta" (sin cabecera, primera, repetición, mismo cuerpo escrito de otra forma, cuerpo distinto → 422, clientes distintos, peticiones simultáneas, 400 que no consume la clave, cabecera inválida, token sin `sub`). Añadir a `TestTokens` una forma de elegir el `sub` o de omitirlo. Verificar: `dotnet test` en verde.

## 5. Integración

- [x] 5.1 Actualizar `CLAUDE.md` (borrar `ordering.db` tras cambios de esquema; cabecera `Idempotency-Key`). Ejecutar `dotnet test` completo y una prueba manual: borrar `ordering.db`, arrancar la API y repetir un POST con la misma clave (2 × 201, el segundo con `Idempotent-Replayed`, mismo id) y con otro cuerpo (422). Verificar: todo en verde y las respuestas esperadas.

## Workflow follow-up

- Ejecutar `/openspec-verify-change add-create-order-idempotency`.
- Archivar con `/openspec-archive-change add-create-order-idempotency`, que añade el requisito a `openspec/specs/order-creation/spec.md`.
