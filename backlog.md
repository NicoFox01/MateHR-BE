# Backlog — Módulo Recruitment

> Este backlog define el roadmap del módulo **Recruitment**. Está agrupado **por cosa** (tarea
> padre) y cada cosa se construye con **4 subtareas** en este orden:
>
> 1. **Domain + tests**
> 2. **Infrastructure + tests**
> 3. **Application + tests**
> 4. **Api + tests**
>
> No se avanza a la siguiente subtarea sin terminar la anterior y con sus tests en verde. El
> módulo Auth/Tenants/Users ya está implementado y es la plantilla: Clean Architecture, CQRS sin
> MediatR (`ExecuteAsync`), repos que devuelven la entidad o lanzan `KeyNotFoundException`, DTOs +
> FluentValidation + AutoMapper, y tests xUnit + FluentAssertions + Moq + EF InMemory (TDD: rojo
> → verde).

## Cómo leer una tarea

Cada tarea padre tiene esta forma:

```text
TAREA REC-xx · <cosa>
  US: historia de usuario
  Depende de: ...
  ├── REC-xx.1  Domain + tests        -> entidades/enums/reglas + tests de dominio
  ├── REC-xx.2  Infrastructure + tests -> EF config + repositorio + tests de repo
  ├── REC-xx.3  Application + tests   -> DTOs/validators/mappings/interfaces/use cases + tests
  └── REC-xx.4  Api + tests           -> controller + policy + tests de integración
```

El orden de ejecución recomendado es **por cosa en vertical**: se termina `REC-xx.1 → .4` antes de
pasar a `REC-xx+1`. Algunas cosas triviales (catálogos) tienen subtareas cortas; las grandes
(Job, Candidate, apply externo) tienen subtareas con varios use cases.

## Convenciones de carpetas (plantilla a copiar por cosa)

```text
MateHR.Domain/Recruitment/
├── Entities/
├── Enums/
├── Interfaces/          # IAreaRepository, IJobRepository...
└── Common/              # validaciones compartidas del módulo

MateHR.Application/Recruitment/
├── Areas/               # 1 carpeta por recurso: Commands/Queries/DTOs/Validators/Interfaces/Mappings
├── SubAreas/ Clients/ JobStatuses/ ProcessSteps/ SearchReasons/ CandidateProcessStatuses/
├── Jobs/ Candidates/ SelectionProcesses/
├── Public/              # apply externo
├── Interfaces/          # servicios del módulo (IEmailService)
├── Mappings/
└── DependencyInjection.cs

MateHR.Infrastructure/Persistance/
├── Configurations/Recruitment/   # una config por entidad
├── Repositories/                 # un repo por recurso
└── Migrations/                   # AddRecruitmentModule

MateHR.Api/Controllers/v1/Recruitment/   # uno por recurso
MateHR.Api/Controllers/v1/Public/        # PublicJobsController
```

Tests (proyecto único `Tests/MateHR.Tests.csproj`, namespace por capa):

```text
Tests/Domain/Recruitment/**            -> MateHR.Domain.Tests.Recruitment.*
Tests/Application/Recruitment/**       -> MateHR.Application.Tests.Recruitment.*
Tests/Infrastructure/Recruitment/**    -> MateHR.Infrastructure.Tests.Recruitment.*
Tests/Api/Recruitment/**               -> MateHR.Api.Tests.Recruitment.*
```

## Decisiones del módulo (DEC-REC)

| # | Decisión | Detalle |
|---|---|---|
| DEC-REC-1 | **Todo es tenant-scoped** | Áreas, subáreas, clientes, estados, steps, search reasons, estados de proceso, jobs, candidatos y procesos tienen `TenantId`. Ninguna consulta puede cruzar tenants. |
| DEC-REC-2 | **El tenant sale del `ITenantContext`** | El use case lee `ITenantContext.CurrentTenantId`; si es `null` lanza `UnauthorizedAccessException` (middleware → 403). El repositorio **siempre** recibe `tenantId` explícito y filtra por él. |
| DEC-REC-3 | **Policies nuevas** | `RequireTenantAdmin` = rol `Admin`; `RequireRecruiter` = roles `Admin`, `Recruiter`. SuperAdmin es plataforma y **no** opera recruitment. |
| DEC-REC-4 | **Catálogos con borrado referencial** | Si un catálogo está referenciado por un job/proceso, el borrado devuelve 409 (`EntityInUseException`). No hay borrado en cascada. |
| DEC-REC-5 | **Steps ordenados por join** | El job guarda `JobStep { Id, JobId, ProcessStepId, Order }`. El orden es contiguo desde 1. |
| DEC-REC-6 | **Borrado de job en 2 pasos** | El recruiter *solicita* (`DeletionRequestedAt`), el admin *confirma* (`DeletionConfirmedAt`, `IsActive=false`, soft delete). |
| DEC-REC-7 | **Candidato externo** | El apply público crea el candidato si el mail no existe y lo marca `IsExternallyCreated=true`. Si existe, solo registra actividad. Dispara notificación/email. |
| DEC-REC-8 | **`ProcessHistory` es derivado** | No se persiste: es la lista de `SelectionProcess` del candidato, que arma el query. |
| DEC-REC-9 | **Teléfono `string`, DNI `int`** | `Dni` int (7-8 dígitos, > 0). `Phone` string (≤ 30) para no perder ceros ni desbordar `int`. |
| DEC-REC-10 | **Coordenadas geográficas** | `Province` y `Country` son enums (`int`), empiezan en 1. `Country` arranca con set inicial; pendiente lista ISO completa. |
| DEC-REC-11 | **Sin psychometric todavía** | El sub-módulo psicotécnico queda como bloque futuro; `SelectionProcess.StatusId` queda preparado para engancharlo. |

## Mapa de tareas

| Tarea | Cosa | Subtareas |
|---|---|---|
| REC-00 | Fundamentos del módulo (enums, guard, excepciones, EF base, DI, policies, middleware) | .1 Domain · .2 Infra · .3 App · .4 Api |
| REC-01 | Estados de job (`JobStatus`) | .1 · .2 · .3 · .4 |
| REC-02 | Áreas (`Area`) | .1 · .2 · .3 · .4 |
| REC-03 | Subáreas (`SubArea`) | .1 · .2 · .3 · .4 |
| REC-04 | Clientes (`Client`, interno/externo) | .1 · .2 · .3 · .4 |
| REC-05 | Pasos de proceso (`ProcessStep`) | .1 · .2 · .3 · .4 |
| REC-06 | Motivos de búsqueda (`SearchReason`) | .1 · .2 · .3 · .4 |
| REC-07 | Estados de proceso de candidato (`CandidateProcessStatus`) | .1 · .2 · .3 · .4 |
| REC-08 | Job/empleo (CRUD, estados, notas, steps, borrado 2 pasos, cierre) | .1 · .2 · .3 · .4 |
| REC-09 | Candidatos (CRUD interno + historial) | .1 · .2 · .3 · .4 |
| REC-10 | Procesos de selección (`SelectionProcess`) | .1 · .2 · .3 · .4 |
| REC-11 | Portal externo (apply) + notificación/email | .1 · .2 · .3 · .4 |

---

# TAREA REC-00 · Fundamentos del módulo

**US:** Como equipo quiero la base del módulo (enums, validaciones, excepciones, persistencia
común, DI y policies) para poder construir cada cosa sobre cimientos firmes.

**Depende de:** — (el módulo Auth/Tenants/Users ya existe)

## Subtarea REC-00.1 — Domain + tests

**Estado:** ✅ Completado (2026-10-23).

**Decisiones tomadas:**
- `Country` queda con `Argentina = 1` (alcance inicial; sin lista ISO por ahora).
- `SeniorityLevel` incluye `Expert = 5` y `Principal = 6`.
- Las excepciones quedan en `MateHR.Domain/Recruitment/Exceptions` (namespace `MateHR.Domain.Recruitment.Exceptions`).
- `JobAlreadyClosedException : InvalidOperationException`, para que el cierre duplicado siga siendo 409 vía middleware.

**Archivos:** `MateHR.Domain/Recruitment/Enums/{SeniorityLevel,ClientType,Province,Country}.cs`,
`MateHR.Domain/Recruitment/Common/RecruitmentGuard.cs`,
`MateHR.Domain/Recruitment/Exceptions/{EntityInUseException,DuplicateEntityNameException,JobAlreadyClosedException}.cs`.

**Código:**

