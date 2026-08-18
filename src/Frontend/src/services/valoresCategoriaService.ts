import { apiClient } from './configurationService'
import type {
  CatalogItem,
  ClonacionMasivaValoresCategoriaDto,
  ValorCategoriaCreateUpdateDto,
  ValorCategoriaDetailDto,
  ValorCategoriaItemCreateUpdateDto,
  ValorCategoriaItemInputDto,
  ValorCategoriaListItemDto,
  ValorCategoriaTipoCreateUpdateDto,
} from '../types/configuration'

let valoresCache: ValorCategoriaListItemDto[] | null = null

function toListItem(detail: ValorCategoriaDetailDto): ValorCategoriaListItemDto {
  return {
    id: detail.id,
    descripcion: detail.descripcion,
    idTipo: detail.idTipo,
    tipo: detail.tipo,
    cantidadItems: detail.items.length,
  }
}

export const valoresCategoriaService = {
  // ── Tipos ──────────────────────────────────────────────────────────────────

  async getTipos(): Promise<CatalogItem[]> {
    const { data } = await apiClient.get<CatalogItem[]>('/valores-categoria/tipos')
    return data
  },

  async createTipo(dto: ValorCategoriaTipoCreateUpdateDto): Promise<CatalogItem> {
    const { data } = await apiClient.post<CatalogItem>('/valores-categoria/tipos', dto)
    return data
  },

  async updateTipo(id: number, dto: ValorCategoriaTipoCreateUpdateDto): Promise<CatalogItem> {
    const { data } = await apiClient.put<CatalogItem>(`/valores-categoria/tipos/${id}`, dto)
    return data
  },

  async deleteTipo(id: number): Promise<void> {
    await apiClient.delete(`/valores-categoria/tipos/${id}`)
  },

  // ── Valores ────────────────────────────────────────────────────────────────
  // Caché simple en memoria, igual que valoresFijosService: se pide una sola vez
  // por sesión de la SPA y las mutaciones la actualizan in-place.

  hasCachedValores(): boolean {
    return valoresCache !== null
  },

  async getAll(forceRefresh = false): Promise<ValorCategoriaListItemDto[]> {
    if (valoresCache && !forceRefresh) return valoresCache
    const { data } = await apiClient.get<ValorCategoriaListItemDto[]>('/valores-categoria')
    valoresCache = data
    return valoresCache
  },

  async getById(id: number): Promise<ValorCategoriaDetailDto> {
    const { data } = await apiClient.get<ValorCategoriaDetailDto>(`/valores-categoria/${id}`)
    return data
  },

  async create(dto: ValorCategoriaCreateUpdateDto): Promise<ValorCategoriaDetailDto> {
    const { data } = await apiClient.post<ValorCategoriaDetailDto>('/valores-categoria', dto)
    if (valoresCache) valoresCache = [...valoresCache, toListItem(data)]
    return data
  },

  async update(id: number, dto: ValorCategoriaCreateUpdateDto): Promise<ValorCategoriaDetailDto> {
    const { data } = await apiClient.put<ValorCategoriaDetailDto>(`/valores-categoria/${id}`, dto)
    if (valoresCache) valoresCache = valoresCache.map((v) => (v.id === id ? toListItem(data) : v))
    return data
  },

  async delete(id: number): Promise<void> {
    await apiClient.delete(`/valores-categoria/${id}`)
    if (valoresCache) valoresCache = valoresCache.filter((v) => v.id !== id)
  },

  // ── Items ──────────────────────────────────────────────────────────────────

  async createItem(
    valorCategoriaId: number,
    dto: ValorCategoriaItemCreateUpdateDto,
  ): Promise<ValorCategoriaItemInputDto> {
    const { data } = await apiClient.post<ValorCategoriaItemInputDto>(
      `/valores-categoria/${valorCategoriaId}/items`,
      dto,
    )
    if (valoresCache) {
      valoresCache = valoresCache.map((v) =>
        v.id === valorCategoriaId ? { ...v, cantidadItems: v.cantidadItems + 1 } : v,
      )
    }
    return data
  },

  async updateItem(
    valorCategoriaId: number,
    itemId: number,
    dto: ValorCategoriaItemCreateUpdateDto,
  ): Promise<ValorCategoriaItemInputDto> {
    const { data } = await apiClient.put<ValorCategoriaItemInputDto>(
      `/valores-categoria/${valorCategoriaId}/items/${itemId}`,
      dto,
    )
    return data
  },

  async deleteItem(valorCategoriaId: number, itemId: number): Promise<void> {
    await apiClient.delete(`/valores-categoria/${valorCategoriaId}/items/${itemId}`)
    if (valoresCache) {
      valoresCache = valoresCache.map((v) =>
        v.id === valorCategoriaId ? { ...v, cantidadItems: v.cantidadItems - 1 } : v,
      )
    }
  },

  async cloneMasivo(dto: ClonacionMasivaValoresCategoriaDto): Promise<ValorCategoriaDetailDto[]> {
    const { data } = await apiClient.post<ValorCategoriaDetailDto[]>('/valores-categoria/clonacion-masiva', dto)
    if (valoresCache) valoresCache = [...valoresCache, ...data.map(toListItem)]
    return data
  },
}
