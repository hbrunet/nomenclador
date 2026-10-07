import { apiClient } from './configurationService'
import type { FormulaCreateUpdateDto, FormulaDetailDto, FormulaVerificarResult } from '../types/configuration'

export const formulasService = {
  async getById(id: number): Promise<FormulaDetailDto> {
    const { data } = await apiClient.get<FormulaDetailDto>(`/formulas/${id}`)
    return data
  },

  async create(dto: FormulaCreateUpdateDto): Promise<FormulaDetailDto> {
    const { data } = await apiClient.post<FormulaDetailDto>('/formulas', dto)
    return data
  },

  async update(id: number, dto: FormulaCreateUpdateDto): Promise<FormulaDetailDto> {
    const { data } = await apiClient.put<FormulaDetailDto>(`/formulas/${id}`, dto)
    return data
  },

  async verificar(condicion: string, accion: string): Promise<FormulaVerificarResult> {
    const { data } = await apiClient.post<FormulaVerificarResult>('/formulas/verificar', { condicion, accion })
    return data
  },
}
