import type {
  ConfiguracionNomencladorCreateUpdateDto,
  ValidacionConfiguracionResponse,
  ValidationMessage,
} from '../types/configuration'

export function createEmptyValidation(): ValidacionConfiguracionResponse {
  return {
    valida: true,
    errores: [],
    warnings: [],
  }
}

export function validateDraft(
  draft: ConfiguracionNomencladorCreateUpdateDto,
): ValidacionConfiguracionResponse {
  const errores: ValidationMessage[] = []
  const warnings: ValidationMessage[] = []

  if (!draft.idNomenclador) {
    errores.push({
      codigo: 'NOMENCLADOR_REQUERIDO',
      mensaje: 'Seleccione un nomenclador.',
      campo: 'idNomenclador',
    })
  }

  if (!draft.idEscalaSalarial) {
    errores.push({
      codigo: 'ESCALA_REQUERIDA',
      mensaje: 'Seleccione una escala salarial.',
      campo: 'idEscalaSalarial',
    })
  }

  if (!draft.fechaInicio) {
    errores.push({
      codigo: 'FECHA_INICIO_REQUERIDA',
      mensaje: 'La fecha de inicio es obligatoria.',
      campo: 'fechaInicio',
    })
  }

  if (draft.fechaFin && draft.fechaFin < draft.fechaInicio) {
    errores.push({
      codigo: 'FECHA_FIN_INVALIDA',
      mensaje: 'La fecha fin no puede ser menor a la fecha inicio.',
      campo: 'fechaFin',
    })
  }

  if (!draft.conceptos.length) {
    warnings.push({
      codigo: 'SIN_CONCEPTOS',
      mensaje: 'Todavía no se agregaron conceptos.',
    })
  }

  return {
    valida: errores.length === 0,
    errores,
    warnings,
  }
}

export function mergeValidationResults(
  ...results: ValidacionConfiguracionResponse[]
): ValidacionConfiguracionResponse {
  const errores = results.flatMap((item) => item.errores)
  const warnings = results.flatMap((item) => item.warnings)

  return {
    valida: errores.length === 0,
    errores,
    warnings,
  }
}
