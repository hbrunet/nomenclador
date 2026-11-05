<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import Textarea from 'primevue/textarea'
import Checkbox from 'primevue/checkbox'
import Message from 'primevue/message'
import Tag from 'primevue/tag'
import { useToast } from 'primevue/usetoast'
import { asistenteService } from '../services/asistenteService'
import { asistenteFeedbackService } from '../services/asistenteFeedbackService'
import { configurationService } from '../services/configurationService'
import type { AsistenteAccionPropuestaDto, AsistenteFeedbackMotivo, AsistenteMensajeDto } from '../types/asistente'

type EstadoAccion = 'pendiente' | 'ejecutando' | 'confirmada' | 'descartada' | 'error'

interface AccionUi {
  accion: AsistenteAccionPropuestaDto
  estado: EstadoAccion
  resultadoId?: number
  error?: string
}

// 'ninguno': sin votar. 'detalle': 👎 clickeado, eligiendo motivo antes de enviar.
// 'enviando'/'enviado'/'error': resultado del POST a /asistente/feedback.
type EstadoFeedback = 'ninguno' | 'detalle' | 'enviando' | 'enviado' | 'error'

interface FeedbackUi {
  estado: EstadoFeedback
  util: boolean | null
  motivo: AsistenteFeedbackMotivo | null
  comentario: string
  compartir: boolean
  error?: string
}

interface MensajeUi {
  id: string
  rol: 'user' | 'assistant'
  contenido: string
  herramientas: string[]
  acciones: AccionUi[]
  error?: string
  // Sólo se completa en mensajes del asistente: pregunta que la originó (para feedback con opt-in).
  pregunta?: string
  feedback?: FeedbackUi
}

const motivosFeedback: { valor: AsistenteFeedbackMotivo; etiqueta: string }[] = [
  { valor: 'dato_incorrecto', etiqueta: 'Dato incorrecto' },
  { valor: 'respuesta_incompleta', etiqueta: 'Respuesta incompleta' },
  { valor: 'no_entendio', etiqueta: 'No entendió mi consulta' },
  { valor: 'otro', etiqueta: 'Otro' },
]

const router = useRouter()
const toast = useToast()

const habilitado = ref<boolean | null>(null)
const mensajes = ref<MensajeUi[]>([])
const entrada = ref('')
const enviando = ref(false)
const listaRef = ref<HTMLElement | null>(null)
let abortController: AbortController | null = null

const sugerencias = [
  '¿Cómo creo o edito una configuración?',
  'Buscá configuraciones activas',
]

const nombresHerramientas: Record<string, string> = {
  ConsultarAyuda: 'manual de usuario',
  ObtenerPeriodoActivo: 'período activo',
  BuscarNomencladores: 'nomencladores',
  BuscarEscalasSalariales: 'escalas salariales',
  BuscarZonas: 'zonas',
  ListarCategoriasDeEscala: 'categorías',
  BuscarConceptos: 'conceptos',
  ObtenerConcepto: 'detalle de concepto',
  BuscarValoresFijos: 'valores fijos',
  ContarUsosValorFijo: 'usos de valor fijo',
  BuscarValoresPorCategoria: 'valores por categoría',
  BuscarConfiguraciones: 'configuraciones',
  ObtenerConfiguracion: 'detalle de configuración',
  BuscarTiposValoresFijos: 'tipos de valores fijos',
  BuscarTiposValoresPorCategoria: 'tipos de valores por categoría'
}

function etiquetaHerramienta(nombre: string) {
  if (nombre.startsWith('Proponer')) return 'preparando acción'
  return `consultando ${nombresHerramientas[nombre] ?? nombre}`
}

onMounted(async () => {
  try {
    habilitado.value = (await asistenteService.getEstado()).habilitado
  } catch {
    habilitado.value = false
  }
})

onBeforeUnmount(() => abortController?.abort())

async function scrollAlFinal() {
  await nextTick()
  listaRef.value?.scrollTo({ top: listaRef.value.scrollHeight, behavior: 'smooth' })
}

function historialParaApi(): AsistenteMensajeDto[] {
  return mensajes.value
    .filter((m) => m.contenido.trim().length > 0)
    .map((m) => ({ rol: m.rol, contenido: m.contenido }))
}