```csharp
namespace MateHR.Domain.Recruitment.Enums
{
    // Empiezan en 1 (convención: el 0 no es un estado válido por accidente).
    public enum SeniorityLevel { Trainee = 1, Junior = 2, SemiSenior = 3, Senior = 4, Expert = 5, Principal = 6 }

    public enum ClientType { Internal = 1, External = 2 }

    // 24 provincias (Argentina), orden alfabético, arranca en 1.
    public enum Province
    {
        BuenosAires = 1, Catamarca = 2, Chaco = 3, Chubut = 4,
        CiudadAutonomaDeBuenosAires = 5, Cordoba = 6, Corrientes = 7, EntreRios = 8,
        Formosa = 9, Jujuy = 10, LaPampa = 11, LaRioja = 12, Mendoza = 13, Misiones = 14,
        Neuquen = 15, RioNegro = 16, Salta = 17, SanJuan = 18, SanLuis = 19, SantaCruz = 20,
        SantaFe = 21, SantiagoDelEstero = 22, TierraDelFuego = 23, Tucuman = 24
    }

    // Alcance inicial: solo Argentina. Pendiente completar la lista ISO completa en el futuro.
    public enum Country { Argentina = 1 }
}
```

```csharp
namespace MateHR.Domain.Recruitment.Common
{
    // Helper del módulo para no repetir ValidateAndTrim en cada entidad.
    public static class RecruitmentGuard
    {
        public static string RequiredName(string? name, int maxLength, string paramName = "Name") { /* trim + throw ArgumentException */ }
        public static TEnum ValidEnum<TEnum>(TEnum value, string paramName = "value") where TEnum : struct, Enum { /* Enum.IsDefined -> ArgumentOutOfRangeException */ }
        public static Guid NotEmptyGuid(Guid value, string paramName = "value") { /* Guid.Empty -> ArgumentException */ }
        public static int NonNegative(int value, string paramName = "value") { /* < 0 -> ArgumentOutOfRangeException */ }
    }
}
```

```csharp
public sealed class EntityInUseException : Exception
{
    public EntityInUseException(string entity, Guid id)
        : base($"No se puede eliminar {entity} {id} porque esta en uso.") { }
}

public sealed class DuplicateEntityNameException : Exception
{
    public string Name { get; }
    public DuplicateEntityNameException(string name)
        : base($"Ya existe un registro con el nombre '{name}'.") { Name = name; }
}

public sealed class JobAlreadyClosedException : InvalidOperationException
{
    public Guid JobId { get; }
    public JobAlreadyClosedException(Guid jobId)
        : base($"El job {jobId} ya esta cerrado.") { JobId = jobId; }
}
```

**Criterios de aceptación**
- [x] `RecruitmentGuard.RequiredName` trimmea, corta vacíos y excede longitud
- [x] Enums fijan sus valores numéricos (contrato de API/DB)

**Tests (REC-00.1-T)** — `Tests/Domain/Recruitment/Enums/RecruitmentEnumsTests.cs` y `Common/RecruitmentGuardTests.cs`:
`SeniorityLevel_Trainee_IsOne`, `ClientType_Internal_IsOne`, `Province_BuenosAires_IsOne`, `Country_Argentina_IsOne`,
`RequiredName_Whitespace_ThrowsArgumentException`, `RequiredName_TooLong_ThrowsArgumentException`, `RequiredName_TrimsValue`,
`ValidEnum_Undefined_ThrowsArgumentOutOfRangeException`, `NotEmptyGuid_Empty_ThrowsArgumentException`, `NonNegative_Negative_ThrowsArgumentOutOfRangeException`.

## Subtarea REC-00.2 — Infrastructure + tests

**Estado:** ⏸️ Pendiente incremental.

**Nota:** No se agregan todos los DbSets de golpe. Se irán completando entidad por entidad junto con cada subtarea **REC-xx.2** (DbSet + configuración EF + repositorio). La migración `AddRecruitmentModule` se creará una vez que estén las configuraciones de todas las entidades necesarias, siguiendo el enfoque del backlog.

**Archivos:** `MateHR.Infrastructure/Persistance/ApplicationDbContext.cs` (DbSets + `StampUpdatedAt`),
migración `AddRecruitmentModule`.

**Código:**

```csharp
// ApplicationDbContext
public DbSet<JobStatus> JobStatuses { get; set; }
public DbSet<Area> Areas { get; set; }
public DbSet<SubArea> SubAreas { get; set; }
public DbSet<Client> Clients { get; set; }
public DbSet<ProcessStep> ProcessSteps { get; set; }
public DbSet<SearchReason> SearchReasons { get; set; }
public DbSet<CandidateProcessStatus> CandidateProcessStatuses { get; set; }
public DbSet<Job> Jobs { get; set; }
public DbSet<JobStep> JobSteps { get; set; }
public DbSet<Candidate> Candidates { get; set; }
public DbSet<SelectionProcess> SelectionProcesses { get; set; }
```

`StampUpdatedAt()` suma `ChangeTracker.Entries<Job>()` (y los catálogos/entidades que tengan `UpdatedAt`).

Comando de migración (crear cuando estén todas las configuraciones de las tareas REC-01…REC-10):

```bash
dotnet ef migrations add AddRecruitmentModule --project MateHR.Infrastructure --startup-project MateHR.Api
dotnet ef database update --project MateHR.Infrastructure --startup-project MateHR.Api
```

**Criterios de aceptación**
- [ ] `OnModelCreating` levanta las configuraciones de `Configurations/Recruitment/*`
- [ ] La migración aplica limpia sobre el esquema actual

**Tests (REC-00.2-T):** sin tests unitarios propios; se valida con los tests de repositorio de cada
tarea y con aplicar la migración en el CI.

## Subtarea REC-00.3 — Application + tests

**Estado:** ✅ Completado (2026-10-23).

**Archivos:** `MateHR.Application/Recruitment/DependencyInjection.cs`,
`MateHR.Application/DependencyInjection.cs` (llamar `services.AddRecruitment()`).

**Código:**

```csharp
// Application/Recruitment/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddRecruitment(this IServiceCollection services)
    {
        // se completa tarea por tarea:
        // services.AddScoped<ICreateArea, CreateArea>(); ...
        return services;
    }
}
```

**Criterios de aceptación**
- [x] `AddApplication()` llama `AddRecruitment()`
- [x] `AssertConfigurationIsValid` pasa con los profiles nuevos

**Tests (REC-00.3-T)** — extender `Tests/Application/Common/MappingConfigurationTests.cs`:
`RecruitmentMappings_AssertConfigurationIsValid_Passes`.

## Subtarea REC-00.4 — Api + tests

**Estado:** ✅ Completado (2026-10-23).

**Archivos:** `MateHR.Api/Authorization/AuthPolicies.cs`, `MateHR.Api/Program.cs`,
`MateHR.Api/Middleware/ExceptionMiddleware.cs`.

**Código:**

```csharp
// AuthPolicies
public const string RequireTenantAdmin = nameof(RequireTenantAdmin);
public const string RequireRecruiter = nameof(RequireRecruiter);
public const string RecruiterRole = "Recruiter";

// Program.cs
options.AddPolicy(AuthPolicies.RequireTenantAdmin, p => p.RequireRole(AuthPolicies.AdminRole));
options.AddPolicy(AuthPolicies.RequireRecruiter, p => p.RequireRole(AuthPolicies.AdminRole, AuthPolicies.RecruiterRole));
```

```csharp
// ExceptionMiddleware: agregar al switch
EntityInUseException or DuplicateEntityNameException or InvalidOperationException => StatusCodes.Status409Conflict,
UnauthorizedAccessException => StatusCodes.Status403Forbidden,
```

**Criterios de aceptación**
- [x] `Recruiter` recibe 403 en endpoints `RequireTenantAdmin`
- [x] `Admin` pasa en ambos; `SuperAdmin`/`Employee` reciben 403 en recruitment interno

**Tests (REC-00.4-T)** — `Tests/Api/Recruitment/*`:
`RequireTenantAdmin_WithRecruiterToken_Returns403`, `RequireRecruiter_WithRecruiterToken_Passes`,
`InvokeAsync_EntityInUseException_Returns409`, `InvokeAsync_DuplicateEntityNameException_Returns409`,
`InvokeAsync_UnauthorizedAccessException_Returns403`.

---

# TAREA REC-01 · Estados de job (`JobStatus`)

**US:** Como admin de tenant quiero gestionar los estados de job (ej. Abierto, En proceso, Cerrado,
Pausado) para clasificar mis búsquedas.

