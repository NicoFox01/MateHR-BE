## Bloque 5 — Tests (TDD)

> Acá va la guía para que vos mismo escribas los tests de cada archivo. El scaffolding ya está
> armado (proyectos `MateHR.*.Tests`, paquetes, referencias y `.slnx`), así que no hace falta
> crear nada más: solamente crear los `.cs` dentro de las carpetas espejo y correrlos.
>
> **Cómo correr los tests:**
> ```bash
> dotnet test                       # todos los proyectos
> dotnet test MateHR.Application.Tests    # una sola capa
> dotnet test --filter "ClassName=CreateTenantTests"   # un solo archivo
> ```
>
> **Regla de oro (TDD):** escribí el test ANTES de arreglar el código. El test debe fallar
> (rojo) → después hacés que pase (verde) → y recién ahí refactorizás. Si el test pasa sin tocar
> código, es porque no está probando lo que tiene que probar.
>
> **Estilo:** xUnit. Cada test es `[Fact]` (o `[Theory]` + `[InlineData]` para variantes).
> Aserts con **FluentAssertions** (`.Should().Be(...)`). Mocks con **Moq** (`Mock<I...>`).
> Cada archivo se llama `NombreTests.cs` y vive en la carpeta espejo del proyecto `.Tests`.

### Prerrequisito para toda la capa Api (TEST-00)

`Program.cs` usa *top-level statements*: la clase `Program` generada es `internal` y
`WebApplicationFactory<Program>` necesita verla. Agregá al final de `Program.cs` (o en un
`Api/TestsMarker.cs` aparte):

```csharp
public partial class Program { }
```

Sin esto, los tests de integración de `MateHR.Api.Tests` no compilan.

### Estructura de archivos esperada (dónde va cada test)

```
MateHR.Domain.Tests/
  Common/SlugGeneratorTests.cs, PagedResultTests.cs
  Tenants/Entities/TenantTests.cs
  Tenants/ValueObjects/AddressTests.cs
  Tenants/Enums/IndustryEnumTests.cs
  Users/Entities/UserTests.cs, RefreshTokenTests.cs
  Exceptions/ExceptionsTests.cs
MateHR.Application.Tests/
  Common/MappingConfigurationTests.cs
  Tenants/Commands/CreateTenantTests.cs, UpdateTenantTests.cs,
              ChangeStatusTenantTests.cs, ChangeRecruitmentModeTenantTests.cs,
              ChangeSubscriptionTypeTenantTests.cs
  Tenants/Queries/GetTenantsTests.cs, GetTenantByIdTests.cs, GetTenantBySlugTests.cs
  Tenants/Validators/CreateTenantDtoValidatorTests.cs, GetTenantsRequestValidatorTests.cs
  Users/Commands/LoginUserTests.cs, RefreshAccessTokenTests.cs,
              CreateUserTests.cs, LogoutUserTests.cs
  Users/Queries/GetCurrentUserTests.cs
  Users/Services/UserProvisioningServiceTests.cs
  Users/Validators/CreateUserRequestValidatorTests.cs, LoginRequestValidatorTests.cs
MateHR.Infrastructure.Tests/
  Persistance/Repositories/TenantRepositoryTests.cs, UserRepositoryTests.cs,
                  RefreshTokenRepositoryTests.cs
  Authentication/PasswordHasherTests.cs, JwtServiceTests.cs, RefreshTokenServiceTests.cs
  Services/TenantContextTests.cs, UserSecurityStampValidatorTests.cs
MateHR.Api.Tests/
  Controllers/v1/AuthControllerTests.cs, TenantsControllerTests.cs, UsersControllerTests.cs
  Middleware/ExceptionMiddlewareTests.cs
```

---

## Bloque 5a — Domain (tests puros, sin mocks)

#### TEST-01 · `SlugGeneratorTests`

**Archivo:** `MateHR.Domain.Tests/Common/SlugGeneratorTests.cs`
**Depende de:** —

Probar el generador de slugs. Mirá el código y vas a ver la lógica: pasa a minúsculas, espacios
→ `-`, elimina todo lo que no sea ascii letra/dígito o `-`, colapsa `--` repetidos y recorta
guiones de los bordes. Lanza `ArgumentException` si entra null/vacío o si el nombre no genera
slug (ej. `"!!!"`).

**Casos:**
- `"Empresa Tech SA"` → `"empresa-tech-sa"`
- `"  Empresa   Otra  "` → `"empresa-otra"` (colapsa espacios y guiones, trima bordes)
- `"Café"` → `"caf"` (**trampa:** la `é` no es ascii, se tira; quedate atento a que no rompa el código de producción, testealo y listo)
- `"123"` → `"123"`
- `"a--b"` → `"a-b"`
- `Generate(null)`, `Generate("")`, `Generate("   ")` → `ArgumentException`
- `Generate("!!!")` → `ArgumentException`

**Aceptación**
- [ ] Cubre minúsculas, espacios, caracteres raros, colapso de guiones y casos de error
- [ ] Usa `[Theory]`/`[InlineData]` para los casos de transformación

---

#### TEST-02 · `PagedResultTests`

**Archivo:** `MateHR.Domain.Tests/Common/PagedResultTests.cs`

Resultado paginado genérico. Fijate en `PagedResult.cs`: calcula `TotalPages` con redondeo hacia
arriba, `HasPreviousPage = PageNumber > 1`, `HasNextPage = PageNumber < TotalPages`, y valida que
`pageNumber` y `pageSize` sean ≥ 1.

**Casos:**
- 10 ítems, page 1, size 20 → `TotalPages == 1`, `HasNextPage == false`
- 50 ítems, page 1, size 20 → `TotalPages == 3`
- `TotalCount == 0` → `TotalPages == 0`
- page 2 de 3 → `HasPreviousPage == true` y `HasNextPage == true`
- `pageNumber = 0` → `ArgumentOutOfRangeException`; `pageSize = 0` → `ArgumentOutOfRangeException`
- `Items` guarda la lista que se le pasa

---

#### TEST-03 · `TenantTests`

**Archivo:** `MateHR.Domain.Tests/Tenants/Entities/TenantTests.cs`

Entidad `Tenant`. El constructor valida todo (enums con `ValidateEnum`, nombre obligatorio y ≤ 200,
CUIT de 11 dígitos, email válido y ≤ 320, slug normalizado) y los setters son privados: los cambios
van por `ChangeStatus`, `ChangeRecruitmentMode`, `ChangeSubscriptionType` y `Update`. Vamos a
escribir un test por validación y por método de cambio, siempre con el patrón:
*nombre del test → datos que le pasamos → qué procesamos → qué resultado esperamos*.

**Tests del constructor:**

- Vamos a generar un test llamado **`Constructor_ValidData_SetsDefaults`**. Le pasamos
  `name = "  Acme Corp  "`, `slug = "Mi Empresa"`, `cuit = "20304050601"`,
  `ownerEmail = "owner@acme.com"`, `industry = Technology`, `address = null`,
  `status = Active`, `recruitmentMode = Internal`, `subscriptionType = Free`. Vamos a procesar
  la información construyendo `new Tenant(...)` y esperamos que no lance nada, que `Id` no sea
  `Guid.Empty`, que `Name == "Acme Corp"` (trimmeado), que `Slug == "mi-empresa"` (normalizado por
  `SlugGenerator`), que `Status`, `RecruitmentMode` y `SubscriptionType` queden con los valores que
  pasamos, que `SubscriptionStatus == None`, `SubscriptionExpiresAt == null` y que `CreatedAt`
  esté seteado cerca de `DateTimeOffset.UtcNow`.

- Vamos a generar un test llamado **`Constructor_WhitespaceName_ThrowsArgumentException`**.
  Le pasamos un `name = "   "` y todo lo demás válido; vamos a procesar el `new Tenant(...)`
  y esperamos que lance `ArgumentException` con `ParamName == "name"`.

- Vamos a generar un test llamado **`Constructor_NameExceedsMaxLength_ThrowsArgumentException`**.
  Le pasamos un `name` de 201 caracteres (`new string('a', 201)`) y el resto válido; procesamos el
  constructor y esperamos `ArgumentException` cuyo mensaje mencione los 200 caracteres.

- Vamos a generar un test llamado **`Constructor_WhitespaceSlug_ThrowsArgumentException`**.
  Le pasamos `slug = ""` y el resto válido; procesamos el constructor y esperamos
  `ArgumentException` con `ParamName == "slug"`.

- Vamos a generar un test llamado **`Constructor_SlugWithoutValidAscii_ThrowsArgumentException`**.
  Le pasamos `slug = "!!!"` (no genera ningún slug) y el resto válido; procesamos el constructor y
  esperamos `ArgumentException`, porque `SlugGenerator.Generate` explota antes de llegar a
  `NormalizeSlug`.

- Vamos a generar un test llamado **`Constructor_CuitWithLetters_ThrowsArgumentException`**.
  Le pasamos `cuit = "20A30405061"` (11 caracteres pero con una letra) y el resto válido;
  procesamos el constructor y esperamos `ArgumentException` con
  `ParamName == "cuit"`.

- Vamos a generar un test llamado **`Constructor_CuitWrongLength_ThrowsArgumentException`**.
  Le pasamos `cuit = "2030405060"` (10 dígitos) y el resto válido; procesamos el constructor y
  esperamos `ArgumentException`. Podemos escribirlo como `[Theory]` con `[InlineData]` para 10 y 12
  dígitos.

- Vamos a generar un test llamado **`Constructor_InvalidEmail_ThrowsArgumentException`**.
  Le pasamos `ownerEmail = "no-es-un-email"` y el resto válido; procesamos el constructor y
  esperamos `ArgumentException` con `ParamName == "ownerEmail"` (lo rechaza
  `MailAddress.TryCreate`).

- Vamos a generar un test llamado **`Constructor_UndefinedIndustry_ThrowsArgumentOutOfRangeException`**.
  Le pasamos `industry = (Industry)999` y el resto válido; procesamos el constructor y esperamos
  `ArgumentOutOfRangeException`, porque `ValidateEnum` chequea `Enum.IsDefined`. Repetimos la misma
  historia con **`Constructor_UndefinedStatus_...`**, **`Constructor_UndefinedRecruitmentMode_...`**
  y **`Constructor_UndefinedSubscriptionType_...`**, pasando `(TenantStatus)999`,
  `(RecruitmentMode)999` y `(SubscriptionType)999` respectivamente.

**Tests de los métodos de cambio:**

- Vamos a generar un test llamado **`ChangeStatus_ValidStatus_ChangesStatus`**. Le pasamos a un
  tenant construido con `Status = Active` el valor `TenantStatus.Suspended`; procesamos
  `tenant.ChangeStatus(TenantStatus.Suspended)` y esperamos que `tenant.Status` quede en
  `Suspended` (y que el resto de las propiedades no cambie).

- Vamos a generar un test llamado **`ChangeStatus_UndefinedStatus_ThrowsArgumentOutOfRangeException`**.
  Le pasamos `(TenantStatus)999` a un tenant vivo; procesamos `ChangeStatus(...)` y esperamos
  `ArgumentOutOfRangeException` y que `Status` siga siendo el original (el método valida antes de
  asignar).

- Vamos a generar un test llamado **`ChangeRecruitmentMode_ValidMode_ChangesMode`**. Le pasamos
  `RecruitmentMode.External` a un tenant que arranca en `Internal`; procesamos
  `ChangeRecruitmentMode(...)` y esperamos que `RecruitmentMode == External`.

- Vamos a generar un test llamado **`ChangeRecruitmentMode_UndefinedMode_ThrowsArgumentOutOfRangeException`**.
  Le pasamos `(RecruitmentMode)999`; procesamos el método y esperamos
  `ArgumentOutOfRangeException`.

- Vamos a generar un test llamado **`ChangeSubscriptionType_ValidType_ResetsSubscriptionStatusAndExpiry`**.
  Le pasamos `SubscriptionType.Monthly` a un tenant `Free`; procesamos
  `ChangeSubscriptionType(SubscriptionType.Monthly)` y esperamos que `SubscriptionType == Monthly`,
  que `SubscriptionStatus` siga en `None` y que `SubscriptionExpiresAt` quede en `null` (el método
  siempre resetea la suscripción vieja: es el invariante que queremos fijar aunque arranque igual).

- Vamos a generar un test llamado **`ChangeSubscriptionType_UndefinedType_ThrowsArgumentOutOfRangeException`**.
  Le pasamos `(SubscriptionType)999`; procesamos el método y esperamos
  `ArgumentOutOfRangeException` y que `SubscriptionType` no cambie.

