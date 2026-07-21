import { apiClient } from './configurationService'
import type {
  CategoriaCatalogItem,
  CategoriaCreateUpdateDto,
  EscalaCreateUpdateDto,
  EscalaDetailDto,
  EscalaListItemDto,
} from '../types/configuration'

export const escalasService = {
  async getAll(): Promise<EscalaListItemDto[]> {
    const { data } = await apiClient.get<EscalaListItemDto[]>('/escalas')
    return data
  },

  async getById(id: number): Promise<EscalaDetailDto> {
    const { data } = await apiClient.get<EscalaDetailDto>(`/escalas/${id}`)
    return data
  },

  async create(dto: EscalaCreateUpdateDto): Promise<EscalaDetailDto> {
    const { data } = await apiClient.post<EscalaDetailDto>('/escalas', dto)
    return data
  },

  async update(id: number, dto: EscalaCreateUpdateDto): Promise<EscalaDetailDto> {
    const { data } = await apiClient.put<EscalaDetailDto>(`/escalas/${id}`, dto)
    return data
  },

  async delete(id: number): Promise<void> {
    await apiClient.delete(`/escalas/${id}`)
  },

  async createCategoria(escalaId: number, dto: CategoriaCreateUpdateDto): Promise<CategoriaCatalogItem> {
    const { data } = await apiClient.post<CategoriaCatalogItem>(`/escalas/${escalaId}/categorias`, dto)
    return data
  },

  async updateCategoria(
    escalaId: number,
    catId: number,
    dto: CategoriaCreateUpdateDto,
  ): Promise<CategoriaCatalogItem> {
    const { data } = await apiClient.put<CategoriaCatalogItem>(
      `/escalas/${escalaId}/categorias/${catId}`,
      dto,
    )
    return data
  },

  async deleteCategoria(escalaId: number, catId: number): Promise<void> {
    await apiClient.delete(`/escalas/${escalaId}/categorias/${catId}`)
  },
}
