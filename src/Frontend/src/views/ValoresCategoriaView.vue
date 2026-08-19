<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Tabs from 'primevue/tabs'
import TabList from 'primevue/tablist'
import Tab from 'primevue/tab'
import TabPanels from 'primevue/tabpanels'
import TabPanel from 'primevue/tabpanel'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import ValorCategoriaTipoDialog from '../components/ValorCategoriaTipoDialog.vue'
import { valoresCategoriaService } from '../services/valoresCategoriaService'
import type { CatalogItem, ValorCategoriaListItemDto, ValorCategoriaTipoCreateUpdateDto } from '../types/configuration'

const router = useRouter()
const confirm = useConfirm()
const toast = useToast()
const activeTab = ref('valores')

// ── Valores ─────────────────────────────────────────────────────────────────
const valores = ref<ValorCategoriaListItemDto[]>([])
const loadingValores = ref(false)
const filterValores = ref('')
const filterTipo = ref('')
const deleteValorError = ref<string | null>(null)

const filteredValores = computed(() => {
  const q = filterValores.value.toLowerCase().trim()
  const t = filterTipo.value.toLowerCase().trim()
  return valores.value.filter(
    (v) =>
      (!q || v.descripcion.toLowerCase().includes(q)) &&
      (!t || v.tipo.toLowerCase().includes(t)),
  )
})

async function loadValores(forceRefresh = false) {
  // Evita el parpadeo del spinner cuando el dato ya está en caché.
  const showSpinner = forceRefresh || !valoresCategoriaService.hasCachedValores()
  if (showSpinner) loadingValores.value = true
  try {
    valores.value = await valoresCategoriaService.getAll(forceRefresh)
  } finally {
    loadingValores.value = false
  }
}

async function handleDeleteValor(id: number) {
  deleteValorError.value = null
  try {
    await valoresCategoriaService.delete(id)
    valores.value = valores.value.filter((v) => v.id !== id)
  } catch (e: any) {
    deleteValorError.value =
      e.response?.data?.mensaje ??
      'El valor está siendo utilizado por una o más configuraciones y no puede eliminarse.'
  }
}

function confirmDeleteValor(valor: ValorCategoriaListItemDto) {
  confirm.require({
    message: `¿Eliminar el valor por categoría "${valor.descripcion}"?`,
    header: 'Confirmar eliminación',
    icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'Eliminar',
    acceptProps: { severity: 'danger' },
    rejectLabel: 'Cancelar',
    rejectProps: { severity: 'secondary', outlined: true },
    accept: () => handleDeleteValor(valor.id),
  })
}

// ── Tipos ────────────────────────────────────────────────────────────────────
const tipos = ref<CatalogItem[]>([])
const loadingTipos = ref(false)
const deleteTipoError = ref<string | null>(null)
const tipoDialogRef = ref<InstanceType<typeof ValorCategoriaTipoDialog> | null>(null)
let editingTipoId: number | null = null

async function loadTipos() {
  loadingTipos.value = true
  try {
    tipos.value = await valoresCategoriaService.getTipos()
  } finally {
    loadingTipos.value = false
  }
}

function openCreateTipo() {
  editingTipoId = null
  tipoDialogRef.value?.open('create')
}

function openEditTipo(tipo: CatalogItem) {
  editingTipoId = tipo.id
  tipoDialogRef.value?.open('edit', tipo.descripcion)
}

async function handleTipoSave(dto: ValorCategoriaTipoCreateUpdateDto) {
  if (editingTipoId) {
    const updated = await valoresCategoriaService.updateTipo(editingTipoId, dto)
    const idx = tipos.value.findIndex((t) => t.id === editingTipoId)
    if (idx !== -1) tipos.value[idx] = updated
    toast.add({ severity: 'success', summary: 'Tipo actualizado', detail: 'El tipo se guardó correctamente.', life: 2500 })
  } else {
    const created = await valoresCategoriaService.createTipo(dto)
    tipos.value = [...tipos.value, created].sort((a, b) => a.descripcion.localeCompare(b.descripcion))
    toast.add({ severity: 'success', summary: 'Tipo creado', detail: 'El tipo se creó correctamente.', life: 2500 })
  }
}

async function handleDeleteTipo(id: number) {
  deleteTipoError.value = null
  try {
    await valoresCategoriaService.deleteTipo(id)
    tipos.value = tipos.value.filter((t) => t.id !== id)
  } catch (e: any) {
    deleteTipoError.value =
      e.response?.data?.mensaje ??
      'El tipo está siendo utilizado por uno o más valores y no puede eliminarse.'
  }
}

