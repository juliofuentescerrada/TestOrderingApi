# Ordering API

API de pedidos en .NET 8 con Clean Architecture, MediatR, EF Core (SQLite) y autenticación JWT.

## Estructura

```
src/
  Ordering.Domain          Entidades e invariantes (Order, OrderLine)
  Ordering.Application     Casos de uso con MediatR y contratos (IOrderRepository, IOrderReadService)
  Ordering.Infrastructure  EF Core + SQLite (repositorios, configuración del modelo)
  Ordering.Api             Controllers, autenticación JWT y políticas de autorización
tests/
  Ordering.Tests           xUnit; tests unitarios y de integración con WebApplicationFactory
```

## Requisitos

- SDK de **.NET 8**.

## Ejecutar

```bash
dotnet restore
dotnet test
dotnet run --project src/Ordering.Api
```

La API arranca en `http://localhost:5080` (entorno Development) y crea la base SQLite `ordering.db` al iniciar.

## Documentación de la API

En Development, Swagger está disponible en `http://localhost:5080/swagger`. Desde el botón **Authorize** puedes pegar un token JWT para probar los endpoints protegidos.

## Autenticación

Los permisos viajan en el claim `scope` del JWT, separados por espacios (estilo OAuth 2.0). Los tests generan tokens con `TestTokens.Create(...)`.
