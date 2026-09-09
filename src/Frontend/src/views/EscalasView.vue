<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Dialog from 'primevue/dialog'
import DatePicker from 'primevue/datepicker'
import InputNumber from 'primevue/inputnumber'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import { escalasService } from '../services/escalasService'
import { formatLocalDate } from '../utils/date'
import type { EscalaCloneConflictDto, EscalaListItemDto } from '../types/configuration'

const router = useRouter()
const confirm = useConfirm()
const toast = useToast()
const escalas = ref<EscalaListItemDto[]>([])
const loading = ref(false)
const filterQuery = ref('')
const deleteError = ref<string | null>(null)

const filteredEscalas = computed(() => {
  const q = filterQuery.value.toLowerCase().trim()
  if (!q) return escalas.value
  return escalas.value.filter((e) => e.descripcion.toLowerCase().includes(q) 
  || e.id.toString().includes(q))
})

async function load() {
  loading.value = true
  try {
    escalas.value = await escalasService.getAll()
  } finally {
    loading.value = false
  }
}

async function handleDelete(id: number) {
  deleteError.value = null
  try {
    await escalasService.delete(id)
    escalas.value = escalas.value.filter((e) => e.id !== id)
  } catch (e: any) {
    deleteError.value =
      e.response?.data?.mensaje ?? e.response?.data?.message ?? 'Error al eliminar la escala salarial'
  }
}

function confirmDelete(escala: EscalaListItemDto) {
  confirm.require({
    message: `¿Eliminar la escala "${escala.descripcion}"?`,
    header: 'Confirmar eliminación',
    icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'Eliminar',
    acceptProps: { severity: 'danger' },
    rejectLabel: 'Cancelar',
    rejectProps: { severity: 'secondary', outlined: true },
    accept: () => handleDelete(escala.id),
  })
}

// ── Clonación individual ───────────────────────────────────────────────────
const cloneDialogVisible = ref(false)
const cloningEscala = ref<EscalaListItemDto | null>(null)
const nuevoPeriodo = ref<Date | null>(null)
const coeficienteAjuste = ref<number>(1)
const cloning = ref(false)
const cloneConflict = ref<EscalaCloneConflictDto | null>(null)
const conflictDialogVisible = ref(false)

const canClone = computed(() => nuevoPeriodo.value !== null && (coeficienteAjuste.value ?? 0) > 0)

function openCloneDialog(escala: EscalaListItemDto) {
  cloningEscala.value = escala
  nuevoPeriodo.value = null
  coeficienteAjuste.value = 1
  cloneDialogVisible.value = true
}

async function handleClone(actualizarSiExiste = false) {
  if (!cloningEscala.value || !nuevoPeriodo.value || !canClone.value) return

  cloning.value = true
  try {
    const clon = await escalasService.clone(cloningEscala.value.id, {
      nuevoPeriodo: formatLocalDate(nuevoPeriodo.value),
      coeficienteAjuste: coeficienteAjuste.value,
      actualizarSiExiste,
    })
    cloneDialogVisible.value = false
    conflictDialogVisible.value = false
    toast.add({
      severity: 'success',
      summary: actualizarSiExiste ? 'Escala actualizada' : 'Escala clonada',
      detail: `Se ${actualizarSiExiste ? 'actualizó' : 'creó'} la escala "${clon.descripcion}".`,
      life: 4000,
    })
    await load()
  } catch (e: any) {
    if (e.response?.status === 409) {
      cloneConflict.value = e.response.data
      conflictDialogVisible.value = true
    } else {
      toast.add({
        severity: 'error',
        summary: 'Error al clonar',
        detail: e.response?.data?.mensaje ?? e.response?.data?.message ?? 'Ocurrió un error al clonar la escala salarial.',
      })
    }
  } finally {
    cloning.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="panel p-4">
    <div class="flex justify-content-between align-items-center mb-3">
      <h2 class="text-xl mt-0 mb-0 font-semibold">Escalas salariales</h2>
      <Button label="Nueva escala" icon="pi pi-plus" @click="router.push('/escalas/nueva')" />
    </div>

    <div class="mb-3" style="max-width: 400px">
      <InputText
        v-model="filterQuery"
        placeholder="Filtrar por ID o descripción..."
        class="w-full"
      />
    </div>

    <Message
      v-if="deleteError"
      severity="error"
      :closable="true"
      class="mb-3"
      @close="deleteError = null"
    >
      {{ deleteError }}
    </Message>

    <DataTable
      :value="filteredEscalas"
      :loading="loading"
      striped-rows
      :sort-field="'descripcion'"
      :sort-order="1"
      paginator
      :rows="15"
      :rows-per-page-options="[10, 15, 25, 50]"
    >
      <template #empty>
        <span class="muted">
          {{ filterQuery ? 'Sin resultados para el filtro aplicado.' : 'No hay escalas salariales cargadas.' }}
        </span>
      </template>
      <Column field="id" header="ID" style="width: 5rem; text-align: right" sortable />
      <Column field="descripcion" header="Descripción" sortable />
      <Column header="Categorías" style="width: 8rem; text-align: right">
        <template #body="{ data }">
          {{ data.cantidadCategorias }}
        </template>
      </Column>
      <Column style="width: 18rem">
        <template #body="{ data }">
          <div class="flex gap-1 align-items-center">
            <Button
              label="Editar"
              icon="pi pi-pencil"
              size="small"
              severity="secondary"
              outlined
              @click="router.push(`/escalas/${data.id}`)"
            />
            <Button
              label="Clonar"
              icon="pi pi-copy"
              size="small"
              severity="secondary"
              outlined
              @click="openCloneDialog(data)"
            />
            <Button
              icon="pi pi-trash"
              size="small"
              severity="danger"
              text
              rounded
              @click="confirmDelete(data)"
            />
          </div>
        </template>
      </Column>
    </DataTable>

    <Dialog v-model:visible="cloneDialogVisible" header="Clonar escala salarial" modal style="width: 420px">
      <p class="muted mt-0">
        Se creará una nueva escala a partir de "{{ cloningEscala?.descripcion }}", reemplazando el
        período de la descripción y ajustando el monto de cada categoría por el coeficiente indicado.
      </p>
      <div class="flex flex-column gap-3">
        <div class="flex flex-column gap-1">
          <label class="field-label">Nuevo período</label>
          <DatePicker v-model="nuevoPeriodo" view="month" date-format="mm/yy" class="w-full" />
        </div>
        <div class="flex flex-column gap-1">
          <label class="field-label">Coeficiente de ajuste</label>
          <InputNumber v-model="coeficienteAjuste" :min-fraction-digits="2" :max-fraction-digits="4" class="w-full" />
        </div>
      </div>
      <template #footer>
        <Button label="Cancelar" severity="secondary" outlined @click="cloneDialogVisible = false" />
        <Button label="Clonar" :loading="cloning" :disabled="!canClone" @click="handleClone()" />
      </template>
    </Dialog>

    <Dialog v-model:visible="conflictDialogVisible" header="Ya existe una escala para ese período" modal style="width: 460px">
      <p class="mt-0">
        Ya existe la escala "{{ cloneConflict?.escalaExistenteDescripcion }}" (ID {{ cloneConflict?.escalaExistenteId }})
        con el mismo nombre que tendría el clon. ¿Desea actualizar sus categorías con el coeficiente indicado
        en lugar de crear una escala duplicada?
      </p>
      <template #footer>
        <Button label="Cancelar" severity="secondary" outlined @click="conflictDialogVisible = false" />
        <Button label="Actualizar existente" :loading="cloning" @click="handleClone(true)" />
      </template>
    </Dialog>
  </section>
</template>

