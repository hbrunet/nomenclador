<script setup lang="ts">
import { computed, ref } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import ValorCategoriaItemsModal from './ValorCategoriaItemsModal.vue'
import ValorCategoriaCombobox from './ValorCategoriaCombobox.vue'
import type { ValorCategoriaCatalogItem, ValorCategoriaConfiguradoInputDto } from '../types/configuration'

const valoresCategorias = defineModel<ValorCategoriaConfiguradoInputDto[]>({ required: true })

const props = defineProps<{
  valoresDisponibles: ValorCategoriaCatalogItem[]
}>()

const selectedItemIndex = ref<number | null>(null)
const modalRef = ref<InstanceType<typeof ValorCategoriaItemsModal> | null>(null)
const filterQuery = ref('')

const valuesById = computed(() => new Map(props.valoresDisponibles.map((item) => [item.id, item])))
const valoresExcluidos = computed(() => valoresCategorias.value.map((item) => item.idValorCategoria))

const tableData = computed(() => {
  const q = filterQuery.value.toLowerCase().trim()
  return valoresCategorias.value
    .filter((item) => {
      const cat = valuesById.value.get(item.idValorCategoria)
      const matchesQuery =
        !q ||
        (cat?.descripcion ?? '').toLowerCase().includes(q) ||
        (cat?.tipo ?? '').toLowerCase().includes(q)
      return matchesQuery
    })
    .map((item) => ({
      idValorCategoria: item.idValorCategoria,
      tipo: valuesById.value.get(item.idValorCategoria)?.tipo ?? 'N/D',
      descripcion: valuesById.value.get(item.idValorCategoria)?.descripcion ?? 'Valor sin catálogo',
    }))
})

const selectedItem = computed(() =>
  selectedItemIndex.value !== null ? (valoresCategorias.value[selectedItemIndex.value] ?? null) : null,
)

const selectedDescripcion = computed(() =>
  selectedItem.value
    ? (valuesById.value.get(selectedItem.value.idValorCategoria)?.descripcion ?? 'Valor sin catálogo')
    : '',
)

const selectedTipo = computed(() =>
  selectedItem.value
    ? (valuesById.value.get(selectedItem.value.idValorCategoria)?.tipo ?? 'N/D')
    : '',
)

function addValorCategoria(id: number) {
  if (valoresCategorias.value.some((item) => item.idValorCategoria === id)) return
  valoresCategorias.value = [...valoresCategorias.value, { idValorCategoria: id, items: [] }]
}

function removeValorCategoria(idValorCategoria: number) {
  valoresCategorias.value = valoresCategorias.value.filter(
    (item) => item.idValorCategoria !== idValorCategoria,
  )
}

function verItems(idValorCategoria: number) {
  const index = valoresCategorias.value.findIndex(
    (item) => item.idValorCategoria === idValorCategoria,
  )
  selectedItemIndex.value = index
  modalRef.value?.open(idValorCategoria)
}
</script>

<template>
  <div class="flex flex-column gap-3 pt-3">
    <ValorCategoriaCombobox
      :valores-disponibles="valoresDisponibles"
      :valores-excluidos="valoresExcluidos"
      @add="addValorCategoria"
    />

    <div class="flex flex-column gap-1" style="max-width: 400px">
      <InputText v-model="filterQuery" placeholder="Buscar por descripción o tipo..." class="w-full" />
    </div>

    <DataTable :value="tableData" striped-rows :sort-field="'descripcion'" :sort-order="1">
      <template #empty>
        <span class="muted">
          {{ valoresCategorias.length ? 'Sin resultados para el filtro aplicado.' : 'No hay valores por categoría configurados.' }}
        </span>
      </template>
      <Column field="idValorCategoria" header="ID" sortable style="text-align: right" />
      <Column field="descripcion" header="Descripción" sortable />
      <Column field="tipo" header="Tipo" sortable />
      <Column>
        <template #body="{ data }">
          <div class="flex gap-1 align-items-center">
            <Button
              label="Ver items"
              icon="pi pi-list"
              size="small"
              severity="secondary"
              outlined
              @click="verItems(data.idValorCategoria)"
            />
            <Button
              icon="pi pi-trash"
              size="small"
              severity="danger"
              text
              rounded
              @click="removeValorCategoria(data.idValorCategoria)"
            />
          </div>
        </template>
      </Column>
    </DataTable>

    <ValorCategoriaItemsModal
      ref="modalRef"
      :item="selectedItem"
      :descripcion="selectedDescripcion"
      :tipo="selectedTipo"
    />
  </div>
</template>
