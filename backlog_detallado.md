# Backlog detallado — Módulo Recruitment (TDD)

> Este archivo deriva de `backlog.md` y está pensado para **copiar tarjeta por tarjeta** en Asana o Trello.
> No contiene código de producción: contiene la historia de usuario, la guía técnica, el plan TDD,
> los casos de prueba con datos/acción/resultado, los archivos a tocar y los criterios de aceptación.
>
> Regla base: **primero el test que falla, después la implementación mínima, después refactor**.
> No se avanza de capa sin la anterior en verde.

---

## 0. Cómo usar este archivo en Asana/Trello

### 0.1 Estructura de tarjetas

- **Tarjeta padre:** `REC-xx · <cosa>` (épica). Contiene la historia de usuario grande, alcance,
  dependencias y criterios de aceptación de la épica.
- **Tarjetas hijas:** `REC-xx.1` a `REC-xx.4` (una por capa). Cada una es una tarjeta ejecutable.
- **Checklist:** cada subtarea trae una lista de criterios de aceptación para pegar como checklist.
- **Etiquetas sugeridas:** `modulo:recruitment`, `capa:domain`, `capa:infrastructure`,
  `capa:application`, `capa:api`, `tdd`, `tipo:epica`, `tipo:subtarea`.
- **Columnas sugeridas:** `Backlog` → `En curso` → `Review` → `Hecho`.
- **Regla de WIP:** una sola subtarea `En curso` por persona. Si una subtarea depende de otra, no se
  mueve a `En curso` hasta que la dependencia esté en `Hecho`.

### 0.2 Qué copiar en cada tarjeta

1. Título: el ID y el nombre de la subtarea.
2. Descripción: desde `Historia de usuario` hasta `Trampas / notas`.
3. Checklist: los `Criterios de aceptación`.
4. Etiquetas: las de la tarjeta.
5. Adjunto o link: este archivo, sección correspondiente.

### 0.3 Orden de lectura

1. Leer la tarjeta padre.
2. Leer la subtarea.
3. Leer el `Anexo A` si la tarjeta es de catálogo (`REC-02` a `REC-07`).
4. Escribir los tests de la tabla.
5. Recién después implementar.

---

## 1. Contrato de trabajo

### 1.1 Ciclo TDD obligatorio por subtarea

1. **Rojo:** escribir todos los tests de la tabla de la subtarea. Ejecutarlos y confirmar que fallan
   por la razón correcta (no por error de compilación ni por un mock mal armado).
2. **Verde:** implementar lo mínimo para que pasen, sin agregar comportamiento no testeado.
3. **Refactor:** mejorar nombres, extraer métodos privados y quitar duplicación, manteniendo verde.
4. **Verificación:** correr toda la suite de la capa y, si la capa es Application o Api, correr también
   los tests de contrato/mapping.
5. **Commit:** un commit por subtarea, con mensaje `REC-xx.y <capa>: <resumen>`.

> Si un test no puede escribirse antes porque la capa aún no existe, se escribe el test que define el
> contrato (interfaz o firma esperada) y se deja fallando. No se implementa “de paso”.

### 1.2 Reglas de test

- Un test = un comportamiento. Nada de tests que validan cinco cosas a la vez.
- Nombres: `Metodo_Escenario_ResultadoEsperado`.
- Patrón AAA: Arrange (datos), Act (acción), Assert (resultado).
- Cada test crea sus propios datos. No compartir estado mutable entre tests.
- No testear detalles internos: testear comportamiento observable.
- En Domain no se usan mocks.
- En Infrastructure se usa `ApplicationDbContext` real con EF InMemory.
- En Application se mockean repositorios e `ITenantContext`; el validador y el mapper se usan reales
  (o el `TestMapper` existente) salvo que la tarjeta diga lo contrario.
- En Api se usa `CustomWebApplicationFactory`, pipeline real y tokens reales.
- Los tests de tenant isolation son obligatorios en Infrastructure y Application.
- EF InMemory **no aplica índices únicos**: la unicidad se valida en Application y se testea ahí.
- Los tests de API no deben depender del orden de ejecución ni de una base compartida.

### 1.3 Límites entre capas

| Capa | Qué vive acá | Qué NO vive acá |
|---|---|---|
| Domain | Entidades, enums, value objects, guardas, excepciones de negocio, interfaces de repositorio | EF, DTOs, validadores, controllers, `ITenantContext` en entidades |
| Infrastructure | Configuraciones EF, DbContext, repositorios, servicios técnicos (email, hashing) | Reglas de negocio, DTOs, controllers |
| Application | Casos de uso, DTOs, validadores, mappings, interfaces de casos de uso, lectura de `ITenantContext` | EF directo, `DbContext`, controllers |
| Api | Controllers, policies, middleware, composición | Reglas de negocio, acceso directo a repositorios |

### 1.4 Mapa de errores a HTTP

| Excepción | HTTP | Cuándo |
|---|---|---|
| `ValidationException` (FluentValidation) | 400 | DTO inválido, reglas de entrada |
| `ArgumentException` / `ArgumentOutOfRangeException` | 400 | Guarda de dominio mal usada desde un caso de uso |
| `UnauthorizedAccessException` | 403 | No hay tenant en contexto |
| `KeyNotFoundException` | 404 | Entidad inexistente o de otro tenant |
| `DuplicateEntityNameException` | 409 | Nombre/email duplicado en el tenant |
| `EntityInUseException` | 409 | Borrado bloqueado por referencias |
| `JobAlreadyClosedException` | 409 | Cerrar un job ya cerrado |
| `InvalidOperationException` | 409 (si se mapea) / 500 (hoy) | Ver decisión pendiente D-04 |
| `EmailAlreadyExistsException` | 409 | Email de candidato duplicado, si se reutiliza |

> **Importante:** hoy `ExceptionMiddleware` **no** mapea `InvalidOperationException` genérica. Si una
> tarjeta espera 409 por estado inválido, crear una excepción específica del módulo y mapearla, o
> agregar el mapeo explícito. No dejar que un caso de negocio termine en 500.

### 1.5 Definition of Ready

Una subtarea puede empezar solo si:

- La dependencia está en `Hecho`.
- La historia de usuario y los criterios de aceptación están claros.
- Los casos de prueba de la tabla están definidos.
- Los archivos a crear/tocar están identificados.
- No hay una decisión pendiente que cambie el contrato (si la hay, se resuelve antes).

### 1.6 Definition of Done

- Tests de la subtarea escritos primero y en verde.
- Suite completa de la capa en verde (`dotnet test`).
- Sin warnings nuevos.
- Sin código muerto ni `TODO` sin tarjeta.
- DI actualizada si se agregó un caso de uso o servicio.
- `AssertConfigurationIsValid` pasa si se agregó un mapping.
- Migración aplicada si la subtarea la requiere.
- Tarjeta actualizada con evidencia (nombre de tests, comando ejecutado, PR).

---

## 2. Estado actual detectado en el repo (2026-10-29)

| Tarjeta | Estado | Evidencia / nota |
|---|---|---|
| REC-00.1 | Hecho | Enums, guard y excepciones existen; tests de dominio presentes. |
| REC-00.2 | Parcial / incremental | `JobStatus` ya tiene DbSet, configuración y repositorio. Faltan DbSets/configuraciones de las demás entidades y la migración final. |
| REC-00.3 | Hecho | `AddRecruitment()` existe y está llamado desde `AddApplication()`. |
| REC-00.4 | Hecho, verificar | Policies y middleware existen. Ver D-04: `InvalidOperationException` no está mapeada. |
| REC-01.1 | Hecho | `JobStatus` + tests de dominio. |
| REC-01.2 | Hecho | Configuración, repositorio, interfaz y tests de repositorio. |
| REC-01.3 | Pendiente | No hay casos de uso, DTOs, validadores ni mapping de JobStatus. |
| REC-01.4 | Pendiente | No hay `JobStatusesController`. |
| REC-02 a REC-11 | Pendiente | Solo existe lo listado arriba. |

> Antes de arrancar `REC-01.3`, correr los tests de dominio e infraestructura de `JobStatus` para
> confirmar que están verdes. Si no lo están, la primera tarjeta es arreglar eso, no avanzar.

### Primer paso concreto (para hoy)

1. Correr la suite actual de `JobStatus` (dominio + infraestructura) y confirmar verde.
2. Resolver D-01 y D-05 (afectan el contrato del repositorio de catálogo).
3. Abrir la tarjeta `REC-01.3` y escribir los 17 tests de su tabla **antes** de tocar código.
4. Implementar `CreateJobStatus` primero; cuando esté verde, seguir con `Update`, `Delete`, `GetById`
   y `GetAll`.
5. Recién cuando `REC-01.3` esté en verde, abrir `REC-01.4` y hacer los 14 tests de API.

---

## 3. Decisiones pendientes que necesito que confirmes

Estas decisiones cambian contratos. Resolverlas antes de la tarjeta indicada.

| ID | Decisión | Recomendación | Impacto |
|---|---|---|---|
| D-01 | ¿Cómo se detecta que un catálogo está en uso para bloquear el borrado? | Agregar `IsInUseAsync(tenantId, id)` al repositorio de cada catálogo. Infraestructura sabe si hay jobs que lo referencian; Application solo pregunta. | REC-01.3 a REC-07.3 |
| D-02 | ¿Cómo se define el estado inicial de un proceso de candidato? | Agregar `IsInitial` a `CandidateProcessStatus`, con regla “un solo inicial por tenant”. | REC-07 y REC-11.3 |
| D-03 | ¿Quién es el usuario que solicita el borrado de un job? | Agregar `ICurrentUserContext` en Application (o pasar `userId` desde el controller). Recomiendo `ICurrentUserContext` para no contaminar DTOs. | REC-08.3 |
| D-04 | ¿Qué excepción usar para “confirmar borrado sin solicitud previa”? | Crear `DeletionNotRequestedException` en Domain/Recruitment/Exceptions y mapearla a 409. No mapear toda `InvalidOperationException` a 409. | REC-08.3 y REC-08.4 |
| D-05 | ¿La unicidad de nombres de catálogo es case-insensitive? | Sí. “Ventas” y “ventas” deben considerarse duplicados. Ajustar repositorio y tests. | REC-01.2 en adelante |
| D-06 | ¿Teléfono, ubicación y LinkedIn son obligatorios? ¿DNI 7-8 dígitos? | Teléfono obligatorio y trimmeado (≤30); ubicación opcional (≤100); LinkedIn opcional y URL absoluta si viene; DNI entre 1.000.000 y 99.999.999. | REC-09.1 |
| D-07 | ¿Se validan los `ProcessStepIds` al crear/editar un job? | Sí: cada step debe existir en el tenant. Si no, `KeyNotFoundException` o `ValidationException` (elegir una y testearla). | REC-08.3 |
| D-08 | Si falla el email del apply, ¿se revierte la postulación? | No. Persistir primero, enviar después; si el email falla, loguear y devolver éxito. Un fallo de SMTP no debe perder la postulación. | REC-11.3 |
| D-09 | ¿Se permite más de un proceso para el mismo candidato+job? | No. Agregar chequeo en repositorio y, si se puede, índice único. El apply repetido devuelve 409. | REC-10.2 y REC-11.3 |
| D-10 | ¿Cuándo se crea la migración `AddRecruitmentModule`? | Una sola migración cuando estén todas las configuraciones (después de REC-10.2, antes de REC-11.4). No crear una por entidad. | REC-00.2 |
| D-11 | ¿Cómo se resuelve el tenant en el portal público? | Por el job: el use case busca el job por ID global y toma su `TenantId`. No hay `ITenantContext` en endpoints anónimos. | REC-11.3 |
| D-12 | ¿Qué jobs ve el portal público? | Solo jobs activos, no cerrados y no confirmados para borrado. Sin datos sensibles. | REC-11.3 y REC-11.4 |

---

## 4. Resumen de tarjetas

| ID | Título | Capa | Depende de | Tamaño | Estado |
|---|---|---|---|---|---|
| REC-00 | Fundamentos del módulo | Épica | — | M | Parcial |
| REC-00.1 | Domain + tests | Domain | — | S | Hecho |
| REC-00.2 | Infrastructure + tests | Infrastructure | 00.1 | M | Parcial |
| REC-00.3 | Application + tests | Application | 00.1 | S | Hecho |
| REC-00.4 | Api + tests | Api | 00.2, 00.3 | S | Hecho |
| REC-01 | Estados de job (`JobStatus`) | Épica | REC-00 | S | En curso |
| REC-01.1 | Domain + tests | Domain | REC-00.1 | S | Hecho |
| REC-01.2 | Infrastructure + tests | Infrastructure | REC-00.2, 01.1 | S | Hecho |
| REC-01.3 | Application + tests | Application | 01.2 | M | Pendiente |
| REC-01.4 | Api + tests | Api | 01.3 | S | Pendiente |
| REC-02 | Áreas (`Area`) | Épica | REC-00 | S | Pendiente |
| REC-02.1 | Domain + tests | Domain | REC-00.1 | S | Pendiente |
| REC-02.2 | Infrastructure + tests | Infrastructure | 02.1 | S | Pendiente |
| REC-02.3 | Application + tests | Application | 02.2 | S | Pendiente |
| REC-02.4 | Api + tests | Api | 02.3 | S | Pendiente |
| REC-03 | Subáreas (`SubArea`) | Épica | REC-00, REC-02 | M | Pendiente |
| REC-03.1 | Domain + tests | Domain | 02.1 | S | Pendiente |
| REC-03.2 | Infrastructure + tests | Infrastructure | 03.1 | M | Pendiente |
| REC-03.3 | Application + tests | Application | 03.2 | M | Pendiente |
| REC-03.4 | Api + tests | Api | 03.3 | S | Pendiente |
| REC-04 | Clientes (`Client`) | Épica | REC-00 | S | Pendiente |
| REC-04.1 | Domain + tests | Domain | REC-00.1 | S | Pendiente |
| REC-04.2 | Infrastructure + tests | Infrastructure | 04.1 | S | Pendiente |
| REC-04.3 | Application + tests | Application | 04.2 | M | Pendiente |
| REC-04.4 | Api + tests | Api | 04.3 | S | Pendiente |
| REC-05 | Pasos de proceso (`ProcessStep`) | Épica | REC-00 | S | Pendiente |
| REC-05.1 | Domain + tests | Domain | REC-00.1 | S | Pendiente |
| REC-05.2 | Infrastructure + tests | Infrastructure | 05.1 | S | Pendiente |
| REC-05.3 | Application + tests | Application | 05.2 | S | Pendiente |
| REC-05.4 | Api + tests | Api | 05.3 | S | Pendiente |
| REC-06 | Motivos de búsqueda (`SearchReason`) | Épica | REC-00 | S | Pendiente |
| REC-06.1 | Domain + tests | Domain | REC-00.1 | S | Pendiente |
| REC-06.2 | Infrastructure + tests | Infrastructure | 06.1 | S | Pendiente |
| REC-06.3 | Application + tests | Application | 06.2 | S | Pendiente |
| REC-06.4 | Api + tests | Api | 06.3 | S | Pendiente |
| REC-07 | Estados de proceso (`CandidateProcessStatus`) | Épica | REC-00 | M | Pendiente |
| REC-07.1 | Domain + tests | Domain | REC-00.1 | S | Pendiente |
| REC-07.2 | Infrastructure + tests | Infrastructure | 07.1 | S | Pendiente |
| REC-07.3 | Application + tests | Application | 07.2 | M | Pendiente |
| REC-07.4 | Api + tests | Api | 07.3 | S | Pendiente |
| REC-08 | Job/empleo | Épica | REC-00 a REC-07 | L | Pendiente |
| REC-08.1 | Domain + tests | Domain | REC-01.1 a 07.1 | L | Pendiente |
| REC-08.2 | Infrastructure + tests | Infrastructure | 08.1 | L | Pendiente |
| REC-08.3 | Application + tests | Application | 08.2 | L | Pendiente |
| REC-08.4 | Api + tests | Api | 08.3 | M | Pendiente |
| REC-09 | Candidatos | Épica | REC-00, REC-10 | L | Pendiente |
| REC-09.1 | Domain + tests | Domain | REC-00.1 | M | Pendiente |
| REC-09.2 | Infrastructure + tests | Infrastructure | 09.1 | M | Pendiente |
| REC-09.3 | Application + tests | Application | 09.2, 10.2 | L | Pendiente |
| REC-09.4 | Api + tests | Api | 09.3 | M | Pendiente |
| REC-10 | Procesos de selección | Épica | REC-00, 02 a 09 | L | Pendiente |
| REC-10.1 | Domain + tests | Domain | REC-00.1 | M | Pendiente |
| REC-10.2 | Infrastructure + tests | Infrastructure | 10.1 | M | Pendiente |
| REC-10.3 | Application + tests | Application | 10.2 | L | Pendiente |
| REC-10.4 | Api + tests | Api | 10.3 | M | Pendiente |
| REC-11 | Portal externo + email | Épica | REC-00, 08, 09, 10 | L | Pendiente |
| REC-11.1 | Domain + tests | Domain | 09.1, 10.1 | S | Pendiente |
| REC-11.2 | Infrastructure + tests | Infrastructure | 11.1 | M | Pendiente |
| REC-11.3 | Application + tests | Application | 11.2, 09.3, 10.3 | L | Pendiente |
| REC-11.4 | Api + tests | Api | 11.3 | M | Pendiente |

