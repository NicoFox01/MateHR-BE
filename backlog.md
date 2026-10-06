# Backlog — Módulo de Tenants y Autenticación

> Documento de planificación. **No contiene código**: sólo tareas, dependencias y criterios de aceptación.
> Última actualización: 2026-10-06 · Estado: Bloques 1 a 4 completados, autenticación JWT completa
> (login / refresh / logout / me, alta de usuarios, validación de SecurityStamp) y `Tenants`
> restringido a SuperAdmin. DEB-01 a DEB-13 resueltos.

## Contexto

Estamos construyendo el módulo de `Tenants` en MateHR, un SaaS de RRHH multi-tenant. Es el primer módulo completo del proyecto y el que va a marcar el patrón para los demás (`Recruitment`, y los que vengan).

El objetivo de esta tanda es cerrar el camino completo de un tenant: DTOs → use cases → controller → endpoint. La tabla `Tenants` ya tiene su migración (`InitialCreate`, aplicada el 2026-10-06), así que los tickets se validaron contra la base y no solo contra el modelo de EF.

### Estado de avance

| Bloque | Alcance | Estado |
|---|---|---|
| 1 | Primitivas del `Domain` (`PagedResult`, `SlugGenerator`, `SlugAlreadyExistsException`) | Completado |
| 2 | Repositorio paginado, `ExistsBySlugAsync`, 409 en el middleware | Completado |
| 3 | Capa `Application` completa | Completado |
| 4 | Cableo en `Api` + controller | Completado |

### Capas y dirección de dependencias

```
Api  →  Infrastructure  →  Application  →  Domain
                            Api  →  Application  →  Domain
```

Regla práctica: **`Application` nunca referencia `Infrastructure`.** Si un tipo se necesita en las dos, vive en `Domain`. Por eso `PagedResult<T>` y `SlugGenerator` están en `Domain/Common/` y no en `Application/Common/`.

---

## Decisiones tomadas

Estas decisiones ya están tomadas. No reabrir el debate sin motivo, pero sí documentarlas acá para que el motivo sobreviva.

| # | Decisión | Motivo |
|---|---|---|
| 1 | El tenant se resuelve por **claim del JWT** | Evita DNS y certificados wildcard. `JwtMiddleware` ya estaba en el plan original |
| 2 | Validación con **FluentValidation** | Carpeta `Validators/` ya reservada; los errores llegan limpios al controller |
| 3 | **El slug se deriva del nombre**, el cliente no lo manda | Menos campos que el front pueda mandar mal. Aplica igual en create y en update |
| 4 | Los enums viajan como **texto** en el JSON | `"status": "Suspended"` y no `3`. Un cliente nunca depende del orden de nuestros enums |
| 5 | `GetTenants` **paginado desde ahora** | Agregar paginación después es un breaking change de la API |
| 6 | Slug duplicado → **409**, con chequeo previo | Sin esto el cliente recibía 500 genérico y no sabía qué campo corregir |
| 7 | `Industry` **arranca en 0** (Opción A, no se renumera) | Aceptado, con la condición de que los DTOs lo declaren nullable + `.NotNull()` |

### Consecuencia de la decisión 7

`Industry` tiene `Technology = 0` implícito, los demás enums arrancan en 1. Si un DTO declara `Industry Industry { get; set; }` no-nullable y el JSON omite el campo, el deserializador asigna `Technology` **en silencio**. Por eso los tickets TEN-02 y TEN-03 lo exigen nullable.

### Consecuencia de la decisión 3

En `update`, renombrar la empresa **cambia su slug**, y por lo tanto su URL. Decisión consciente, no accidente. Además el chequeo de duplicados debe excluir al propio tenant (ver TEN-21), o cada guardado sobre el mismo nombre devolvería 409 falso.

---

## Bloque 3 — Application

### DTOs

#### TEN-01 · `AddressDto`

