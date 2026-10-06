# MateHR - Architecture Guidelines

## Objetivo

MateHR es una plataforma SaaS de Recursos Humanos desarrollada utilizando:

- .NET 10
- ASP.NET Core Web API
- Clean Architecture
- Domain Driven Design (DDD)
- CQRS (opcional)
- Entity Framework Core
- SQL Server
- JWT Authentication

El objetivo es mantener una arquitectura escalable, mantenible y desacoplada.

---

# Documentación

| Documento | Contenido |
|---|---|
| [Backlog — Módulo de Tenants](Documentation/backlog.md) | Tareas pendientes, decisiones tomadas y deuda técnica |

---

# Domain

El dominio representa las reglas de negocio.

```text
MateHR.Domain

├── Tenants
├── Employees
├── Vacations
├── Climate
├── Training
├── Recruitment
├── Performance
├── Attendance
├── Payroll
├── Benefits
├── Organization
│
├── Common
└── Exceptions
```

### Carpetas transversales del Domain

`Common` y `Exceptions` no son módulos de negocio: contienen tipos compartidos por todos.

```text
Common
├── PagedResult<T>
└── SlugGenerator

Exceptions
└── SlugAlreadyExistsException
```

`PagedResult<T>` vive acá y no en `Application` porque `ITenantRepository` —que está en el `Domain`— lo necesita como tipo de retorno. Si estuviera en `Application`, el `Domain` tendría que referenciar a `Application` y la Clean Architecture se invierte.

---

## Contenido de Domain

### Entities

Representan objetos con identidad.

Ejemplo:

```csharp
public class Employee
{
    public Guid Id { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }
}
```

### ValueObjects

Representan conceptos sin identidad.

Ejemplo:

```csharp
public class Email
{
    public string Value { get; }
}
```

### Aggregates

Raíces de agregados.

Ejemplo:

```text
Employee
└── VacationRequest
```

### Domain Events

Eventos de dominio.

Ejemplo:

```csharp
EmployeeCreatedEvent
VacationApprovedEvent
```

### Enums

```csharp
EmployeeStatus
VacationStatus
```

---

# Application

Contiene los casos de uso del sistema.

No contiene acceso a datos.

No contiene Entity Framework.

---

## Estructura

```text
MateHR.Application

├── Employees
├── Vacations
├── Climate
├── Training
├── Recruitment
├── Performance
├── Attendance
├── Payroll
├── Benefits
├── Organization
│
├── Common
│
└── DependencyInjection.cs
```

---

# Organización por módulo

Ejemplo Employees:

```text
Employees

├── Commands (POST PUT PATCH DELETE)
│
├── Queries (GET)
│
├── DTOs
│
├── Validators
│
├── Interfaces
│
└── Mappings
```

---

# CQRS

Cada funcionalidad debe vivir dentro de su propio caso de uso.

```text
Tenants

├── Commands (escritura)
│   ├── CreateTenant
│   ├── UpdateTenant
│   ├── ChangeStatusTenant
│   ├── ChangeRecruitmentModeTenant
│   └── ChangeSubscriptionTypeTenant
│
├── Queries (lectura)
│   ├── GetTenantById
│   ├── GetTenantBySlug
│   └── GetTenants
│
├── DTOs
│   ├── AddressDto
│   ├── CreateTenantDto
│   ├── UpdateTenantDto
│   ├── TenantResponse
│   └── GetTenantsRequest
│
├── Validators
├── Interfaces
└── Mappings
```

Cada caso de uso es una clase con **una** interfaz que la acompaña, registrada como *scoped*. Sin mediator:

```text
Tenants

├── Interfaces/IGetTenantById.cs
└── Queries/GetTenantById.cs
```

La separación Commands/Queries se mantiene, pero no hay `IRequest`/`IRequestHandler` ni paquete de MediatR. Cada caso de uso expone `ExecuteAsync`.

Todos los DTOs, validators y mappings viven **en la carpeta del módulo**, no en subcarpetas por tipo de caso de uso.

---

# Interfaces

Dónde vive una interfaz depende de qué expone:

| Tipo de interfaz | Ubicación | Ejemplo |
|---|---|---|
| De repositorio | `Domain/<Modulo>/Interfaces/` | `ITenantRepository` |
| De contexto | `Domain/<Modulo>/Interfaces/` | `ITenantContext` |
| De servicio de aplicación | `Application/Common/Interfaces/` | `IJwtService` |

