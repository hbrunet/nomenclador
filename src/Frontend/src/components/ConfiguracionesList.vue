<script setup lang="ts">
import type { ConfiguracionNomencladorListItemDto } from '../types/configuration'

defineProps<{
  items: ConfiguracionNomencladorListItemDto[]
  loading: boolean
}>()

defineEmits<{
  (event: 'create'): void
  (event: 'edit', id: number): void
}>()
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
  </section>
</template>
