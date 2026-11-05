<script setup lang="ts">
import { onMounted, ref } from 'vue'
import ProgressSpinner from 'primevue/progressspinner'
import Message from 'primevue/message'
import Tag from 'primevue/tag'
import { asistenteFeedbackService } from '../services/asistenteFeedbackService'
import type { AsistenteFeedbackResumenDto } from '../types/asistente'

const resumen = ref<AsistenteFeedbackResumenDto | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)

const etiquetasMotivo: Record<string, string> = {
  dato_incorrecto: 'Dato incorrecto',
  respuesta_incompleta: 'Respuesta incompleta',
  no_entendio: 'No entendió la consulta',
  otro: 'Otro',
}

function etiquetaMotivo(motivo: string) {
  return etiquetasMotivo[motivo] ?? motivo
}

function porcentaje(parte: number, total: number) {
  return total === 0 ? '—' : `${Math.round((parte / total) * 100)}%`
}

async function cargar() {
  loading.value = true
  error.value = null
  try {
    resumen.value = await asistenteFeedbackService.getResumen()
  } catch {
    error.value = 'No se pudo cargar el resumen de feedback.'
  } finally {
    loading.value = false
  }
}

onMounted(cargar)
</script>

<template>
  <section class="panel p-4">
    <div class="mb-3">
      <h2 class="text-xl mt-0 mb-0 font-semibold">Feedback del asistente</h2>
      <p class="muted mt-2 mb-0">
        Valoraciones de los usuarios sobre las respuestas del asistente IA, para detectar qué ajustar
        en el prompt o el manual de ayuda.
      </p>
    </div>

    <Message v-if="error" severity="error" :closable="false" class="mb-3">{{ error }}</Message>

    <div v-if="loading" class="flex justify-content-center p-5">
      <ProgressSpinner style="width: 3rem; height: 3rem" />
    </div>

    <template v-else-if="resumen">
      <div class="flex flex-wrap gap-3 mb-4">
        <div class="feedback-card">
          <span class="feedback-card-valor">{{ resumen.totalVotos }}</span>
          <span class="feedback-card-label">Votos totales</span>
        </div>
        <div class="feedback-card">
          <span class="feedback-card-valor text-green-600">
            <i class="pi pi-thumbs-up-fill mr-1" aria-hidden="true"></i>{{ resumen.positivos }}
          </span>
          <span class="feedback-card-label">Positivos ({{ porcentaje(resumen.positivos, resumen.totalVotos) }})</span>
        </div>
        <div class="feedback-card">
          <span class="feedback-card-valor text-red-600">
            <i class="pi pi-thumbs-down-fill mr-1" aria-hidden="true"></i>{{ resumen.negativos }}
          </span>
          <span class="feedback-card-label">Negativos ({{ porcentaje(resumen.negativos, resumen.totalVotos) }})</span>
        </div>
      </div>

      <h3 class="text-lg font-semibold mb-2">Motivos de los votos negativos</h3>
      <p v-if="resumen.motivosNegativos.length === 0" class="muted">
        Todavía no hay votos negativos con motivo registrado.
      </p>
      <div v-else class="flex flex-wrap gap-2">
        <Tag
          v-for="item in resumen.motivosNegativos"
          :key="item.motivo"
          :value="`${etiquetaMotivo(item.motivo)}: ${item.cantidad}`"
          severity="warn"
        />
      </div>
    </template>
  </section>
</template>

<style scoped>
.feedback-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.25rem;
  padding: 1rem 1.5rem;
  border: 1px solid var(--p-content-border-color, #e4e4e7);
  border-radius: 8px;
  min-width: 10rem;
}

.feedback-card-valor {
  font-size: 1.75rem;
  font-weight: 600;
}

.feedback-card-label {
  color: var(--p-text-muted-color);
  font-size: 0.875rem;
}
</style>
