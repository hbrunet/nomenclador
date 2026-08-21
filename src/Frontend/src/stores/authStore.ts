import { defineStore } from 'pinia'
import { authService } from '../services/authService'
import { tokenStorage } from '../utils/tokenStorage'
import type { LoginDto } from '../types/auth'

interface AuthState {
  token: string | null
  displayName: string | null
  loggingIn: boolean
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    token: tokenStorage.getToken(),
    displayName: tokenStorage.getDisplayName(),
    loggingIn: false,
  }),
  getters: {
    isAuthenticated: (state) => !!state.token,
  },
  actions: {
    async login(credentials: LoginDto) {
      this.loggingIn = true
      try {
        const result = await authService.login(credentials)
        tokenStorage.setSession(result.token, result.displayName)
        this.token = result.token
        this.displayName = result.displayName
      } finally {
        this.loggingIn = false
      }
    },

    logout() {
      tokenStorage.clearSession()
      this.token = null
      this.displayName = null
    },
  },
})
