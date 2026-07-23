<script setup lang="ts">
import { computed, ref } from 'vue'
import Dialog from 'primevue/dialog'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import type { ConceptoCatalogItem } from '../types/configuration'

const props = defineProps<{
  conceptosDisponibles: ConceptoCatalogItem[]
  conceptosExcluidos: number[]
  loadingCatalog?: boolean
}>()

const emit = defineEmits<{
  (e: 'add-multiple', ids: number[]): void
}>()

const isOpen = ref(false)
const query = ref('')
const selectedItems = ref<ConceptoCatalogItem[]>([])

const excludedSet = computed(() => new Set(props.conceptosExcluidos))

const filteredItems = computed(() => {
  const q = query.value.toLowerCase().trim()
  return props.conceptosDisponibles
    .filter((item) => !excludedSet.value.has(item.id))
    .filter(
      (item) =>
        !q ||
        item.codigo?.toLowerCase().includes(q) ||
        String(item.subcodigo).toLowerCase().includes(q) ||
        (item.descripcionBreve ?? '').toLowerCase().includes(q) ||
        (item.descripcion ?? '').toLowerCase().includes(q),
    )
})

function handleOpen() {
  selectedItems.value = []
  query.value = ''
  isOpen.value = true
}

function handleAddSelected() {
  if (!selectedItems.value.length) return
  emit(
    'add-multiple',
    selectedItems.value.map((item) => item.id),
  )
  selectedItems.value = []
  isOpen.value = false
  query.value = ''
}

function handleClose() {
  isOpen.value = false
  selectedItems.value = []
  query.value = ''
}
</script>

<template>
  <div>
    <Button label="Agregar concepto" icon="pi pi-plus" severity="secondary" @click="handleOpen" />

    <Dialog
      v-model:visible="isOpen"
      header="Agregar concepto"
      :modal="true"
      :style="{ width: '52rem' }"
      :closable="true"
      @hide="handleClose"
    >
      <div class="flex flex-column gap-3">
        <InputText
          v-model="query"
          placeholder="Buscar por código, subcódigo o descripción..."
          class="w-full"
          autofocus
        />

        <DataTable
          v-model:selection="selectedItems"
          :value="filteredItems"
          data-key="id"
          scrollable
          scroll-height="320px"
          striped-rows
          :sort-field="'codigo'"
          :sort-order="1"
          :virtual-scroller-options="{ itemSize: 46 }"
          :loading="loadingCatalog"
        >
          <template #empty>
            <span class="muted">No hay conceptos disponibles para agregar.</span>
          </template>
          <Column selection-mode="multiple" header-style="width: 3rem" />
          <Column field="codigo" header="Código" sortable style="width: 8rem; text-align: right" />
          <Column field="subcodigo" header="Subcódigo" sortable style="width: 8rem; text-align: right" />
          <Column field="descripcionBreve" header="Descripción Breve" sortable />
          <Column field="descripcion" header="Descripción" sortable />
        </DataTable>
      </div>

      <template #footer>
        <Button label="Cerrar" severity="secondary" @click="handleClose" />
        <Button
          :label="`Agregar seleccionados${selectedItems.length ? ' (' + selectedItems.length + ')' : ''}`"
          icon="pi pi-check"
          :disabled="!selectedItems.length"
          @click="handleAddSelected"
        />
      </template>
    </Dialog>
  </div>
</template>
