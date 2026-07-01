import { defineStore } from 'pinia'
import { configurationService } from '../services/configurationService'
import {
  createEmptyValidation,
  mergeValidationResults,
  validateDraft,
} from '../services/validationService'
import type {
  CatalogsState,
  ClonarConfiguracionDto,
  ConfigurationFilters,
  ConfiguracionNomencladorCreateUpdateDto,
  ConfiguracionNomencladorDetailDto,
  ConfiguracionNomencladorListItemDto,
  ValidacionConfiguracionResponse,
} from '../types/configuration'

interface PaginationState {
  total: number
  page: number
  pageSize: number
}

interface ConfigurationState {
  items: ConfiguracionNomencladorListItemDto[]
  pagination: PaginationState
  current: ConfiguracionNomencladorDetailDto | null
  draft: ConfiguracionNomencladorCreateUpdateDto
  catalogs: CatalogsState
  validation: ValidacionConfiguracionResponse
  loadingList: boolean
  loadingDetail: boolean
  saving: boolean
}

function createEmptyDraft(): ConfiguracionNomencladorCreateUpdateDto {
  return {
    idNomenclador: 0,
    idEscalaSalarial: 0,
    idZona: 0,
    fechaInicio: new Date().toISOString().slice(0, 10),
    fechaFin: null,
    conceptos: [],
    valoresFijos: [],
    valoresCategorias: [],
  }
}

function mapDetailToDraft(
  detail: ConfiguracionNomencladorDetailDto,
): ConfiguracionNomencladorCreateUpdateDto {
  return {
    idNomenclador: detail.idNomenclador,
    idEscalaSalarial: detail.idEscalaSalarial,
    idZona: detail.idZona,
    fechaInicio: detail.fechaInicio,
    fechaFin: detail.fechaFin,
    conceptos: detail.conceptos.map((item) => ({
      idConcepto: item.idConcepto,
      orden: item.orden,
    })),
    valoresFijos: detail.valoresFijos.map((item) => ({
      idValorFijo: item.idValorFijo,
      valor: item.valor,
    })),
    valoresCategorias: detail.valoresCategorias.map((item) => ({
      idValorCategoria: item.idValorCategoria,
      items: item.items.map((subitem) => ({
        id: subitem.id,
        numeroCategoria: subitem.numeroCategoria,
        importe: subitem.importe,
      })),
    })),
  }
}

function nextClonePayload(detail: ConfiguracionNomencladorDetailDto): ClonarConfiguracionDto {
  const startDate = new Date(detail.fechaInicio)
  startDate.setFullYear(startDate.getFullYear() + 1)

  return {
    fechaInicio: startDate.toISOString().slice(0, 10),
    fechaFin: null,
    copiarConceptos: true,
    copiarValoresFijos: true,
    copiarValoresCategoria: true,
  }
}

export const useConfigurationStore = defineStore('configuration', {
  state: (): ConfigurationState => ({
    items: [],
    pagination: { total: 0, page: 1, pageSize: 20 },
    current: null,
    draft: createEmptyDraft(),
    catalogs: {
      nomencladores: [],
      escalas: [],
      zonas: [],
      categorias: [],
      valoresFijos: [],
      valoresCategorias: [],
    },
    validation: createEmptyValidation(),
    loadingList: false,
    loadingDetail: false,
    saving: false,
  }),
  actions: {
    initializeDraft() {
      this.current = null
      this.draft = createEmptyDraft()
      this.validation = createEmptyValidation()
    },

    async fetchCatalogs(escalaId?: number) {
      const [nomencladores, escalas, zonas, valoresFijos, valoresCategorias] = await Promise.all([
        configurationService.getNomencladores(),
        configurationService.getEscalas(),
        configurationService.getZonas(),
        configurationService.getValoresFijos(),
        configurationService.getValoresCategorias(),
      ])

      this.catalogs = {
        ...this.catalogs,
        nomencladores,
        escalas,
        zonas,
        valoresFijos,
        valoresCategorias,
      }

      if (escalaId) {
        await this.fetchCategorias(escalaId)
      }
    },

    async fetchCategorias(escalaId?: number) {
      this.catalogs.categorias = escalaId
        ? await configurationService.getCategorias(escalaId)
        : []
    },

    async fetchList(filters: ConfigurationFilters = {}) {
      this.loadingList = true

      try {
        const result = await configurationService.list(filters)
        this.items = result.items
        this.pagination = { total: result.total, page: result.page, pageSize: result.pageSize }
      } finally {
        this.loadingList = false
      }
    },

    async fetchDetail(id: number) {
      this.loadingDetail = true

      try {
        this.current = await configurationService.getById(id)
        this.draft = mapDetailToDraft(this.current)
        this.validation = createEmptyValidation()
      } finally {
        this.loadingDetail = false
      }
    },

    async validateCurrent() {
      const localValidation = validateDraft(this.draft)
      const serverValidation = await configurationService.validate(this.draft)
      this.validation = mergeValidationResults(localValidation, serverValidation)
      return this.validation
    },

    async saveCurrent() {
      this.saving = true

      try {
        const validation = await this.validateCurrent()
        if (!validation.valida) {
          return null
        }

        const result = this.current
          ? await configurationService.update(this.current.id, this.draft)
          : await configurationService.create(this.draft)

        this.current = result
        this.draft = mapDetailToDraft(result)
        await this.fetchList()
        return result
      } finally {
        this.saving = false
      }
    },

    async cloneCurrent() {
      if (!this.current) {
        return null
      }

      this.saving = true

      try {
        const clone = await configurationService.clone(
          this.current.id,
          nextClonePayload(this.current),
        )
        this.current = clone
        this.draft = mapDetailToDraft(clone)
        await this.fetchList()
        return clone
      } finally {
        this.saving = false
      }
    },
  },
})
