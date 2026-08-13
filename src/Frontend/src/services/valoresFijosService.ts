import { apiClient } from './configurationService'
import type {
  CatalogItem,
  ValorFijoCatalogItem,
  ValorFijoCloneDto,
  ValorFijoCreateUpdateDto,
} from '../types/configuration'

let valoresCache: ValorFijoCatalogItem[] | null = null

export const valoresFijosService = {
  // ── Tipos ──────────────────────────────────────────────────────────────────

  async getTipos(): Promise<CatalogItem[]> {
    const { data } = await apiClient.get<CatalogItem[]>('/valores-fijos/tipos')
    return data
  },
  async createTipo(dto: { descripcion: string }): Promise<CatalogItem> {
    const { data } = await apiClient.post<CatalogItem>('/valores-fijos/tipos', dto)
    return data
  },

  async updateTipo(id: number, dto: { descripcion: string }): Promise<CatalogItem> {
    const { data } = await apiClient.put<CatalogItem>(`/valores-fijos/tipos/${id}`, dto)
    return data
  },

  async deleteTipo(id: number): Promise<void> {
    await apiClient.delete(`/valores-fijos/tipos/${id}`)
  },

  // ── Valores ────────────────────────────────────────────────────────────────
  // Caché simple en memoria: el catálogo trae miles de filas, así que sólo se
  // pide al backend una vez por sesión de la SPA; las mutaciones lo actualizan
  // in-place para que quede consistente sin necesidad de refetch.

  hasCachedValores(): boolean {
    return valoresCache !== null
  },

  async getAll(forceRefresh = false): Promise<ValorFijoCatalogItem[]> {
    if (valoresCache && !forceRefresh) return valoresCache
    const { data } = await apiClient.get<ValorFijoCatalogItem[]>('/valores-fijos')
    valoresCache = data
    return valoresCache
  },

  async getById(id: number): Promise<ValorFijoCatalogItem> {
    const { data } = await apiClient.get<ValorFijoCatalogItem>(`/valores-fijos/${id}`)
    return data
  },

  async create(dto: ValorFijoCreateUpdateDto): Promise<ValorFijoCatalogItem> {
    const { data } = await apiClient.post<ValorFijoCatalogItem>('/valores-fijos', dto)
    if (valoresCache) valoresCache = [...valoresCache, data]
    return data
  },

  async update(id: number, dto: ValorFijoCreateUpdateDto): Promise<ValorFijoCatalogItem> {
    const { data } = await apiClient.put<ValorFijoCatalogItem>(`/valores-fijos/${id}`, dto)
    if (valoresCache) valoresCache = valoresCache.map((v) => (v.id === id ? data : v))
    return data
  },

  async delete(id: number): Promise<void> {
    await apiClient.delete(`/valores-fijos/${id}`)
    if (valoresCache) valoresCache = valoresCache.filter((v) => v.id !== id)
  },

  async clone(id: number, dto: ValorFijoCloneDto): Promise<ValorFijoCatalogItem> {
    const { data } = await apiClient.post<ValorFijoCatalogItem>(`/valores-fijos/${id}/clonar`, dto)
    if (valoresCache) valoresCache = [...valoresCache, data]
    return data
  },
}
