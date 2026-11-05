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
- `GET /api/asistente/estado`
- `POST /api/asistente/chat` (streaming SSE)
- `POST /api/asistente/feedback` — registra 👍/👎 sobre una respuesta puntual del asistente
- `GET /api/asistente/feedback/resumen` — totales y motivos de los votos negativos

### Asistente IA

La vista **Asistente** (`/asistente`) es un chat que responde consultas sobre los datos del sistema y su uso.
Usa un modelo local con [Ollama](https://ollama.com) (los datos no salen de la red) a través de su API
compatible con OpenAI y `Microsoft.Extensions.AI`, con *function calling*: el modelo nunca accede directamente
a la base de datos y sólo puede invocar las funciones definidas en `Services/AsistenteTools.cs`.

- **Consultas**: configuraciones, conceptos, catálogos, valores fijos y por categoría (sólo lectura).
  Para conceptos, las búsquedas y consultas de existencia por código o descripción devuelven un resumen; el detalle
  incluye propiedades y fórmulas y puede pedirse por código, descripción o Id. Si no puede identificar un único concepto,
  el asistente pide elegir uno. Las búsquedas pueden hacerse sin filtro y muestran hasta 25 resultados; por código solo
  admiten coincidencias parciales, mientras que código/subcódigo busca ambos números exactos.
  Para resolver un Id de tipo a partir de su descripción, el asistente ofrece búsquedas separadas para tipos de valores
  fijos y tipos de valores por categoría; después puede usar ese Id como filtro de valores o configuraciones.
  En el chat, las configuraciones se pueden filtrar por descripción del nomenclador, código/subcódigo del concepto
  e Id de tipo de valor fijo o por categoría, además de escala y zona por Id, fecha de vigencia y estado.
  Los filtros por tipo incluyen cualquier valor asociado de ese tipo. Las búsquedas devuelven `filtrosAplicados`:
  un filtro nulo o ausente no se aplicó; `estado=Todos` indica que no se filtró por estado.
  El total corresponde a todos los resultados que cumplen los filtros, no sólo a la página.
  Por ejemplo, "activas con valor fijo de tipo 65" combina `estado=Activa` y `valorFijoTipoId=65`.
  Para pedir el detalle no hace falta conocer
  el Id de la configuración: si los filtros identifican una sola se devuelve su detalle; si hay varias, el asistente
  muestra opciones para elegir. También pide una elección si la descripción del nomenclador o el código/subcódigo
  del concepto son ambiguos. El filtro de vigencia acepta `MM/yyyy` o `yyyy-MM`; los filtros inválidos no se omiten.
- **Ayuda de uso**: para preguntas sobre navegación y procedimientos, el backend consulta automáticamente por palabras clave
  el manual Markdown en `src/Backend/Nomenclador.Api/docs/ayuda/` y proporciona la sección más relevante al modelo antes de
  generar la respuesta. Esta consulta no depende de que el modelo elija una herramienta. Las herramientas de datos siguen
  disponibles para preguntas que combinan instrucciones de uso y datos reales. El contenido se copia
  al compilar y publicar la API. El asistente es de sólo lectura; las operaciones de escritura se realizan desde las pantallas.
- **Feedback**: cada respuesta del asistente tiene botones 👍/👎. Un voto negativo pide un motivo
  (dato incorrecto, respuesta incompleta, no entendió la consulta u otro) y permite agregar un comentario; un
  checkbox desmarcado por defecto permite compartir la pregunta y la respuesta completas para ayudar a diagnosticar
  el caso. El historial de conversación no se persiste en el servidor, así que el feedback no incluye la conversación
  salvo que el usuario la comparta explícitamente. Los votos se guardan en `USUARIO.ASISTENTE_FEEDBACK`
  (script `db/2026-11-05_asistente_feedback.sql`, aplicar manualmente) junto con las herramientas invocadas y el
  modelo vigente, para poder correlacionar los votos negativos con ajustes al prompt o al manual de ayuda. La vista
  **Feedback del asistente** (`/asistente/feedback`) muestra el resumen de totales y motivos.

#### Instalación de Ollama

1. Instalar Ollama para Windows desde https://ollama.com/download (escucha en `http://localhost:11434`).
2. Definir variables de entorno de sistema y reiniciar Ollama:
   - `OLLAMA_CONTEXT_LENGTH=16384`: el prompt, las herramientas y los resultados superan el contexto por defecto.
   - `OLLAMA_KEEP_ALIVE=24h`: mantiene el modelo cargado y evita la demora de la primera consulta.
   - Sólo si Ollama corre en otro servidor: `OLLAMA_HOST=0.0.0.0:11434`, y restringir el puerto en el firewall
     para que sólo lo alcance la API (Ollama no tiene autenticación).
3. Descargar un modelo con soporte de herramientas, por ejemplo `ollama pull qwen2.5:3b` (equipos con 8 GB de RAM)
   o `ollama pull qwen2.5:7b` (16 GB o más, mejor calidad). Si el proxy bloquea la descarga, se puede copiar la
   carpeta `%USERPROFILE%\.ollama\models` desde otro equipo.

Configuración (sección `Asistente` de `appsettings.json`):

| Clave | Por defecto | Descripción |
|-------|-------------|-------------|
| `Endpoint` | `http://localhost:11434/v1` | URL de la API compatible con OpenAI |
| `Model` | `qwen2.5:3b` | Nombre del modelo (`ollama list`) |
| `ApiKey` | — | Opcional; Ollama no la usa. Para proveedores externos usar user-secrets o `Asistente__ApiKey` |
| `TimeoutSegundos` | `300` | Espera máxima por respuesta (en CPU un modelo local puede tardar) |
| `MaxMensajesHistorial` | `20` | Mensajes previos que se envían al modelo |

Si el modelo no está disponible, el resto de la aplicación funciona normalmente y el chat muestra el error.

El cliente del asistente respeta el proxy predeterminado de .NET (configuración del sistema o variables
`HTTPS_PROXY` / `HTTP_PROXY`, con las exclusiones de `NO_PROXY`) y se autentica ante él con las credenciales
integradas de Windows de la cuenta que ejecuta la API. Las credenciales incluidas en la URL del proxy
se ignoran: configurá únicamente su dirección y puerto, sin usuario ni contraseña.
Si Ollama corre en otro servidor interno, agregá ese host a las exclusiones del proxy (`NO_PROXY` o la
configuración del sistema) para que la API no intente alcanzarlo a través de él.

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
