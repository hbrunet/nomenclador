<script setup lang="ts">
import { computed, ref } from 'vue'
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

function addConcepto() {
  if (!selectedConceptId.value) {
    return
  }

  const alreadyAdded = conceptos.value.some(
    (item) => item.idConcepto === selectedConceptId.value,
  )

  if (alreadyAdded) {
    return
  }

  conceptos.value = [
    ...conceptos.value,
    {
      idConcepto: selectedConceptId.value,
      orden: conceptos.value.length + 1,
    },
  ]
}

function removeConcepto(idConcepto: number) {
  conceptos.value = conceptos.value
    .filter((item) => item.idConcepto !== idConcepto)
    .map((item, index) => ({
      ...item,
      orden: index + 1,
    }))
}
</script>

<template>
  <div class="stack">
    <div class="inline-actions">
      <label class="field-stack">
        <span>Agregar concepto</span>
        <select v-model.number="selectedConceptId">
          <option :value="null">Seleccione un concepto</option>
          <option v-for="concepto in conceptosDisponibles" :key="concepto.id" :value="concepto.id">
            {{ concepto.codigo }} - {{ concepto.descripcionBreve }}
          </option>
        </select>
      </label>
      <button class="secondary-button" type="button" :disabled="loadingCatalog" @click="addConcepto">
        Agregar
      </button>
    </div>

    <div class="table-wrapper">
      <table>
        <thead>
          <tr>
            <th>Orden</th>
            <th>Código</th>
            <th>Descripción</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!conceptos.length">
            <td colspan="4" class="muted">Agregue conceptos desde el catálogo.</td>
          </tr>
          <tr v-for="item in conceptos" :key="item.idConcepto">
            <td>
              <input v-model.number="item.orden" min="1" type="number" />
            </td>
            <td>{{ selectedLookup.get(item.idConcepto)?.codigo ?? item.idConcepto }}</td>
            <td>{{ selectedLookup.get(item.idConcepto)?.descripcion ?? 'Concepto sin catálogo' }}</td>
            <td>
              <button class="danger-button" type="button" @click="removeConcepto(item.idConcepto)">
                Quitar
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
