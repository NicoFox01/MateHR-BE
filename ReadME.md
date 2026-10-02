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


# Domain

El dominio representa las reglas de negocio.

```text
MateHR.Domain

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
└── Common
```

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

Ejemplo:

```text
Employees

└── CreateEmployee
    ├── Command.cs
    ├── Handler.cs
    ├── Validator.cs
    └── Response.cs
```

---

# Interfaces

Las interfaces viven en Application.

Ejemplo:

```csharp
public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id);
}
```

```csharp
public interface IJwtService
{
    string GenerateToken(User user);
}
```

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

Implementan interfaces definidas en Application.

---

# Authentication

Contendrá:

```text
JwtService.cs
PasswordHasher.cs
RefreshTokenService.cs
```

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

- MediatR
- FluentValidation
- AutoMapper
- Behaviors

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