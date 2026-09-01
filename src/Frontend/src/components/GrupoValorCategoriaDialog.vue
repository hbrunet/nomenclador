<script setup lang="ts">
import { computed, ref } from 'vue'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import MultiSelect from 'primevue/multiselect'
import Button from 'primevue/button'
import type { CatalogItem, GrupoValorCategoriaCreateUpdateDto } from '../types/configuration'

defineProps<{
  tipos: CatalogItem[]
}>()

const emit = defineEmits<{
  (e: 'save', dto: GrupoValorCategoriaCreateUpdateDto): void
}>()

const isVisible = ref(false)
const isEditMode = ref(false)
const descripcion = ref('')
const tiposIds = ref<number[]>([])

const isValid = computed(() => descripcion.value.trim().length > 0 && tiposIds.value.length > 0)

function open(mode: 'create' | 'edit', initial?: { descripcion: string; tiposIds: number[] }) {
  isEditMode.value = mode === 'edit'
  descripcion.value = initial?.descripcion ?? ''
  tiposIds.value = initial?.tiposIds ?? []
  isVisible.value = true
}

function handleSave() {
  if (!isValid.value) return
  emit('save', { descripcion: descripcion.value.trim(), tiposIds: tiposIds.value })
  isVisible.value = false
}

defineExpose({ open })
</script>

<template>
  <Dialog
    v-model:visible="isVisible"
    :header="isEditMode ? 'Editar grupo' : 'Nuevo grupo'"
    :modal="true"
    :style="{ width: '35rem' }"
  >
    <div class="flex flex-column gap-4 pt-2">
      <div class="field">
        <label class="field-label">Descripción</label>
        <InputText v-model="descripcion" class="w-full" placeholder="Nombre del grupo..." autofocus />
      </div>

      <div class="field">
        <label class="field-label">Tipos de valor por categoría</label>
        <MultiSelect
          v-model="tiposIds"
          :options="tipos"
          :option-label="(option) => `${option.id} - ${option.descripcion}`"
          option-value="id"
          placeholder="Seleccioná los tipos..."
          filter
          display="chip"
          class="w-full"
          :maxSelectedLabels="3"
        />
      </div>
    </div>

    <template #footer>
      <Button label="Cancelar" severity="secondary" @click="isVisible = false" />
      <Button
        :label="isEditMode ? 'Guardar' : 'Crear'"
        :icon="isEditMode ? 'pi pi-check' : 'pi pi-plus'"
        :disabled="!isValid"
        @click="handleSave"
      />
    </template>
  </Dialog>
</template>