function confirmDeleteTipo(tipo: CatalogItem) {
  confirm.require({
    message: `¿Eliminar el tipo "${tipo.descripcion}"?`,
    header: 'Confirmar eliminación',
    icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'Eliminar',
    acceptProps: { severity: 'danger' },
    rejectLabel: 'Cancelar',
    rejectProps: { severity: 'secondary', outlined: true },
    accept: () => handleDeleteTipo(tipo.id),
  })
}

onMounted(async () => {
  await Promise.all([loadValores(), loadTipos()])
})
</script>

<template>
  <section class="panel p-4">
    <div class="flex justify-content-between align-items-center mb-3">
      <h2 class="text-xl mt-0 mb-0 font-semibold">Valores por categoría</h2>
    </div>

    <Tabs v-model:value="activeTab">
      <TabList>
        <Tab value="valores">Valores</Tab>
        <Tab value="tipos">Tipos</Tab>
      </TabList>

      <TabPanels>
        <!-- ── Valores ─────────────────────────────────────────────────────── -->
        <TabPanel value="valores">
          <div class="flex justify-content-between align-items-end gap-3 mt-3 mb-3 flex-wrap">
            <div class="flex gap-2 flex-wrap">
              <InputText v-model="filterValores" placeholder="Buscar por descripción..." style="width: 240px" />
              <InputText v-model="filterTipo" placeholder="Filtrar por tipo..." style="width: 180px" />
            </div>
            <div class="flex gap-2">
              <Button
                label="Actualizar"
                icon="pi pi-refresh"
                severity="secondary"
                outlined
                :loading="loadingValores"
                @click="loadValores(true)"
              />
              <Button
                label="Nuevo valor"
                icon="pi pi-plus"
                @click="router.push('/valores-categoria/nuevo')"
              />
            </div>
          </div>

          <Message
            v-if="deleteValorError"
            severity="error"
            :closable="true"
            class="mb-3"
            @close="deleteValorError = null"
          >
            {{ deleteValorError }}
          </Message>

          <DataTable
            :value="filteredValores"
            :loading="loadingValores"
            striped-rows
            :sort-field="'descripcion'"
            :sort-order="1"
            paginator
            :rows="15"
            :rows-per-page-options="[10, 15, 25, 50]"
          >
            <template #empty>
              <span class="muted">
                {{ filterValores || filterTipo ? 'Sin resultados para el filtro aplicado.' : 'No hay valores cargados.' }}
              </span>
            </template>
            <Column field="id" header="ID" style="width: 5rem; text-align: right" />
            <Column field="descripcion" header="Descripción" sortable />
            <Column field="tipo" header="Tipo" sortable>
              <template #body="{ data }">
                {{ data.idTipo }} - {{ data.tipo }}
              </template>
            </Column>
            <Column header="Items" style="width: 6rem; text-align: right">
              <template #body="{ data }">{{ data.cantidadItems }}</template>
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
                    @click="router.push(`/valores-categoria/${data.id}`)"
                  />
                  <Button
                    icon="pi pi-trash"
                    size="small"
                    severity="danger"
                    text
                    rounded
                    @click="confirmDeleteValor(data)"
                  />
                </div>
              </template>
            </Column>
          </DataTable>
        </TabPanel>

        <!-- ── Tipos ──────────────────────────────────────────────────────── -->
        <TabPanel value="tipos">
          <div class="flex justify-content-end mt-3 mb-3">
            <Button label="Nuevo tipo" icon="pi pi-plus" @click="openCreateTipo" />
          </div>

          <Message
            v-if="deleteTipoError"
            severity="error"
            :closable="true"
            class="mb-3"
            @close="deleteTipoError = null"
          >
            {{ deleteTipoError }}
          </Message>

          <DataTable
            :value="tipos"
            :loading="loadingTipos"
            striped-rows
            :sort-field="'descripcion'"
            :sort-order="1"
          >
            <template #empty>
              <span class="muted">No hay tipos cargados.</span>
            </template>
            <Column field="id" header="ID" style="width: 5rem; text-align: right" />
            <Column field="descripcion" header="Descripción" sortable />
            <Column style="width: 10rem">
              <template #body="{ data }">
                <div class="flex gap-1 align-items-center">
                  <Button
                    icon="pi pi-pencil"
                    size="small"
                    severity="secondary"
                    outlined
                    rounded
                    @click="openEditTipo(data)"
                  />
                  <Button
                    icon="pi pi-trash"
                    size="small"
                    severity="danger"
                    text
                    rounded
                    @click="confirmDeleteTipo(data)"
                  />
                </div>
              </template>
            </Column>
          </DataTable>

          <ValorCategoriaTipoDialog ref="tipoDialogRef" @save="handleTipoSave" />
        </TabPanel>
      </TabPanels>
    </Tabs>
  </section>
</template>
