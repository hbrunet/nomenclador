<script setup lang="ts">
import { computed, ref } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import type {
  ConfiguracionNomencladorDetailDto,
  ValorFijoCatalogItem,
  ValorFijoConfiguradoInputDto,
} from '../types/configuration'
import { configurationService } from '../services/configurationService'
import ValorFijoEditModal from './ValorFijoEditModal.vue'
import ValorFijoCombobox from './ValorFijoCombobox.vue'
import SustituirValorFijoModal from './SustituirValorFijoModal.vue'

const valoresFijos = defineModel<ValorFijoConfiguradoInputDto[]>({ required: true })

const props = defineProps<{
  valoresDisponibles: ValorFijoCatalogItem[]
  configuracionId?: number
}>()

const emit = defineEmits<{
  (e: 'catalog-refresh'): void
  (e: 'detail-updated', detail: ConfiguracionNomencladorDetailDto): void
}>()

const confirm = useConfirm()
const toast = useToast()
const editModalRef = ref<InstanceType<typeof ValorFijoEditModal> | null>(null)
const sustituirModalRef = ref<InstanceType<typeof SustituirValorFijoModal> | null>(null)
const descFilter = ref('')
const tipoFilter = ref('')
const saving = ref(false)
const removingIds = ref<Set<number>>(new Set())
const errorMessage = ref<string | null>(null)
const selectedRows = ref<{ idValorFijo: number; idTipo: number; tipo: string }[]>([])

const valuesById = computed(() => new Map(props.valoresDisponibles.map((item) => [item.id, item])))
const valoresExcluidos = computed(() => valoresFijos.value.map((item) => item.idValorFijo))

const tableData = computed(() => {
  const descQ = descFilter.value.toLowerCase().trim()
  const tipoQ = tipoFilter.value.toLowerCase().trim()
  return valoresFijos.value
    .filter((item) => {
      const cat = valuesById.value.get(item.idValorFijo)
      const matchesQuery =
        (!descQ || (cat?.descripcion ?? '').toLowerCase().includes(descQ)) &&
        (!tipoQ || (cat?.tipo ?? '').toLowerCase().includes(tipoQ) || (cat?.idTipo.toString() ?? '').toLowerCase().includes(tipoQ))
      return matchesQuery
    })
    .map((item) => ({
      idValorFijo: item.idValorFijo,
      idTipo: valuesById.value.get(item.idValorFijo)?.idTipo,
      tipo: valuesById.value.get(item.idValorFijo)?.tipo ?? 'N/D',
      descripcion: valuesById.value.get(item.idValorFijo)?.descripcion ?? 'Valor fijo',
      valor: valuesById.value.get(item.idValorFijo)?.valor,
    }))
})

const cantidadValoresFijos = computed(() => tableData.value.length)

async function addValorFijo(id: number) {
  if (valoresFijos.value.some((item) => item.idValorFijo === id)) return

  errorMessage.value = null

  if (!props.configuracionId) {
    // Configuración aún no persistida: se agrega solo al borrador local.
    valoresFijos.value = [...valoresFijos.value, { idValorFijo: id, valor: 0 }]
    return
  }

  saving.value = true
  try {
    const updated = await configurationService.addValorFijo(props.configuracionId, { idValorFijo: id, valor: 0 })
    valoresFijos.value = updated.valoresFijos.map((item) => ({ idValorFijo: item.idValorFijo, valor: item.valor }))
    emit('detail-updated', updated)
  } catch (e: any) {
    errorMessage.value = e.response?.data?.mensaje ?? 'No se pudo agregar el valor fijo seleccionado.'
  } finally {
    saving.value = false
  }
}

async function removeValorFijo(idValorFijo: number) {
  errorMessage.value = null

  if (!props.configuracionId) {
    // Configuración aún no persistida: se quita solo del borrador local.
    valoresFijos.value = valoresFijos.value.filter((item) => item.idValorFijo !== idValorFijo)
    return
  }

  removingIds.value.add(idValorFijo)
  try {
    const updated = await configurationService.removeValorFijo(props.configuracionId, idValorFijo)
    valoresFijos.value = updated.valoresFijos.map((item) => ({ idValorFijo: item.idValorFijo, valor: item.valor }))
    emit('detail-updated', updated)
    toast.add({ severity: 'success', summary: 'Valor fijo eliminado', detail: 'El valor fijo se quitó de la configuración.', life: 2500 })
  } catch (e: any) {
    errorMessage.value = e.response?.data?.mensaje ?? 'No se pudo eliminar el valor fijo seleccionado.'
  } finally {
    removingIds.value.delete(idValorFijo)
  }
}

function confirmRemoveValorFijo(idValorFijo: number) {
  const descripcion = valuesById.value.get(idValorFijo)?.descripcion ?? `#${idValorFijo}`
  confirm.require({
    message: `¿Eliminar el valor fijo "${descripcion}" de esta configuración?`,
    header: 'Confirmar eliminación',
    icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'Eliminar',
    acceptProps: { severity: 'danger' },
    rejectLabel: 'Cancelar',
    rejectProps: { severity: 'secondary', outlined: true },
    accept: () => removeValorFijo(idValorFijo),
  })
}

