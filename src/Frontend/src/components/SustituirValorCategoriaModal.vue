<script setup lang="ts">
import { computed, ref } from 'vue'
import Dialog from 'primevue/dialog'
import DatePicker from 'primevue/datepicker'
import Select from 'primevue/select'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import { configurationService } from '../services/configurationService'
import { valoresCategoriaService } from '../services/valoresCategoriaService'
import { formatLocalDate } from '../utils/date'
import type { ConfiguracionNomencladorDetailDto, SustitucionValorCategoriaMatch } from '../types/configuration'

type SelectedRow = { idValorCategoria: number; idTipo: number; tipo: string }

const emit = defineEmits<{
  (e: 'substituted', detail: ConfiguracionNomencladorDetailDto): void
}>()

const toast = useToast()
const confirm = useConfirm()

const isVisible = ref(false)
const configuracionId = ref<number | null>(null)
const rows = ref<SelectedRow[]>([])
const periodo = ref<Date | null>(null)
const matches = ref<SustitucionValorCategoriaMatch[] | null>(null)
const searching = ref(false)
const applying = ref(false)
// Por tipo: id del valor por categoría elegido para reemplazar. Se autocompleta cuando hay un
// único candidato; cuando hay varios (ambiguo), queda null hasta que el usuario elige uno a mano.
const seleccion = ref<Record<number, number | null>>({})

const tiposResumen = computed(() => {
  const map = new Map<number, { idTipo: number; tipo: string; cantidad: number }>()
  for (const row of rows.value) {
    const entry = map.get(row.idTipo)
    if (entry) entry.cantidad += 1
    else map.set(row.idTipo, { idTipo: row.idTipo, tipo: row.tipo, cantidad: 1 })
  }
  return [...map.values()].sort((a, b) => a.idTipo - b.idTipo)
})

const matchesByTipo = computed(() => new Map((matches.value ?? []).map((m) => [m.idTipo, m])))

// Entradas resueltas: una por tipo con id de valor por categoría elegido (automático si había un
// único candidato, manual si el usuario lo eligió en el caso ambiguo).
const resueltos = computed(() =>
  tiposResumen.value
    .map((t) => ({ idTipo: t.idTipo, idValorCategoria: seleccion.value[t.idTipo] ?? null }))
    .filter((r): r is { idTipo: number; idValorCategoria: number } => r.idValorCategoria !== null),
)

const canSearch = computed(() => periodo.value !== null && !searching.value)
const canAplicar = computed(() => resueltos.value.length > 0 && !applying.value)

function open(selectedRows: SelectedRow[], id: number) {
  rows.value = selectedRows
  configuracionId.value = id
  periodo.value = null
  matches.value = null
  seleccion.value = {}
  searching.value = false
  applying.value = false
  isVisible.value = true
}

function close() {
  isVisible.value = false
}

async function buscar() {
  if (!periodo.value) return
  searching.value = true
  matches.value = null
  seleccion.value = {}
  try {
    matches.value = await valoresCategoriaService.buscarPorTipoYPeriodo(
      tiposResumen.value.map((t) => t.idTipo),
      formatLocalDate(periodo.value),
    )
    // Autoselecciona cuando hay un único candidato; con varios, queda pendiente de elección manual.
    const nueva: Record<number, number | null> = {}
    for (const m of matches.value) {
      nueva[m.idTipo] = m.candidatos.length === 1 ? m.candidatos[0].idValorCategoria : null
    }
    seleccion.value = nueva
  } catch (e: any) {
    toast.add({
      severity: 'error',
      summary: 'Error',
      detail: e.response?.data?.mensaje ?? e.response?.data?.message ?? 'No se pudo buscar los valores del período indicado.',
      life: 5000,
    })
  } finally {
    searching.value = false
  }
}

function confirmAplicar() {
  if (!canAplicar.value) return
  confirm.require({
    message: `Se sustituirán los valores por categoría seleccionados de ${resueltos.value.length} tipo(s) por el valor elegido para el período indicado. Esta acción no se puede deshacer.`,
    header: 'Confirmar sustitución',
    acceptLabel: 'Sustituir',
    rejectLabel: 'Cancelar',
    rejectProps: { severity: 'secondary', outlined: true },
    accept: () => aplicar(),
  })
}

async function aplicar() {
  if (!canAplicar.value || !configuracionId.value) return

  applying.value = true
  try {
    const newIds = [...new Set(resueltos.value.map((r) => r.idValorCategoria))]

    // No hace falta desasociar el valor viejo a mano: la config nunca tiene más de un
    // valor por tipo (AddValorPorCategoriaAsync ya lo reemplaza al agregar), así que
    // asociarValoresCategoriasMasivo detecta ese único conflicto por tipo y lo borra solo.
    await configurationService.asociarValoresCategoriasMasivo({
      valoresCategoriasIds: newIds,
      configuracionesIds: [configuracionId.value],
    })

    const updated = await configurationService.getById(configuracionId.value)
    emit('substituted', updated)
    toast.add({
      severity: 'success',
      summary: 'Sustitución realizada',
      detail: `Se sustituyeron los valores por categoría de ${resueltos.value.length} tipo(s).`,
      life: 3000,
    })
    close()
  } catch (e: any) {
    toast.add({
      severity: 'error',
      summary: 'Error',
      detail: e.response?.data?.mensaje ?? 'No se pudo completar la sustitución.',
      life: 5000,
    })
  } finally {
    applying.value = false
  }
}

defineExpose({ open })
</script>

<template>
  <Dialog v-model:visible="isVisible" header="Sustituir valores por categoría" :modal="true" :style="{ width: '40rem' }">
    <div class="flex flex-column gap-4">
      <div class="flex flex-column gap-2">
        <div
          v-for="t in tiposResumen"
          :key="t.idTipo"
          class="flex justify-content-between align-items-center gap-3 p-2 border-1 border-round"
          style="border-color: #e2e8f0"
        >
          <span>{{ t.idTipo }} - {{ t.tipo }}</span>
          <template v-if="matchesByTipo.get(t.idTipo)">
            <Tag
              v-if="matchesByTipo.get(t.idTipo)!.candidatos.length === 1"
              severity="success"
              :value="matchesByTipo.get(t.idTipo)!.candidatos[0].descripcion"
            />
            <Select
              v-else-if="matchesByTipo.get(t.idTipo)!.candidatos.length > 1"
              v-model="seleccion[t.idTipo]"
              :options="matchesByTipo.get(t.idTipo)!.candidatos"
              option-label="descripcion"
              option-value="idValorCategoria"
              placeholder="Varios candidatos: elegir uno"
              show-clear
              style="min-width: 16rem"
            />
            <Tag v-else severity="danger" value="No encontrado" />
          </template>
        </div>
      </div>

      <div class="flex flex-column gap-1">
        <label class="field-label">Período a buscar</label>
        <DatePicker v-model="periodo" view="month" date-format="mm/yy" class="w-full" />
      </div>

      <Message v-if="matches && resueltos.length === 0" severity="warn" :closable="false">
        No hay ningún tipo resuelto todavía: elegí un candidato para los tipos ambiguos o probá otro período.
      </Message>
    </div>

    <template #footer>
      <Button label="Cerrar" severity="secondary" outlined :disabled="applying" @click="close" />
      <Button label="Buscar" icon="pi pi-search" severity="secondary" :disabled="!canSearch" :loading="searching" @click="buscar" />
      <Button label="Sustituir" icon="pi pi-sync" :disabled="!canAplicar" :loading="applying" @click="confirmAplicar" />
    </template>
  </Dialog>
</template>
