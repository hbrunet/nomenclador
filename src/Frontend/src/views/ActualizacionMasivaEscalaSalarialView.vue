<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import DatePicker from 'primevue/datepicker'
import InputNumber from 'primevue/inputnumber'
import Button from 'primevue/button'
import Paginator from 'primevue/paginator'
import Tag from 'primevue/tag'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import { configurationService } from '../services/configurationService'
import { formatLocalDate, formatPeriodo, parseLocalDate } from '../utils/date'
import type { ConfiguracionNomencladorListItemDto } from '../types/configuration'

const toast = useToast()
const confirm = useConfirm()

function estadoSeverity(estado: string) {
  if (estado === 'Activa') return 'success'
  if (estado === 'Futura') return 'info'
  if (estado === 'Vencida') return 'warn'
  return 'secondary'
}

// ── Configuraciones (izquierda) ───────────────────────────────────────────────
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

// ── Parámetros (derecha): nuevo período + coeficiente de ajuste ─────────────
const nuevoPeriodo = ref<Date | null>(null)
const coeficienteAjuste = ref<number>(1)
const submitting = ref(false)

const isValidPeriodo = computed(() => nuevoPeriodo.value !== null)
const isValidCoeficiente = computed(() => (coeficienteAjuste.value ?? 0) > 0)
const canConfirm = computed(
  () => selectedConfiguraciones.value.length > 0 && isValidPeriodo.value && isValidCoeficiente.value,
)

function confirmActualizar() {
  if (!canConfirm.value || !nuevoPeriodo.value) return
  confirm.require({
    message: `Se clonará la escala salarial de cada una de las ${selectedConfiguraciones.value.length} configuración(es) seleccionada(s) (con el período y coeficiente indicados) y se reasignará esa configuración a la escala clonada. Esta acción no se puede deshacer.`,
    header: 'Confirmar actualización masiva de escala salarial',
    icon: 'pi pi-sync',
    acceptLabel: 'Confirmar',
    rejectLabel: 'Cancelar',
    rejectProps: { severity: 'secondary', outlined: true },
    accept: () => handleConfirmar(),
  })
}

async function handleConfirmar() {
  if (!canConfirm.value || !nuevoPeriodo.value) return

  submitting.value = true
  try {
    const result = await configurationService.actualizarEscalaSalarialMasivo({
      configuracionesIds: selectedConfiguraciones.value.map((c) => c.id),
      nuevoPeriodo: formatLocalDate(nuevoPeriodo.value),
      coeficienteAjuste: coeficienteAjuste.value,
    })
    toast.add({
      severity: 'success',
      summary: 'Actualización masiva completada',
      detail: `Se clonaron ${result.escalasClonadas} escala(s) salarial(es) y se actualizaron ${result.configuracionesActualizadas} configuración(es).`,
      life: 6000,
    })
    clearConfigSelection()
    await loadConfiguraciones(pagination.page)
  } catch (e: any) {
    toast.add({
      severity: 'error',
      summary: 'Error al actualizar',
      detail: e.response?.data?.mensaje ?? e.response?.data?.message ?? 'Ocurrió un error al actualizar la escala salarial de las configuraciones seleccionadas.',
    })
  } finally {
    submitting.value = false
  }
}

onMounted(async () => {
  const periodoActivo = await configurationService.getPeriodoActivo()
  if (periodoActivo) {
    filters.vigenteEn = parseLocalDate(periodoActivo)
  }
  await loadConfiguraciones()
})
</script>

<template>
  <div>
    <section class="panel p-4 mt-3">
      <h2 class="text-xl mt-0 mb-0 font-semibold">Actualización masiva de escala salarial</h2>
      <p class="muted mt-2 mb-0">
        Paso 1: Seleccioná las configuraciones vigentes, el nuevo período y el coeficiente de ajuste. 
      </p>
      <p class="muted mt-2 mb-0">
        Paso 2: Al confirmar, el sistema clonará las escalas salariales con sus categorías ajustadas y las asignará automáticamente.
      </p>
    </section>

    <div class="flex gap-3 flex-wrap mt-3" style="align-items: flex-start">
      <!-- Izquierda: selección de configuraciones -->
      <section class="panel p-4 flex flex-column gap-3" style="flex: 2; min-width: 480px">
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
          <Button v-if="selectedConfiguraciones.length" label="Limpiar selección" icon="pi pi-times"
            severity="secondary" text size="small" @click="clearConfigSelection" />
        </div>

        <DataTable
          :selection="selectedConfiguraciones"
          @update:selection="(value) => (selectedConfiguraciones = value)"
          :value="configuraciones"
          :loading="loadingConfiguraciones" data-key="id" striped-rows>
          <template #empty>
            <span class="muted">No hay configuraciones para el filtro seleccionado.</span>
          </template>
          <Column selection-mode="multiple" header-style="width: 3rem" />
          <Column field="nomencladorDescripcion" header="Nomenclador" />
          <Column field="escalaDescripcion" header="Escala salarial" />
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

      <!-- Derecha: parámetros -->
      <section class="panel p-4 flex flex-column gap-3" style="flex: 1; min-width: 320px">
        <h3 class="text-lg m-0 font-semibold">Parámetros</h3>

        <div class="field">
          <label class="field-label">Nuevo período</label>
          <DatePicker v-model="nuevoPeriodo" view="month" date-format="mm/yy" class="w-full" fluid />
        </div>

        <div class="field">
          <label class="field-label">Coeficiente de ajuste</label>
          <InputNumber
            v-model="coeficienteAjuste"
            input-id="coeficienteAjuste"
            :min-fraction-digits="2"
            :max-fraction-digits="4"
            :min="0"
            :input-style="{ textAlign: 'right' }"
            fluid
          />
        </div>

        <Button
          label="Actualizar"
          :loading="submitting"
          :disabled="!canConfirm"
          @click="confirmActualizar"
        />
      </section>
    </div>
  </div>
</template>
