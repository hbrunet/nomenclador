<script setup lang="ts">
import { computed, ref } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import type { ValorFijoCatalogItem, ValorFijoConfiguradoInputDto } from '../types/configuration'
import ValorFijoEditModal from './ValorFijoEditModal.vue'
import ValorFijoCombobox from './ValorFijoCombobox.vue'

const valoresFijos = defineModel<ValorFijoConfiguradoInputDto[]>({ required: true })

const props = defineProps<{
  valoresDisponibles: ValorFijoCatalogItem[]
  configuracionId?: number
}>()

const emit = defineEmits<{
  (e: 'catalog-refresh'): void
}>()

const editModalRef = ref<InstanceType<typeof ValorFijoEditModal> | null>(null)
const filterQuery = ref('')

const valuesById = computed(() => new Map(props.valoresDisponibles.map((item) => [item.id, item])))
const valoresExcluidos = computed(() => valoresFijos.value.map((item) => item.idValorFijo))

const tableData = computed(() => {
  const q = filterQuery.value.toLowerCase().trim()
  return valoresFijos.value
    .filter((item) => {
      const cat = valuesById.value.get(item.idValorFijo)
      const matchesQuery =
        !q ||
        (cat?.descripcion ?? '').toLowerCase().includes(q) ||
        (cat?.tipo ?? '').toLowerCase().includes(q)
      return matchesQuery
    })
    .map((item) => ({
      idValorFijo: item.idValorFijo,
      tipo: valuesById.value.get(item.idValorFijo)?.tipo ?? 'N/D',
      descripcion: valuesById.value.get(item.idValorFijo)?.descripcion ?? 'Valor fijo',
      valor: valuesById.value.get(item.idValorFijo)?.valor,
    }))
})

function addValorFijo(id: number) {
  if (valoresFijos.value.some((item) => item.idValorFijo === id)) return
  valoresFijos.value = [...valoresFijos.value, { idValorFijo: id, valor: 0 }]
}

function removeValorFijo(idValorFijo: number) {
  valoresFijos.value = valoresFijos.value.filter((item) => item.idValorFijo !== idValorFijo)
}

function openEditModal(idValorFijo: number) {
  const catalogItem = valuesById.value.get(idValorFijo)
  if (catalogItem) editModalRef.value?.open(catalogItem)
}

function handleModalSaved(
  payload:
    | { mode: 'updated'; item: ValorFijoCatalogItem }
    | { mode: 'replaced'; oldId: number; newItem: ValorFijoCatalogItem },
) {
  if (payload.mode === 'replaced') {
    valoresFijos.value = valoresFijos.value.map((item) =>
      item.idValorFijo === payload.oldId ? { ...item, idValorFijo: payload.newItem.id } : item,
    )
  }
  emit('catalog-refresh')
}
</script>

<template>
  <div class="flex flex-column gap-3 pt-3">
    <ValorFijoCombobox
      :valores-disponibles="valoresDisponibles"
      :valores-excluidos="valoresExcluidos"
      @add="addValorFijo"
    />

    <div class="flex flex-column gap-1" style="max-width: 400px">
      <InputText v-model="filterQuery" placeholder="Buscar por descripción o tipo..." class="w-full" />
    </div>

    <DataTable :value="tableData" striped-rows :sort-field="'descripcion'" :sort-order="1">
      <template #empty>
        <span class="muted">
          {{ valoresFijos.length ? 'Sin resultados para el filtro aplicado.' : 'No hay valores fijos configurados.' }}
        </span>
      </template>
      <Column field="idValorFijo" header="ID" sortable style="text-align: right"/>
       <Column field="descripcion" header="Descripción" sortable />
      <Column field="tipo" header="Tipo" sortable />
      <Column header="Valor" style="text-align: right">
        <template #body="{ data }">
          {{ data.valor?.toLocaleString('es-AR', { minimumFractionDigits: 2 }) ?? '—' }}
        </template>
      </Column>
      <Column style="width: 10rem">
        <template #body="{ data }">
          <div class="flex gap-1 align-items-center">
            <Button
              label="Editar"
              icon="pi pi-pencil"
              size="small"
              severity="secondary"
              outlined
              @click="openEditModal(data.idValorFijo)"
            />
            <Button
              icon="pi pi-trash"
              size="small"
              severity="danger"
              text
              rounded
              @click="removeValorFijo(data.idValorFijo)"
            />
          </div>
        </template>
      </Column>
    </DataTable>

    <ValorFijoEditModal ref="editModalRef" :configuracion-id="props.configuracionId" @saved="handleModalSaved" />
  </div>
</template>