**Archivo:** `MateHR.Application/Tenants/DTOs/AddressDto.cs`
**Depende de:** —

Cinco propiedades `string`: `Street`, `City`, `State`, `Country`, `PostalCode`.

**Aceptación**
- [ ] Mapea 1:1 con `Domain/Tenants/ValueObjects/Address.cs`
- [ ] Sin constantes propias: las longitudes se referencian desde el `Domain`
- [ ] Namespace `MateHR.Application.Tenants.DTOs`

---

#### TEN-02 · `CreateTenantDto`

**Archivo:** `MateHR.Application/Tenants/DTOs/CreateTenantDto.cs`
**Depende de:** —

Propiedades: `Name`, `Cuit`, `OwnerEmail`, `Industry?` (nullable), `AddressDto?` (nullable).

**No lleva** `Slug`, `Status`, `RecruitmentMode` ni `SubscriptionType`. El slug lo deriva el servidor (decisión 3) y el resto los fija el use case: si el cliente pudiera elegir el status, podría crearse un tenant suspendido.

**Aceptación**
- [ ] `Industry` es nullable (obligatorio por la decisión 7)
- [ ] `Address` es nullable: la dirección es opcional en la entidad
- [ ] El DTO **no** expone `Slug` aunque el constructor de `Tenant` lo pida

---

#### TEN-03 · `UpdateTenantDto`

**Archivo:** `MateHR.Application/Tenants/DTOs/UpdateTenantDto.cs`
**Depende de:** —

Idéntico a TEN-02. Tampoco lleva `Slug`, porque en update el slug también se deriva del nombre.

**Aceptación**
- [ ] Mismos campos y misma nulabilidad que TEN-02
- [ ] Sin `Status`, `RecruitmentMode` ni `SubscriptionType`: esos cambian por sus propios endpoints

---

#### TEN-04 · `TenantResponse`

**Archivo:** `MateHR.Application/Tenants/DTOs/TenantResponse.cs`
**Depende de:** TEN-01

`Id` + los campos de TEN-02 + `Status`, `RecruitmentMode`, `SubscriptionType`, `SubscriptionStatus`, `SubscriptionExpiresAt`, `CreatedAt`, `UpdatedAt` + `AddressDto?`.

**Aceptación**
- [ ] Expone todo lo que la entidad tiene público
- [ ] **No** expone `SubscriptionExpiresAt` como obligatorio: es nullable en la entidad
- [ ] Es el único DTO de salida de los 8 use cases

---

#### TEN-05 · `PagedTenantResponse`

**Archivo:** `MateHR.Application/Tenants/DTOs/PagedTenantResponse.cs`
**Depende de:** TEN-04

Wrapper de paginación sobre `PagedResult<TenantResponse>`.

**Aceptación**
- [ ] **Reutiliza** `PagedResult<T>` de `Domain/Common/`, no re-declara la estructura
- [ ] Si hace falta un tipo propio, mapea los mismos campos (`Items`, `PageNumber`, `PageSize`, `TotalCount`, `TotalPages`, `HasPreviousPage`, `HasNextPage`)
- [ ] El serializado JSON expone esos campos al front


OBSERVACION: TEN-05 QUEDA DESCARTADO, SE USA DIRECTAMENTE `PagedResult<TenantResponse>` EN EL ENDPOINT DE GET TENANTS
---

#### TEN-06 · DTOs de los commands de cambio

**Archivos:**
- `Tenants/DTOs/ChangeTenantStatusDto.cs` → `TenantStatus Status`
- `Tenants/DTOs/ChangeRecruitmentModeDto.cs` → `RecruitmentMode RecruitmentMode`
- `Tenants/DTOs/ChangeSubscriptionTypeDto.cs` → `SubscriptionType SubscriptionType`

Un enum cada uno, para que el body sea `{ "status": "Suspended" }`.

**Aceptación**
- [ ] 3 archivos
- [ ] Cada uno expone un único enum
- [ ] El nombre de la propiedad coincide con el nombre del enum

