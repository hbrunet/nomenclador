import { apiClient } from './configurationService'
import type { CatalogItem, ConceptoCatalogItem, ConceptoCreateUpdateDto, PagedResult } from '../types/configuration'

export const conceptosService = {
  async list(query = '') {
    const { data } = await apiClient.get<ConceptoCatalogItem[]>('/conceptos', {
      params: query ? { q: query } : {},
    })
    return data
  },

  async listPaged(query = '', page = 1, pageSize = 100, idTipoConcepto?: number | null) {
    const { data } = await apiClient.get<PagedResult<ConceptoCatalogItem>>('/conceptos/paginado', {
      params: { q: query || undefined, page, pageSize, idTipoConcepto: idTipoConcepto || undefined },
    })
    return data
  },

  async getTipos(): Promise<CatalogItem[]> {
    const { data } = await apiClient.get<CatalogItem[]>('/conceptos/tipos')
    return data
  },

  async getTiposLiquidacion(): Promise<CatalogItem[]> {
    const { data } = await apiClient.get<CatalogItem[]>('/conceptos/tipos-liquidacion')
    return data
  },

  async getById(id: number): Promise<ConceptoCatalogItem> {
    const { data } = await apiClient.get<ConceptoCatalogItem>(`/conceptos/${id}`)
    return data
  },

  async create(dto: ConceptoCreateUpdateDto): Promise<ConceptoCatalogItem> {
    const { data } = await apiClient.post<ConceptoCatalogItem>('/conceptos', dto)
    return data
  },

  async update(id: number, dto: ConceptoCreateUpdateDto): Promise<ConceptoCatalogItem> {
    const { data } = await apiClient.put<ConceptoCatalogItem>(`/conceptos/${id}`, dto)
    return data
  },
}
