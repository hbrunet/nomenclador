// crypto.randomUUID() requiere un contexto seguro (HTTPS o localhost); si la app se sirve por
// HTTP en un host/IP interno, el navegador no la expone y lanza "crypto.randomUUID is not a
// function". getRandomValues() sí está disponible fuera de contextos seguros, así que se arma
// un UUID v4 a mano con ella; como último recurso (entornos sin Web Crypto) se usa Math.random,
// sin pretensión de ser criptográficamente seguro: sólo se usa como id de correlación en la UI.
export function generarId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID()
  }

  if (typeof crypto !== 'undefined' && typeof crypto.getRandomValues === 'function') {
    const bytes = crypto.getRandomValues(new Uint8Array(16))
    bytes[6] = (bytes[6] & 0x0f) | 0x40
    bytes[8] = (bytes[8] & 0x3f) | 0x80
    const hex = Array.from(bytes, (b) => b.toString(16).padStart(2, '0')).join('')
    return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`
  }

  return `id-${Date.now()}-${Math.random().toString(16).slice(2)}`
}
