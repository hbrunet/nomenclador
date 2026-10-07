<script setup lang="ts">
import { computed, ref } from 'vue'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Listbox from 'primevue/listbox'
import Button from 'primevue/button'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import { useToast } from 'primevue/usetoast'
import { formulasService } from '../services/formulasService'
import { configurationService } from '../services/configurationService'
import type { PrimitivaItem } from '../types/configuration'

const emit = defineEmits<{
  (e: 'saved'): void
}>()

const toast = useToast()

const isVisible = ref(false)
const loading = ref(false)
const saving = ref(false)
const verifying = ref(false)
const errorMessage = ref<string | null>(null)

const formulaId = ref<number | null>(null)
const isNew = computed(() => !formulaId.value)

const conceptoId = ref<number | null>(null)
const condicion = ref('')
const accion = ref('')

const primitivas = ref<PrimitivaItem[]>([])
const primitivaQuery = ref('')
const selectedPrimitiva = ref<PrimitivaItem | null>(null)

// InputText de PrimeVue expone el <input> nativo en $el; se usa para insertar la primitiva
// elegida en la posición del cursor del campo que tenía el foco.
const condicionInputRef = ref<InstanceType<typeof InputText> | null>(null)
const accionInputRef = ref<InstanceType<typeof InputText> | null>(null)
const activeField = ref<'condicion' | 'accion'>('condicion')

const primitivasFiltradas = computed(() => {
  const q = primitivaQuery.value.toLowerCase().trim()
  if (!q) return primitivas.value
  return primitivas.value.filter((p) => (p.nombre ?? '').toLowerCase().includes(q))
})

const isValid = computed(() => condicion.value.trim().length > 0 && accion.value.trim().length > 0)

function setActiveField(field: 'condicion' | 'accion') {
  activeField.value = field
}

function insertPrimitiva(primitiva: PrimitivaItem | null) {
  if (!primitiva?.nombre) return
  const isCondicion = activeField.value === 'condicion'
  const inputComponent = isCondicion ? condicionInputRef.value : accionInputRef.value
  // InputText de PrimeVue expone el <input> nativo vía $el (no tipado en su interfaz pública).
  const inputEl = (inputComponent as unknown as { $el?: HTMLInputElement } | null)?.$el ?? null
  const current = isCondicion ? condicion : accion
  const texto = primitiva.nombre

  if (!inputEl) {
    current.value += texto
    return
  }
  const start = inputEl.selectionStart ?? current.value.length
  const end = inputEl.selectionEnd ?? current.value.length
  current.value = current.value.slice(0, start) + texto + current.value.slice(end)
  requestAnimationFrame(() => {
    inputEl.focus()
    inputEl.setSelectionRange(start + texto.length, start + texto.length)
  })
}

// Resalta, mientras se escribe, qué palabras de Condición/Acción son primitivas reconocidas y
// cuáles no — pensado como ayuda/sugerencia liviana en vez de un paso de validación aparte.
const TOKEN_REGEX = /[A-Za-z_][A-Za-z0-9_]*/g
const PALABRAS_IGNORADAS = new Set(['AND', 'OR', 'NOT', 'ROUND'])

const primitivaNombres = computed(() => new Set(primitivas.value.map((p) => p.nombre).filter(Boolean) as string[]))

function analizarTokens(texto: string) {
  const reconocidas = new Set<string>()
  const desconocidas = new Set<string>()
  for (const match of texto.matchAll(TOKEN_REGEX)) {
    const token = match[0]
    if (primitivaNombres.value.has(token)) reconocidas.add(token)
    else if (!PALABRAS_IGNORADAS.has(token.toUpperCase())) desconocidas.add(token)
  }
  return { reconocidas: [...reconocidas], desconocidas: [...desconocidas] }
}

const condicionTokens = computed(() => analizarTokens(condicion.value))
const accionTokens = computed(() => analizarTokens(accion.value))

async function open(options: { conceptoId: number; conceptoLabel: string } | { formulaId: number }) {
  errorMessage.value = null
  condicion.value = ''
  accion.value = ''
  primitivaQuery.value = ''
  selectedPrimitiva.value = null
  isVisible.value = true
  loading.value = true
  try {
    if (!primitivas.value.length) primitivas.value = await configurationService.getPrimitivas()

    if ('formulaId' in options) {
      formulaId.value = options.formulaId
      const data = await formulasService.getById(options.formulaId)
      conceptoId.value = data.conceptoId
      condicion.value = data.condicion
      accion.value = data.accion
    } else {
      formulaId.value = null
      conceptoId.value = options.conceptoId
    }
  } finally {
    loading.value = false
  }
}