async function enviar(texto?: string) {
  const contenido = (texto ?? entrada.value).trim()
  if (!contenido || enviando.value) return

  mensajes.value.push({ id: crypto.randomUUID(), rol: 'user', contenido, herramientas: [], acciones: [] })
  entrada.value = ''
  const historial = historialParaApi()

  mensajes.value.push({
    id: crypto.randomUUID(),
    rol: 'assistant',
    contenido: '',
    herramientas: [],
    acciones: [],
    pregunta: contenido,
    feedback: { estado: 'ninguno', util: null, motivo: null, comentario: '', compartir: false },
  })
  // Se toma la referencia reactiva (proxy) para que las mutaciones actualicen la vista.
  const respuesta = mensajes.value[mensajes.value.length - 1]

  enviando.value = true
  abortController = new AbortController()
  await scrollAlFinal()

  try {
    await asistenteService.chat(
      { mensajes: historial },
      (evento) => {
        if (evento.tipo === 'texto' && evento.texto) {
          respuesta.contenido += evento.texto
        } else if (evento.tipo === 'herramienta' && evento.texto) {
          respuesta.herramientas.push(evento.texto)
        } else if (evento.tipo === 'accion' && evento.accion) {
          respuesta.acciones.push({ accion: evento.accion, estado: 'pendiente' })
        } else if (evento.tipo === 'error') {
          respuesta.error = evento.texto ?? 'Error desconocido.'
        }
        scrollAlFinal()
      },
      abortController.signal,
    )
  } catch (error) {
    if ((error as Error).name !== 'AbortError') {
      respuesta.error = (error as Error).message
    }
  } finally {
    enviando.value = false
    abortController = null
  }
}

function detener() {
  abortController?.abort()
}

function nuevaConversacion() {
  detener()
  mensajes.value = []
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Enter' && !event.shiftKey) {
    event.preventDefault()
    enviar()
  }
}

// Helper en vez de comparar mensaje.feedback.estado directamente en el v-if: evita que
// vue-tsc, al ver ese mismo acceso de propiedad comparado contra otro literal más abajo
// (p. ej. 'enviando' dentro del bloque 'detalle'), lo marque como una comparación imposible.
function esFeedbackDetalle(mensaje: MensajeUi) {
  return mensaje.feedback?.estado === 'detalle'
}

function votar(mensaje: MensajeUi, util: boolean) {
  const feedback = mensaje.feedback
  if (!feedback || feedback.estado === 'enviando') return

  feedback.util = util
  feedback.error = undefined
  if (util) {
    feedback.motivo = null
    feedback.comentario = ''
    feedback.compartir = false
    enviarFeedback(mensaje)
  } else {
    feedback.estado = 'detalle'
  }
}

function cancelarFeedback(mensaje: MensajeUi) {
  const feedback = mensaje.feedback
  if (!feedback) return

  feedback.estado = 'ninguno'
  feedback.util = null
  feedback.motivo = null
  feedback.comentario = ''
  feedback.compartir = false
  feedback.error = undefined
}

async function enviarFeedback(mensaje: MensajeUi) {
  const feedback = mensaje.feedback
  if (!feedback || feedback.util === null) return
  if (feedback.util === false && !feedback.motivo) return

  feedback.estado = 'enviando'
  try {
    await asistenteFeedbackService.enviar({
      respuestaId: mensaje.id,
      util: feedback.util,
      motivo: feedback.util ? null : feedback.motivo,
      comentario: feedback.comentario.trim() || null,
      herramientas: mensaje.herramientas,
      consultaUsuario: feedback.compartir ? mensaje.pregunta ?? null : null,
      respuestaAsistente: feedback.compartir ? mensaje.contenido : null,
    })
    feedback.estado = 'enviado'
  } catch {
    feedback.estado = 'error'
    feedback.error = 'No se pudo enviar el feedback. Probá nuevamente.'
  }
}

async function confirmarAccion(item: AccionUi) {
  const { accion } = item
  item.estado = 'ejecutando'
  try {
    switch (accion.tipo) {
      case 'clonar-configuracion': {
        const creada = await configurationService.clone(accion.configuracionId, accion.clonar!)
        item.resultadoId = creada.id
        break
      }
      case 'agregar-concepto':
        await configurationService.addConcepto(accion.configuracionId, { idConcepto: accion.conceptoId!, orden: 0 })
        break
      case 'quitar-concepto':
        await configurationService.removeConcepto(accion.configuracionId, accion.conceptoId!)
        break
      case 'agregar-valor-fijo':
        await configurationService.addValorFijo(accion.configuracionId, { idValorFijo: accion.valorFijoId!, valor: 0 })
        break
      case 'quitar-valor-fijo':
        await configurationService.removeValorFijo(accion.configuracionId, accion.valorFijoId!)
        break
    }
    item.estado = 'confirmada'
    toast.add({ severity: 'success', summary: 'Acción realizada', detail: accion.descripcion, life: 5000 })
  } catch (error) {
    item.estado = 'error'
    const payload = (error as { response?: { data?: { mensaje?: string; errores?: { mensaje: string }[] } } }).response?.data
    item.error = payload?.errores?.map((e) => e.mensaje).join(' ') ?? payload?.mensaje ?? 'No se pudo ejecutar la acción.'
  }
}

