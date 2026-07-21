---
applyTo: "src/Frontend/**"
---

# Frontend Conventions (Vue 3 + TypeScript + Pinia)

## Entry Point
- **Always use `useConfiguration()`** composable in views and components — never import stores directly.
- It exposes all state refs and actions from both `configurationStore` and `conceptosStore`.

## Component Patterns
- **Draft editing**: components receive the draft via `v-model:draft` — the store owns state, components emit updates.
- **Validation display**: bind `validation.errores` and `validation.warnings` from the store; do not manage local error state.
- **Catalog dropdowns**: populated from `catalogs.*` in the store; call `fetchCatalogs(escalaId?)` on mount.

## Store Actions — Correct Sequence
```
// Edit flow
fetchCatalogs() → fetchDetail(id) → [user edits draft] → validateCurrent() → saveCurrent()

// Create flow
fetchCatalogs() → initializeDraft() → [user edits draft] → validateCurrent() → saveCurrent()

// Clone flow
cloneCurrent() → navigate to new id
```
- `saveCurrent()` internally validates before persisting — do **not** call `validateCurrent()` separately before it.

## Types (`types/configuration.ts`)
- Mirror backend DTOs exactly — keep in sync when backend DTOs change.
- **Catalog items**: `CatalogItem`, `ValorFijoCatalogItem`, `ValorCategoriaCatalogItem`, `CategoriaCatalogItem`, `ConceptoCatalogItem`.
- **View models** (frontend-only, for bound collections): `ConceptoConfiguradoViewModel`, `ValorFijoConfiguradoViewModel`, `ValorCategoriaConfiguradoViewModel`.
- **Input DTO** (sent to API): `ConfiguracionNomencladorCreateUpdateDto` — contains IDs, not descriptions.
- **Output DTOs** (received from API): `ConfiguracionNomencladorDetailDto`, `ConfiguracionNomencladorListItemDto`.

## Services (`services/`)
- All HTTP calls go through `configurationService` (CRUD + catalogs) or `conceptosService` (concept search).
- Base URL from `VITE_API_BASE_URL` env var; falls back to `http://localhost:5297/api`.
- Do **not** call Axios directly in components or stores — always go through the service layer.

## Validation
- **Client-side** (`validationService.validateDraft()`): required fields, date range, at least one concepto.
- **Server-side** (`configurationService.validate()`): overlap detection, duplicates.
- Merge both with `mergeValidationResults()` — the store's `validateCurrent()` action does this automatically.

## Build Check
- No test suite — run `npm run build` (runs `vue-tsc` + Vite) to catch type errors after changes.