---

#### TEN-07 · `GetTenantsRequest`

**Archivo:** `MateHR.Application/Tenants/DTOs/GetTenantsRequest.cs`
**Depende de:** —

`TenantStatus? Status`, `int PageNumber = 1`, `int PageSize = 20`.

**Aceptación**
- [ ] Defaults sensatos: página 1, 20 por página
- [ ] `Status` nullable para no filtrar cuando no se pasa
- [ ] El controller lo arma desde query string

---

### Mappings

#### TEN-10 · `TenantMapping`

**Archivo:** `MateHR.Application/Tenants/Mappings/TenantMapping.cs`
**Depende de:** TEN-01, TEN-04

Perfil de AutoMapper.

**Aceptación**
- [ ] Mapea `Tenant → TenantResponse`
- [ ] Mapea `Address → AddressDto`
- [ ] Sin código de lógica: sólo declaraciones de mapeo
- [ ] Namespace `MateHR.Application.Tenants.Mappings`

> Ojo: `Program.cs:8` tiene comentado `AddProfile<CustomerMapping>()`, un tipo que no existe. Al implementar este ticket, esa línea se reemplaza por la config de TEN-40.

---

### Use cases

Cada ticket cubre **interfaz + implementación**: `Tenants/Interfaces/` y `Tenants/Commands/` o `Tenants/Queries/`. Las 8 interfaces existen pero están vacías (`{ }`).

---

#### TEN-20 · `CreateTenant`

**Depende de:** TEN-02, TEN-30, TEN-40

**Aceptación**
- [ ] Genera el slug con `SlugGenerator.Generate(dto.Name)`
- [ ] Consulta `ExistsBySlugAsync(slug, excludeTenantId: null)`; si existe, lanza `SlugAlreadyExistsException` → 409
- [ ] Fija `TenantStatus.Active`, `RecruitmentMode.Internal` y `SubscriptionType.Free` en el servidor
- [ ] `SubscriptionStatus` queda en `None` — eso ya lo hace el constructor de la entidad
- [ ] Persiste vía `CreateTenantAsync` y devuelve `TenantResponse`
- [ ] Propaga `CancellationToken` hasta el repositorio
- [ ] Si `SlugGenerator` lanza (nombre que no genera slug válido), el error llega al cliente como 400

---

#### TEN-21 · `UpdateTenant`

**Depende de:** TEN-03, TEN-31, TEN-20

**Aceptación**
- [ ] Carga el tenant por id (404 si no existe)
- [ ] Re-deriva el slug del nombre nuevo
- [ ] Consulta `ExistsBySlugAsync(slug, excludeTenantId: dto.Id)` — **el exclude es obligatorio**: sin él, guardar un tenant sin cambiarle el nombre da 409 falso
- [ ] Pasa el slug derivado a `UpdateTenantAsync`
- [ ] Devuelve `TenantResponse` con el nombre ya normalizado (trim) y el slug derivado

---

#### TEN-22 · `ChangeStatusTenant`

**Depende de:** TEN-06, TEN-32

**Aceptación**
- [ ] Cargar por id → `ChangeStatus(status)` → guardar
- [ ] 404 si el tenant no existe
- [ ] `IsInEnum()` en el validator: un status inventado devuelve 400, no un 500

---

#### TEN-23 · `ChangeRecruitmentModeTenant`

**Depende de:** TEN-06, TEN-33

**Aceptación**
- [ ] Cargar por id → `ChangeRecruitmentMode(recruitmentMode)` → guardar
- [ ] 404 si no existe

---

#### TEN-24 · `ChangeSubscriptionTypeTenant`

**Depende de:** TEN-06, TEN-34
**Bloqueado por:** DEB-01

