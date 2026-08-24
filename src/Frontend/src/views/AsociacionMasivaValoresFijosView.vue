<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import DatePicker from 'primevue/datepicker'
import Button from 'primevue/button'
import Paginator from 'primevue/paginator'
import Tag from 'primevue/tag'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import { configurationService } from '../services/configurationService'
import { gruposValorFijoService } from '../services/gruposValorFijoService'
import { formatPeriodo } from '../utils/date'
import type {
  CatalogItem,
  ConfiguracionNomencladorListItemDto,
  GrupoValorFijoDto,
  ValorFijoCatalogItem,
} from '../types/configuration'

const toast = useToast()
const confirm = useConfirm()

function estadoSeverity(estado: string) {
  if (estado === 'Activa') return 'success'
  if (estado === 'Futura') return 'info'
  if (estado === 'Vencida') return 'warn'
  return 'secondary'
}

// ── Valores fijos (izquierda) ────────────────────────────────────────────────
const valoresFijos = ref<ValorFijoCatalogItem[]>([])
const loadingValores = ref(false)
const tipoFilter = ref<number | null>(null)
const grupoFilter = ref<number | null>(null)
const valorQuery = ref('')
const selectedValores = ref<ValorFijoCatalogItem[]>([])

const tiposDisponibles = computed<CatalogItem[]>(() => {
  const map = new Map<number, string>()
  for (const v of valoresFijos.value) {
    if (v.idTipo) map.set(v.idTipo, v.tipo)
  }
  return [...map.entries()]
    .map(([id, descripcion]) => ({ id, descripcion }))
    .sort((a, b) => a.descripcion.localeCompare(b.descripcion))
})

// El grupo es un filtro más (junto a Tipo y Buscar), habilita elegir de una un
// valor por cada tipo del grupo para asociarlos en la misma tanda masiva.
const tiposDelGrupo = computed(() => {
  const grupo = grupos.value.find((g) => g.id === grupoFilter.value)
  return grupo ? new Set(grupo.tipos.map((t) => t.id)) : null
})

const valoresFiltrados = computed(() => {
  const q = valorQuery.value.toLowerCase().trim()
  return valoresFijos.value
    .filter((v) => !tipoFilter.value || v.idTipo === tipoFilter.value)
    .filter((v) => !tiposDelGrupo.value || tiposDelGrupo.value.has(v.idTipo))
    .filter((v) => !q || v.descripcion.toLowerCase().includes(q) || v.tipo.toLowerCase().includes(q))
})

async function loadValoresFijos() {
  loadingValores.value = true
  try {
    valoresFijos.value = await configurationService.getValoresFijos()
  } finally {
    loadingValores.value = false
  }
}

// ── Grupos (filtro por tipos guardados) ───────────────────────────────────────
const grupos = ref<GrupoValorFijoDto[]>([])

async function loadGrupos() {
  grupos.value = await gruposValorFijoService.getAll()
}

function clearValoresSelection() {
  selectedValores.value = []
}

