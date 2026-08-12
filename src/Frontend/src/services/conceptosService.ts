import { apiClient } from './configurationService'
import type { ConceptoCatalogItem, PagedResult } from '../types/configuration'

export const conceptosService = {
  async list(query = '') {
    const { data } = await apiClient.get<ConceptoCatalogItem[]>('/conceptos', {
      params: query ? { q: query } : {},
    })
    return data
  },

  async listPaged(query = '', page = 1, pageSize = 100) {
    const { data } = await apiClient.get<PagedResult<ConceptoCatalogItem>>('/conceptos/paginado', {
      params: { q: query || undefined, page, pageSize },
    })
    return data
  },
}
