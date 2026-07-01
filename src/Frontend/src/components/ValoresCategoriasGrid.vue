<script setup lang="ts">
import { computed, ref } from 'vue'
import ValorCategoriaItemsModal from './ValorCategoriaItemsModal.vue'
import type { ValorCategoriaCatalogItem, ValorCategoriaConfiguradoInputDto } from '../types/configuration'

const valoresCategorias = defineModel<ValorCategoriaConfiguradoInputDto[]>({ required: true })

const props = defineProps<{
  valoresDisponibles: ValorCategoriaCatalogItem[]
}>()

const selectedValorId = ref<number | null>(null)
const selectedItemIndex = ref<number | null>(null)
const modalRef = ref<InstanceType<typeof ValorCategoriaItemsModal> | null>(null)

const valuesById = computed(() => new Map(props.valoresDisponibles.map((item) => [item.id, item])))

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

function addValorCategoria() {
  if (!selectedValorId.value) return

  if (valoresCategorias.value.some((item) => item.idValorCategoria === selectedValorId.value)) return

  valoresCategorias.value = [
    ...valoresCategorias.value,
    { idValorCategoria: selectedValorId.value, items: [] },
  ]
}

function removeValorCategoria(idValorCategoria: number) {
  valoresCategorias.value = valoresCategorias.value.filter(
    (item) => item.idValorCategoria !== idValorCategoria,
  )
}

function verItems(index: number) {
  selectedItemIndex.value = index
  modalRef.value?.open()
}
</script>

<template>
  <div class="stack">
    <div class="inline-actions">
      <label class="field-stack">
        <span>Agregar valor por categoría</span>
        <select v-model.number="selectedValorId">
          <option :value="null">Seleccione un valor</option>
          <option v-for="item in valoresDisponibles" :key="item.id" :value="item.id">
            {{ item.descripcion }} ({{ item.tipo }})
          </option>
        </select>
      </label>
      <button class="secondary-button" type="button" @click="addValorCategoria">Agregar</button>
    </div>

    <div class="table-wrapper">
      <table>
        <thead>
          <tr>
            <th>Descripción</th>
            <th>Tipo</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!valoresCategorias.length">
            <td colspan="3" class="muted">No hay valores por categoría configurados.</td>
          </tr>
          <tr v-for="(item, index) in valoresCategorias" :key="item.idValorCategoria">
            <td>{{ valuesById.get(item.idValorCategoria)?.descripcion ?? 'Valor sin catálogo' }}</td>
            <td>{{ valuesById.get(item.idValorCategoria)?.tipo ?? 'N/D' }}</td>
            <td>
              <div class="inline-actions">
                <button
                  class="secondary-button"
                  type="button"
                  @click="verItems(index)"
                >
                  Ver items
                </button>
                <button
                  class="danger-button"
                  type="button"
                  @click="removeValorCategoria(item.idValorCategoria)"
                >
                  Quitar
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <ValorCategoriaItemsModal
      ref="modalRef"
      :item="selectedItem"
      :descripcion="selectedDescripcion"
      :tipo="selectedTipo"
    />
  </div>
</template>
