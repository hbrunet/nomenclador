<script setup lang="ts">
import { ref } from 'vue'
import ConceptosList from './ConceptosList.vue'
import ValoresFijosList from './ValoresFijosList.vue'
import ValoresCategoriasGrid from './ValoresCategoriasGrid.vue'
import CategoriasList from './CategoriasList.vue'
import type {
  CatalogsState,
  CategoriaCatalogItem,
  ConceptoCatalogItem,
  ConfiguracionNomencladorCreateUpdateDto,
  ValidacionConfiguracionResponse,
} from '../types/configuration'

const draft = defineModel<ConfiguracionNomencladorCreateUpdateDto>('draft', { required: true })

defineProps<{
  catalogs: CatalogsState
  categorias: CategoriaCatalogItem[]
  conceptosDisponibles: ConceptoCatalogItem[]
  loadingConceptos: boolean
  validation: ValidacionConfiguracionResponse
  loading: boolean
}>()

const emit = defineEmits<{
  (event: 'save'): void
  (event: 'validate'): void
  (event: 'clone'): void
  (event: 'back'): void
  (event: 'catalog-refresh'): void
}>()

const activeTab = ref<'conceptos' | 'valores-fijos' | 'valores-categorias' | 'categorias'>('conceptos')
</script>

<template>
  <section class="page-card stack">
    <div class="editor-toolbar">
      <div>
        <h2>Editor de configuración</h2>
      </div>
      <div class="inline-actions">
        <button class="secondary-button" type="button" @click="emit('back')">Volver</button>
        <button class="secondary-button" type="button" :disabled="loading" @click="emit('clone')">
          Clonar
        </button>
      </div>
    </div>

    <div class="form-grid">
      <label>
        <span>Nomenclador</span>
        <select v-model.number="draft.idNomenclador">
          <option :value="0">Seleccione</option>
          <option v-for="item in catalogs.nomencladores" :key="item.id" :value="item.id">
            {{ item.descripcion }}
          </option>
        </select>
      </label>

      <label>
        <span>Escala salarial</span>
        <select
          v-model.number="draft.idEscalaSalarial"
        >
          <option :value="0">Seleccione</option>
          <option v-for="item in catalogs.escalas" :key="item.id" :value="item.id">
            {{ item.descripcion }}
          </option>
        </select>
      </label>

      <label>
        <span>Zona</span>
        <select v-model.number="draft.idZona">
          <option :value="0">Seleccione</option>
          <option v-for="item in catalogs.zonas" :key="item.id" :value="item.id">
            {{ item.descripcion }}
          </option>
        </select>
      </label>

      <label>
        <span>Fecha inicio</span>
        <input v-model="draft.fechaInicio" type="date" />
      </label>

      <label>
        <span>Fecha fin</span>
        <input v-model="draft.fechaFin" type="date" />
      </label>
    </div>

    <div class="tab-list">
      <button
        class="tab-button"
        :class="{ active: activeTab === 'conceptos' }"
        type="button"
        @click="activeTab = 'conceptos'"
      >
        Conceptos
      </button>
      <button
        class="tab-button"
        :class="{ active: activeTab === 'valores-fijos' }"
        type="button"
        @click="activeTab = 'valores-fijos'"
      >
        Valores fijos
      </button>
      <button
        class="tab-button"
        :class="{ active: activeTab === 'valores-categorias' }"
        type="button"
        @click="activeTab = 'valores-categorias'"
      >
        Valores por categoría
      </button>
      <button
        class="tab-button"
        :class="{ active: activeTab === 'categorias' }"
        type="button"
        @click="activeTab = 'categorias'"
      >
        Categorías Escala Salarial
      </button>
    </div>

    <ConceptosList
      v-if="activeTab === 'conceptos'"
      v-model="draft.conceptos"
      :conceptos-disponibles="conceptosDisponibles"
      :loading-catalog="loadingConceptos"
    />

    <ValoresFijosList
      v-else-if="activeTab === 'valores-fijos'"
      v-model="draft.valoresFijos"
      :valores-disponibles="catalogs.valoresFijos"
      @catalog-refresh="emit('catalog-refresh')"
    />

    <ValoresCategoriasGrid
      v-else-if="activeTab === 'valores-categorias'"
      v-model="draft.valoresCategorias"
      :valores-disponibles="catalogs.valoresCategorias"
    />

    <CategoriasList
      v-else-if="activeTab === 'categorias'"
      :categorias="categorias"
    />

    <div class="validation-grid">
      <div class="validation-box">
        <span class="badge" :class="{ error: !validation.valida }">Errores</span>
        <ul>
          <li v-if="!validation.errores.length" class="muted">Sin errores.</li>
          <li v-for="error in validation.errores" :key="error.codigo + error.mensaje">
            {{ error.mensaje }}
          </li>
        </ul>
      </div>
      <div class="validation-box">
        <span class="badge warning">Warnings</span>
        <ul>
          <li v-if="!validation.warnings.length" class="muted">Sin advertencias.</li>
          <li v-for="warning in validation.warnings" :key="warning.codigo + warning.mensaje">
            {{ warning.mensaje }}
          </li>
        </ul>
      </div>
    </div>
  </section>
</template>
