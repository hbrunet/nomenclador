<script setup lang="ts">
import { watch } from 'vue'
import type {
  CategoriaCatalogItem,
  ValorCategoriaConfiguradoInputDto,
} from '../types/configuration'

const valoresCategorias = defineModel<ValorCategoriaConfiguradoInputDto[]>({ required: true })

const props = defineProps<{
  categorias: CategoriaCatalogItem[]
}>()

watch(
  () => props.categorias,
  (categorias) => {
    const currentValues = new Map(
      valoresCategorias.value.map((item) => [item.idCategoria, item]),
    )

    valoresCategorias.value = categorias.map(
      (categoria) =>
        currentValues.get(categoria.id) ?? {
          idCategoria: categoria.id,
          importe: 0,
        },
    )
  },
  { immediate: true },
)

function findValor(idCategoria: number) {
  return valoresCategorias.value.find((item) => item.idCategoria === idCategoria)
}
</script>

<template>
  <div class="table-wrapper">
    <table>
      <thead>
        <tr>
          <th>Categoría</th>
          <th>Número</th>
          <th>Importe</th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="!categorias.length">
          <td colspan="3" class="muted">
            Seleccione una escala salarial para cargar las categorías.
          </td>
        </tr>
        <tr v-for="categoria in categorias" :key="categoria.id">
          <td>{{ categoria.descripcion }}</td>
          <td>{{ categoria.numero }}</td>
          <td>
            <input
              v-model.number="findValor(categoria.id)!.importe"
              min="0"
              step="0.01"
              type="number"
            />
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