---

## 5. Plantilla de tarjeta (para copiar)

```text
Título: REC-xx.y — <Capa> + tests

Historia de usuario:
Como <rol> quiero <acción> para <beneficio>.

Guía técnica:
1. ...
2. ...

Depende de: ...

Plan TDD:
1. Escribir estos tests y verlos fallar: ...
2. Implementar lo mínimo: ...
3. Verdes.
4. Refactor.

Casos de prueba:

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | ... | ... | ... |

Archivos:
- ...

Criterios de aceptación:
- [ ] ...
- [ ] ...

Trampas / notas:
- ...

DoD: tests verdes, sin warnings, DI/mapping actualizados, PR con evidencia.
```

---

## 6. Anexo A — Patrón Catálogo (aplicar en REC-01 a REC-07)

Los catálogos (`JobStatus`, `Area`, `SubArea`, `Client`, `ProcessStep`, `SearchReason`,
`CandidateProcessStatus`) comparten el mismo esqueleto. Este anexo es parte de esas tarjetas.

### A.1 Domain — casos base

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| D1 | tenant válido, nombre “Ventas” | construir entidad | Id no vacío, TenantId correcto, Name “Ventas”, sin campos extra |
| D2 | tenant `Guid.Empty` | construir entidad | `ArgumentException` |
| D3 | nombre `"   "` | construir entidad | `ArgumentException` |
| D4 | nombre `"  Ventas  "` | construir entidad | Name queda `"Ventas"` (trim) |
| D5 | nombre de longitud máxima + 1 | construir entidad | `ArgumentException` |
| D6 | entidad válida | `Rename("  Nuevo  ")` | Name `"Nuevo"` |
| D7 | entidad válida | `Rename("")` | `ArgumentException` y el nombre anterior se conserva |

### A.2 Infrastructure — casos base

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| I1 | entidad persistida en tenant A | `GetByIdAsync(A, id)` | devuelve la entidad |
| I2 | id inexistente | `GetByIdAsync(A, id)` | `KeyNotFoundException` |
| I3 | entidad persistida en tenant B | `GetByIdAsync(A, id)` | `KeyNotFoundException` (aislamiento) |
| I4 | 25 entidades en tenant A, 5 en B | `GetAllAsync(A, 1, 10)` | solo 25, primera página de 10, total 25 |
| I5 | 3 páginas solicitadas | `GetAllAsync(A, 3, 10)` | devuelve los últimos 5 y total correcto |
| I6 | “Ventas” ya existe en A | `NameExistsAsync(A, "Ventas", null)` | `true` |
| I7 | “Ventas” existe en A, excluyendo su propio id | `NameExistsAsync(A, "Ventas", idPropio)` | `false` |
| I8 | “Ventas” existe en A, consultando B | `NameExistsAsync(B, "Ventas", null)` | `false` |
| I9 | entidad válida | `CreateAsync` | persiste y devuelve la entidad con Id |
| I10 | entidad existente | `RenameAsync(A, id, "  Nuevo  ")` | persiste “Nuevo” y lo devuelve |
| I11 | entidad existente | `DeleteAsync(A, id)` | ya no se encuentra |
| I12 | id inexistente u otro tenant | `DeleteAsync(A, id)` | `KeyNotFoundException` |

> InMemory no valida índices únicos. El duplicado se testea en Application.

### A.3 Application — casos base

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| A1 | DTO válido, tenant A, nombre no duplicado | `Create` | respuesta mapeada; repositorio crea una vez con tenant A y nombre trimmeado |
| A2 | DTO válido, nombre ya existente | `Create` | `DuplicateEntityNameException`; no se crea |
| A3 | DTO válido, `ITenantContext` nulo | `Create` | `UnauthorizedAccessException`; no se toca el repositorio |
| A4 | nombre vacío o demasiado largo | `Create` | `ValidationException`; no se toca el repositorio |
| A5 | entidad existente, DTO válido, sin duplicado | `Update` | respuesta mapeada; rename con tenant/id/nombre trimmeado |
| A6 | entidad existente, nombre de otra entidad | `Update` | `DuplicateEntityNameException` |
| A7 | id inexistente | `Update` | `KeyNotFoundException` |
| A8 | entidad existente, sin referencias | `Delete` | repositorio borra una vez |
| A9 | entidad referenciada por un job | `Delete` | `EntityInUseException`; no se borra |
| A10 | id inexistente | `Delete` | `KeyNotFoundException` |
| A11 | entidad existente | `GetById` | respuesta mapeada |
| A12 | id inexistente | `GetById` | `KeyNotFoundException` |
| A13 | tenant A, página 2, tamaño 10 | `GetAll` | repositorio recibe 2 y 10; `PagedResult` mapeado con total correcto |
| A14 | página 0 o tamaño 0/101 | `GetAll` | `ValidationException` |

### A.4 Api — casos base

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| P1 | sin token | cualquier endpoint | 401 |
| P2 | token `Employee` | cualquier endpoint | 403 |
| P3 | token `Recruiter` | GET lista / GET por id | 200 |
| P4 | token `Recruiter` | POST / PUT / DELETE | 403 |
| P5 | token `Admin` | POST con body válido | 201 + `Location` + body |
| P6 | token `Admin` | PUT con body válido | 200 + body actualizado |
| P7 | token `Admin` | DELETE existente | 204 |
| P8 | token `Admin` | POST con nombre duplicado | 409 |
| P9 | token `Admin` | DELETE referenciado | 409 |
| P10 | token `Admin` | POST/PUT con body inválido | 400 con errores por campo |
| P11 | token `Admin` | GET por id inexistente u otro tenant | 404 |
| P12 | token `Admin` | GET lista | 200 con paginación y total |

### A.5 Variaciones por entidad

| Entidad | Longitud máx. nombre | Campos extra | Reglas específicas |
|---|---|---|---|
| `JobStatus` | 100 | — | Catálogo simple. |
| `Area` | 100 | — | Catálogo simple. |
| `SubArea` | 100 | `AreaId` | Debe existir el área en el tenant. `MoveToArea` valida `AreaId` no vacío. Listado filtra por `areaId`. |
| `Client` | 150 | `ClientType` (`Internal`/`External`) | Enum obligatorio y definido. `Update` cambia nombre y tipo. En API viaja como texto. |
| `ProcessStep` | 100 | — | Catálogo simple. |
| `SearchReason` | 150 | — | Catálogo simple. |
| `CandidateProcessStatus` | 100 | `IsInitial` (si se aprueba D-02) | Un solo inicial por tenant. Si no se aprueba D-02, el estado inicial se resuelve en REC-11.3. |

### A.6 Guía técnica por capa (catálogo)

**Domain**

1. Entidad con constructor público que valida y método `Rename` (y `Update`/`MoveToArea` si aplica).
2. Constructor privado vacío para EF.
3. Id generado en el constructor; `TenantId` obligatorio; nombre trimmeado y con máximo.
4. No referenciar `ITenantContext`, DTOs ni EF.

**Infrastructure**

1. Configuración EF: tabla, clave, `ValueGeneratedNever`, máximo de nombre, índice por `TenantId` + nombre.
2. Repositorio con `GetByIdAsync`, `GetAllAsync`, `NameExistsAsync`, `CreateAsync`, `RenameAsync`,
   `DeleteAsync` y, si se aprueba D-01, `IsInUseAsync`.
3. Todas las consultas filtran por `tenantId`. `GetByIdAsync` lanza `KeyNotFoundException`.
4. `AsNoTracking` en lecturas; `SaveChangesAsync` en escrituras.
5. Agregar el DbSet al `ApplicationDbContext` en esta subtarea (no antes).
6. Registrar la configuración por assembly (ya está configurado en `OnModelCreating`).

**Application**

1. DTOs de create/update/response.
2. Validador FluentValidation con las mismas reglas que el dominio (no confiar solo en el dominio para
   devolver 400 legible).
3. Interfaz del caso de uso + implementación con `ExecuteAsync`.
4. Leer `ITenantContext.CurrentTenantId`; si es nulo, `UnauthorizedAccessException`.
5. Validar duplicado antes de crear/renombrar.
6. En delete, si `IsInUseAsync` es true, `EntityInUseException`.
7. Mapping entidad → response y `PagedResult` → `PagedResult<Response>`.
8. Registrar el caso de uso en `MateHR.Application/Recruitment/DependencyInjection.cs`.

**Api**

1. Controller delgado, sin lógica de negocio.
2. Ruta `api/v1/recruitment/<recurso>`.
3. Policy: lectura con `RequireRecruiter`; escritura con `RequireTenantAdmin`.
4. `POST` devuelve 201 con `CreatedAtAction`; `DELETE` devuelve 204.
5. Los errores los traduce el middleware; el controller no arma respuestas de error a mano.
6. Tests de integración con `CustomWebApplicationFactory`, sembrando tenant y usuarios Admin/Recruiter/Employee.

---

## 7. Tarjetas

# REC-00 · Fundamentos del módulo

**Tipo:** Épica.
**US:** Como equipo quiero la base del módulo (enums, validaciones, excepciones, persistencia común,
DI y policies) para poder construir cada cosa sobre cimientos firmes.
**Depende de:** — (Auth/Tenants/Users ya existe).
**Alcance:** enums del módulo, guardas, excepciones, DbContext base, DI, policies y middleware.
**Fuera de alcance:** entidades de negocio (van en REC-01 a REC-11).
**Criterios de aceptación de la épica:**
- [ ] Enums con valores numéricos fijos y testeados.
- [ ] Guardas reutilizables y testeadas.
- [ ] Excepciones del módulo mapeadas a HTTP.
- [ ] `ApplicationDbContext` listo para recibir configuraciones por assembly.
- [ ] `AddRecruitment()` integrado.
- [ ] Policies `RequireTenantAdmin` y `RequireRecruiter` funcionando.
**Subtareas:** REC-00.1, REC-00.2, REC-00.3, REC-00.4.

## REC-00.1 — Domain + tests

**Estado:** Hecho. Usar como referencia, no re-trabajar.
**US:** Como equipo quiero los enums, guardas y excepciones base para que todas las entidades del
módulo validen igual.
**Guía técnica:** ya implementado en `MateHR.Domain/Recruitment/Enums`,
`MateHR.Domain/Recruitment/Common/RecruitmentGuard.cs` y
`MateHR.Domain/Recruitment/Exceptions`.
**Tests existentes:** valores numéricos de enums; trim, vacío y longitud de `RequiredName`; enum
indefinido; guid vacío; entero negativo.
**Verificación antes de seguir:**
- [ ] Los tests de `Tests/Domain/Recruitment` están verdes.
- [ ] `Country.Argentina = 1`, `Province.BuenosAires = 1`, `SeniorityLevel.Trainee = 1`,
      `ClientType.Internal = 1`.
- [ ] No se cambian valores numéricos sin migración y aviso al front (son contrato de API/DB).

## REC-00.2 — Infrastructure + tests

**Estado:** Parcial / incremental.
**US:** Como equipo quiero la persistencia base del módulo para que cada entidad se enchufe sin
duplicar configuración.
**Guía técnica (qué quiero que hagas):**
1. Mantener `ApplyConfigurationsFromAssembly` en `OnModelCreating`.
2. Agregar el DbSet de cada entidad **en la subtarea `.2` de esa entidad**, no todos de golpe.
3. Agregar `StampUpdatedAt` para cada entidad que tenga `UpdatedAt` (hoy `Tenant` y `User`; sumar
   `Job` cuando exista).
4. No crear la migración todavía: se crea una sola `AddRecruitmentModule` después de REC-10.2.
5. Verificar que `JobStatus` ya está enchufado (DbSet + configuración + repositorio).
**Plan TDD:**
1. Escribir un test de humo de Infrastructure: el modelo de `ApplicationDbContext` contiene
   `JobStatus` y su configuración aplica tabla, clave y máximo de nombre.
2. Escribir un test de `StampUpdatedAt` con una entidad que ya tenga `UpdatedAt` (por ejemplo
   `Tenant`): modificar y guardar; el valor debe actualizarse.
3. Ver rojo, implementar lo que falte, ver verde.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | contexto InMemory | inspeccionar modelo | `JobStatus` está en el modelo |
| 2 | configuración de `JobStatus` | inspeccionar metadata | tabla `JobStatuses`, nombre máx. 100, índice por tenant+nombre |
| 3 | `Tenant` existente | modificar y `SaveChangesAsync` | `UpdatedAt` cambia a un valor mayor |
| 4 | `JobStatus` válido | crear y guardar | se recupera por Id y tenant |
**Archivos:** `MateHR.Infrastructure/Persistance/ApplicationDbContext.cs`; configuraciones en
`MateHR.Infrastructure/Persistance/Configurations/Recruitment/`; tests en
`Tests/Infrastructure/Recruitment/`.
**Criterios de aceptación:**
- [ ] `JobStatus` está en el modelo y persiste.
- [ ] `StampUpdatedAt` cubre las entidades con `UpdatedAt` existentes.
- [ ] No se agregaron DbSets de entidades que todavía no existen.
- [ ] No se creó una migración prematura.
**Trampas / notas:**
- EF InMemory no valida índices ni longitudes: esos tests son de modelo, no de inserción.
- Si agregás una entidad y olvidás el DbSet, los tests de repositorio fallan; por eso el DbSet se
  agrega en la misma subtarea `.2` de la entidad.

