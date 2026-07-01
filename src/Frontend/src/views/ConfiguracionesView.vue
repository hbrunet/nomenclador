<script setup lang="ts">
import { onMounted, reactive } from 'vue'
import { useRouter } from 'vue-router'
import ConfiguracionesList from '../components/ConfiguracionesList.vue'
import { useConfiguration } from '../composables/useConfiguration'

const router = useRouter()

const { catalogs, configuraciones, pagination, loadingList, fetchCatalogs, fetchList } =
  useConfiguration()

const filters = reactive({
  nomencladorId: undefined as number | undefined,
  escalaSalarialId: undefined as number | undefined,
  zonaId: undefined as number | undefined,
  vigenteEn: '',
  estado: '',
})

const PAGE_SIZE = 20

function buildParams(page: number) {
  return {
    nomencladorId: filters.nomencladorId,
    escalaSalarialId: filters.escalaSalarialId,
    zonaId: filters.zonaId,
    vigenteEn: filters.vigenteEn || undefined,
    estado: filters.estado || undefined,
    page,
    pageSize: PAGE_SIZE,
  }
}

async function loadList() {
  await fetchList(buildParams(1))
}

async function goToPage(page: number) {
  await fetchList(buildParams(page))
}

onMounted(async () => {
  await fetchCatalogs()
  await loadList()
})
</script>

<template>
  <section class="page-card stack">
    <div class="page-header">
      <div>
        <h2>Configuraciones de nomenclador</h2>
        <p class="muted">Grilla inicial con filtros por nomenclador, escala, zona y estado.</p>
      </div>
    </div>

    <div class="filters-grid">
      <label>
        <span>Nomenclador</span>
        <select v-model.number="filters.nomencladorId">
          <option :value="undefined">Todos</option>
          <option v-for="item in catalogs.nomencladores" :key="item.id" :value="item.id">
            {{ item.descripcion }}
          </option>
        </select>
      </label>

      <label>
        <span>Escala</span>
        <select v-model.number="filters.escalaSalarialId">
          <option :value="undefined">Todas</option>
          <option v-for="item in catalogs.escalas" :key="item.id" :value="item.id">
            {{ item.descripcion }}
          </option>
        </select>
      </label>

      <label>
        <span>Zona</span>
        <select v-model.number="filters.zonaId">
          <option :value="undefined">Todas</option>
          <option v-for="item in catalogs.zonas" :key="item.id" :value="item.id">
            {{ item.descripcion }}
          </option>
        </select>
      </label>

      <label>
        <span>Vigente en</span>
        <input v-model="filters.vigenteEn" type="date" />
      </label>

      <label>
        <span>Estado</span>
        <select v-model="filters.estado">
          <option value="">Todos</option>
          <option value="Activa">Activa</option>
          <option value="Futura">Futura</option>
          <option value="Vencida">Vencida</option>
        </select>
      </label>

      <div class="field-stack inline">
        <button class="primary-button" type="button" @click="loadList">Aplicar filtros</button>
      </div>
    </div>
  </section>

  <ConfiguracionesList
    :items="configuraciones"
    :loading="loadingList"
    :total="pagination.total"
    :page="pagination.page"
    :page-size="pagination.pageSize"
    @create="router.push('/configuraciones/nueva')"
    @edit="router.push(`/configuraciones/${$event}`)"
    @page-change="goToPage"
  />
  />
</template>
