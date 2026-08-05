<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ProgressSpinner from 'primevue/progressspinner'
import Message from 'primevue/message'
import { useToast } from 'primevue/usetoast'
import ConfiguracionEditor from '../components/ConfiguracionEditor.vue'
import { useConfiguration } from '../composables/useConfiguration'
import type { CategoriaCatalogItem, ConfiguracionNomencladorDetailDto } from '../types/configuration'

const route = useRoute()
const router = useRouter()
const toast = useToast()

const {
  catalogs,
  current,
  draft,
  validation,
  loadingDetail,
  saving,
  fetchCatalogs,
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
  // Catálogos y detalle son independientes entre sí, se disparan en paralelo.
  // El catálogo de conceptos ya NO se precarga completo: ConceptoCombobox lo
  // busca bajo demanda (búsqueda server-side), porque esa tabla es grande.
  const tasks: Promise<unknown>[] = [fetchCatalogs()]

  if (currentId.value) {
    tasks.push(fetchDetail(currentId.value))
  } else {
    initializeDraft()
  }

  await Promise.all(tasks)
}

const saveError = ref<string | null>(null)

async function handleSave() {
  saveError.value = null
  const wasNew = !currentId.value
  try {
    const saved = await saveCurrent()
    if (saved) {
      toast.add({
        severity: 'success',
        summary: wasNew ? 'Configuración creada' : 'Configuración actualizada',
        detail: wasNew
          ? 'La configuración se ha creado correctamente.'
          : 'Los datos generales se han guardado correctamente.',
        life: 5000,
      })
      await router.replace(`/configuraciones/${saved.id}`)
    }
  } catch (e: any) {
    saveError.value = e.response?.data?.mensaje ?? 'No se pudo guardar la configuración.'
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

    <Message v-if="saveError" severity="error" :closable="true" @close="saveError = null">
      {{ saveError }}
    </Message>

    <ConfiguracionEditor
      v-else
      v-model:draft="draft"
      :catalogs="catalogs"
      :categorias="current?.categorias ?? []"
      :conceptos-configurados="current?.conceptos ?? []"
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
