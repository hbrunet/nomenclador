import { apiClient } from './configurationService'
import type { GrupoValorFijoCreateUpdateDto, GrupoValorFijoDto } from '../types/configuration'

export const gruposValorFijoService = {
  async getAll(): Promise<GrupoValorFijoDto[]> {
    const { data } = await apiClient.get<GrupoValorFijoDto[]>('/grupos-valor-fijo')
    return data
  },

  async getById(id: number): Promise<GrupoValorFijoDto> {
    const { data } = await apiClient.get<GrupoValorFijoDto>(`/grupos-valor-fijo/${id}`)
    return data
  },

  async create(dto: GrupoValorFijoCreateUpdateDto): Promise<GrupoValorFijoDto> {
    const { data } = await apiClient.post<GrupoValorFijoDto>('/grupos-valor-fijo', dto)
    return data
  },

  async update(id: number, dto: GrupoValorFijoCreateUpdateDto): Promise<GrupoValorFijoDto> {
    const { data } = await apiClient.put<GrupoValorFijoDto>(`/grupos-valor-fijo/${id}`, dto)
    return data
  },

  async delete(id: number): Promise<void> {
    await apiClient.delete(`/grupos-valor-fijo/${id}`)
  },
}
