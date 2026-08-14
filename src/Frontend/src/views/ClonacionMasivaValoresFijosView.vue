<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Checkbox from 'primevue/checkbox'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import DatePicker from 'primevue/datepicker'
import InputNumber from 'primevue/inputnumber'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import { valoresFijosService } from '../services/valoresFijosService'
import { formatLocalDate } from '../utils/date'
import type { CatalogItem, ValorFijoCatalogItem } from '../types/configuration'

const toast = useToast()
const confirm = useConfirm()

// ── Valores fijos (selección) ────────────────────────────────────────────────
const valoresFijos = ref<ValorFijoCatalogItem[]>([])
const loadingValores = ref(false)
const tipoFilter = ref<number | null>(null)
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

const valoresFiltrados = computed(() => {
  const q = valorQuery.value.toLowerCase().trim()
  return valoresFijos.value
    .filter((v) => !tipoFilter.value || v.idTipo === tipoFilter.value)
    .filter((v) => !q || v.descripcion.toLowerCase().includes(q) || v.tipo.toLowerCase().includes(q))
})

async function loadValoresFijos(forceRefresh = false) {
  loadingValores.value = true
  try {
    valoresFijos.value = await valoresFijosService.getAll(forceRefresh)
  } finally {
    loadingValores.value = false
  }
}

function clearSelection() {
  selectedValores.value = []
}

function isSelected(item: ValorFijoCatalogItem) {
  return selectedValores.value.some((v) => v.id === item.id)
}

// Selección fila por fila: sin checkbox "seleccionar todo" en el header, que con
// muchos registros sin filtrar tildaba la UI al seleccionar de una todo el listado.
function toggleSelection(item: ValorFijoCatalogItem, checked: boolean | null) {
  selectedValores.value = checked
    ? [...selectedValores.value, item]
    : selectedValores.value.filter((v) => v.id !== item.id)
}

// ── Clonación masiva ──────────────────────────────────────────────────────────
const nuevoPeriodo = ref<Date | null>(null)
const coeficienteAjuste = ref<number>(1)
const cloning = ref(false)

const isValidPeriodo = computed(() => nuevoPeriodo.value !== null)
const isValidCoeficiente = computed(() => (coeficienteAjuste.value ?? 0) > 0)
const canClone = computed(
  () => selectedValores.value.length > 0 && isValidPeriodo.value && isValidCoeficiente.value,
)

function confirmClonar() {
  if (!canClone.value || !nuevoPeriodo.value) return
  confirm.require({
    message: `Se clonarán ${selectedValores.value.length} valor(es) fijo(s), reemplazando el período de su descripción por el nuevo período seleccionado. Esta acción no se puede deshacer.`,
    header: 'Confirmar clonación masiva',
    icon: 'pi pi-clone',
    acceptLabel: 'Clonar',
    rejectLabel: 'Cancelar',
    rejectProps: { severity: 'secondary', outlined: true },
    accept: () => handleClonar(),
  })
}

async function handleClonar() {
  if (!canClone.value || !nuevoPeriodo.value) return

  cloning.value = true
  try {
    const clonados = await valoresFijosService.cloneMasivo({
      valoresFijosIds: selectedValores.value.map((v) => v.id),
      nuevoPeriodo: formatLocalDate(nuevoPeriodo.value),
      coeficienteAjuste: coeficienteAjuste.value,
    })
    toast.add({
      severity: 'success',
      summary: 'Clonación masiva completada',
      detail: `Se crearon ${clonados.length} valor(es) fijo(s) nuevo(s).`,
      life: 5000,
    })
    clearSelection()
    await loadValoresFijos(true)
  } catch (e: any) {
    toast.add({
      severity: 'error',
      summary: 'Error al clonar',
      detail: e.response?.data?.message ?? 'Ocurrió un error al clonar los valores fijos seleccionados.',
      life: 5000,
    })
  } finally {
    cloning.value = false
  }
}

onMounted(() => loadValoresFijos())
</script>

<template>
  <div>
    <section class="panel p-4 mt-3">
      <h2 class="text-xl mt-0 mb-0 font-semibold">Clonación masiva de valores fijos</h2>
      <p class="muted mt-2 mb-0">
        Seleccioná los valores fijos a clonar y definí el nuevo período y el coeficiente de ajuste a aplicar sobre
        cada uno.
      </p>
    </section>

    <div class="flex gap-3 flex-wrap mt-3" style="align-items: flex-start">
      <!-- Izquierda: selección de valores fijos -->
      <section class="panel p-4 flex flex-column gap-3" style="flex: 2; min-width: 480px">
        <div class="flex justify-content-between align-items-center">
          <h3 class="text-lg m-0 font-semibold">Valores fijos</h3>
          <Tag :value="`${selectedValores.length} seleccionados`" severity="info" />
        </div>

        <div class="flex gap-2 flex-wrap">
          <div class="flex flex-column gap-1" style="flex: 1; min-width: 160px">
            <label class="field-label">Tipo</label>
            <Select
              v-model="tipoFilter"
              :options="tiposDisponibles"
              :option-label="(option) => `${option.id} - ${option.descripcion}`"
              option-value="id"
              placeholder="Todos"
              show-clear
              filter
              filter-placeholder="Buscar tipo..."
              class="w-full"
            />
          </div>
          <div class="flex flex-column gap-1" style="flex: 2; min-width: 200px">
            <label class="field-label">Buscar</label>
            <InputText v-model="valorQuery" placeholder="Buscar por descripción..." class="w-full" />
          </div>
        </div>

        <Button
          v-if="selectedValores.length"
          label="Limpiar selección"
          icon="pi pi-times"
          severity="secondary"
          text
          size="small"
          class="align-self-start"
          @click="clearSelection"
        />

        <DataTable
          :selection="selectedValores"
          @update:selection="(value) => (selectedValores.value = value)"
          :value="valoresFiltrados"
          :loading="loadingValores"
          data-key="id"
          striped-rows
          sort-field="descripcion"
          :sort-order="1"
          scrollable
          scroll-height="520px"
          :virtual-scroller-options="{ itemSize: 46 }"
        >
          <template #empty>
            <span class="muted">No hay valores fijos para el filtro aplicado.</span>
          </template>
          <Column style="width: 3rem">
            <template #body="{ data }">
              <Checkbox
                :model-value="isSelected(data)"
                binary
                @update:model-value="(checked) => toggleSelection(data, checked)"
              />
            </template>
          </Column>
          <Column field="descripcion" header="Descripción" sortable />
          <Column field="tipo" header="Tipo" sortable >
            <template #body="{ data }">
              {{ data.idTipo }} - {{ data.tipo }}
            </template>
          </Column>
          <Column header="Valor" style="text-align: right">
            <template #body="{ data }">
              {{ data.valor?.toLocaleString('es-AR', { minimumFractionDigits: 2 }) }}
            </template>
          </Column>
        </DataTable>
      </section>

      <!-- Derecha: parámetros de la clonación -->
      <section class="panel p-4 flex flex-column gap-4" style="flex: 1; min-width: 320px">
        <h3 class="text-lg m-0 font-semibold">Parámetros</h3>

        <div class="field">
          <label class="field-label">Nuevo período</label>
          <DatePicker v-model="nuevoPeriodo" view="month" date-format="mm/yy" placeholder="MM/AAAA" class="w-full" />
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
          label="Clonar seleccionados"
          icon="pi pi-clone"
          :loading="cloning"
          :disabled="!canClone"
          @click="confirmClonar"
        />
      </section>
    </div>
  </div>
</template>
