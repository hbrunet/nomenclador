import { defineStore } from 'pinia'
import { conceptosService } from '../services/conceptosService'
import type { CatalogItem, ConceptoCatalogItem, ConceptoCreateUpdateDto, PagedResult } from '../types/configuration'

interface ConceptosState {
  items: ConceptoCatalogItem[]
  loading: boolean
  pagedItems: ConceptoCatalogItem[]
  loadingPaged: boolean
  pagination: { total: number; page: number; pageSize: number }
  tipos: CatalogItem[]
  tiposLiquidacion: CatalogItem[]
  pagedRequestId: number
}

export const useConceptosStore = defineStore('conceptos', {
  state: (): ConceptosState => ({
    items: [],
    loading: false,
    pagedItems: [],
    loadingPaged: false,
    pagination: { total: 0, page: 1, pageSize: 50 },
    tipos: [],
    tiposLiquidacion: [],
    pagedRequestId: 0,
  }),
  actions: {
    async fetchConceptos(query = '') {
      this.loading = true

      try {
        this.items = await conceptosService.list(query)
      } finally {
        this.loading = false
      }
    },
    async fetchConceptosPaginados(query = '', page = 1, pageSize = 50, idTipoConcepto?: number | null) {
      const requestId = ++this.pagedRequestId
      this.loadingPaged = true
      this.pagination.pageSize = pageSize

      try {
        const result: PagedResult<ConceptoCatalogItem> = await conceptosService.listPaged(
          query, page, pageSize, idTipoConcepto,
        )
        if (requestId !== this.pagedRequestId) return
        this.pagedItems = result.items
        this.pagination.total = result.total
        this.pagination.page = result.page
        this.pagination.pageSize = result.pageSize
      } finally {
        if (requestId === this.pagedRequestId) this.loadingPaged = false
      }
    },
    async fetchTiposConcepto() {
      this.tipos = await conceptosService.getTipos()
    },
    async fetchTiposLiquidacion() {
      this.tiposLiquidacion = await conceptosService.getTiposLiquidacion()
    },
    async fetchConcepto(id: number) {
      return conceptosService.getById(id)
    },
    async saveConcepto(id: number | null, dto: ConceptoCreateUpdateDto) {
      return id === null
        ? conceptosService.create(dto)
        : conceptosService.update(id, dto)
    },
  },
})
