<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import type { ValorFijoCatalogItem } from '../types/configuration'

const MAX_RESULTS = 10

const props = defineProps<{
  valoresDisponibles: ValorFijoCatalogItem[]
  valoresExcluidos: number[]
}>()

const emit = defineEmits<{
  (e: 'add', id: number): void
  (e: 'filter-change', payload: { tipo: string; query: string }): void
}>()

const wrapperRef = ref<HTMLElement | null>(null)

// — Tipo filter —
const tipoQuery = ref('')
const selectedTipo = ref('')
const isTipoOpen = ref(false)

const tiposDisponibles = computed(() =>
  [...new Set(props.valoresDisponibles.map((v) => v.tipo).filter(Boolean))].sort(),
)

const filteredTipos = computed(() => {
  const q = tipoQuery.value.toLowerCase().trim()
  return tiposDisponibles.value.filter((t) => !q || t.toLowerCase().includes(q))
})

function openTipoDropdown() {
  isOpen.value = false
  isTipoOpen.value = true
}

function onTipoInput() {
  selectedTipo.value = ''
  isTipoOpen.value = true
}

function selectTipo(tipo: string) {
  selectedTipo.value = tipo
  tipoQuery.value = tipo
  isTipoOpen.value = false
}

function clearTipo() {
  selectedTipo.value = ''
  tipoQuery.value = ''
  isTipoOpen.value = false
}

// — Main search —
const query = ref('')
const selected = ref<ValorFijoCatalogItem | null>(null)
const isOpen = ref(false)

const excludedSet = computed(() => new Set(props.valoresExcluidos))

const matchingItems = computed(() => {
  const q = query.value.toLowerCase().trim()
  return props.valoresDisponibles
    .filter((item) => !excludedSet.value.has(item.id))
    .filter((item) => !selectedTipo.value || item.tipo === selectedTipo.value)
    .filter((item) => !q || (item.descripcion ?? '').toLowerCase().includes(q))
})

const filteredResults = computed(() => matchingItems.value.slice(0, MAX_RESULTS))
const hasMore = computed(() => matchingItems.value.length > MAX_RESULTS)

function openMainDropdown() {
  isTipoOpen.value = false
  isOpen.value = true
}

function onQueryInput() {
  selected.value = null
  isOpen.value = true
}

function selectItem(item: ValorFijoCatalogItem) {
  selected.value = item
  query.value = item.descripcion ?? ''
  isOpen.value = false
}

function clearQuery() {
  query.value = ''
  selected.value = null
  isOpen.value = false
}

function handleAdd() {
  if (!selected.value) return
  emit('add', selected.value.id)
  isOpen.value = false
}

function handleDocumentClick(e: MouseEvent) {
  if (!wrapperRef.value?.contains(e.target as Node)) {
    isOpen.value = false
    isTipoOpen.value = false
  }
}

onMounted(() => document.addEventListener('click', handleDocumentClick))
onBeforeUnmount(() => document.removeEventListener('click', handleDocumentClick))

watch([selectedTipo, query], ([tipo, q]) => {
  emit('filter-change', { tipo, query: q })
})
</script>

