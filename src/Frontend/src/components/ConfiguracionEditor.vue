<script setup lang="ts">
import { computed, ref } from 'vue'
import Select from 'primevue/select'
import Button from 'primevue/button'
import Tabs from 'primevue/tabs'
import TabList from 'primevue/tablist'
import Tab from 'primevue/tab'
import TabPanels from 'primevue/tabpanels'
import TabPanel from 'primevue/tabpanel'
import Message from 'primevue/message'
import DatePicker from 'primevue/datepicker';
import ConceptosList from './ConceptosList.vue'
import ValoresFijosList from './ValoresFijosList.vue'
import ValoresCategoriasGrid from './ValoresCategoriasGrid.vue'
import CategoriasList from './CategoriasList.vue'
import type {
  CatalogsState,
  CategoriaCatalogItem,
  ConceptoConfiguradoViewModel,
  ConfiguracionNomencladorCreateUpdateDto,
  ConfiguracionNomencladorDetailDto,
  ValidacionConfiguracionResponse,
} from '../types/configuration'

const draft = defineModel<ConfiguracionNomencladorCreateUpdateDto>('draft', { required: true })

const props = defineProps<{
  catalogs: CatalogsState
  categorias: CategoriaCatalogItem[]
  conceptosConfigurados: ConceptoConfiguradoViewModel[]
  loading: boolean
  validation: ValidacionConfiguracionResponse | null
  configuracionId?: number
}>()

const emit = defineEmits<{
  (event: 'save'): void
  (event: 'validate'): void
  (event: 'clone'): void
  (event: 'back'): void
  (event: 'catalog-refresh'): void
  (event: 'montos-saved', categorias: CategoriaCatalogItem[]): void
  (event: 'detail-updated', detail: ConfiguracionNomencladorDetailDto): void
}>()

const nomencladorId = computed({
  get: () => draft.value.idNomenclador || null,
  set: (val: number | null) => { draft.value.idNomenclador = val ?? 0 },
})
const escalaSalarialId = computed({
  get: () => draft.value.idEscalaSalarial || null,
  set: (val: number | null) => { draft.value.idEscalaSalarial = val ?? 0 },
})
const zonaId = computed({
  get: () => draft.value.idZona,
  set: (val: number | null) => { draft.value.idZona = val },
})

const hasErrors = computed(() => (props.validation?.errores?.length ?? 0) > 0)
const hasWarnings = computed(() => (props.validation?.warnings?.length ?? 0) > 0)

const isNew = computed(() => !props.configuracionId)

const activeTab = ref('datos-generales')
</script>

<template>
  <section class="panel p-4 flex flex-column gap-3">
    <div class="flex justify-content-between align-items-center flex-wrap gap-3">
      <h2 class="text-xl mt-0 mb-0 font-semibold">Editor de configuración</h2>
      <div class="flex gap-2 flex-wrap">
        <Button label="Volver" severity="secondary" text icon="pi pi-arrow-left" @click="emit('back')" />
      </div>
    </div>

    <div v-if="hasErrors || hasWarnings" class="flex flex-column gap-2">
      <Message
        v-for="msg in validation?.errores ?? []"
        :key="msg.codigo"
        severity="error"
        :closable="false"
      >
        {{ msg.mensaje }}
      </Message>
      <Message
        v-for="msg in validation?.warnings ?? []"
        :key="msg.codigo"
        severity="warn"
        :closable="false"
      >
        {{ msg.mensaje }}
      </Message>
    </div>

    <Tabs v-model:value="activeTab">
      <TabList>
        <Tab value="datos-generales">Datos generales</Tab>
        <Tab value="conceptos" :disabled="isNew">Conceptos</Tab>
        <Tab value="valores-fijos" :disabled="isNew">Valores fijos</Tab>
        <Tab value="valores-categorias" :disabled="isNew">Valores por categoría</Tab>
        <Tab value="categorias" :disabled="isNew">Categorías escala</Tab>
      </TabList>

      <TabPanels>
        <TabPanel value="datos-generales">
          <form @submit.prevent="emit('save')">
            <div class="grid pt-3">
              <div class="col-6 flex flex-column gap-1">
                <label class="field-label">Nomenclador</label>
                <Select
                  v-model="nomencladorId"
                  :options="catalogs.nomencladores"
                  option-label="descripcion"
                  option-value="id"
                  placeholder="Seleccione un nomenclador"
                  class="w-full"
                />
              </div>

              <div class="col-6 flex flex-column gap-1">
                <label class="field-label">Escala salarial</label>
                <Select
                  v-model="escalaSalarialId"
                  :options="catalogs.escalas"
                  option-label="descripcion"
                  option-value="id"
                  placeholder="Seleccione una escala"
                  class="w-full"
                />
              </div>

              <div class="col-6 flex flex-column gap-1">
                <label class="field-label">Zona (opcional)</label>
                <Select
                  v-model="zonaId"
                  :options="catalogs.zonas"
                  option-label="descripcion"
                  option-value="id"
                  placeholder="Seleccione una zona"
                  show-clear
                  class="w-full"
                />
              </div>

              <div class="col-3 flex flex-column gap-1">
                <label class="field-label">Fecha inicio</label>
                <DatePicker v-model="draft.fechaInicio" type="date" class="w-full"  view="month" dateFormat="mm/yy"/>
              </div>

              <div class="col-3 flex flex-column gap-1">
                <label class="field-label">Fecha fin</label>
                <DatePicker v-model="draft.fechaFin" type="date" class="w-full" view="month" dateFormat="mm/yy"/>
              </div>
            </div>

            <div class="flex justify-content-end gap-2 mt-4">
              <Button label="Guardar" severity="primary" :loading="loading" @click="emit('save')" />
            </div>
          </form>
        </TabPanel>

        <TabPanel value="conceptos">
          <ConceptosList
            v-model="draft.conceptos"
            :conceptos-resueltos="conceptosConfigurados"
            :configuracion-id="props.configuracionId"
            @detail-updated="(detail) => emit('detail-updated', detail)"
          />
        </TabPanel>

        <TabPanel value="valores-fijos">
          <ValoresFijosList
            v-model="draft.valoresFijos"
            :valores-disponibles="catalogs.valoresFijos"
            :configuracion-id="props.configuracionId"
            @catalog-refresh="emit('catalog-refresh')"
            @detail-updated="(detail) => emit('detail-updated', detail)"
          />
        </TabPanel>

        <TabPanel value="valores-categorias">
          <ValoresCategoriasGrid
            v-model="draft.valoresCategorias"
            :valores-disponibles="catalogs.valoresCategorias"
            :configuracion-id="props.configuracionId"
            @detail-updated="(detail) => emit('detail-updated', detail)"
          />
        </TabPanel>

        <TabPanel value="categorias">
          <CategoriasList :categorias="categorias" @montos-saved="(cats) => emit('montos-saved', cats)" />
        </TabPanel>
      </TabPanels>
    </Tabs>
  </section>
</template>
