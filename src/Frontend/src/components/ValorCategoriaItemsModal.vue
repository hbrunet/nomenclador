<script setup lang="ts">
import { ref } from 'vue'
import type { ValorCategoriaConfiguradoInputDto } from '../types/configuration'

defineProps<{
  item: ValorCategoriaConfiguradoInputDto | null
  descripcion: string
  tipo: string
}>()

const dialogRef = ref<HTMLDialogElement | null>(null)

function open() {
  dialogRef.value?.showModal()
}

function close() {
  dialogRef.value?.close()
}

defineExpose({ open })
</script>

<template>
  <dialog ref="dialogRef" class="items-dialog">
    <div class="stack">
      <div class="section-header">
        <div>
          <h3 class="dialog-title">{{ descripcion }}</h3>
          <span class="muted">{{ tipo }}</span>
        </div>
        <button class="secondary-button" type="button" @click="close">Cerrar</button>
      </div>

      <div class="table-wrapper">
        <table>
          <thead>
            <tr>
              <th>N° Categoría</th>
              <th>Importe</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="!item?.items?.length">
              <td colspan="2" class="muted">Sin items configurados.</td>
            </tr>
            <tr v-for="subitem in item?.items ?? []" :key="subitem.id">
              <td>{{ subitem.numeroCategoria }}</td>
              <td>
                <input v-model.number="subitem.importe" type="number" step="0.01" min="0" />
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </dialog>
</template>

<style scoped>
.items-dialog {
  border: 1px solid #dbe4f0;
  border-radius: 1rem;
  padding: 1.5rem;
  min-width: 420px;
  box-shadow: 0 12px 30px rgba(15, 23, 42, 0.12);
}

.items-dialog::backdrop {
  background: rgba(15, 23, 42, 0.4);
}

.dialog-title {
  margin: 0;
}
</style>
