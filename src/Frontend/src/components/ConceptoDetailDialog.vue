<script setup lang="ts">
import { computed, ref } from 'vue'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import Select from 'primevue/select'
import Checkbox from 'primevue/checkbox'
import MultiSelect from 'primevue/multiselect'

import Button from 'primevue/button'
import Message from 'primevue/message'
import { useToast } from 'primevue/usetoast'
import { conceptosService } from '../services/conceptosService'
import type { CatalogItem, ConceptoCatalogItem, ConceptoCreateUpdateDto } from '../types/configuration'

const props = defineProps<{
  tipos: CatalogItem[]
  tiposLiquidacion: CatalogItem[]
}>()

const emit = defineEmits<{
  (e: 'saved', item: ConceptoCatalogItem): void
}>()

const toast = useToast()
const isVisible = ref(false)
const loading = ref(false)
const saving = ref(false)
const errorMessage = ref<string | null>(null)
const editingId = ref<number | null>(null)
const activeTab = ref('general')

const codigo = ref<number>(0)
const subcodigo = ref<number>(0)
const descripcionBreve = ref('')
const descripcion = ref('')
const idTipoConcepto = ref<number | null>(null)
const idPartidaPresupuestaria = ref<number | null>(null)
const mesesAplicables = ref<Set<number>>(new Set())
const tiposLiquidacionIds = ref<number[]>([])
// Sin checkbox propio en este formulario; se preserva el valor existente para no pisarlo al guardar.
const ppp = ref(false)

const flags = ref({
  acumulaJubilacion: false,
  acumulaObraSocial: false,
  acumulaRemunerativo: false,
  basico: false,
  bonificable: false,
  calculaTicket: false,
  calculaPorPersona: false,
  deduceJubilacion: false,
  deducePension: false,
  especial: false,
  ganancia: false,
  imprimeCantidad: false,
  liquidaSiempre: false,
  participaFondo: false,
  reliquidar: false,
})

const FLAG_LABELS: Record<keyof typeof flags.value, string> = {
  acumulaJubilacion: 'Acumula jubilación',
  acumulaObraSocial: 'Acumula obra social',
  acumulaRemunerativo: 'Acumula remunerativo',
  basico: 'Básico',
  bonificable: 'Bonificable',
  calculaTicket: 'Calcula ticket',
  calculaPorPersona: 'Calcula por persona',
  deduceJubilacion: 'Deduce jubilación',
  deducePension: 'Deduce pensión',
  especial: 'Especial',
  ganancia: 'Ganancia',
  imprimeCantidad: 'Imp. cantidad',
  liquidaSiempre: 'Liquida siempre',
  participaFondo: 'Fondo estimulo',
  reliquidar: 'Reliquidar',
}
const flagKeys = Object.keys(FLAG_LABELS) as (keyof typeof flags.value)[]

const MESES = [
  { value: 1, label: 'Ene' }, { value: 2, label: 'Feb' }, { value: 3, label: 'Mar' },
  { value: 4, label: 'Abr' }, { value: 5, label: 'May' }, { value: 6, label: 'Jun' },
  { value: 7, label: 'Jul' }, { value: 8, label: 'Ago' }, { value: 9, label: 'Sep' },
  { value: 10, label: 'Oct' }, { value: 11, label: 'Nov' }, { value: 12, label: 'Dic' },
]

const isNew = computed(() => !editingId.value)
const isValidDescripcion = computed(() => descripcion.value.trim().length > 0 && descripcion.value.length <= 60)

function toggleMes(mes: number) {
  const next = new Set(mesesAplicables.value)
  if (next.has(mes)) next.delete(mes)
  else next.add(mes)
  mesesAplicables.value = next
}

function resetForm() {
  codigo.value = 0
  subcodigo.value = 0
  descripcionBreve.value = ''
  descripcion.value = ''
  idTipoConcepto.value = null
  idPartidaPresupuestaria.value = null
  mesesAplicables.value = new Set()
  tiposLiquidacionIds.value = []
  ppp.value = false
  for (const key of flagKeys) flags.value[key] = false
}

async function open(id?: number) {
  editingId.value = id ?? null
  errorMessage.value = null
  activeTab.value = 'general'
  resetForm()
  isVisible.value = true

  if (id) {
    loading.value = true
    try {
      const data = await conceptosService.getById(id)
      codigo.value = data.codigo
      subcodigo.value = data.subcodigo
      descripcionBreve.value = data.descripcionBreve
      descripcion.value = data.descripcion
      idTipoConcepto.value = data.idTipoConcepto
      idPartidaPresupuestaria.value = data.idPartidaPresupuestaria
      mesesAplicables.value = new Set(data.mesesAplicables)
      tiposLiquidacionIds.value = data.tiposLiquidacion.map((t) => t.id)
      ppp.value = data.ppp
      for (const key of flagKeys) flags.value[key] = data[key]
    } finally {
      loading.value = false
    }
  }
}

