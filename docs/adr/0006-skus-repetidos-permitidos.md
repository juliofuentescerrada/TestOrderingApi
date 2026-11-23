# ADR-0006: Se permiten SKUs repetidos en un pedido

- **Estado:** Aceptado
- **Cambio:** `add-create-order-endpoint`

## Contexto

El enunciado no dice nada sobre SKUs repetidos. El dominio actual los permite: cada `AddLine` crea una línea nueva.

## Decisión

Permitirlos sin cambiar el dominio.

## Alternativas

- **Rechazarlos con `400`:** sería una regla nueva sin un requisito que la pida.
- **Agruparlos en una línea (sumando cantidades):** ambiguo si los precios unitarios difieren.

## Consecuencias

- Un pedido puede tener varias líneas con el mismo SKU.
- Si negocio decide otra cosa, el cambio va en `Order.AddLine` (dominio), no en la API.
