const DISPLAY_NAME_KEY = 'nomenclador.displayName'

export const tokenStorage = {
  getDisplayName(): string | null {
    return localStorage.getItem(DISPLAY_NAME_KEY)
  },

  setSession(displayName: string) {
    localStorage.setItem(DISPLAY_NAME_KEY, displayName)
  },

  clearSession() {
    localStorage.removeItem(DISPLAY_NAME_KEY)
  },
}