**Regla:** si la interfaz expone entidades del dominio, va en el `Domain`. `Application` y `Infrastructure` la ven igual, porque los tres proyectos convergen en `Domain`. Si la interfaz no toca entidades, va en `Application`, que tampoco depende de `Infrastructure`.

Ejemplo — repositorio:

```csharp
public interface ITenantRepository
{
    Task<Tenant> GetTenantByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
```

Ejemplo — contexto:

```csharp
public interface ITenantContext
{
    Guid? CurrentTenantId { get; }
}
```

`ITenantContext` declara sólo el getter a propósito. El setter vive en la implementación (`Infrastructure/TenantContext`), así ningún caso de uso puede cambiar el tenant de la request.

Ejemplo — servicio de aplicación:

```csharp
public interface IJwtService
{
    string GenerateToken(User user);
}
```

## Convenciones de los repositorios

- **Devuelven la entidad, nunca `null`.** Si no existe, lanzan `KeyNotFoundException`, que el middleware mapea a 404 sin necesidad de try/catch en el caso de uso.
- **Todo método de escritura recibe `CancellationToken`.** Se propaga hasta EF Core.
- **Los métodos que aplican reglas de negocio llaman al método de la entidad** (`ChangeStatus`, `ChangeRecruitmentMode`), nunca escriben las propiedades directamente.
- **Las escrituras son independientes de las lecturas**: cada método carga la entidad, la muta y llama a `SaveChangesAsync`.

---

# Infrastructure

Contiene las implementaciones concretas.

---

## Estructura

```text
MateHR.Infrastructure

├── Persistence
│
├── Authentication
│
├── Services
│
├── Settings
│
└── DependencyInjection.cs
```

---

# Persistence

Contiene el acceso a datos.

```text
Persistence

├── ApplicationDbContext.cs
│
├── Configurations
│
├── Repositories
│
└── Migrations
```

---

# ApplicationDbContext

Debe contener únicamente:

```csharp
DbSet<Employee>
DbSet<VacationRequest>
DbSet<Course>
```

y

```csharp
ApplyConfigurationsFromAssembly()
```

---

# Configurations

Contienen configuraciones de EF Core.

Ejemplo:

```text
EmployeeConfiguration.cs
VacationConfiguration.cs
TrainingConfiguration.cs
```

Implementan:

```csharp
IEntityTypeConfiguration<TEntity>
```

Ejemplo:

```csharp
public class EmployeeConfiguration
    : IEntityTypeConfiguration<Employee>
{
}
```

---

# Repositories

Implementaciones concretas.

Ejemplo:

```text
EmployeeRepository.cs
VacationRepository.cs
TrainingRepository.cs
```

Implementan las interfaces definidas en el `Domain`.

---

# Authentication

Implementado:

```text
JwtService.cs
PasswordHasher.cs
RefreshTokenService.cs
Settings/JwtSettings.cs
```

Detalles de diseño:

- **Access token**: JWT firmado HS256, con `sub`, `email`, `role`, `security_stamp` y
  `tenant_id` (presente sólo para roles con tenant).
- **Refresh token**: opaco de 64 bytes aleatorios (base64url). Viaja **únicamente** en
  cookie `httpOnly` de path `/api/v1/auth`; nunca aparece en el cuerpo JSON.
- En la base se guarda **el hash SHA-256** del refresh token, no el token. El logout
  hashea la cookie y busca por `TokenHash`. Si alguien lee la tabla `RefreshTokens` no
  puede suplantar ninguna sesión.
- **Contraseñas**: PBKDF2-SHA256, 210 000 iteraciones, salt de 16 bytes por usuario.
- `PasswordHasher` usa comparación de tiempo constante en la verificación.

Configuración (sección `Jwt` de `appsettings.json`):

| Clave | Por defecto | Notas |
|---|---|---|
| `Issuer` | `MateHR` | |
| `Audience` | `MateHR.Api` | |
| `SigningKey` | *(vacío)* | **No se versiona.** Definir con user-secrets. |
| `AccessTokenMinutes` | `60` | |
| `RefreshTokenDays` | `14` | |

