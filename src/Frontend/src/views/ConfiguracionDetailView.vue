<script setup lang="ts">
import { computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ConfiguracionEditor from '../components/ConfiguracionEditor.vue'
import { useConfiguration } from '../composables/useConfiguration'

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

onMounted(loadScreen)
watch(() => route.fullPath, loadScreen)
</script>

<template>
  <section class="stack">
    <p v-if="loadingDetail" class="muted">Cargando configuración...</p>

    <ConfiguracionEditor
      v-else
      v-model:draft="draft"
      :catalogs="catalogs"
      :categorias="current?.categorias ?? []"
      :conceptos-disponibles="conceptosDisponibles"
      :loading-conceptos="loadingConceptos"
      :validation="validation"
      :loading="saving"
      @save="handleSave"
      @validate="validateCurrent"
      @clone="handleClone"
      @back="router.push('/configuraciones')"
      @catalog-refresh="fetchCatalogs()"
    />
  </section>
</template>
