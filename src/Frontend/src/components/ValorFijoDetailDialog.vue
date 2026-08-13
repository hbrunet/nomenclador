<script setup lang="ts">
import { computed, ref } from 'vue'
import Message from 'primevue/message'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import Button from 'primevue/button'
import InputNumber from 'primevue/inputnumber'
import { useToast } from 'primevue/usetoast'
import { valoresFijosService } from '../services/valoresFijosService'
import type { CatalogItem, ValorFijoCatalogItem } from '../types/configuration'

const props = defineProps<{
  tipos: CatalogItem[]
}>()

const emit = defineEmits<{
  (e: 'saved', item: ValorFijoCatalogItem): void
}>()

const toast = useToast()
const isVisible = ref(false)
const loading = ref(false)
const saving = ref(false)
const editingId = ref<number | null>(null)
const descripcion = ref('')
const selectedTipoId = ref<number | null>(null)
const valor = ref<number>(0)

const isNew = computed(() => !editingId.value)
const isValidDescripcion = computed(() => descripcion.value.length > 0 && descripcion.value.length <= 40)

async function open(id?: number) {
  editingId.value = id ?? null
  descripcion.value = ''
  selectedTipoId.value = null
  valor.value = 0
  isVisible.value = true

  if (id) {
    loading.value = true
    try {
      const data = await valoresFijosService.getById(id)
      descripcion.value = data.descripcion
      selectedTipoId.value = data.idTipo || null
      valor.value = data.valor || 0
    } finally {
      loading.value = false
    }
  }
}

async function handleSave() {
  if (!selectedTipoId.value) return
  saving.value = true
  try {
    const dto = { descripcion: descripcion.value.trim(), idTipo: selectedTipoId.value, valor: valor.value }
    const saved = isNew.value
      ? await valoresFijosService.create(dto)
      : await valoresFijosService.update(editingId.value!, dto)
    emit('saved', saved)
    toast.add({
      severity: 'success',
      summary: isNew.value ? 'Valor fijo creado' : 'Valor fijo actualizado',
      detail: isNew.value ? 'El valor fijo se creó correctamente.' : 'Los cambios se guardaron correctamente.',
      life: 2500,
    })
    isVisible.value = false
  } finally {
    saving.value = false
  }
}

defineExpose({ open })
</script>

<template>
  <Dialog
    v-model:visible="isVisible"
    :header="isNew ? 'Nuevo valor fijo' : 'Editar valor fijo'"
    :modal="true"
    :style="{ width: '26rem' }"
  >
    <div class="flex flex-column gap-4 pt-2">
      <div class="field">
        <label class="field-label">Descripción</label>
        <InputText
          v-model="descripcion"
          class="w-full"
          placeholder="Descripción del valor..."
          autofocus
          :disabled="loading"
        />
         <Message v-if="!isValidDescripcion" severity="error" size="small" variant="simple">La descripción es obligatoria y debe tener como máximo 40 caracteres.</Message>
            
      </div>
      
      <div class="field">
        <label class="field-label">Tipo</label>
        <Select
          v-model="selectedTipoId"
          :options="props.tipos"
          option-label="descripcion"
          option-value="id"
          placeholder="Seleccionar tipo..."
          class="w-full"
          :disabled="loading"
          show-clear filter filter-placeholder="Buscar..."
        />
      </div>
      <div class="field">
        <label class="field-label">Valor</label>
        <InputNumber
          v-model="valor"
          input-id="valor"
          :min-fraction-digits="2"
          :max-fraction-digits="2"
          :min="0"
          :input-style="{ textAlign: 'right' }"
          :disabled="loading"
          fluid
        />
      </div>
    </div>

    <template #footer>
      <Button label="Cancelar" severity="secondary" @click="isVisible = false" />
      <Button
        label="Guardar"
        icon="pi pi-check"
        :loading="saving"
        :disabled="loading || !isValidDescripcion || !selectedTipoId"
        @click="handleSave"
      />
    </template>
  </Dialog>
</template>
