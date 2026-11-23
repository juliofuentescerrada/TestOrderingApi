# order-creation Specification

## Purpose

Permite a clientes autorizados dar de alta pedidos con sus líneas a través de la API, garantizando que solo se persisten pedidos válidos.

## Requirements

### Requirement: Alta de pedido
El sistema SHALL exponer `POST /api/orders` que, con una petición válida (`customerId` y `lines` con `sku`, `quantity`, `unitPrice`), persiste el pedido en estado `Pending` y responde `201 Created` con la cabecera `Location` apuntando a `GET /api/orders/{id}` y el pedido creado (incluido su total) en el cuerpo.

#### Scenario: Pedido válido
- **WHEN** un cliente con scope `orders:write` envía un `customerId` y dos líneas (`SKU-1`, 2 x 10.00) y (`SKU-2`, 1 x 5.50)
- **THEN** el sistema responde `201` con `Location` = `/api/orders/{id}`, y el cuerpo contiene el `id`, estado `Pending`, las 2 líneas y total `25.50`

#### Scenario: El pedido creado es consultable
- **WHEN** se hace `GET` a la URL de la cabecera `Location` con scope `orders:read`
- **THEN** el sistema responde `200` con el mismo pedido

#### Scenario: SKU repetido
- **WHEN** la petición contiene dos líneas con el mismo `sku`
- **THEN** el sistema crea el pedido con ambas líneas

### Requirement: Autorización de alta
El sistema MUST exigir un token JWT válido con el scope `orders:write` para crear pedidos.

#### Scenario: Sin token
- **WHEN** se envía la petición sin token
- **THEN** el sistema responde `401` y no crea el pedido

#### Scenario: Token sin scope de escritura
- **WHEN** se envía la petición con un token que solo tiene `orders:read`
- **THEN** el sistema responde `403` y no crea el pedido

### Requirement: Validación de la entrada
El sistema MUST rechazar con `400` y un cuerpo `ProblemDetails` (`application/problem+json`) cualquier petición inválida, sin persistir nada. Si hay varios errores de entrada, MUST informarlos todos, identificando el campo afectado.

#### Scenario: Sin líneas
- **WHEN** `lines` está vacío o ausente
- **THEN** el sistema responde `400` con un error sobre `lines`

#### Scenario: Línea nula
- **WHEN** `lines` contiene un elemento `null`
- **THEN** el sistema responde `400` con un error sobre `lines`

#### Scenario: Más de 50 líneas
- **WHEN** `lines` contiene 51 elementos
- **THEN** el sistema responde `400` con un error sobre `lines`

#### Scenario: customerId ausente o vacío
- **WHEN** `customerId` falta o es `00000000-0000-0000-0000-000000000000`
- **THEN** el sistema responde `400`

#### Scenario: Sku vacío o demasiado largo
- **WHEN** una línea tiene `sku` ausente, vacío, solo espacios o de más de 64 caracteres
- **THEN** el sistema responde `400` con un error sobre el `sku` de esa línea

#### Scenario: Cantidad no positiva
- **WHEN** una línea tiene `quantity` ausente, `0` o negativa
- **THEN** el sistema responde `400` con un error sobre la `quantity` de esa línea

#### Scenario: Precio ausente o negativo
- **WHEN** una línea tiene `unitPrice` ausente o menor que `0`
- **THEN** el sistema responde `400` con un error sobre el `unitPrice` de esa línea

#### Scenario: Varios errores a la vez
- **WHEN** la petición tiene `customerId` ausente y una línea con `quantity` `0`
- **THEN** el cuerpo `400` incluye ambos errores

#### Scenario: Precio cero
- **WHEN** una línea tiene `unitPrice` `0`
- **THEN** el sistema la acepta

### Requirement: Idempotencia del alta
El sistema SHALL aceptar en `POST /api/orders` una cabecera opcional `Idempotency-Key`. Para un mismo cliente autenticado (claim `sub`), una clave identifica una única creación: las repeticiones con el mismo cuerpo MUST devolver el pedido original sin crear otro, y las que traen un cuerpo distinto MUST rechazarse.

#### Scenario: Sin cabecera
- **WHEN** se envía una petición válida sin `Idempotency-Key` dos veces
- **THEN** el sistema crea dos pedidos distintos

#### Scenario: Primera petición con clave
- **WHEN** se envía una petición válida con una `Idempotency-Key` no usada por ese cliente
- **THEN** el sistema crea el pedido y responde `201`, sin la cabecera `Idempotent-Replayed`

#### Scenario: Repetición con el mismo cuerpo
- **WHEN** el mismo cliente repite la petición con la misma clave y el mismo cuerpo
- **THEN** el sistema responde `201` con la misma `Location` y el mismo pedido, añade `Idempotent-Replayed: true` y no crea otro pedido

#### Scenario: Mismo cuerpo escrito de otra forma
- **WHEN** la repetición cambia solo el orden de las propiedades JSON, las mayúsculas del `customerId` o la escala de un precio (`10` en lugar de `10.00`)
- **THEN** el sistema la trata como el mismo cuerpo y devuelve el pedido original

#### Scenario: Misma clave con cuerpo distinto
- **WHEN** el mismo cliente reutiliza la clave con un cuerpo distinto
- **THEN** el sistema responde `422` con un `ProblemDetails` y no crea ningún pedido

#### Scenario: Misma clave en clientes distintos
- **WHEN** dos clientes con distinto `sub` usan la misma clave
- **THEN** el sistema crea un pedido para cada uno y ninguno recibe el pedido del otro

#### Scenario: Peticiones simultáneas con la misma clave
- **WHEN** llegan a la vez varias peticiones del mismo cliente con la misma clave y el mismo cuerpo
- **THEN** el sistema crea un único pedido y todas las respuestas lo devuelven

#### Scenario: Una petición rechazada no consume la clave
- **WHEN** una petición con clave se rechaza con `400` y después se envía una válida con la misma clave
- **THEN** el sistema crea el pedido y responde `201`

#### Scenario: Cabecera inválida
- **WHEN** `Idempotency-Key` está vacía, solo tiene espacios o supera los 64 caracteres
- **THEN** el sistema responde `400` y no crea ningún pedido

#### Scenario: Token sin sub
- **WHEN** se envía `Idempotency-Key` con un token válido que no tiene claim `sub`
- **THEN** el sistema responde `400` y no crea ningún pedido
