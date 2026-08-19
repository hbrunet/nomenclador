<script setup lang="ts">
import { onMounted, ref } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import GrupoValorCategoriaDialog from '../components/GrupoValorCategoriaDialog.vue'
import { gruposValorCategoriaService } from '../services/gruposValorCategoriaService'
import { valoresCategoriaService } from '../services/valoresCategoriaService'
import type { CatalogItem, GrupoValorCategoriaDto } from '../types/configuration'

const confirm = useConfirm()
const toast = useToast()

const grupos = ref<GrupoValorCategoriaDto[]>([])
const tipos = ref<CatalogItem[]>([])
const loading = ref(false)
const deleteError = ref<string | null>(null)
const dialogRef = ref<InstanceType<typeof GrupoValorCategoriaDialog> | null>(null)
let editingId: number | null = null

async function loadGrupos() {
  loading.value = true
  try {
    grupos.value = await gruposValorCategoriaService.getAll()
  } finally {
    loading.value = false
  }
}

async function loadTipos() {
  tipos.value = await valoresCategoriaService.getTipos()
}

function openCreate() {
  editingId = null
  dialogRef.value?.open('create')
}

function openEdit(grupo: GrupoValorCategoriaDto) {
  editingId = grupo.id
  dialogRef.value?.open('edit', { descripcion: grupo.descripcion, tiposIds: grupo.tipos.map((t) => t.id) })
}

async function handleSave(dto: { descripcion: string; tiposIds: number[] }) {
  if (editingId !== null) {
    const updated = await gruposValorCategoriaService.update(editingId, dto)
    grupos.value = grupos.value.map((g) => (g.id === editingId ? updated : g))
    toast.add({ severity: 'success', summary: 'Grupo actualizado', detail: 'El grupo se guardó correctamente.', life: 2500 })
  } else {
    const created = await gruposValorCategoriaService.create(dto)
    grupos.value = [...grupos.value, created].sort((a, b) => a.descripcion.localeCompare(b.descripcion))
    toast.add({ severity: 'success', summary: 'Grupo creado', detail: 'El grupo se creó correctamente.', life: 2500 })
  }
}

async function handleDelete(id: number) {
  deleteError.value = null
  try {
    await gruposValorCategoriaService.delete(id)
    grupos.value = grupos.value.filter((g) => g.id !== id)
  } catch (e: any) {
    deleteError.value = e.response?.data?.mensaje ?? e.response?.data?.message ?? 'No se pudo eliminar el grupo.'
  }
}

function confirmDelete(grupo: GrupoValorCategoriaDto) {
  confirm.require({
    message: `¿Eliminar el grupo "${grupo.descripcion}"?`,
    header: 'Confirmar eliminación',
    icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'Eliminar',
    acceptProps: { severity: 'danger' },
    rejectLabel: 'Cancelar',
    rejectProps: { severity: 'secondary', outlined: true },
    accept: () => handleDelete(grupo.id),
  })
}

onMounted(async () => {
  await Promise.all([loadGrupos(), loadTipos()])
})
</script>

<template>
  <section class="panel p-4">
    <div class="flex justify-content-between align-items-center mb-3">
      <div>
        <h2 class="text-xl mt-0 mb-0 font-semibold">Grupos de valores por categoría</h2>
        <p class="muted mt-2 mb-0">
          Agrupá tipos de valor por categoría para reutilizarlos al asociarlos masivamente.
        </p>
      </div>
      <Button label="Nuevo grupo" icon="pi pi-plus" @click="openCreate" />
    </div>

    <Message v-if="deleteError" severity="error" :closable="true" class="mb-3" @close="deleteError = null">
      {{ deleteError }}
    </Message>

    <DataTable :value="grupos" :loading="loading" striped-rows sort-field="descripcion" :sort-order="1">
      <template #empty>
        <span class="muted">No hay grupos creados.</span>
      </template>
      <Column field="id" header="ID" style="width: 5rem; text-align: right" />
      <Column field="descripcion" header="Descripción" sortable />
      <Column header="Tipos">
        <template #body="{ data }">
          <div class="flex gap-1 flex-wrap">
            <Tag v-for="tipo in data.tipos" :key="tipo.id" :value="`${tipo.id} - ${tipo.descripcion}`" severity="info" />
          </div>
        </template>
      </Column>
      <Column style="width: 8rem">
        <template #body="{ data }">
          <div class="flex gap-1 align-items-center">
            <Button
              icon="pi pi-pencil"
              size="small"
              severity="secondary"
              outlined
              @click="openEdit(data)"
            />
            <Button icon="pi pi-trash" size="small" severity="danger" text rounded @click="confirmDelete(data)" />
          </div>
        </template>
      </Column>
    </DataTable>

    <GrupoValorCategoriaDialog ref="dialogRef" :tipos="tipos" @save="handleSave" />
  </section>
</template>