function verConfiguracion(id: number) {
  router.push(`/configuraciones/${id}`)
}

// Markdown mínimo y seguro: se escapa todo el HTML antes de aplicar formato.
function escapeHtml(texto: string) {
  return texto
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;')
}

function formatoEnLinea(texto: string) {
  return texto
    .replace(/`([^`]+)`/g, '<code>$1</code>')
    .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
    .replace(/(^|[^*])\*([^*\s][^*]*)\*/g, '$1<em>$2</em>')
}

function renderMarkdown(markdown: string) {
  const lineas = escapeHtml(markdown).split('\n')
  const html: string[] = []
  let lista: 'ul' | 'ol' | null = null
  let tabla: string[][] | null = null

  const cerrarLista = () => {
    if (lista) html.push(`</${lista}>`)
    lista = null
  }
  const cerrarTabla = () => {
    if (!tabla) return
    const [encabezado, ...filas] = tabla
    html.push(
      '<table><thead><tr>' +
        encabezado.map((c) => `<th>${formatoEnLinea(c)}</th>`).join('') +
        '</tr></thead><tbody>' +
        filas.map((f) => '<tr>' + f.map((c) => `<td>${formatoEnLinea(c)}</td>`).join('') + '</tr>').join('') +
        '</tbody></table>',
    )
    tabla = null
  }

  for (const linea of lineas) {
    const trimmed = linea.trim()

    if (trimmed.startsWith('|')) {
      cerrarLista()
      if (/^\|[\s:|-]+\|$/.test(trimmed)) continue
      const celdas = trimmed.replace(/^\||\|$/g, '').split('|').map((c) => c.trim())
      tabla = tabla ?? []
      tabla.push(celdas)
      continue
    }
    cerrarTabla()

    const vineta = /^[-*]\s+(.*)$/.exec(trimmed)
    const numerada = /^\d+[.)]\s+(.*)$/.exec(trimmed)
    if (vineta || numerada) {
      const tipo = vineta ? 'ul' : 'ol'
      if (lista !== tipo) {
        cerrarLista()
        html.push(`<${tipo}>`)
        lista = tipo
      }
      html.push(`<li>${formatoEnLinea((vineta ?? numerada)![1])}</li>`)
      continue
    }
    cerrarLista()

    const titulo = /^(#{1,4})\s+(.*)$/.exec(trimmed)
    if (titulo) {
      html.push(`<p class="md-titulo">${formatoEnLinea(titulo[2])}</p>`)
    } else if (trimmed.length > 0) {
      html.push(`<p>${formatoEnLinea(trimmed)}</p>`)
    }
  }
  cerrarLista()
  cerrarTabla()
  return html.join('')
}
</script>

<template>
  <section class="panel p-4 asistente">
    <div class="flex justify-content-between align-items-center mb-3">
      <h2 class="text-xl mt-0 mb-0 font-semibold">
        <i class="pi pi-sparkles mr-2" aria-hidden="true"></i>Asistente
      </h2>
      <Button
        label="Nueva conversación"
        icon="pi pi-refresh"
        severity="secondary"
        text
        :disabled="mensajes.length === 0"
        @click="nuevaConversacion"
      />
    </div>

    <Message v-if="habilitado === false" severity="warn" :closable="false">
      El asistente no está configurado en el servidor. Revisá la sección <code>Asistente</code> (Endpoint y Model).
    </Message>

    <div ref="listaRef" class="asistente-mensajes" role="log" aria-live="polite" aria-label="Conversación con el asistente">
      <div v-if="mensajes.length === 0" class="asistente-vacio">
        <p class="mb-3">Preguntame sobre configuraciones, conceptos, valores o cómo usar el sistema.</p>
        <div class="flex flex-wrap gap-2 justify-content-center">
          <Button
            v-for="sugerencia in sugerencias"
            :key="sugerencia"
            :label="sugerencia"
            severity="secondary"
            outlined
            size="small"
            :disabled="!habilitado"
            @click="enviar(sugerencia)"
          />
        </div>
      </div>

      <div v-for="(mensaje, index) in mensajes" :key="mensaje.id" :class="['asistente-mensaje', `rol-${mensaje.rol}`]">
        <template v-if="mensaje.rol === 'user'">
          <div class="burbuja">{{ mensaje.contenido }}</div>
        </template>
        <template v-else>
          <div class="burbuja">
            <div v-if="mensaje.herramientas.length" class="flex flex-wrap gap-1 mb-2">
              <Tag
                v-for="(herramienta, i) in mensaje.herramientas"
                :key="i"
                :value="etiquetaHerramienta(herramienta)"
                severity="secondary"
                icon="pi pi-database"
              />
            </div>

            <!-- eslint-disable-next-line vue/no-v-html -- renderMarkdown escapa el HTML de entrada -->
            <div v-if="mensaje.contenido" class="md" v-html="renderMarkdown(mensaje.contenido)"></div>
            <div
              v-else-if="enviando && index === mensajes.length - 1 && !mensaje.error"
              class="text-color-secondary"
            >
              <i class="pi pi-spin pi-spinner mr-2" aria-hidden="true"></i>Pensando…
            </div>

            <div v-for="item in mensaje.acciones" :key="item.accion.id" class="accion">
              <div class="flex align-items-start gap-2">
                <i class="pi pi-exclamation-circle mt-1" aria-hidden="true"></i>
                <span class="flex-1">{{ item.accion.descripcion }}</span>
              </div>
              <div class="flex gap-2 mt-2 align-items-center flex-wrap">
                <template v-if="item.estado === 'pendiente' || item.estado === 'ejecutando'">
                  <Button
                    label="Confirmar"
                    icon="pi pi-check"
                    size="small"
                    :loading="item.estado === 'ejecutando'"
                    @click="confirmarAccion(item)"
                  />
                  <Button
                    label="Descartar"
                    icon="pi pi-times"
                    size="small"
                    severity="secondary"
                    text
                    :disabled="item.estado === 'ejecutando'"
                    @click="item.estado = 'descartada'"
                  />
                </template>
                <Tag v-else-if="item.estado === 'confirmada'" severity="success" value="Realizada" />
                <Tag v-else-if="item.estado === 'descartada'" severity="secondary" value="Descartada" />
                <Tag v-else-if="item.estado === 'error'" severity="danger" value="Error" />
                <Button
                  v-if="item.estado === 'confirmada'"
                  label="Ver configuración"
                  icon="pi pi-external-link"
                  size="small"
                  text
                  @click="verConfiguracion(item.resultadoId ?? item.accion.configuracionId)"
                />
              </div>
              <small v-if="item.error" class="text-red-600 block mt-1">{{ item.error }}</small>
            </div>

            <Message v-if="mensaje.error" severity="error" :closable="false" class="mt-2">{{ mensaje.error }}</Message>

            <div
              v-if="mensaje.feedback && mensaje.contenido && !(enviando && index === mensajes.length - 1)"
              class="asistente-feedback"
            >
              <div v-if="mensaje.feedback.estado === 'enviado'" class="flex align-items-center gap-2">
                <small class="text-color-secondary">
                  <i :class="mensaje.feedback.util ? 'pi pi-thumbs-up-fill' : 'pi pi-thumbs-down-fill'" aria-hidden="true"></i>
                  Gracias por tu feedback.
                </small>
                <Button label="Cambiar" text size="small" @click="cancelarFeedback(mensaje)" />
              </div>
              <template v-else>
                <div class="flex align-items-center gap-2">
                  <small class="text-color-secondary">¿Te sirvió esta respuesta?</small>
                  <Button
                    icon="pi pi-thumbs-up"
                    text
                    rounded
                    size="small"
                    :severity="mensaje.feedback.util === true ? 'success' : 'secondary'"
                    :disabled="mensaje.feedback.estado === 'enviando'"
                    aria-label="Sí, me sirvió"
                    @click="votar(mensaje, true)"
                  />
                  <Button
                    icon="pi pi-thumbs-down"
                    text
                    rounded
                    size="small"
                    :severity="mensaje.feedback.util === false ? 'danger' : 'secondary'"
                    :disabled="mensaje.feedback.estado === 'enviando'"
                    aria-label="No me sirvió"
                    @click="votar(mensaje, false)"
                  />
                </div>

                <div v-if="esFeedbackDetalle(mensaje)" class="asistente-feedback-detalle mt-2">
                  <div class="flex flex-wrap gap-2 mb-2">
                    <Button
                      v-for="opcion in motivosFeedback"
                      :key="opcion.valor"
                      :label="opcion.etiqueta"
                      size="small"
                      :outlined="mensaje.feedback.motivo !== opcion.valor"
                      @click="mensaje.feedback.motivo = opcion.valor"
                    />
                  </div>
                  <Textarea
                    v-model="mensaje.feedback.comentario"
                    auto-resize
                    rows="2"
                    class="w-full"
                    placeholder="Contanos qué faltó o qué estuvo mal (opcional)"
                  />
                  <div class="flex align-items-center gap-2 mt-2">
                    <Checkbox v-model="mensaje.feedback.compartir" binary :input-id="`compartir-${mensaje.id}`" />
                    <label :for="`compartir-${mensaje.id}`" class="text-sm">
                      Compartir mi pregunta y esta respuesta para ayudar a mejorar el asistente
                    </label>
                  </div>
                  <div class="flex gap-2 mt-2">
                    <Button
                      label="Enviar"
                      size="small"
                      :disabled="!mensaje.feedback.motivo"
                      :loading="mensaje.feedback.estado === 'enviando'"
                      @click="enviarFeedback(mensaje)"
                    />
                    <Button label="Cancelar" size="small" text severity="secondary" @click="cancelarFeedback(mensaje)" />
                  </div>
                </div>
                <small v-if="mensaje.feedback.error" class="text-red-600 block mt-1">{{ mensaje.feedback.error }}</small>
              </template>
            </div>
          </div>
        </template>
      </div>
    </div>

    <div class="asistente-entrada">
      <Textarea
        v-model="entrada"
        auto-resize
        rows="2"
        class="flex-1"
        placeholder="Escribí tu consulta… (Enter para enviar, Shift+Enter para nueva línea)"
        :disabled="!habilitado"
        @keydown="onKeydown"
      />
      <Button v-if="enviando" icon="pi pi-stop" severity="danger" aria-label="Detener" @click="detener" />
      <Button v-else icon="pi pi-send" aria-label="Enviar" :disabled="!habilitado || !entrada.trim()" @click="enviar()" />
    </div>
    <small class="text-color-secondary block mt-2">
      Las respuestas son generadas por IA y pueden contener errores. El asistente es de consulta y no modifica datos.
    </small>
  </section>
</template>

<style scoped>
.asistente {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 11rem);
  min-height: 420px;
}

.asistente-mensajes {
  flex: 1;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding: 0.5rem 0.25rem;
}

.asistente-vacio {
  margin: auto;
  text-align: center;
  max-width: 640px;
  color: var(--p-text-muted-color);
}

.asistente-mensaje {
  display: flex;
}

.rol-user {
  justify-content: flex-end;
}

.burbuja {
  max-width: 80%;
  padding: 0.75rem 1rem;
  border-radius: 12px;
  line-height: 1.45;
}

.rol-user .burbuja {
  background: var(--p-primary-color);
  color: var(--p-primary-contrast-color);
  white-space: pre-wrap;
}

.rol-assistant .burbuja {
  background: var(--p-content-hover-background, #f4f4f5);
  border: 1px solid var(--p-content-border-color, #e4e4e7);
}

.accion {
  margin-top: 0.75rem;
  padding: 0.75rem;
  border: 1px dashed var(--p-primary-color);
  border-radius: 8px;
  background: var(--p-content-background, #fff);
}

.asistente-feedback {
  margin-top: 0.75rem;
  padding-top: 0.5rem;
  border-top: 1px solid var(--p-content-border-color, #e4e4e7);
}

.asistente-feedback-detalle {
  max-width: 28rem;
}

.asistente-entrada {
  display: flex;
  gap: 0.5rem;
  align-items: flex-end;
  margin-top: 0.75rem;
}

.md :deep(p) {
  margin: 0 0 0.5rem;
}

.md :deep(p:last-child) {
  margin-bottom: 0;
}

.md :deep(.md-titulo) {
  font-weight: 600;
}

.md :deep(ul),
.md :deep(ol) {
  margin: 0 0 0.5rem;
  padding-left: 1.25rem;
}

.md :deep(code) {
  background: rgba(0, 0, 0, 0.06);
  padding: 0 0.25rem;
  border-radius: 4px;
}

.md :deep(table) {
  border-collapse: collapse;
  margin: 0.25rem 0 0.5rem;
  font-size: 0.9rem;
}

.md :deep(th),
.md :deep(td) {
  border: 1px solid var(--p-content-border-color, #e4e4e7);
  padding: 0.25rem 0.5rem;
  text-align: left;
}
</style>
