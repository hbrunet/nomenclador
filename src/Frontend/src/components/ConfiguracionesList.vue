<script setup lang="ts">
import { computed } from 'vue'
import type { ConfiguracionNomencladorListItemDto } from '../types/configuration'

const props = defineProps<{
  items: ConfiguracionNomencladorListItemDto[]
  loading: boolean
  total: number
  page: number
  pageSize: number
}>()

defineEmits<{
  (event: 'create'): void
  (event: 'edit', id: number): void
  (event: 'page-change', page: number): void
}>()

const totalPages = computed(() => Math.ceil(props.total / props.pageSize) || 1)
</script>

<template>
  <section class="section-card stack">
    <div class="section-header">
      <div>
        <h2>Configuraciones disponibles</h2>
        <p class="muted">Listado inicial con filtros y acceso rápido al editor.</p>
      </div>
      <button class="primary-button" type="button" @click="$emit('create')">
        Nueva configuración
      </button>
    </div>

    <div class="table-wrapper">
      <table>
        <thead>
          <tr>
            <th>Nomenclador</th>
            <th>Escala</th>
            <th>Zona</th>
            <th>Vigencia</th>
            <th>Estado</th>
            <th>Conceptos</th>
            <th>Valores fijos</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td colspan="8" class="muted">Cargando configuraciones...</td>
          </tr>
          <tr v-else-if="!items.length">
            <td colspan="8" class="muted">No hay configuraciones para los filtros seleccionados.</td>
          </tr>
          <tr v-for="item in items" :key="item.id">
            <td>{{ item.nomencladorDescripcion }}</td>
            <td>{{ item.escalaDescripcion }}</td>
            <td>{{ item.zonaDescripcion }}</td>
            <td>{{ item.fechaInicio }} — {{ item.fechaFin ?? 'Vigente' }}</td>
            <td>
              <span class="badge">{{ item.estado }}</span>
            </td>
            <td>{{ item.cantidadConceptos }}</td>
            <td>{{ item.cantidadValoresFijos }}</td>
            <td>
              <button class="ghost-button" type="button" @click="$emit('edit', item.id)">
                Editar
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="total > 0" class="pagination">
      <button
        class="ghost-button"
        type="button"
        :disabled="page <= 1"
        @click="$emit('page-change', page - 1)"
      >
        ← Anterior
      </button>
      <span class="muted pagination-info">
        Página {{ page }} de {{ totalPages }} &middot; {{ total }} registros
      </span>
      <button
        class="ghost-button"
        type="button"
        :disabled="page >= totalPages"
        @click="$emit('page-change', page + 1)"
      >
        Siguiente →
      </button>
    </div>
  </section>
</template>
