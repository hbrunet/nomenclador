<script setup lang="ts">
import { computed, ref } from 'vue'
import Dialog from 'primevue/dialog'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import type { ValorCategoriaCatalogItem } from '../types/configuration'

const props = defineProps<{
  valoresDisponibles: ValorCategoriaCatalogItem[]
  valoresExcluidos: number[]
}>()

const emit = defineEmits<{
  (e: 'add', id: number): void
}>()

const isOpen = ref(false)
const descFilter = ref('')
const tipoFilter = ref('')

const excludedSet = computed(() => new Set(props.valoresExcluidos))

const filteredItems = computed(() => {
  const descQ = descFilter.value.toLowerCase().trim()
  const tipoQ = tipoFilter.value.toLowerCase().trim()
  return props.valoresDisponibles
    .filter((item) => !excludedSet.value.has(item.id))
    .filter(
      (item) =>
        (!descQ || (item.descripcion ?? '').toLowerCase().includes(descQ)) &&
        (!tipoQ || (item.tipo ?? '').toLowerCase().includes(tipoQ) || (item.idTipo ?? '').toString().includes(tipoQ)),
    )
})

function handleAdd(id: number) {
  emit('add', id)
}

function handleClose() {
  isOpen.value = false
  descFilter.value = ''
  tipoFilter.value = ''
}
</script>

<template>
  <div>
    <Button
      label="Agregar valor por categoría"
      icon="pi pi-plus"
      severity="secondary"
      @click="isOpen = true"
    />

    <Dialog
      v-model:visible="isOpen"
      header="Agregar valor por categoría"
      :modal="true"
      :style="{ width: '38rem' }"
      :closable="true"
      @hide="descFilter = ''; tipoFilter = ''"
    >
      <div class="flex flex-column gap-3">

        <div class="flex gap-2">
          <InputText
            v-model="descFilter"
            placeholder="Filtrar por descripción..."
            autofocus
          />

        <InputText
            v-model="tipoFilter"
          placeholder="Filtrar por tipo..."
          autofocus
        />
        </div>

        <DataTable
          :value="filteredItems"
          scrollable
          scroll-height="320px"
          striped-rows
          :sort-field="'descripcion'"
          :sort-order="1"
          :virtual-scroller-options="{ itemSize: 46 }"
        >
          <template #empty>
            <span class="muted">No hay valores disponibles para agregar.</span>
          </template>
          <Column field="descripcion" header="Descripción" sortable />
          <Column field="tipo" header="Tipo" sortable >
            <template #body="{ data }">
              {{ data.idTipo }} - {{ data.tipo }}
            </template>
          </Column>
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
