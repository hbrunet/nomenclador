import { apiClient } from './configurationService'
import type { AsistenteFeedbackCreateDto, AsistenteFeedbackResumenDto } from '../types/asistente'

export const asistenteFeedbackService = {
  async enviar(dto: AsistenteFeedbackCreateDto): Promise<void> {
    await apiClient.post('/asistente/feedback', dto)
  },

  async getResumen(): Promise<AsistenteFeedbackResumenDto> {
    const { data } = await apiClient.get<AsistenteFeedbackResumenDto>('/asistente/feedback/resumen')
    return data
  },
}
