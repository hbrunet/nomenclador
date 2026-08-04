<script setup lang="ts">
import { computed, ref } from 'vue'
import Button from 'primevue/button'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Message from 'primevue/message'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import type {
  ConceptoCatalogItem,
  ConceptoConfiguradoInputDto,
  ConceptoConfiguradoViewModel,
  ConfiguracionNomencladorDetailDto,
} from '../types/configuration'
import { configurationService } from '../services/configurationService'
import ConceptoCombobox from './ConceptoCombobox.vue'

const confirm = useConfirm()
const toast = useToast()

const conceptos = defineModel<ConceptoConfiguradoInputDto[]>({ required: true })

const props = defineProps<{
  // Conceptos ya resueltos por el backend (codigo/descripcion), provenientes del
  // detalle de la configuración. Para una configuración nueva (sin guardar) viene vacío;
  // en ese caso usamos localResolved (ver abajo) con lo que el usuario fue agregando.
  conceptosResueltos: ConceptoConfiguradoViewModel[]
  configuracionId?: number
}>()

const emit = defineEmits<{
  (e: 'detail-updated', detail: ConfiguracionNomencladorDetailDto): void
}>()

const saving = ref(false)
const removingIds = ref<Set<number>>(new Set())
const errorMessage = ref<string | null>(null)

// Cache local de conceptos agregados en esta sesión de edición, capturados directamente
// desde los resultados de búsqueda del combobox. Evita depender del catálogo completo
// (grande) solo para mostrar código/descripción de lo que ya se seleccionó.
const localResolved = ref<Map<number, ConceptoCatalogItem>>(new Map())

const resolvedLookup = computed(() => {
  const map = new Map<number, { codigo: string; subcodigo: number; descripcion: string; descripcionBreve: string }>()
  for (const item of props.conceptosResueltos) {
    map.set(item.idConcepto, item)
  }
  for (const [id, item] of localResolved.value) {
    if (!map.has(id)) {
      map.set(id, item)
    }
  }
  return map
})

const conceptosExcluidos = computed(() => conceptos.value.map((item) => item.idConcepto))

const tableData = computed(() =>
  conceptos.value.map((item) => {
    const resolved = resolvedLookup.value.get(item.idConcepto)
    return {
      idConcepto: item.idConcepto,
      codigo: resolved?.codigo ?? String(item.idConcepto),
      subcodigo: resolved && resolved.codigo !== 'N/D' ? resolved.subcodigo : 'N/D',
      descripcion: resolved?.descripcion ?? 'N/D',
    }
  })
)

async function addConcepto(item: ConceptoCatalogItem) {
  const idConcepto = item.id
  if (conceptos.value.some((c) => c.idConcepto === idConcepto)) return

  errorMessage.value = null
  localResolved.value.set(idConcepto, item)

  if (!props.configuracionId) {
    // Configuración aún no persistida: se agrega solo al borrador local.
    conceptos.value = [...conceptos.value, { idConcepto, orden: conceptos.value.length + 1 }]
    return
  }

  saving.value = true
  try {
    const updated = await configurationService.addConcepto(props.configuracionId, {
      idConcepto,
      orden: conceptos.value.length + 1,
    })
    conceptos.value = updated.conceptos.map((c) => ({ idConcepto: c.idConcepto, orden: c.orden }))
    emit('detail-updated', updated)
  } catch (e: any) {
    errorMessage.value = e.response?.data?.mensaje ?? 'No se pudo agregar el concepto seleccionado.'
  } finally {
    saving.value = false
  }
}

async function removeConcepto(idConcepto: number) {
  errorMessage.value = null

  if (!props.configuracionId) {
    // Configuración aún no persistida: se quita solo del borrador local.
    conceptos.value = conceptos.value
      .filter((item) => item.idConcepto !== idConcepto)
      .map((item, index) => ({ ...item, orden: index + 1 }))
    return
  }

  removingIds.value.add(idConcepto)
  try {
    const updated = await configurationService.removeConcepto(props.configuracionId, idConcepto)
    conceptos.value = updated.conceptos.map((item) => ({ idConcepto: item.idConcepto, orden: item.orden }))
    emit('detail-updated', updated)
    toast.add({ severity: 'success', summary: 'Concepto eliminado', detail: 'El concepto se quitó de la configuración.', life: 2500 })
  } catch (e: any) {
    errorMessage.value = e.response?.data?.mensaje ?? 'No se pudo eliminar el concepto seleccionado.'
  } finally {
    removingIds.value.delete(idConcepto)
  }
}

function confirmRemoveConcepto(idConcepto: number) {
  const descripcion = resolvedLookup.value.get(idConcepto)?.descripcion ?? `#${idConcepto}`
  confirm.require({
    message: `¿Eliminar el concepto "${descripcion}" de esta configuración?`,
    header: 'Confirmar eliminación',
    icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'Eliminar',
    acceptProps: { severity: 'danger' },
    rejectLabel: 'Cancelar',
    rejectProps: { severity: 'secondary', outlined: true },
    accept: () => removeConcepto(idConcepto),
  })
}
</script>

<template>
  <div class="flex flex-column gap-3 pt-3">
    <Message v-if="errorMessage" severity="error" :closable="false">{{ errorMessage }}</Message>

    <ConceptoCombobox
      :conceptos-excluidos="conceptosExcluidos"
      :saving="saving"
      @add="addConcepto"
    />

    <DataTable
      :value="tableData"
      striped-rows
      scrollable
      scroll-height="600px"
      :virtual-scroller-options="{ itemSize: 46 }"
    >
      <template #empty>
        <span class="muted">Agregue conceptos desde el catálogo.</span>
      </template>
      <Column field="codigo" header="Código" style="text-align: right" />
      <Column field="subcodigo" header="Subcódigo" style="text-align: right" />
      <Column field="descripcionBreve" header="Descripción Breve" />
      <Column field="descripcion" header="Descripción" />
      <Column>
        <template #body="{ data }">
          <Button
            icon="pi pi-trash"
            severity="danger"
            text
            rounded
            size="small"
            :loading="removingIds.has(data.idConcepto)"
            :disabled="removingIds.has(data.idConcepto)"
            @click="confirmRemoveConcepto(data.idConcepto)"
          />
        </template>
      </Column>
    </DataTable>
  </div>
</template>
