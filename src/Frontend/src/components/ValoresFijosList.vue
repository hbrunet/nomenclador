<script setup lang="ts">
import { computed, ref } from 'vue'
import type { ValorFijoCatalogItem, ValorFijoConfiguradoInputDto } from '../types/configuration'
import ValorFijoEditModal from './ValorFijoEditModal.vue'
import ValorFijoCombobox from './ValorFijoCombobox.vue'

const valoresFijos = defineModel<ValorFijoConfiguradoInputDto[]>({ required: true })

const props = defineProps<{
  valoresDisponibles: ValorFijoCatalogItem[]
}>()

const emit = defineEmits<{
  (e: 'catalog-refresh'): void
}>()

const editModalRef = ref<InstanceType<typeof ValorFijoEditModal> | null>(null)

const activeFilter = ref({ tipo: '', query: '' })

function onFilterChange(payload: { tipo: string; query: string }) {
  activeFilter.value = payload
}

const valuesById = computed(() => new Map(props.valoresDisponibles.map((item) => [item.id, item])))
const valoresExcluidos = computed(() => valoresFijos.value.map((item) => item.idValorFijo))

const valoresFijosVisibles = computed(() => {
  const { tipo, query } = activeFilter.value
  const q = query.toLowerCase().trim()
  if (!tipo && !q) return valoresFijos.value
  return valoresFijos.value.filter((item) => {
    const cat = valuesById.value.get(item.idValorFijo)
    const matchesTipo = !tipo || cat?.tipo === tipo
    const matchesQuery = !q || (cat?.descripcion ?? '').toLowerCase().includes(q)
    return matchesTipo && matchesQuery
  })
})

function addValorFijo(id: number) {
  if (valoresFijos.value.some((item) => item.idValorFijo === id)) return
  valoresFijos.value = [...valoresFijos.value, { idValorFijo: id, valor: 0 }]
}

function removeValorFijo(idValorFijo: number) {
  valoresFijos.value = valoresFijos.value.filter((item) => item.idValorFijo !== idValorFijo)
}

function openEditModal(idValorFijo: number) {
  const catalogItem = valuesById.value.get(idValorFijo)
  if (catalogItem) editModalRef.value?.open(catalogItem)
}

function handleModalSaved(
  payload:
    | { mode: 'updated'; item: ValorFijoCatalogItem }
    | { mode: 'replaced'; oldId: number; newItem: ValorFijoCatalogItem },
) {
  if (payload.mode === 'replaced') {
    valoresFijos.value = valoresFijos.value.map((item) =>
      item.idValorFijo === payload.oldId ? { ...item, idValorFijo: payload.newItem.id } : item,
    )
  }
  emit('catalog-refresh')
}
</script>

<template>
  <div class="stack">
    <ValorFijoCombobox
      :valores-disponibles="valoresDisponibles"
      :valores-excluidos="valoresExcluidos"
      @add="addValorFijo"
      @filter-change="onFilterChange"
    />

    <div class="table-wrapper">
      <table>
        <thead>
          <tr>
            <th>Tipo</th>
            <th>Descripción</th>
            <th>Valor</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!valoresFijosVisibles.length">
            <td colspan="4" class="muted">
              {{ valoresFijos.length ? 'Sin resultados para el filtro aplicado.' : 'No hay valores fijos configurados.' }}
            </td>
          </tr>
          <tr v-for="item in valoresFijosVisibles" :key="item.idValorFijo">
            <td>{{ valuesById.get(item.idValorFijo)?.tipo ?? 'N/D' }}</td>
            <td>{{ valuesById.get(item.idValorFijo)?.descripcion ?? 'Valor fijo' }}</td>
            <td>{{ valuesById.get(item.idValorFijo)?.valor ?? '—' }}</td>
            <td class="row-actions">
              <button
                class="secondary-button"
                type="button"
                @click="openEditModal(item.idValorFijo)"
              >
                Editar valor
              </button>
              <button
                class="danger-button"
                type="button"
                @click="removeValorFijo(item.idValorFijo)"
              >
                Quitar
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <ValorFijoEditModal ref="editModalRef" @saved="handleModalSaved" />
  </div>
</template>

<style scoped>
.row-actions {
  display: flex;
  gap: 0.5rem;
}
</style>