async function handleSave() {
  errorMessage.value = null
  saving.value = true
  try {
    const dto: ConceptoCreateUpdateDto = {
      codigo: codigo.value,
      subcodigo: subcodigo.value,
      descripcionBreve: descripcionBreve.value.trim(),
      descripcion: descripcion.value.trim(),
      idTipoConcepto: idTipoConcepto.value,
      idPartidaPresupuestaria: idPartidaPresupuestaria.value,
      mesesAplicables: [...mesesAplicables.value],
      tiposLiquidacionIds: tiposLiquidacionIds.value,
      ppp: ppp.value,
      ...flags.value,
    }
    const saved = isNew.value
      ? await conceptosService.create(dto)
      : await conceptosService.update(editingId.value!, dto)
    emit('saved', saved)
    toast.add({
      severity: 'success',
      summary: isNew.value ? 'Concepto creado' : 'Concepto actualizado',
      detail: isNew.value ? 'El concepto se creó correctamente.' : 'Los cambios se guardaron correctamente.',
      life: 2500,
    })
    isVisible.value = false
  } catch (e: any) {
    errorMessage.value = e.response?.data?.mensaje ?? e.response?.data?.message ?? 'No se pudo guardar el concepto.'
  } finally {
    saving.value = false
  }
}

defineExpose({ open })
</script>

<template>
  <Dialog v-model:visible="isVisible" :header="isNew ? 'Nuevo concepto' : 'Editar concepto - ' + editingId"
    :modal="true" :style="{ width: '52rem' }">
    <Message v-if="errorMessage" severity="error" size="small" variant="simple" class="mb-3">{{ errorMessage }}
    </Message>

    <div class="flex flex-column gap-4 pt-3">
      <div class="grid formgrid">
        <div class="field col-6 md:col-3">
          <label class="field-label">Código</label>
          <InputNumber v-model="codigo" class="w-full" :disabled="loading" :use-grouping="false" fluid />
        </div>
        <div class="field col-6 md:col-3">
          <label class="field-label">Subcódigo</label>
          <InputNumber v-model="subcodigo" class="w-full" :disabled="loading" :use-grouping="false" fluid />
        </div>
        <div class="field col-12 md:col-6">
          <label class="field-label">Tipo de concepto</label>
          <Select v-model="idTipoConcepto" :options="props.tipos" option-label="descripcion" option-value="id"
            placeholder="Seleccionar tipo..." class="w-full" :disabled="loading" show-clear filter
            filter-placeholder="Buscar..." />
        </div>
      </div>

      <div class="field">
        <label class="field-label">Descripción breve</label>
        <InputText v-model="descripcionBreve" class="w-full" :disabled="loading" maxlength="10" />
      </div>

      <div class="field">
        <label class="field-label">Descripción</label>
        <InputText v-model="descripcion" class="w-full" :disabled="loading" maxlength="60" />
        <Message v-if="!isValidDescripcion" severity="error" size="small" variant="simple">La descripción es obligatoria
          y debe tener como máximo 60 caracteres.</Message>
      </div>


      <div class="field">
        <label class="field-label">Tipos de liquidación</label>
        <MultiSelect v-model="tiposLiquidacionIds" :options="props.tiposLiquidacion" option-label="descripcion"
          option-value="id" placeholder="Seleccioná los tipos de liquidación..." filter display="chip" class="w-full"
          :disabled="loading" :max-selected-labels="3" />
      </div>
      <div class="field">
        <label class="field-label">Comportamiento</label>
        <div class="grid">
          <div v-for="key in flagKeys" :key="key" class="col-6 md:col-6 lg:col-4 flex align-items-center gap-2">
            <Checkbox :input-id="`flag-${key}`" v-model="flags[key]" binary :disabled="loading" />
            <label :for="`flag-${key}`">{{ FLAG_LABELS[key] }}</label>
          </div>
        </div>
      </div>

      <div class="field">
        <label class="field-label">Meses de aplicación</label>
        <div class="flex flex-wrap gap-3">
          <div v-for="mes in MESES" :key="mes.value" class="flex align-items-center gap-2">
            <Checkbox :input-id="`mes-${mes.value}`" :model-value="mesesAplicables.has(mes.value)" binary
              :disabled="loading" @update:model-value="toggleMes(mes.value)" />
            <label :for="`mes-${mes.value}`">{{ mes.label }}</label>
          </div>
        </div>
      </div>
    </div>

    <template #footer>
      <Button label="Cancelar" severity="secondary" @click="isVisible = false" />
      <Button label="Guardar" icon="pi pi-check" :loading="saving" :disabled="loading || !isValidDescripcion"
        @click="handleSave" />
    </template>
  </Dialog>
</template>
