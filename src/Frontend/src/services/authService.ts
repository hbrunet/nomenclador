import { apiClient } from './configurationService'
import type { LoginDto, LoginResultDto } from '../types/auth'

export const authService = {
  async login(credentials: LoginDto): Promise<LoginResultDto> {
    const { data } = await apiClient.post<LoginResultDto>('/seg/login', credentials)
    return data
  },
}