- Vamos a generar un test llamado **`Update_ValidData_UpdatesAllFields`**. Le pasamos
  `name = "  Nueva Empresa  "`, `slug = "Nueva Empresa"`, `cuit = "11222333444"`,
  `ownerEmail = "nuevo@empresa.com"`, `industry = Finance` y un `Address` con valores nuevos;
  procesamos `tenant.Update(...)` y esperamos que `Name == "Nueva Empresa"` (trimmeado),
  `Slug == "nueva-empresa"` (normalizado), `CUIT`, `OwnerEmail`, `Industry` y `Address` actualizados,
  y que `Status`/`SubscriptionType` no se toquen.

- Vamos a generar un test llamado **`Update_WhitespaceName_ThrowsArgumentException`**. Le pasamos
  `name = "   "` al `Update`; procesamos y esperamos `ArgumentException`, y de paso verificamos que
  el tenant quedó igual que antes (la validación corta antes de asignar).

- Vamos a generar un test llamado **`Update_InvalidEmail_ThrowsArgumentException`**. Le pasamos
  `ownerEmail = "abc"`; procesamos y esperamos `ArgumentException`.

- Vamos a generar un test llamado **`Update_UndefinedIndustry_ThrowsArgumentOutOfRangeException`**.
  Le pasamos `(Industry)999`; procesamos y esperamos `ArgumentOutOfRangeException`.

**Aceptación**
- [ ] Cubre las validaciones del constructor (nombre, slug, CUIT, email y los 4 enums)
- [ ] Cubre los 4 métodos de cambio, incluyendo el reset de `SubscriptionStatus`/`SubscriptionExpiresAt`
- [ ] Usa `[Theory]`/`[InlineData]` para las variantes de CUIT y de enums

---

#### TEST-04 · `UserTests`

**Archivo:** `MateHR.Domain.Tests/Users/Entities/UserTests.cs`

Entidad `User`. Las invariantes del constructor: `SuperAdmin` **no** puede tener `TenantId`, cualquier
otro rol **debe** tenerlo, `Guid.Empty` siempre es inválido y el email se normaliza a minúsculas con
trim. Los métodos de ciclo de vida: `RecordLogin`, `ChangePassword`, `Deactivate`, `Activate` y
`CanAuthenticate`.

**Tests del constructor:**

- Vamos a generar un test llamado **`Constructor_ValidUserWithTenant_InitializesUser`**. Le pasamos
  `firstName = "Ana"`, `lastName = "Pérez"`, `email = "ana@acme.com"`, `passwordHash = "hash-123"`,
  `role = Recruiter` y `tenantId = Guid.NewGuid()`; procesamos `new User(...)` y esperamos que no
  lance, que `Id` no sea `Guid.Empty`, que `Role == Recruiter`, que `TenantId` sea el que pasamos,
  que `IsActive == true`, que `SecurityStamp` no sea `Guid.Empty` y que `CreatedAt` esté seteado.

- Vamos a generar un test llamado **`Constructor_SuperAdminWithTenant_ThrowsArgumentException`**.
  Le pasamos `role = SuperAdmin` y `tenantId = Guid.NewGuid()`; procesamos el constructor y esperamos
  `ArgumentException` con `ParamName == "tenantId"`.

- Vamos a generar un test llamado **`Constructor_RoleWithoutTenant_ThrowsArgumentException`**.
  Le pasamos `role = Admin` y `tenantId = null`; procesamos el constructor y esperamos
  `ArgumentException` con `ParamName == "tenantId"`.

- Vamos a generar un test llamado **`Constructor_TenantIdEmpty_ThrowsArgumentException`**. Le pasamos
  `role = Employee` y `tenantId = Guid.Empty`; procesamos el constructor y esperamos
  `ArgumentException` (el chequeo de `Guid.Empty` va antes que el de "rol sin tenant", así que
  pasamos un rol común para que entre por esa rama).

- Vamos a generar un test llamado **`Constructor_UndefinedRole_ThrowsArgumentOutOfRangeException`**.
  Le pasamos `role = (UserRole)999` y `tenantId = Guid.NewGuid()`; procesamos y esperamos
  `ArgumentOutOfRangeException`.

- Vamos a generar un test llamado **`Constructor_MixedCaseEmail_NormalizesToLowercase`**. Le pasamos
  `email = "  ANA.PEREZ@ACME.COM  "`; procesamos el constructor y esperamos que
  `Email == "ana.perez@acme.com"` (trim + `ToLowerInvariant`).

- Vamos a generar un test llamado **`Constructor_WhitespaceFirstName_ThrowsArgumentException`**.
  Le pasamos `firstName = "  "`; procesamos y esperamos `ArgumentException` con
  `ParamName == "firstName"`. Repetimos la historia con
  **`Constructor_WhitespaceLastName_...`** y **`Constructor_WhitespacePasswordHash_...`**
  (le pasamos `passwordHash = ""` y esperamos `ArgumentException` con `ParamName == "passwordHash"`).

**Tests de ciclo de vida:**

- Vamos a generar un test llamado **`RecordLogin_ValidMoment_UpdatesLastLoginAt`**. Le pasamos a un
  user recién creado un `moment = DateTimeOffset.UtcNow.AddMinutes(1)`; procesamos
  `user.RecordLogin(moment)` y esperamos que `LastLoginAt == moment` y `UpdatedAt == moment`.

- Vamos a generar un test llamado **`RecordLogin_MomentBeforeCreatedAt_ThrowsArgumentOutOfRangeException`**.
  Le pasamos `moment = user.CreatedAt.AddSeconds(-1)`; procesamos `RecordLogin(moment)` y esperamos
  `ArgumentOutOfRangeException` y que `LastLoginAt` siga en `null`.

- Vamos a generar un test llamado **`ChangePassword_NewHash_UpdatesHashAndRegeneratesSecurityStamp`**.
  Le pasamos `newPasswordHash = "hash-nuevo"` a un user con `SecurityStamp` original; procesamos
  `user.ChangePassword("hash-nuevo")` y esperamos que `PasswordHash == "hash-nuevo"`, que
  `SecurityStamp` haya cambiado (es el punto: invalida sesiones viejas) y que `UpdatedAt` esté
  seteado.

- Vamos a generar un test llamado **`ChangePassword_WhitespaceHash_ThrowsArgumentException`**.
  Le pasamos `newPasswordHash = "   "`; procesamos y esperamos `ArgumentException`, y que ni el hash
  ni el `SecurityStamp` hayan cambiado.

- Vamos a generar un test llamado **`Deactivate_ActiveUser_DeactivatesAndRegeneratesStamp`**. Le
  pasamos un user activo `moment = DateTimeOffset.UtcNow`; procesamos `user.Deactivate(moment)` y
  esperamos `IsActive == false`, un `SecurityStamp` distinto al original y `UpdatedAt == moment`.

- Vamos a generar un test llamado **`Deactivate_AlreadyInactive_ThrowsInvalidOperationException`**.
  Le pasamos un user que ya está inactivo (lo desactivamos primero en el setup); procesamos
  `Deactivate(moment)` y esperamos `InvalidOperationException`.

- Vamos a generar un test llamado **`Activate_InactiveUser_ActivatesAndRegeneratesStamp`**. Le
  pasamos un user inactivo; procesamos `Activate(moment)` y esperamos `IsActive == true`,
  `SecurityStamp` regenerado y `UpdatedAt == moment`.

- Vamos a generar un test llamado **`Activate_AlreadyActive_ThrowsInvalidOperationException`**. Le
  pasamos un user activo; procesamos `Activate(moment)` y esperamos `InvalidOperationException`.

- Vamos a generar un test llamado **`CanAuthenticate_ActiveUser_ReturnsTrue`**. Le pasamos un user
  recién creado; procesamos `user.CanAuthenticate()` y esperamos `true`.

- Vamos a generar un test llamado **`CanAuthenticate_InactiveUser_ReturnsFalse`**. Le pasamos un user
  que desactivamos antes; procesamos `CanAuthenticate()` y esperamos `false`.

**Aceptación**
- [ ] Cubre las invariantes SuperAdmin/sin tenant, rol/tenant obligatorio y `Guid.Empty`
- [ ] Cubre `RecordLogin`, `ChangePassword`, `Deactivate`, `Activate` y `CanAuthenticate`
- [ ] Cubre la normalización del email a minúsculas

---

#### TEST-05 · `RefreshTokenTests`

**Archivo:** `MateHR.Domain.Tests/Users/Entities/RefreshTokenTests.cs`

`RefreshToken` es el corazón de la rotación: valida en el constructor, `Revoke` es idempotente,
`ReplaceWith` marca el reemplazo y `IsActive` decide si el token sirve.

- Vamos a generar un test llamado **`Constructor_ValidData_CreatesToken`**. Le pasamos
  `userId = Guid.NewGuid()`, `tokenHash = "abc123"`, `createdAt = DateTimeOffset.UtcNow` y
  `expiresAt = createdAt.AddDays(14)`; procesamos `new RefreshToken(...)` y esperamos que no lance,
  que `Id` no sea `Guid.Empty`, que `UserId` y `TokenHash` se guarden tal cual, que `RevokedAt` y
  `ReplacedAt` arranquen en `null` y que `ExpiresAt`/`CreatedAt` coincidan con lo que pasamos.

- Vamos a generar un test llamado **`Constructor_EmptyUserId_ThrowsArgumentException`**. Le pasamos
  `userId = Guid.Empty`; procesamos el constructor y esperamos `ArgumentException` con
  `ParamName == "userId"`.

- Vamos a generar un test llamado **`Constructor_ExpiresAtBeforeCreatedAt_ThrowsArgumentOutOfRangeException`**.
  Le pasamos `createdAt = now` y `expiresAt = now.AddMinutes(-1)`; procesamos el constructor y
  esperamos `ArgumentOutOfRangeException` con `ParamName == "expiresAt"`.

- Vamos a generar un test llamado **`Constructor_ExpiresAtEqualsCreatedAt_ThrowsArgumentOutOfRangeException`**.
  Le pasamos `expiresAt == createdAt`; procesamos el constructor y esperamos
  `ArgumentOutOfRangeException` (la condición es `<=`, ojo con el igual: es el caso tramposo).

- Vamos a generar un test llamado **`Constructor_WhitespaceTokenHash_ThrowsArgumentException`**. Le
  pasamos `tokenHash = ""`; procesamos y esperamos `ArgumentException` con `ParamName == "tokenHash"`.

- Vamos a generar un test llamado **`Constructor_TokenHashTooLong_ThrowsArgumentException`**. Le
  pasamos un `tokenHash` de 65 caracteres (`new string('a', 65)`, el máximo es 64); procesamos y
  esperamos `ArgumentException`.

- Vamos a generar un test llamado **`Revoke_ValidMoment_SetsRevokedAt`**. Le pasamos a un token vivo
  `moment = DateTimeOffset.UtcNow`; procesamos `token.Revoke(moment)` y esperamos
  `RevokedAt == moment`.

- Vamos a generar un test llamado **`Revoke_Twice_KeepsOriginalRevokedAt`** — *el caso tramposo*.
  Le pasamos `moment1` en la primera llamada y `moment2 = moment1.AddMinutes(5)` en la segunda;
  procesamos `Revoke(moment1)` y después `Revoke(moment2)` y esperamos que `RevokedAt == moment1`
  (el segundo llamado tiene que salir por el `return` sin pisar la fecha).

- Vamos a generar un test llamado **`ReplaceWith_SetsRevokedAtAndReplacedAt`**. Le pasamos
  `moment = DateTimeOffset.UtcNow`; procesamos `token.ReplaceWith(moment)` y esperamos
  `RevokedAt == moment` y `ReplacedAt == moment`.

- Vamos a generar un test llamado **`IsActive_ActiveToken_ReturnsTrue`**. Le pasamos un token con
  `expiresAt = now.AddDays(1)` y un `moment = now`; procesamos `token.IsActive(moment)` y esperamos
  `true`.

- Vamos a generar un test llamado **`IsActive_RevokedToken_ReturnsFalse`**. Le pasamos un token que
  revocamos con `Revoke(now)` y un `moment = now.AddMinutes(1)` (antes de expirar); procesamos
  `IsActive(moment)` y esperamos `false` — el token sigue vigente en el tiempo pero ya fue
  revocado.

- Vamos a generar un test llamado **`IsActive_ExpiredToken_ReturnsFalse`**. Le pasamos un token con
  `expiresAt = now` y `moment = now.AddSeconds(1)`; procesamos `IsActive(moment)` y esperamos
  `false`.

**Aceptación**
- [ ] Cubre las 5 validaciones del constructor (userId, expiración, hash vacío, hash largo)
- [ ] Cubre la idempotencia de `Revoke` con fechas distintas
- [ ] Cubre `IsActive` en los 3 estados: vigente, revocado y expirado

---

#### TEST-06 · `ExceptionsTests`

**Archivo:** `MateHR.Domain.Tests/Exceptions/ExceptionsTests.cs`

Las tres excepciones de dominio son contrato: mensaje y propiedad expuesta. Son tests cortitos pero
nos protegen de cambios silenciosos.

