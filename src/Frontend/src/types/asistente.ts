import type { ClonarConfiguracionDto } from './configuration'

export type AsistenteRol = 'user' | 'assistant'

export interface AsistenteMensajeDto {
  rol: AsistenteRol
  contenido: string
}

export interface AsistenteChatRequestDto {
  mensajes: AsistenteMensajeDto[]
}

export type AsistenteAccionTipo =
  | 'clonar-configuracion'
  | 'agregar-concepto'
  | 'quitar-concepto'
  | 'agregar-valor-fijo'
  | 'quitar-valor-fijo'

export interface AsistenteAccionPropuestaDto {
  id: string
  tipo: AsistenteAccionTipo
  descripcion: string
  configuracionId: number
  conceptoId: number | null
  valorFijoId: number | null
  clonar: ClonarConfiguracionDto | null
}

export interface AsistenteEventoDto {
  tipo: 'texto' | 'herramienta' | 'accion' | 'error' | 'fin'
  texto: string | null
  accion: AsistenteAccionPropuestaDto | null
}

export interface AsistenteEstadoDto {
  habilitado: boolean
}
