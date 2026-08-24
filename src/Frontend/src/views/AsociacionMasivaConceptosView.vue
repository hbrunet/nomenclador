<script setup lang="ts">
import { computed, onMounted, onUnmounted, reactive, ref, watch } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import InputText from 'primevue/inputtext'
import DatePicker from 'primevue/datepicker'
import Button from 'primevue/button'
import Paginator from 'primevue/paginator'
import Tag from 'primevue/tag'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import { configurationService } from '../services/configurationService'
import { conceptosService } from '../services/conceptosService'
import { formatPeriodo } from '../utils/date'
import type { ConceptoCatalogItem, ConfiguracionNomencladorListItemDto } from '../types/configuration'

const toast = useToast()
const confirm = useConfirm()

const DEBOUNCE_MS = 300

function estadoSeverity(estado: string) {
  if (estado === 'Activa') return 'success'
  if (estado === 'Futura') return 'info'
  if (estado === 'Vencida') return 'warn'
  return 'secondary'
}

// ── Conceptos (izquierda) ─────────────────────────────────────────────────────
const conceptos = ref<ConceptoCatalogItem[]>([])
const loadingConceptos = ref(false)
const conceptoQuery = ref('')
const selectedConceptos = ref<ConceptoCatalogItem[]>([])

const conceptosPagination = reactive({ total: 0, page: 1, pageSize: 100 })
const conceptosPaginatorFirst = computed(
  () => (conceptosPagination.page - 1) * conceptosPagination.pageSize,
)

async function loadConceptos(page = 1) {
  loadingConceptos.value = true
  try {
    const result = await conceptosService.listPaged(
      conceptoQuery.value.trim(), page, conceptosPagination.pageSize,
    )
    conceptos.value = result.items
    conceptosPagination.total = result.total
    conceptosPagination.page = result.page
    conceptosPagination.pageSize = result.pageSize
  } finally {
    loadingConceptos.value = false
  }
}

function onConceptosPageChange(event: { page: number; rows: number }) {
  conceptosPagination.pageSize = event.rows
  loadConceptos(event.page + 1)
}

// Búsqueda server-side con debounce: evita traer todo el catálogo de conceptos
// al cliente (puede ser muy grande) y reinicia siempre a la primera página.
let conceptoQueryDebounce: ReturnType<typeof setTimeout> | undefined
watch(conceptoQuery, () => {
  if (conceptoQueryDebounce) clearTimeout(conceptoQueryDebounce)
  conceptoQueryDebounce = setTimeout(() => {
    loadConceptos(1)
  }, DEBOUNCE_MS)
})
onUnmounted(() => {
  if (conceptoQueryDebounce) clearTimeout(conceptoQueryDebounce)
})

function clearConceptosSelection() {
  selectedConceptos.value = []
}

function handleConceptosSelectionChange(newSelection: ConceptoCatalogItem[]) {
  selectedConceptos.value = newSelection
}

// ── Configuraciones (derecha) ─────────────────────────────────────────────────
const configuraciones = ref<ConfiguracionNomencladorListItemDto[]>([])
const loadingConfiguraciones = ref(false)

const filters = reactive({
  vigenteEn: new Date(),
})

const pagination = reactive({ total: 0, page: 1, pageSize: 20 })
const paginatorFirst = computed(() => (pagination.page - 1) * pagination.pageSize)
const selectedConfiguraciones = ref<ConfiguracionNomencladorListItemDto[]>([])

function buildParams(page: number, pageSize = pagination.pageSize) {
  return {
    nomencladorId: undefined,
    escalaSalarialId: undefined,
    zonaId: undefined,
    vigenteEn: filters.vigenteEn ? filters.vigenteEn.toISOString().substring(0, 7) : undefined,
    estado: undefined,
    page,
    pageSize,
  }
}

async function loadConfiguraciones(page = 1) {
  loadingConfiguraciones.value = true
  try {
    const result = await configurationService.list(buildParams(page))
    configuraciones.value = result.items
    pagination.total = result.total
    pagination.page = result.page
    pagination.pageSize = result.pageSize
  } finally {
    loadingConfiguraciones.value = false
  }
}

function onPageChange(event: { page: number; rows: number }) {
  pagination.pageSize = event.rows
  loadConfiguraciones(event.page + 1)
}