**Depende de:** REC-00

## Subtarea REC-01.1 — Domain + tests

**Archivo:** `MateHR.Domain/Recruitment/Entities/JobStatus.cs`.

**Código:**

```csharp
public class JobStatus
{
    public const int NameMaxLength = 100;
    private JobStatus() { }

    public JobStatus(Guid tenantId, string name)
    {
        RecruitmentGuard.NotEmptyGuid(tenantId, nameof(tenantId));
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Name = RecruitmentGuard.RequiredName(name, NameMaxLength, nameof(name));
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public void Rename(string name) =>
        Name = RecruitmentGuard.RequiredName(name, NameMaxLength, nameof(name));
}
```

**Tests (REC-01.1-T)** — `Tests/Domain/Recruitment/Entities/JobStatusTests.cs`:
`Constructor_ValidData_SetsDefaults`, `Constructor_EmptyTenant_ThrowsArgumentException`,
`Constructor_WhitespaceName_ThrowsArgumentException`, `Constructor_NameTooLong_ThrowsArgumentException`,
`Rename_ValidName_ChangesName`, `Rename_Whitespace_ThrowsAndKeepsName`.

## Subtarea REC-01.2 — Infrastructure + tests

**Archivos:** `MateHR.Infrastructure/Persistance/Configurations/Recruitment/JobStatusConfiguration.cs`,
`MateHR.Infrastructure/Persistance/Repositories/JobStatusRepository.cs`,
`MateHR.Domain/Recruitment/Interfaces/IJobStatusRepository.cs`.

**Código (contrato de catálogo):**

```csharp
public interface IJobStatusRepository
{
    Task<JobStatus> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<PagedResult<JobStatus>> GetAllAsync(Guid tenantId, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<bool> NameExistsAsync(Guid tenantId, string name, Guid? excludeId, CancellationToken ct = default);
    Task<JobStatus> CreateAsync(JobStatus entity, CancellationToken ct = default);
    Task<JobStatus> RenameAsync(Guid tenantId, Guid id, string name, CancellationToken ct = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default);
}
```

```csharp
public class JobStatusRepository : IJobStatusRepository
{
    private readonly ApplicationDbContext _context;
    public JobStatusRepository(ApplicationDbContext context) => _context = context;

    public async Task<JobStatus> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        var entity = await _context.JobStatuses.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);
        if (entity is null) throw new KeyNotFoundException($"No se encontro el estado con ID {id}");
        return entity;
    }

    public Task<bool> NameExistsAsync(Guid tenantId, string name, Guid? excludeId, CancellationToken ct = default)
        => _context.JobStatuses.AsNoTracking().AnyAsync(
            s => s.TenantId == tenantId && s.Name == name && (excludeId == null || s.Id != excludeId), ct);

    public async Task<JobStatus> RenameAsync(Guid tenantId, Guid id, string name, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(tenantId, id, ct);
        entity.Rename(name);                       // muta vía método de la entidad (regla del ReadME)
        await _context.SaveChangesAsync(ct);
        return entity;
    }
    // CreateAsync / DeleteAsync: mismo patrón.
}
```

```csharp
// JobStatusConfiguration
builder.ToTable("JobStatuses");
builder.HasKey(s => s.Id);
builder.Property(s => s.Id).ValueGeneratedNever();
builder.Property(s => s.Name).HasMaxLength(JobStatus.NameMaxLength).IsRequired();
builder.HasIndex(s => new { s.TenantId, s.Name });
```

**Tests (REC-01.2-T)** — `Tests/Infrastructure/Recruitment/Repositories/JobStatusRepositoryTests.cs`:
`GetByIdAsync_Existing_ReturnsEntity`, `GetByIdAsync_Unknown_ThrowsKeyNotFoundException`,
`GetByIdAsync_OtherTenant_ThrowsKeyNotFoundException`, `GetAllAsync_PagesAndCounts`,
`NameExistsAsync_Duplicate_ReturnsTrue`, `NameExistsAsync_ExcludingOwnId_ReturnsFalse`,
`CreateAsync_Persists`, `RenameAsync_ChangesName`, `DeleteAsync_Removes`.

> Trampa InMemory: no aplica índices únicos; el chequeo de duplicado va en Application.

## Subtarea REC-01.3 — Application + tests

**Archivos:** carpeta `MateHR.Application/Recruitment/JobStatuses/`
(`Commands/CreateJobStatus.cs`, `UpdateJobStatus.cs`, `DeleteJobStatus.cs`;
`Queries/GetJobStatusById.cs`, `GetJobStatuses.cs`; `DTOs/…`; `Validators/…`;
`Interfaces/…`; `Mappings/JobStatusMapping.cs`).

**Código (esqueleto de use case, patrón de todo el módulo):**

```csharp
public class CreateJobStatus : ICreateJobStatus
{
    private readonly IJobStatusRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateJobStatusDto> _validator;

    public async Task<JobStatusResponse> ExecuteAsync(CreateJobStatusDto dto, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) throw new ValidationException(validation.Errors);

        var tenantId = _tenantContext.CurrentTenantId
            ?? throw new UnauthorizedAccessException("No hay tenant en el contexto.");

        if (await _repository.NameExistsAsync(tenantId, dto.Name.Trim(), null, ct))
            throw new DuplicateEntityNameException(dto.Name.Trim());

        var entity = await _repository.CreateAsync(new JobStatus(tenantId, dto.Name), ct);
        return _mapper.Map<JobStatusResponse>(entity);
    }
}
```

```csharp
// CreateJobStatusDtoValidator
RuleFor(x => x.Name).NotEmpty().MaximumLength(JobStatus.NameMaxLength);

// JobStatusMapping
CreateMap<JobStatus, JobStatusResponse>();
CreateMap<PagedResult<JobStatus>, PagedResult<JobStatusResponse>>();
```

**Tests (REC-01.3-T)** — `Tests/Application/Recruitment/JobStatuses/*Tests.cs`:
happy path con `Verify`, nombre duplicado → `DuplicateEntityNameException`,
tenant nulo → `UnauthorizedAccessException`, DTO inválido → `ValidationException`,
delete referenciado → `EntityInUseException`, `GetJobStatuses` forwards paging + mapper real.

## Subtarea REC-01.4 — Api + tests

**Archivo:** `MateHR.Api/Controllers/v1/Recruitment/JobStatusesController.cs`.

| Método | Ruta | Policy |
|---|---|---|
| POST | `/api/v1/recruitment/job-statuses` | RequireTenantAdmin |
| GET | `/api/v1/recruitment/job-statuses` | RequireRecruiter |
| GET | `/api/v1/recruitment/job-statuses/{id}` | RequireRecruiter |
| PUT | `/api/v1/recruitment/job-statuses/{id}` | RequireTenantAdmin |
| DELETE | `/api/v1/recruitment/job-statuses/{id}` | RequireTenantAdmin |

**Código (patrón de controller, molde `TenantsController`):**

```csharp
[ApiController]
[Route("api/v1/recruitment/job-statuses")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireTenantAdmin)]
public class JobStatusesController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(JobStatusResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<JobStatusResponse>> Create([FromBody] CreateJobStatusDto request, CancellationToken ct)
    {
        var created = await _create.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
    // GET lista -> [Authorize(Policy = AuthPolicies.RequireRecruiter)] por método
}
```

**Tests (REC-01.4-T)** — `Tests/Api/Recruitment/JobStatusesControllerTests.cs`:
`Endpoints_WithoutToken_Returns401`, `Endpoints_WithEmployeeToken_Returns403`,
`Create_WithAdminToken_Returns201WithLocation`, `Create_DuplicateName_Returns409`,
`GetAll_WithRecruiterToken_Returns200`, `Delete_Referenced_Returns409`.

---

# TAREA REC-02 · Áreas (`Area`)

**US:** Como admin de tenant quiero crear áreas (ej. Tecnología, Ventas) para asignarlas a los jobs.

**Depende de:** REC-00

## Subtarea REC-02.1 — Domain + tests

**Archivo:** `MateHR.Domain/Recruitment/Entities/Area.cs` — misma forma que `JobStatus`
(Id, TenantId, Name + `Rename`), con `NameMaxLength = 100`.

**Tests (REC-02.1-T)** — `AreaTests.cs`: mismos casos que `JobStatusTests`.

## Subtarea REC-02.2 — Infrastructure + tests

**Archivos:** `AreaConfiguration.cs`, `AreaRepository.cs`, `IAreaRepository.cs`
(mismo contrato de catálogo que REC-01.2).

