<script setup lang="ts">
import { computed, ref } from 'vue'
import type { ValorFijoCatalogItem, ValorFijoConfiguradoInputDto } from '../types/configuration'

const valoresFijos = defineModel<ValorFijoConfiguradoInputDto[]>({ required: true })

const props = defineProps<{
  valoresDisponibles: ValorFijoCatalogItem[]
}>()

const selectedValorId = ref<number | null>(null)

const valuesById = computed(() => new Map(props.valoresDisponibles.map((item) => [item.id, item])))

function addValorFijo() {
  if (!selectedValorId.value) {
    return
  }

  if (valoresFijos.value.some((item) => item.idValorFijo === selectedValorId.value)) {
    return
  }

  valoresFijos.value = [
    ...valoresFijos.value,
    {
      idValorFijo: selectedValorId.value,
      importe: 0,
    },
  ]
}

function removeValorFijo(idValorFijo: number) {
  valoresFijos.value = valoresFijos.value.filter((item) => item.idValorFijo !== idValorFijo)
}
</script>

<template>
  <div class="stack">
    <div class="inline-actions">
      <label class="field-stack">
        <span>Agregar valor fijo</span>
        <select v-model.number="selectedValorId">
          <option :value="null">Seleccione un valor fijo</option>
          <option v-for="item in valoresDisponibles" :key="item.id" :value="item.id">
            {{ item.descripcion }} ({{ item.tipo }})
          </option>
        </select>
      </label>
      <button class="secondary-button" type="button" @click="addValorFijo">Agregar</button>
    </div>

    <div class="table-wrapper">
      <table>
        <thead>
          <tr>
            <th>Descripción</th>
            <th>Tipo</th>
            <th>Importe</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!valoresFijos.length">
            <td colspan="4" class="muted">No hay valores fijos configurados.</td>
          </tr>
          <tr v-for="item in valoresFijos" :key="item.idValorFijo">
            <td>{{ valuesById.get(item.idValorFijo)?.descripcion ?? 'Valor fijo' }}</td>
            <td>{{ valuesById.get(item.idValorFijo)?.tipo ?? 'N/D' }}</td>
            <td>
              <input v-model.number="item.importe" min="0" step="0.01" type="number" />
            </td>
            <td>
              <button class="danger-button" type="button" @click="removeValorFijo(item.idValorFijo)">
                Quitar
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
