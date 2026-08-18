import { apiClient } from './configurationService'
import type { GrupoValorCategoriaCreateUpdateDto, GrupoValorCategoriaDto } from '../types/configuration'

export const gruposValorCategoriaService = {
  async getAll(): Promise<GrupoValorCategoriaDto[]> {
    const { data } = await apiClient.get<GrupoValorCategoriaDto[]>('/grupos-valor-categoria')
    return data
  },

  async getById(id: number): Promise<GrupoValorCategoriaDto> {
    const { data } = await apiClient.get<GrupoValorCategoriaDto>(`/grupos-valor-categoria/${id}`)
    return data
  },

  async create(dto: GrupoValorCategoriaCreateUpdateDto): Promise<GrupoValorCategoriaDto> {
    const { data } = await apiClient.post<GrupoValorCategoriaDto>('/grupos-valor-categoria', dto)
    return data
  },

  async update(id: number, dto: GrupoValorCategoriaCreateUpdateDto): Promise<GrupoValorCategoriaDto> {
    const { data } = await apiClient.put<GrupoValorCategoriaDto>(`/grupos-valor-categoria/${id}`, dto)
    return data
  },

  async delete(id: number): Promise<void> {
    await apiClient.delete(`/grupos-valor-categoria/${id}`)
  },
}
