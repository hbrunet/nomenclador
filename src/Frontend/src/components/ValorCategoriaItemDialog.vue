<script setup lang="ts">
import { ref } from 'vue'
import Dialog from 'primevue/dialog'
import InputNumber from 'primevue/inputnumber'
import Button from 'primevue/button'
import type { ValorCategoriaItemCreateUpdateDto } from '../types/configuration'

const emit = defineEmits<{
  (e: 'save', dto: ValorCategoriaItemCreateUpdateDto): void
}>()

const isVisible = ref(false)
const isEditMode = ref(false)
const form = ref<ValorCategoriaItemCreateUpdateDto>({ numeroCategoria: 0, importe: 0 })

function open(mode: 'create' | 'edit', initial?: Partial<ValorCategoriaItemCreateUpdateDto>) {
  isEditMode.value = mode === 'edit'
  form.value = {
    numeroCategoria: initial?.numeroCategoria ?? 0,
    importe: initial?.importe ?? 0,
  }
  isVisible.value = true
}

function handleSave() {
  emit('save', { ...form.value })
  isVisible.value = false
}

defineExpose({ open })
</script>

<template>
  <Dialog
    v-model:visible="isVisible"
    :header="isEditMode ? 'Editar item' : 'Agregar item'"
    :modal="true"
    :style="{ width: '22rem' }"
  >
    <div class="flex flex-column gap-3 pt-2">
      <div class="flex flex-column gap-1">
        <label class="field-label">N° de categoría</label>
        <InputNumber v-model="form.numeroCategoria" :min="1" :use-grouping="false" fluid />
      </div>
      <div class="flex flex-column gap-1">
        <label class="field-label">Importe</label>
        <InputNumber
          v-model="form.importe"
          :min-fraction-digits="2"
          :max-fraction-digits="2"
          :min="0"
          fluid
        />
      </div>
    </div>

    <template #footer>
      <Button label="Cancelar" severity="secondary" @click="isVisible = false" />
      <Button
        :label="isEditMode ? 'Guardar' : 'Agregar'"
        :icon="isEditMode ? 'pi pi-check' : 'pi pi-plus'"
        :disabled="!form.numeroCategoria"
        @click="handleSave"
      />
    </template>
  </Dialog>
</template>
