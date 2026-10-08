<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import Dialog from 'primevue/dialog'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import DatePicker from 'primevue/datepicker'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import Paginator from 'primevue/paginator'
import { configurationService } from '../services/configurationService'
import { formatPeriodo, parseLocalDate } from '../utils/date'
import type { ConfigurationFilters, ConfiguracionNomencladorListItemDto } from '../types/configuration'

export type TipoReferencia = 'concepto' | 'escala' | 'valorFijo' | 'valorCategoria'

const ETIQUETAS: Record<TipoReferencia, string> = {
  concepto: 'concepto',
  escala: 'escala salarial',
  valorFijo: 'valor fijo',
  valorCategoria: 'valor por categoría',
}

const router = useRouter()

const isVisible = ref(false)
const tipo = ref<TipoReferencia>('concepto')
const referenciaId = ref<number | null>(null)
const referenciaDescripcion = ref<string>('')
const vigenteEn = ref<Date | null>(null)
const items = ref<ConfiguracionNomencladorListItemDto[]>([])
const loading = ref(false)
const printing = ref(false)
const error = ref<string | null>(null)
const pagination = ref({ total: 0, page: 1, pageSize: 10 })

const paginatorFirst = computed(() => (pagination.value.page - 1) * pagination.value.pageSize)
const etiqueta = computed(() => ETIQUETAS[tipo.value])
const titulo = computed(() => `Configuraciones asociadas al ${etiqueta.value}`)

function estadoSeverity(estado: string) {
  if (estado === 'Activa') return 'success'
  if (estado === 'Futura') return 'info'
  if (estado === 'Vencida') return 'warn'
  return 'secondary'
}

// El backend expone un filtro distinto por tipo de referencia; la escala salarial
// es un dato propio de la configuración y por eso usa escalaSalarialId.
function buildFiltroReferencia(id: number): ConfigurationFilters {
  switch (tipo.value) {
    case 'concepto':
      return { conceptoId: id }
    case 'escala':
      return { escalaSalarialId: id }
    case 'valorFijo':
      return { valorFijoId: id }
    case 'valorCategoria':
      return { valorCategoriaId: id }
  }
}

async function load(page = 1) {
  if (referenciaId.value === null) return

  loading.value = true
  error.value = null
  try {
    const result = await configurationService.list({
      ...buildFiltroReferencia(referenciaId.value),
      // Mismo formato "YYYY-MM" que usa el resto de las pantallas con filtro de vigencia.
      vigenteEn: vigenteEn.value ? formatMes(vigenteEn.value) : undefined,
      page,
      pageSize: pagination.value.pageSize,
    })
    items.value = result.items
    pagination.value = { total: result.total, page: result.page, pageSize: result.pageSize }
  } catch (e: any) {
    items.value = []
    pagination.value = { ...pagination.value, total: 0, page: 1 }
    error.value =
      e.response?.data?.mensaje ??
      e.response?.data?.message ??
      'No se pudieron obtener las configuraciones asociadas.'
  } finally {
    loading.value = false
  }
}

