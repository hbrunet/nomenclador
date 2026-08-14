<script setup lang="ts">
import { computed, ref } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import ValorCategoriaItemsModal from './ValorCategoriaItemsModal.vue'
import ValorCategoriaCombobox from './ValorCategoriaCombobox.vue'
import { configurationService } from '../services/configurationService'
import type {
  ConfiguracionNomencladorDetailDto,
  ValorCategoriaCatalogItem,
  ValorCategoriaConfiguradoInputDto,
} from '../types/configuration'

const valoresCategorias = defineModel<ValorCategoriaConfiguradoInputDto[]>({ required: true })

const props = defineProps<{
  valoresDisponibles: ValorCategoriaCatalogItem[]
  configuracionId?: number
}>()

const emit = defineEmits<{
  (e: 'detail-updated', detail: ConfiguracionNomencladorDetailDto): void
}>()

const confirm = useConfirm()
const toast = useToast()
const selectedItemIndex = ref<number | null>(null)
const modalRef = ref<InstanceType<typeof ValorCategoriaItemsModal> | null>(null)
const filterQuery = ref('')
const saving = ref(false)
const removingIds = ref<Set<number>>(new Set())
const errorMessage = ref<string | null>(null)

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
        (cat?.tipo ?? '').toLowerCase().includes(q) ||
        (cat?.idTipo.toString() ?? '').toLowerCase().includes(q)
      return matchesQuery
    })
    .map((item) => ({
      idValorCategoria: item.idValorCategoria,
      tipo: valuesById.value.get(item.idValorCategoria)?.tipo ?? 'N/D',
      descripcion: valuesById.value.get(item.idValorCategoria)?.descripcion ?? 'Valor sin catálogo',
      idTipo: valuesById.value.get(item.idValorCategoria)?.idTipo ?? 0,
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

async function addValorCategoria(id: number) {
  if (saving.value) return
  if (valoresCategorias.value.some((item) => item.idValorCategoria === id)) return

  errorMessage.value = null

  if (!props.configuracionId) {
    // Configuración aún no persistida: se agrega solo al borrador local.
    valoresCategorias.value = [...valoresCategorias.value, { idValorCategoria: id, items: [] }]
    return
  }

  saving.value = true
  try {
    const updated = await configurationService.addValorPorCategoria(props.configuracionId, { idValorCategoria: id, items: [] })
    valoresCategorias.value = updated.valoresCategorias.map((item) => ({
      idValorCategoria: item.idValorCategoria,
      items: item.items.map((i) => ({ id: i.id, numeroCategoria: i.numeroCategoria, importe: i.importe })),
    }))
    emit('detail-updated', updated)
  } catch (e: any) {
    errorMessage.value = e.response?.data?.mensaje ?? 'No se pudo agregar el valor por categoría seleccionado.'
  } finally {
    saving.value = false
  }
}

async function removeValorCategoria(idValorCategoria: number) {
  errorMessage.value = null

  if (!props.configuracionId) {
    // Configuración aún no persistida: se quita solo del borrador local.
    valoresCategorias.value = valoresCategorias.value.filter(
      (item) => item.idValorCategoria !== idValorCategoria,
    )
    return
  }

  removingIds.value.add(idValorCategoria)
  try {
    const updated = await configurationService.removeValorPorCategoria(props.configuracionId, idValorCategoria)
    valoresCategorias.value = updated.valoresCategorias.map((item) => ({
      idValorCategoria: item.idValorCategoria,
      items: item.items.map((i) => ({ id: i.id, numeroCategoria: i.numeroCategoria, importe: i.importe })),
    }))
    emit('detail-updated', updated)
    toast.add({ severity: 'success', summary: 'Valor por categoría eliminado', detail: 'El valor por categoría se quitó de la configuración.', life: 2500 })
  } catch (e: any) {
    errorMessage.value = e.response?.data?.mensaje ?? 'No se pudo eliminar el valor por categoría seleccionado.'
  } finally {
    removingIds.value.delete(idValorCategoria)
  }
}

function confirmRemoveValorCategoria(idValorCategoria: number) {
  const descripcion = valuesById.value.get(idValorCategoria)?.descripcion ?? `#${idValorCategoria}`
  confirm.require({
    message: `¿Eliminar el valor por categoría "${descripcion}" de esta configuración?`,
    header: 'Confirmar eliminación',
    icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'Eliminar',
    acceptProps: { severity: 'danger' },
    rejectLabel: 'Cancelar',
    rejectProps: { severity: 'secondary', outlined: true },
    accept: () => removeValorCategoria(idValorCategoria),
  })
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
    <Message v-if="errorMessage" severity="error" :closable="false">{{ errorMessage }}</Message>

    <ValorCategoriaCombobox
      :valores-disponibles="valoresDisponibles"
      :valores-excluidos="valoresExcluidos"
      @add="addValorCategoria"
    />

    <div class="flex flex-column gap-1" style="max-width: 400px">
      <InputText v-model="filterQuery" placeholder="Buscar por descripción o tipo..." class="w-full" />
    </div>

    <DataTable
      :value="tableData"
      striped-rows
      :sort-field="'descripcion'"
      :sort-order="1"
      scrollable
      scroll-height="600px"
      :virtual-scroller-options="{ itemSize: 46 }"
    >
      <template #empty>
        <span class="muted">
          {{ valoresCategorias.length ? 'Sin resultados para el filtro aplicado.' : 'No hay valores por categoría configurados.' }}
        </span>
      </template>
      <Column field="idValorCategoria" header="ID" sortable style="text-align: right" />
      <Column field="descripcion" header="Descripción" sortable />
      <Column field="tipo" header="Tipo" sortable >
        <template #body="{ data }">
          {{ data.idTipo }} - {{ data.tipo }}
        </template>
      </Column>
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
              :loading="removingIds.has(data.idValorCategoria)"
              :disabled="removingIds.has(data.idValorCategoria)"
              @click="confirmRemoveValorCategoria(data.idValorCategoria)"
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
