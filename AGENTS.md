# Nomenclador – Agent Instructions

## Project Overview

Full-stack payroll nomenclature configuration system.  
**Backend**: ASP.NET Core + NHibernate (C#) · **Frontend**: Vue 3 + TypeScript + Pinia · **DB**: Oracle 11g

---

## Build & Run

### Backend
```bash
cd src/Backend/Nomenclador.Api
dotnet run
# Listens on http://localhost:5297
```

### Frontend
```bash
cd src/Frontend
npm install   # first time only
npm run dev   # http://localhost:5173
npm run build # vue-tsc + vite build (type-checks before bundling)
```

> Connection string lives in `src/Backend/Nomenclador.Api/appsettings.Development.json`  
> Oracle host: `10.6.46.17:1521` (service `HTEST01`), proxy user `APPUSER`.

---

## Architecture

See [README.md](README.md) for a high-level overview.

### Backend layers (all under `src/Backend/Nomenclador.Api/`)
| Layer | Path | Notes |
|-------|------|-------|
| Controllers | `Controllers/` | Thin – delegate to Services |
| Services | `Services/` | Business logic, validation, cloning |
| Repositories | `Repositories/` | NHibernate `QueryOver` queries |
| Mappers | `Mappers/` | Entity ↔ DTO conversion (requires `CatalogSnapshot`) |
| Models | `Models/` | NHibernate entities |
| DTOs | `DTOs/` | Input (`*InputDto`, `CreateUpdateDto`) and output (`DetailDto`, `ListItemDto`) |
| Data/Mappings | `Data/Mappings/` | NHibernate `ClassMap<T>` per entity |

**Root aggregate**: `ConfiguracionNomencladorEntity` owns `Conceptos`, `ValoresFijos`, `ValoresCategorias` collections.

### Frontend layers (all under `src/Frontend/src/`)
| Layer | Path | Notes |
|-------|------|-------|
| Views | `views/` | Route-level components, orchestrate stores |
| Components | `components/` | Reusable UI; editor tabs, modals, lists |
| Stores (Pinia) | `stores/` | `configurationStore`, `conceptosStore` |
| Composables | `composables/` | `useConfiguration` wraps both stores |
| Services | `services/` | Axios HTTP clients; `configurationService`, `conceptosService`, `validationService` |
| Types | `types/` | TypeScript interfaces mirroring backend DTOs |

---

## Key Conventions

### Backend
- **All classes are `sealed`** – entities, DTOs, services, repositories, controllers, mappers, mappings.
- **All DTO properties are `init`-only** – `public int Id { get; init; }`. Collections default to `[]`.
- **ISession is scoped per HTTP request** – injected into repositories and services via DI; never resolve it from a singleton.
- **NHibernate `QueryOver` only** – do not use LINQ to NHibernate. Use `Restrictions` for Oracle-compatible filtering.
- **NHibernate mappings** – each entity has a corresponding `ClassMap<T>` in `Data/Mappings/`. Adding a new entity requires a new map file.
- **DateOnly** – Oracle 11g needs the custom `DateOnlyUserType` (`Data/DateOnlyUserType.cs`); use `.CustomType<DateOnlyUserType>()` in every date column mapping.
- **Oracle schema** – all tables use the `USUARIO.*` prefix (e.g., `Table("USUARIO.HISTORIALNOMENCLADOR")`). Columns are UPPERCASE. Sequence IDs use `USUARIO.*_SEQ` (e.g., `GeneratedBy.Sequence("USUARIO.HISTORIALNOMENCLADOR_SEQ")`).
- **Catalog data** – always fetched via `CatalogRepository.GetSnapshot()` and passed as a `CatalogSnapshot` to mappers; never query catalogs inside a mapper.
- **Validation** – client (`validationService`) + server (`ValidacionConfiguracionService`) run both; server throws `ConfiguracionValidationException` for hard errors.
- **Overlap rule** – no two configurations may share the same `NomencladorId + EscalaSalarialId + ZonaId` with overlapping date ranges.
- **Error handling** – all unhandled exceptions are caught by `ApiExceptionMiddleware` and returned as JSON.
- **Input DTOs contain IDs only** – `IdNomenclador`, `IdEscalaSalarial`; output DTOs contain resolved descriptions (`NomencladorDescripcion`).

### Frontend
- **Two-way binding via `v-model:draft`** – the editor component receives a `CreateUpdateDto` draft; stores own the state.
- **Save flow**: `saveCurrent()` → validate (client + server) → create or update → reload list → navigate to detail.
- **Clone flow**: `cloneCurrent()` shifts dates by +1 year, POSTs to `/clonar`, navigates to new config.
- **Estado resolution** is display-only (Futura / Activa / Vencida), computed in the list mapper on the backend.
- **Single composable entry point** – `useConfiguration()` wraps both Pinia stores; use it in all views/components.
- **API base URL** – read from `VITE_API_BASE_URL` env var; defaults to `http://localhost:5297/api`.
- **No test suite** exists yet – validate changes by running `npm run build` (type-checks) and manual smoke-testing.

---

## API Endpoints Reference

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/configuraciones-nomenclador` | Paginated list with filters |
| GET | `/api/configuraciones-nomenclador/{id}` | Detail |
| POST | `/api/configuraciones-nomenclador` | Create |
| PUT | `/api/configuraciones-nomenclador/{id}` | Update |
| POST | `/api/configuraciones-nomenclador/validar` | Server-side validate only |
| POST | `/api/configuraciones-nomenclador/{id}/clonar` | Clone |
| GET | `/api/conceptos?q=…` | Search concepts |
| GET | `/api/catalogs/*` | nomencladores, escalas, zonas, categorias, valores-fijos, valores-categorias |

---

## Common Pitfalls

- Forgetting `DateOnlyUserType` in a new NHibernate mapping causes silent date conversion errors with Oracle.
- The `CatalogSnapshot` must be built before calling any mapper `ToDetailDto`/`ToListItemDto` – it's not injected automatically.
- Frontend `types/` must stay in sync with backend `DTOs/` – discrepancies surface only at runtime.
- CORS policy (`VueClient`) only allows `localhost:5173` and `localhost:4173`; add origins in `Program.cs` if needed.
