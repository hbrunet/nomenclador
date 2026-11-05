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

export type AsistenteFeedbackMotivo = 'dato_incorrecto' | 'respuesta_incompleta' | 'no_entendio' | 'otro'

export interface AsistenteFeedbackCreateDto {
  respuestaId: string
  util: boolean
  motivo: AsistenteFeedbackMotivo | null
  comentario: string | null
  herramientas: string[]
  consultaUsuario: string | null
  respuestaAsistente: string | null
}

export interface AsistenteFeedbackMotivoResumenDto {
  motivo: string
  cantidad: number
}

export interface AsistenteFeedbackResumenDto {
  totalVotos: number
  positivos: number
  negativos: number
  motivosNegativos: AsistenteFeedbackMotivoResumenDto[]
}
