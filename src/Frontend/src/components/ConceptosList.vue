<script setup lang="ts">
import { computed, ref } from 'vue'
import Select from 'primevue/select'
import Button from 'primevue/button'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import type { ConceptoCatalogItem, ConceptoConfiguradoInputDto } from '../types/configuration'

const conceptos = defineModel<ConceptoConfiguradoInputDto[]>({ required: true })

const props = defineProps<{
  conceptosDisponibles: ConceptoCatalogItem[]
  loadingCatalog: boolean
}>()

const selectedConceptId = ref<number | null>(null)

const selectedLookup = computed(
  () => new Map(props.conceptosDisponibles.map((item) => [item.id, item])),
)

const tableData = computed(() =>
  conceptos.value.map((item) => ({
    idConcepto: item.idConcepto,
    orden: item.orden,
    codigo: selectedLookup.value.get(item.idConcepto)?.codigo ?? String(item.idConcepto),
    subcodigo: selectedLookup.value.get(item.idConcepto)?.subcodigo ?? 'N/D',
    descripcion: selectedLookup.value.get(item.idConcepto)?.descripcion ?? 'Concepto sin catálogo',
  }))
)

function addConcepto() {
  if (!selectedConceptId.value) return
  if (conceptos.value.some((item) => item.idConcepto === selectedConceptId.value)) return
  conceptos.value = [
    ...conceptos.value,
    { idConcepto: selectedConceptId.value, orden: conceptos.value.length + 1 },
  ]
}

function removeConcepto(idConcepto: number) {
  conceptos.value = conceptos.value
    .filter((item) => item.idConcepto !== idConcepto)
    .map((item, index) => ({ ...item, orden: index + 1 }))
}
</script>

<template>
  <div class="flex flex-column gap-3 pt-3">
    <div class="flex align-items-end gap-3 flex-wrap">
      <div class="flex flex-column gap-1 flex-1" style="min-width: 280px">
        <label class="field-label">Agregar concepto</label>
        <Select
          v-model="selectedConceptId"
          :options="conceptosDisponibles"
          option-label="descripcionBreve"
          option-value="id"
          placeholder="Seleccione un concepto"
          :loading="loadingCatalog"
          filter
          class="w-full"
        />
      </div>
      <Button
        label="Agregar"
        icon="pi pi-plus"
        severity="secondary"
        :disabled="!selectedConceptId || loadingCatalog"
        @click="addConcepto"
      />
    </div>

    <DataTable :value="tableData" striped-rows>
      <template #empty>
        <span class="muted">Agregue conceptos desde el catálogo.</span>
      </template>
      <Column field="codigo" header="Código" style="text-align: right" />
      <Column field="subcodigo" header="Subcódigo" style="text-align: right" />
      <Column field="descripcion" header="Descripción" />
      <Column>
        <template #body="{ data }">
          <Button
            icon="pi pi-trash"
            severity="danger"
            text
            rounded
            size="small"
            @click="removeConcepto(data.idConcepto)"
          />
        </template>
      </Column>
    </DataTable>
  </div>
</template>
