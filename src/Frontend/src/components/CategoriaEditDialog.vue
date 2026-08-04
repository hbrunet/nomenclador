<script setup lang="ts">
import { ref } from 'vue'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import Button from 'primevue/button'
import type { CategoriaCreateUpdateDto } from '../types/configuration'

const emit = defineEmits<{
  (e: 'save', dto: CategoriaCreateUpdateDto): void
}>()

const isVisible = ref(false)
const isEditMode = ref(false)
const form = ref<CategoriaCreateUpdateDto>({ numero: 0, descripcion: '', monto: 0 })

function open(mode: 'create' | 'edit', initial?: Partial<CategoriaCreateUpdateDto>) {
  isEditMode.value = mode === 'edit'
  form.value = {
    numero: initial?.numero ?? 0,
    descripcion: initial?.descripcion ?? '',
    monto: initial?.monto ?? 0,
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
    :header="isEditMode ? 'Editar categoría' : 'Agregar categoría'"
    :modal="true"
    :style="{ width: '26rem' }"
  >
    <div class="flex flex-column gap-3 pt-2">
      <div class="flex flex-column gap-1">
        <label class="field-label">N° de categoría</label>
        <InputNumber 
          v-model="form.numero" 
          :min="1" 
          :use-grouping="false" 
          :input-style="{ textAlign: 'right'}"
          fluid 
        />
      </div>
      <div class="flex flex-column gap-1">
        <label class="field-label">Descripción</label>
        <InputText v-model="form.descripcion" class="w-full" />
      </div>
      <div class="flex flex-column gap-1">
        <label class="field-label">Monto</label>
        <InputNumber
          v-model="form.monto"
          :min-fraction-digits="2"
          :max-fraction-digits="2"
          :min="0"
          :input-style="{ textAlign: 'right'}"
          fluid
        />
      </div>
    </div>

    <template #footer>
      <Button label="Cancelar" severity="secondary" @click="isVisible = false" />
      <Button
        :label="isEditMode ? 'Guardar' : 'Agregar'"
        :icon="isEditMode ? 'pi pi-check' : 'pi pi-plus'"
        :disabled="!form.descripcion.trim() || !form.numero"
        @click="handleSave"
      />
    </template>
  </Dialog>
</template>