// Regla de negocio de esta pantalla: no tiene sentido asociar dos valores fijos
// del mismo tipo en la misma tanda masiva, así que ante un nuevo intento se
// reemplaza la selección previa de ese tipo (último elegido gana).
function handleValoresSelectionChange(newSelection: ValorFijoCatalogItem[]) {
  const porTipo = new Map<number, ValorFijoCatalogItem>()
  for (const item of newSelection) porTipo.set(item.idTipo, item)

  const descartados = newSelection.length - porTipo.size
  selectedValores.value = [...porTipo.values()]

  if (descartados > 0) {
    toast.add({
      severity: 'warn',
      summary: 'Solo un valor fijo por tipo',
      detail: 'Se reemplazó la selección anterior de ese tipo: solo se permite elegir un valor fijo por cada tipo.',
      life: 3500,
    })
  }
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
const totalAsociaciones = computed(() => selectedValores.value.length * selectedConfiguraciones.value.length)

async function handleAsociar() {
  if (selectedValores.value.length === 0 || selectedConfiguraciones.value.length === 0) return

  submitting.value = true
  try {
    const result = await configurationService.asociarValoresFijosMasivo({
      valoresFijosIds: selectedValores.value.map((v) => v.id),
      configuracionesIds: selectedConfiguraciones.value.map((c) => c.id),
    })
    toast.add({
      severity: 'success',
      summary: 'Asociación masiva completada',
      detail: `Se crearon ${result.asociacionesCreadas} asociaciones nuevas. ${result.asociacionesExistentes} ya existían y no se duplicaron.`,
      life: 6000,
    })
    clearValoresSelection()
    clearConfigSelection()
  } catch (e: any) {
    toast.add({
      severity: 'error',
      summary: 'Error al asociar',
      detail: e.response?.data?.mensaje ?? 'Ocurrió un error al asociar los valores fijos seleccionados.',
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
    message: `¿Desasociar ${selectedValores.value.length} valor(es) fijo(s) de ${selectedConfiguraciones.value.length} configuración(es)? Esta acción no se puede deshacer.`,
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
  if (selectedValores.value.length === 0 || selectedConfiguraciones.value.length === 0) return

  desasociando.value = true
  try {
    const result = await configurationService.desasociarValoresFijosMasivo({
      valoresFijosIds: selectedValores.value.map((v) => v.id),
      configuracionesIds: selectedConfiguraciones.value.map((c) => c.id),
    })
    toast.add({
      severity: 'success',
      summary: 'Desasociación masiva completada',
      detail: `Se eliminaron ${result.asociacionesEliminadas} asociaciones. ${result.asociacionesInexistentes} no existían.`,
      life: 6000,
    })
    clearValoresSelection()
    clearConfigSelection()
  } catch (e: any) {
    toast.add({
      severity: 'error',
      summary: 'Error al desasociar',
      detail: e.response?.data?.mensaje ?? 'Ocurrió un error al desasociar los valores fijos seleccionados.',
      life: 5000,
    })
  } finally {
    desasociando.value = false
  }
}

onMounted(async () => {
  await Promise.all([loadValoresFijos(), loadGrupos()])
  await loadConfiguraciones()
})

const virtualScrollerOptions = computed(() =>
  valoresFiltrados.value.length > 150 ? { itemSize: 46 } : undefined,
)
</script>

<template>
 <div>
  <section class="panel p-4 mt-3 flex justify-content-between align-items-center flex-wrap gap-3">
    <h2 class="text-xl mt-0 mb-0 font-semibold">Asociación masiva de valores fijos</h2>
    <p class="m-0">
      <strong>{{ selectedValores.length }}</strong> valor(es) fijo(s) ×
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
    <!-- Izquierda: valores fijos -->
    <section class="panel p-4 flex flex-column gap-3" style="flex: 1; min-width: 380px">
      <div class="flex justify-content-between align-items-center">
        <h3 class="text-lg m-0 font-semibold">Valores fijos</h3>
        <Tag :value="`${selectedValores.length} seleccionados`" severity="info" />
      </div>

      <div class="flex gap-2 flex-wrap">
        <div class="flex flex-column gap-1" style="flex: 1; min-width: 160px">
          <label class="field-label">Grupo</label>
          <Select v-model="grupoFilter"
            :options="grupos"
            option-label="descripcion"
            option-value="id"
            placeholder="Todos"
            show-clear
            filter
            class="w-full" />
        </div>
        <div class="flex flex-column gap-1" style="flex: 1; min-width: 160px">
          <label class="field-label">Tipo</label>
          <Select v-model="tipoFilter" 
            :options="tiposDisponibles" 
            :option-label="option => `${option.id} - ${option.descripcion}`"
            option-value="id"
            placeholder="Todos" 
            show-clear 
            filter filter-placeholder="Filtrar..." 
            class="w-full" />
        </div>
        <div class="flex flex-column gap-1" style="flex: 2; min-width: 200px">
          <label class="field-label">Filtrar</label>
          <InputText v-model="valorQuery" placeholder="Filtrar por descripción..." class="w-full" />
        </div>
      </div>

      <Button v-if="selectedValores.length" label="Limpiar selección" icon="pi pi-times" severity="secondary" text
        size="small" class="align-self-start" @click="clearValoresSelection" />

      <DataTable 
        :selection="selectedValores" 
        @update:selection="handleValoresSelectionChange" 
        :value="valoresFiltrados"
        :loading="loadingValores" 
        data-key="id" striped-rows sort-field="descripcion" 
        :sort-order="1" scrollable
        scroll-height="1040px"
        :virtual-scroller-options="virtualScrollerOptions">
        <template #empty>
          <span class="muted">No hay valores fijos para el filtro aplicado.</span>
        </template>
        <Column selection-mode="multiple" />
        <Column field="descripcion" header="Descripción" sortable />
        <Column field="tipo" header="Tipo" sortable  >
          <template #body="{ data }">
              {{ data.idTipo }} - {{ data.tipo }}
          </template>
        </Column>
        <Column header="Valor" sortable sort-field="valor" style="text-align: right">
          <template #body="{ data }">
            {{ data.valor?.toLocaleString('es-AR', { minimumFractionDigits: 2 }) }}
          </template>
        </Column>
      </DataTable>
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

      <DataTable 
        :selection="selectedConfiguraciones" 
        @update:selection="(value) => (selectedConfiguraciones = value)"
        :value="configuraciones" 
        :loading="loadingConfiguraciones" data-key="id" striped-rows>
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