async function selectAllFiltered() {
  if (pagination.total === 0) return
  loadingConfiguraciones.value = true
  try {
    // Tope de seguridad: no traer resultados ilimitados de un filtro demasiado amplio.
    const cap = 2000
    const capped = Math.min(pagination.total, cap)
    const result = await configurationService.list(buildParams(1, capped))
    const existingIds = new Set(selectedConfiguraciones.value.map((c) => c.id))
    const nuevos = result.items.filter((item) => !existingIds.has(item.id))
    selectedConfiguraciones.value = [...selectedConfiguraciones.value, ...nuevos]

    const truncated = pagination.total > cap
    toast.add({
      severity: 'success',
      summary: 'Selección actualizada',
      detail: truncated
        ? `Se agregaron ${nuevos.length} configuraciones (primeros ${cap} resultados; total ${pagination.total}).`
        : `Se agregaron ${nuevos.length} configuraciones que cumplen el filtro actual.`,
      life: 2500,
    })
  } finally {
    loadingConfiguraciones.value = false
  }
}

function clearConfigSelection() {
  selectedConfiguraciones.value = []
}

// ── Asociación masiva ─────────────────────────────────────────────────────────
const submitting = ref(false)
const totalAsociaciones = computed(() => selectedConceptos.value.length * selectedConfiguraciones.value.length)

async function handleAsociar() {
  if (selectedConceptos.value.length === 0 || selectedConfiguraciones.value.length === 0) return

  submitting.value = true
  try {
    const result = await configurationService.asociarConceptosMasivo({
      conceptosIds: selectedConceptos.value.map((c) => c.id),
      configuracionesIds: selectedConfiguraciones.value.map((c) => c.id),
    })
    toast.add({
      severity: 'success',
      summary: 'Asociación masiva completada',
      detail: `Se crearon ${result.asociacionesCreadas} asociaciones nuevas. ${result.asociacionesExistentes} ya existían y no se duplicaron.`,
      life: 6000,
    })
    clearConceptosSelection()
    clearConfigSelection()
  } catch (e: any) {
    toast.add({
      severity: 'error',
      summary: 'Error al asociar',
      detail: e.response?.data?.mensaje ?? 'Ocurrió un error al asociar los conceptos seleccionados.',
      life: 5000,
    })
  } finally {
    submitting.value = false
  }
}

const desasociando = ref(false)

function confirmDesasociar() {
  if (totalAsociaciones.value === 0) return
  confirm.require({
    message: `¿Desasociar ${selectedConceptos.value.length} concepto(s) de ${selectedConfiguraciones.value.length} configuración(es)? Esta acción no se puede deshacer.`,
    header: 'Confirmar desasociación masiva',
    icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'Desasociar',
    acceptProps: { severity: 'danger' },
    rejectLabel: 'Cancelar',
    rejectProps: { severity: 'secondary', outlined: true },
    accept: () => handleDesasociar(),
  })
}

async function handleDesasociar() {
  if (selectedConceptos.value.length === 0 || selectedConfiguraciones.value.length === 0) return

  desasociando.value = true
  try {
    const result = await configurationService.desasociarConceptosMasivo({
      conceptosIds: selectedConceptos.value.map((c) => c.id),
      configuracionesIds: selectedConfiguraciones.value.map((c) => c.id),
    })
    toast.add({
      severity: 'success',
      summary: 'Desasociación masiva completada',
      detail: `Se eliminaron ${result.asociacionesEliminadas} asociaciones. ${result.asociacionesInexistentes} no existían.`,
      life: 6000,
    })
    clearConceptosSelection()
    clearConfigSelection()
  } catch (e: any) {
    toast.add({
      severity: 'error',
      summary: 'Error al desasociar',
      detail: e.response?.data?.mensaje ?? 'Ocurrió un error al desasociar los conceptos seleccionados.',
      life: 5000,
    })
  } finally {
    desasociando.value = false
  }
}

onMounted(async () => {
  await Promise.all([
    loadConceptos()
  ])
  await loadConfiguraciones()
})
</script>

