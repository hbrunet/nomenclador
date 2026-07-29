<script setup lang="ts">
import { ref } from 'vue'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import type { ValorCategoriaTipoCreateUpdateDto } from '../types/configuration'

const emit = defineEmits<{
  (e: 'save', dto: ValorCategoriaTipoCreateUpdateDto): void
}>()

const isVisible = ref(false)
const isEditMode = ref(false)
const descripcion = ref('')

function open(mode: 'create' | 'edit', initial?: string) {
  isEditMode.value = mode === 'edit'
  descripcion.value = initial ?? ''
  isVisible.value = true
}

function handleSave() {
  emit('save', { descripcion: descripcion.value })
  isVisible.value = false
}

defineExpose({ open })
</script>

<template>
  <Dialog
    v-model:visible="isVisible"
    :header="isEditMode ? 'Editar tipo' : 'Nuevo tipo'"
    :modal="true"
    :style="{ width: '22rem' }"
  >
    <div class="flex flex-column gap-1 pt-2">
      <label class="field-label">Descripción</label>
      <InputText v-model="descripcion" class="w-full" autofocus />
    </div>

    <template #footer>
      <Button label="Cancelar" severity="secondary" @click="isVisible = false" />
      <Button
        :label="isEditMode ? 'Guardar' : 'Agregar'"
        :icon="isEditMode ? 'pi pi-check' : 'pi pi-plus'"
        :disabled="!descripcion.trim()"
        @click="handleSave"
      />
    </template>
  </Dialog>
</template>
