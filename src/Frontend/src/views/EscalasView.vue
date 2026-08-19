<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import { useConfirm } from 'primevue/useconfirm'
import { escalasService } from '../services/escalasService'
import type { EscalaListItemDto } from '../types/configuration'

const router = useRouter()
const confirm = useConfirm()
const escalas = ref<EscalaListItemDto[]>([])
const loading = ref(false)
const filterQuery = ref('')
const deleteError = ref<string | null>(null)

const filteredEscalas = computed(() => {
  const q = filterQuery.value.toLowerCase().trim()
  if (!q) return escalas.value
  return escalas.value.filter((e) => e.descripcion.toLowerCase().includes(q))
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
      e.response?.data?.mensaje ?? e.response?.data?.message ??
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
        placeholder="Buscar por descripción..."
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
      <Column field="id" header="ID" style="width: 5rem; text-align: right" />
      <Column field="descripcion" header="Descripción" sortable />
      <Column header="Categorías" style="width: 8rem; text-align: right">
        <template #body="{ data }">
          {{ data.cantidadCategorias }}
        </template>
      </Column>
      <Column style="width: 10rem">
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
  </section>
</template>
