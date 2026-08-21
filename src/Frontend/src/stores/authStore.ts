import { defineStore } from 'pinia'
import { authService } from '../services/authService'
import { tokenStorage } from '../utils/tokenStorage'
import type { LoginDto } from '../types/auth'

interface AuthState {
  displayName: string | null
  loggingIn: boolean
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    displayName: tokenStorage.getDisplayName(),
    loggingIn: false,
  }),
  getters: {
    isAuthenticated: (state) => !!state.displayName,
  },
  actions: {
    async login(credentials: LoginDto) {
      this.loggingIn = true
      try {
        const result = await authService.login(credentials)
        tokenStorage.setSession(result.displayName)
        this.displayName = result.displayName
      } finally {
        this.loggingIn = false
      }
    },

    clearSession() {
      tokenStorage.clearSession()
      this.displayName = null
    },

    async logout() {
      try {
        await authService.logout()
      } finally {
        this.clearSession()
      }
    },
  },
})