## REC-00.3 — Application + tests

**Estado:** Hecho.
**US:** Como equipo quiero un punto único de registro de casos de uso del módulo para que la API los
resuelva sin tocar `Program.cs` por cada uno.
**Guía técnica:** `AddRecruitment()` existe y se llama desde `AddApplication()`. Cada caso de uso se
registra en `MateHR.Application/Recruitment/DependencyInjection.cs` al crear su subtarea `.3`.
**Verificación:**
- [ ] `AddApplication()` llama `AddRecruitment()`.
- [ ] `MappingConfigurationTests` pasa con los profiles de Recruitment.
- [ ] Cada caso de uso nuevo queda registrado en `AddRecruitment()`.
**Trampa:** si el caso de uso no está registrado, el test de API falla con error de DI, no con 404.

## REC-00.4 — Api + tests

**Estado:** Hecho, con una verificación pendiente (D-04).
**US:** Como equipo quiero policies y middleware de errores del módulo para que los permisos y los
códigos HTTP sean consistentes.
**Guía técnica:** policies en `MateHR.Api/Authorization/AuthPolicies.cs` y `Program.cs`; mapeo en
`MateHR.Api/Middleware/ExceptionMiddleware.cs`.
**Verificación:**
- [ ] `RequireTenantAdmin` = rol `Admin`; `Recruiter` recibe 403.
- [ ] `RequireRecruiter` = roles `Admin` y `Recruiter`; `Admin` y `Recruiter` pasan.
- [ ] `SuperAdmin` y `Employee` reciben 403 en endpoints internos de recruitment.
- [ ] `EntityInUseException`, `DuplicateEntityNameException` y `JobAlreadyClosedException` → 409.
- [ ] `UnauthorizedAccessException` → 403; `KeyNotFoundException` → 404; `ValidationException` → 400.
- [ ] Resolver D-04: definir cómo se devuelve 409 para “confirmar borrado sin solicitud”.
**Tests existentes:** `Tests/Api/Recruitment/AuthorizationPolicyTest.cs` y
`Tests/Api/Middleware/ExceptionMiddlewareTests.cs`.
**Trampa:** no mapear `InvalidOperationException` genérica a 409 sin analizar el impacto: puede tapar
errores de programación. Preferir excepciones específicas del módulo.

---

# REC-01 · Estados de job (`JobStatus`)

**Tipo:** Épica.
**US:** Como admin de tenant quiero gestionar los estados de job (Abierto, En proceso, Cerrado,
Pausado) para clasificar mis búsquedas.
**Depende de:** REC-00.
**Alcance:** CRUD completo de estados de job, tenant-scoped.
**Fuera de alcance:** lógica de transición entre estados (el job solo referencia un estado).
**Criterios de aceptación de la épica:**
- [ ] Solo `Admin` crea/edita/borra; `Admin` y `Recruiter` listan/ven.
- [ ] No se puede crear ni renombrar a un nombre duplicado en el tenant.
- [ ] No se puede borrar un estado referenciado por un job (cuando exista Job).
- [ ] Todas las consultas filtran por tenant.
**Subtareas:** REC-01.1, REC-01.2, REC-01.3, REC-01.4.

## REC-01.1 — Domain + tests

**Estado:** Hecho.
**US:** Como admin quiero que un estado de job tenga nombre válido y pertenezca a mi tenant.
**Guía técnica:** entidad `JobStatus` con Id, TenantId, Name, máximo 100, constructor que valida y
`Rename`.
**Tests existentes:** constructor válido, tenant vacío, nombre vacío/largo, rename válido e inválido.
**Verificación:**
- [ ] `JobStatus.NameMaxLength` es 100.
- [ ] `Rename` no deja el nombre en estado inválido si falla.
- [ ] No hay dependencias de EF ni de Application.

## REC-01.2 — Infrastructure + tests

**Estado:** Hecho.
**US:** Como admin quiero que mis estados se persistan y se consulten solo dentro de mi tenant.
**Guía técnica:** configuración EF, repositorio y contrato ya existen. Si se aprueba D-01, agregar
`IsInUseAsync`; si se aprueba D-05, hacer `NameExistsAsync` case-insensitive.
**Tests existentes:** get existing/unknown/other tenant, paging, duplicado, excluir propio id,
create, rename, delete, delete desconocido.
**Verificación:**
- [ ] Los tests de `JobStatusRepositoryTests` están verdes.
- [ ] `GetByIdAsync` filtra por tenant y lanza `KeyNotFoundException`.
- [ ] `NameExistsAsync` respeta `excludeId`.
- [ ] Si se aprueba D-01/D-05, agregar tests y ajustar implementación antes de REC-01.3.

## REC-01.3 — Application + tests

**Estado:** Pendiente.
**US:** Como admin de tenant quiero crear, editar, listar, ver y borrar estados de job para mantener
mi catálogo al día, y como recruiter quiero consultarlos para clasificar búsquedas.
**Guía técnica (qué quiero que hagas):**
1. Crear la carpeta `MateHR.Application/Recruitment/JobStatuses/` con
   `Commands/CreateJobStatus`, `UpdateJobStatus`, `DeleteJobStatus`,
   `Queries/GetJobStatusById`, `GetJobStatuses`, `DTOs`, `Validators`, `Interfaces`, `Mappings`.
2. Cada caso de uso implementa `ExecuteAsync` y depende de repositorio, `ITenantContext`, mapper y
   validador.
3. `Create`: validar DTO → obtener tenant (si nulo, 403) → chequear duplicado → crear → mapear.
4. `Update`: validar → tenant → obtener entidad (404 si no existe) → chequear duplicado excluyendo el
   propio id → renombrar → mapear.
5. `Delete`: tenant → obtener entidad (404) → si `IsInUseAsync` es true, `EntityInUseException` →
   borrar.
6. `GetById`: tenant → repositorio → mapear.
7. `GetAll`: validar paginación → repositorio → mapear `PagedResult`.
8. Registrar los cinco casos de uso en `Recruitment/DependencyInjection.cs`.
9. Crear el mapping entidad → response y `PagedResult` → `PagedResult<Response>`.
**Depende de:** REC-01.2 y D-01/D-05 resueltas.
**Plan TDD:**
1. Escribir los tests de la tabla y verlos fallar.
2. Implementar cada caso de uso de a uno, empezando por `Create`.
3. Al final, correr `MappingConfigurationTests` y la suite de Application.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | DTO `{Name:"Abierto"}`, tenant A, no duplicado | `CreateJobStatus` | response con Id/Name; `CreateAsync` llamado una vez con tenant A y “Abierto” |
| 2 | DTO `{Name:"  Abierto  "}`, no duplicado | `CreateJobStatus` | se crea con “Abierto” trimmeado |
| 3 | DTO válido, `NameExistsAsync` true | `CreateJobStatus` | `DuplicateEntityNameException`; no se crea |
| 4 | DTO válido, `ITenantContext` nulo | `CreateJobStatus` | `UnauthorizedAccessException`; no se toca el repositorio |
| 5 | `Name` vacío | `CreateJobStatus` | `ValidationException`; no se toca el repositorio |
| 6 | `Name` de 101 caracteres | `CreateJobStatus` | `ValidationException` |
| 7 | entidad existente, DTO `{Name:"Pausado"}`, no duplicado | `UpdateJobStatus` | response “Pausado”; rename con tenant/id correctos |
| 8 | entidad existente, nombre de otro estado | `UpdateJobStatus` | `DuplicateEntityNameException` |
| 9 | id inexistente | `UpdateJobStatus` | `KeyNotFoundException` |
| 10 | entidad existente, sin referencias | `DeleteJobStatus` | `DeleteAsync` llamado una vez |
| 11 | `IsInUseAsync` true | `DeleteJobStatus` | `EntityInUseException`; no se borra |
| 12 | id inexistente | `DeleteJobStatus` | `KeyNotFoundException` |
| 13 | entidad existente | `GetJobStatusById` | response mapeado |
| 14 | id inexistente | `GetJobStatusById` | `KeyNotFoundException` |
| 15 | tenant A, página 2, tamaño 10 | `GetJobStatuses` | repositorio recibe 2/10; `PagedResult` mapeado con total |
| 16 | página 0 o tamaño 0/101 | `GetJobStatuses` | `ValidationException` |
| 17 | dos entidades en el tenant | `GetJobStatuses` | response con dos ítems y orden estable (definir: por nombre) |
**Archivos:** `MateHR.Application/Recruitment/JobStatuses/**`,
`MateHR.Application/Recruitment/DependencyInjection.cs`,
`Tests/Application/Recruitment/JobStatuses/**`.
**Criterios de aceptación:**
- [ ] Los cinco casos de uso existen y están registrados en DI.
- [ ] Todos los tests de la tabla están verdes.
- [ ] El duplicado se chequea con nombre trimmeado y excluyendo el propio id en update.
- [ ] Delete bloquea referencias con `EntityInUseException`.
- [ ] El mapping valida con `AssertConfigurationIsValid`.
- [ ] No hay lógica de EF en Application.
**Trampas / notas:**
- No confiar en el índice único de la base para el duplicado: InMemory no lo aplica y la regla debe
  estar en Application.
- Definir el orden del listado (recomendado: por nombre ascendente) y testearlo.
- Si `NameExistsAsync` es case-insensitive (D-05), testear “Abierto” vs “abierto”.

## REC-01.4 — Api + tests

**Estado:** Pendiente.
**US:** Como admin quiero exponer el catálogo de estados por API para que el front lo gestione, y como
recruiter quiero consultarlo.
**Guía técnica (qué quiero que hagas):**
1. Crear `MateHR.Api/Controllers/v1/Recruitment/JobStatusesController.cs`.
2. Ruta base `api/v1/recruitment/job-statuses`.
3. Policy a nivel clase `RequireRecruiter` para lectura; en POST/PUT/DELETE aplicar
   `RequireTenantAdmin` por método (o al revés, pero que el resultado sea el de la tabla).
4. Inyectar solo las interfaces de casos de uso; el controller no usa repositorios.
5. `POST` → 201 con `CreatedAtAction`; `PUT` → 200; `DELETE` → 204.
6. No capturar excepciones en el controller: las traduce el middleware.
7. Agregar tests de integración con `CustomWebApplicationFactory`.
**Depende de:** REC-01.3.
**Plan TDD:**
1. Escribir los tests de integración de la tabla y verlos fallar (401/404 por falta de controller).
2. Implementar el controller mínimo.
3. Verdes; luego refactor de nombres/rutas.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | sin token | GET lista | 401 |
| 2 | token `Employee` | GET lista | 403 |
| 3 | token `Recruiter` | GET lista | 200 |
| 4 | token `Recruiter` | POST | 403 |
| 5 | token `Admin`, body válido | POST | 201 + `Location` + body |
| 6 | token `Admin`, nombre duplicado | POST | 409 |
| 7 | token `Admin`, body con nombre vacío | POST | 400 con `errors` |
| 8 | token `Recruiter`, id existente | GET por id | 200 |
| 9 | token `Admin`, id inexistente | GET por id | 404 |
| 10 | token `Admin`, id existente | PUT | 200 con nombre nuevo |
| 11 | token `Admin`, id existente | DELETE | 204 |
| 12 | token `Admin`, id inexistente | DELETE | 404 |
| 13 | token `Admin`, estado referenciado (mock/integration) | DELETE | 409 |
| 14 | token `Admin`, página 1 tamaño 10 | GET lista | 200 con total y datos |
**Archivos:** `MateHR.Api/Controllers/v1/Recruitment/JobStatusesController.cs`,
`Tests/Api/Recruitment/JobStatusesControllerTests.cs`.
**Criterios de aceptación:**
- [ ] Las cinco rutas existen con las policies correctas.
- [ ] 401/403/400/404/409/201/204 según la tabla.
- [ ] `Location` del 201 apunta al GET por id.
- [ ] El controller no tiene lógica de negocio ni acceso a `DbContext`.
- [ ] Los tests usan tokens reales y tenant sembrado.
**Trampas / notas:**
- Si ponés `RequireTenantAdmin` a nivel clase, el GET para `Recruiter` da 403; usar override por
  método o clase con `RequireRecruiter`.
- El `TenantContextMiddleware` necesita el claim de tenant en el token; sembrar usuarios con
  `tenantId` correcto.

---

# REC-02 · Áreas (`Area`)

**Tipo:** Épica.
**US:** Como admin de tenant quiero crear áreas (Tecnología, Ventas) para asignarlas a los jobs.
**Depende de:** REC-00.
**Alcance:** CRUD de áreas tenant-scoped.
**Criterios de aceptación de la épica:**
- [ ] Solo `Admin` escribe; `Admin` y `Recruiter` leen.
- [ ] Nombre único por tenant (case-insensitive si se aprueba D-05).
- [ ] No se borra un área referenciada por un job.
**Subtareas:** REC-02.1, REC-02.2, REC-02.3, REC-02.4.

## REC-02.1 — Domain + tests

**US:** Como admin quiero que un área tenga nombre válido y pertenezca a mi tenant.
**Guía técnica:** aplicar el Patrón Catálogo (Anexo A) con `Area`: Id, TenantId, Name, máximo 100,
constructor y `Rename`.
**Plan TDD:** escribir los casos D1–D7 del Anexo A en `AreaTests.cs`, ver rojo, implementar, verde.
**Archivos:** `MateHR.Domain/Recruitment/Entities/Area.cs`,
`Tests/Domain/Recruitment/Entities/AreaTests.cs`.
**Criterios de aceptación:**
- [ ] Casos D1–D7 verdes.
- [ ] Sin dependencias de EF/Application.
- [ ] `NameMaxLength = 100`.

## REC-02.2 — Infrastructure + tests

**US:** Como admin quiero que mis áreas se persistan y se consulten solo dentro de mi tenant.
**Guía técnica:** aplicar Anexo A.2: configuración EF, repositorio, interfaz y DbSet `Areas`.
Incluir `IsInUseAsync` si se aprueba D-01.
**Plan TDD:** escribir I1–I12 del Anexo A en `AreaRepositoryTests.cs`, ver rojo, implementar, verde.
**Archivos:** `AreaConfiguration.cs`, `AreaRepository.cs`, `IAreaRepository.cs`,
`ApplicationDbContext.cs`, `Tests/Infrastructure/Recruitment/Repositories/AreaRepositoryTests.cs`.
**Criterios de aceptación:**
- [ ] Casos I1–I12 verdes, incluido `GetByIdAsync_OtherTenant_ThrowsKeyNotFoundException`.
- [ ] DbSet `Areas` agregado.
- [ ] Configuración aplicada por assembly.
- [ ] `NameExistsAsync` respeta `excludeId`.

## REC-02.3 — Application + tests