**Aceptación**
- [ ] Cargar por id → `ChangeSubscriptionType(subscriptionType)` → guardar
- [ ] Verificar que `ChangeSubscriptionType` resetea `SubscriptionStatus` a `None` y `SubscriptionExpiresAt` a `null`
- [ ] **No arranca hasta cerrar DEB-01**: el método del repositorio tira `NotImplementedException`

---

#### TEN-25 · `GetTenantById`

**Depende de:** TEN-04, TEN-40

**Aceptación**
- [ ] Usa `GetTenantByIdAsync`, que ya lanza `KeyNotFoundException`
- [ ] El middleware la mapea a 404 — no hace falta try/catch en el use case
- [ ] Devuelve `TenantResponse`

---

#### TEN-26 · `GetTenantBySlug`

**Depende de:** TEN-04, TEN-40

**Aceptación**
- [ ] **Normaliza el input antes de buscar**: la entidad normaliza a minúsculas al escribir, así que `"Acme-SA"` tiene que encontrar a `acme-sa`
- [ ] 404 si no existe
- [ ] Este endpoint es público (lo usa el login), no debe exigir tenant resuelto

---

#### TEN-27 · `GetTenants`

**Depende de:** TEN-05, TEN-07, TEN-35, TEN-40

**Aceptación**
- [ ] Devuelve `PagedResult<TenantResponse>`
- [ ] Pasa `PageNumber` y `PageSize` al repositorio
- [ ] Aplica el filtro opcional por `Status`
- [ ] `PageSize` validado entre 1 y 100 en TEN-35

---

### Validators

#### TEN-30 · `CreateTenantDtoValidator`

**Depende de:** TEN-02

**Aceptación**
- [ ] `Name`: `.NotEmpty()` + `.MaximumLength(Tenant.NameMaxLength)` (200)
- [ ] `Cuit`: `.NotEmpty()` + `.Length(11)` + sólo dígitos — la entidad también valida, pero acá el mensaje llega mejor
- [ ] `OwnerEmail`: `.NotEmpty()` + `.EmailAddress()` + `.MaximumLength(Tenant.OwnerEmailMaxLength)` (320)
- [ ] `Industry`: **`.NotNull()`** — cierra el hole de la decisión 7
- [ ] `Address`: reglas anidadas si viene; `Street` `.MaximumLength(Address.StreetMaxLength)` (200), `City`/`State`/`Country` 100, `PostalCode` 20
- [ ] Las longitudes se referencian desde las constantes del `Domain`, nunca escritas a mano

---

#### TEN-31 · `UpdateTenantDtoValidator`

**Depende de:** TEN-03

**Aceptación**
- [ ] Mismas reglas que TEN-30
- [ ] Sin regla de `Slug`: no hay slug en el DTO

---

#### TEN-32 · `ChangeTenantStatusDtoValidator`

**Aceptación**
- [ ] `IsInEnum()` sobre `Status`

---

#### TEN-33 · `ChangeRecruitmentModeDtoValidator`

**Aceptación**
- [ ] `IsInEnum()` sobre `RecruitmentMode`

---

#### TEN-34 · `ChangeSubscriptionTypeDtoValidator`

**Aceptación**
- [ ] `IsInEnum()` sobre `SubscriptionType`

---

#### TEN-35 · `GetTenantsRequestValidator`

**Aceptación**
- [ ] `PageNumber`: `.GreaterThanOrEqualTo(1)`
- [ ] `PageSize`: `.InclusiveBetween(1, 100)`
- [ ] El techo de 100 es para que nadie pida toda la tabla de un saque

> `PagedResult<T>` ya valida en su constructor y lanza `ArgumentOutOfRangeException` (→ 400). El validator es la primera barrera y da un mensaje más claro; el constructor es la red de seguridad.

---

## Bloque 4 — Api

#### TEN-40 · `AddApplication()`

**Archivo:** `MateHR.Application/DependencyInjection.cs`
**Depende de:** TEN-10, TEN-30

Hoy la clase está vacía y **no es `static`**.

