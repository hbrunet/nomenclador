const TOKEN_KEY = 'nomenclador.token'
const DISPLAY_NAME_KEY = 'nomenclador.displayName'

export const tokenStorage = {
  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY)
  },

  getDisplayName(): string | null {
    return localStorage.getItem(DISPLAY_NAME_KEY)
  },

  setSession(token: string, displayName: string) {
    localStorage.setItem(TOKEN_KEY, token)
    localStorage.setItem(DISPLAY_NAME_KEY, displayName)
  },

  clearSession() {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(DISPLAY_NAME_KEY)
  },
}