**Tests (REC-02.2-T)** — `AreaRepositoryTests.cs`: mismos casos que `JobStatusRepositoryTests`.
Además `GetByIdAsync_OtherTenant_ThrowsKeyNotFoundException` (clave de DEC-REC-2).

## Subtarea REC-02.3 — Application + tests

**Archivos:** `MateHR.Application/Recruitment/Areas/` (CRUD completo).

**Tests (REC-02.3-T)** — `Tests/Application/Recruitment/Areas/*Tests.cs`: espejo de REC-01.3-T.

## Subtarea REC-02.4 — Api + tests

**Archivo:** `AreasController.cs` con las mismas 5 rutas/policies de REC-01.4
(`/api/v1/recruitment/areas`).

**Tests (REC-02.4-T)** — `AreasControllerTests.cs`: espejo de REC-01.4-T.

---

# TAREA REC-03 · Subáreas (`SubArea`)

**US:** Como admin de tenant quiero crear subáreas dentro de un área (ej. Backend dentro de
Tecnología), para afinar la búsqueda.

**Depende de:** REC-00, REC-02 (valida contra `Area`)

## Subtarea REC-03.1 — Domain + tests

**Archivo:** `MateHR.Domain/Recruitment/Entities/SubArea.cs`.

**Código:**

```csharp
public class SubArea
{
    public const int NameMaxLength = 100;
    private SubArea() { }

    public SubArea(Guid tenantId, Guid areaId, string name)
    {
        RecruitmentGuard.NotEmptyGuid(tenantId, nameof(tenantId));
        RecruitmentGuard.NotEmptyGuid(areaId, nameof(areaId));
        Id = Guid.NewGuid();
        TenantId = tenantId;
        AreaId = areaId;
        Name = RecruitmentGuard.RequiredName(name, NameMaxLength, nameof(name));
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid AreaId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public void Rename(string name) => Name = RecruitmentGuard.RequiredName(name, NameMaxLength, nameof(name));
    public void MoveToArea(Guid areaId) { RecruitmentGuard.NotEmptyGuid(areaId, nameof(areaId)); AreaId = areaId; }
}
```

**Tests (REC-03.1-T)** — `SubAreaTests.cs`: constructor válido, tenant/area vacíos, nombre vacío/largo,
`Rename`, `MoveToArea` válido e inválido.

## Subtarea REC-03.2 — Infrastructure + tests

**Archivos:** `SubAreaConfiguration.cs` (FK `AreaId` → `Areas` con `Restrict` y filtro por tenant),
`SubAreaRepository.cs`, `ISubAreaRepository.cs`.

**Código (punto fino):**

```csharp
// además de GetByIdAsync filtrando por tenant, el repo valida que el área exista en el tenant
public async Task<SubArea> CreateAsync(SubArea subArea, CancellationToken ct = default)
{
    var areaExists = await _context.Areas.AsNoTracking()
        .AnyAsync(a => a.Id == subArea.AreaId && a.TenantId == subArea.TenantId, ct);
    if (!areaExists) throw new KeyNotFoundException($"No se encontro el area {subArea.AreaId}");
    await _context.SubAreas.AddAsync(subArea, ct);
    await _context.SaveChangesAsync(ct);
    return subArea;
}
```

**Tests (REC-03.2-T)** — `SubAreaRepositoryTests.cs`:
`Create_UnknownArea_ThrowsKeyNotFoundException`, `Create_OtherTenantArea_ThrowsKeyNotFoundException`,
`GetAllAsync_FiltersByAreaId`, `GetByIdAsync_OtherTenant_ThrowsKeyNotFoundException`, y los genéricos de catálogo.

## Subtarea REC-03.3 — Application + tests

**Archivos:** `MateHR.Application/Recruitment/SubAreas/`.

**Regla extra:** el use case valida que el `AreaId` exista en el tenant
(`IAreaRepository.GetByIdAsync`); si no → `KeyNotFoundException` (404).

**Tests (REC-03.3-T)** — `Tests/Application/Recruitment/SubAreas/*Tests.cs`:
happy path (verifica `AreaId`), área inexistente → `KeyNotFoundException`,
listado filtrando por `areaId`, nombre duplicado dentro del área.

## Subtarea REC-03.4 — Api + tests

**Archivo:** `SubAreasController.cs` — rutas bajo `/api/v1/recruitment/sub-areas`
(incluye `GET ?areaId=`).

**Tests (REC-03.4-T)** — `SubAreasControllerTests.cs`: 401/403, 201, listado por área, 404 área inexistente.

---

# TAREA REC-04 · Clientes (`Client`)

**US:** Como admin de tenant quiero mantener mis clientes indicando si son **internos** o
**externos**, para asociarlos a los jobs.

**Depende de:** REC-00

## Subtarea REC-04.1 — Domain + tests

**Archivo:** `MateHR.Domain/Recruitment/Entities/Client.cs` (usa `Enums.ClientType`).

**Código:**

```csharp
public class Client
{
    public const int NameMaxLength = 150;
    private Client() { }

    public Client(Guid tenantId, string name, ClientType clientType)
    {
        RecruitmentGuard.NotEmptyGuid(tenantId, nameof(tenantId));
        RecruitmentGuard.ValidEnum(clientType);
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Name = RecruitmentGuard.RequiredName(name, NameMaxLength, nameof(name));
        ClientType = clientType;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ClientType ClientType { get; private set; }

    public void Update(string name, ClientType clientType)
    {
        RecruitmentGuard.ValidEnum(clientType);
        Name = RecruitmentGuard.RequiredName(name, NameMaxLength, nameof(name));
        ClientType = clientType;
    }
}
```

**Tests (REC-04.1-T)** — `ClientTests.cs`:
`Constructor_ValidData_SetsDefaults`, `Constructor_EmptyTenant_Throws…`,
`Constructor_UndefinedClientType_ThrowsArgumentOutOfRangeException`, `Constructor_WhitespaceName_Throws…`,
`Update_ValidData_UpdatesAll`, `Update_UndefinedType_ThrowsAndKeepsValues`.

## Subtarea REC-04.2 — Infrastructure + tests

**Archivos:** `ClientConfiguration.cs` (`ClientType` con `HasConversion<int>()`),
`ClientRepository.cs`, `IClientRepository.cs`.

**Tests (REC-04.2-T)** — `ClientRepositoryTests.cs`: genéricos de catálogo + `Create_PersistsClientType`.

## Subtarea REC-04.3 — Application + tests

**Archivos:** `MateHR.Application/Recruitment/Clients/`.

**Tests (REC-04.3-T)** — `Tests/Application/Recruitment/Clients/*Tests.cs`:
happy path con `ClientType`, enum inválido → `ValidationException`, duplicado, DTO inválido, delete referenciado.

## Subtarea REC-04.4 — Api + tests

**Archivo:** `ClientsController.cs` — `/api/v1/recruitment/clients`
(`clientType` viaja como texto por `JsonStringEnumConverter`).

**Tests (REC-04.4-T)** — `ClientsControllerTests.cs`: 401/403, 201 con `clientType`,
400 enum inválido, 409 duplicado.

---

# TAREA REC-05 · Pasos de proceso (`ProcessStep`)

**US:** Como admin de tenant quiero definir los pasos de un proceso (ej. Screening, Entrevista
técnica, Oferta) para armar el pipeline del job.

**Depende de:** REC-00

## Subtarea REC-05.1 — Domain + tests

**Archivo:** `ProcessStep.cs` — igual a `Area` (Id, TenantId, Name + `Rename`), `NameMaxLength = 100`.
**Tests (REC-05.1-T)** — `ProcessStepTests.cs`: mismos casos que `JobStatusTests`.

## Subtarea REC-05.2 — Infrastructure + tests

**Archivos:** `ProcessStepConfiguration.cs`, `ProcessStepRepository.cs`, `IProcessStepRepository.cs`.
**Tests (REC-05.2-T)** — `ProcessStepRepositoryTests.cs`: genéricos de catálogo.

## Subtarea REC-05.3 — Application + tests

**Archivos:** `MateHR.Application/Recruitment/ProcessSteps/`.
**Tests (REC-05.3-T)** — espejo de REC-01.3-T.

## Subtarea REC-05.4 — Api + tests

**Archivo:** `ProcessStepsController.cs` — `/api/v1/recruitment/process-steps`.
**Tests (REC-05.4-T)** — espejo de REC-01.4-T.

---

# TAREA REC-06 · Motivos de búsqueda (`SearchReason`)