- Vamos a generar un test llamado **`SlugAlreadyExistsException_Slug_ContainsSlugAndExposesProperty`**.
  Le pasamos `slug = "acme-corp"`; procesamos `new SlugAlreadyExistsException("acme-corp")` y
  esperamos que `.Message` contenga `"acme-corp"` y que `.Slug == "acme-corp"`.

- Vamos a generar un test llamado **`EmailAlreadyExistsException_Email_ContainsEmailAndExposesProperty`**.
  Le pasamos `email = "ana@acme.com"`; procesamos `new EmailAlreadyExistsException(...)` y esperamos
  que `.Message` contenga `"ana@acme.com"` y que `.Email == "ana@acme.com"`.

- Vamos a generar un test llamado **`InvalidCredentialsException_DefaultMessage_ReturnsDefaultText`**.
  Le pasamos nada (constructor sin argumentos); procesamos `new InvalidCredentialsException()` y
  esperamos que `.Message == "Las credenciales son invalidas."`.

- Vamos a generar un test llamado **`InvalidCredentialsException_CustomMessage_ReturnsCustomMessage`**.
  Le pasamos `message = "El usuario esta inactivo."`; procesamos y esperamos que `.Message` sea
  exactamente ese string (el overload custom no se pisa con el default).

**Aceptación**
- [ ] Mensajes y propiedades verificadas para las 3 excepciones

---

#### TEST-07 · `AddressTests` y `IndustryEnumTests` (rápidos)

**Archivos:** `MateHR.Domain.Tests/Tenants/ValueObjects/AddressTests.cs` y
`MateHR.Domain.Tests/Tenants/Enums/IndustryEnumTests.cs`

- Vamos a generar un test llamado **`NewInstance_DefaultsAreEmptyStrings`** en `AddressTests`. Le
  pasamos nada (creamos `new Address()`); procesamos la instancia nueva y esperamos que las 5
  propiedades (`Street`, `City`, `State`, `Country`, `PostalCode`) arranquen en `string.Empty`, no
  en `null`.

- Vamos a generar un test llamado **`Industry_Technology_IsZero`** en `IndustryEnumTests`. Le
  pasamos `Industry.Technology`; procesamos `(int)Industry.Technology` y esperamos `0` — es una
  decisión deliberada (Decisión 7) y si alguien renumera el enum rompe contratos de API y base.

- Vamos a generar un test llamado **`TenantStatus_Active_IsOne`**. Le pasamos
  `TenantStatus.Active`; procesamos `(int)` y esperamos `1` (que no arranque en 0, así el default
  no es un estado "mágico").

- Vamos a generar un test llamado **`UserRole_SuperAdmin_IsOne`**, **`RecruitmentMode_Internal_IsOne`**
  y **`SubscriptionType_Free_IsOne`**: misma historia, les pasamos cada valor inicial del enum,
  procesamos el cast a `int` y esperamos `1`.

- Vamos a generar un test llamado **`SubscriptionStatus_None_IsZero`**. Le pasamos
  `SubscriptionStatus.None`; procesamos y esperamos `0` — acá `None = 0` sí es intencional (es el
  "sin suscripción"), a diferencia de los otros enums.

**Aceptación**
- [ ] `Address` defaults en `string.Empty`
- [ ] Los enums fijan sus valores numéricos (los que son contrato de API)

---

## Bloque 5b — Application (use cases con Moq + AutoMapper real)

Consejo general para esta capa: usá **mocks de Moq** para las interfaces de repositorio y
servicios (`ITenantRepository`, `IUserRepository`, `IPasswordHasher`, `IJwtService`, etc.) y **el
mapper real de AutoMapper** (no lo mockees) armado con los profiles:

```csharp
var mapperConfiguration = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<TenantMapping>();
    cfg.AddProfile<UserMapping>();
});
var mapper = mapperConfiguration.CreateMapper();
```

Y los validators reales de FluentValidation (`new CreateTenantDtoValidator()`, etc.), así
probás las reglas de negocio de verdad, no un stub que devuelve "siempre válido".

#### TEST-08 · `MappingConfigurationTests`

**Archivo:** `MateHR.Application.Tests/Common/MappingConfigurationTests.cs`

Antes de cualquier test de use case, queremos un guardián que nos diga si los mapeos están bien
definidos.

- Vamos a generar un test llamado **`TenantMapping_AssertConfigurationIsValid_Passes`**. Le pasamos
  una `MapperConfiguration` con `cfg.AddProfile<TenantMapping>()`; procesamos
  `.AssertConfigurationIsValid()` y esperamos que no lance `AutoMapperConfigurationException`
  (si lanza, algún miembro de `Tenant`/`TenantResponse` quedó sin mapear).

- Vamos a generar un test llamado **`UserMapping_AssertConfigurationIsValid_Passes`**. Le pasamos
  una `MapperConfiguration` con `cfg.AddProfile<UserMapping>()`; procesamos
  `.AssertConfigurationIsValid()` y esperamos que no lance.

- Vamos a generar un test llamado **`Map_TenantToTenantResponse_MapsProperties`**. Le pasamos un
  `Tenant` construido con datos conocidos y los **dos** profiles juntos (por `PagedResult<T>`
  necesita `TenantMapping`); procesamos `mapper.Map<TenantResponse>(tenant)` y esperamos que
  `Id`, `Name`, `Slug`, `CUIT`, `OwnerEmail`, `Industry`, `Status`, `RecruitmentMode`,
  `SubscriptionType` y `CreatedAt` lleguen con los mismos valores.

- Vamos a generar un test llamado **`Map_AddressDtoToAddress_MapsProperties`** (y su espejo
  **`Map_AddressToAddressDto_...`**). Le pasamos un `AddressDto` con calle/ciudad/etc. llenos;
  procesamos `mapper.Map<Address>(dto)` y esperamos que las 5 propiedades se copien una a una.

- Vamos a generar un test llamado **`Map_PagedResultOfTenantToPagedResultOfTenantResponse_MapsItems`**.
  Le pasamos un `PagedResult<Tenant>` con 2 ítems, `pageNumber = 1`, `pageSize = 20`,
  `totalCount = 2`; procesamos `mapper.Map<PagedResult<TenantResponse>>(paged)` y esperamos que
  `Items` tenga 2 elementos mapeados y que `PageNumber`/`PageSize`/`TotalCount` se conserven
  (acá se confirma la trampa del constructor de `PagedResult<T>`).

**Aceptación**
- [ ] `AssertConfigurationIsValid` pasa para ambos profiles
- [ ] Mapeos puntuales verificados, incluido `PagedResult<T>`

---

#### TEST-09 · `CreateTenantTests`

**Archivo:** `MateHR.Application.Tests/Tenants/Commands/CreateTenantTests.cs`

`CreateTenant.ExecuteAsync` hace: validar DTO → trimmer el nombre → generar slug → chequear slug
duplicado → construir entidad → persistir → mapear respuesta. Mockeamos `ITenantRepository`, usamos
el mapper real y `new CreateTenantDtoValidator()`.

- Vamos a generar un test llamado **`ExecuteAsync_ValidDto_CreatesTenantWithGeneratedSlug`**. Le
  pasamos un `CreateTenantDto` válido: `Name = "Acme Corp"`, `CUIT = "20304050601"`,
  `OwnerEmail = "owner@acme.com"`, `Industry = Technology`, `Address = null`; le decimos al mock
  `ExistsBySlugAsync("acme-corp", null, _) → false` y `CreateTenantAsync(...) → la misma entidad`.
  Vamos a procesar `ExecuteAsync(dto)` y esperamos que devuelva un `TenantResponse` con los campos
  mapeados (`Name == "Acme Corp"`, `Slug == "acme-corp"`); además verificamos con
  `mock.Verify(...)` que `CreateTenantAsync` se llamó con una entidad que tenga
  `Slug == "acme-corp"`, `Status == Active`, `RecruitmentMode == Internal` y
  `SubscriptionType == Free`.

- Vamos a generar un test llamado **`ExecuteAsync_DuplicateSlug_ThrowsSlugAlreadyExistsException`**.
  Le pasamos el mismo DTO válido, pero le decimos al mock `ExistsBySlugAsync("acme-corp", null, _) → true`;
  procesamos `ExecuteAsync(dto)` y esperamos `SlugAlreadyExistsException` con `.Slug == "acme-corp"`,
  y verificamos que `CreateTenantAsync` **nunca** se llamó (`Times.Never`).

- Vamos a generar un test llamado **`ExecuteAsync_EmptyName_ThrowsValidationException`**. Le
  pasamos un DTO con `Name = ""` (el resto puede ser basura); procesamos `ExecuteAsync(dto)` y
  esperamos `FluentValidation.ValidationException` (la corta el validator, antes de tocar el repo:
  verificamos `Times.Never` sobre cualquier método del repositorio).

- Vamos a generar un test llamado **`ExecuteAsync_NullIndustry_ThrowsValidationException`**. Le
  pasamos un DTO con `Industry = null`; procesamos `ExecuteAsync(dto)` y esperamos
  `ValidationException` — recordá la decisión de negocio: `Industry` es `NotNull`, no tiene default.

- Vamos a generar un test llamado **`ExecuteAsync_InvalidCuit_ThrowsValidationException`**. Le
  pasamos un DTO con `CUIT = "20-30405060-1"` (con guiones); procesamos y esperamos
  `ValidationException` porque el validator pide `Length(11)` + `Matches("^[0-9]+$")`.

- Vamos a generar un test llamado **`ExecuteAsync_NameWithSpaces_TrimsNameBeforeCreating`**. Le
  pasamos `Name = "  Acme Corp  "`; procesamos `ExecuteAsync(dto)` y esperamos que la entidad que
  llega a `CreateTenantAsync` tenga `Name == "Acme Corp"` y `Slug == "acme-corp"` (el trim ocurre
  antes de generar el slug: si no lo trimmea, el slug tendría guiones de más).

**Aceptación**
- [ ] Cubre happy path con verificaciones de argumentos (`Verify`)
- [ ] Cubre slug duplicado, validación de DTO y nombre trimmeado

---

#### TEST-10 · `UpdateTenantTests`

**Archivo:** `MateHR.Application.Tests/Tenants/Commands/UpdateTenantTests.cs`

Mismo esquema que `CreateTenant` pero con un paso más: primero `GetTenantByIdAsync`, y el chequeo de
slug duplicado va **excluyendo el propio id**.

- Vamos a generar un test llamado **`ExecuteAsync_ValidDto_UpdatesTenantAndReturnsMappedResponse`**.
  Le pasamos `tenantId = un guid` y un `UpdateTenantDto` válido; le decimos al mock
  `GetTenantByIdAsync(tenantId) → un tenant existente`, `ExistsBySlugAsync("nuevo-slug", tenant.Id, _) → false`
  y `UpdateTenantAsync(...) → el mismo tenant`. Procesamos `ExecuteAsync(tenantId, dto)` y esperamos
  un `TenantResponse` mapeado con los valores nuevos, y verificamos que `UpdateTenantAsync` se
  llamó con un tenant cuyo `Slug == "nuevo-slug"`.

- Vamos a generar un test llamado **`ExecuteAsync_DuplicateSlug_ThrowsAndExcludesOwnTenant`**. Le
  pasamos un DTO con un nombre que genera un slug usado por **otro** tenant; le decimos al mock
  `ExistsBySlugAsync(slug, tenant.Id, _) → true`; procesamos y esperamos
  `SlugAlreadyExistsException`. La clave: verificamos con Moq que `ExistsBySlugAsync` se llamó con
  el `tenantId` propio como `excludeTenantId` (si excluye mal, editar un tenant sin cambiar de
  nombre devolvería un 409 falso — es el caso TEN-21 del backlog).

- Vamos a generar un test llamado **`ExecuteAsync_TenantNotFound_ThrowsKeyNotFoundException`**.
  Le pasamos `tenantId` y le decimos al mock `GetTenantByIdAsync(...) → throw new KeyNotFoundException(...)`;
  procesamos `ExecuteAsync(...)` y esperamos que la excepción se propague tal cual (así el
  middleware la convierte en 404), y verificamos que `ExistsBySlugAsync` nunca se llamó.

- Vamos a generar un test llamado **`ExecuteAsync_InvalidDto_ThrowsValidationException`**. Le
  pasamos un DTO con `Name = ""`; procesamos y esperamos `ValidationException` sin tocar el
  repositorio.

**Aceptación**
- [ ] Happy path mapeado + `Verify` de `UpdateTenantAsync`
- [ ] El `excludeTenantId` del `ExistsBySlugAsync` está verificado (TEN-21)
- [ ] Tenant inexistente y DTO inválido cubiertos

---

#### TEST-11 · `ChangeStatusTenantTests`, `ChangeRecruitmentModeTenantTests`, `ChangeSubscriptionTypeTenantTests`

