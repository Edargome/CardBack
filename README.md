# CardBack (Backend) — .NET + SQLite + JWT (Arquitectura Hexagonal)

API construida con **.NET (ASP.NET Core)** usando **Arquitectura Hexagonal** (Domain / Application / Infrastructure) con persistencia en **SQLite** y autenticación **JWT**.

## Estructura (Arquitectura Hexagonal)

El repositorio contiene 4 proyectos principales: :contentReference[oaicite:3]{index=3}

- `CardBack`  
  **Capa de entrada** (API): endpoints, configuración DI, autenticación/autorization, swagger.
- `CardBack.Domain`  
  **Núcleo de dominio**: entidades, reglas de negocio, contratos del dominio.
- `CardBack.Application`  
  **Casos de uso**: servicios, DTOs, lógica de aplicación (auth, cards, transactions).
- `CardBack.Infrastructure`  
  **Adaptadores**: EF Core, SQLite, repositorios, implementaciones concretas, seeding.

> Nota: El archivo de solución se llama `CardBack.slnx`. :contentReference[oaicite:4]{index=4}

## Requisitos

- .NET SDK (recomendado: **8.x**)
- SQLite (no necesitas instalarlo para EF Core; se usa por archivo `.db`)
- (Opcional) `dotnet-ef` si vas a usar migraciones

## Configuración

La API usa una cadena de conexión SQLite tipo:

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=cardback.db"
  }
}