**Aceptación**
- [ ] Convertirla en `static class`
- [ ] Método `AddApplication(this IServiceCollection services)`
- [ ] Registra los 8 use cases como `Scoped`
- [ ] `AddValidatorsFromAssembly` (ya está el paquete `FluentValidation.DependencyInjectionExtensions`)
- [ ] `AddAutoMapper` con el assembly de `Application`
- [ ] Devuelve `services` para poder encadenar

---

#### TEN-41 · Registrar `AddApplication()` en `Program.cs`

**Depende de:** TEN-40

**Aceptación**
- [ ] `builder.Services.AddApplication();` antes de `AddInfrastructure(...)`
- [ ] Reemplazar el `AddAutoMapper` comentado de la línea 8, que apunta a `CustomerMapping()` —un tipo inexistente—

---

#### TEN-42 · Enums como texto

**Archivo:** `MateHR.Api/Program.cs`

**Aceptación**
- [ ] `JsonStringEnumConverter` agregado a `AddControllers()`
- [ ] `"status": "Suspended"` en lugar de `"status": 3`

---

#### TEN-43 · `TenantsController`

**Archivo:** `MateHR.Api/Controllers/TenantsController.cs`
**Depende de:** TEN-20 a TEN-27

`Api/Controllers/` está vacía. La carpeta ya está en el `.csproj` del proyecto API.

**Aceptación**
- [ ] `[ApiController]` + `[Route("api/[controller]")]`
- [ ] CRUD de `Tenant` + los 3 endpoints de cambio
- [ ] Devuelve `TenantResponse` o `PagedResult<TenantResponse>` — **nunca la entidad**
- [ ] `201 Created` en create, `204 NoContent` o el recurso actualizado en update
- [ ] `400` si el validator falla; el mensaje de error se muestra al cliente
- [ ] Autenticado: `[Authorize]` salvo el create y el `GetTenantBySlug`, que son los dos paths públicos de onboarding

---

## Deuda técnica

Encontrada revisando el código. No pertenece a los bloques 3 y 4, pero se pierde si no queda escrita.

| ID | Qué | Dónde | Prioridad | Estado |
|---|---|---|---|---|
| DEB-01 | `UpdateSusccriptionTypeTenantAsync` lanza `NotImplementedException`. Compila, pero devuelve 500 genérico | `TenantRepository.cs:113` | Alta — bloquea TEN-24 | Resuelto |
| DEB-02 | Nombre de archivo no coincide con la clase: `SlugAlreadyExistException.cs` vs `SlugAlreadyExistsException`. Compila igual | `Domain/Exceptions/` | Baja | Resuelto — el archivo se llama igual que la clase |
| DEB-03 | `Program.cs:8` comentado apunta a `CustomerMapping()`, que no existe | `MateHR.Api/Program.cs` | Baja | Resuelto en TEN-41 |
| DEB-04 | `UseAuthorization()` sin `UseAuthentication()` antes: hoy es código muerto | `Program.cs:25` | Media | Resuelto — se registró JWT bearer |
| DEB-05 | Migración `InitialCreate` nunca generada. La tabla `Tenants` no existe en la base | — | Alta | Resuelto el 2026-10-06 |
| DEB-06 | 5 warnings `CS8618` en `Address`: propiedades no-nullables sin constructor | `ValueObjects/Address.cs:14-18` | Baja | Resuelto el 2026-10-06 — se inicializan a `string.Empty` |
| DEB-07 | `Infrastructure` tenía `Nullable=disable` mientras `Domain` y `Application` lo tenían en `enable` | `Infrastructure.csproj:6` | Media | Resuelto el 2026-10-06 — `Nullable=enable` sin warnings nuevos |
| DEB-08 | Con `Nullable=disable` había que poner `#nullable enable` archivo por archivo en los repositorios | `UserRepository.cs`, `RefreshTokenRepository.cs` | Baja | Resuelto junto con DEB-07 — ya no hacen falta |
| DEB-09 | No había forma de crear el primer usuario: sin registro ni endpoint administrativo | `MateHR.Api` | **Alta** | Resuelto el 2026-10-06 — `POST /api/v1/users` (SuperAdmin) + comando `--seed-superadmin` |
| DEB-10 | No existía `/api/v1/auth/refresh`: el refresh token se emitía y revocaba, pero nada lo canjeaba | `AuthController.cs` | Media | Resuelto el 2026-10-06 — rotación con detección de reuso |
| DEB-11 | `SecurityStamp` se emitía en el JWT pero no se validaba contra la base | `JwtService.cs` | Media | Resuelto el 2026-10-06 — `SecurityStampValidationMiddleware` con caché de 30 s |
| DEB-12 | `RefreshToken.ReplaceWith()` estaba implementado pero sin usar | `RefreshToken.cs` | Baja | Resuelto el 2026-10-06 — lo usa la rotación de `/auth/refresh` |
| DEB-13 | `UserRole` empezaba en 0 (`SuperAdmin=0`), así que el `default` era un rol válido | `Domain/Users/Enums/UserRole.cs` | Baja | Resuelto el 2026-10-06 — ahora 1..4, con migración de remapeo |

