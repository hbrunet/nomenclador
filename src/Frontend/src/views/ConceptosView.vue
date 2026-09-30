<script setup lang="ts">
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import ConceptoDetailDialog from '../components/ConceptoDetailDialog.vue'
import { conceptosService } from '../services/conceptosService'
import type { CatalogItem, ConceptoCatalogItem } from '../types/configuration'

const DEBOUNCE_MS = 300

const conceptos = ref<ConceptoCatalogItem[]>([])
const loading = ref(false)
const query = ref('')
const tipoFilter = ref<number | null>(null)
const dialogRef = ref<InstanceType<typeof ConceptoDetailDialog> | null>(null)

const tipos = ref<CatalogItem[]>([])
const tiposLiquidacion = ref<CatalogItem[]>([])

const pagination = reactive({ total: 0, page: 1, pageSize: 20 })
const paginatorFirst = computed(() => (pagination.page - 1) * pagination.pageSize)

async function loadConceptos(page = 1) {
  loading.value = true
  try {
    const result = await conceptosService.listPaged(
      query.value.trim(), page, pagination.pageSize, tipoFilter.value,
    )
    conceptos.value = result.items
    pagination.total = result.total
    pagination.page = result.page
    pagination.pageSize = result.pageSize
  } finally {
    loading.value = false
  }
}

function onPageChange(event: { page: number; rows: number }) {
  pagination.pageSize = event.rows
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

function handleSaved(item: ConceptoCatalogItem) {
  const idx = conceptos.value.findIndex((c) => c.id === item.id)
  if (idx !== -1) {
    conceptos.value = conceptos.value.map((c) => (c.id === item.id ? item : c))
  } else {
    // Concepto nuevo: recargar para reflejar el orden/paginado real del backend.
    loadConceptos(pagination.page)
  }
}

onMounted(async () => {
  await Promise.all([
    loadConceptos(),
    conceptosService.getTipos().then((data) => (tipos.value = data)),
    conceptosService.getTiposLiquidacion().then((data) => (tiposLiquidacion.value = data)),
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
      :rows-per-page-options="[20, 50, 100]"
      @page="onPageChange"
    >
      <template #empty>
        <span class="muted">
          {{ query ? 'Sin resultados para la búsqueda aplicada.' : 'No hay conceptos cargados.' }}
        </span>
      </template>
      <Column field="codigo" header="Código" style="width: 6rem; text-align: right;" />
      <Column field="subcodigo" header="Subcódigo" style="width: 7rem; text-align: right;" />
      <Column field="descripcionBreve" header="Desc. breve" style="width: 10rem" />
      <Column field="descripcion" header="Descripción" />
      <Column header="Tipo" style="width: 14rem">
        <template #body="{ data }">
          <Tag v-if="data.tipoConcepto" :value="data.tipoConcepto" severity="secondary" />
          <span v-else class="muted">Sin tipo</span>
        </template>
      </Column>
      <Column header="" style="width: 6rem">
        <template #body="{ data }">
          <Button icon="pi pi-pencil" text rounded @click="openEdit(data.id)" />
        </template>
      </Column>
    </DataTable>

    <ConceptoDetailDialog ref="dialogRef" :tipos="tipos" :tipos-liquidacion="tiposLiquidacion" @saved="handleSaved" />
  </section>
</template>