**US:** Como admin de tenant quiero catalogar el motivo de la búsqueda (Reemplazo, Aumento de
equipo, Inversión, etc.) para reportar por qué se abre un job.

**Depende de:** REC-00

## Subtarea REC-06.1 — Domain + tests

**Archivo:** `SearchReason.cs` — igual a `Area`, `NameMaxLength = 150`.
**Tests (REC-06.1-T)** — `SearchReasonTests.cs`: mismos casos que `JobStatusTests`.

## Subtarea REC-06.2 — Infrastructure + tests

**Archivos:** `SearchReasonConfiguration.cs`, `SearchReasonRepository.cs`, `ISearchReasonRepository.cs`.
**Tests (REC-06.2-T)** — `SearchReasonRepositoryTests.cs`: genéricos de catálogo.

## Subtarea REC-06.3 — Application + tests

**Archivos:** `MateHR.Application/Recruitment/SearchReasons/`.
**Tests (REC-06.3-T)** — espejo de REC-01.3-T.

## Subtarea REC-06.4 — Api + tests

**Archivo:** `SearchReasonsController.cs` — `/api/v1/recruitment/search-reasons`.
**Tests (REC-06.4-T)** — espejo de REC-01.4-T.

---

# TAREA REC-07 · Estados de proceso de candidato (`CandidateProcessStatus`)

**US:** Como admin de tenant quiero catalogar los estados de un proceso de selección (Postulado,
En entrevista, Oferta, Contratado, Rechazado); el recruiter los usa y solo los ve.

**Depende de:** REC-00

## Subtarea REC-07.1 — Domain + tests

**Archivo:** `CandidateProcessStatus.cs` — igual a `Area`, `NameMaxLength = 100`.
**Tests (REC-07.1-T)** — `CandidateProcessStatusTests.cs`: mismos casos que `JobStatusTests`.

## Subtarea REC-07.2 — Infrastructure + tests

**Archivos:** `CandidateProcessStatusConfiguration.cs`, `CandidateProcessStatusRepository.cs`, `ICandidateProcessStatusRepository.cs`.
**Tests (REC-07.2-T)** — `CandidateProcessStatusRepositoryTests.cs`: genéricos de catálogo.

## Subtarea REC-07.3 — Application + tests

**Archivos:** `MateHR.Application/Recruitment/CandidateProcessStatuses/`.
**Tests (REC-07.3-T)** — espejo de REC-01.3-T.

## Subtarea REC-07.4 — Api + tests

**Archivo:** `CandidateProcessStatusesController.cs` — `/api/v1/recruitment/candidate-process-statuses`.
**Tests (REC-07.4-T)** — espejo de REC-01.4-T.

---

# TAREA REC-08 · Job/empleo

**US:** Como recruiter/admin quiero crear un job con sus datos, cliente, área/subárea, seniority,
rango salarial, motivo, observaciones y pasos ordenados; y poder consultarlo, filtrarlo por estado,
cambiar su estado/datos/notas, cerrarlo y gestionar su borrado (el recruiter solicita, el admin
confirma).

**Depende de:** REC-00 y los catálogos (REC-01…REC-07)

## Subtarea REC-08.1 — Domain + tests

**Archivos:** `MateHR.Domain/Recruitment/Entities/Job.cs`, `JobStep.cs`.

**Código:**

```csharp
public class Job
{
    public const int NameMaxLength = 150;
    public const int ClientNameMaxLength = 150;
    public const int ObservationsMaxLength = 4000;

    private readonly List<JobStep> _steps = new();
    private Job() { }

    public Job(
        Guid tenantId, string name, SeniorityLevel seniority,
        Guid areaId, Guid subAreaId,
        int salaryMin, int salaryMax,
        ClientType clientType, string clientName,
        Guid jobStatusId, Guid searchReasonId, string? observations)
    {
        RecruitmentGuard.NotEmptyGuid(tenantId, nameof(tenantId));
        RecruitmentGuard.ValidEnum(seniority);
        RecruitmentGuard.NotEmptyGuid(areaId, nameof(areaId));
        RecruitmentGuard.NotEmptyGuid(subAreaId, nameof(subAreaId));
        RecruitmentGuard.NonNegative(salaryMin, nameof(salaryMin));
        RecruitmentGuard.NonNegative(salaryMax, nameof(salaryMax));
        if (salaryMax < salaryMin) throw new ArgumentException("El salario maximo no puede ser menor al minimo.", nameof(salaryMax));
        RecruitmentGuard.ValidEnum(clientType);
        RecruitmentGuard.NotEmptyGuid(jobStatusId, nameof(jobStatusId));
        RecruitmentGuard.NotEmptyGuid(searchReasonId, nameof(searchReasonId));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        Name = RecruitmentGuard.RequiredName(name, NameMaxLength, nameof(name));
        Seniority = seniority;
        AreaId = areaId; SubAreaId = subAreaId;
        SalaryMin = salaryMin; SalaryMax = salaryMax;
        ClientType = clientType;
        ClientName = RecruitmentGuard.RequiredName(clientName, ClientNameMaxLength, nameof(clientName));
        JobStatusId = jobStatusId;
        SearchReasonId = searchReasonId;
        Observations = observations?.Trim();
        IsActive = true;
        OpenDate = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public SeniorityLevel Seniority { get; private set; }
    public Guid AreaId { get; private set; }
    public Guid SubAreaId { get; private set; }
    public int SalaryMin { get; private set; }
    public int SalaryMax { get; private set; }
    public ClientType ClientType { get; private set; }
    public string ClientName { get; private set; } = string.Empty;
    public IReadOnlyList<JobStep> Steps => _steps.AsReadOnly();
    public bool IsActive { get; private set; }
    public Guid JobStatusId { get; private set; }
    public Guid SearchReasonId { get; private set; }
    public DateTimeOffset OpenDate { get; private set; }
    public DateTimeOffset? CloseDate { get; private set; }
    public string? Observations { get; private set; }
    public DateTimeOffset? DeletionRequestedAt { get; private set; }
    public Guid? DeletionRequestedBy { get; private set; }
    public DateTimeOffset? DeletionConfirmedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void UpdateData(string name, SeniorityLevel seniority, Guid areaId, Guid subAreaId,
        int salaryMin, int salaryMax, ClientType clientType, string clientName, Guid searchReasonId) { /* valida + asigna + UpdatedAt */ }
    public void ChangeStatus(Guid jobStatusId) { /* valida + asigna + UpdatedAt */ }
    public void UpdateObservations(string? observations) { /* max length + UpdatedAt */ }
    public void SetSteps(IEnumerable<Guid> processStepIdsInOrder) { /* reemplaza _steps, orden 1..n, sin duplicados */ }
    public void RequestDeletion(Guid userId, DateTimeOffset moment) { /* setea DeletionRequestedAt/By */ }
    public void ConfirmDeletion(DateTimeOffset moment) { /* soft delete: IsActive=false, DeletionConfirmedAt */ }
    public void Close(DateTimeOffset moment) { /* CloseDate=moment, IsActive=false */ }
}

public class JobStep
{
    private JobStep() { }
    public JobStep(Guid jobId, Guid processStepId, int order) { /* valida >0 y no vacíos */ }
    public Guid Id { get; private set; }
    public Guid JobId { get; private set; }
    public Guid ProcessStepId { get; private set; }
    public int Order { get; private set; }
}
```

**Criterios de aceptación**
- [ ] `salaryMax < salaryMin` → `ArgumentException`
- [ ] `SetSteps` ordena 1..n, rechaza duplicados y permite lista vacía
- [ ] `Close` setea `CloseDate` y baja `IsActive`; no se puede cerrar dos veces

**Tests (REC-08.1-T)** — `Tests/Domain/Recruitment/Entities/JobTests.cs` y `JobStepTests.cs`:
`Constructor_ValidData_SetsDefaults`, `Constructor_UndefinedSeniority_Throws…`, `Constructor_SalaryMaxLessThanMin_Throws…`,
`Constructor_UndefinedClientType_Throws…`, `Constructor_WhitespaceName_Throws…`, `SetSteps_OrdersFromOneToN`,
`SetSteps_DuplicateStep_Throws…`, `SetSteps_Empty_ClearsSteps`, `ChangeStatus_UpdatesStatus`,
`UpdateObservations_TooLong_Throws…`, `RequestDeletion_SetsRequestFields`, `ConfirmDeletion_SoftDeletes`,
`ConfirmDeletion_AlreadyInactive_ThrowsInvalidOperationException`, `Close_SetsCloseDateAndDeactivates`,
`Close_AlreadyClosed_Throws…`.

