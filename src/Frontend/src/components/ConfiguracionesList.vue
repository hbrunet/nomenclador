<script setup lang="ts">
import { computed } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Paginator from 'primevue/paginator'
import type { ConfiguracionNomencladorListItemDto } from '../types/configuration'
import { parseLocalDate } from '../utils/date'

function formatearFecha(fecha: string): string {
  const date = parseLocalDate(fecha)
  return `${(date.getMonth() + 1).toString().padStart(2, '0')}/${date.getFullYear()}`;
}

const props = defineProps<{
  items: ConfiguracionNomencladorListItemDto[]
  loading: boolean
  total: number
  page: number
  pageSize: number
}>()

const emit = defineEmits<{
  (event: 'create'): void
  (event: 'edit', id: number): void
  (event: 'page-change', page: number, pageSize: number): void
}>()

const paginatorFirst = computed(() => (props.page - 1) * props.pageSize)

function estadoSeverity(estado: string) {
  if (estado === 'Activa') return 'success'
  if (estado === 'Futura') return 'info'
  if (estado === 'Vencida') return 'warn'
  return 'secondary'
}

function onPageChange(event: { page: number; rows: number }) {
  emit('page-change', event.page + 1, event.rows)
}
</script>

<template>
  <section class="panel p-4 flex flex-column gap-3">
    <div class="flex justify-content-between align-items-center flex-wrap gap-3">
      <div>
       
      </div>
      <Button
        label="Nueva configuración"
        icon="pi pi-plus"
        @click="emit('create')"
      />
    </div>

    <DataTable
      :value="items"
      :loading="loading"
      striped-rows
      :sort-field="'nomencladorDescripcion'" :sort-order="1"
    >
      <template #empty>
        <span class="muted">No hay configuraciones para los filtros seleccionados.</span>
      </template>

      <Column field="nomencladorDescripcion" header="Nomenclador" sortable />
      <Column field="escalaDescripcion" header="Escala" sortable />
      <Column field="zonaDescripcion" header="Zona" sortable />

      <Column header="Vigencia">
        <template #body="{ data }">
          {{ formatearFecha(data.fechaInicio) }} — {{ data.fechaFin ? formatearFecha(data.fechaFin) : 'Vigente' }}
        </template>
      </Column>

      <Column header="Estado">
        <template #body="{ data }">
          <Tag :value="data.estado" :severity="estadoSeverity(data.estado)" />
        </template>
      </Column>


      <Column>
        <template #body="{ data }">
          <Button
            label="Editar"
            icon="pi pi-pencil"
            size="small"
            text
            @click="emit('edit', data.id)"
          />
          <Button 
            label="Clonar" 
            icon="pi pi-copy" 
            size="small"
            text
          />
        </template>
      </Column>
    </DataTable>

    <Paginator
      v-if="total > 0"
      :rows="pageSize"
      :total-records="total"
      :first="paginatorFirst"
      :rows-per-page-options="[10, 20, 50, 100]"
      @page="onPageChange"
    />
  </section>
</template>