**Archivos:** `MateHR.Application.Tests/Tenants/Commands/*.cs` (uno por cada comando)

Son use cases delgados: validan el DTO y delegan en el repositorio. Para **cada uno** de los tres
archivos vamos a escribir la misma terna de tests, cambiando solo el método del repo y el campo del
DTO:

- Vamos a generar un test llamado **`ExecuteAsync_ValidDto_UpdatesAndReturnsMappedResponse`**. Le
  pasamos `tenantId = un guid` y el DTO correspondiente con un valor válido
  (`ChangeTenantStatusDto { Status = Suspended }`, `ChangeRecruitmentModeDto { RecruitmentMode = External }`
  o `ChangeSubscriptionTypeDto { SubscriptionType = Yearly }`); le decimos al mock que
  `UpdateStatusTenantAsync` / `UpdateRecruitmentModeTenantAsync` / `UpdateSubscriptionTypeTenantAsync`
  devuelva el tenant. Procesamos `ExecuteAsync(tenantId, dto)` y esperamos un `TenantResponse`
  mapeado con el estado nuevo, y verificamos con `Verify` que el repo se llamó con ese `tenantId`
  y **exactamente** el valor del DTO (no un valor default).

- Vamos a generar un test llamado **`ExecuteAsync_UndefinedEnum_ThrowsValidationException`**. Le
  pasamos el DTO con el enum fuera de rango (`(TenantStatus)999`, `(RecruitmentMode)999` o
  `(SubscriptionType)999`); procesamos `ExecuteAsync(...)` y esperamos `ValidationException` (los
  validators usan `.IsInEnum()`), verificando que el repo no se tocó.

- Vamos a generar un test llamado **`ExecuteAsync_TenantNotFound_ThrowsKeyNotFoundException`**.
  Le pasamos un `tenantId` y le decimos al mock que el método del repo lance
  `KeyNotFoundException`; procesamos y esperamos que se propague (404 vía middleware).

**Aceptación**
- [ ] Los 3 archivos cubren happy path + `Verify` de argumentos
- [ ] Enum inválido → `ValidationException` sin llegar al repo

---

#### TEST-12 · `GetTenantsTests`

**Archivo:** `MateHR.Application.Tests/Tenants/Queries/GetTenantsTests.cs`

- Vamos a generar un test llamado **`ExecuteAsync_ValidRequest_ReturnsPagedResultWithMappedItems`**.
  Le pasamos un `GetTenantsRequest { PageNumber = 1, PageSize = 20, Status = null }`; le decimos al
  mock que `GetTenantsAsync(null, 1, 20, _)` devuelva un `PagedResult<Tenant>` con 2 tenants
  reales; procesamos `ExecuteAsync(request)` y esperamos un `PagedResult<TenantResponse>` con los
  ítems mapeados y el `TotalCount` conservado (acá confirmamos que el mapeo de `PagedResult<T>` de
  TEST-08 funciona en un caso real).

- Vamos a generar un test llamado **`ExecuteAsync_ForwardsStatusAndPagingToRepository`**. Le
  pasamos `Status = TenantStatus.Active`, `PageNumber = 3`, `PageSize = 50`; procesamos
  `ExecuteAsync(request)` y verificamos con `Verify` que el repo recibió exactamente esos tres
  valores (`GetTenantsAsync(TenantStatus.Active, 3, 50, _)`), no una copia alterada.

- Vamos a generar un test llamado **`ExecuteAsync_PageSizeAbove100_ThrowsValidationException`**.
  Le pasamos `PageSize = 101`; procesamos y esperamos `ValidationException` (validator:
  `InclusiveBetween(1, 100)`), y que el repo no se llamó.

- Vamos a generar un test llamado **`ExecuteAsync_PageNumberZero_ThrowsValidationException`**. Le
  pasamos `PageNumber = 0`; procesamos y esperamos `ValidationException` (`GreaterThanOrEqualTo(1)`).

**Aceptación**
- [ ] Happy path con mapper real y `Verify` de parámetros
- [ ] Los 2 casos de request inválido cortan antes del repo

---

#### TEST-13 · `GetTenantByIdTests` y `GetTenantBySlugTests`

**Archivos:** `MateHR.Application.Tests/Tenants/Queries/GetTenantByIdTests.cs` y
`GetTenantBySlugTests.cs`

- Vamos a generar un test llamado **`ExecuteAsync_ExistingId_ReturnsMappedResponse`** en
  `GetTenantByIdTests`. Le pasamos `tenantId = un guid` que el mock devuelve como `Tenant`
  existente; procesamos `ExecuteAsync(tenantId)` y esperamos un `TenantResponse` con los mismos
  campos.

- Vamos a generar un test llamado **`ExecuteAsync_UnknownId_ThrowsKeyNotFoundException`**. Le
  pasamos un `tenantId` y le decimos al mock `GetTenantByIdAsync(...) → throw new KeyNotFoundException()`;
  procesamos y esperamos que se propague (el middleware la mapea a 404).

- Vamos a generar un test llamado **`ExecuteAsync_UncleanSlug_NormalizesSlugBeforeQuery`** en
  `GetTenantBySlugTests`. Le pasamos `slug = "Mi Empresa"` (con mayúscula y espacio); procesamos
  `ExecuteAsync("Mi Empresa")` y verificamos que el repo fue llamado con
  `GetTenantBySlugAsync("mi-empresa", _)` — es la normalización de `SlugGenerator` antes de buscar.

- Vamos a generar un test llamado **`ExecuteAsync_UnknownSlug_ThrowsKeyNotFoundException`**. Le
  pasamos `slug = "no-existe"` y le decimos al mock que lance `KeyNotFoundException`;
  procesamos y esperamos que se propague.

**Aceptación**
- [ ] Ambos queries: happy path mapeado y `KeyNotFoundException` propagada
- [ ] El slug se normaliza antes de pegarle al repo

---

#### TEST-14 · Validators (por fuera, opcional pero muy barato)

**Archivos:** `MateHR.Application.Tests/Tenants/Validators/CreateTenantDtoValidatorTests.cs` y
`GetTenantsRequestValidatorTests.cs`

Si queremos validar las reglas sueltas sin pasar por el use case, instanciamos el validator real y
probamos `Validate(dto)`:

- Vamos a generar un test llamado **`Validate_EmptyName_IsInvalid`**. Le pasamos un
  `CreateTenantDto` con `Name = ""`; procesamos `validator.Validate(dto)` y esperamos
  `IsValid == false` y un error con `PropertyName == "Name"`.

- Vamos a generar un test llamado **`Validate_NameExceedsMaxLength_IsInvalid`**. Le pasamos
  `Name = new string('a', 201)`; procesamos y esperamos `IsValid == false`.

- Vamos a generar un test llamado **`Validate_CuitWithLetters_IsInvalid`**. Le pasamos
  `CUIT = "20A30405061"`; procesamos y esperamos `IsValid == false` (falla el `Matches`).

- Vamos a generar un test llamado **`Validate_CuitWrongLength_IsInvalid`**. Le pasamos
  `CUIT = "123"`; procesamos y esperamos `IsValid == false` (falla el `Length(11)`).

- Vamos a generar un test llamado **`Validate_InvalidOwnerEmail_IsInvalid`**. Le pasamos
  `OwnerEmail = "no-es-un-mail"`; procesamos y esperamos `IsValid == false` (`EmailAddress()`).

- Vamos a generar un test llamado **`Validate_NullIndustry_IsInvalid`**. Le pasamos
  `Industry = null`; procesamos y esperamos `IsValid == false`.

- Vamos a generar un test llamado **`Validate_ValidDto_IsValid`**. Le pasamos un DTO completo y
  válido; procesamos y esperamos `IsValid == true` y `Errors` vacío.

- Vamos a generar un test llamado **`Validate_PageSizeZero_IsInvalid`** en
  `GetTenantsRequestValidatorTests`. Le pasamos `PageSize = 0`; procesamos y esperamos
  `IsValid == false`. Repetimos la historia con **`Validate_PageSizeAbove100_IsInvalid`**
  (`PageSize = 101`) y **`Validate_PageNumberZero_IsInvalid`** (`PageNumber = 0`).

- Vamos a generar un test llamado **`Validate_DefaultRequest_IsValid`**. Le pasamos un
  `GetTenantsRequest` recién creado (los defaults: `PageNumber = 1`, `PageSize = 20`); procesamos y
  esperamos `IsValid == true`.

**Aceptación**
- [ ] Todas las reglas de `CreateTenantDtoValidator` cubiertas, más el happy path
- [ ] Paging inválido y request por defecto de `GetTenantsRequest`

---

#### TEST-15 · `LoginUserTests` — el más importante de auth

**Archivo:** `MateHR.Application.Tests/Users/Commands/LoginUserTests.cs`

`LoginUser` tiene muchos colaboradores, así que el mock se arma así:

```csharp
var userRepository = new Mock<IUserRepository>();
var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
var passwordHasher = new Mock<IPasswordHasher>();
var refreshTokenService = new Mock<IRefreshTokenService>();
var jwtService = new Mock<IJwtService>();
// mapper real + new LoginRequestValidator()
```

- Vamos a generar un test llamado **`ExecuteAsync_ValidCredentials_ReturnsTokensAndMappedUser`**.
  Le pasamos `LoginRequest { Email = "ana@acme.com", Password = "secreta123" }`; le decimos a los
  mocks: `GetByEmailAsync("ana@acme.com") → user activo con PasswordHash = "hash"`,
  `passwordHasher.Verify("secreta123", "hash") → true`,
  `jwtService.GenerateAccessToken(...) → new IssuedToken("access-token", ...)`,
  `refreshTokenService.Generate() → new IssuedToken("refresh-token", ...)`,
  `refreshTokenService.ComputeHash("refresh-token") → "hash-del-refresh"`,
  `refreshTokenRepository.CreateRefreshTokenAsync(...) → el entity`. Procesamos
  `ExecuteAsync(request)` y esperamos una `LoginResponse` con `AccessToken == "access-token"`,
  `RefreshToken == "refresh-token"` y el `User` mapeado; además verificamos que se llamaron
  `InvalidateExpiredUserRefreshTokensAsync(user.Id, ...)`, `CreateRefreshTokenAsync(...)` con un
  token cuyo `TokenHash == "hash-del-refresh"` y `UserId == user.Id`, y `UpdateUserAsync(user)`.

- Vamos a generar un test llamado **`ExecuteAsync_UnknownEmail_ThrowsInvalidCredentialsException`**.
  Le pasamos un email que no existe; le decimos al mock `GetByEmailAsync(...) → null`; procesamos
  `ExecuteAsync(request)` y esperamos `InvalidCredentialsException` con el mensaje por defecto
  `"Las credenciales son invalidas."`, y que ni `GenerateAccessToken` ni `CreateRefreshTokenAsync`
  se hayan llamado.

- Vamos a generar un test llamado **`ExecuteAsync_WrongPassword_ThrowsSameInvalidCredentialsException`**.
  Le pasamos un email válido y `Password = "otra-clave"`; le decimos al mock que el user existe pero
  `passwordHasher.Verify(...) → false`; procesamos y esperamos `InvalidCredentialsException`. La
  historia clave: **el mensaje tiene que ser idéntico al del test anterior** (no diferencia usuario
  inexistente de contraseña mala — es a propósito, evita la enumeración de usuarios).

- Vamos a generar un test llamado **`ExecuteAsync_InactiveUser_ThrowsInvalidCredentialsExceptionWithInactiveMessage`**.
  Le pasamos un user con `IsActive = false` y contraseña correcta; procesamos y esperamos
  `InvalidCredentialsException` con mensaje `"El usuario esta inactivo."`, y que
  `RevokeAllUserRefreshTokensAsync`/`CreateRefreshTokenAsync` no se llamen.

- Vamos a generar un test llamado **`ExecuteAsync_ShortPassword_ThrowsValidationException`**. Le
  pasamos `Password = "123"`; procesamos y esperamos `ValidationException` (validator:
  `MinimumLength(8)`), sin tocar el repo.

- Vamos a generar un test llamado **`ExecuteAsync_MixedCaseEmail_NormalizesBeforeQuery`**. Le
  pasamos `Email = "  ANA@ACME.COM  "`; procesamos `ExecuteAsync(request)` y verificamos con Moq
  que `GetByEmailAsync` fue llamado con `"ana@acme.com"` (`Trim().ToLowerInvariant()`).

**Aceptación**
- [ ] Happy path con todas las verificaciones de interacción (`Verify`)
- [ ] Usuario inexistente y contraseña mala → misma excepción con el mismo mensaje
- [ ] Usuario inactivo, request inválido y normalización del email cubiertos

---

#### TEST-16 · `RefreshAccessTokenTests` — el de seguridad más delicado

