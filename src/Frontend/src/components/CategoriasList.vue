<script setup lang="ts">
import type { CategoriaCatalogItem } from '../types/configuration'

defineProps<{
  categorias: CategoriaCatalogItem[]
}>()
</script>

<template>
  <div class="stack">
    <div class="table-wrapper">
      <table>
        <thead>
          <tr>
            <th>N°</th>
            <th>Descripción</th>
            <th class="align-right">Monto</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!categorias.length">
            <td colspan="3" class="muted">Sin categorías configuradas.</td>
          </tr>
          <tr v-for="categoria in categorias" :key="categoria.id">
            <td>{{ categoria.numero }}</td>
            <td>{{ categoria.descripcion }}</td>
            <td class="align-right">{{ categoria.monto.toLocaleString('es-AR', { minimumFractionDigits: 2 }) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.align-right {
  text-align: right;
}
</style>
