# Sistema de Configuración de Nomencladores Salariales

Proyecto full-stack para gestionar configuraciones de nomencladores salariales con vigencias, conceptos, escalas y valores parametrizados.

## Stack
- **Backend**: .NET (ASP.NET Core)
- **Frontend**: Vue 3 + TypeScript
- **State Management**: Pinia
- **HTTP Client**: Axios

## Estructura del Proyecto

### Backend
- `src/Backend/` - API REST en .NET

### Frontend
- `src/Frontend/` - Aplicación Vue 3

## Configuración Inicial

### Backend

```bash
cd /home/runner/work/nomenclador/nomenclador/src/Backend/Nomenclador.Api
dotnet run
```

La API expone:

- `GET /api/configuraciones-nomenclador`
- `GET /api/configuraciones-nomenclador/{id}`
- `POST /api/configuraciones-nomenclador`
- `PUT /api/configuraciones-nomenclador/{id}`
- `POST /api/configuraciones-nomenclador/validar`
- `POST /api/configuraciones-nomenclador/{id}/clonar`
- `GET /api/conceptos`
- `GET /api/catalogs/*`

### Frontend

```bash
cd /home/runner/work/nomenclador/nomenclador/src/Frontend
npm install
npm run dev
```

La aplicación Vue 3 incluye:

- vista de listado con filtros
- vista de detalle con editor por tabs
- stores de Pinia para configuraciones y conceptos
- servicios REST reutilizables
- tipos TypeScript para contratos de API
