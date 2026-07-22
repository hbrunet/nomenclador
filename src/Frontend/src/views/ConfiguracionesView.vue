<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import DatePicker from 'primevue/datepicker'
import { useRouter } from 'vue-router'
import AutoComplete from 'primevue/autocomplete'
import type { AutoCompleteCompleteEvent } from 'primevue/autocomplete'
import Select from 'primevue/select'
import Button from 'primevue/button'
import ConfiguracionesList from '../components/ConfiguracionesList.vue'
import { useConfiguration } from '../composables/useConfiguration'
import type { CatalogItem } from '../types/configuration'

const router = useRouter()

const { catalogs, configuraciones, pagination, loadingList, fetchCatalogs, fetchList } =
  useConfiguration()

const selectedNomenclador = ref<CatalogItem | null>(null)
const nomencladorSuggestions = ref<CatalogItem[]>([])

function searchNomenclador(event: AutoCompleteCompleteEvent) {
  const q = event.query.toLowerCase().trim()
  nomencladorSuggestions.value = catalogs.value.nomencladores
    .filter((n: CatalogItem) => !q || n.descripcion.toLowerCase().includes(q))
    .slice(0, 20)
}

const filters = reactive({
  vigenteEn: null as Date | null,
  estado: null as string | null,
})

const estadoOptions = ['Activa', 'Futura', 'Vencida']

const PAGE_SIZE = ref(20)

function buildParams(page: number) {
  return {
    nomencladorId: selectedNomenclador.value?.id ?? undefined,
    vigenteEn: filters.vigenteEn ? filters.vigenteEn.toISOString().substring(0, 7) : undefined,
    estado: filters.estado ?? undefined,
    page,
    pageSize: PAGE_SIZE.value,
  }
}

async function loadList() {
  await fetchList(buildParams(1))
}

async function goToPage(page: number, pageSize: number) {
  PAGE_SIZE.value = pageSize
  await fetchList(buildParams(page))
}

onMounted(async () => {
  await fetchCatalogs()
  await loadList()
})
</script>

<template>
  <section class="panel p-4">
    <div>
    <h2 class="text-xl mt-0 mb-3 font-semibold">Configuraciones</h2>
    <p class="muted m-0">Filtros de búsqueda</p>
    </div>  
    <div class="flex flex-wrap gap-3 align-items-end">
      <div class="flex flex-column gap-1" style="flex: 2 ">
        <label class="field-label">Nomenclador</label>
        <AutoComplete
          v-model="selectedNomenclador"
          :suggestions="nomencladorSuggestions"
          option-label="descripcion"
          placeholder="Escribir para buscar..."
          force-selection
          show-clear
          fluid
          @complete="searchNomenclador"
        />
      </div>

      <div class="flex flex-column gap-1" style="flex: 1">
        <label class="field-label">Vigente en</label>
        <DatePicker v-model="filters.vigenteEn" type="date" class="w-full"  view="month" dateFormat="mm/yy"/>
      </div>

      <div class="flex flex-column gap-1" style="flex: 1">
        <label class="field-label">Estado</label>
        <Select
          v-model="filters.estado"
          :options="estadoOptions"
          placeholder="Todos"
          show-clear
          class="w-full"
        />
      </div>

      <div class="flex align-items-end">
        <Button label="Aplicar filtros" icon="pi pi-search" @click="loadList" />
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
</template>