**Archivo:** `MateHR.Application.Tests/Users/Commands/RefreshAccessTokenTests.cs`

Es el corazón de la rotación de tokens y la detección de reuso. Si llega un token ya usado o
revocado, se revocan **todos** los refresh del usuario (`HandleReuseAsync`).

- Vamos a generar un test llamado **`ExecuteAsync_NullToken_ThrowsInvalidCredentialsException`**.
  Le pasamos `refreshToken = null`; procesamos `ExecuteAsync(null)` y esperamos
  `InvalidCredentialsException` con mensaje `"El refresh token no es valido."`, sin tocar ningún
  mock. Repetimos la historia con **`ExecuteAsync_WhitespaceToken_...`** (le pasamos `"   "`).

- Vamos a generar un test llamado **`ExecuteAsync_UnknownToken_ThrowsAndHashesBeforeLookup`**. Le
  pasamos `refreshToken = "token-inexistente"`; le decimos a los mocks
  `refreshTokenService.ComputeHash("token-inexistente") → "sha-del-token"` y
  `GetByTokenHashAsync("sha-del-token") → null`. Procesamos y esperamos
  `InvalidCredentialsException`; verificamos que el hash se calculó **antes** de buscar en el repo
  (`Verify` de `ComputeHash` y de `GetByTokenHashAsync("sha-del-token")`).

- Vamos a generar un test llamado **`ExecuteAsync_ActiveToken_RotatesTokenAndInvalidatesStamp`** —
  el happy path. Le pasamos un token cuyo hash da un `storedToken` con `RevokedAt = null` y
  `ExpiresAt > now`, y un user `IsActive = true`; los mocks devuelven el access token y el refresh
  nuevo. Procesamos y esperamos una `LoginResponse` con los dos tokens nuevos y el `User` mapeado;
  verificamos que `UpdateRefreshTokenAsync(storedToken)` se llamó **después** de que el token viejo
  tenga `RevokedAt` seteado (usá `It.Is<RefreshToken>(t => t.RevokedAt != null)`), que
  `CreateRefreshTokenAsync(...)` recibió un entity con `UserId == user.Id` y el hash del refresh
  nuevo, y que `stampValidator.Invalidate(user.Id)` se llamó.

- Vamos a generar un test llamado **`ExecuteAsync_ReusedToken_RevokesAllUserTokensAndThrows`** —
  *el caso que previene el robo de sesión*. Le pasamos un token que la base devuelve con
  `RevokedAt` ya seteado (alguien lo reusó); procesamos `ExecuteAsync(token)` y esperamos
  `InvalidCredentialsException`, y sobre todo verificamos que se llamó
  `RevokeAllUserRefreshTokensAsync(storedToken.UserId, ...)`: si este `Verify` no se cumple, el
  test no está probando nada. No lo pases por alto.

- Vamos a generar un test llamado **`ExecuteAsync_ExpiredToken_RevokesAllUserTokensAndThrows`**.
  Le pasamos un token con `ExpiresAt < now` y `RevokedAt = null`; procesamos y esperamos
  `InvalidCredentialsException` + `RevokeAllUserRefreshTokensAsync` (un token expirado también
  dispara el protocolo de reuso).

- Vamos a generar un test llamado **`ExecuteAsync_InactiveUser_RevokesAllTokensAndThrows`**. Le
  pasamos un token vigente pero con `user.CanAuthenticate() == false`; procesamos y esperamos
  `InvalidCredentialsException` con mensaje `"El usuario esta inactivo."` y verificamos
  `RevokeAllUserRefreshTokensAsync(user.Id, ...)`.

**Aceptación**
- [ ] Rotación feliz: token viejo revocado, token nuevo creado, stamp invalidado
- [ ] Reuso (revocado y expirado) → revoca todos los tokens del usuario y falla
- [ ] Token huérfano/nulo e usuario inactivo cubiertos, con el hash calculado antes de buscar

---

#### TEST-17 · `CreateUserTests`

**Archivo:** `MateHR.Application.Tests/Users/Commands/CreateUserTests.cs`

`CreateUser.ExecuteAsync`: valida DTO → chequea email duplicado → valida que el tenant exista (si
viene `TenantId`) → construye el `User` con el password hasheado → persiste → invalida la stamp.

- Vamos a generar un test llamado **`ExecuteAsync_ValidRequest_CreatesUserWithHashedPassword`**.
  Le pasamos un `CreateUserRequest` con rol `Recruiter`, `TenantId = Guid.NewGuid()`,
  `Email = "  NUEVA@ACME.COM  "` y `Password = "secreta123"`; le decimos a los mocks:
  `ExistsByEmailAsync("nueva@acme.com") → false`, `GetTenantByIdAsync(tenantId) → un tenant`,
  `passwordHasher.Hash("secreta123") → "hash-abc"`,
  `CreateUserAsync(...) → el mismo user`. Procesamos `ExecuteAsync(request)` y esperamos un
  `UserResponse` mapeado; verificamos que el `User` que llegó a `CreateUserAsync` tiene
  `Email == "nueva@acme.com"` (normalizado), `PasswordHash == "hash-abc"` (el hash del hasher, no
  la password en claro) y `TenantId` seteado.

- Vamos a generar un test llamado **`ExecuteAsync_DuplicateEmail_ThrowsEmailAlreadyExistsException`**.
  Le pasamos un email que ya existe (`ExistsByEmailAsync(...) → true`); procesamos y esperamos
  `EmailAlreadyExistsException` con `.Email == "nueva@acme.com"` y que `CreateUserAsync` no se
  llamó.

- Vamos a generar un test llamado **`ExecuteAsync_UnknownTenant_ThrowsKeyNotFoundException`**. Le
  pasamos un `TenantId` y le decimos al mock `GetTenantByIdAsync(...) → throw new KeyNotFoundException()`;
  procesamos y esperamos que se propague (404 vía middleware) y que no se cree el user.

- Vamos a generar un test llamado **`ExecuteAsync_SuperAdminWithTenant_ThrowsValidationException`**.
  Le pasamos `Role = SuperAdmin` y `TenantId = Guid.NewGuid()`; procesamos y esperamos
  `ValidationException` (lo valida `CreateUserRequestValidator` con un mensaje útil de API, no la
  excepción de dominio).

- Vamos a generar un test llamado **`ExecuteAsync_ShortPassword_ThrowsValidationException`**. Le
  pasamos `Password = "123"`; procesamos y esperamos `ValidationException` (`MinimumLength(8)`).

- Vamos a generar un test llamado **`ExecuteAsync_UserCreated_InvalidatesSecurityStamp`**. Usando
  el mismo setup del happy path, procesamos `ExecuteAsync(request)` y verificamos con Moq que
  `stampValidator.Invalidate(created.Id)` se llamó con el id del user recién creado (importa para
  que una stamp cacheada vieja no dé acceso).

**Aceptación**
- [ ] Happy path con email normalizado y hash del hasher
- [ ] Email duplicado, tenant inexistente y reglas SuperAdmin/tenant
- [ ] `Invalidate` de la stamp verificado

---

#### TEST-18 · `LogoutUserTests`

**Archivo:** `MateHR.Application.Tests/Users/Commands/LogoutUserTests.cs`

`LogoutUser` es tolerante a fallos: si el token es vacío o no existe, no hace nada y no lanza.

- Vamos a generar un test llamado **`ExecuteAsync_NullToken_DoesNotTouchRepository`**. Le pasamos
  `refreshToken = null`; procesamos `ExecuteAsync(null)` y esperamos que la tarea complete sin
  excepción, y verificamos con `Verify(..., Times.Never)` que `GetByTokenHashAsync` y
  `UpdateRefreshTokenAsync` nunca se llamaron. Repetimos la historia con
  **`ExecuteAsync_WhitespaceToken_...`** (le pasamos `"   "`).

- Vamos a generar un test llamado **`ExecuteAsync_UnknownToken_ReturnsWithoutPersisting`**. Le
  pasamos un token cualquiera; le decimos a los mocks `ComputeHash(token) → "sha"` y
  `GetByTokenHashAsync("sha") → null`; procesamos y esperamos que no lance nada, y verificamos
  `UpdateRefreshTokenAsync(..., Times.Never)`.

- Vamos a generar un test llamado **`ExecuteAsync_ExistingToken_RevokesAndPersists`**. Le pasamos
  un token; le decimos a los mocks `ComputeHash(token) → "sha"` y
  `GetByTokenHashAsync("sha") → un token vivo`; procesamos y esperamos que `storedToken.RevokedAt`
  quede seteado, y verificamos que `UpdateRefreshTokenAsync(token, ...)` se llamó una vez.

**Aceptación**
- [ ] Tolerancia a token nulo/vacío/desconocido (`Times.Never`)
- [ ] Token encontrado → revocado y persistido

---

#### TEST-19 · `GetCurrentUserTests` y `UserProvisioningServiceTests`

**Archivos:** `MateHR.Application.Tests/Users/Queries/GetCurrentUserTests.cs` y
`MateHR.Application.Tests/Users/Services/UserProvisioningServiceTests.cs`

- Vamos a generar un test llamado **`ExecuteAsync_ExistingUser_ReturnsMappedResponse`** en
  `GetCurrentUserTests`. Le pasamos `userId = un guid` que el mock devuelve como `User` existente;
  procesamos `ExecuteAsync(userId)` y esperamos un `UserResponse` con `Id`, `Email`, `Role` y
  `TenantId` iguales a los del user.

- Vamos a generar un test llamado **`ExecuteAsync_UnknownUser_ThrowsKeyNotFoundException`**. Le
  pasamos un `userId` y le decimos al mock que lance `KeyNotFoundException`; procesamos y esperamos
  que se propague (404 vía middleware).

- Vamos a generar un test llamado **`CreateAsync_ShortPassword_ThrowsArgumentException`** en
  `UserProvisioningServiceTests`. Le pasamos `plainTextPassword = "1234567"`; procesamos
  `CreateAsync(...)` y esperamos `ArgumentException` con `ParamName == "plainTextPassword"`,
  sin tocar el repo.

- Vamos a generar un test llamado **`CreateAsync_DuplicateEmail_ThrowsEmailAlreadyExistsException`**.
  Le pasamos `email = "  ANA@ACME.COM  "` y le decimos al mock `ExistsByEmailAsync("ana@acme.com") → true`;
  procesamos y esperamos `EmailAlreadyExistsException` con `.Email == "ana@acme.com"` (email ya
  normalizado).

- Vamos a generar un test llamado **`CreateAsync_ValidData_CreatesAndPersistsUser`**. Le pasamos
  `firstName = "Ana"`, `lastName = "Pérez"`, `email = "ANA@ACME.com"`, `plainTextPassword = "secreta123"`,
  `role = Recruiter`, `tenantId = Guid.NewGuid()`; le decimos a los mocks
  `ExistsByEmailAsync(...) → false`, `passwordHasher.Hash(...) → "hash-1"`,
  `CreateUserAsync(...) → el user`. Procesamos y esperamos que devuelva el `User` persistido con
  `Email == "ana@acme.com"` y `PasswordHash == "hash-1"`.

**Aceptación**
- [ ] `GetCurrentUser`: happy path y `KeyNotFoundException`
- [ ] `UserProvisioningService`: password corta, email duplicado y creación feliz

---

#### TEST-20 · Validators de Users

**Archivos:** `MateHR.Application.Tests/Users/Validators/CreateUserRequestValidatorTests.cs` y
`LoginRequestValidatorTests.cs`

- Vamos a generar un test llamado **`Validate_SuperAdminWithTenant_IsInvalid`**. Le pasamos un
  `CreateUserRequest` con `Role = SuperAdmin` y `TenantId = Guid.NewGuid()`; procesamos
  `validator.Validate(request)` y esperamos `IsValid == false` con un error cuyo mensaje diga que
  un SuperAdmin no pertenece a un tenant.

- Vamos a generar un test llamado **`Validate_RoleWithoutTenant_IsInvalid`**. Le pasamos
  `Role = Recruiter` y `TenantId = null`; procesamos y esperamos `IsValid == false`.

- Vamos a generar un test llamado **`Validate_TenantIdEmpty_IsInvalid`**. Le pasamos
  `Role = Employee` y `TenantId = Guid.Empty`; procesamos y esperamos `IsValid == false` con el
  mensaje "El tenant no puede ser un Guid vacio."

- Vamos a generar un test llamado **`Validate_ShortPassword_IsInvalid`**. Le pasamos
  `Password = "123"`; procesamos y esperamos `IsValid == false`. Repetimos con
  **`Validate_InvalidEmail_IsInvalid`** (`Email = "no-es-un-mail"`).

- Vamos a generar un test llamado **`Validate_ValidRequest_IsValid`**. Le pasamos un request completo
  y válido (rol común con `TenantId` válido); procesamos y esperamos `IsValid == true`.

