<script setup lang="ts">
import { computed, ref } from 'vue'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import InputNumber from 'primevue/inputnumber'
import Message from 'primevue/message'
import { useToast } from 'primevue/usetoast'
import { valoresFijosService } from '../services/valoresFijosService'
import type { ValorFijoCatalogItem } from '../types/configuration'

const emit = defineEmits<{
  (e: 'cloned', item: ValorFijoCatalogItem): void
}>()

const toast = useToast()
const isVisible = ref(false)
const loading = ref(false)
const saving = ref(false)
const sourceId = ref<number | null>(null)
const source = ref<ValorFijoCatalogItem | null>(null)
const descripcion = ref('')
const coeficienteAjuste = ref<number>(1)

const isValidDescripcion = computed(() => descripcion.value.trim().length > 0 && descripcion.value.length <= 40)
// Solo previsualización: el valor real se calcula y persiste en el backend con decimal.
// +Number.EPSILON mitiga el error de precisión binaria de JS (ej. 1.005 * 100 = 100.4999...).
const valorResultante = computed(() => {
  if (!source.value) return 0
  return Math.round((source.value.valor * (coeficienteAjuste.value ?? 0) + Number.EPSILON) * 100) / 100
})

async function open(id: number) {
  sourceId.value = id
  source.value = null
  descripcion.value = ''
  coeficienteAjuste.value = 1
  isVisible.value = true

  loading.value = true
  try {
    const data = await valoresFijosService.getById(id)
    source.value = data
    descripcion.value = `Copia de ${data.descripcion}`
  } finally {
    loading.value = false
  }
}

async function handleClone() {
  if (!sourceId.value) return

  const coef = coeficienteAjuste.value ?? 0
  if (coef <= 0) {
    toast.add({
      severity: 'error',
      summary: 'Coeficiente inválido',
      detail: 'El coeficiente de ajuste debe ser mayor a cero.',
      life: 2500,
    })
    return
  }

  saving.value = true
  try {
    const dto = { descripcion: descripcion.value.trim(), coeficienteAjuste: coef }
    const cloned = await valoresFijosService.clone(sourceId.value, dto)
    emit('cloned', cloned)
    toast.add({
      severity: 'success',
      summary: 'Valor fijo clonado',
      detail: 'El valor fijo se clonó correctamente.',
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
    header="Clonar valor fijo"
    :modal="true"
    :style="{ width: '26rem' }"
  >
    <div class="flex flex-column gap-4 pt-2">
      <div v-if="source" class="muted">
        Origen: <strong>{{ source.descripcion }}</strong> - {{ source.tipo }} - valor
        {{ source.valor?.toLocaleString('es-AR', { minimumFractionDigits: 2 }) }}
      </div>

      <div class="field">
        <label class="field-label">Descripción</label>
        <InputText
          v-model="descripcion"
          class="w-full"
          placeholder="Descripción del nuevo valor..."
          autofocus
          :disabled="loading"
        />
        <Message v-if="!isValidDescripcion" severity="error" size="small" variant="simple">
          La descripción es obligatoria y debe tener como máximo 40 caracteres.
        </Message>
      </div>

      <div class="field">
        <label class="field-label">Coeficiente de ajuste</label>
        <InputNumber
          v-model="coeficienteAjuste"
          input-id="coeficienteAjuste"
          :min-fraction-digits="2"
          :max-fraction-digits="4"
          :min="0"
          :input-style="{ textAlign: 'right' }"
          :disabled="loading"
          fluid
        />
      </div>

      <div v-if="source" class="muted">
        Valor resultante:
        <strong>{{ valorResultante.toLocaleString('es-AR', { minimumFractionDigits: 2 }) }}</strong>
      </div>
    </div>

    <template #footer>
      <Button label="Cancelar" severity="secondary" @click="isVisible = false" />
      <Button
        label="Clonar"
        icon="pi pi-clone"
        :loading="saving"
        :disabled="loading || !isValidDescripcion"
        @click="handleClone"
      />
    </template>
  </Dialog>
</template>
