<script setup lang="ts">
import Dialog from 'primevue/dialog'
import DatePicker from 'primevue/datepicker'
import Button from 'primevue/button'
import { ref } from 'vue'
import type { ConfiguracionNomencladorListItemDto } from '../types/configuration'
import { formatPeriodo } from '../utils/date'
const saving = ref(false)

const emit = defineEmits<{
  (e: 'clone', sourceId: number, dto: object): void
}>()

const isVisible = ref(false)
const fechaInicio = ref<Date | null>(null)
const fechaFin = ref<Date | null>(null)

let configSource: ConfiguracionNomencladorListItemDto

function open(source: ConfiguracionNomencladorListItemDto) {
  configSource = source
  isVisible.value = true
  fechaInicio.value = new Date()
  fechaFin.value = null
}

function handleClone() {
  if (fechaInicio.value) {
    saving.value = true

    emit('clone', configSource.id, {
        fechaInicio: fechaInicio.value, 
        fechaFin: fechaFin.value ? fechaFin.value : null,
        copiarConceptos: true,
        copiarValoresFijos: true,
        copiarValoresCategoria: true
    })
    isVisible.value = false
    saving.value = false
  }
}

defineExpose({ open })
</script>

<template>
  <Dialog
    v-model:visible="isVisible"
    header="Clonar configuración"
    :modal="true"
    :style="{ width: '22rem' }"
  >
    <p>{{ configSource?.nomencladorDescripcion }} {{ formatPeriodo(configSource?.fechaInicio) }} — {{ configSource?.fechaFin ? formatPeriodo(configSource?.fechaFin) : 'Vigente' }}</p>

    <div class="flex flex-column gap-1 pt-2">
        <label class="field-label">Fecha inicio</label>
        <DatePicker v-model="fechaInicio" type="date" class="w-full"  view="month" dateFormat="mm/yy"/>
    </div>

    <div class="flex flex-column gap-1 pt-2">
        <label class="field-label">Fecha fin</label>
        <DatePicker v-model="fechaFin" type="date" class="w-full" view="month" dateFormat="mm/yy"/>
    </div>

    <template #footer>
      <Button label="Cancelar" severity="secondary" @click="isVisible = false" />
      <Button
        label="Clonar"
        icon="pi pi-copy"
        @click="handleClone"
        :loading="saving"
      />
    </template>
  </Dialog>
</template>