async function handleVerificar() {
  verifying.value = true
  try {
    const result = await formulasService.verificar(condicion.value, accion.value)

    // Los errores de "primitiva desconocida" del backend se reemplazan por el detalle
    // por campo ya calculado en el cliente (condicionTokens/accionTokens); el resto de los
    // errores del backend (campos vacíos, paréntesis sin balancear) se muestran tal cual.
    const otrosErrores = result.errores.filter((e) => !e.includes('no es una primitiva conocida'))

    const lineas: string[] = [...otrosErrores]
    if (condicionTokens.value.desconocidas.length)
      lineas.push(`Condición - No reconocidas: ${condicionTokens.value.desconocidas.join(', ')}`)
    if (accionTokens.value.desconocidas.length)
      lineas.push(`Acción - No reconocidas: ${accionTokens.value.desconocidas.join(', ')}`)

    const esValida = result.valida && lineas.length === 0
    toast.add({
      severity: esValida ? 'success' : 'warn',
      summary: esValida ? 'La fórmula está bien armada' : 'Revisá la fórmula',
      detail: esValida
        ? 'Condición y Acción usan solo primitivas reconocidas.'
        : lineas.join('\n'),
      life: 5000,
    })
  } finally {
    verifying.value = false
  }
}

async function handleGuardar() {
  if (!isValid.value || !conceptoId.value) return
  errorMessage.value = null
  saving.value = true
  try {
    const dto = {
      conceptoId: conceptoId.value,
      condicion: condicion.value.trim(),
      accion: accion.value.trim(),
    }
    if (isNew.value) {
      await formulasService.create(dto)
    } else {
      await formulasService.update(formulaId.value!, dto)
    }
    toast.add({
      severity: 'success',
      summary: isNew.value ? 'Fórmula creada' : 'Fórmula actualizada',
      detail: 'Los cambios se guardaron y el procedimiento se recompiló correctamente.',
      life: 3000,
    })
    isVisible.value = false
    emit('saved')
  } catch (e: any) {
    errorMessage.value = e.response?.data?.mensaje ?? e.response?.data?.message ?? 'No se pudo guardar la fórmula.'
  } finally {
    saving.value = false
  }
}

defineExpose({ open })
</script>

<template>
  <Dialog
    v-model:visible="isVisible"
    :header="isNew ? 'Nueva fórmula' : 'Editar fórmula - ' + formulaId"
    :modal="true"
    :style="{ width: '48rem' }"
  >
    <div v-if="loading" class="flex justify-content-center py-6">
      <ProgressSpinner />
    </div>

    <div v-else class="flex flex-column gap-4">
      <Message v-if="errorMessage" severity="error" size="small" variant="simple">{{ errorMessage }}</Message>

      <div class="field">
        <label class="field-label">Condición</label>
        <InputText
          ref="condicionInputRef"
          v-model="condicion"
          class="w-full"
          @focus="setActiveField('condicion')"
        />
      </div>

      <div class="field">
        <label class="field-label">Acción</label>
        <InputText
          ref="accionInputRef"
          v-model="accion"
          class="w-full"
          @focus="setActiveField('accion')"
        />
      </div>

      <Message severity="info" size="small" variant="simple">
        Operadores disponibles: <code>&lt; &gt; = &lt;&gt; &lt;= &gt;= AND OR NOT</code> · Función: <code>ROUND(x, n)</code>
      </Message>

      <div class="field">
        <div class="flex justify-content-between align-items-center mb-2">
          <label class="field-label m-0">Primitivas</label>
          <Button label="Verificar" icon="pi pi-check-circle" size="small" severity="secondary" outlined :loading="verifying" @click="handleVerificar" />
        </div>
        <InputText v-model="primitivaQuery" placeholder="Buscar primitiva..." class="w-full mb-2" />
        <Listbox
          v-model="selectedPrimitiva"
          :options="primitivasFiltradas"
          option-label="nombre"
          list-style="max-height: 220px"
          class="w-full"
          @dblclick="insertPrimitiva(selectedPrimitiva)"
        />
        <div class="flex justify-content-end mt-2">
          <Button label="Insertar en el campo activo" icon="pi pi-arrow-right" size="small" :disabled="!selectedPrimitiva" @click="insertPrimitiva(selectedPrimitiva)" />
        </div>
      </div>
    </div>

    <template #footer>
      <Button label="Cancelar" severity="secondary" @click="isVisible = false" />
      <Button label="Aceptar" icon="pi pi-check" :loading="saving" :disabled="!isValid" @click="handleGuardar" />
    </template>
  </Dialog>
</template>