La clave de firma **nunca** se commitea. En desarrollo:

```bash
dotnet user-secrets --project MateHR.Api set "Jwt:SigningKey" "<clave de 32+ caracteres>"
```

Sin `SigningKey`, la API arranca pero **todos los endpoints `[Authorize]` responden 401**
y el login falla; el arranque emite un warning explícito. `JwtService` valida la
configuración al resolverse (fail-closed) en vez de confiar en valores vacíos.

### Endpoints

| Método | Ruta | Auth | Respuesta |
|---|---|---|---|
| `POST` | `/api/v1/auth/login` | Anónimo | `200` access token + info de usuario; cookie refresh |
| `POST` | `/api/v1/auth/logout` | Anónimo | `204`; revoca el refresh de la cookie |
| `GET` | `/api/v1/auth/me` | Bearer | `200` con el usuario del token |

`/api/v1/auth/refresh` (rotación del access token) **queda pendiente**: hoy el refresh
token se emite y revoca, pero no hay endpoint que lo canjee. También falta el alta de
usuarios (registro o endpoint administrativo), así que hoy la tabla `Users` se puebla a
mano.

Policies disponibles: `RequireSuperAdmin`, `RequireAdmin`.

---

# Settings

Configuración fuerte tipada.

```text
JwtSettings.cs
EmailSettings.cs
StorageSettings.cs
```

---

# Dependency Injection

Cada proyecto debe registrar sus dependencias.

---

## Application

```csharp
builder.Services.AddApplication();
```

Contendrá:

- Casos de uso (Commands y Queries), como *scoped*
- FluentValidation, con `AddValidatorsFromAssembly`
- AutoMapper, con los perfiles del assembly

`DependencyInjection.cs` es una `static class` y devuelve el `IServiceCollection` para poder encadenar.

**No se usa MediatR.** Los casos de uso son clases planas con `ExecuteAsync`, registradas en el `IServiceCollection`. Agregar MediatR después no obliga a cambiar esta estructura: sólo reemplaza el registro por `AddMediatR(...)` y los `ExecuteAsync` por handlers.

---

## Infrastructure

```csharp
builder.Services.AddInfrastructure(
    builder.Configuration);
```

Contendrá:

- DbContext
- Repositories
- JWT
- Servicios externos

---

# Api

La API solamente expone HTTP.

---

## Estructura

```text
MateHR.Api

├── Controllers
├── Middleware
├── Extensions
├── Filters
└── Program.cs
```

---

# Controllers

Un controlador por módulo.

```text
EmployeesController
VacationsController
TrainingController
ClimateController
```

---

# Middleware

Ejemplos:

```text
ExceptionMiddleware
JwtMiddleware
RequestLoggingMiddleware
```

---

# Program.cs

Debe mantenerse limpio.

Ejemplo:

```csharp
builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.AddControllers();
```

Evitar registrar servicios directamente aquí.

---

# Módulos actuales

```text
Tenants         ← implementado
Employees
Vacations
Climate
Training
Recruitment
Performance
Attendance
Payroll
Benefits
Organization
Auth
```

`Tenants` es el primero. Cuando se implemente el segundo módulo, se copia la estructura de `Tenants` como plantilla.

---

# Convenciones

## Nombres

Interfaces:

```csharp
IEmployeeRepository
IJwtService
```

Repositorios:

```csharp
EmployeeRepository
VacationRepository
```

DTOs:

```csharp
EmployeeDto
VacationDto
```

Commands:

```csharp
CreateEmployeeCommand
ApproveVacationCommand
```

Queries:

```csharp
GetEmployeeByIdQuery
GetVacationBalanceQuery
```

---

# Qué NO hacer

❌ Domain referencia Infrastructure

❌ Domain referencia Application

❌ Controllers llamando DbContext directamente

❌ Entity Framework dentro de Application

❌ Servicios de infraestructura dentro de Domain

❌ Lógica de negocio dentro de Controllers

❌ Reglas de negocio dentro de Repositories

---

# Objetivo final

Mantener una arquitectura:

- Escalable
- Modular
- Testeable
- Desacoplada
- Preparada para crecer a múltiples módulos de RRHH
- Preparada para incorporar Identity, Azure, Google Login o nuevos proveedores sin afectar el dominio