## Subtarea REC-08.2 — Infrastructure + tests

**Archivos:** `MateHR.Infrastructure/Persistance/Configurations/Recruitment/JobConfiguration.cs`,
`JobStepConfiguration.cs`, `JobRepository.cs`, `MateHR.Domain/Recruitment/Interfaces/IJobRepository.cs`.

**Código:**

```csharp
// IJobRepository
public interface IJobRepository
{
    Task<Job> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);   // incluye Steps
    Task<PagedResult<Job>> GetAllAsync(Guid tenantId, Guid? jobStatusId, bool? isActive, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<bool> NameExistsAsync(Guid tenantId, string name, Guid? excludeId, CancellationToken ct = default);
    Task<Job> CreateAsync(Job job, CancellationToken ct = default);
    Task<Job> UpdateAsync(Job job, CancellationToken ct = default);
    Task<Job> ChangeStatusAsync(Guid tenantId, Guid id, Guid jobStatusId, CancellationToken ct = default);
    Task<Job> UpdateObservationsAsync(Guid tenantId, Guid id, string? observations, CancellationToken ct = default);
    Task<Job> SetStepsAsync(Guid tenantId, Guid id, IReadOnlyList<Guid> processStepIdsInOrder, CancellationToken ct = default);
    Task<Job> RequestDeletionAsync(Guid tenantId, Guid id, Guid requestedBy, CancellationToken ct = default);
    Task<Job> ConfirmDeletionAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<Job> CloseAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<bool> IsReferencedByProcessAsync(Guid tenantId, Guid jobId, CancellationToken ct = default);
}
```

```csharp
// JobConfiguration
builder.HasMany(j => j.Steps).WithOne().HasForeignKey(s => s.JobId).OnDelete(DeleteBehavior.Cascade);
builder.Navigation(j => j.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);
builder.Property(j => j.Seniority).HasConversion<int>().IsRequired();
builder.Property(j => j.ClientType).HasConversion<int>().IsRequired();
builder.HasIndex(j => new { j.TenantId, j.Name });
```

```csharp
// JobRepository (puntos finos)
public async Task<Job> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
{
    var job = await _context.Jobs.Include(j => j.Steps)
        .FirstOrDefaultAsync(j => j.Id == id && j.TenantId == tenantId, ct);
    if (job is null) throw new KeyNotFoundException($"No se encontro el job con ID {id}");
    return job;
}

public async Task<Job> SetStepsAsync(Guid tenantId, Guid id, IReadOnlyList<Guid> processStepIdsInOrder, CancellationToken ct = default)
{
    var job = await GetByIdAsync(tenantId, id, ct);
    job.SetSteps(processStepIdsInOrder);   // la entidad arma JobStep con Order 1..n
    await _context.SaveChangesAsync(ct);
    return job;
}
```

**Tests (REC-08.2-T)** — `Tests/Infrastructure/Recruitment/Repositories/JobRepositoryTests.cs`:
búsqueda por id / desconocido / otro tenant, `GetAllAsync` filtra por `jobStatusId` y `isActive` y ordena por `OpenDate` desc,
`CreateAsync` persiste con Steps, `SetStepsAsync` ordena 1..n, `ChangeStatusAsync` cambia estado y estampa `UpdatedAt`,
`RequestDeletionAsync` no borra, `ConfirmDeletionAsync` deja `IsActive=false`, `CloseAsync` setea `CloseDate`.

## Subtarea REC-08.3 — Application + tests

**Archivos:** `MateHR.Application/Recruitment/Jobs/`
(`Commands/`: `CreateJob`, `UpdateJob`, `ChangeJobStatus`, `UpdateJobObservations`, `SetJobSteps`,
`RequestJobDeletion`, `ConfirmJobDeletion`, `CloseJob`; `Queries/`: `GetJobById`, `GetJobs`;
`DTOs/`; `Validators/`; `Interfaces/`; `Mappings/JobMapping.cs`).

**Código:**