**US:** Como admin quiero gestionar áreas y como recruiter consultarlas.
**Guía técnica:** aplicar Anexo A.3 en `MateHR.Application/Recruitment/Areas/`: create, update,
delete, get by id, get list, DTOs, validadores, mapping e interfaces. Registrar en DI.
**Plan TDD:** escribir A1–A14 del Anexo A en `Tests/Application/Recruitment/Areas/`, ver rojo,
implementar, verde.
**Criterios de aceptación:**
- [ ] Casos A1–A14 verdes.
- [ ] Duplicado y tenant isolation testeados.
- [ ] Delete referenciado → `EntityInUseException` (con `IsInUseAsync` mockeado).
- [ ] Mapping válido y casos de uso registrados en DI.

## REC-02.4 — Api + tests

**US:** Como admin quiero exponer áreas por API y como recruiter consultarlas.
**Guía técnica:** `AreasController` en `/api/v1/recruitment/areas` con las mismas rutas/policies de
REC-01.4. Aplicar Anexo A.4.
**Plan TDD:** escribir P1–P12 del Anexo A en `AreasControllerTests.cs`, ver rojo, implementar, verde.
**Criterios de aceptación:**
- [ ] 401/403/400/404/409/201/204 según Anexo A.4.
- [ ] `Location` correcto en 201.
- [ ] Controller delgado.

---

# REC-03 · Subáreas (`SubArea`)

**Tipo:** Épica.
**US:** Como admin de tenant quiero crear subáreas dentro de un área (Backend dentro de Tecnología)
para afinar la búsqueda.
**Depende de:** REC-00, REC-02 (valida contra `Area`).
**Alcance:** CRUD de subáreas, siempre dentro de un área del mismo tenant.
**Criterios de aceptación de la épica:**
- [ ] Toda subárea pertenece a un área existente del tenant.
- [ ] El listado se puede filtrar por área.
- [ ] No se puede crear una subárea con un área de otro tenant.
- [ ] Nombre único dentro del área (definir si es por área o por tenant; recomendado: por área).
**Subtareas:** REC-03.1, REC-03.2, REC-03.3, REC-03.4.

## REC-03.1 — Domain + tests

**US:** Como admin quiero que una subárea tenga nombre, área y tenant válidos.
**Guía técnica:** entidad `SubArea` con Id, TenantId, AreaId, Name (máx. 100), constructor,
`Rename` y `MoveToArea`. Validar tenant y área no vacíos; nombre con guarda.
**Plan TDD:** escribir los casos y ver rojo.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | tenant/área válidos, nombre “Backend” | construir | Id no vacío, AreaId correcto, Name “Backend” |
| 2 | tenant vacío | construir | `ArgumentException` |
| 3 | areaId vacío | construir | `ArgumentException` |
| 4 | nombre vacío | construir | `ArgumentException` |
| 5 | nombre 101 caracteres | construir | `ArgumentException` |
| 6 | entidad válida | `Rename("  Frontend  ")` | Name “Frontend” |
| 7 | entidad válida | `Rename("")` | `ArgumentException` y conserva nombre |
| 8 | entidad válida | `MoveToArea(areaIdValido)` | AreaId cambia |
| 9 | entidad válida | `MoveToArea(Guid.Empty)` | `ArgumentException` y conserva AreaId |
**Archivos:** `SubArea.cs`, `Tests/Domain/Recruitment/Entities/SubAreaTests.cs`.
**Criterios de aceptación:**
- [ ] Los 9 casos verdes.
- [ ] `NameMaxLength = 100`.

## REC-03.2 — Infrastructure + tests

**US:** Como admin quiero que las subáreas se persistan y solo se puedan asociar a áreas de mi tenant.
**Guía técnica:** configuración EF con FK `AreaId` → `Areas` con `Restrict`; repositorio con
`GetByIdAsync`, `GetAllAsync` (filtro por `areaId`), `NameExistsAsync`, `CreateAsync`, `RenameAsync`,
`MoveToAreaAsync`, `DeleteAsync`, `IsInUseAsync` si aplica. En `CreateAsync`, validar que el área
exista en el tenant antes de insertar.
**Plan TDD:** escribir los casos y ver rojo.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | área existente en tenant A | `CreateAsync` | persiste la subárea |
| 2 | `AreaId` inexistente | `CreateAsync` | `KeyNotFoundException` |
| 3 | área existente en tenant B, subárea de A | `CreateAsync` | `KeyNotFoundException` |
| 4 | subárea de A | `GetByIdAsync(A, id)` | devuelve |
| 5 | subárea de B | `GetByIdAsync(A, id)` | `KeyNotFoundException` |
| 6 | 10 subáreas en 3 áreas | `GetAllAsync(A, areaId)` | solo las del área pedida |
| 7 | sin filtro de área | `GetAllAsync` | todas las del tenant, paginadas |
| 8 | nombre duplicado en la misma área | `NameExistsAsync` | `true` |
| 9 | mismo nombre en otra área | `NameExistsAsync` | `false` (si la unicidad es por área) |
| 10 | subárea existente | `RenameAsync` | persiste el nuevo nombre |
| 11 | subárea existente | `MoveToAreaAsync` | persiste el nuevo `AreaId` |
| 12 | subárea existente | `DeleteAsync` | se elimina |
**Archivos:** `SubAreaConfiguration.cs`, `SubAreaRepository.cs`, `ISubAreaRepository.cs`,
`ApplicationDbContext.cs`, `Tests/Infrastructure/Recruitment/Repositories/SubAreaRepositoryTests.cs`.
**Criterios de aceptación:**
- [ ] Los 12 casos verdes.
- [ ] FK con `Restrict`.
- [ ] Toda consulta filtra por tenant.
- [ ] `CreateAsync` no permite área de otro tenant.

## REC-03.3 — Application + tests

**US:** Como admin quiero gestionar subáreas y como recruiter consultarlas por área.
**Guía técnica:** casos de uso create/update/delete/get by id/get list en
`MateHR.Application/Recruitment/SubAreas/`. Regla extra: validar que `AreaId` exista en el tenant
usando `IAreaRepository`; si no, `KeyNotFoundException`. El listado debe aceptar `areaId` opcional.
**Plan TDD:** escribir los casos y ver rojo.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | DTO válido, área existente, sin duplicado | `Create` | crea con `AreaId` correcto; `CreateAsync` una vez |
| 2 | `AreaId` inexistente | `Create` | `KeyNotFoundException`; no crea |
| 3 | `AreaId` de otro tenant | `Create` | `KeyNotFoundException`; no crea |
| 4 | nombre duplicado en el área | `Create` | `DuplicateEntityNameException` |
| 5 | tenant nulo | `Create` | `UnauthorizedAccessException` |
| 6 | DTO inválido (nombre vacío/área vacía) | `Create` | `ValidationException` |
| 7 | subárea existente, DTO válido | `Update` | renombra y/o mueve; response actualizado |
| 8 | subárea inexistente | `Update` | `KeyNotFoundException` |
| 9 | subárea sin referencias | `Delete` | borra una vez |
| 10 | subárea referenciada | `Delete` | `EntityInUseException` |
| 11 | tenant A, `areaId` X | `GetAll` | repositorio recibe `areaId` X y paginación |
| 12 | id existente | `GetById` | response mapeado |
| 13 | id inexistente | `GetById` | `KeyNotFoundException` |
**Archivos:** `MateHR.Application/Recruitment/SubAreas/**`,
`Tests/Application/Recruitment/SubAreas/**`, `Recruitment/DependencyInjection.cs`.
**Criterios de aceptación:**
- [ ] Los 13 casos verdes.
- [ ] Valida área existente en create y en move.
- [ ] Listado filtra por `areaId`.
- [ ] Registrado en DI y mapping válido.

## REC-03.4 — Api + tests

**US:** Como admin quiero exponer subáreas por API y como recruiter consultarlas por área.
**Guía técnica:** `SubAreasController` en `/api/v1/recruitment/sub-areas`, con `GET ?areaId=`.
Mismas policies que catálogos: lectura `RequireRecruiter`, escritura `RequireTenantAdmin`.
**Plan TDD:** escribir los casos y ver rojo.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | sin token | GET | 401 |
| 2 | token `Employee` | GET | 403 |
| 3 | token `Recruiter` | GET ?areaId=X | 200 solo de esa área |
| 4 | token `Admin`, body válido y área existente | POST | 201 + Location |
| 5 | token `Admin`, área inexistente | POST | 404 |
| 6 | token `Admin`, body inválido | POST | 400 |
| 7 | token `Admin`, duplicado en el área | POST | 409 |
| 8 | token `Recruiter` | POST | 403 |
| 9 | token `Admin` | DELETE existente | 204 |
| 10 | token `Admin` | DELETE referenciada | 409 |
**Archivos:** `SubAreasController.cs`, `Tests/Api/Recruitment/SubAreasControllerTests.cs`.
**Criterios de aceptación:**
- [ ] Los 10 casos verdes.
- [ ] `GET ?areaId=` filtra correctamente.
- [ ] 404 para área inexistente en create.

---

# REC-04 · Clientes (`Client`)

**Tipo:** Épica.
**US:** Como admin de tenant quiero mantener mis clientes indicando si son internos o externos para
asociarlos a los jobs.
**Depende de:** REC-00.
**Alcance:** CRUD de clientes con `ClientType`.
**Criterios de aceptación de la épica:**
- [ ] `ClientType` es obligatorio y solo admite `Internal`/`External`.
- [ ] El tipo viaja como texto en la API y se guarda como entero.
- [ ] No se borra un cliente referenciado por un job.
**Subtareas:** REC-04.1, REC-04.2, REC-04.3, REC-04.4.

## REC-04.1 — Domain + tests

**US:** Como admin quiero que un cliente tenga nombre, tipo y tenant válidos.
**Guía técnica:** entidad `Client` con Id, TenantId, Name (máx. 150), `ClientType`, constructor y
`Update(name, clientType)`. Validar enum definido y nombre con guarda.
**Plan TDD:** escribir los casos y ver rojo.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | tenant válido, “Acme”, `External` | construir | campos correctos |
| 2 | tenant vacío | construir | `ArgumentException` |
| 3 | `ClientType` indefinido (por ejemplo 99) | construir | `ArgumentOutOfRangeException` |
| 4 | nombre vacío | construir | `ArgumentException` |
| 5 | nombre 151 caracteres | construir | `ArgumentException` |
| 6 | cliente válido | `Update("Nuevo", Internal)` | cambia nombre y tipo |
| 7 | cliente válido | `Update("Nuevo", (ClientType)99)` | `ArgumentOutOfRangeException`; conserva valores |
| 8 | cliente válido | `Update("", Internal)` | `ArgumentException`; conserva valores |
**Archivos:** `Client.cs`, `Tests/Domain/Recruitment/Entities/ClientTests.cs`.
**Criterios de aceptación:**
- [ ] Los 8 casos verdes.
- [ ] `NameMaxLength = 150`.
- [ ] `Update` es atómico ante error.

## REC-04.2 — Infrastructure + tests

**US:** Como admin quiero que el tipo de cliente se persista correctamente y se filtre por tenant.
**Guía técnica:** configuración EF con `ClientType` convertido a entero; repositorio e interfaz;
DbSet `Clients`; `IsInUseAsync` si aplica D-01.
**Plan TDD:** escribir los casos del Anexo A.2 más el específico de tipo; ver rojo, implementar.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | cliente `External` | `CreateAsync` y releer | `ClientType == External` |
| 2 | cliente `Internal` | `CreateAsync` y releer | `ClientType == Internal` |
| 3 | Anexo A.2 I1–I12 | — | todos verdes |
**Archivos:** `ClientConfiguration.cs`, `ClientRepository.cs`, `IClientRepository.cs`,
`ApplicationDbContext.cs`, `Tests/Infrastructure/Recruitment/Repositories/ClientRepositoryTests.cs`.
**Criterios de aceptación:**
- [ ] Anexo A.2 verde.
- [ ] `ClientType` persiste como entero.
- [ ] DbSet `Clients` agregado.

## REC-04.3 — Application + tests

**US:** Como admin quiero gestionar clientes y como recruiter consultarlos.
**Guía técnica:** aplicar Anexo A.3 en `MateHR.Application/Recruitment/Clients/`. Validar
`ClientType` con FluentValidation (`IsInEnum` o equivalente) para devolver 400 legible.
**Plan TDD:** escribir los casos y ver rojo.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | DTO válido con `ClientType=External` | `Create` | crea y response con tipo |
| 2 | `ClientType` nulo o indefinido | `Create` | `ValidationException` |
| 3 | nombre duplicado | `Create` | `DuplicateEntityNameException` |
| 4 | tenant nulo | `Create` | `UnauthorizedAccessException` |
| 5 | DTO inválido (nombre vacío) | `Create` | `ValidationException` |
| 6 | cliente existente | `Update` con nuevo nombre/tipo | response actualizado |
| 7 | cliente existente, tipo indefinido | `Update` | `ValidationException` |
| 8 | cliente existente, nombre de otro | `Update` | `DuplicateEntityNameException` |
| 9 | cliente sin referencias | `Delete` | borra |
| 10 | cliente referenciado | `Delete` | `EntityInUseException` |
| 11 | Anexo A.3 A11–A14 | get/list | verdes |
**Archivos:** `MateHR.Application/Recruitment/Clients/**`,
`Tests/Application/Recruitment/Clients/**`, `Recruitment/DependencyInjection.cs`.
**Criterios de aceptación:**
- [ ] Los 11 casos verdes.
- [ ] Enum inválido → `ValidationException`.
- [ ] Mapping válido y DI actualizada.

## REC-04.4 — Api + tests

**US:** Como admin quiero exponer clientes por API y como recruiter consultarlos.
**Guía técnica:** `ClientsController` en `/api/v1/recruitment/clients`. El `clientType` viaja como
texto (`JsonStringEnumConverter` ya configurado). Aplicar Anexo A.4.
**Plan TDD:** escribir los casos y ver rojo.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | Anexo A.4 P1–P7, P11–P12 | — | verdes |
| 2 | body `clientType:"External"` | POST Admin | 201 con `clientType:"External"` |
| 3 | body `clientType:"external"` | POST Admin | decidir: aceptar case-insensitive o 400; testear la decisión |
| 4 | body `clientType:"Otro"` | POST Admin | 400 |
| 5 | nombre duplicado | POST Admin | 409 |
| 6 | cliente referenciado | DELETE Admin | 409 |
**Archivos:** `ClientsController.cs`, `Tests/Api/Recruitment/ClientsControllerTests.cs`.
**Criterios de aceptación:**
- [ ] Anexo A.4 verde.
- [ ] Enum de texto en request/response.
- [ ] 400 para enum inválido.

---

# REC-05 · Pasos de proceso (`ProcessStep`)

**Tipo:** Épica.
**US:** Como admin de tenant quiero definir los pasos de un proceso (Screening, Entrevista técnica,
Oferta) para armar el pipeline del job.
**Depende de:** REC-00.
**Alcance:** CRUD de pasos de proceso tenant-scoped.
**Criterios de aceptación de la épica:**
- [ ] Solo `Admin` escribe; `Admin` y `Recruiter` leen.
- [ ] Nombre único por tenant.
- [ ] No se borra un paso referenciado por un job.
**Subtareas:** REC-05.1, REC-05.2, REC-05.3, REC-05.4.

## REC-05.1 — Domain + tests

