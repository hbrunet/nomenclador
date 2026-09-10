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
import ClonacionMasivaConfiguracionesDialog from '../components/ClonacionMasivaConfiguracionesDialog.vue'
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

// Solo las configuraciones activas pueden clonarse masivamente (el resto ya
// tiene un período cerrado/futuro, no aplica el flujo de "cerrar y clonar").
const selectedRows = ref<ConfiguracionNomencladorListItemDto[]>([])

// El checkbox "seleccionar todo" del header solo emite las filas de la página
// actual (reemplazo completo), pero el toggle de una fila individual emite la
// selección COMPLETA ya fusionada con otras páginas (PrimeVue la mantiene
// internamente) — hay que quedarse solo con lo que newSelection dice sobre la
// página actual y preservar el resto a mano, si no se duplican las filas de
// otras páginas cada vez que se toca una fila individual.
function handleSelectionChange(newSelection: ConfiguracionNomencladorListItemDto[]) {
  const idsPaginaActual = new Set(props.items.map((item) => item.id))
  const seleccionOtrasPaginas = selectedRows.value.filter((item) => !idsPaginaActual.has(item.id))
  const seleccionPaginaActual = newSelection.filter((item) => idsPaginaActual.has(item.id))

  const validas = seleccionPaginaActual.filter((item) => item.estado === 'Activa')
  if (validas.length !== seleccionPaginaActual.length) {
    toast.add({
      severity: 'warn',
      summary: 'Selección inválida',
      detail: 'Solo se pueden seleccionar configuraciones activas para la clonación masiva.',
      life: 4000,
    })
  }

  selectedRows.value = [...seleccionOtrasPaginas, ...validas]
}

function clearSelection() {
  selectedRows.value = []
}

const clonarDialogRef = ref<InstanceType<typeof ClonarConfiguracionDialog> | null>(null)
const clonacionMasivaDialogRef = ref<InstanceType<typeof ClonacionMasivaConfiguracionesDialog> | null>(null)
// Mientras está en true el diálogo se mantiene abierto (sin poder cerrarse) y
// bloquea sus propios controles — la clonación de N configuraciones puede
// tardar varios segundos y sin esto el usuario podía cerrar/repetir la acción
// creyendo que no se había disparado.
const cloningMasivo = ref(false)

function openClonacionMasivaDialog() {
  clonacionMasivaDialogRef.value?.open(selectedRows.value)
}

async function handleCloneMasivo(sourceIds: number[], dto: any) {
  cloningMasivo.value = true
  try {
    const resultado = await configurationService.cloneMasivo({
      configuracionesIds: sourceIds,
      fechaInicio: formatLocalDate(dto.fechaInicio),
      fechaFin: dto.fechaFin ? formatLocalDate(dto.fechaFin) : null,
      copiarConceptos: dto.copiarConceptos,
      copiarValoresFijos: dto.copiarValoresFijos,
      copiarValoresCategoria: dto.copiarValoresCategoria,
    })
    toast.add({
      severity: 'success',
      summary: 'Configuraciones clonadas',
      detail: `Se cerraron ${resultado.configuracionesCerradas} configuración(es) y se crearon ${resultado.clones.length} clon(es).`,
      life: 5000,
    })
    clearSelection()
  } catch (error) {
    if (axios.isAxiosError<ValidacionConfiguracionResponse>(error)) {
      const validation = error.response?.data
      console.error('Error al clonar masivamente las configuraciones:', validation ?? error)
      const mensajes = validation?.errores?.map(({ mensaje }) => mensaje).join('\n')
      toast.add({
        severity: 'error',
        summary: 'Error al clonar las configuraciones',
        detail: mensajes || `Ocurrió un error al clonar las configuraciones. ${error.message}`,
        life: 6000,
      })
    } else {
      toast.add({
        severity: 'error',
        summary: 'Error al clonar las configuraciones',
        detail: `Ocurrió un error inesperado al clonar las configuraciones. ${error}`,
        life: 6000,
      })
    }
  } finally {
    cloningMasivo.value = false
    clonacionMasivaDialogRef.value?.close()
    // Volver a la página 1: tras cerrar/clonar en lote, la página en la que
    // estaba el usuario puede haber quedado vacía o desactualizada (varias
    // configuraciones cambian de estado a la vez), a diferencia del clon
    // individual donde recargar la misma página alcanza.
    emit('page-change', 1, props.pageSize)
  }
}

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
  <section class="panel p-4">
    <div class="flex justify-content-between align-items-center">
      <div class="flex gap-2 align-items-center">
        <Tag v-if="selectedRows.length" :value="`${selectedRows.length} activa(s) seleccionada(s)`" severity="info" />
        <Button
          v-if="selectedRows.length"
          label="Clonar seleccionadas"
          icon="pi pi-copy"
          severity="secondary"
          outlined
          size="small"
          :disabled="cloningMasivo"
          @click="openClonacionMasivaDialog"
        />
        <Button
          v-if="selectedRows.length"
          label="Limpiar selección"
          icon="pi pi-times"
          severity="secondary"
          text
          size="small"
          :disabled="cloningMasivo"
          @click="clearSelection"
        />
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
      :selection="selectedRows"
      @update:selection="handleSelectionChange"
      data-key="id"
      striped-rows
    >
      <template #empty>
        <span class="muted">No hay configuraciones para los filtros seleccionados.</span>
      </template>

      <Column selection-mode="multiple" header-style="width: 3rem" />
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
          <div class="flex gap-1 align-items-center">
            <Button
              label="Editar"
              icon="pi pi-pencil"
              severity="secondary"
              size="small"
              outlined
              @click="emit('edit', data.id)"
            />
            <Button 
              label="Clonar" 
              icon="pi pi-copy" 
              severity="secondary"
              outlined
              size="small"
              @click="openCloneConfigDialog(data)"
            />
          </div>
        </template>
      </Column>
    </DataTable>
    <ClonarConfiguracionDialog ref="clonarDialogRef" @clone="handleCloneConfig"  />
    <ClonacionMasivaConfiguracionesDialog
      ref="clonacionMasivaDialogRef"
      :loading="cloningMasivo"
      @clone-masivo="handleCloneMasivo"
    />
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
