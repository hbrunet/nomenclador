<script setup lang="ts">
import axios from 'axios'
import { computed, ref } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Paginator from 'primevue/paginator'
import { useToast } from 'primevue/usetoast';
import type { ConfiguracionNomencladorListItemDto, ValidacionConfiguracionResponse } from '../types/configuration'
import { formatLocalDate, formatPeriodo } from '../utils/date'
import ClonarConfiguracionDialog from '../components/ClonarConfiguracionDialog.vue'
import { configurationService } from '../services/configurationService'

const toast = useToast()

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

const clonarDialogRef = ref<InstanceType<typeof ClonarConfiguracionDialog> | null>(null)

async function handleCloneConfig(sourceId: number, dto: any) {
  try {
    await configurationService.clone(
      sourceId,
      {
        fechaInicio: formatLocalDate(dto.fechaInicio),
        fechaFin: dto.fechaFin ? formatLocalDate(dto.fechaFin) : null,
        copiarConceptos: dto.copiarConceptos,
        copiarValoresFijos: dto.copiarValoresFijos,
        copiarValoresCategoria: dto.copiarValoresCategoria
      }
    )
    toast.add({ severity: 'success', summary: 'Configuración clonada', detail: 'La configuración se ha clonado correctamente.', life: 5000 })
  } catch (error) {
    if (axios.isAxiosError<ValidacionConfiguracionResponse>(error)) {
      const validation = error.response?.data
      console.error('Error al clonar la configuración:', validation ?? error)

      const mensajes = validation?.errores.map(({ mensaje }) => mensaje).join('\n')
    
      toast.add({ severity: 'error', summary: 'Error al clonar la configuración', detail: mensajes || `Ocurrió un error al clonar la configuración. ${error.message}`, life: 5000 })
    } else {
      toast.add({ severity: 'error', summary: 'Error al clonar la configuración', detail: `Ocurrió un error inesperado al clonar la configuración. ${error}`, life: 5000 })
    }
  } finally {
    emit('page-change', props.page, props.pageSize)
  }
}

function openCloneConfigDialog(source: ConfiguracionNomencladorListItemDto) {
  clonarDialogRef.value?.open(source)
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
    >
      <template #empty>
        <span class="muted">No hay configuraciones para los filtros seleccionados.</span>
      </template>

      <Column field="nomencladorDescripcion" header="Nomenclador" />
      <Column field="escalaDescripcion" header="Escala" />
      <Column field="zonaDescripcion" header="Zona" />

      <Column header="Vigencia">
        <template #body="{ data }">
          {{ formatPeriodo(data.fechaInicio) }} — {{ data.fechaFin ? formatPeriodo(data.fechaFin) : 'Vigente' }}
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
            @click="openCloneConfigDialog(data)"
          />
        </template>
      </Column>
    </DataTable>
    <ClonarConfiguracionDialog ref="clonarDialogRef" @clone="handleCloneConfig"  />
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