**US:** Como admin quiero que un paso tenga nombre válido y pertenezca a mi tenant.
**Guía técnica:** aplicar Patrón Catálogo con `ProcessStep`, máximo 100.
**Plan TDD:** casos D1–D7 del Anexo A.
**Archivos:** `ProcessStep.cs`, `Tests/Domain/Recruitment/Entities/ProcessStepTests.cs`.
**Criterios de aceptación:**
- [ ] D1–D7 verdes.
- [ ] `NameMaxLength = 100`.

## REC-05.2 — Infrastructure + tests

**US:** Como admin quiero que los pasos se persistan y se consulten solo dentro de mi tenant.
**Guía técnica:** Anexo A.2; configuración, repositorio, interfaz, DbSet `ProcessSteps`,
`IsInUseAsync` si aplica.
**Plan TDD:** casos I1–I12.
**Archivos:** `ProcessStepConfiguration.cs`, `ProcessStepRepository.cs`,
`IProcessStepRepository.cs`, `ApplicationDbContext.cs`, tests de repositorio.
**Criterios de aceptación:**
- [ ] I1–I12 verdes.
- [ ] DbSet agregado y configuración aplicada.

## REC-05.3 — Application + tests

**US:** Como admin quiero gestionar pasos y como recruiter consultarlos.
**Guía técnica:** Anexo A.3 en `MateHR.Application/Recruitment/ProcessSteps/`; registrar en DI.
**Plan TDD:** casos A1–A14.
**Criterios de aceptación:**
- [ ] A1–A14 verdes.
- [ ] Delete referenciado → `EntityInUseException`.
- [ ] Mapping válido y DI actualizada.

## REC-05.4 — Api + tests

**US:** Como admin quiero exponer pasos por API y como recruiter consultarlos.
**Guía técnica:** `ProcessStepsController` en `/api/v1/recruitment/process-steps`; Anexo A.4.
**Plan TDD:** casos P1–P12.
**Criterios de aceptación:**
- [ ] P1–P12 verdes.
- [ ] Policies correctas y controller delgado.

---

# REC-06 · Motivos de búsqueda (`SearchReason`)

**Tipo:** Épica.
**US:** Como admin de tenant quiero catalogar el motivo de la búsqueda (Reemplazo, Aumento de equipo,
Inversión) para reportar por qué se abre un job.
**Depende de:** REC-00.
**Alcance:** CRUD de motivos tenant-scoped.
**Criterios de aceptación de la épica:**
- [ ] Solo `Admin` escribe; `Admin` y `Recruiter` leen.
- [ ] Nombre único por tenant.
- [ ] No se borra un motivo referenciado por un job.
**Subtareas:** REC-06.1, REC-06.2, REC-06.3, REC-06.4.

## REC-06.1 — Domain + tests

**US:** Como admin quiero que un motivo tenga nombre válido y pertenezca a mi tenant.
**Guía técnica:** aplicar Patrón Catálogo con `SearchReason`, máximo 150.
**Plan TDD:** casos D1–D7.
**Archivos:** `SearchReason.cs`, `Tests/Domain/Recruitment/Entities/SearchReasonTests.cs`.
**Criterios de aceptación:**
- [ ] D1–D7 verdes.
- [ ] `NameMaxLength = 150`.

## REC-06.2 — Infrastructure + tests

**US:** Como admin quiero que los motivos se persistan y se consulten solo dentro de mi tenant.
**Guía técnica:** Anexo A.2; DbSet `SearchReasons`; `IsInUseAsync` si aplica.
**Plan TDD:** casos I1–I12.
**Archivos:** `SearchReasonConfiguration.cs`, `SearchReasonRepository.cs`,
`ISearchReasonRepository.cs`, `ApplicationDbContext.cs`, tests de repositorio.
**Criterios de aceptación:**
- [ ] I1–I12 verdes.
- [ ] DbSet agregado y configuración aplicada.

## REC-06.3 — Application + tests

**US:** Como admin quiero gestionar motivos y como recruiter consultarlos.
**Guía técnica:** Anexo A.3 en `MateHR.Application/Recruitment/SearchReasons/`; registrar en DI.
**Plan TDD:** casos A1–A14.
**Criterios de aceptación:**
- [ ] A1–A14 verdes.
- [ ] Delete referenciado → `EntityInUseException`.
- [ ] Mapping válido y DI actualizada.

## REC-06.4 — Api + tests

**US:** Como admin quiero exponer motivos por API y como recruiter consultarlos.
**Guía técnica:** `SearchReasonsController` en `/api/v1/recruitment/search-reasons`; Anexo A.4.
**Plan TDD:** casos P1–P12.
**Criterios de aceptación:**
- [ ] P1–P12 verdes.
- [ ] Policies correctas y controller delgado.

---

# REC-07 · Estados de proceso de candidato (`CandidateProcessStatus`)

**Tipo:** Épica.
**US:** Como admin de tenant quiero catalogar los estados de un proceso de selección (Postulado, En
entrevista, Oferta, Contratado, Rechazado); el recruiter los usa y solo los ve.
**Depende de:** REC-00.
**Alcance:** CRUD de estados de proceso de candidato, con la decisión D-02 sobre estado inicial.
**Criterios de aceptación de la épica:**
- [ ] Solo `Admin` escribe; `Admin` y `Recruiter` leen.
- [ ] Nombre único por tenant.
- [ ] Exactamente un estado inicial por tenant (si se aprueba D-02).
- [ ] No se borra un estado referenciado por un proceso.
**Subtareas:** REC-07.1, REC-07.2, REC-07.3, REC-07.4.

## REC-07.1 — Domain + tests

**US:** Como admin quiero que un estado de proceso tenga nombre válido y, si aplica, sea el inicial.
**Guía técnica:** aplicar Patrón Catálogo con `CandidateProcessStatus`, máximo 100. Si se aprueba
D-02, agregar `IsInitial` y un método para marcarlo/desmarcarlo con regla de dominio.
**Plan TDD:** casos D1–D7 + casos de `IsInitial`.
**Casos de prueba adicionales (si D-02):**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | estado nuevo | construir con `IsInitial=true` | queda inicial |
| 2 | estado nuevo | construir con `IsInitial=false` | no inicial |
| 3 | estado no inicial | `MarkAsInitial()` | pasa a inicial |
| 4 | estado inicial | `UnmarkInitial()` | deja de ser inicial |
**Archivos:** `CandidateProcessStatus.cs`,
`Tests/Domain/Recruitment/Entities/CandidateProcessStatusTests.cs`.
**Criterios de aceptación:**
- [ ] D1–D7 verdes.
- [ ] Si D-02: regla de inicial testeada.
- [ ] `NameMaxLength = 100`.

## REC-07.2 — Infrastructure + tests

**US:** Como admin quiero que los estados se persistan y se consulten solo dentro de mi tenant.
**Guía técnica:** Anexo A.2; DbSet `CandidateProcessStatuses`; `IsInUseAsync` si aplica. Si D-02,
persistir `IsInitial` y agregar consulta `GetInitialAsync(tenantId)` o equivalente.
**Plan TDD:** casos I1–I12 + inicial.
**Casos de prueba adicionales (si D-02):**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | estado inicial en A | `GetInitialAsync(A)` | devuelve ese estado |
| 2 | sin inicial en A | `GetInitialAsync(A)` | `null` o excepción (definir y testear) |
| 3 | inicial en B | `GetInitialAsync(A)` | no lo devuelve |
**Archivos:** `CandidateProcessStatusConfiguration.cs`,
`CandidateProcessStatusRepository.cs`, `ICandidateProcessStatusRepository.cs`,
`ApplicationDbContext.cs`, tests de repositorio.
**Criterios de aceptación:**
- [ ] I1–I12 verdes.
- [ ] DbSet agregado y configuración aplicada.
- [ ] Si D-02: consulta de inicial y aislamiento por tenant.

## REC-07.3 — Application + tests

**US:** Como admin quiero gestionar estados de proceso y como recruiter consultarlos.
**Guía técnica:** Anexo A.3 en `MateHR.Application/Recruitment/CandidateProcessStatuses/`. Si D-02,
al marcar un estado como inicial, desmarcar el anterior del tenant en la misma operación.
**Plan TDD:** casos A1–A14 + inicial.
**Casos de prueba adicionales (si D-02):**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | estado A inicial, estado B no | marcar B como inicial | B inicial, A deja de serlo |
| 2 | estado ya inicial | marcar como inicial | idempotente o `InvalidOperationException` (definir) |
| 3 | estado referenciado | delete | `EntityInUseException` |
**Archivos:** `MateHR.Application/Recruitment/CandidateProcessStatuses/**`,
`Tests/Application/Recruitment/CandidateProcessStatuses/**`, `Recruitment/DependencyInjection.cs`.
**Criterios de aceptación:**
- [ ] A1–A14 verdes.
- [ ] Regla de inicial consistente.
- [ ] Mapping válido y DI actualizada.

## REC-07.4 — Api + tests

**US:** Como admin quiero exponer estados de proceso por API y como recruiter consultarlos.
**Guía técnica:** `CandidateProcessStatusesController` en
`/api/v1/recruitment/candidate-process-statuses`; Anexo A.4. Si D-02, endpoint para marcar inicial.
**Plan TDD:** casos P1–P12 + inicial.
**Archivos:** `CandidateProcessStatusesController.cs`,
`Tests/Api/Recruitment/CandidateProcessStatusesControllerTests.cs`.
**Criterios de aceptación:**
- [ ] P1–P12 verdes.
- [ ] Si D-02: marcar inicial devuelve 200 y el listado refleja el cambio.
- [ ] Policies correctas y controller delgado.

---

# REC-08 · Job/empleo

**Tipo:** Épica.
**US:** Como recruiter/admin quiero crear un job con sus datos, cliente, área/subárea, seniority,
rango salarial, motivo, observaciones y pasos ordenados; y poder consultarlo, filtrarlo por estado,
cambiar su estado/datos/notas, cerrarlo y gestionar su borrado (el recruiter solicita, el admin
confirma).
**Depende de:** REC-00 y los catálogos (REC-01 a REC-07).
**Alcance:** CRUD de Job, estados, observaciones, steps ordenados, borrado en dos pasos y cierre.
**Fuera de alcance:** procesos de selección y candidatos (REC-09/REC-10).
**Criterios de aceptación de la épica:**
- [ ] Job tenant-scoped y con todas sus referencias validadas.
- [ ] Salario máximo ≥ mínimo.
- [ ] Steps ordenados 1..n, sin duplicados.
- [ ] Cierre idempotente bloqueado: no se cierra dos veces.
- [ ] Borrado en dos pasos: recruiter solicita, admin confirma.
- [ ] Soft delete: el job confirmado queda `IsActive=false`.
**Subtareas:** REC-08.1, REC-08.2, REC-08.3, REC-08.4.

## REC-08.1 — Domain + tests

**US:** Como recruiter quiero que un job tenga datos válidos, pasos ordenados y reglas de cierre/borrado
para no dejar información inconsistente.
**Guía técnica (qué quiero que hagas):**
1. Crear `Job` y `JobStep` en `MateHR.Domain/Recruitment/Entities/`.
2. `Job`: Id, TenantId, Name (máx. 150), Seniority, AreaId, SubAreaId, SalaryMin/Max, ClientType,
   ClientName (máx. 150), JobStatusId, SearchReasonId, Observations (máx. 4000), IsActive, OpenDate,
   CloseDate, DeletionRequestedAt/By, DeletionConfirmedAt, UpdatedAt, Steps.
3. Constructor valida tenant, enums, guids, salarios, nombre y cliente; setea `IsActive=true`,
   `OpenDate=now`, `CloseDate=null`.
4. `UpdateData`, `ChangeStatus`, `UpdateObservations`, `SetSteps`, `RequestDeletion`,
   `ConfirmDeletion`, `Close`.
5. `SetSteps`: reemplaza la lista, orden 1..n, rechaza duplicados y permite lista vacía.
6. `Close`: setea `CloseDate` y `IsActive=false`; si ya está cerrado, `JobAlreadyClosedException`.
7. `JobStep`: JobId, ProcessStepId, Order > 0, guids no vacíos.
**Plan TDD:** escribir todos los casos de la tabla y verlos fallar; implementar de a uno.
**Casos de prueba — Job:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | todos los datos válidos | construir | Id no vacío, tenant, nombre trim, `IsActive=true`, `OpenDate` cercano a ahora, `CloseDate` null, sin steps |
| 2 | tenant vacío | construir | `ArgumentException` |
| 3 | seniority indefinido | construir | `ArgumentOutOfRangeException` |
| 4 | areaId vacío | construir | `ArgumentException` |
| 5 | subAreaId vacío | construir | `ArgumentException` |
| 6 | salaryMin negativo | construir | `ArgumentOutOfRangeException` |
| 7 | salaryMax negativo | construir | `ArgumentOutOfRangeException` |
| 8 | salaryMax < salaryMin | construir | `ArgumentException` |
| 9 | clientType indefinido | construir | `ArgumentOutOfRangeException` |
| 10 | clientName vacío | construir | `ArgumentException` |
| 11 | name vacío | construir | `ArgumentException` |
| 12 | observations con espacios | construir | se guardan trimmeadas |
| 13 | datos válidos | `UpdateData` con todo válido | actualiza y setea `UpdatedAt` |
| 14 | datos válidos | `UpdateData` con `salaryMax < salaryMin` | `ArgumentException`; conserva todo |
| 15 | job válido | `ChangeStatus(nuevoId)` | cambia y setea `UpdatedAt` |
| 16 | job válido | `ChangeStatus(Guid.Empty)` | `ArgumentException` |
| 17 | job válido | `UpdateObservations(null)` | Observations null y `UpdatedAt` |
| 18 | job válido | `UpdateObservations` de 4001 | `ArgumentException`; conserva anterior |
| 19 | job válido | `SetSteps([a,b,c])` | Steps con orden 1,2,3 |
| 20 | job válido | `SetSteps([a,a])` | `ArgumentException`; conserva steps anteriores |
| 21 | job válido | `SetSteps([])` | Steps vacío |
| 22 | job válido | `SetSteps(null)` | `ArgumentException` |
| 23 | job válido | `SetSteps([Guid.Empty])` | `ArgumentException` |
| 24 | job válido | `RequestDeletion(userId, now)` | setea `DeletionRequestedAt/By` |
| 25 | job ya solicitado | `RequestDeletion` de nuevo | definir: `InvalidOperationException` o idempotente; testear |
| 26 | job con solicitud | `ConfirmDeletion(now)` | `IsActive=false`, `DeletionConfirmedAt` seteado |
| 27 | job ya inactivo | `ConfirmDeletion` | `InvalidOperationException` |
| 28 | job activo | `Close(now)` | `CloseDate=now`, `IsActive=false` |
| 29 | job ya cerrado | `Close` | `JobAlreadyClosedException` |
| 30 | job borrado | `Close` | definir: `JobAlreadyClosedException` o `InvalidOperationException`; testear |
**Casos de prueba — JobStep:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | job/step válidos, orden 1 | construir | campos correctos |
| 2 | jobId vacío | construir | `ArgumentException` |
| 3 | processStepId vacío | construir | `ArgumentException` |
| 4 | orden 0 o negativo | construir | `ArgumentOutOfRangeException` |
**Archivos:** `Job.cs`, `JobStep.cs`, `Tests/Domain/Recruitment/Entities/JobTests.cs`,
`JobStepTests.cs`.
**Criterios de aceptación:**
- [ ] Todos los casos verdes.
- [ ] `SetSteps` es atómico: si falla, no deja la lista a medias.
- [ ] `Close` no se puede ejecutar dos veces.
- [ ] `RequestDeletion` no borra ni desactiva.
- [ ] `ConfirmDeletion` es soft delete.
**Trampas / notas:**
- No persistir `Steps` como lista mutable expuesta: devolver solo lectura.
- `UpdatedAt` se setea en cada mutación; no en el constructor.
- Definir y documentar el comportamiento de doble `RequestDeletion` y `Close` de job borrado.

