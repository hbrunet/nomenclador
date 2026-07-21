<script setup lang="ts">
import { ref } from 'vue'
import Dialog from 'primevue/dialog'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import InputNumber from 'primevue/inputnumber'
import type { ValorCategoriaConfiguradoInputDto, ValorCategoriaItemInputDto } from '../types/configuration'
import { configurationService } from '../services/configurationService'

const props = defineProps<{
  item: ValorCategoriaConfiguradoInputDto | null
  descripcion: string
  tipo: string
}>()

const isVisible = ref(false)
const isLoading = ref(false)
const isEditing = ref(false)
const isSaving = ref(false)
const valorCategoriaId = ref(0)
let originalItems: ValorCategoriaItemInputDto[] = []

async function open(id: number) {
  valorCategoriaId.value = id
  isEditing.value = false
  isLoading.value = true
  isVisible.value = true
  try {
    const items = await configurationService.getValorCategoriaConfiguradoItems(id)
    props.item?.items.splice(0, props.item.items.length, ...items)
  } finally {
    isLoading.value = false
  }
}

function startEdit() {
  originalItems = props.item?.items.map((i) => ({ ...i })) ?? []
  isEditing.value = true
}

function agregarItem() {
  if (!props.item) return
  const maxNumero = props.item.items.reduce((max, i) => Math.max(max, i.numeroCategoria), 0)
  props.item.items.push({ id: 0, numeroCategoria: maxNumero + 1, importe: 0 })
}

function eliminarItem(rowItem: ValorCategoriaItemInputDto) {
  if (!props.item) return
  const index = props.item.items.indexOf(rowItem)
  if (index !== -1) props.item.items.splice(index, 1)
}

async function save() {
  isSaving.value = true
  try {
    const saved = await configurationService.updateValorCategoriaItems(
      valorCategoriaId.value,
      props.item?.items ?? [],
    )
    props.item?.items.splice(0, props.item.items.length, ...saved)
    isEditing.value = false
    isVisible.value = false
  } finally {
    isSaving.value = false
  }
}

function cancelEdit() {
  if (props.item) {
    props.item.items.splice(0, props.item.items.length, ...originalItems)
  }
  isEditing.value = false
}

function close() {
  isVisible.value = false
}

defineExpose({ open })
</script>

<template>
  <Dialog
    v-model:visible="isVisible"
    :header="descripcion"
    :modal="true"
    :style="{ width: '36rem' }"
    :closable="!isEditing"
    @hide="isEditing = false"
  >
    <template #default>
      <p class="muted" style="margin: 0 0 1rem">{{ tipo }}</p>

      <div v-if="isEditing" class="flex justify-content-end mb-2">
        <Button label="Agregar" icon="pi pi-plus" size="small" severity="secondary" outlined @click="agregarItem" />
      </div>

      <DataTable :value="item?.items ?? []" :loading="isLoading" striped-rows :sort-field="'numeroCategoria'" :sort-order="1">
        <template #empty>
          <span class="muted">Sin items configurados.</span>
        </template>
        <Column field="numeroCategoria" header="N° Categoría" sortable style="text-align: right" />
        <Column header="Importe" style="text-align: right">
          <template #body="{ data }">
            <InputNumber
              v-if="isEditing"
              v-model="data.importe"
              :min-fraction-digits="2"
              :max-fraction-digits="2"
              :min="0"
              :input-style="{ textAlign: 'right', width: '130px' }"
            />
            <template v-else>
              {{ data.importe.toLocaleString('es-AR', { minimumFractionDigits: 2 }) }}
            </template>
          </template>
        </Column>
        <Column v-if="isEditing" style="width: 3rem">
          <template #body="{ data }">
            <Button
              icon="pi pi-trash"
              size="small"
              severity="danger"
              text
              rounded
              @click="eliminarItem(data)"
            />
          </template>
        </Column>
      </DataTable>
    </template>

    <template #footer>
      <template v-if="!isEditing">
        <Button label="Editar" icon="pi pi-pencil" severity="secondary" outlined :disabled="isLoading" @click="startEdit" />
        <Button label="Cerrar" severity="secondary" @click="close" />
      </template>
      <template v-else>
        <Button label="Cancelar" severity="secondary" :disabled="isSaving" @click="cancelEdit" />
        <Button label="Guardar" icon="pi pi-check" :loading="isSaving" @click="save" />
      </template>
    </template>
  </Dialog>
</template>