- Vamos a generar un test llamado **`Validate_InvalidEmail_IsInvalid`** en
  `LoginRequestValidatorTests`. Le pasamos `Email = "no-es-un-mail"`; procesamos y esperamos
  `IsValid == false`.

- Vamos a generar un test llamado **`Validate_ShortPassword_IsInvalid`** en
  `LoginRequestValidatorTests`. Le pasamos `Password = "1234567"`; procesamos y esperamos
  `IsValid == false`.

- Vamos a generar un test llamado **`Validate_ValidRequest_IsValid`** en
  `LoginRequestValidatorTests`. Le pasamos `Email = "ana@acme.com"` y `Password = "secreta123"`;
  procesamos y esperamos `IsValid == true`.

**Aceptación**
- [ ] Invariantes SuperAdmin/tenant y password/email de `CreateUserRequestValidator`
- [ ] Reglas de `LoginRequestValidator` con su happy path

---

## Bloque 5c — Infrastructure (repos con EF InMemory, servicios con contexto real)

Para los repositorios necesitás un `ApplicationDbContext` en memoria. Helpers típicos en cada
archivo de test (o una clase `InMemoryDbContextFactory` compartida):

```csharp
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase($"test-db-{Guid.NewGuid()}")   // nombre único por test, evita contaminar
    .Options;
var context = new ApplicationDbContext(options);
var repository = new TenantRepository(context);
```

**Trampas IMPORTANTES de EF InMemory (léelas antes de escribir):**
1. **No aplica índices únicos.** `TenantConfiguration` define el índice único sobre `Slug`, pero
   InMemory lo ignora: dos tenants con el mismo slug se pueden insertar sin error. No escribas un
   test que espere que la base rechace duplicados (el chequeo de duplicados lo hace el use case,
   ya testearlo en Application).
2. El override de `SaveChangesAsync` de `ApplicationDbContext` que estampa `UpdatedAt` **sí
   funciona** en InMemory: aprovechalo para testear que al modificar un tenant/user se setea
   `UpdatedAt`.
3. `Address` es owned type (con `OwnsOne`): al reasignar, cambiá la instancia completa o usá el
   change tracker sin romper el estado.

#### TEST-21 · `TenantRepositoryTests`

**Archivo:** `MateHR.Infrastructure.Tests/Persistance/Repositories/TenantRepositoryTests.cs`

Setup de cada test: context InMemory con nombre único + `new TenantRepository(context)`, y seed de
tenants con `context.Tenants.Add(...)` + `SaveChanges()`.

- Vamos a generar un test llamado **`GetTenantByIdAsync_ExistingTenant_ReturnsTenant`**. Le
  insertamos un tenant en la base y le pasamos su `Id`; procesamos `GetTenantByIdAsync(id)` y
  esperamos el mismo tenant con todos sus campos.

- Vamos a generar un test llamado **`GetTenantByIdAsync_UnknownId_ThrowsKeyNotFoundException`**.
  Le pasamos un `Guid.NewGuid()` que no existe; procesamos y esperamos `KeyNotFoundException` con
  un mensaje que incluya el id.

- Vamos a generar un test llamado **`GetTenantBySlugAsync_ExistingSlug_ReturnsTenant`**. Le
  insertamos un tenant con `Slug = "acme"` y le pasamos `"acme"`; procesamos y esperamos el tenant.

- Vamos a generar un test llamado **`GetTenantBySlugAsync_UnknownSlug_ThrowsKeyNotFoundException`**.
  Le pasamos `"no-existe"`; procesamos y esperamos `KeyNotFoundException`.

- Vamos a generar un test llamado **`GetTenantsAsync_Page2Size2_ReturnsPageAndTotalCount`**. Le
  insertamos 5 tenants y le pasamos `status = null, pageNumber = 2, pageSize = 2`; procesamos
  `GetTenantsAsync(...)` y esperamos que `Items` tenga 2 ítems (los 3ros por orden de nombre),
  `TotalCount == 5`, `PageNumber == 2` y `TotalPages == 3`.

- Vamos a generar un test llamado **`GetTenantsAsync_WithStatusFilter_ReturnsOnlyMatching`**. Le
  insertamos 3 tenants `Active` y 2 `Suspended`, y le pasamos `status = TenantStatus.Active`;
  procesamos y esperamos que `Items` tenga solo los 3 `Active` y que `TotalCount == 3`.

- Vamos a generar un test llamado **`GetTenantsAsync_ReturnsOrderedByName`**. Le insertamos tenants
  con nombres desordenados (`"Zeta"`, `"Alpha"`, `"Mid"`); procesamos sin filtro y esperamos que
  `Items` venga ordenado `Alpha, Mid, Zeta` (`OrderBy(t => t.Name)`).

- Vamos a generar un test llamado **`ExistsBySlugAsync_ExistingSlug_ReturnsTrue`**. Le insertamos un
  tenant con `Slug = "acme"` y le pasamos `("acme", null)`; procesamos y esperamos `true`.

- Vamos a generar un test llamado **`ExistsBySlugAsync_UnknownSlug_ReturnsFalse`**. Le pasamos
  `("no-existe", null)`; procesamos y esperamos `false`.

- Vamos a generar un test llamado **`ExistsBySlugAsync_ExcludingOwnTenant_ReturnsFalse`**. Le
  insertamos un tenant con `Slug = "acme"` y le pasamos `("acme", tenant.Id)`; procesamos y
  esperamos `false` — con el mismo slug pero excluyendo al propio tenant no hay conflicto (es la
  lógica del TEN-21).

- Vamos a generar un test llamado **`CreateTenantAsync_PersistsTenant`**. Le construimos un tenant
  válido; procesamos `CreateTenantAsync(tenant)` y esperamos que devuelva el tenant, y además
  re-consultamos por `GetTenantByIdAsync(tenant.Id)` y esperamos que traiga los mismos
  `Name`/`Slug`/`CUIT`/`OwnerEmail`.

- Vamos a generar un test llamado **`UpdateStatusTenantAsync_ChangesStatusAndSetsUpdatedAt`**. Le
  insertamos un tenant, **lo traemos primero con `GetTenantByIdAsync`** (para que el change tracker
  lo trackee — si lo construís a mano, queda `Unchanged` y no se persiste el cambio), y le pasamos
  `TenantStatus.Suspended`; procesamos `UpdateStatusTenantAsync(id, Suspended)` y esperamos que el
  tenant devuelto tenga `Status == Suspended` y que `UpdatedAt` esté seteado (el override de
  `SaveChangesAsync` funciona en InMemory).

- Vamos a generar un test llamado **`UpdateRecruitmentModeTenantAsync_ChangesMode`**. Mismo setup;
  le pasamos `RecruitmentMode.External` y esperamos `RecruitmentMode == External` al re-consultar.

- Vamos a generar un test llamado **`UpdateSubscriptionTypeTenantAsync_ChangesType`**. Mismo setup;
  le pasamos `SubscriptionType.Yearly` y esperamos `SubscriptionType == Yearly` (y
  `SubscriptionStatus == None`) al re-consultar.

**Aceptación**
- [ ] Búsquedas por id/slug con éxito y con `KeyNotFoundException`
- [ ] Paginación, filtro por status y orden por nombre
- [ ] `ExistsBySlugAsync` con y sin `excludeTenantId`
- [ ] Los 3 `Update*` persisten el cambio y estampan `UpdatedAt`

---

#### TEST-22 · `UserRepositoryTests`

**Archivo:** `MateHR.Infrastructure.Tests/Persistance/Repositories/UserRepositoryTests.cs`

- Vamos a generar un test llamado **`GetByIdAsync_ExistingUser_ReturnsUser`**. Le insertamos un user
  y le pasamos su `Id`; procesamos y esperamos el mismo user.

- Vamos a generar un test llamado **`GetByIdAsync_UnknownId_ThrowsKeyNotFoundException`**. Le
  pasamos un `Guid.NewGuid()`; procesamos y esperamos `KeyNotFoundException`.

- Vamos a generar un test llamado **`GetByEmailAsync_ExistingEmail_ReturnsUser`**. Le insertamos un
  user con `Email = "ana@acme.com"` y le pasamos `"ana@acme.com"`; procesamos y esperamos el user.

- Vamos a generar un test llamado **`GetByEmailAsync_UnknownEmail_ReturnsNull`**. Le pasamos
  `"no@existe.com"`; procesamos y esperamos `null`.

- Vamos a generar un test llamado **`GetByEmailAsync_MixedCaseEmail_FindsUser`**. Le insertamos el
  user con email ya normalizado y le pasamos `"ANA@ACME.COM "` (mayúsculas y espacio); procesamos y
  esperamos el user — el repo aplica `email.Trim().ToLowerInvariant()` antes de comparar.

- Vamos a generar un test llamado **`ExistsByEmailAsync_ExistingEmail_ReturnsTrue`** y su hermano
  **`ExistsByEmailAsync_UnknownEmail_ReturnsFalse`**. Le pasamos el email (con mayúsculas en el caso
  `true`); procesamos y esperamos `true`/`false`.

- Vamos a generar un test llamado **`CreateUserAsync_PersistsUser`**. Le construimos un user válido;
  procesamos `CreateUserAsync(user)` y esperamos que lo devuelva, y re-consultamos por id para
  comparar `Email`, `Role` y `TenantId`.

- Vamos a generar un test llamado **`UpdateUserAsync_SetsUpdatedAt`**. Le traemos un user con
  `GetByIdAsync`, le cambiamos algo (por ejemplo `RecordLogin(now)`); procesamos
  `UpdateUserAsync(user)` y esperamos que al re-consultar `UpdatedAt` venga con el nuevo valor (el
  override de `SaveChangesAsync`).

**Aceptación**
- [ ] Búsquedas por id/email con éxito, `null` y `KeyNotFoundException`
- [ ] Normalización del email en la búsqueda
- [ ] `Create` persiste y `Update` estampa `UpdatedAt`

---

#### TEST-23 · `RefreshTokenRepositoryTests`

**Archivo:** `MateHR.Infrastructure.Tests/Persistance/Repositories/RefreshTokenRepositoryTests.cs`

- Vamos a generar un test llamado **`GetByTokenHashAsync_ExistingHash_ReturnsToken`**. Le
  insertamos un token con `TokenHash = "sha-1"` y le pasamos `"sha-1"`; procesamos y esperamos el
  token.

- Vamos a generar un test llamado **`GetByTokenHashAsync_UnknownHash_ReturnsNull`**. Le pasamos
  `"sha-inexistente"`; procesamos y esperamos `null`.

- Vamos a generar un test llamado **`CreateRefreshTokenAsync_PersistsToken`**. Le construimos un
  token válido; procesamos y esperamos que lo devuelva, y re-consultamos por hash para comparar
  `UserId` y `ExpiresAt`.

- Vamos a generar un test llamado **`UpdateRefreshTokenAsync_PersistsRevocation`**. Le traemos un
  token insertado, lo revocamos con `Revoke(now)`; procesamos `UpdateRefreshTokenAsync(token)` y
  esperamos que al re-consultar `RevokedAt` esté seteado.

- Vamos a generar un test llamado **`InvalidateExpiredUserRefreshTokensAsync_RevokesOnlyExpiredAndReturnsCount`**.
  Le insertamos 3 tokens del mismo user: uno expirado (`ExpiresAt <= now`, sin revocar), uno vigente
  y uno ya revocado/expirado; le pasamos `(userId, now)`. Procesamos y esperamos que devuelva `1`,
  que el expirado tenga `RevokedAt` seteado, que el vigente siga `RevokedAt == null` y que el ya
  revocado conserve su fecha original. **Trampa:** la condición es `ExpiresAt <= moment` (el igual
  cuenta como expirado).

- Vamos a generar un test llamado **`RevokeAllUserRefreshTokensAsync_RevokesOnlyUsersActiveTokens`**.
  Le insertamos 2 tokens activos del user A y 1 token activo del user B; le pasamos
  `(userA.Id, now)`. Procesamos y esperamos que devuelva `2`, que
  los dos tokens de A tengan `RevokedAt` y que el de B siga sin revocar.

**Aceptación**
- [ ] Búsqueda por hash, alta y persistencia de la revocación
- [ ] `InvalidateExpired*` revoca solo los expirados (`<=`) y cuenta bien
- [ ] `RevokeAll*` no toca los tokens de otros usuarios

---

#### TEST-24 · `PasswordHasherTests`

**Archivo:** `MateHR.Infrastructure.Tests/Authentication/PasswordHasherTests.cs`

**Nota:** los hashes con 210.000 iteraciones son lentos (~100ms cada uno). No crees decenas de
usuarios; 4 o 5 casos alcanzan.

- Vamos a generar un test llamado **`Hash_ValidPassword_ReturnsFourPartPbkdf2Format`**. Le pasamos
  `password = "secreta123"`; procesamos `Hash("secreta123")` y esperamos un string que se parta en
  `$` en 4 partes, donde la primera es `"pbkdf2-sha256"`, la segunda parsea como `int` de
  iteraciones y las dos últimas son Base64 válido.

