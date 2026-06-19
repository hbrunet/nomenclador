import { apiClient } from './configurationService'
import type { ConceptoCatalogItem } from '../types/configuration'

export const conceptosService = {
  async list(query = '') {
    const { data } = await apiClient.get<ConceptoCatalogItem[]>('/conceptos', {
      params: query ? { q: query } : {},
    })
    return data
  },
}