<template>
 <div>
  <section class="panel p-4 mt-3 flex justify-content-between align-items-center flex-wrap gap-3">
    <h2 class="text-xl mt-0 mb-0 font-semibold">Asociación masiva de conceptos</h2>
    <p class="m-0">
      <strong>{{ selectedConceptos.length }}</strong> concepto(s) ×
      <strong>{{ selectedConfiguraciones.length }}</strong> configuración(es) =
      <strong>{{ totalAsociaciones }}</strong> combinación(es) seleccionada(s).
    </p>
    <div class="flex gap-2">
      <Button label="Desasociar" icon="pi pi-unlink" severity="danger" outlined :loading="desasociando"
        :disabled="totalAsociaciones === 0" @click="confirmDesasociar" />
      <Button label="Asociar" icon="pi pi-link" :loading="submitting" :disabled="totalAsociaciones === 0"
        @click="handleAsociar" />
    </div>
  </section>

  <div class="flex gap-3 flex-wrap" style="align-items: flex-start">
    <!-- Izquierda: conceptos -->
    <section class="panel p-4 flex flex-column gap-3" style="flex: 1; min-width: 380px">
      <div class="flex justify-content-between align-items-center">
        <h3 class="text-lg m-0 font-semibold">Conceptos</h3>
        <Tag :value="`${selectedConceptos.length} seleccionados`" severity="info" />
      </div>

      <div class="flex flex-column gap-1">
        <label class="field-label">Filtrar</label>
        <InputText v-model="conceptoQuery" placeholder="Código, código/subcódigo (25/100) o d:descripción..."
          class="w-full" />
        <span class="muted text-sm">
          Por defecto filtra por código. Usá "código/subcódigo" para filtrar por ambos (ej. 25/100),
          o el prefijo "d:" para filtrar por descripción (ej. d:decreto).
        </span>
      </div>

      <Button v-if="selectedConceptos.length" label="Limpiar selección" icon="pi pi-times" severity="secondary" text
        size="small" class="align-self-start" @click="clearConceptosSelection" />

      <DataTable :selection="selectedConceptos" @update:selection="handleConceptosSelectionChange"
        :value="conceptos" :loading="loadingConceptos" data-key="id" striped-rows scrollable scroll-height="1000px">
        <template #empty>
          <span class="muted">No hay conceptos para el filtro aplicado.</span>
        </template>
        <Column selection-mode="multiple" header-style="width: 3rem" />
        <Column field="codigo" header="Código" style="width: 8rem" />
        <Column field="subcodigo" header="Subcódigo" />
        <Column field="descripcion" header="Descripción" />
      </DataTable>

      <Paginator v-if="conceptosPagination.total > 0" :rows="conceptosPagination.pageSize"
        :total-records="conceptosPagination.total" :first="conceptosPaginatorFirst"
        :rows-per-page-options="[50, 100, 200]" @page="onConceptosPageChange" />
    </section>

    <!-- Derecha: configuraciones -->
    <section class="panel p-4 flex flex-column gap-3" style="flex: 1; min-width: 480px">
      <div class="flex justify-content-between align-items-center">
        <h3 class="text-lg m-0 font-semibold">Configuraciones</h3>
        <Tag :value="`${selectedConfiguraciones.length} seleccionadas`" severity="info" />
      </div>

      <div class="flex flex-wrap gap-2 align-items-end">

        <div class="flex flex-column gap-1">
          <label class="field-label">Vigente en</label>
          <DatePicker v-model="filters.vigenteEn" view="month" date-format="mm/yy"
            @date-select="loadConfiguraciones(1)" />
        </div>
      </div>

      <div class="flex gap-2 flex-wrap">
        <Button label="Seleccionar todo el resultado filtrado" icon="pi pi-check-square" severity="secondary"
          size="small" :disabled="pagination.total === 0" @click="selectAllFiltered" />
        <Button v-if="selectedConfiguraciones.length" label="Limpiar selección" icon="pi pi-times" severity="secondary"
          text size="small" @click="clearConfigSelection" />
      </div>

      <DataTable :selection="selectedConfiguraciones" @update:selection="(value) => (selectedConfiguraciones = value)"
        :value="configuraciones" :loading="loadingConfiguraciones" data-key="id" striped-rows>
        <template #empty>
          <span class="muted">No hay configuraciones para los filtros seleccionados.</span>
        </template>
        <Column selection-mode="multiple" header-style="width: 3rem" />
        <Column field="nomencladorDescripcion" header="Nomenclador" />
        <Column header="Vigencia">
          <template #body="{ data }">
            {{ formatPeriodo(data.fechaInicio) }} — {{ data.fechaFin ? formatPeriodo(data.fechaFin) : 'Vigente' }}
          </template>
        </Column>
        <Column header="Estado">
          <template #body="{ data }">
            <Tag :value="data.estado" :severity="estadoSeverity(data.estado)" />
          </template>
        </Column>
      </DataTable>

      <Paginator v-if="pagination.total > 0" :rows="pagination.pageSize" :total-records="pagination.total"
        :first="paginatorFirst" :rows-per-page-options="[10, 20, 50, 100]" @page="onPageChange" />
    </section>
  </div>
 </div>
</template>
