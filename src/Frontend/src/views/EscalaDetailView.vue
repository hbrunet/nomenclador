<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import ProgressSpinner from 'primevue/progressspinner'
import { useToast } from 'primevue/usetoast'
import CategoriaEditDialog from '../components/CategoriaEditDialog.vue'
import { escalasService } from '../services/escalasService'
import type { CategoriaCatalogItem, CategoriaCreateUpdateDto } from '../types/configuration'

const route = useRoute()
const router = useRouter()
const toast = useToast()

const escalaId = computed(() => {
  const id = Number(route.params.id)
  return Number.isFinite(id) && id > 0 ? id : null
})

const isNew = computed(() => !escalaId.value)

const loading = ref(false)
const saving = ref(false)
const descripcion = ref('')
const categorias = ref<CategoriaCatalogItem[]>([])
const dialogRef = ref<InstanceType<typeof CategoriaEditDialog> | null>(null)
const editingCatId = ref<number | null>(null)

async function load() {
  if (!escalaId.value) return
  loading.value = true
  try {
    const data = await escalasService.getById(escalaId.value)
    descripcion.value = data.descripcion
    categorias.value = [...data.categorias]
  } finally {
    loading.value = false
  }
}

async function handleSaveEscala() {
  saving.value = true
  try {
    if (isNew.value) {
      const created = await escalasService.create({ descripcion: descripcion.value })
      await router.replace(`/escalas/${created.id}`)
      toast.add({ severity: 'success', summary: 'Escala creada', detail: 'La escala salarial se creó correctamente.', life: 2500 })
    } else {
      await escalasService.update(escalaId.value!, { descripcion: descripcion.value })
      toast.add({ severity: 'success', summary: 'Escala actualizada', detail: 'Los cambios se guardaron correctamente.', life: 2500 })
    }
  } finally {
    saving.value = false
  }
}

function openCreateDialog() {
  editingCatId.value = null
  const maxNumero = categorias.value.reduce((max, c) => Math.max(max, c.numero), 0)
  dialogRef.value?.open('create', { numero: maxNumero + 1 })
}

function openEditDialog(cat: CategoriaCatalogItem) {
  editingCatId.value = cat.id
  dialogRef.value?.open('edit', { numero: cat.numero, descripcion: cat.descripcion, monto: cat.monto })
}

async function handleDialogSave(dto: CategoriaCreateUpdateDto) {
  if (!escalaId.value) return
  if (editingCatId.value) {
    const updated = await escalasService.updateCategoria(escalaId.value, editingCatId.value, dto)
    const idx = categorias.value.findIndex((c) => c.id === editingCatId.value)
    if (idx !== -1) categorias.value[idx] = updated
    toast.add({ severity: 'success', summary: 'Categoría actualizada', detail: 'La categoría se guardó correctamente.', life: 2500 })
  } else {
    const created = await escalasService.createCategoria(escalaId.value, dto)
    categorias.value = [...categorias.value, created].sort((a, b) => a.numero - b.numero)
    toast.add({ severity: 'success', summary: 'Categoría agregada', detail: 'La categoría se agregó correctamente.', life: 2500 })
  }
}

async function handleDeleteCategoria(cat: CategoriaCatalogItem) {
  if (!escalaId.value) return
  await escalasService.deleteCategoria(escalaId.value, cat.id)
  categorias.value = categorias.value.filter((c) => c.id !== cat.id)
}

onMounted(load)
watch(() => route.params.id, load)
</script>

<template>
  <div>
    <div v-if="loading" class="flex align-items-center justify-content-center p-8">
      <ProgressSpinner style="width: 2.5rem; height: 2.5rem" />
    </div>

    <section v-else class="panel p-4 flex flex-column gap-4">
      <div class="flex justify-content-between align-items-center">
        <h2 class="text-xl mt-0 mb-0 font-semibold">
          {{ isNew ? 'Nueva escala salarial' : 'Editar escala salarial' }}
        </h2>
        <Button
          label="Volver"
          severity="secondary"
          text
          icon="pi pi-arrow-left"
          @click="router.push('/escalas')"
        />
      </div>

      <div class="flex align-items-end gap-3" style="max-width: 520px">
        <div class="flex flex-column gap-1 flex-1">
          <label class="field-label">Descripción</label>
          <InputText v-model="descripcion" class="w-full" placeholder="Nombre de la escala..." />
        </div>
        <Button
          label="Guardar"
          icon="pi pi-check"
          :loading="saving"
          :disabled="!descripcion.trim()"
          @click="handleSaveEscala"
        />
      </div>

      <template v-if="!isNew">
        <div class="flex justify-content-between align-items-center">
          <h3 class="text-base mt-0 mb-0 font-semibold">Categorías</h3>
          <Button
            label="Agregar categoría"
            icon="pi pi-plus"
            size="small"
            severity="secondary"
            @click="openCreateDialog"
          />
        </div>

        <DataTable
          :value="categorias"
          striped-rows
          :sort-field="'numero'"
          :sort-order="1"
        >
          <template #empty>
            <span class="muted">Sin categorías. Usá "Agregar categoría" para incorporar la primera.</span>
          </template>
          <Column field="numero" header="N°" sortable style="width: 5rem; text-align: right" />
          <Column field="descripcion" header="Descripción" sortable />
          <Column header="Monto" sortable sort-field="monto" style="width: 10rem; text-align: right">
            <template #body="{ data }">
              {{ data.monto.toLocaleString('es-AR', { minimumFractionDigits: 2 }) }}
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
                  @click="openEditDialog(data)"
                />
                <Button
                  icon="pi pi-trash"
                  size="small"
                  severity="danger"
                  text
                  rounded
                  @click="handleDeleteCategoria(data)"
                />
              </div>
            </template>
          </Column>
        </DataTable>
      </template>

      <CategoriaEditDialog ref="dialogRef" @save="handleDialogSave" />
    </section>
  </div>
</template>
