<script setup lang="ts">
import { ref } from 'vue'
import { configurationService } from '../services/configurationService'
import type { ValorFijoCatalogItem } from '../types/configuration'

type SavedPayload =
  | { mode: 'updated'; item: ValorFijoCatalogItem }
  | { mode: 'replaced'; oldId: number; newItem: ValorFijoCatalogItem }

const emit = defineEmits<{
  (e: 'saved', payload: SavedPayload): void
}>()

const dialogRef = ref<HTMLDialogElement | null>(null)
const item = ref<ValorFijoCatalogItem | null>(null)
const mode = ref<'update-all' | 'create-new'>('update-all')
const newValor = ref(0)
const newDescripcion = ref('')
const usagesCount = ref<number | null>(null)
const loadingUsages = ref(false)
const saving = ref(false)

async function open(valorFijo: ValorFijoCatalogItem) {
  item.value = valorFijo
  mode.value = 'update-all'
  newValor.value = valorFijo.valor
  newDescripcion.value = valorFijo.descripcion
  usagesCount.value = null
  saving.value = false
  dialogRef.value?.showModal()

  loadingUsages.value = true
  try {
    const result = await configurationService.getValorFijoUsages(valorFijo.id)
    usagesCount.value = result.count
  } finally {
    loadingUsages.value = false
  }
}

function close() {
  dialogRef.value?.close()
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
  <dialog ref="dialogRef" class="valor-fijo-dialog">
    <div class="stack">
      <div class="dialog-header">
        <div>
          <h3 class="dialog-title">Modificar valor</h3>
          <span class="muted">{{ item?.descripcion }} · {{ item?.tipo }}</span>
        </div>
        <button class="secondary-button" type="button" @click="close">Cerrar</button>
      </div>

      <div class="mode-options">
        <label class="mode-option" :class="{ selected: mode === 'update-all' }">
          <input type="radio" v-model="mode" value="update-all" />
          <div class="mode-option-text">
            <strong>Actualizar para todas las configuraciones</strong>
            <p class="muted">
              Modifica el registro del catálogo.
              <template v-if="loadingUsages"> Calculando usos...</template>
              <template v-else-if="usagesCount !== null">
                Afecta a <strong>{{ usagesCount }}</strong>
                {{ usagesCount === 1 ? 'configuración' : 'configuraciones' }} actualmente.
              </template>
            </p>
          </div>
        </label>

        <label class="mode-option" :class="{ selected: mode === 'create-new' }">
          <input type="radio" v-model="mode" value="create-new" />
          <div class="mode-option-text">
            <strong>Usar un valor diferente solo aquí</strong>
            <p class="muted">
              Crea un nuevo registro en el catálogo exclusivo para esta configuración. Las demás no
              se modifican.
            </p>
          </div>
        </label>
      </div>

      <div class="form-grid">
        <label>
          <span>Nuevo valor</span>
          <input v-model.number="newValor" type="number" step="0.01" min="0" />
        </label>

        <label v-if="mode === 'create-new'">
          <span>Descripción del nuevo registro</span>
          <input v-model="newDescripcion" type="text" />
        </label>
      </div>

      <div class="dialog-actions">
        <button class="secondary-button" type="button" :disabled="saving" @click="close">
          Cancelar
        </button>
        <button class="primary-button" type="button" :disabled="saving" @click="handleSave">
          {{ saving ? 'Guardando...' : 'Aplicar cambio' }}
        </button>
      </div>
    </div>
  </dialog>
</template>

<style scoped>
.valor-fijo-dialog {
  border: 1px solid #dbe4f0;
  border-radius: 1rem;
  padding: 1.5rem;
  width: min(520px, 90vw);
  box-sizing: border-box;
  box-shadow: 0 12px 30px rgba(15, 23, 42, 0.12);
}

.valor-fijo-dialog::backdrop {
  background: rgba(15, 23, 42, 0.4);
}

.dialog-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
}

.dialog-title {
  margin: 0;
}

.mode-options {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.mode-option {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 0.875rem 1rem;
  border: 1px solid #dbe4f0;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: border-color 0.15s, background-color 0.15s;
}

.mode-option:hover {
  border-color: #94a3b8;
}

.mode-option.selected {
  border-color: #3b82f6;
  background-color: #eff6ff;
}

.mode-option input[type='radio'] {
  width: auto;
  margin-top: 0.2rem;
  flex-shrink: 0;
}

.mode-option-text {
  min-width: 0;
  flex: 1;
}

.mode-option p {
  margin: 0.25rem 0 0;
  font-size: 0.875rem;
}

.dialog-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
}
</style>
