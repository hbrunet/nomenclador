<script setup lang="ts">
import { computed, ref } from 'vue'
import Message from 'primevue/message'
import Dialog from 'primevue/dialog'
import RadioButton from 'primevue/radiobutton'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import Button from 'primevue/button'
import { configurationService } from '../services/configurationService'
import type { ValorFijoCatalogItem } from '../types/configuration'

type SavedPayload =
  | { mode: 'updated'; item: ValorFijoCatalogItem }
  | { mode: 'replaced'; oldId: number; newItem: ValorFijoCatalogItem }

const emit = defineEmits<{
  (e: 'saved', payload: SavedPayload): void
}>()

const props = defineProps<{
  configuracionId?: number
}>()

const isVisible = ref(false)
const item = ref<ValorFijoCatalogItem | null>(null)
const mode = ref<'update-all' | 'create-new'>('update-all')
const newValor = ref<number>(0)
const newDescripcion = ref('')
const usagesCount = ref<number | null>(null)
const loadingUsages = ref(false)
const saving = ref(false)
const isValidDescripcion = computed(() => newDescripcion.value.trim().length > 0 && newDescripcion.value.trim().length <= 40)

async function open(valorFijo: ValorFijoCatalogItem) {
  item.value = valorFijo
  mode.value = 'update-all'
  newValor.value = valorFijo.valor
  newDescripcion.value = valorFijo.descripcion
  usagesCount.value = null
  saving.value = false
  isVisible.value = true

  loadingUsages.value = true
  try {
    const result = await configurationService.getValorFijoUsages(valorFijo.id)
    usagesCount.value = result.count
  } finally {
    loadingUsages.value = false
  }
}

function close() {
  isVisible.value = false
}

async function handleSave() {
  if (!item.value) return
  saving.value = true
  try {
    if (mode.value === 'update-all') {
      const updated = await configurationService.updateValorFijo(item.value.id, newValor.value)
      emit('saved', { mode: 'updated', item: updated })
    } else {
      const created = await configurationService.createValorFijo({
        descripcion: newDescripcion.value,
        idTipo: item.value.idTipo,
        valor: newValor.value,
        configuracionId: props.configuracionId,
      })
      emit('saved', { mode: 'replaced', oldId: item.value.id, newItem: created })
    }
    close()
  } finally {
    saving.value = false
  }
}

defineExpose({ open })
</script>

<template>
  <Dialog
    v-model:visible="isVisible"
    :header="item?.descripcion"
    :modal="true"
    :style="{ width: '32rem' }"
    :closable="true"
  >
    <template #default>
      <div class="flex flex-column gap-4">
        <p class="muted m-0">{{ item?.tipo }}</p>

        <div class="flex flex-column gap-2">
          <label
            class="flex align-items-start gap-3 p-3 border-1 border-round cursor-pointer"
            :style="mode === 'update-all' ? { borderColor: '#3b82f6', background: '#eff6ff' } : { borderColor: '#e2e8f0' }"
          >
            <RadioButton v-model="mode" value="update-all" input-id="mode-update-all" />
            <div class="flex flex-column gap-1">
              <strong>Actualizar para todas las configuraciones</strong>
              <p class="muted m-0">
                <template v-if="loadingUsages"> Calculando usos...</template>
                <template v-else-if="usagesCount !== null">
                  Afecta a <strong>{{ usagesCount }}</strong>
                  {{ usagesCount === 1 ? 'configuración' : 'configuraciones' }} actualmente.
                </template>
              </p>
            </div>
          </label>

          <label
            class="flex align-items-start gap-3 p-3 border-1 border-round cursor-pointer"
            :style="mode === 'create-new' ? { borderColor: '#3b82f6', background: '#eff6ff' } : { borderColor: '#e2e8f0' }"
          >
            <RadioButton v-model="mode" value="create-new" input-id="mode-create-new" />
            <div class="flex flex-column gap-1">
              <strong>Usar un valor diferente solo aquí</strong>
              <p class="muted m-0">
                Crea un nuevo valor fijo exclusivo para esta configuración.
              </p>
            </div>
          </label>
        </div>

        <div class="flex flex-column gap-3">
          <div class="flex flex-column gap-1">
            <label class="field-label" for="new-valor">Nuevo valor</label>
            <InputNumber
              v-model="newValor"
              input-id="new-valor"
              :min-fraction-digits="2"
              :max-fraction-digits="2"
              :min="0"
              :input-style="{ textAlign: 'right'}"
              fluid
            />
          </div>

          <div v-if="mode === 'create-new'" class="flex flex-column gap-1">
            <label class="field-label" for="new-descripcion">Descripción</label>
            <InputText id="new-descripcion" v-model="newDescripcion" fluid />
            <Message
              v-if="!isValidDescripcion"
              severity="error">
              La descripción es obligatoria y debe tener como máximo 40 caracteres.</Message>
          </div>
        </div>
      </div>
    </template>

    <template #footer>
      <Button label="Cancelar" severity="secondary" :disabled="saving" @click="close" />
      <Button
        label="Guardar"
        icon="pi pi-check"
        :disabled="saving || (mode === 'create-new' && !isValidDescripcion)"
        :loading="saving"
        @click="handleSave"
      />
    </template>
  </Dialog>
</template>
