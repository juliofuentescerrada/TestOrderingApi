# Spec Delta

## ADDED Requirements

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
