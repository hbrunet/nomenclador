<script setup lang="ts">
import { ref } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import InputNumber from 'primevue/inputnumber'
import Message from 'primevue/message'
import { useToast } from 'primevue/usetoast'
import type { CategoriaCatalogItem, CategoriaMontoUpdateItem } from '../types/configuration'
import { configurationService } from '../services/configurationService'

const toast = useToast()

const props = defineProps<{
  categorias: CategoriaCatalogItem[]
}>()

const emit = defineEmits<{
  (e: 'montos-saved', categorias: CategoriaCatalogItem[]): void
}>()

const editMode = ref(false)
const editValues = ref<Record<number, number>>({})
const saving = ref(false)

function startEdit() {
  editValues.value = Object.fromEntries(props.categorias.map((c) => [c.id, c.monto]))
  editMode.value = true
}

function cancelEdit() {
  editMode.value = false
  editValues.value = {}
}

async function saveEdit() {
  saving.value = true
  try {
    const items: CategoriaMontoUpdateItem[] = props.categorias.map((c) => ({
      id: c.id,
      monto: editValues.value[c.id] ?? c.monto,
    }))
    await configurationService.updateCategoriaMontos(items)
    const updatedCategorias: CategoriaCatalogItem[] = props.categorias.map((c) => ({
      ...c,
      monto: editValues.value[c.id] ?? c.monto,
    }))
    editMode.value = false
    editValues.value = {}
    emit('montos-saved', updatedCategorias)
    toast.add({
      severity: 'success',
      summary: 'Montos actualizados',
      detail: 'Los montos de la escala se guardaron correctamente.',
      life: 2500,
    })
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="flex flex-column gap-3 pt-3">
    <Message v-if="editMode" severity="warn" :closable="true">
        Estás modificando los montos de una escala salarial compartida. Los cambios se aplicarán a
        <strong>todas las configuraciones</strong> que usen esta misma escala.
    </Message>
    <div class="flex justify-content-end">
      <div v-if="editMode" class="flex gap-2">
        <Button label="Cancelar" severity="secondary" :disabled="saving" @click="cancelEdit" />
        <Button label="Guardar montos" icon="pi pi-check" :loading="saving" @click="saveEdit" />
      </div>
      <Button
        v-else
        label="Editar montos"
        icon="pi pi-pencil"
        severity="secondary"
        :disabled="categorias.length === 0"
        @click="startEdit"
      />
    </div>

    <DataTable :value="categorias" striped-rows :sort-field="'numero'" :sort-order="1">
      <template #empty>
        <span class="muted">Sin categorías configuradas.</span>
      </template>
      <Column field="numero" header="N°" sortable />
      <Column field="descripcion" header="Descripción" sortable/>
      <Column header="Monto" style="text-align: right">
        <template #body="{ data }">
          <InputNumber
            v-if="editMode"
            v-model="editValues[data.id]"
            :min-fraction-digits="2"
            :max-fraction-digits="2"
            :min="0"
            :input-style="{ textAlign: 'right', width: '130px' }"
          />
          <template v-else>
            {{ data.monto.toLocaleString('es-AR', { minimumFractionDigits: 2 }) }}
          </template>
        </template>
      </Column>
    </DataTable>
  </div>
</template>
