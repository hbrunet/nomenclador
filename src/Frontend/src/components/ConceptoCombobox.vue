<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import Dialog from 'primevue/dialog'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import { useConfiguration } from '../composables/useConfiguration'
import type { ConceptoCatalogItem } from '../types/configuration'

const MIN_QUERY_LENGTH = 2
const DEBOUNCE_MS = 300

const props = defineProps<{
  conceptosExcluidos: number[]
  saving?: boolean
}>()

const emit = defineEmits<{
  (e: 'add', item: ConceptoCatalogItem): void
}>()

// El catálogo de conceptos es grande: en vez de traerlo completo, se busca bajo
// demanda contra el backend (con debounce) a medida que el usuario tipea.
const { conceptosDisponibles, loadingConceptos, fetchConceptos } = useConfiguration()

const isOpen = ref(false)
const query = ref('')
let debounceHandle: ReturnType<typeof setTimeout> | undefined

watch(query, (value) => {
  if (debounceHandle) clearTimeout(debounceHandle)

  const trimmed = value.trim()
  if (trimmed.length > 0 && trimmed.length < MIN_QUERY_LENGTH) {
    return
  }

  debounceHandle = setTimeout(() => {
    fetchConceptos(trimmed)
  }, DEBOUNCE_MS)
})

// Al abrir el popup precargamos los primeros conceptos (igual que valores fijos/por
// categoría) en vez de mostrar la grilla vacía hasta que el usuario tipee.
watch(isOpen, (open) => {
  if (open) {
    fetchConceptos(query.value.trim())
  }
})

const excludedSet = computed(() => new Set(props.conceptosExcluidos))

const filteredItems = computed(() =>
  conceptosDisponibles.value.filter((item) => !excludedSet.value.has(item.id)),
)

const emptyMessage = computed(() =>
  loadingConceptos.value ? 'Buscando...' : 'No se encontraron conceptos para ese criterio de búsqueda.',
)

function handleAdd(item: ConceptoCatalogItem) {
  emit('add', item)
}

function handleClose() {
  isOpen.value = false
  query.value = ''
}
</script>

<template>
  <div>
    <Button label="Agregar concepto" icon="pi pi-plus" severity="secondary" @click="isOpen = true" />

    <Dialog
      v-model:visible="isOpen"
      header="Agregar concepto"
      :modal="true"
      :style="{ width: '52rem' }"
      :closable="true"
      @hide="query = ''"
    >
      <div class="flex flex-column gap-3">
        <InputText
          v-model="query"
          placeholder="Buscar por código, subcódigo o descripción..."
          class="w-full"
          autofocus
        />

        <DataTable
          :value="filteredItems"
          scrollable
          scroll-height="320px"
          striped-rows
          :sort-field="'codigo'"
          :sort-order="1"
          :virtual-scroller-options="{ itemSize: 46 }"
          :loading="loadingConceptos"
        >
          <template #empty>
            <span class="muted">{{ emptyMessage }}</span>
          </template>
          <Column field="codigo" header="Código" sortable style="width: 8rem; text-align: right" />
          <Column field="subcodigo" header="Subcódigo" sortable style="width: 8rem; text-align: right" />
          <Column field="descripcionBreve" header="Descripción Breve" sortable />
          <Column field="descripcion" header="Descripción" sortable />
          <Column style="width: 4rem">
            <template #body="{ data }">
              <Button
                icon="pi pi-plus"
                size="small"
                rounded
                severity="success"
                :disabled="saving"
                @click="handleAdd(data)"
              />
            </template>
          </Column>
        </DataTable>
      </div>

      <template #footer>
        <Button label="Cerrar" severity="secondary" @click="handleClose" />
      </template>
    </Dialog>
  </div>
</template>
