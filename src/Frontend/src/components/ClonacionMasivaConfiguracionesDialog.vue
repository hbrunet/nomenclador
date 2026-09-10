<script setup lang="ts">
import Dialog from 'primevue/dialog'
import DatePicker from 'primevue/datepicker'
import Button from 'primevue/button'
import Message from 'primevue/message'
import { ref } from 'vue'
import type { ConfiguracionNomencladorListItemDto } from '../types/configuration'
import { formatPeriodo } from '../utils/date'

const props = defineProps<{
  loading: boolean
}>()

const emit = defineEmits<{
  (e: 'clone-masivo', sourceIds: number[], dto: object): void
}>()

const isVisible = ref(false)
const fechaInicio = ref<Date | null>(null)
const fechaFin = ref<Date | null>(null)
let items: ConfiguracionNomencladorListItemDto[] = []
const selectedItems = ref<ConfiguracionNomencladorListItemDto[]>([])

function open(sources: ConfiguracionNomencladorListItemDto[]) {
  items = sources
  selectedItems.value = sources
  isVisible.value = true
  fechaInicio.value = null
  fechaFin.value = null
}

function close() {
  isVisible.value = false
}

function handleClone() {
  if (!fechaInicio.value || props.loading) return

  // El diálogo queda abierto (modal, sin poder cerrarse) hasta que el padre
  // llame a close() al terminar — evita que el usuario piense que no pasó
  // nada y dispare la clonación dos veces mientras la request (varios
  // segundos, N configuraciones) sigue en curso.
  emit(
    'clone-masivo',
    items.map((item) => item.id),
    {
      fechaInicio: fechaInicio.value,
      fechaFin: fechaFin.value ? fechaFin.value : null,
      copiarConceptos: true,
      copiarValoresFijos: true,
      copiarValoresCategoria: true,
    },
  )
}

defineExpose({ open, close })
</script>

<template>
  <Dialog
    v-model:visible="isVisible"
    header="Clonación masiva de configuraciones"
    :modal="true"
    :closable="!loading"
    :close-on-escape="!loading"
    :dismissable-mask="!loading"
    :style="{ width: '32rem' }"
  >
    <Message severity="info" :closable="false">
      Se clonarán las {{ selectedItems.length }} configuración(es) seleccionada(s) al nuevo período,
      y se les pondrá fecha fin (mes anterior al nuevo período) a las configuraciones originales.
    </Message>

    <ul class="m-0 mt-2" style="max-height: 180px; overflow-y: auto; padding-left: 1.2rem">
      <li v-for="item in selectedItems" :key="item.id" class="muted" style="font-size: 0.85rem">
        {{ item.nomencladorDescripcion }} — {{ item.escalaDescripcion }} — vigente desde {{ formatPeriodo(item.fechaInicio) }}
      </li>
    </ul>

    <div class="flex flex-column gap-1 pt-3">
      <label class="field-label">Fecha inicio del nuevo período</label>
      <DatePicker v-model="fechaInicio" type="date" class="w-full" view="month" dateFormat="mm/yy" :disabled="loading" />
    </div>

    <div class="flex flex-column gap-1 pt-2">
      <label class="field-label">Fecha fin del nuevo período (opcional)</label>
      <DatePicker v-model="fechaFin" type="date" class="w-full" view="month" dateFormat="mm/yy" :disabled="loading" />
    </div>

    <Message v-if="loading" severity="warn" :closable="false" class="mt-2">
      Clonando configuraciones, por favor esperá — no cierres ni repitas la operación.
    </Message>

    <template #footer>
      <Button label="Cancelar" severity="secondary" :disabled="loading" @click="close" />
      <Button
        label="Clonar seleccionadas"
        icon="pi pi-copy"
        :disabled="!fechaInicio || loading"
        @click="handleClone"
        :loading="loading"
      />
    </template>
  </Dialog>
</template>
