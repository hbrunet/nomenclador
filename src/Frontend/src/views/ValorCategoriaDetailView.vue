<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import Button from 'primevue/button'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import ProgressSpinner from 'primevue/progressspinner'
import ValorCategoriaItemDialog from '../components/ValorCategoriaItemDialog.vue'
import { valoresCategoriaService } from '../services/valoresCategoriaService'
import type {
  CatalogItem,
  ValorCategoriaItemCreateUpdateDto,
  ValorCategoriaItemInputDto,
} from '../types/configuration'

const route = useRoute()
const router = useRouter()

const valorId = computed(() => {
  const id = Number(route.params.id)
  return Number.isFinite(id) && id > 0 ? id : null
})
const isNew = computed(() => !valorId.value)

const loading = ref(false)
const saving = ref(false)
const descripcion = ref('')
const selectedTipoId = ref<number | null>(null)
const items = ref<ValorCategoriaItemInputDto[]>([])
const tipos = ref<CatalogItem[]>([])
const dialogRef = ref<InstanceType<typeof ValorCategoriaItemDialog> | null>(null)
const editingItemId = ref<number | null>(null)

async function load() {
  const [tiposData] = await Promise.all([
    valoresCategoriaService.getTipos(),
    valorId.value ? loadDetalle() : Promise.resolve(),
  ])
  tipos.value = tiposData
}

async function loadDetalle() {
  if (!valorId.value) return
  loading.value = true
  try {
    const data = await valoresCategoriaService.getById(valorId.value)
    descripcion.value = data.descripcion
    selectedTipoId.value = data.idTipo || null
    items.value = [...data.items]
  } finally {
    loading.value = false
  }
}

async function handleSave() {
  if (!selectedTipoId.value) return
  saving.value = true
  try {
    const dto = { descripcion: descripcion.value, idTipo: selectedTipoId.value }
    if (isNew.value) {
      const created = await valoresCategoriaService.create(dto)
      await router.replace(`/valores-categoria/${created.id}`)
    } else {
      await valoresCategoriaService.update(valorId.value!, dto)
    }
  } finally {
    saving.value = false
  }
}

function openCreateItem() {
  editingItemId.value = null
  const maxNumero = items.value.reduce((max, i) => Math.max(max, i.numeroCategoria), 0)
  dialogRef.value?.open('create', { numeroCategoria: maxNumero + 1 })
}

function openEditItem(item: ValorCategoriaItemInputDto) {
  editingItemId.value = item.id
  dialogRef.value?.open('edit', { numeroCategoria: item.numeroCategoria, importe: item.importe })
}

async function handleItemSave(dto: ValorCategoriaItemCreateUpdateDto) {
  if (!valorId.value) return
  if (editingItemId.value) {
    const updated = await valoresCategoriaService.updateItem(valorId.value, editingItemId.value, dto)
    const idx = items.value.findIndex((i) => i.id === editingItemId.value)
    if (idx !== -1) items.value[idx] = updated
  } else {
    const created = await valoresCategoriaService.createItem(valorId.value, dto)
    items.value = [...items.value, created].sort((a, b) => a.numeroCategoria - b.numeroCategoria)
  }
}

async function handleDeleteItem(item: ValorCategoriaItemInputDto) {
  if (!valorId.value) return
  await valoresCategoriaService.deleteItem(valorId.value, item.id)
  items.value = items.value.filter((i) => i.id !== item.id)
}

onMounted(load)
watch(() => route.params.id, loadDetalle)
</script>

<template>
  <div>
    <div v-if="loading" class="flex align-items-center justify-content-center p-8">
      <ProgressSpinner style="width: 2.5rem; height: 2.5rem" />
    </div>

    <section v-else class="panel p-4 flex flex-column gap-4">
      <div class="flex justify-content-between align-items-center">
        <h2 class="text-xl mt-0 mb-0 font-semibold">
          {{ isNew ? 'Nuevo valor por categoría' : 'Editar valor por categoría' }}
        </h2>
        <Button
          label="Volver"
          severity="secondary"
          text
          icon="pi pi-arrow-left"
          @click="router.push('/valores-categoria')"
        />
      </div>

      <div class="flex align-items-end gap-3 flex-wrap" style="max-width: 640px">
        <div class="flex flex-column gap-1 flex-1" style="min-width: 220px">
          <label class="field-label">Descripción</label>
          <InputText v-model="descripcion" class="w-full" placeholder="Descripción del valor..." />
        </div>
        <div class="flex flex-column gap-1" style="width: 200px">
          <label class="field-label">Tipo</label>
          <Select
            v-model="selectedTipoId"
            :options="tipos"
            option-label="descripcion"
            option-value="id"
            placeholder="Seleccionar tipo..."
            class="w-full"
          />
        </div>
        <Button
          label="Guardar"
          icon="pi pi-check"
          :loading="saving"
          :disabled="!descripcion.trim() || !selectedTipoId"
          @click="handleSave"
        />
      </div>

      <template v-if="!isNew">
        <div class="flex justify-content-between align-items-center">
          <h3 class="text-base mt-0 mb-0 font-semibold">Items (N° categoría / Importe)</h3>
          <Button
            label="Agregar item"
            icon="pi pi-plus"
            size="small"
            severity="secondary"
            @click="openCreateItem"
          />
        </div>

        <DataTable
          :value="items"
          striped-rows
          :sort-field="'numeroCategoria'"
          :sort-order="1"
        >
          <template #empty>
            <span class="muted">Sin items. Usá "Agregar item" para incorporar el primero.</span>
          </template>
          <Column
            field="numeroCategoria"
            header="N° Categoría"
            sortable
            style="width: 8rem; text-align: right"
          />
          <Column header="Importe" sortable sort-field="importe" style="text-align: right">
            <template #body="{ data }">
              {{ data.importe.toLocaleString('es-AR', { minimumFractionDigits: 2 }) }}
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
                  rounded
                  @click="openEditItem(data)"
                />
                <Button
                  icon="pi pi-trash"
                  size="small"
                  severity="danger"
                  text
                  rounded
                  @click="handleDeleteItem(data)"
                />
              </div>
            </template>
          </Column>
        </DataTable>
      </template>

      <ValorCategoriaItemDialog ref="dialogRef" @save="handleItemSave" />
    </section>
  </div>
</template>
