<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import ConceptoDetailDialog from '../components/ConceptoDetailDialog.vue'
import type { ConceptoCatalogItem } from '../types/configuration'
import FormulasDialog from '../components/FormulasDialog.vue'
import NomencladoresAsociadosDialog from '../components/NomencladoresAsociadosDialog.vue'
import { useConfiguration } from '../composables/useConfiguration'

const DEBOUNCE_MS = 300

const {
  conceptosPaginados: conceptos,
  loadingConceptosPaginados: loading,
  paginacionConceptos: pagination,
  tiposConcepto: tipos,
  tiposLiquidacion,
  fetchConceptosPaginados,
  fetchTiposConcepto,
  fetchTiposLiquidacion,
} = useConfiguration()
const query = ref('')
const tipoFilter = ref<number | null>(null)
const dialogRef = ref<InstanceType<typeof ConceptoDetailDialog> | null>(null)
const formulasDialogRef = ref<InstanceType<typeof FormulasDialog> | null>(null)
const asociadosDialogRef = ref<InstanceType<typeof NomencladoresAsociadosDialog> | null>(null)

const paginatorFirst = computed(() => (pagination.value.page - 1) * pagination.value.pageSize)

async function loadConceptos(page = 1) {
  await fetchConceptosPaginados(
    query.value.trim(), page, pagination.value.pageSize, tipoFilter.value,
  )
}

function onPageChange(event: { page: number; rows: number }) {
  pagination.value.pageSize = event.rows
  loadConceptos(event.page + 1)
}

// Búsqueda server-side con debounce: el catálogo de conceptos es grande (~1800 filas),
// evita traerlo completo al cliente y siempre reinicia a la primera página.
let queryDebounce: ReturnType<typeof setTimeout> | undefined
function handleQueryInput() {
  if (queryDebounce) clearTimeout(queryDebounce)
  queryDebounce = setTimeout(() => loadConceptos(1), DEBOUNCE_MS)
}
onUnmounted(() => {
  if (queryDebounce) clearTimeout(queryDebounce)
})

// El filtro por tipo es una selección discreta (no texto libre): recarga inmediata, sin debounce.
function handleTipoFilterChange() {
  loadConceptos(1)
}

function openCreate() {
  dialogRef.value?.open()
}

function openEdit(id: number) {
  dialogRef.value?.open(id)
}
function openFormulas(conceptoId: number) {
  formulasDialogRef.value?.open({ conceptoId })
}


function openAsociadosConcepto(concepto: ConceptoCatalogItem) {
  asociadosDialogRef.value?.open('concepto', concepto.id, concepto.descripcion)
}

function handleSaved() {
  void loadConceptos(pagination.value.page)
}

onMounted(async () => {
  await Promise.all([
    loadConceptos(),
    fetchTiposConcepto(),
    fetchTiposLiquidacion(),
  ])
})
</script>

<template>
  <section class="panel p-4">
    <div class="flex justify-content-between align-items-center mb-3">
      <h2 class="text-xl mt-0 mb-0 font-semibold">Conceptos</h2>
    </div>

    <div class="flex justify-content-between align-items-end gap-3 mt-3 mb-3 flex-wrap">
      <div class="flex gap-2 flex-wrap">
        <InputText
          v-model="query"
          placeholder="Buscar por código, cod/subcod o d:descripción..."
          style="width: 400px"
          @input="handleQueryInput"
        />
        <Select
          v-model="tipoFilter"
          :options="tipos"
          option-label="descripcion"
          option-value="id"
          placeholder="Filtrar por tipo..."
          style="width: 260px"
          show-clear
          filter
          filter-placeholder="Buscar..."
          @update:model-value="handleTipoFilterChange"
        />
      </div>
      <Button label="Nuevo concepto" icon="pi pi-plus" @click="openCreate" />
    </div>

    <DataTable
      :value="conceptos"
      :loading="loading"
      data-key="id"
      striped-rows
      lazy
      paginator
      :rows="pagination.pageSize"
      :total-records="pagination.total"
      :first="paginatorFirst"
      :rows-per-page-options="[50, 100, 200]"
      @page="onPageChange"
    >
      <template #empty>
        <span class="muted">
          {{ query ? 'Sin resultados para la búsqueda aplicada.' : 'No hay conceptos cargados.' }}
        </span>
      </template>
      <Column field="id" header="ID" style="width: 4rem; text-align: right;" />
      <Column field="codigo" header="Código" style="width: 6rem; text-align: right;" />
      <Column field="subcodigo" header="Subcódigo" style="width: 7rem; text-align: right;" />
      <Column field="descripcionBreve" header="Desc. breve" style="width: 10rem" />
      <Column field="descripcion" header="Descripción" >
        <template #body="{ data }">
          {{ data.descripcion ? data.descripcion.substring(0, 50) : "" }}
          <span v-if="data.descripcion && data.descripcion.length > 50">...</span>
        </template>
      </Column>
      <Column header="Tipo" style="width: 14rem">
        <template #body="{ data }">
          <Tag v-if="data.tipoConcepto" :value="data.tipoConcepto" severity="secondary" />
          <span v-else class="muted">Sin tipo</span>
        </template>
      </Column>
      <Column header="">
        <template #body="{ data }">
          <Button title="Editar" icon="pi pi-pencil" severity="secondary" size="small" rounded @click="openEdit(data.id)" />
          <Button title="Administrar fórmulas" icon="pi pi-calculator" severity="secondary" size="small" rounded @click="openFormulas(data.id)" />
          <Button
                    icon="pi pi-sitemap"
                    size="small"
                    severity="secondary"
                    rounded
                    title="Configuraciones asociadas"
                    @click="openAsociadosConcepto(data)"
                  />
        </template>
      </Column>
    </DataTable>

    <ConceptoDetailDialog ref="dialogRef" :tipos="tipos" :tipos-liquidacion="tiposLiquidacion" @saved="handleSaved" />
    <FormulasDialog ref="formulasDialogRef" />
    <NomencladoresAsociadosDialog ref="asociadosDialogRef" />
  </section>
</template>