function formatMes(date: Date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`
}

function escapeHtml(value: string | number) {
  return String(value)
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#039;')
}

function buildPrintDocument(configuraciones: ConfiguracionNomencladorListItemDto[]) {
  const referencia = referenciaDescripcion.value
    ? `${referenciaDescripcion.value} (ID ${referenciaId.value})`
    : `ID ${referenciaId.value}`
  const periodo = vigenteEn.value
    ? vigenteEn.value.toLocaleDateString('es-AR', { month: 'long', year: 'numeric' })
    : 'Todas las vigencias'
  const rows = configuraciones
    .map(
      (item) => `
        <tr>
          <td>${escapeHtml(item.nomencladorDescripcion)}</td>
          <td>${escapeHtml(formatPeriodo(item.fechaInicio))}</td>
          <td>${escapeHtml(item.fechaFin ? formatPeriodo(item.fechaFin) : '—')}</td>
          <td>${escapeHtml(item.estado)}</td>
        </tr>`,
    )
    .join('')

  return `<!doctype html>
<html lang="es">
  <head>
    <meta charset="utf-8">
    <title>${escapeHtml(titulo.value)}</title>
    <style>
      @page { size: portrait; margin: 12mm; }
      * { box-sizing: border-box; }
      body { color: #111827; font-family: Arial, sans-serif; font-size: 10pt; margin: 0; }
      h1 { font-size: 16pt; margin: 0 0 6px; }
      .summary { margin-bottom: 14px; }
      .summary p { margin: 3px 0; }
      table { border-collapse: collapse; width: 100%; }
      th, td { border: 1px solid #9ca3af; padding: 6px 7px; text-align: left; vertical-align: top; }
      th { background: #e5e7eb; font-weight: 700; }
      tr { break-inside: avoid; }
      .number { text-align: right; }
      .total { font-weight: 700; }
    </style>
  </head>
  <body>
    <h1>${escapeHtml(titulo.value)}</h1>
    <div class="summary">
      <p><strong>Referencia:</strong> ${escapeHtml(referencia)}</p>
      <p><strong>Vigencia:</strong> ${escapeHtml(periodo)}</p>
      <p class="total">Total: ${configuraciones.length} configuración(es)</p>
    </div>
    <table>
      <thead>
        <tr>
          <th>Nomenclador</th>
          <th>Desde</th>
          <th>Hasta</th>
          <th>Estado</th>
        </tr>
      </thead>
      <tbody>${rows}</tbody>
    </table>
  </body>
</html>`
}

async function printListado() {
  if (referenciaId.value === null || pagination.value.total === 0) return

  const printWindow = window.open('', '_blank')
  if (!printWindow) {
    error.value = 'El navegador bloqueó la ventana de impresión. Habilite las ventanas emergentes e intente nuevamente.'
    return
  }

  printWindow.opener = null
  printWindow.document.write('<p style="font-family: Arial, sans-serif">Preparando listado para imprimir...</p>')
  printing.value = true
  error.value = null

  try {
    const result = await configurationService.list({
      ...buildFiltroReferencia(referenciaId.value),
      vigenteEn: vigenteEn.value ? formatMes(vigenteEn.value) : undefined,
      page: 1,
      pageSize: pagination.value.total,
    })

    if (printWindow.closed) return

    printWindow.document.open()
    printWindow.document.write(buildPrintDocument(result.items))
    printWindow.document.close()
    printWindow.focus()
    printWindow.print()
  } catch (e: any) {
    if (!printWindow.closed) printWindow.close()
    error.value =
      e.response?.data?.mensaje ??
      e.response?.data?.message ??
      'No se pudo preparar el listado para imprimir.'
  } finally {
    printing.value = false
  }
}

function onPageChange(event: { page: number; rows: number }) {
  pagination.value.pageSize = event.rows
  load(event.page + 1)
}

async function open(tipoReferencia: TipoReferencia, id: number, descripcion = '') {
  tipo.value = tipoReferencia
  referenciaId.value = id
  referenciaDescripcion.value = descripcion
  items.value = []
  error.value = null
  pagination.value = { total: 0, page: 1, pageSize: 10 }
  isVisible.value = true

  // Por defecto se muestran las configuraciones vigentes en el período activo.
  const periodoActivo = await configurationService.getPeriodoActivo()
  vigenteEn.value = periodoActivo ? parseLocalDate(periodoActivo) : new Date()
  await load()
}

function close() {
  isVisible.value = false
}

function abrirConfiguracion(id: number) {
  close()
  router.push(`/configuraciones/${id}`)
}

defineExpose({ open, close })
</script>

<template>
  <Dialog v-model:visible="isVisible" :header="titulo" modal :style="{ width: '60rem' }">
    <div class="flex flex-column gap-3">
      <p class="m-0 muted">
        {{ referenciaDescripcion || `ID ${referenciaId}` }}
        <span v-if="referenciaDescripcion && referenciaId !== null"> (ID {{ referenciaId }})</span>
      </p>

      <div class="flex align-items-end gap-2 flex-wrap">
        <div class="flex flex-column gap-1">
          <label class="field-label">Vigente en</label>
          <DatePicker
            v-model="vigenteEn"
            view="month"
            date-format="mm/yy"
            show-clear
            style="width: 12rem"
            @update:model-value="load(1)"
          />
        </div>
        <Button
          label="Actualizar"
          icon="pi pi-refresh"
          severity="secondary"
          outlined
          :loading="loading"
          @click="load(pagination.page)"
        />
        <span class="muted ml-2">{{ pagination.total }} configuración(es)</span>
      </div>

      <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>

      <DataTable :value="items" :loading="loading" striped-rows>
        <template #empty>
          <span class="muted">
            {{
              vigenteEn
                ? 'El elemento no está asociado a ninguna configuración vigente en el período seleccionado.'
                : 'El elemento no está asociado a ninguna configuración.'
            }}
          </span>
        </template>
        <Column field="nomencladorDescripcion" header="Nomenclador" />
        <Column field="escalaDescripcion" header="Escala" />
        <Column field="zonaDescripcion" header="Zona" />
        <Column header="Desde" style="width: 7rem">
          <template #body="{ data }">{{ formatPeriodo(data.fechaInicio) }}</template>
        </Column>
        <Column header="Hasta" style="width: 7rem">
          <template #body="{ data }">
            {{ data.fechaFin ? formatPeriodo(data.fechaFin) : '—' }}
          </template>
        </Column>
        <Column header="Estado" style="width: 8rem">
          <template #body="{ data }">
            <Tag :value="data.estado" :severity="estadoSeverity(data.estado)" />
          </template>
        </Column>
        <Column style="width: 6rem">
          <template #body="{ data }">
            <Button
              icon="pi pi-external-link"
              size="small"
              severity="secondary"
              text
              rounded
              title="Abrir configuración"
              @click="abrirConfiguracion(data.id)"
            />
          </template>
        </Column>
      </DataTable>

      <Paginator
        v-if="pagination.total > pagination.pageSize"
        :first="paginatorFirst"
        :rows="pagination.pageSize"
        :total-records="pagination.total"
        :rows-per-page-options="[10, 20, 50]"
        @page="onPageChange"
      />
    </div>

    <template #footer>
      <Button
        label="Imprimir"
        icon="pi pi-print"
        severity="secondary"
        outlined
        :loading="printing"
        :disabled="loading || pagination.total === 0"
        @click="printListado"
      />
      <Button label="Cerrar" severity="secondary" outlined @click="close" />
    </template>
  </Dialog>
</template>
