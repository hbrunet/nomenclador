<script setup lang="ts">
import { computed, ref } from 'vue'
import type { ValorCategoriaCatalogItem, ValorCategoriaConfiguradoInputDto } from '../types/configuration'

const valoresCategorias = defineModel<ValorCategoriaConfiguradoInputDto[]>({ required: true })

const props = defineProps<{
  valoresDisponibles: ValorCategoriaCatalogItem[]
}>()

const selectedValorId = ref<number | null>(null)

const valuesById = computed(() => new Map(props.valoresDisponibles.map((item) => [item.id, item])))

function addValorCategoria() {
  if (!selectedValorId.value) return

  if (valoresCategorias.value.some((item) => item.idValorCategoria === selectedValorId.value)) return

  valoresCategorias.value = [
    ...valoresCategorias.value,
    { idValorCategoria: selectedValorId.value },
  ]
}

function removeValorCategoria(idValorCategoria: number) {
  valoresCategorias.value = valoresCategorias.value.filter(
    (item) => item.idValorCategoria !== idValorCategoria,
  )
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
          <tr v-for="item in valoresCategorias" :key="item.idValorCategoria">
            <td>{{ valuesById.get(item.idValorCategoria)?.descripcion ?? 'Valor sin catálogo' }}</td>
            <td>{{ valuesById.get(item.idValorCategoria)?.tipo ?? 'N/D' }}</td>
            <td>
              <button
                class="danger-button"
                type="button"
                @click="removeValorCategoria(item.idValorCategoria)"
              >
                Quitar
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
