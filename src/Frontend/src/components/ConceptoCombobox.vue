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
  (e: 'add', id: number): void
}>()

const isOpen = ref(false)
const query = ref('')

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

function handleAdd(id: number) {
  emit('add', id)
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
          :loading="loadingCatalog"
        >
          <template #empty>
            <span class="muted">No hay conceptos disponibles para agregar.</span>
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
                @click="handleAdd(data.id)"
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
