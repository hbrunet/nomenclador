import { apiClient } from './configurationService'
import { tokenStorage } from '../utils/tokenStorage'
import type { AsistenteChatRequestDto, AsistenteEstadoDto, AsistenteEventoDto } from '../types/asistente'

const baseUrl: string = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5297/api'

async function extractErrorMessage(response: Response): Promise<string> {
  try {
    const payload = await response.json()
    if (typeof payload?.mensaje === 'string') return payload.mensaje
    if (typeof payload?.message === 'string') return payload.message
  } catch {
    // respuesta sin cuerpo JSON
  }
  return `Error ${response.status} al consultar el asistente.`
}

export const asistenteService = {
  async getEstado() {
    const { data } = await apiClient.get<AsistenteEstadoDto>('/asistente/estado')
    return data
  },

  // Axios no soporta streaming de respuestas en el navegador, por eso se usa fetch para leer el SSE.
  async chat(
    request: AsistenteChatRequestDto,
    onEvento: (evento: AsistenteEventoDto) => void,
    signal?: AbortSignal,
  ): Promise<void> {
    const response = await fetch(`${baseUrl}/asistente/chat`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream' },
      body: JSON.stringify(request),
      signal,
    })

    if (response.status === 401) {
      tokenStorage.clearSession()
      window.location.href = '/login'
      return
    }

    if (!response.ok || !response.body) {
      throw new Error(await extractErrorMessage(response))
    }

    const reader = response.body.pipeThrough(new TextDecoderStream()).getReader()
    let buffer = ''

    while (true) {
      const { value, done } = await reader.read()
      if (done) break
      buffer += value

      let separator = buffer.indexOf('\n\n')
      while (separator >= 0) {
        const rawEvent = buffer.slice(0, separator)
        buffer = buffer.slice(separator + 2)
        const data = rawEvent
          .split('\n')
          .filter((line) => line.startsWith('data:'))
          .map((line) => line.slice(5).trimStart())
          .join('\n')
        if (data) onEvento(JSON.parse(data) as AsistenteEventoDto)
        separator = buffer.indexOf('\n\n')
      }
    }
  },
}