<template>
  <div ref="wrapperRef" class="vf-combobox">
    <div class="vf-combobox-row">

      <!-- Tipo autocomplete -->
      <div class="vf-tipo-wrap">
        <label class="field-stack">
          <span>Tipo</span>
          <div class="vf-input-wrap">
            <input
              v-model="tipoQuery"
              type="text"
              placeholder="Todos"
              autocomplete="off"
              @focus="openTipoDropdown"
              @input="onTipoInput"
            />
            <button
              v-if="selectedTipo"
              class="vf-clear-btn"
              type="button"
              @mousedown.prevent="clearTipo"
            >×</button>
          </div>
        </label>
        <div v-if="isTipoOpen && filteredTipos.length" class="vf-dropdown">
          <button
            v-for="tipo in filteredTipos"
            :key="tipo"
            class="vf-dropdown-item"
            :class="{ 'vf-dropdown-item--active': selectedTipo === tipo }"
            type="button"
            @mousedown.prevent="selectTipo(tipo)"
          >
            {{ tipo }}
          </button>
        </div>
      </div>

      <!-- Descripción autocomplete -->
      <div class="vf-search-wrap">
        <label class="field-stack">
          <span>Agregar valor fijo</span>
          <div class="vf-input-wrap">
            <input
              v-model="query"
              type="text"
              placeholder="Escribir para buscar..."
              autocomplete="off"
              @focus="openMainDropdown"
              @input="onQueryInput"
            />
            <button
              v-if="query"
              class="vf-clear-btn"
              type="button"
              @mousedown.prevent="clearQuery"
            >×</button>
          </div>
        </label>
      </div>

      <button
        class="secondary-button vf-add-btn"
        type="button"
        :disabled="!selected"
        @click="handleAdd"
      >
        Agregar
      </button>
    </div>

    <!-- Main results dropdown -->
    <div v-if="isOpen && filteredResults.length" class="vf-dropdown vf-main-dropdown">
      <button
        v-for="item in filteredResults"
        :key="item.id"
        class="vf-dropdown-item"
        :class="{ 'vf-dropdown-item--active': selected?.id === item.id }"
        type="button"
        @mousedown.prevent="selectItem(item)"
      >
        <span class="vf-item-desc">{{ item.descripcion ?? '(sin descripción)' }}</span>
        <span class="vf-item-tipo">{{ item.tipo }}</span>
        <span class="vf-item-valor">{{ (item.valor ?? 0).toLocaleString('es-AR') }}</span>
      </button>
      <div v-if="hasMore" class="vf-dropdown-hint">
        Mostrando {{ MAX_RESULTS }} de {{ matchingItems.length }} — refiná la búsqueda
      </div>
    </div>

    <div
      v-else-if="isOpen && query.trim() && !filteredResults.length"
      class="vf-dropdown vf-main-dropdown"
    >
      <div class="vf-dropdown-hint">Sin resultados para "{{ query }}"</div>
    </div>
  </div>
</template>

<style scoped>
.vf-combobox {
  position: relative;
}

.vf-combobox-row {
  display: flex;
  align-items: flex-end;
  gap: 1rem;
  flex-wrap: wrap;
}

.vf-tipo-wrap {
  position: relative;
  flex: 0 0 200px;
}

.vf-search-wrap {
  position: relative;
  flex: 1 1 200px;
  min-width: 180px;
}

.vf-input-wrap {
  position: relative;
  display: flex;
  align-items: center;
}

.vf-input-wrap input {
  width: 100%;
  padding-right: 2rem;
}

.vf-clear-btn {
  position: absolute;
  right: 0.5rem;
  background: transparent;
  color: #64748b;
  padding: 0.1rem 0.3rem;
  font-size: 1rem;
  line-height: 1;
  border-radius: 0.25rem;
  width: auto;
}

.vf-clear-btn:hover {
  color: #0f172a;
  background: #e2e8f0;
}

.vf-add-btn {
  flex-shrink: 0;
}

.vf-dropdown {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  z-index: 100;
  background: #fff;
  border: 1px solid #dbe4f0;
  border-radius: 0.75rem;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.12);
  max-height: 280px;
  overflow-y: auto;
  margin-top: 0.25rem;
}

.vf-main-dropdown {
  left: 0;
  right: 0;
}

.vf-dropdown-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  width: 100%;
  text-align: left;
  padding: 0.7rem 1rem;
  background: transparent;
  border: 0;
  border-bottom: 1px solid #f1f5f9;
  border-radius: 0;
  cursor: pointer;
  transition: background-color 0.1s;
}

.vf-dropdown-item:last-of-type {
  border-bottom: 0;
}

.vf-dropdown-item:hover,
.vf-dropdown-item--active {
  background: #eff6ff;
}

.vf-item-desc {
  flex: 1;
  font-weight: 500;
}

.vf-item-tipo {
  font-size: 0.8rem;
  color: #64748b;
  background: #f1f5f9;
  padding: 0.15rem 0.45rem;
  border-radius: 0.3rem;
  white-space: nowrap;
}

.vf-item-valor {
  font-size: 0.875rem;
  color: #0f172a;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.vf-dropdown-hint {
  padding: 0.65rem 1rem;
  color: #64748b;
  font-size: 0.8rem;
  text-align: center;
}
</style>
