<script setup lang="ts">
import { computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ProgressSpinner from 'primevue/progressspinner'
import ConfiguracionEditor from '../components/ConfiguracionEditor.vue'
import { useConfiguration } from '../composables/useConfiguration'
import type { CategoriaCatalogItem, ConfiguracionNomencladorDetailDto } from '../types/configuration'

const route = useRoute()
const router = useRouter()

const {
  catalogs,
  current,
  draft,
  validation,
  loadingDetail,
  saving,
  conceptosDisponibles,
  loadingConceptos,
  fetchCatalogs,
  fetchConceptos,
  fetchDetail,
  initializeDraft,
  saveCurrent,
  validateCurrent,
  cloneCurrent,
} = useConfiguration()

const currentId = computed(() => {
  const routeId = Number(route.params.id)
  return Number.isFinite(routeId) ? routeId : null
})

async function loadScreen() {
  await fetchCatalogs()
  await fetchConceptos()

  if (currentId.value) {
    await fetchDetail(currentId.value)
    return
  }

  initializeDraft()
}

async function handleSave() {
  const saved = await saveCurrent()
  if (saved) {
    await router.replace(`/configuraciones/${saved.id}`)
  }
}

async function handleClone() {
  const cloned = await cloneCurrent()
  if (cloned) {
    await router.replace(`/configuraciones/${cloned.id}`)
  }
}

function handleMontosSaved(updatedCategorias: CategoriaCatalogItem[]) {
  if (current.value) {
    current.value = { ...current.value, categorias: updatedCategorias }
  }
}

function handleDetailUpdated(detail: ConfiguracionNomencladorDetailDto) {
  current.value = detail
}

onMounted(loadScreen)
watch(() => route.fullPath, loadScreen)
</script>

<template>
  <section>
    <div v-if="loadingDetail" class="flex flex-column align-items-center justify-content-center gap-3 p-8">
      <ProgressSpinner style="width: 2.5rem; height: 2.5rem" />
      <p class="muted">Cargando configuración...</p>
    </div>

    <ConfiguracionEditor
      v-else
      v-model:draft="draft"
      :catalogs="catalogs"
      :categorias="current?.categorias ?? []"
      :conceptos-disponibles="conceptosDisponibles"
      :loading-conceptos="loadingConceptos"
      :validation="validation"
      :loading="saving"
      :configuracion-id="currentId ?? undefined"
      @save="handleSave"
      @validate="validateCurrent"
      @clone="handleClone"
      @back="router.push('/configuraciones')"
      @catalog-refresh="fetchCatalogs()"
      @montos-saved="handleMontosSaved"
      @detail-updated="handleDetailUpdated"
    />
  </section>
</template>