- Vamos a generar un test llamado **`Hash_SamePasswordTwice_ReturnsDifferentHashes`**. Le pasamos la
  misma password dos veces; procesamos `Hash(...)` en ambas y esperamos strings distintos (salt
  aleatorio: dos hashes iguales sería un fallo de seguridad).

- Vamos a generar un test llamado **`Verify_CorrectPassword_ReturnsTrue`**. Le generamos un hash con
  `Hash("secreta123")` y le pasamos `("secreta123", hash)`; procesamos `Verify(...)` y esperamos
  `true`.

- Vamos a generar un test llamado **`Verify_WrongPassword_ReturnsFalse`**. Le generamos el hash de
  `"secreta123"` y le pasamos `("otra-clave", hash)`; procesamos y esperamos `false`.

- Vamos a generar un test llamado **`Verify_MalformedHash_ReturnsFalse`**. Le pasamos
  `("secreta123", "basura")`; procesamos y esperamos `false`, **sin que lance** (el split de 4
  partes falla y devuelve false).

- Vamos a generar un test llamado **`Verify_NullOrEmptyPassword_ReturnsFalse`**. Le pasamos
  `(null, hash)` y `("", hash)`; procesamos y esperamos `false` en ambos (el `IsNullOrEmpty` corta
  antes de hashear). Podemos escribirlo como `[Theory]` con `[InlineData]`.

- Vamos a generar un test llamado **`Hash_NullPassword_ThrowsArgumentException`**. Le pasamos
  `null`; procesamos `Hash(null)` y esperamos `ArgumentException`
  (`ArgumentException.ThrowIfNullOrWhiteSpace`).

**Aceptación**
- [ ] Formato del hash, salt aleatorio y `Verify` con password correcta/incorrecta
- [ ] Hash malformado y entradas null devuelven `false`, no lanzan

---

#### TEST-25 · `JwtServiceTests`

**Archivo:** `MateHR.Infrastructure.Tests/Authentication/JwtServiceTests.cs`

Necesitamos `IOptions<JwtSettings>` con un `SigningKey` válido (≥ 32 caracteres):

```csharp
var settings = Options.Create(new JwtSettings
{
    Issuer = "test",
    Audience = "test",
    SigningKey = "clave-de-prueba-con-32-caracteres-minimo",
    AccessTokenMinutes = 60,
    RefreshTokenDays = 14
});
```

- Vamos a generar un test llamado **`Constructor_InvalidSettings_ThrowsInvalidOperationException`**.
  Le pasamos settings con `Issuer = ""`; procesamos `new JwtService(settings)` y esperamos
  `InvalidOperationException`. Repetimos la historia con **`..._MissingAudience_...`**,
  **`..._MissingSigningKey_...`**, **`..._ShortSigningKey_...`** (le pasamos una key de 10
  caracteres), **`..._AccessTokenMinutesZero_...`** (`AccessTokenMinutes = 0`) y
  **`..._RefreshTokenDaysZero_...`** (`RefreshTokenDays = 0`) — son las 6 ramas de
  `ValidateSettings()`.

- Vamos a generar un test llamado **`GenerateAccessToken_NullUser_ThrowsArgumentNullException`**.
  Le pasamos `null` como `AuthenticatedUser`; procesamos `GenerateAccessToken(null)` y esperamos
  `ArgumentNullException`.

- Vamos a generar un test llamado **`GenerateAccessToken_ValidUser_ReturnsValidTokenWithClaims`**.
  Le pasamos un `AuthenticatedUser` con `Id`, `Email = "ana@acme.com"`, `Role = SuperAdmin`,
  `TenantId = null` y un `SecurityStamp`; procesamos `GenerateAccessToken(user)` y esperamos un
  `IssuedToken` cuyo `Token` se pueda validar con la **misma key** (usá
  `JwtSecurityTokenHandler.ValidateToken` + `TokenValidationParameters` como en `Program.cs`), con
  issuer/audience correctos y con los claims `sub`(user id), `email`, `role == "SuperAdmin"` y
  `security_stamp` coincidentes.

- Vamos a generar un test llamado **`GenerateAccessToken_UserWithTenant_IncludesTenantClaim`**. Le
  pasamos el mismo user pero con `TenantId = Guid.NewGuid()`; procesamos, validamos el token y
  esperamos que el claim `tenant_id` esté presente con ese valor.

- Vamos a generar un test llamado **`GenerateAccessToken_UserWithoutTenant_OmitsTenantClaim`**. Le
  pasamos `TenantId = null`; procesamos, validamos y esperamos que el claim `tenant_id` **no**
  exista en el token.

- Vamos a generar un test llamado **`GenerateAccessToken_ExpiresAtMatchesConfiguredMinutes`**. Le
  pasamos un user válido con `AccessTokenMinutes = 60`; procesamos y esperamos que
  `token.ExpiresAt` esté entre `now + 60min - 5s` y `now + 60min + 5s` (margen de segundos para el
  clock).

**Aceptación**
- [ ] Las 6 validaciones de `ValidateSettings()` y el `null` del user
- [ ] Token validable con la misma key, con claims correctos y `tenant_id` condicional
- [ ] `ExpiresAt` respeta `AccessTokenMinutes`

---

#### TEST-26 · `RefreshTokenServiceTests`

**Archivo:** `MateHR.Infrastructure.Tests/Authentication/RefreshTokenServiceTests.cs`

- Vamos a generar un test llamado **`Generate_ReturnsUrlSafeTokenWithoutPadding`**. Le pasamos
  nada (servicio con settings válidos); procesamos `Generate()` y esperamos un `Token` no vacío,
  sin caracteres `=` (por el `TrimEnd('=')`) y compuesto solo por `A-Za-z0-9-_` (base64url); y un
  `ExpiresAt` cercano a `now + RefreshTokenDays`.

- Vamos a generar un test llamado **`Generate_Twice_ReturnsDifferentTokens`**. Le pedimos dos
  tokens seguidos; procesamos `Generate()` dos veces y esperamos strings distintos (aleatorio).

- Vamos a generar un test llamado **`Generate_ExpiresAtMatchesConfiguredDays`**. Le pasamos
  `RefreshTokenDays = 14`; procesamos y esperamos `ExpiresAt` dentro de `now + 14 días ± 5 segundos`.

- Vamos a generar un test llamado   **`ComputeHash_SameInput_ReturnsSameHash`** — la determinística.
  Le pasamos `"token-de-prueba"` dos veces; procesamos `ComputeHash(...)` en ambas y esperamos el
  mismo string (SHA-256 del mismo input, sin salt).

- Vamos a generar un test llamado **`ComputeHash_Returns64LowercaseHexChars`**. Le pasamos
  `"token-de-prueba"`; procesamos y esperamos un string de 64 caracteres, todos en
  `[0-9a-f]` (formato `Convert.ToHexString(...).ToLowerInvariant()`).

- Vamos a generar un test llamado **`ComputeHash_NullOrEmpty_ThrowsArgumentException`**. Le pasamos
  `null` y `""`; procesamos y esperamos `ArgumentException` en ambos (podemos usar `[Theory]`).

**Aceptación**
- [ ] Token base64url sin padding, aleatorio y con expiración configurada
- [ ] `ComputeHash` determinista, 64 hex minúsculas y entradas vacías lanzan

---

#### TEST-27 · `TenantContextTests`

**Archivo:** `MateHR.Infrastructure.Tests/Services/TenantContextTests.cs`

Es simple y no requiere nada más que `new TenantContext()`.

- Vamos a generar un test llamado **`CurrentTenantId_WhenNotSet_IsNull`**. Le creamos el contexto
  nuevo; procesamos leer `CurrentTenantId` y esperamos `null`.

- Vamos a generar un test llamado **`SetCurrentTenantId_WithGuid_SetsValue`**. Le pasamos
  `tenantId = Guid.NewGuid()`; procesamos `SetCurrentTenantId(guid)` y esperamos que
  `CurrentTenantId == guid`.

- Vamos a generar un test llamado **`SetCurrentTenantId_WithNull_ClearsValue`**. Le seteamos primero
  un guid y después le pasamos `null`; procesamos `SetCurrentTenantId(null)` y esperamos
  `CurrentTenantId == null`.

**Aceptación**
- [ ] Default `null`, set y clear

---

#### TEST-28 · `UserSecurityStampValidatorTests`

**Archivo:** `MateHR.Infrastructure.Tests/Services/UserSecurityStampValidatorTests.cs`

Necesita un contexto InMemory con un `User` + un `IMemoryCache` real
(`new MemoryCache(new MemoryCacheOptions())`). Guarda en caché 30 s por userId. **Ojo:** creá la
`MemoryCache` por test (local), no la dejes compartida entre tests.

- Vamos a generar un test llamado **`IsStampCurrentAsync_EmptyUserId_ReturnsFalseWithoutQuerying`**.
  Le pasamos `userId = Guid.Empty` y cualquier `stamp`; procesamos `IsStampCurrentAsync(...)` y
  esperamos `false`, y verificamos que no se pegó a la base (con el contexto vacío alcanza: si
  consulta, devolvería `null` y también `false`, así que el valor del assert es `false` igual).

- Vamos a generar un test llamado **`IsStampCurrentAsync_UnknownUser_ReturnsFalse`**. Le pasamos un
  `userId` que no existe; procesamos y esperamos `false`.

- Vamos a generar un test llamado **`IsStampCurrentAsync_InactiveUser_ReturnsFalse`**. Le
  insertamos un user con `IsActive = false` y le pasamos su `SecurityStamp` **correcto**; procesamos
  y esperamos `false` (un usuario inactivo ni siquiera con el stamp válido pasa).

- Vamos a generar un test llamado **`IsStampCurrentAsync_MatchingStamp_ReturnsTrue`**. Le
  insertamos un user activo y le pasamos `user.SecurityStamp`; procesamos y esperamos `true`.

- Vamos a generar un test llamado **`IsStampCurrentAsync_DifferentStamp_ReturnsFalse`**. Le
  insertamos un user activo y le pasamos `Guid.NewGuid()` (un stamp distinto); procesamos y
  esperamos `false`.

- Vamos a generar un test llamado **`IsStampCurrentAsync_CacheHitsUntilInvalidate`** — el ciclo
  caché → invalidar → base. Le insertamos un user activo; primero consultamos con el stamp correcto
  (va a la base y cachea), después **cambiamos el `SecurityStamp` en la base directamente con el
  context** y volvemos a consultar con el stamp viejo: esperamos `true` (sigue cacheado, sin pegar
  a la base); recién cuando llamamos `Invalidate(user.Id)` esperamos que la siguiente consulta con
  el stamp viejo devuelva `false` y con el nuevo devuelva `true`.

**Aceptación**
- [ ] `Guid.Empty`, usuario inexistente e inactivo → `false`
- [ ] Stamp correcto/incorrecto → `true`/`false`
- [ ] Ciclo de caché completo: cachea, no se entera del cambio y al `Invalidate` vuelve a la base

---

## Bloque 5d — Api (integración con WebApplicationFactory)

