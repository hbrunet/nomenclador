<script setup lang="ts">
import { computed, ref } from 'vue'
import Dialog from 'primevue/dialog'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import type { ValorFijoCatalogItem } from '../types/configuration'

const props = defineProps<{
  valoresDisponibles: ValorFijoCatalogItem[]
  valoresExcluidos: number[]
}>()

const emit = defineEmits<{
  (e: 'add', id: number): void
}>()

const isOpen = ref(false)
const query = ref('')

const excludedSet = computed(() => new Set(props.valoresExcluidos))

const filteredItems = computed(() => {
  const q = query.value.toLowerCase().trim()
  return props.valoresDisponibles
    .filter((item) => !excludedSet.value.has(item.id))
    .filter(
      (item) =>
        !q ||
        (item.descripcion ?? '').toLowerCase().includes(q) ||
        (item.tipo ?? '').toLowerCase().includes(q),
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
    <Button
      label="Agregar valor fijo"
      icon="pi pi-plus"
      severity="secondary"
      @click="isOpen = true"
    />

    <Dialog
      v-model:visible="isOpen"
      header="Agregar valor fijo"
      :modal="true"
      :style="{ width: '42rem' }"
      :closable="true"
      @hide="query = ''"
    >
      <div class="flex flex-column gap-3">
        <InputText
          v-model="query"
          placeholder="Buscar por descripción o tipo..."
          class="w-full"
          autofocus
        />

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
          <Column field="tipo" header="Tipo" sortable style="width: 10rem" />
          <Column header="Valor" sortable sort-field="valor" style="width: 9rem; text-align: right">
            <template #body="{ data }">
              {{ (data.valor ?? 0).toLocaleString('es-AR', { minimumFractionDigits: 2 }) }}
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
