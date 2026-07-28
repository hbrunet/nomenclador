import { apiClient } from './configurationService'
import type {
  CatalogItem,
  ValorFijoCatalogItem,
  ValorFijoCreateUpdateDto,
} from '../types/configuration'

export const valoresFijosService = {
  // ── Tipos ──────────────────────────────────────────────────────────────────

  async getTipos(): Promise<CatalogItem[]> {
    const { data } = await apiClient.get<CatalogItem[]>('/valores-fijos/tipos')
    return data
  },
  async createTipo(dto: CatalogItem): Promise<CatalogItem> {
    const { data } = await apiClient.post<CatalogItem>('/valores-fijos/tipos', dto)
    return data
  },

  async updateTipo(id: number, dto: CatalogItem): Promise<CatalogItem> {
    const { data } = await apiClient.put<CatalogItem>(`/valores-fijos/tipos/${id}`, dto)
    return data
  },

  async deleteTipo(id: number): Promise<void> {
    await apiClient.delete(`/valores-fijos/tipos/${id}`)
  },

  // ── Valores ────────────────────────────────────────────────────────────────

  async getAll(): Promise<ValorFijoCatalogItem[]> {
    const { data } = await apiClient.get<ValorFijoCatalogItem[]>('/valores-fijos')
    return data
  },

  async getById(id: number): Promise<ValorFijoCatalogItem> {
    const { data } = await apiClient.get<ValorFijoCatalogItem>(`/valores-fijos/${id}`)
    return data
  },

  async create(dto: ValorFijoCreateUpdateDto): Promise<ValorFijoCatalogItem> {
    const { data } = await apiClient.post<ValorFijoCatalogItem>('/valores-fijos', dto)
    return data
  },

  async update(id: number, dto: ValorFijoCreateUpdateDto): Promise<ValorFijoCatalogItem> {
    const { data } = await apiClient.put<ValorFijoCatalogItem>(`/valores-fijos/${id}`, dto)
    return data
  },

  async delete(id: number): Promise<void> {
    await apiClient.delete(`/valores-fijos/${id}`)
  },
}
