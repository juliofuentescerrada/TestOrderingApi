# ADR-0002: Request DTO de la API separado del command

- **Estado:** Aceptado
- **Cambio:** `add-create-order-endpoint`

## Contexto

La validación del borde (ADR-0001) necesita atributos de DataAnnotations y tipos nulables (ADR-0003). El caso de uso vive en Application, que no debería depender de detalles del transporte HTTP.

## Decisión

El controller recibe un `CreateOrderRequest` (capa Api) y lo traduce a un `CreateOrderCommand` (capa Application) con tipos no nulables (`Guid`, `decimal`).

## Alternativas

- **Bindear el command directamente:** se ahorra el mapeo, pero los `Guid?`/`decimal?` y los atributos de validación entrarían en Application, y el handler tendría que usar `.Value`.

## Consecuencias

- El command expresa exactamente lo que el handler acepta y se puede reutilizar fuera de HTTP.
- Coste: dos records más y unas líneas de mapeo en el controller.