## REC-08.2 — Infrastructure + tests

**US:** Como recruiter quiero que los jobs y sus pasos se persistan con filtros por estado/actividad y
con el borrado en dos pasos reflejado en la base.
**Guía técnica (qué quiero que hagas):**
1. Crear `JobConfiguration` y `JobStepConfiguration`.
2. Job: tabla `Jobs`, clave, `ValueGeneratedNever`, máximos, `Seniority` y `ClientType` como entero,
   índice por `TenantId` + `Name`, relación uno-a-muchos con Steps con cascada y acceso por campo.
3. JobStep: tabla `JobSteps`, clave, FK a Job, orden obligatorio.
4. Crear `IJobRepository` y `JobRepository` con: get by id (incluye Steps), get all con filtros
   `jobStatusId`/`isActive` ordenado por `OpenDate` desc, name exists, create, update, change status,
   update observations, set steps, request deletion, confirm deletion, close, is referenced by process.
5. Agregar DbSets `Jobs` y `JobSteps`.
6. Sumar `Job` a `StampUpdatedAt`.
**Plan TDD:** escribir los casos y verlos fallar; implementar por método.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | job de A | `GetByIdAsync(A, id)` | devuelve con Steps cargados |
| 2 | id inexistente | `GetByIdAsync(A, id)` | `KeyNotFoundException` |
| 3 | job de B | `GetByIdAsync(A, id)` | `KeyNotFoundException` |
| 4 | jobs con estados distintos | `GetAllAsync(A, jobStatusId: X)` | solo los de X |
| 5 | jobs activos e inactivos | `GetAllAsync(A, isActive: false)` | solo inactivos |
| 6 | jobs con fechas distintas | `GetAllAsync` | orden `OpenDate` desc |
| 7 | página 2 tamaño 5 | `GetAllAsync` | página y total correctos |
| 8 | nombre existente | `NameExistsAsync` | true |
| 9 | nombre existente excluyendo propio id | `NameExistsAsync` | false |
| 10 | job válido con steps | `CreateAsync` | persiste job y steps |
| 11 | job existente | `UpdateAsync` | persiste cambios y `UpdatedAt` |
| 12 | job existente | `ChangeStatusAsync` | persiste estado y `UpdatedAt` |
| 13 | job existente | `UpdateObservationsAsync` | persiste observación |
| 14 | job con steps viejos | `SetStepsAsync` con nuevos | reemplaza y ordena 1..n |
| 15 | job existente | `RequestDeletionAsync` | persiste solicitud; sigue activo |
| 16 | job con solicitud | `ConfirmDeletionAsync` | `IsActive=false`, confirma |
| 17 | job activo | `CloseAsync` | `CloseDate` seteado e inactivo |
| 18 | proceso existente para el job | `IsReferencedByProcessAsync` | true |
| 19 | sin procesos | `IsReferencedByProcessAsync` | false |
| 20 | job de B | cualquier operación con tenant A | `KeyNotFoundException` |
**Archivos:** `JobConfiguration.cs`, `JobStepConfiguration.cs`, `JobRepository.cs`,
`IJobRepository.cs`, `ApplicationDbContext.cs`,
`Tests/Infrastructure/Recruitment/Repositories/JobRepositoryTests.cs`.
**Criterios de aceptación:**
- [ ] Los 20 casos verdes.
- [ ] GetById incluye Steps; listado no necesariamente.
- [ ] Filtros y orden testeados.
- [ ] `UpdatedAt` se estampa en update/change status.
- [ ] `RequestDeletion` no desactiva; `ConfirmDeletion` sí.
- [ ] DbSets y `StampUpdatedAt` actualizados.
**Trampas / notas:**
- InMemory no aplica índices ni cascadas reales; testear la relación cargando Steps.
- Si `SetStepsAsync` reemplaza la colección, asegurar que EF elimine los JobStep viejos.
- `IsReferencedByProcessAsync` depende de `SelectionProcesses`, que existe en REC-10. Si se adelanta
  REC-08, devolver false y completar el test cuando exista la entidad.

## REC-08.3 — Application + tests

**US:** Como recruiter quiero crear/editar jobs, cambiar estado/observaciones, ordenar pasos, solicitar
borrado y cerrar; como admin quiero confirmar el borrado.
**Guía técnica (qué quiero que hagas):**
1. Crear `MateHR.Application/Recruitment/Jobs/` con commands:
   `CreateJob`, `UpdateJob`, `ChangeJobStatus`, `UpdateJobObservations`, `SetJobSteps`,
   `RequestJobDeletion`, `ConfirmJobDeletion`, `CloseJob`; queries `GetJobById`, `GetJobs`.
2. DTOs: create/update/status/observations/steps/request-deletion/response/summary/request de listado.
3. Validadores: name, seniority, area/subarea, salarios, clientType, clientName, observations,
   page/pageSize, lista de steps.
4. `CreateJob`: validar DTO → tenant → validar área y subárea (misma área y mismo tenant) → validar
   steps (si D-07) → duplicado → crear → set steps → mapear.
5. `UpdateJob`: validar → tenant → obtener job → validar referencias → duplicado excluyendo id →
   actualizar datos → mapear.
6. `ChangeJobStatus`: validar estado existente en tenant → cambiar.
7. `UpdateJobObservations`: validar longitud → actualizar.
8. `SetJobSteps`: validar duplicados y existencia de steps → reemplazar.
9. `RequestJobDeletion`: obtener usuario actual (D-03) → solicitar.
10. `ConfirmJobDeletion`: si no hay solicitud previa, excepción específica (D-04) → confirmar.
11. `CloseJob`: cerrar; si ya cerrado, 409.
12. `GetJobById`/`GetJobs`: tenant → repo → mapping.
13. Registrar los 10 casos de uso en DI y crear `JobMapping`.
**Depende de:** REC-08.2, D-03, D-04, D-07.
**Plan TDD:** escribir los casos y verlos fallar; implementar command por command.
**Casos de prueba — CreateJob:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | DTO válido, área/subárea/estado/motivo existentes, steps válidos | `CreateJob` | response; job creado con `IsActive=true`, `OpenDate`, steps ordenados |
| 2 | `AreaId` inexistente | `CreateJob` | `KeyNotFoundException` |
| 3 | `SubAreaId` inexistente | `CreateJob` | `KeyNotFoundException` |
| 4 | subárea de otra área | `CreateJob` | `ValidationException` (o `KeyNotFoundException`; definir) |
| 5 | subárea de otro tenant | `CreateJob` | `KeyNotFoundException` |
| 6 | nombre duplicado en tenant | `CreateJob` | `DuplicateEntityNameException` |
| 7 | `SalaryMax < SalaryMin` | `CreateJob` | `ValidationException` |
| 8 | seniority/clientType indefinidos | `CreateJob` | `ValidationException` |
| 9 | tenant nulo | `CreateJob` | `UnauthorizedAccessException` |
| 10 | steps duplicados | `CreateJob` | `ValidationException` |
| 11 | step inexistente (si D-07) | `CreateJob` | `KeyNotFoundException` |
| 12 | DTO inválido (nombre vacío, salario negativo) | `CreateJob` | `ValidationException`; no crea |
**Casos de prueba — GetJobs / GetJobById:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | jobs en tenant A con distintos estados | `GetJobs` con `jobStatusId` | solo los del estado; `PagedResult` mapeado |
| 2 | jobs activos/inactivos | `GetJobs` con `isActive` | filtra correctamente |
| 3 | página 2 tamaño 5 | `GetJobs` | repo recibe 2/5 y total correcto |
| 4 | pageSize 101 o page 0 | `GetJobs` | `ValidationException` |
| 5 | job existente con steps | `GetJobById` | response con steps ordenados |
| 6 | id inexistente u otro tenant | `GetJobById` | `KeyNotFoundException` |
**Casos de prueba — UpdateJob / ChangeJobStatus / UpdateJobObservations / SetJobSteps:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | job existente, datos válidos | `UpdateJob` | actualiza y response |
| 2 | job inexistente | `UpdateJob` | `KeyNotFoundException` |
| 3 | nombre de otro job | `UpdateJob` | `DuplicateEntityNameException` |
| 4 | subárea de otra área | `UpdateJob` | `ValidationException` |
| 5 | job existente, estado existente | `ChangeJobStatus` | cambia estado |
| 6 | estado inexistente | `ChangeJobStatus` | `KeyNotFoundException` |
| 7 | job inexistente | `ChangeJobStatus` | `KeyNotFoundException` |
| 8 | observación válida | `UpdateJobObservations` | actualiza |
| 9 | observación 4001 | `UpdateJobObservations` | `ValidationException` |
| 10 | steps válidos | `SetJobSteps` | reemplaza y ordena |
| 11 | steps duplicados | `SetJobSteps` | `ValidationException` |
| 12 | step inexistente (si D-07) | `SetJobSteps` | `KeyNotFoundException` |
| 13 | job inexistente | `SetJobSteps` | `KeyNotFoundException` |
**Casos de prueba — RequestJobDeletion / ConfirmJobDeletion / CloseJob:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | job activo, usuario actual | `RequestJobDeletion` | setea `DeletionRequestedAt/By`; sigue activo |
| 2 | job sin solicitud | `ConfirmJobDeletion` | excepción específica (D-04) → 409 |
| 3 | job con solicitud | `ConfirmJobDeletion` | `IsActive=false`, confirma |
| 4 | job inexistente | `RequestJobDeletion`/`ConfirmJobDeletion` | `KeyNotFoundException` |
| 5 | job activo | `CloseJob` | `CloseDate` seteado, inactivo |
| 6 | job ya cerrado | `CloseJob` | `JobAlreadyClosedException` |
| 7 | job inexistente | `CloseJob` | `KeyNotFoundException` |
**Archivos:** `MateHR.Application/Recruitment/Jobs/**`,
`Tests/Application/Recruitment/Jobs/**`, `Recruitment/DependencyInjection.cs`.
**Criterios de aceptación:**
- [ ] Los 10 casos de uso existen y están registrados en DI.
- [ ] Todos los casos verdes.
- [ ] Validación de área/subárea y steps.
- [ ] `ConfirmJobDeletion` sin solicitud devuelve 409 (no 500).
- [ ] `CloseJob` no permite doble cierre.
- [ ] Mapping válido.

## REC-08.4 — Api + tests

**US:** Como recruiter quiero operar jobs por API y como admin confirmar borrados.
**Guía técnica (qué quiero que hagas):**
1. Crear `JobsController` en `/api/v1/recruitment/jobs`.
2. Rutas y policies de la tabla.
3. Inyectar solo casos de uso.
4. `POST` 201 con Location; `GET` 200; `PUT/PATCH` 200; `POST deletion-request` 200;
   `POST deletion-confirm` 200; `POST close` 200.
5. No capturar excepciones.
**Depende de:** REC-08.3.
**Plan TDD:** escribir los casos y verlos fallar; implementar endpoint por endpoint.
**Rutas y policies:**

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
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | sin token | cualquier endpoint | 401 |
| 2 | token `Employee` | cualquier endpoint | 403 |
| 3 | token `Recruiter`, body válido | POST | 201 + Location |
| 4 | token `Recruiter`, body inválido | POST | 400 |
| 5 | token `Recruiter`, nombre duplicado | POST | 409 |
| 6 | token `Recruiter`, filtro estado | GET lista | 200 solo coincidentes |
| 7 | token `Recruiter` | GET por id | 200 |
| 8 | token `Recruiter`, id inexistente | GET por id | 404 |
| 9 | token `Recruiter` | PUT datos | 200 |
| 10 | token `Recruiter` | PATCH status | 200 con nuevo estado |
| 11 | token `Recruiter` | PATCH observations | 200 |
| 12 | token `Recruiter` | PUT steps | 403 |
| 13 | token `Admin` | PUT steps | 200 |
| 14 | token `Recruiter` | POST deletion-request | 200 |
| 15 | token `Recruiter` | POST deletion-confirm | 403 |
| 16 | token `Admin` | POST deletion-confirm con solicitud | 200 |
| 17 | token `Admin` | POST deletion-confirm sin solicitud | 409 |
| 18 | token `Recruiter` | POST close | 200 con `CloseDate` |
| 19 | token `Recruiter` | POST close ya cerrado | 409 |
**Archivos:** `JobsController.cs`, `Tests/Api/Recruitment/JobsControllerTests.cs`.
**Criterios de aceptación:**
- [ ] Las 10 rutas existen con las policies de la tabla.
- [ ] Los 19 casos verdes.
- [ ] `Location` correcto en 201.
- [ ] `deletion-confirm` con `Recruiter` da 403; con `Admin` funciona.
- [ ] Controller delgado.

---

# REC-09 · Candidatos

**Tipo:** Épica.
**US:** Como recruiter/admin quiero crear, editar, listar/ver y eliminar candidatos (datos personales,
contacto, ubicación, comentarios) viendo el historial de procesos en los que participó.
**Depende de:** REC-00, REC-10 (para el historial).
**Alcance:** CRUD de candidatos, búsqueda y `ProcessHistory` derivado.
**Fuera de alcance:** alta pública (REC-11).
**Criterios de aceptación de la épica:**
- [ ] Email único por tenant, normalizado y case-insensitive.
- [ ] DNI válido; teléfono string ≤30; enums de provincia/país válidos.
- [ ] `LastActivity` se actualiza en cada mutación.
- [ ] No se borra un candidato con procesos.
- [ ] El historial no se persiste: se arma desde `SelectionProcess`.
**Subtareas:** REC-09.1, REC-09.2, REC-09.3, REC-09.4.

## REC-09.1 — Domain + tests

**US:** Como recruiter quiero que un candidato tenga datos personales y de contacto válidos y que
registre su última actividad.
**Guía técnica (qué quiero que hagas):**
1. Crear `Candidate` con los campos del backlog: nombres, apellido, DNI, teléfono, email, LinkedIn,
   provincia, país, ubicación, comentarios, fechas y flag de externo.