Para estos tests necesitás una factory que levante la API real pero con base en memoria y JWT
configurado. Base para `CustomWebApplicationFactory`:

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "test",
                ["Jwt:Audience"] = "test",
                ["Jwt:SigningKey"] = "clave-de-prueba-con-32-caracteres-minimo",
                ["Jwt:AccessTokenMinutes"] = "60",
                ["Jwt:RefreshTokenDays"] = "14"
            });
        });
        builder.ConfigureServices(services =>
        {
            // Sacar el DbContext registrado por AddInfrastructure y usar InMemory
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null) services.Remove(descriptor);
            services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase("api-tests"));
        });
    }
    public HttpClient CreateAuthenticatedClient(UserRole role, Guid? tenantId = null) { ... }
}
```

Y un helper para generar tokens: creá un `AuthenticatedUser` y un `JwtService` (o un
`JwtSecurityTokenHandler` directo con la misma key de prueba) y mandá el `Authorization: Bearer
<token>`.

**A tener en cuenta:**
- Los enums viajan como texto en el JSON (`JsonStringEnumConverter`), así que en los bodies usás
  `"status": "Active"`, `"industry": "Technology"`, `"role": "SuperAdmin"`
- El refresh token va en una cookie `HttpOnly` (`matehr_refresh_token`): para leerlo del response
  usá `response.Headers.GetValues("Set-Cookie")` y después reenviá esa cookie en el request
  (`requestHeaders.Add("Cookie", ...)`)
- Los endpoints protegidos devuelven 401 sin token. `TenantsController` y `UsersController` exigen
  política `RequireSuperAdmin` (403 si el rol es `Admin`); `AuthController.Me` solo exige estar
  autenticado

#### TEST-29 · `AuthControllerTests`

**Archivo:** `MateHR.Api.Tests/Controllers/v1/AuthControllerTests.cs`

Necesitamos un usuario en la base InMemory para los flujos de login/refresh: cargalo en el setup
con el propio `UserProvisioningService`/`UserRepository` contra el contexto, o con una seed dentro
de la factory.

- Vamos a generar un test llamado **`Login_WithValidCredentials_Returns200WithTokensAndCookie`**.
  Le mandamos `POST /api/v1/auth/login` con body `{"email":"ana@acme.com","password":"secreta123"}`
  (usuario sembrado con esa password); procesamos la request y esperamos `200 OK`, un body JSON con
  `accessToken` no vacío y el objeto `user` con `email == "ana@acme.com"`, y un header `Set-Cookie`
  que contenga `matehr_refresh_token=`, `HttpOnly` y `Path=/api/v1/auth`.

- Vamos a generar un test llamado **`Login_WithInvalidCredentials_Returns401`**. Le mandamos el
  mismo body con `password = "clave-mala"`; procesamos y esperamos `401 Unauthorized` con un body
  `message` (y sin cookie de refresh).

- Vamos a generar un test llamado **`Login_WithShortPassword_Returns400`**. Le mandamos
  `password = "123"`; procesamos y esperamos `400 Bad Request` con el diccionario `errors` (el
  `ValidationException` lo convierte el middleware).

- Vamos a generar un test llamado **`Refresh_WithValidCookie_Returns200AndRotatesCookie`**. Le
  hacemos login, le mandamos `POST /api/v1/auth/refresh` con la cookie `matehr_refresh_token` que
  devolvió el login; procesamos y esperamos `200`, un `accessToken` nuevo y un `Set-Cookie` cuyo
  refresh token sea **distinto** al de la cookie enviada (rotación).

- Vamos a generar un test llamado **`Refresh_WithReusedCookie_Returns401`**. Le hacemos login,
  refrescamos una vez (ya rotó) y mandamos **la cookie vieja** del primer refresh; procesamos y
  esperamos `401 Unauthorized` — es el protocolo de reuso, y de paso los demás tokens del user
  quedaron revocados.

- Vamos a generar un test llamado **`Me_WithoutToken_Returns401`**. Le mandamos
  `GET /api/v1/auth/me` sin header `Authorization`; procesamos y esperamos `401`.

- Vamos a generar un test llamado **`Me_WithValidToken_Returns200WithUserResponse`**. Le generamos
  un access token para un user creado en el setup y se lo mandamos en `Authorization: Bearer <token>`;
  procesamos y esperamos `200` con el `UserResponse` (mismo `id` y `email` del user).

- Vamos a generar un test llamado **`Logout_WithCookie_Returns204AndDeletesCookie`**. Le hacemos
  login, mandamos `POST /api/v1/auth/logout` con la cookie; procesamos y esperamos
  `204 No Content` y un `Set-Cookie` con `matehr_refresh_token=` vacío / expirado (borrada).

**Aceptación**
- [ ] Login 200/401/400 con cookie HttpOnly
- [ ] Refresh rota la cookie y el reuso devuelve 401
- [ ] `me` 401/200 y logout 204

---

#### TEST-30 · `TenantsControllerTests`

**Archivo:** `MateHR.Api.Tests/Controllers/v1/TenantsControllerTests.cs`

Todos los endpoints cuelgan de `[Authorize(Policy = RequireSuperAdmin)]`.

- Vamos a generar un test llamado **`Endpoints_WithoutToken_Returns401`**. Le mandamos
  `POST /api/v1/tenants` sin `Authorization`; procesamos y esperamos `401`. Repetimos la historia
  con **`Endpoints_WithAdminToken_Returns403`**: le mandamos la misma request con un token de rol
  `Admin` y esperamos `403 Forbidden` (la política es SuperAdmin).

- Vamos a generar un test llamado **`Create_WithSuperAdminToken_Returns201WithLocation`**. Le
  mandamos `POST /api/v1/tenants` con token SuperAdmin y body válido (`name`, `cuit` de 11 dígitos,
  `ownerEmail`, `industry: "Technology"`); procesamos y esperamos `201 Created`, un header
  `Location` apuntando a `/api/v1/tenants/{id}` y el body con `slug == "acme-corp"` generado.

- Vamos a generar un test llamado **`Create_DuplicateSlug_Returns409`**. Le mandamos dos veces el
  mismo body (o pre-cargamos un tenant con el mismo nombre); procesamos el segundo request y
  esperamos `409 Conflict`.

- Vamos a generar un test llamado **`Create_InvalidDto_Returns400WithErrors`**. Le mandamos un body
  con `name = ""` y `cuit = "abc"`; procesamos y esperamos `400` con el objeto `errors` que agrupa
  por campo.

- Vamos a generar un test llamado **`GetAll_WithStatusFilter_Returns200WithPagedResult`**. Le
  sembramos tenants `Active` y `Suspended` y le mandamos `GET /api/v1/tenants?status=Active`; el
  procesamos y esperamos `200` con `items` solo de los `Active` y `totalCount` acorde.

- Vamos a generar un test llamado **`GetById_ExistingTenant_Returns200`**. Le mandamos
  `GET /api/v1/tenants/{id}` con un id sembrado; procesamos y esperamos `200` con ese tenant.

- Vamos a generar un test llamado **`GetById_UnknownTenant_Returns404`**. Le mandamos
  `GET /api/v1/tenants/{Guid.NewGuid()}`; procesamos y esperamos `404`.

- Vamos a generar un test llamado **`GetBySlug_ExistingSlug_Returns200`** y
  **`GetBySlug_UnknownSlug_Returns404`**. Le mandamos `GET /api/v1/tenants/slug/acme-corp` (con el
  tenant sembrado) esperando `200`, y después `GET .../slug/no-existe` esperando `404`.

- Vamos a generar un test llamado **`Update_ExistingTenant_Returns200`**. Le mandamos
  `PUT /api/v1/tenants/{id}` con un DTO válido y distinto; procesamos y esperamos `200` con los
  valores nuevos.

- Vamos a generar un test llamado **`Update_DuplicateSlugOfAnotherTenant_Returns409`**. Le
  pre-cargamos dos tenants, y le mandamos al segundo un `PUT` con el nombre del primero; procesamos
  y esperamos `409 Conflict`.

- Vamos a generar un test llamado **`ChangeStatus_WithSuperAdminToken_Returns200`**. Le mandamos
  `PATCH /api/v1/tenants/{id}/status` con body `{"status":"Suspended"}`; procesamos y esperamos
  `200` con `status == "Suspended"`. Repetimos la historia con
  **`ChangeRecruitmentMode_...Returns200`** (`PATCH .../recruitment-mode`,
  `{"recruitmentMode":"External"}`) y **`ChangeSubscriptionType_...Returns200`**
  (`PATCH .../subscription-type`, `{"subscriptionType":"Yearly"}`).

**Aceptación**
- [ ] 401 sin token y 403 con rol `Admin` en todos los endpoints
- [ ] CRUD completo: 201 con `Location`, 200, 404 y 409
- [ ] Los 3 PATCH responden 200 con el valor actualizado

---

#### TEST-31 · `UsersControllerTests`

**Archivo:** `MateHR.Api.Tests/Controllers/v1/UsersControllerTests.cs`

- Vamos a generar un test llamado **`Create_WithoutToken_Returns401`**. Le mandamos
  `POST /api/v1/users` sin `Authorization`; procesamos y esperamos `401`.

- Vamos a generar un test llamado **`Create_WithAdminToken_Returns403`**. Le mandamos la misma
  request con token de rol `Admin`; procesamos y esperamos `403`.

- Vamos a generar un test llamado **`Create_SuperAdminValidUser_Returns201WithUserResponse`**. Le
  mandamos `POST /api/v1/users` con token SuperAdmin y body
  `{"firstName":"Ana","lastName":"Pérez","email":"ana@acme.com","password":"secreta123","role":"Recruiter","tenantId":"<guid de un tenant sembrado>"}`;
  procesamos y esperamos `201 Created`, header `Location` con `/api/v1/auth/me` y body con
  `email == "ana@acme.com"` y `role == "Recruiter"`.

- Vamos a generar un test llamado **`Create_DuplicateEmail_Returns409`**. Le mandamos dos veces el
  mismo email (o pre-cargamos el user); procesamos el segundo y esperamos `409`.

- Vamos a generar un test llamado **`Create_InvalidDto_Returns400WithErrors`**. Le mandamos un body
  con `password = "123"` y `email = "no-es-un-mail"`; procesamos y esperamos `400` con `errors`.

- Vamos a generar un test llamado **`Create_SuperAdminWithTenantId_Returns400`**. Le mandamos un
  body con `"role":"SuperAdmin"` y `tenantId` seteado; procesamos y esperamos `400` (lo rechaza el
  `CreateUserRequestValidator` con el mensaje "Un SuperAdmin no pertenece a un tenant.").

**Aceptación**
- [ ] 401/403 en los endpoints protegidos
- [ ] 201 feliz, 409 duplicado, 400 inválido y 400 por SuperAdmin con tenant

---

#### TEST-32 · `ExceptionMiddlewareTests`

**Archivo:** `MateHR.Api.Tests/Middleware/ExceptionMiddlewareTests.cs`

Podemos testear el middleware en forma unitaria sin levantar toda la API: creamos un `HttpContext`
con un `RequestAborted`, un `next` que lance la excepción a probar, ejecutamos
`middleware.InvokeAsync(context)` y verificamos el `StatusCode` del response.

Para cada test la historia es la misma: le pasamos una excepción distante al `next`, procesamos la
invocación y esperamos un código de estado (y, de paso, el body `{ message, traceId }`).

- Vamos a generar un test llamado **`InvokeAsync_KeyNotFoundException_Returns404`**. Le hacemos
  lanzar `new KeyNotFoundException("No se encontro el tenant")` desde el `next`; procesamos
  `InvokeAsync(context)` y esperamos `404`.

- Vamos a generar un test llamado **`InvokeAsync_SlugAlreadyExistsException_Returns409`**. Le
  hacemos lanzar `new SlugAlreadyExistsException("acme-corp")`; procesamos y esperamos `409`.

- Vamos a generar un test llamado **`InvokeAsync_EmailAlreadyExistsException_Returns409`**. Le
  hacemos lanzar `new EmailAlreadyExistsException("ana@acme.com")`; procesamos y esperamos `409`.

- Vamos a generar un test llamado **`InvokeAsync_InvalidCredentialsException_Returns401`**. Le
  hacemos lanzar `new InvalidCredentialsException()`; procesamos y esperamos `401`.

- Vamos a generar un test llamado **`InvokeAsync_ArgumentException_Returns400`**. Le hacemos lanzar
  `new ArgumentException("mensaje")`; procesamos y esperamos `400`.

- Vamos a generar un test llamado **`InvokeAsync_ValidationException_Returns400WithErrors`**. Le
  hacemos lanzar `new FluentValidation.ValidationException(errors)` con dos errores de campos
  distintos; procesamos y esperamos `400` y un body JSON cuyo `errors` tenga una clave por
  `PropertyName` con arrays de mensajes.

- Vamos a generar un test llamado **`InvokeAsync_GenericException_Returns500`**. Le hacemos lanzar
  `new Exception("boom")`; procesamos y esperamos `500` y que el `message` sea
  `"Ocurrio un error inesperado."` (no filtrar el texto de la excepción al cliente).

- Vamos a generar un test llamado **`InvokeAsync_HttpRequestException_Returns502`**. Le hacemos
  lanzar `new HttpRequestException("falla externa")`; procesamos y esperamos `502` con el mensaje
  genérico "No se pudo comunicar con un servicio externo.".

**Aceptación**
- [ ] Los 7 mapeos de excepción → status cubiertos (404, 409 x2, 401, 400, 400 con `errors`, 500)
- [ ] El body de error trae `message` y `traceId`, sin exponer detalles internos en el 500

---

## Orden de ejecución sugerido (para los tests)

```
Bloque 5a — Domain     TEST-01 … TEST-07   (primeros: cero dependencias, más rápidos)
Bloque 5b — Application TEST-08 → guardián: TEST-09, 10, 15, 16
Bloque 5c — Infrastructure TEST-24, 25 → después 21-23 (necesitan el contexto InMemory)
Bloque 5d — Api        TEST-00 (partial Program) y TEST-29, 30 (últimos)
```

No hace falta respetar el orden al pie de la letra, pero arrancar por Domain te da confianza
rápida y te familiariza con los objetos sin tener que armar mocks. Los dos que no deberías
delegar o posponer para el final son **TEST-09/10 (Create/Update Tenant)** y
**TEST-15/16 (Login y Refresh)**, porque son el corazón del negocio y de la seguridad.