```csharp
public class CreateJobDto
{
    public string Name { get; set; } = string.Empty;
    public SeniorityLevel? Seniority { get; set; }
    public Guid AreaId { get; set; }
    public Guid SubAreaId { get; set; }
    public int SalaryMin { get; set; }
    public int SalaryMax { get; set; }
    public ClientType? ClientType { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public Guid JobStatusId { get; set; }
    public Guid SearchReasonId { get; set; }
    public string? Observations { get; set; }
    public List<Guid> ProcessStepIds { get; set; } = new();
}

public class GetJobsRequest
{
    public Guid? JobStatusId { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

**Reglas:** validar DTO → tenant → `AreaId` y `SubAreaId` existen y la subárea pertenece al área
(si no → `ValidationException`/`KeyNotFoundException`) → nombre duplicado en el tenant →
`Job` con `OpenDate=now`, `IsActive=true` → `SetSteps` con los ids ordenados.
`ConfirmJobDeletion` valida que exista request previa (si no → `InvalidOperationException`).

**Tests (REC-08.3-T)** — `Tests/Application/Recruitment/Jobs/*Tests.cs`:
`CreateJobTests`: happy path con `Verify` (seniority, salarios, steps ordenados), subárea de otra área → `ValidationException`,
área/subárea inexistente → `KeyNotFoundException`, nombre duplicado → `DuplicateEntityNameException`,
`SalaryMax < SalaryMin` → `ValidationException`, tenant nulo → `UnauthorizedAccessException`.
`GetJobsTests`: happy path `PagedResult` mapeado, `Verify` de `(jobStatusId, isActive, pageNumber, pageSize)`,
`PageSize=101` → `ValidationException`, `GetJobById` desconocido → `KeyNotFoundException`.
`UpdateJobTests` / `ChangeJobStatusTests` / `UpdateJobObservationsTests` / `SetJobStepsTests`:
happy path + `Verify`, estado inexistente → `KeyNotFoundException`, enum inválido → `ValidationException`,
job de otro tenant → `KeyNotFoundException`, `SetJobSteps` con duplicados → `ValidationException`.
`RequestJobDeletionTests` / `ConfirmJobDeletionTests`: request setea campos, confirm sin request →
`InvalidOperationException`, confirm soft-delete, `Verify` con `tenantId` correcto.
`CloseJobTests`: `CloseDate` seteado + `IsActive=false`, job ya cerrado → `InvalidOperationException`.

## Subtarea REC-08.4 — Api + tests

**Archivo:** `MateHR.Api/Controllers/v1/Recruitment/JobsController.cs`.

| Método | Ruta | Policy |
|---|---|---|
| POST | `/api/v1/recruitment/jobs` | RequireRecruiter |
| GET | `/api/v1/recruitment/jobs?jobStatusId=&isActive=&pageNumber=&pageSize=` | RequireRecruiter |
| GET | `/api/v1/recruitment/jobs/{id}` | RequireRecruiter |
| PUT | `/api/v1/recruitment/jobs/{id}` | RequireRecruiter |
| PATCH | `/api/v1/recruitment/jobs/{id}/status` | RequireRecruiter |
| PATCH | `/api/v1/recruitment/jobs/{id}/observations` | RequireRecruiter |
| PUT | `/api/v1/recruitment/jobs/{id}/steps` | RequireTenantAdmin |
| POST | `/api/v1/recruitment/jobs/{id}/deletion-request` | RequireRecruiter |
| POST | `/api/v1/recruitment/jobs/{id}/deletion-confirm` | RequireTenantAdmin |
| POST | `/api/v1/recruitment/jobs/{id}/close` | RequireRecruiter |

**Tests (REC-08.4-T)** — `Tests/Api/Recruitment/JobsControllerTests.cs`:
`Create_WithRecruiterToken_Returns201WithLocation`, `GetAll_FilterByStatus_ReturnsOnlyMatching`,
`UpdateStatus_Returns200WithNewStatus`, `RequestDeletion_AsRecruiter_Returns200`,
`ConfirmDeletion_AsRecruiter_Returns403`, `ConfirmDeletion_AsAdmin_Returns200`,
`Close_Returns200WithCloseDate`, `Endpoints_WithoutToken_Returns401`.

---

# TAREA REC-09 · Candidatos

**US:** Como recruiter/admin quiero crear, editar, listar/ver y eliminar candidatos (datos
personales, contacto, ubicación, comentarios) viendo el historial de procesos en los que participó.

**Depende de:** REC-00, REC-10 (para el historial)

## Subtarea REC-09.1 — Domain + tests

**Archivo:** `MateHR.Domain/Recruitment/Entities/Candidate.cs`.

**Código:**

```csharp
public class Candidate
{
    public const int FirstNameMaxLength = 100;
    public const int LastNameMaxLength = 100;
    public const int EmailMaxLength = 320;
    public const int PhoneMaxLength = 30;
    public const int LinkedInMaxLength = 300;
    public const int LocationMaxLength = 100;
    public const int CommentsMaxLength = 4000;

    private Candidate() { }

    public Candidate(Guid tenantId, string firstName, string lastName, int dni, string phone,
        string email, string? linkedInUrl, Province province, Country country, string? location,
        string? comments, bool isExternallyCreated)
    {
        RecruitmentGuard.NotEmptyGuid(tenantId, nameof(tenantId));
        if (dni <= 0) throw new ArgumentException("El DNI debe ser mayor a cero.", nameof(dni));
        RecruitmentGuard.ValidEnum(province);
        RecruitmentGuard.ValidEnum(country);
        // email normalizado con MailAddress.TryCreate (igual que User/Tenant)
        Id = Guid.NewGuid();
        TenantId = tenantId;
        // ...
        CreationDate = DateTimeOffset.UtcNow;
        LastActivity = CreationDate;
        IsExternallyCreated = isExternallyCreated;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public int Dni { get; private set; }
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? LinkedInUrl { get; private set; }
    public Province Province { get; private set; }
    public Country Country { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public string? Comments { get; private set; }
    public DateTimeOffset CreationDate { get; private set; }
    public DateTimeOffset LastActivity { get; private set; }
    public bool IsExternallyCreated { get; private set; }

    public void Update(...) { /* valida + asigna + RecordActivity(now) */ }
    public void UpdateComments(string? comments) { /* max length + RecordActivity */ }
    public void RecordActivity(DateTimeOffset moment) { /* LastActivity=moment; no anterior a CreationDate */ }
}
```

> `ProcessHistory` **no** se persiste (DEC-REC-8): lo arma el query cruzando `SelectionProcess`.

**Tests (REC-09.1-T)** — `Tests/Domain/Recruitment/Entities/CandidateTests.cs`:
`Constructor_ValidData_SetsDefaults` (incluye `LastActivity == CreationDate`), `Constructor_NormalizesEmail`,
`Constructor_InvalidEmail_Throws…`, `Constructor_DniZeroOrNegative_Throws…`,
`Constructor_UndefinedProvince_Throws…`, `Constructor_UndefinedCountry_Throws…`,
`Constructor_ExternallyCreated_SetsFlag`, `RecordActivity_UpdatesLastActivity`,
`RecordActivity_BeforeCreationDate_Throws…`, `Update_ValidData_UpdatesAndTouchesActivity`,
`UpdateComments_TooLong_Throws…`.

## Subtarea REC-09.2 — Infrastructure + tests

**Archivos:** `CandidateConfiguration.cs`, `CandidateRepository.cs`, `ICandidateRepository.cs`.

**Código:**

```csharp
public interface ICandidateRepository
{
    Task<Candidate> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<Candidate?> FindByEmailAsync(Guid tenantId, string email, CancellationToken ct = default);  // null permitido (apply externo)
    Task<PagedResult<Candidate>> GetAllAsync(Guid tenantId, string? search, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<Candidate> CreateAsync(Candidate candidate, CancellationToken ct = default);
    Task<Candidate> UpdateAsync(Candidate candidate, CancellationToken ct = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<bool> HasProcessesAsync(Guid tenantId, Guid candidateId, CancellationToken ct = default);
}
```

```csharp
public async Task<Candidate?> FindByEmailAsync(Guid tenantId, string email, CancellationToken ct = default)
{
    var normalized = email.Trim().ToLowerInvariant();
    return await _context.Candidates.AsNoTracking()
        .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Email == normalized, ct);
}
```

**Tests (REC-09.2-T)** — `Tests/Infrastructure/Recruitment/Repositories/CandidateRepositoryTests.cs`:
`FindByEmailAsync_ExistingEmail_ReturnsCandidate`, `FindByEmailAsync_MixedCase_FindsCandidate`,
`FindByEmailAsync_Unknown_ReturnsNull`, `FindByEmailAsync_OtherTenant_ReturnsNull`,
`GetAllAsync_SearchByNameOrEmail_Filters`, `GetAllAsync_OrdersByLastActivityDesc`,
`HasProcessesAsync_WithProcess_ReturnsTrue`, `CreateAsync_Persists`.

## Subtarea REC-09.3 — Application + tests

**Archivos:** `MateHR.Application/Recruitment/Candidates/`
(`Commands/CreateCandidate`, `UpdateCandidate`, `DeleteCandidate`; `Queries/GetCandidateById`, `GetCandidates`).

**Reglas:** email duplicado en el tenant → `DuplicateEntityNameException` (o `EmailAlreadyExistsException`
reutilizada); delete bloqueado si tiene procesos (`HasProcessesAsync` → `EntityInUseException`);
`GetCandidateById` arma `ProcessHistory` con `ISelectionProcessRepository.GetByCandidateAsync`.

**Código:**

```csharp
public class CandidateResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int Dni { get; set; }
    public Province Province { get; set; }
    public Country Country { get; set; }
    public string Location { get; set; } = string.Empty;
    public bool IsExternallyCreated { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public DateTimeOffset LastActivity { get; set; }
    public List<SelectionProcessSummaryResponse> ProcessHistory { get; set; } = new();
}
```

**Tests (REC-09.3-T)** — `Tests/Application/Recruitment/Candidates/*Tests.cs`:
happy path con `Verify`, email duplicado, delete con procesos → `EntityInUseException`,
`GetCandidateById` con `ProcessHistory` mapeado (cross-repo verificado), listado con `search`,
`GetCandidates` forwards paging.

## Subtarea REC-09.4 — Api + tests

**Archivo:** `CandidatesController.cs` — `/api/v1/recruitment/candidates`
(`GET` y `GET/{id}` con `RequireRecruiter`; `POST`/`PUT`/`DELETE` con `RequireRecruiter`).

**Tests (REC-09.4-T)** — `Tests/Api/Recruitment/CandidatesControllerTests.cs`:
`Create_DuplicateEmail_Returns409`, `Delete_WithProcesses_Returns409`,
`GetById_Returns200WithProcessHistory`, `GetAll_WithSearch_Returns200`, 401/403.

---

# TAREA REC-10 · Procesos de selección (`SelectionProcess`)

**US:** Como recruiter/admin quiero registrar que un candidato participa de un job, con su estado,
observación y última actualización, y poder listarlos/verlos/eliminarlos.

**Depende de:** REC-00, REC-02..REC-07, REC-08, REC-09

## Subtarea REC-10.1 — Domain + tests

**Archivo:** `MateHR.Domain/Recruitment/Entities/SelectionProcess.cs`.

**Código:**

```csharp
public class SelectionProcess
{
    public const int ObservationMaxLength = 4000;
    private SelectionProcess() { }

    public SelectionProcess(Guid tenantId, Guid jobId, Guid candidateId, Guid statusId, string? observation)
    {
        RecruitmentGuard.NotEmptyGuid(tenantId, nameof(tenantId));
        RecruitmentGuard.NotEmptyGuid(jobId, nameof(jobId));
        RecruitmentGuard.NotEmptyGuid(candidateId, nameof(candidateId));
        RecruitmentGuard.NotEmptyGuid(statusId, nameof(statusId));
        // ...
        LastUpdate = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid JobId { get; private set; }
    public Guid CandidateId { get; private set; }
    public Guid StatusId { get; private set; }
    public string? Observation { get; private set; }
    public DateTimeOffset LastUpdate { get; private set; }

    public void ChangeStatus(Guid statusId, DateTimeOffset moment) { /* valida + LastUpdate */ }
    public void UpdateObservation(string? observation, DateTimeOffset moment) { /* max length + LastUpdate */ }
}
```

**Tests (REC-10.1-T)** — `Tests/Domain/Recruitment/Entities/SelectionProcessTests.cs`:
`Constructor_ValidData_SetsDefaults`, `Constructor_EmptyJob_Throws…`, `Constructor_EmptyCandidate_Throws…`,
`Constructor_EmptyStatus_Throws…`, `ChangeStatus_UpdatesStatusAndLastUpdate`,
`UpdateObservation_UpdatesObservationAndLastUpdate`, `UpdateObservation_TooLong_Throws…`.

## Subtarea REC-10.2 — Infrastructure + tests

**Archivos:** `SelectionProcessConfiguration.cs`, `SelectionProcessRepository.cs`, `ISelectionProcessRepository.cs`.

**Código:**

```csharp
public interface ISelectionProcessRepository
{
    Task<SelectionProcess> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<PagedResult<SelectionProcess>> GetAllAsync(Guid tenantId, Guid? jobId, Guid? candidateId, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<SelectionProcess>> GetByCandidateAsync(Guid tenantId, Guid candidateId, CancellationToken ct = default); // ProcessHistory
    Task<SelectionProcess> CreateAsync(SelectionProcess process, CancellationToken ct = default);
    Task<SelectionProcess> UpdateAsync(SelectionProcess process, CancellationToken ct = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default);
}
```

**Tests (REC-10.2-T)** — `Tests/Infrastructure/Recruitment/Repositories/SelectionProcessRepositoryTests.cs`:
`GetByCandidateAsync_ReturnsOnlyCandidatesProcessesOrderedByLastUpdate`,
`GetAllAsync_FiltersByJobAndCandidate`, `CreateAsync_Persists`, `UpdateAsync_StampsLastUpdate`,
`GetByIdAsync_OtherTenant_ThrowsKeyNotFoundException`.

## Subtarea REC-10.3 — Application + tests

**Archivos:** `MateHR.Application/Recruitment/SelectionProcesses/`
(`Commands/CreateSelectionProcess`, `UpdateSelectionProcess` (estado + observación), `DeleteSelectionProcess`;
`Queries/GetSelectionProcessById`, `GetSelectionProcesses`).

**Reglas:** job y candidato deben existir en el tenant; el `StatusId` debe existir; al actualizar,
`ChangeStatus`/`UpdateObservation` tocan `LastUpdate`.

**Tests (REC-10.3-T)** — `Tests/Application/Recruitment/SelectionProcesses/*Tests.cs`:
happy path, job/candidato/estado inexistentes → `KeyNotFoundException`, enum inexistente → `ValidationException`,
`Update` setea `LastUpdate`, listado filtrando por `jobId` y por `candidateId`.

## Subtarea REC-10.4 — Api + tests

**Archivo:** `SelectionProcessesController.cs` — `/api/v1/recruitment/selection-processes`
(`GET ?jobId=&candidateId=`). Todas las rutas `RequireRecruiter`.

**Tests (REC-10.4-T)** — `Tests/Api/Recruitment/SelectionProcessesControllerTests.cs`:
`Create_UnknownJob_Returns404`, `Create_Returns201`, `Update_Returns200WithLastUpdate`,
`GetAll_FiltersByJobAndCandidate`, 401/403.

---

# TAREA REC-11 · Portal externo (apply) + notificación/email

**US:** Como persona externa quiero ver los jobs activos y postularme sin autenticarme; el sistema
avisa al tenant para revisarlo y crea el candidato si no existe, marcándolo como externo.

**Depende de:** REC-00, REC-08, REC-09, REC-10

## Subtarea REC-11.1 — Domain + tests

**Archivos:** reutiliza `Candidate` (REC-09.1) y `SelectionProcess` (REC-10.1).
No agrega entidades nuevas; valida la regla "candidato externo".

**Criterios de aceptación**
- [ ] El flag `IsExternallyCreated` es el que distingue el alta por portal
- [ ] No hay reglas de dominio nuevas que no cubran REC-09.1/REC-10.1

**Tests (REC-11.1-T):** cubierto por `CandidateTests.Constructor_ExternallyCreated_SetsFlag`.

## Subtarea REC-11.2 — Infrastructure + tests

**Archivos:** `MateHR.Application/Recruitment/Interfaces/IEmailService.cs`,
`MateHR.Infrastructure/Services/SmtpEmailService.cs` (o `NoOpEmailService` en dev),
`MateHR.Infrastructure/Settings/EmailSettings.cs`, registro en `Infrastructure/DependencyInjection.cs`.

**Código:**

```csharp
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}
```

**Criterios de aceptación**
- [ ] Si `EmailSettings` no está configurado, el `NoOp` no rompe el apply
- [ ] Registro por DI intercambiable (SMTP real vs NoOp)

**Tests (REC-11.2-T):** se testea con un mock de `IEmailService` en Application; el SMTP real queda
como validación manual. Aceptación: el `NoOp` implementa el contrato sin efectos.

## Subtarea REC-11.3 — Application + tests

**Archivos:** `MateHR.Application/Recruitment/Public/`
(`Commands/ApplyToJob.cs`, `Queries/GetPublicJobs.cs`, `GetPublicJobById.cs`; `DTOs/`; `Validators/`).
`IApplyToJob` en `Interfaces/`.

**Reglas:** el job debe existir, ser del tenant y estar activo (si no → 404 / `ValidationException`);
buscar candidato por email:
- no existe → `CreateCandidate(isExternallyCreated: true)` + crear `SelectionProcess` en estado inicial;
- existe → `RecordActivity(now)` + crear `SelectionProcess` (si ya existe el mismo job+candidato → 409);
- disparar `IEmailService.SendAsync` (a `EmailSettings.RecruitmentInbox`).

**Código:**

```csharp
public interface IApplyToJob { Task<ApplyResponse> ExecuteAsync(Guid jobId, ApplyToJobDto dto, CancellationToken ct = default); }

public class ApplyToJobDto
{
    public Guid JobId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Dni { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? LinkedInUrl { get; set; }
    public Province? Province { get; set; }
    public Country? Country { get; set; }
    public string? Location { get; set; }
    public string? Comments { get; set; }
}
```

> El tenant del apply se resuelve por el job (endpoint anónimo: no hay `ITenantContext`). El use case
> busca el job por id global y toma su `TenantId`. Documentar la opción elegida en el test.

**Tests (REC-11.3-T)** — `Tests/Application/Recruitment/Public/ApplyToJobTests.cs`:
candidato nuevo → se crea con `IsExternallyCreated=true` + proceso + email enviado (`Verify`);
candidato existente → `RecordActivity` + NO se duplica el candidato;
postulación repetida al mismo job → `DuplicateEntityNameException`/409;
job inactivo o de otro tenant → `KeyNotFoundException`; email inválido → `ValidationException`.

## Subtarea REC-11.4 — Api + tests

**Archivos:** `MateHR.Api/Controllers/v1/Public/PublicJobsController.cs`.

**Código:**

```csharp
[ApiController]
[Route("api/v1/public/jobs")]
[AllowAnonymous]
public class PublicJobsController : ControllerBase
{
    [HttpGet]                        // lista jobs activos (sin datos sensibles)
    [HttpGet("{id:guid}")]
    [HttpPost("{id:guid}/apply")]    // -> IApplyToJob
}
```

**Notas:** CORS ya permite orígenes del front. Rate-limit queda como deuda técnica anotada.

**Tests (REC-11.4-T)** — `Tests/Api/Recruitment/PublicJobsControllerTests.cs`:
`GetActiveJobs_Anonymous_Returns200WithOnlyActive`,
`Apply_NewCandidate_Returns201AndTaggedExternally`,
`Apply_ExistingCandidate_Returns200AndNoDuplicate`,
`Apply_InactiveJob_Returns404`, `Apply_InvalidBody_Returns400`.

---

# Orden de ejecución sugerido

Se hace **cada tarea completa en vertical** (`REC-xx.1 → .4`) antes de pasar a la siguiente:

```text
REC-00  Fundamentos
REC-01  JobStatus
REC-02  Area
REC-03  SubArea
REC-04  Client
REC-05  ProcessStep
REC-06  SearchReason
REC-07  CandidateProcessStatus
REC-08  Job
REC-09  Candidate
REC-10  SelectionProcess
REC-11  Portal externo (apply) + email
```

**Definition of Done por subtarea:** código mergeado, tests de esa capa en verde (`dotnet test`),
`AssertConfigurationIsValid` pasa cuando corresponda, sin warnings nuevos, y el ticket incluye los
casos de test listados.

## Plantilla para próximos módulos

Al repetir para Employees/Vacations/etc.: copiar el esquema de **una tarea = una cosa con 4
subtareas (Domain/Infra/App/Api)**, listar cada entidad con sus enums y su tabla de rutas/policies,
y mantener el "Definition of Done". Los catálogos se clonan casi 1:1; lo específico de cada módulo
va en la subtarea Application (reglas de negocio) y en la Api (rutas).