2. Normalizar email (trim + minúsculas + validación de formato).
3. Validar DNI (7-8 dígitos, > 0), provincia/país definidos, longitudes máximas.
4. `CreationDate = now`, `LastActivity = CreationDate`, `IsExternallyCreated` según parámetro.
5. Métodos `Update`, `UpdateComments`, `RecordActivity`.
6. `RecordActivity` no puede ser anterior a `CreationDate`.
**Plan TDD:** escribir los casos y verlos fallar; implementar de a uno.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | todos los datos válidos | construir | Id no vacío, `LastActivity == CreationDate`, flag correcto |
| 2 | email `"  JUAN@MAIL.COM  "` | construir | se guarda `"juan@mail.com"` |
| 3 | email inválido | construir | `ArgumentException` |
| 4 | DNI 0 o negativo | construir | `ArgumentException` |
| 5 | DNI 6 dígitos | construir | `ArgumentException` (si D-06) |
| 6 | DNI 9 dígitos | construir | `ArgumentException` (si D-06) |
| 7 | DNI 7 u 8 dígitos | construir | se acepta |
| 8 | provincia indefinida | construir | `ArgumentOutOfRangeException` |
| 9 | país indefinido | construir | `ArgumentOutOfRangeException` |
| 10 | `isExternallyCreated=true` | construir | flag true |
| 11 | `isExternallyCreated=false` | construir | flag false |
| 12 | candidato válido, momento posterior | `RecordActivity` | `LastActivity` cambia |
| 13 | candidato válido, momento anterior a creación | `RecordActivity` | `ArgumentException`; conserva `LastActivity` |
| 14 | candidato válido | `Update` con datos válidos | actualiza y toca `LastActivity` |
| 15 | candidato válido | `UpdateComments` válido | actualiza y toca `LastActivity` |
| 16 | comentario de 4001 | `UpdateComments` | `ArgumentException`; conserva anterior |
| 17 | nombre vacío o 101 caracteres | construir | `ArgumentException` |
| 18 | teléfono de 31 caracteres | construir | `ArgumentException` |
| 19 | LinkedIn inválido (si D-06) | construir | `ArgumentException` |
**Archivos:** `Candidate.cs`, `Tests/Domain/Recruitment/Entities/CandidateTests.cs`.
**Criterios de aceptación:**
- [ ] Todos los casos verdes.
- [ ] Email normalizado y validado.
- [ ] DNI y enums validados.
- [ ] `LastActivity` coherente.
- [ ] `ProcessHistory` no existe en la entidad persistida.

## REC-09.2 — Infrastructure + tests

**US:** Como recruiter quiero buscar candidatos por email/nombre y ver solo los de mi tenant.
**Guía técnica (qué quiero que hagas):**
1. `CandidateConfiguration`: tabla, clave, máximos, email con índice por tenant, enums como entero.
2. `ICandidateRepository` con: get by id, find by email (nullable), get all con búsqueda, create,
   update, delete, has processes.
3. `FindByEmailAsync` normaliza el email y compara case-insensitive.
4. `GetAllAsync` busca por nombre, apellido o email; ordena por `LastActivity` desc.
5. `HasProcessesAsync` consulta `SelectionProcesses` (si existe; si no, false y completar en REC-10).
6. DbSet `Candidates` y `UpdatedAt` si aplica.
**Plan TDD:** escribir los casos y verlos fallar; implementar por método.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | candidato con email | `FindByEmailAsync` | lo devuelve |
| 2 | email en mayúsculas | `FindByEmailAsync` con minúsculas | lo devuelve |
| 3 | email desconocido | `FindByEmailAsync` | `null` |
| 4 | email de otro tenant | `FindByEmailAsync` | `null` |
| 5 | candidatos “Juan Pérez”, “Ana Gómez” | `GetAllAsync(search:"juan")` | solo Juan |
| 6 | búsqueda por email | `GetAllAsync(search:"mail.com")` | coincidencias |
| 7 | fechas de actividad distintas | `GetAllAsync` | orden `LastActivity` desc |
| 8 | página 2 tamaño 5 | `GetAllAsync` | página y total correctos |
| 9 | candidato con proceso | `HasProcessesAsync` | true |
| 10 | candidato sin proceso | `HasProcessesAsync` | false |
| 11 | candidato de A | `GetByIdAsync(A, id)` | devuelve |
| 12 | candidato de B | `GetByIdAsync(A, id)` | `KeyNotFoundException` |
| 13 | candidato válido | `CreateAsync` | persiste |
| 14 | candidato existente | `UpdateAsync` | persiste cambios |
| 15 | candidato sin procesos | `DeleteAsync` | elimina |
| 16 | id inexistente | `DeleteAsync` | `KeyNotFoundException` |
**Archivos:** `CandidateConfiguration.cs`, `CandidateRepository.cs`, `ICandidateRepository.cs`,
`ApplicationDbContext.cs`, `Tests/Infrastructure/Recruitment/Repositories/CandidateRepositoryTests.cs`.
**Criterios de aceptación:**
- [ ] Los 16 casos verdes.
- [ ] Email case-insensitive y tenant-scoped.
- [ ] Búsqueda y orden testeados.
- [ ] DbSet agregado.

## REC-09.3 — Application + tests

**US:** Como recruiter quiero gestionar candidatos, buscarlos y ver su historial de procesos.
**Guía técnica (qué quiero que hagas):**
1. Crear `MateHR.Application/Recruitment/Candidates/` con commands `CreateCandidate`,
   `UpdateCandidate`, `DeleteCandidate`; queries `GetCandidateById`, `GetCandidates`.
2. DTOs: create/update/response/summary/request de listado.
3. Validadores: nombres, email, DNI, teléfono, provincia/país, longitudes.
4. `Create`: validar → tenant → email duplicado → crear con `isExternallyCreated=false` → mapear.
5. `Update`: validar → tenant → obtener → email duplicado excluyendo id → actualizar → mapear.
6. `Delete`: tenant → obtener → si `HasProcessesAsync` true, `EntityInUseException` → borrar.
7. `GetCandidateById`: tenant → candidato → `ISelectionProcessRepository.GetByCandidateAsync` →
   armar `ProcessHistory` en el response.
8. `GetCandidates`: tenant → búsqueda/paginación → mapear.
9. Registrar en DI y crear `CandidateMapping`.
**Depende de:** REC-09.2, REC-10.2 (para historial).
**Plan TDD:** escribir los casos y verlos fallar; implementar command por command.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | DTO válido, email no duplicado | `Create` | response; candidato con `isExternallyCreated=false` |
| 2 | email duplicado en el tenant | `Create` | `DuplicateEntityNameException` o `EmailAlreadyExistsException` (elegir) |
| 3 | email duplicado en otro tenant | `Create` | se permite |
| 4 | tenant nulo | `Create` | `UnauthorizedAccessException` |
| 5 | DTO inválido (email/DNI) | `Create` | `ValidationException` |
| 6 | candidato existente, datos válidos | `Update` | actualiza y toca `LastActivity` |
| 7 | candidato inexistente | `Update` | `KeyNotFoundException` |
| 8 | email de otro candidato | `Update` | excepción de duplicado |
| 9 | candidato sin procesos | `Delete` | borra |
| 10 | candidato con procesos | `Delete` | `EntityInUseException` |
| 11 | candidato con dos procesos | `GetCandidateById` | `ProcessHistory` con dos resúmenes mapeados |
| 12 | candidato sin procesos | `GetCandidateById` | `ProcessHistory` vacío |
| 13 | id inexistente u otro tenant | `GetCandidateById` | `KeyNotFoundException` |
| 14 | búsqueda “juan”, página 2 | `GetCandidates` | repo recibe búsqueda/paginación; `PagedResult` mapeado |
| 15 | pageSize 101 | `GetCandidates` | `ValidationException` |
**Archivos:** `MateHR.Application/Recruitment/Candidates/**`,
`Tests/Application/Recruitment/Candidates/**`, `Recruitment/DependencyInjection.cs`.
**Criterios de aceptación:**
- [ ] Los 15 casos verdes.
- [ ] Email único case-insensitive.
- [ ] Delete bloqueado por procesos.
- [ ] `ProcessHistory` se arma desde el repo de procesos, no se persiste.
- [ ] Mapping válido y DI actualizada.

## REC-09.4 — Api + tests

**US:** Como recruiter quiero operar candidatos por API y ver su historial.
**Guía técnica:** `CandidatesController` en `/api/v1/recruitment/candidates`. `GET` y `GET/{id}` con
`RequireRecruiter`; `POST`/`PUT`/`DELETE` con `RequireRecruiter` (según backlog).
**Plan TDD:** escribir los casos y verlos fallar; implementar endpoint por endpoint.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | sin token | cualquier endpoint | 401 |
| 2 | token `Employee` | cualquier endpoint | 403 |
| 3 | token `Recruiter`, body válido | POST | 201 + Location |
| 4 | token `Recruiter`, email duplicado | POST | 409 |
| 5 | token `Recruiter`, body inválido | POST | 400 |
| 6 | token `Recruiter` | GET lista con `search` | 200 filtrado |
| 7 | token `Recruiter` | GET por id con procesos | 200 con `processHistory` |
| 8 | token `Recruiter` | GET por id inexistente | 404 |
| 9 | token `Recruiter` | PUT datos | 200 |
| 10 | token `Recruiter` | DELETE con procesos | 409 |
| 11 | token `Recruiter` | DELETE sin procesos | 204 |
**Archivos:** `CandidatesController.cs`, `Tests/Api/Recruitment/CandidatesControllerTests.cs`.
**Criterios de aceptación:**
- [ ] Los 11 casos verdes.
- [ ] 409 por email duplicado y por delete con procesos.
- [ ] `processHistory` presente en el detalle.
- [ ] Controller delgado.

---

# REC-10 · Procesos de selección (`SelectionProcess`)

**Tipo:** Épica.
**US:** Como recruiter/admin quiero registrar que un candidato participa de un job, con su estado,
observación y última actualización, y poder listarlos/verlos/eliminarlos.
**Depende de:** REC-00, REC-02 a REC-07, REC-08, REC-09.
**Alcance:** CRUD de procesos de selección y consultas por job/candidato.
**Criterios de aceptación de la épica:**
- [ ] Job, candidato y estado deben existir en el tenant.
- [ ] `LastUpdate` se actualiza al cambiar estado u observación.
- [ ] No se permite duplicar el mismo candidato+job (si se aprueba D-09).
- [ ] El historial de candidato se puede armar desde acá.
**Subtareas:** REC-10.1, REC-10.2, REC-10.3, REC-10.4.

## REC-10.1 — Domain + tests

**US:** Como recruiter quiero que un proceso tenga job, candidato, estado y observación válidos, y que
registre su última actualización.
**Guía técnica:** entidad `SelectionProcess` con Id, TenantId, JobId, CandidateId, StatusId,
Observation (máx. 4000), LastUpdate. Constructor valida guids y setea `LastUpdate=now`.
Métodos `ChangeStatus(statusId, moment)` y `UpdateObservation(observation, moment)`.
**Plan TDD:** escribir los casos y verlos fallar.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | job/candidato/estado válidos | construir | Id no vacío, `LastUpdate` cercano a ahora |
| 2 | tenant vacío | construir | `ArgumentException` |
| 3 | job vacío | construir | `ArgumentException` |
| 4 | candidato vacío | construir | `ArgumentException` |
| 5 | estado vacío | construir | `ArgumentException` |
| 6 | observación válida | construir | se guarda trimmeada |
| 7 | observación 4001 | construir | `ArgumentException` |
| 8 | proceso válido | `ChangeStatus(nuevo, momento)` | cambia estado y `LastUpdate` |
| 9 | proceso válido | `ChangeStatus(Guid.Empty, momento)` | `ArgumentException` |
| 10 | proceso válido | `UpdateObservation("  ok  ", momento)` | observación “ok” y `LastUpdate` |
| 11 | proceso válido | `UpdateObservation` 4001 | `ArgumentException`; conserva anterior |
**Archivos:** `SelectionProcess.cs`,
`Tests/Domain/Recruitment/Entities/SelectionProcessTests.cs`.
**Criterios de aceptación:**
- [ ] Los 11 casos verdes.
- [ ] `LastUpdate` se toca en cada mutación.
- [ ] `ObservationMaxLength = 4000`.

## REC-10.2 — Infrastructure + tests

**US:** Como recruiter quiero consultar procesos por job/candidato y persistir cambios de estado.
**Guía técnica (qué quiero que hagas):**
1. `SelectionProcessConfiguration`: tabla, clave, máximos, enums si aplica, índices por tenant+job y
   tenant+candidato. Si D-09, índice único por tenant+job+candidato.
2. `ISelectionProcessRepository` con: get by id, get all con filtros `jobId`/`candidateId`, get by
   candidate (para historial), create, update, delete, exists by job+candidate (D-09).
3. `GetByCandidateAsync` ordena por `LastUpdate` desc.
4. DbSet `SelectionProcesses`.
**Plan TDD:** escribir los casos y verlos fallar.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | proceso de A | `GetByIdAsync(A, id)` | devuelve |
| 2 | proceso de B | `GetByIdAsync(A, id)` | `KeyNotFoundException` |
| 3 | procesos de dos jobs | `GetAllAsync(A, jobId: X)` | solo los del job |
| 4 | procesos de dos candidatos | `GetAllAsync(A, candidateId: Y)` | solo los del candidato |
| 5 | procesos con fechas distintas | `GetAllAsync` | orden estable (definir) |
| 6 | candidato con dos procesos | `GetByCandidateAsync` | dos, orden `LastUpdate` desc |
| 7 | proceso válido | `CreateAsync` | persiste |
| 8 | proceso existente | `UpdateAsync` | persiste y estampa `LastUpdate` |
| 9 | proceso existente | `DeleteAsync` | elimina |
| 10 | id inexistente | `DeleteAsync` | `KeyNotFoundException` |
| 11 | mismo job+candidato ya existe (D-09) | `ExistsAsync` | true |
| 12 | distinto job o candidato (D-09) | `ExistsAsync` | false |
**Archivos:** `SelectionProcessConfiguration.cs`, `SelectionProcessRepository.cs`,
`ISelectionProcessRepository.cs`, `ApplicationDbContext.cs`,
`Tests/Infrastructure/Recruitment/Repositories/SelectionProcessRepositoryTests.cs`.
**Criterios de aceptación:**
- [ ] Los 12 casos verdes.
- [ ] Filtros y orden de historial testeados.
- [ ] Tenant isolation.
- [ ] Si D-09: chequeo de duplicado e índice.

## REC-10.3 — Application + tests

**US:** Como recruiter quiero crear/actualizar/eliminar procesos y listarlos por job/candidato.
**Guía técnica (qué quiero que hagas):**
1. Crear `MateHR.Application/Recruitment/SelectionProcesses/` con commands
   `CreateSelectionProcess`, `UpdateSelectionProcess` (estado + observación),
   `DeleteSelectionProcess`; queries `GetSelectionProcessById`, `GetSelectionProcesses`.
2. DTOs, validadores, interfaces, mapping.
3. `Create`: validar → tenant → job existe → candidato existe → estado existe → si D-09 y ya existe,
   `DuplicateEntityNameException` o excepción específica → crear.