### Sobre DEB-07

Con nullable deshabilitado en `Infrastructure`, una asignación floja como `tenantContext.CurrentTenantId.Value` **no genera warning**. El compilador avisa en `Application` (nullable activo) pero no en `Infrastructure`. Resuelto el 2026-10-06 activando `Nullable` en la capa: no apareció ningún warning nuevo y los `#nullable enable` por archivo quedaron como redundantes.

---

## Fuera de alcance

Decisiones ya tomadas para **no** hacer en esta tanda. No re-preguntar.

| # | Qué | Por qué después |
|---|---|---|
| 1 | `Subscribe`, `EnterGracePeriod`, `ExpireSubscription`, `CancelSubscription` | Las reglas de facturación no están definidas |
| 2 | `Suspend`, `Reactivate`, `EnsurePaidPlan` | Mismo motivo |
| 3 | `HasSubscriptionExpiredAt` | Depende de #1 |
| 4 | `BackgroundService` de tenants vencidos | Depende de #1 |
| 5 | `tenant_id` en el resto de las tablas | Requiere las entidades `Employee` / `User` |
| 6 | Filtro global de EF (`HasQueryFilter`) | Depende de #5: sin `tenant_id` no hay nada que filtrar |
| 7 | `TenantMiddleware` + `JwtSettings` + `IJwtService` | Va en el mismo commit que el login, para que el nombre del claim sea consistente |
| 8 | `TenantLimit` como value object | Los límites siguen como `const` en `Tenant`. El nombre se reservó para cuotas del plan |
| 9 | `Address` como value object real | Hoy es una clase con setters públicos |
| 10 | Tests unitarios | Decisión del usuario: primero el flujo completo |

> **Trampa conocida con el claim:** el nombre del claim (`tenant_id`) es un contrato entre `TenantMiddleware` y `JwtService`. Si uno lo escribe `tenant_id` y el otro `TenantId`, **no hay error de compilación** — el middleware devuelve `null` en silencio y el aislamiento no filtra. Por eso van en el mismo commit.

---

## Orden de ejecución sugerido

```
Bloque 3a — DTOs        TEN-01 … TEN-07   (sin dependencias)
Bloque 3b — Mapping     TEN-10           (necesita TEN-01, TEN-04)
Bloque 3c — Validators  TEN-30 … TEN-35  (necesitan los DTOs)
Bloque 3d — Use cases   TEN-20 … TEN-27  (necesitan 3a, 3b, 3c)
Bloque 4  — Api         TEN-40 … TEN-43  (necesita 3d)
```

Antes de poder probar cualquier endpoint hace falta **DEB-05**: generar y aplicar la migración inicial.
