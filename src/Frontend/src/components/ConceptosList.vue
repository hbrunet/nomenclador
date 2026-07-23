<script setup lang="ts">
import { computed, ref } from 'vue'
import Button from 'primevue/button'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Message from 'primevue/message'
import type {
  ConceptoCatalogItem,
  ConceptoConfiguradoInputDto,
  ConfiguracionNomencladorDetailDto,
} from '../types/configuration'
import { configurationService } from '../services/configurationService'
import ConceptoCombobox from './ConceptoCombobox.vue'

const conceptos = defineModel<ConceptoConfiguradoInputDto[]>({ required: true })

const props = defineProps<{
  conceptosDisponibles: ConceptoCatalogItem[]
  loadingCatalog: boolean
  configuracionId?: number
}>()

const emit = defineEmits<{
  (e: 'detail-updated', detail: ConfiguracionNomencladorDetailDto): void
}>()

const saving = ref(false)
const removingIds = ref<Set<number>>(new Set())
const errorMessage = ref<string | null>(null)

const selectedLookup = computed(
  () => new Map(props.conceptosDisponibles.map((item) => [item.id, item])),
)

const conceptosExcluidos = computed(() => conceptos.value.map((item) => item.idConcepto))

const tableData = computed(() =>
  conceptos.value.map((item) => ({
    idConcepto: item.idConcepto,
    orden: item.orden,
    codigo: selectedLookup.value.get(item.idConcepto)?.codigo ?? String(item.idConcepto),
    subcodigo: selectedLookup.value.get(item.idConcepto)?.subcodigo ?? 'N/D',
    descripcion: selectedLookup.value.get(item.idConcepto)?.descripcion ?? 'N/D',
    descripcionBreve: selectedLookup.value.get(item.idConcepto)?.descripcionBreve ?? 'N/D',
  }))
)

async function addConceptos(ids: number[]) {
  const existentes = new Set(conceptos.value.map((item) => item.idConcepto))
  const nuevos = ids.filter((id) => !existentes.has(id))
  if (!nuevos.length) return

  errorMessage.value = null

  const nuevosItems = nuevos.map((idConcepto, index) => ({
    idConcepto,
    orden: conceptos.value.length + index + 1,
  }))

  if (!props.configuracionId) {
    // Configuración aún no persistida: se agrega solo al borrador local.
    conceptos.value = [...conceptos.value, ...nuevosItems]
    return
  }

  saving.value = true
  try {
    const updated = await configurationService.addConceptos(props.configuracionId, nuevosItems)
    conceptos.value = updated.conceptos.map((item) => ({ idConcepto: item.idConcepto, orden: item.orden }))
    emit('detail-updated', updated)
  } catch (e: any) {
    errorMessage.value = e.response?.data?.mensaje ?? 'No se pudieron guardar los conceptos seleccionados.'
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
  } catch (e: any) {
    errorMessage.value = e.response?.data?.mensaje ?? 'No se pudo eliminar el concepto seleccionado.'
  } finally {
    removingIds.value.delete(idConcepto)
  }
}
</script>

<template>
  <div class="flex flex-column gap-3 pt-3">
    <Message v-if="errorMessage" severity="error" :closable="false">{{ errorMessage }}</Message>

    <ConceptoCombobox
      :conceptos-disponibles="conceptosDisponibles"
      :conceptos-excluidos="conceptosExcluidos"
      :loading-catalog="loadingCatalog || saving"
      @add-multiple="addConceptos"
    />

    <DataTable :value="tableData" striped-rows>
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
            @click="removeConcepto(data.idConcepto)"
          />
        </template>
      </Column>
    </DataTable>
  </div>
</template>