function openEditModal(idValorFijo: number) {
  const catalogItem = valuesById.value.get(idValorFijo)
  if (catalogItem) editModalRef.value?.open(catalogItem)
}

function openSustituirModal() {
  if (!props.configuracionId || selectedRows.value.length === 0) return
  const rows = selectedRows.value.map((row) => ({
    idValorFijo: row.idValorFijo,
    idTipo: row.idTipo,
    tipo: row.tipo,
  }))
  sustituirModalRef.value?.open(rows, props.configuracionId)
}

function handleSubstituted(detail: ConfiguracionNomencladorDetailDto) {
  valoresFijos.value = detail.valoresFijos.map((item) => ({ idValorFijo: item.idValorFijo, valor: item.valor }))
  emit('detail-updated', detail)
  selectedRows.value = []
  descFilter.value = ''
  tipoFilter.value = ''
}

async function handleModalSaved(
  payload:
    | { mode: 'updated'; item: ValorFijoCatalogItem }
    | { mode: 'replaced'; oldId: number; newItem: ValorFijoCatalogItem },
) {
  if (payload.mode === 'replaced') {
    valoresFijos.value = valoresFijos.value.map((item) =>
      item.idValorFijo === payload.oldId ? { ...item, idValorFijo: payload.newItem.id } : item,
    )

    if (props.configuracionId) {
      // El nuevo valor ya quedó asociado en el backend al crearse; hay que quitar la
      // asociación vieja explícitamente o queda huérfana en la configuración.
      try {
        const updated = await configurationService.removeValorFijo(props.configuracionId, payload.oldId)
        valoresFijos.value = updated.valoresFijos.map((item) => ({ idValorFijo: item.idValorFijo, valor: item.valor }))
        emit('detail-updated', updated)
      } catch (e: any) {
        errorMessage.value = e.response?.data?.mensaje ?? 'No se pudo quitar el valor fijo anterior de la configuración.'
      }
    }
  }
  emit('catalog-refresh')
}

const virtualScrollerOptions = computed(() =>
  tableData.value.length > 150 ? { itemSize: 46 } : undefined,
)
</script>

<template>
  <div class="flex flex-column gap-3 pt-3">
    <Message v-if="errorMessage" severity="error" :closable="false">{{ errorMessage }}</Message>

    <ValorFijoCombobox :valores-disponibles="valoresDisponibles" :valores-excluidos="valoresExcluidos"
      @add="addValorFijo" />

    <div class="flex flex-row gap-2 align-items-center">
      <InputText v-model="tipoFilter" placeholder="Filtrar por tipo..." />
      <InputText v-model="descFilter" placeholder="Filtrar por descripción..." />
      <Button
        label="Sustituir"
        icon="pi pi-sync"
        severity="primary"
        :disabled="selectedRows.length === 0 || !props.configuracionId"
        :title="!props.configuracionId ? 'Guardá la configuración antes de sustituir valores fijos.' : undefined"
        @click="openSustituirModal"
      />
    </div>

    <DataTable
      v-model:selection="selectedRows"
      :value="tableData"
      data-key="idValorFijo"
      striped-rows
      :sort-field="'idTipo'"
      :sort-order="1"
      scrollable
      scroll-height="600px"
      :virtual-scroller-options="virtualScrollerOptions"
    >
      <Column selection-mode="multiple" header-style="width: 3rem" />
      <template #empty>
        <span class="muted">
          {{ valoresFijos.length ? 'Sin resultados para el filtro aplicado.' : 'No hay valores fijos configurados.' }}
        </span>
      </template>
      <Column field="idValorFijo" header="ID" sortable style="text-align: right" />
      <Column field="idTipo" header="Tipo" sortable>
        <template #body="{ data }">
          {{ data.idTipo }} - {{ data.tipo }}
        </template>
      </Column>
      <Column field="descripcion" header="Descripción" sortable />
      <Column header="Valor" style="text-align: right">
        <template #body="{ data }">
          {{ data.valor?.toLocaleString('es-AR', { minimumFractionDigits: 2 }) ?? '—' }}
        </template>
      </Column>
      <Column style="width: 10rem">
        <template #body="{ data }">
          <div class="flex gap-1 align-items-center">
            <Button label="Editar" icon="pi pi-pencil" size="small" severity="secondary" outlined
              @click="openEditModal(data.idValorFijo)" />
            <Button icon="pi pi-trash" size="small" severity="danger" text rounded
              :loading="removingIds.has(data.idValorFijo)" :disabled="removingIds.has(data.idValorFijo)"
              @click="confirmRemoveValorFijo(data.idValorFijo)" />
          </div>
        </template>
      </Column>
    </DataTable>
    <p class="muted" style="text-align: right;">
      Cantidad de items: {{ cantidadValoresFijos }}
    </p>
    <ValorFijoEditModal ref="editModalRef" :configuracion-id="props.configuracionId" @saved="handleModalSaved" />
    <SustituirValorFijoModal ref="sustituirModalRef" @substituted="handleSubstituted" />
  </div>
</template>
