import { apiClient } from './configurationService'
import type {
  CatalogItem,
  ValorCategoriaCreateUpdateDto,
  ValorCategoriaDetailDto,
  ValorCategoriaItemCreateUpdateDto,
  ValorCategoriaItemInputDto,
  ValorCategoriaListItemDto,
  ValorCategoriaTipoCreateUpdateDto,
} from '../types/configuration'

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

  async getAll(): Promise<ValorCategoriaListItemDto[]> {
    const { data } = await apiClient.get<ValorCategoriaListItemDto[]>('/valores-categoria')
    return data
  },

  async getById(id: number): Promise<ValorCategoriaDetailDto> {
    const { data } = await apiClient.get<ValorCategoriaDetailDto>(`/valores-categoria/${id}`)
    return data
  },

  async create(dto: ValorCategoriaCreateUpdateDto): Promise<ValorCategoriaDetailDto> {
    const { data } = await apiClient.post<ValorCategoriaDetailDto>('/valores-categoria', dto)
    return data
  },

  async update(id: number, dto: ValorCategoriaCreateUpdateDto): Promise<ValorCategoriaDetailDto> {
    const { data } = await apiClient.put<ValorCategoriaDetailDto>(`/valores-categoria/${id}`, dto)
    return data
  },

  async delete(id: number): Promise<void> {
    await apiClient.delete(`/valores-categoria/${id}`)
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
  },
}