4. `Update`: obtener → validar estado → cambiar estado/observación → mapear.
5. `Delete`: obtener → borrar.
6. `Get...`: tenant → repo → mapping.
7. Registrar en DI.
**Depende de:** REC-10.2, D-09.
**Plan TDD:** escribir los casos y verlos fallar.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | job/candidato/estado existentes | `Create` | crea; response con `LastUpdate` |
| 2 | job inexistente | `Create` | `KeyNotFoundException` |
| 3 | candidato inexistente | `Create` | `KeyNotFoundException` |
| 4 | estado inexistente | `Create` | `KeyNotFoundException` |
| 5 | job/candidato de otro tenant | `Create` | `KeyNotFoundException` |
| 6 | tenant nulo | `Create` | `UnauthorizedAccessException` |
| 7 | DTO inválido | `Create` | `ValidationException` |
| 8 | mismo job+candidato existente (D-09) | `Create` | 409 (excepción definida) |
| 9 | proceso existente | `Update` estado | cambia y `LastUpdate` |
| 10 | proceso existente | `Update` observación | cambia y `LastUpdate` |
| 11 | proceso inexistente | `Update` | `KeyNotFoundException` |
| 12 | proceso existente | `Delete` | borra |
| 13 | proceso inexistente | `Delete` | `KeyNotFoundException` |
| 14 | job con procesos | `GetSelectionProcesses(jobId)` | filtra |
| 15 | candidato con procesos | `GetSelectionProcesses(candidateId)` | filtra |
| 16 | proceso existente | `GetSelectionProcessById` | response mapeado |
**Archivos:** `MateHR.Application/Recruitment/SelectionProcesses/**`,
`Tests/Application/Recruitment/SelectionProcesses/**`, `Recruitment/DependencyInjection.cs`.
**Criterios de aceptación:**
- [ ] Los 16 casos verdes.
- [ ] Valida job/candidato/estado en el tenant.
- [ ] `LastUpdate` se actualiza.
- [ ] Mapping válido y DI actualizada.

## REC-10.4 — Api + tests

**US:** Como recruiter quiero operar procesos de selección por API.
**Guía técnica:** `SelectionProcessesController` en `/api/v1/recruitment/selection-processes`,
`GET ?jobId=&candidateId=`. Todas las rutas con `RequireRecruiter`.
**Plan TDD:** escribir los casos y verlos fallar.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | sin token | cualquier endpoint | 401 |
| 2 | token `Employee` | cualquier endpoint | 403 |
| 3 | token `Recruiter`, job/candidato/estado válidos | POST | 201 + Location |
| 4 | token `Recruiter`, job inexistente | POST | 404 |
| 5 | token `Recruiter`, body inválido | POST | 400 |
| 6 | token `Recruiter`, duplicado (D-09) | POST | 409 |
| 7 | token `Recruiter` | PUT estado/observación | 200 con `LastUpdate` |
| 8 | token `Recruiter` | GET ?jobId= | 200 filtrado |
| 9 | token `Recruiter` | GET ?candidateId= | 200 filtrado |
| 10 | token `Recruiter` | DELETE existente | 204 |
| 11 | token `Recruiter` | DELETE inexistente | 404 |
**Archivos:** `SelectionProcessesController.cs`,
`Tests/Api/Recruitment/SelectionProcessesControllerTests.cs`.
**Criterios de aceptación:**
- [ ] Los 11 casos verdes.
- [ ] Filtros por job/candidato.
- [ ] 404 para referencias inexistentes.
- [ ] Controller delgado.

---

# REC-11 · Portal externo (apply) + notificación/email

**Tipo:** Épica.
**US:** Como persona externa quiero ver los jobs activos y postularme sin autenticarme; el sistema
avisa al tenant para revisarlo y crea el candidato si no existe, marcándolo como externo.
**Depende de:** REC-00, REC-08, REC-09, REC-10.
**Alcance:** endpoints públicos de jobs, apply, alta de candidato externo y email.
**Criterios de aceptación de la épica:**
- [ ] Listado público solo con jobs activos y sin datos sensibles.
- [ ] Apply crea candidato externo si el email no existe.
- [ ] Apply no duplica candidato si el email existe; registra actividad.
- [ ] Apply repetido al mismo job devuelve 409 (si D-09).
- [ ] Email al inbox del tenant; un fallo de email no pierde la postulación (si D-08).
**Subtareas:** REC-11.1, REC-11.2, REC-11.3, REC-11.4.

## REC-11.1 — Domain + tests

**US:** Como persona externa quiero que mi alta quede marcada como externa y que no se dupliquen mis
datos.
**Guía técnica:** reutiliza `Candidate` (REC-09.1) y `SelectionProcess` (REC-10.1). No crear entidades
nuevas. Verificar que `IsExternallyCreated` es el discriminador y que no hay reglas nuevas sin cubrir.
**Plan TDD:** el caso `Candidate_ExternallyCreated_SetsFlag` ya debe existir; si no, agregarlo.
**Criterios de aceptación:**
- [ ] `IsExternallyCreated` testeado.
- [ ] No hay reglas de dominio nuevas fuera de REC-09.1/REC-10.1.
- [ ] Si se detecta una regla nueva, se agrega acá con su test.

## REC-11.2 — Infrastructure + tests

**US:** Como equipo quiero un servicio de email intercambiable para avisar postulaciones sin romper el
apply en desarrollo.
**Guía técnica (qué quiero que hagas):**
1. Crear `IEmailService` en `MateHR.Application/Recruitment/Interfaces/`.
2. Implementar `SmtpEmailService` y `NoOpEmailService` en Infrastructure.
3. Crear `EmailSettings` (por ejemplo `RecruitmentInbox`, host, puerto, credenciales).
4. Registrar por DI: SMTP si hay configuración; NoOp si no.
5. No acoplar Application a SMTP.
**Plan TDD:** testear el contrato con un fake/mock en Application; testear que NoOp no lanza y que DI
resuelve el servicio.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | NoOp registrado | `SendAsync` | completa sin efectos ni excepción |
| 2 | settings vacíos | resolver `IEmailService` | resuelve NoOp |
| 3 | settings completos | resolver `IEmailService` | resuelve SMTP |
| 4 | mock de `IEmailService` | apply | `SendAsync` llamado una vez con destinatario/asunto/body |
**Archivos:** `MateHR.Application/Recruitment/Interfaces/IEmailService.cs`,
`MateHR.Infrastructure/Services/SmtpEmailService.cs`,
`MateHR.Infrastructure/Services/NoOpEmailService.cs`,
`MateHR.Infrastructure/Settings/EmailSettings.cs`,
`MateHR.Infrastructure/DependencyInjection.cs`, tests de Infrastructure/Application.
**Criterios de aceptación:**
- [ ] NoOp implementa el contrato sin efectos.
- [ ] DI intercambiable.
- [ ] Application solo depende de la interfaz.
- [ ] Si falta configuración, el apply no se rompe.

## REC-11.3 — Application + tests

**US:** Como persona externa quiero ver jobs activos y postularme; como tenant quiero recibir el aviso
y que el candidato quede registrado una sola vez.
**Guía técnica (qué quiero que hagas):**
1. Crear `MateHR.Application/Recruitment/Public/` con `Commands/ApplyToJob`,
   `Queries/GetPublicJobs`, `GetPublicJobById`; DTOs, validadores, interfaces.
2. `GetPublicJobs`: solo jobs activos, no cerrados y no confirmados para borrado (D-12); sin datos
   sensibles; paginado.
3. `GetPublicJobById`: job público por id; 404 si no es visible.
4. `ApplyToJob`: resolver tenant por el job (D-11) → validar job activo → buscar candidato por email.
5. Si no existe: crear candidato con `IsExternallyCreated=true`.
6. Si existe: `RecordActivity(now)` y no duplicar candidato.
7. Crear `SelectionProcess` con el estado inicial (D-02) y observación/comentarios.
8. Si D-09 y ya existe proceso job+candidato, 409.
9. Enviar email al inbox del tenant; si falla, loguear y no revertir (D-08).
10. Registrar en DI.
**Depende de:** REC-11.2, REC-09.3, REC-10.3, D-02, D-08, D-09, D-11, D-12.
**Plan TDD:** escribir los casos y verlos fallar.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | job activo, email nuevo | `ApplyToJob` | crea candidato `IsExternallyCreated=true` + proceso inicial + email enviado |
| 2 | job activo, email existente | `ApplyToJob` | no crea candidato; actualiza `LastActivity`; crea proceso; email |
| 3 | job activo, email existente, proceso ya existe (D-09) | `ApplyToJob` | 409; no crea proceso |
| 4 | job inactivo | `ApplyToJob` | `KeyNotFoundException` o `ValidationException` (definir) |
| 5 | job de otro tenant | `ApplyToJob` | no se puede postular (404) |
| 6 | email inválido | `ApplyToJob` | `ValidationException` |
| 7 | DTO inválido (nombre/DNI) | `ApplyToJob` | `ValidationException` |
| 8 | no hay estado inicial (D-02) | `ApplyToJob` | error de configuración (definir) |
| 9 | email falla (D-08) | `ApplyToJob` | postulación persiste; no se propaga el fallo |
| 10 | jobs activos e inactivos | `GetPublicJobs` | solo activos y visibles |
| 11 | job inactivo | `GetPublicJobById` | 404 |
| 12 | job activo | `GetPublicJobById` | response sin datos sensibles |
**Archivos:** `MateHR.Application/Recruitment/Public/**`,
`Tests/Application/Recruitment/Public/**`, `Recruitment/DependencyInjection.cs`.
**Criterios de aceptación:**
- [ ] Los 12 casos verdes.
- [ ] Candidato externo marcado correctamente.
- [ ] No duplica candidato por email.
- [ ] Proceso inicial creado.
- [ ] Email verificado con mock; fallo no revierte.
- [ ] Sin `ITenantContext` en endpoints públicos.

## REC-11.4 — Api + tests

**US:** Como persona externa quiero consumir los jobs públicos y postularme sin token.
**Guía técnica (qué quiero que hagas):**
1. Crear `MateHR.Api/Controllers/v1/Public/PublicJobsController.cs`.
2. Ruta `api/v1/public/jobs`; `[AllowAnonymous]`.
3. `GET` lista activos; `GET/{id}` detalle público; `POST/{id}/apply`.
4. No exponer datos sensibles (salarios internos, observaciones privadas, datos de otros candidatos).
5. CORS ya está configurado; rate-limit queda como deuda técnica anotada.
**Depende de:** REC-11.3.
**Plan TDD:** escribir los casos y verlos fallar.
**Casos de prueba:**

| # | Datos | Acción | Resultado esperado |
|---|---|---|---|
| 1 | sin token, jobs activos e inactivos | GET lista | 200 solo activos |
| 2 | sin token, job activo | GET por id | 200 sin datos sensibles |
| 3 | sin token, job inactivo | GET por id | 404 |
| 4 | sin token, body válido, email nuevo | POST apply | 201; candidato externo |
| 5 | sin token, body válido, email existente | POST apply | 200; no duplica |
| 6 | sin token, apply repetido mismo job | POST apply | 409 |
| 7 | sin token, job inactivo | POST apply | 404 |
| 8 | sin token, body inválido | POST apply | 400 |
| 9 | sin token | cualquier endpoint | no devuelve 401 |
**Archivos:** `PublicJobsController.cs`, `Tests/Api/Recruitment/PublicJobsControllerTests.cs`.
**Criterios de aceptación:**
- [ ] Los 9 casos verdes.
- [ ] Endpoints anónimos.
- [ ] Solo jobs visibles.
- [ ] 201/200/409/404/400 según la tabla.
- [ ] Sin datos sensibles en el response.

---

# 8. Matriz consolidada de rutas y policies

| Recurso | Ruta base | Lectura | Escritura |
|---|---|---|---|
| JobStatuses | `/api/v1/recruitment/job-statuses` | RequireRecruiter | RequireTenantAdmin |
| Areas | `/api/v1/recruitment/areas` | RequireRecruiter | RequireTenantAdmin |
| SubAreas | `/api/v1/recruitment/sub-areas` | RequireRecruiter | RequireTenantAdmin |
| Clients | `/api/v1/recruitment/clients` | RequireRecruiter | RequireTenantAdmin |
| ProcessSteps | `/api/v1/recruitment/process-steps` | RequireRecruiter | RequireTenantAdmin |
| SearchReasons | `/api/v1/recruitment/search-reasons` | RequireRecruiter | RequireTenantAdmin |
| CandidateProcessStatuses | `/api/v1/recruitment/candidate-process-statuses` | RequireRecruiter | RequireTenantAdmin |
| Jobs | `/api/v1/recruitment/jobs` | RequireRecruiter | RequireRecruiter (steps: RequireTenantAdmin; deletion-confirm: RequireTenantAdmin) |
| Candidates | `/api/v1/recruitment/candidates` | RequireRecruiter | RequireRecruiter |
| SelectionProcesses | `/api/v1/recruitment/selection-processes` | RequireRecruiter | RequireRecruiter |
| PublicJobs | `/api/v1/public/jobs` | Anónimo | Anónimo (apply) |

---

# 9. Orden de ejecución recomendado

Se hace **cada tarea completa en vertical** (`REC-xx.1 → .4`) antes de pasar a la siguiente:

```text
REC-00  Fundamentos (cerrar 00.2 y D-04)
REC-01  JobStatus (01.3 y 01.4)
REC-02  Area
REC-03  SubArea
REC-04  Client
REC-05  ProcessStep
REC-06  SearchReason
REC-07  CandidateProcessStatus (resolver D-02)
REC-08  Job (resolver D-03/D-04/D-07)
REC-09  Candidate (resolver D-06; depende de REC-10 para historial)
REC-10  SelectionProcess (resolver D-09)
REC-11  Portal externo + email (resolver D-08/D-11/D-12)
```

> Nota de dependencia: REC-09.3 necesita el repositorio de REC-10.2 para armar `ProcessHistory`. Si se
> quiere hacer REC-09 antes, se puede dejar el historial vacío y completarlo en REC-09.3 cuando exista
> REC-10.2. No bloquear todo REC-09 por eso.

---

# 10. Checklist de PR por subtarea

- [ ] La rama nombra la tarjeta (`feature/REC-xx.y-descripcion`).
- [ ] El PR incluye el ID y el resumen de la capa.
- [ ] Los tests se escribieron antes (se ve en el orden de commits o en la descripción).
- [ ] Suite de la capa en verde.
- [ ] Sin warnings nuevos.
- [ ] DI actualizada si corresponde.
- [ ] Mapping validado si corresponde.
- [ ] Migración incluida si corresponde.
- [ ] No hay secretos ni cadenas de conexión hardcodeadas.
- [ ] No se tocaron archivos fuera del alcance sin justificarlo.
- [ ] La tarjeta quedó actualizada con evidencia.

---

# 11. Notas finales para el PM/TL

- **No arrancar REC-01.3 sin cerrar D-01 y D-05.** El contrato del repositorio de catálogo cambia si
  se agrega `IsInUseAsync` o si la unicidad pasa a ser case-insensitive.
- **No arrancar REC-08 sin cerrar D-03, D-04 y D-07.** El borrado en dos pasos y la validación de
  steps cambian DTOs y tests de API.
- **No arrancar REC-11 sin cerrar D-02, D-08, D-09, D-11 y D-12.** El estado inicial y la resolución
  del tenant público son decisiones de contrato.
- **La migración se crea una sola vez** después de REC-10.2. Si se crea antes, va a quedar incompleta.
- **Los catálogos son casi clones:** una vez que REC-01.3 y REC-01.4 estén verdes, REC-02 a REC-07
  deberían ser mecánicos. Usar el Anexo A y no inventar variaciones.
- **El orden vertical no es negociable:** si se salta de Domain a Api sin Infrastructure/Application,
  los tests de integración van a fallar por DI o por repositorio faltante, y se pierde el valor de TDD.




