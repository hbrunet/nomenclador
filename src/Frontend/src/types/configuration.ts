export interface CatalogItem {
  id: number
  descripcion: string
}

export interface CategoriaCatalogItem extends CatalogItem {
  escalaSalarialId: number
  numero: number
  monto: number
}

export interface CategoriaMontoUpdateItem {
  id: number
  monto: number
}

export interface EscalaListItemDto {
  id: number
  descripcion: string
  cantidadCategorias: number
}

export interface EscalaDetailDto {
  id: number
  descripcion: string
  categorias: CategoriaCatalogItem[]
}

export interface EscalaCreateUpdateDto {
  descripcion: string
}

export interface ClonarEscalaDto {
  nuevoPeriodo: string
  coeficienteAjuste: number
  actualizarSiExiste?: boolean
}

export interface EscalaCloneConflictDto {
  escalaOriginalId: number
  escalaOriginalDescripcion: string
  escalaExistenteId: number
  escalaExistenteDescripcion: string
  configuracionesIds: number[]
}

export interface CategoriaCreateUpdateDto {
  numero: number
  descripcion: string
  monto: number
}

export interface ValorCategoriaListItemDto {
  id: number
  descripcion: string
  idTipo: number
  tipo: string
  cantidadItems: number
}

export interface ValorCategoriaDetailDto {
  id: number
  descripcion: string
  idTipo: number
  tipo: string
  items: ValorCategoriaItemInputDto[]
}

export interface ValorCategoriaCreateUpdateDto {
  descripcion: string
  idTipo: number
}

export interface ValorCategoriaTipoCreateUpdateDto {
  descripcion: string
}

export interface ValorCategoriaItemCreateUpdateDto {
  numeroCategoria: number
  importe: number
}

export interface ValorFijoCatalogItem extends CatalogItem {
  idTipo: number
  tipo: string
  valor: number
}

export interface ValorFijoCreateUpdateDto {
  descripcion: string
  idTipo: number
  valor: number
}

export interface ValorFijoCloneDto {
  descripcion: string
  coeficienteAjuste?: number
  valorNuevo?: number
}

export interface ClonacionMasivaValoresFijosDto {
  valoresFijosIds: number[]
  nuevoPeriodo?: string
  coeficienteAjuste: number
  actualizarValoresExistentes: boolean
  actualizarSiExiste?: boolean
}

export interface ValorFijoCloneConflictDto {
  originalId: number
  originalDescripcion: string
  existenteId: number
  existenteDescripcion: string
}

export interface SustitucionValorFijoBusquedaDto {
  tiposIds: number[]
  periodo: string
}

export interface SustitucionValorFijoCandidato {
  idValorFijo: number
  descripcion: string
  valor: number
}

export interface SustitucionValorFijoMatch {
  idTipo: number
  tipo: string
  encontrado: boolean
  ambiguo: boolean
  idValorFijo: number | null
  descripcion: string | null
  valor: number | null
  candidatos: SustitucionValorFijoCandidato[]
}

export interface SustitucionValorCategoriaBusquedaDto {
  tiposIds: number[]
  periodo: string
}

export interface SustitucionValorCategoriaCandidato {
  idValorCategoria: number
  descripcion: string
  cantidadItems: number
}

export interface SustitucionValorCategoriaMatch {
  idTipo: number
  tipo: string
  encontrado: boolean
  ambiguo: boolean
  idValorCategoria: number | null
  descripcion: string | null
  candidatos: SustitucionValorCategoriaCandidato[]
}

export interface ClonacionMasivaValoresCategoriaDto {
  valoresCategoriaIds: number[]
  nuevoPeriodo?: string
  coeficienteAjuste: number
  actualizarValoresExistentes: boolean
  actualizarSiExiste?: boolean
}

export interface ValorCategoriaCloneConflictDto {
  originalId: number
  originalDescripcion: string
  existenteId: number
  existenteDescripcion: string
}

export interface ActualizacionMasivaEscalaSalarialDto {
  configuracionesIds: number[]
  nuevoPeriodo: string
  coeficienteAjuste: number
  actualizarSiExiste?: boolean
}

export interface ActualizacionMasivaEscalaSalarialResultDto {
  escalasClonadas: number
  configuracionesActualizadas: number
}

export interface GrupoValorFijoDto {
  id: number
  descripcion: string
  tipos: CatalogItem[]
}

export interface GrupoValorFijoCreateUpdateDto {
  descripcion: string
  tiposIds: number[]
}

export interface GrupoValorCategoriaDto {
  id: number
  descripcion: string
  tipos: CatalogItem[]
}

export interface GrupoValorCategoriaCreateUpdateDto {
  descripcion: string
  tiposIds: number[]
}

export interface ValorCategoriaCatalogItem extends CatalogItem {
  idTipo: number
  tipo: string
}

export interface ConceptoCatalogItem {
  id: number
  codigo: number
  subcodigo: number
  descripcionBreve: string
  descripcion: string
  acumulaJubilacion: boolean
  acumulaObraSocial: boolean
  acumulaRemunerativo: boolean
  basico: boolean
  bonificable: boolean
  calculaTicket: boolean
  calculaPorPersona: boolean
  // Números de mes (1=ene..12=dic) en los que aplica el concepto.
  mesesAplicables: number[]
  deduceJubilacion: boolean
  deducePension: boolean
  especial: boolean
  ganancia: boolean
  idPartidaPresupuestaria: number | null
  idTipoConcepto: number | null
  tipoConcepto: string | null
  imprimeCantidad: boolean
  liquidaSiempre: boolean
  participaFondo: boolean
  ppp: boolean
  reliquidar: boolean
  tiposLiquidacion: CatalogItem[]
  formulas: FormulaItem[]
}

// USUARIO.FORMULA — solo lectura, se muestran en el detalle del concepto.
export interface FormulaItem {
  id: number
  condicion: string | null
  accion: string | null
  ordenEjec: number | null
  condicionInput: number | null
  accionInput: number | null
  codigo: string | null
  spName: string | null
}

// Detalle de una fórmula para el editor de alta/edición (GET/POST/PUT /api/formulas).
export interface FormulaDetailDto {
  id: number
  conceptoId: number
  conceptoCodigo: number
  conceptoSubcodigo: number
  conceptoDescripcion: string
  condicion: string
  accion: string
  ordenEjec: number | null
  spName: string | null
}

export interface FormulaCreateUpdateDto {
  conceptoId: number
  condicion: string
  accion: string
}

export interface FormulaVerificarResult {
  valida: boolean
  errores: string[]
}

// Catálogo USUARIO.PRIMITIVA — usadas en Condición/Acción de una fórmula.
export interface PrimitivaItem {
  id: number
  nombre: string | null
  descripcion: string | null
  esResultLogico: boolean
  cabecera: string | null
  cuerpo: string | null
  pie: string | null
}

// Payload para crear/editar un concepto (POST/PUT /api/conceptos).
export interface ConceptoCreateUpdateDto {
  codigo: number
  subcodigo: number
  descripcionBreve: string
  descripcion: string
  acumulaJubilacion: boolean
  acumulaObraSocial: boolean
  acumulaRemunerativo: boolean
  basico: boolean
  bonificable: boolean
  calculaTicket: boolean
  calculaPorPersona: boolean
  mesesAplicables: number[]
  deduceJubilacion: boolean
  deducePension: boolean
  especial: boolean
  ganancia: boolean
  idPartidaPresupuestaria: number | null
  idTipoConcepto: number | null
  imprimeCantidad: boolean
  liquidaSiempre: boolean
  participaFondo: boolean
  ppp: boolean
  reliquidar: boolean
  tiposLiquidacionIds: number[]
}

export interface ConceptoConfiguradoViewModel {
  idConcepto: number
  codigo: number
  subcodigo: number
  descripcion: string
  descripcionBreve: string
  orden: number
}

export interface ValorFijoConfiguradoViewModel {
  idValorFijo: number
  descripcion: string
  tipo: string
  valor: number
  idTipo: number
}

export interface ValorCategoriaItemViewModel {
  id: number
  numeroCategoria: number
  importe: number
}

export interface ValorCategoriaConfiguradoViewModel {
  idValorCategoria: number
  descripcion: string
  tipo: string
  items: ValorCategoriaItemViewModel[]
  idTipo: number
}

export interface ConfiguracionNomencladorListItemDto {
  id: number
  nomencladorDescripcion: string
  escalaDescripcion: string
  zonaDescripcion: string
  fechaInicio: string
  fechaFin: string | null
  estado: string
  cantidadConceptos: number
  cantidadValoresFijos: number
}

export interface ConfiguracionNomencladorDetailDto {
  id: number
  idNomenclador: number
  nomencladorDescripcion: string
  idEscalaSalarial: number
  escalaDescripcion: string
  idZona: number | null
  zonaDescripcion: string
  fechaInicio: string
  fechaFin: string | null
  estado: string
  conceptos: ConceptoConfiguradoViewModel[]
  valoresFijos: ValorFijoConfiguradoViewModel[]
  valoresCategorias: ValorCategoriaConfiguradoViewModel[]
  categorias: CategoriaCatalogItem[]
}

export interface ConceptoConfiguradoInputDto {
  idConcepto: number
  orden: number
}

export interface ValorFijoConfiguradoInputDto {
  idValorFijo: number
  valor: number
}

export interface ValorCategoriaItemInputDto {
  id: number
  numeroCategoria: number
  importe: number
}

export interface ValorCategoriaConfiguradoInputDto {
  idValorCategoria: number
  items: ValorCategoriaItemInputDto[]
}

export interface ConfiguracionNomencladorCreateUpdateDto {
  idNomenclador: number
  idEscalaSalarial: number
  idZona: number | null
  fechaInicio: Date
  fechaFin: Date | null
  conceptos: ConceptoConfiguradoInputDto[]
  valoresFijos: ValorFijoConfiguradoInputDto[]
  valoresCategorias: ValorCategoriaConfiguradoInputDto[]
}

export interface ValidationMessage {
  codigo: string
  mensaje: string
  campo?: string
}

export interface ValidacionConfiguracionResponse {
  valida: boolean
  errores: ValidationMessage[]
  warnings: ValidationMessage[]
}

export interface ClonarConfiguracionDto {
  fechaInicio: string
  fechaFin: string | null
  copiarConceptos: boolean
  copiarValoresFijos: boolean
  copiarValoresCategoria: boolean
}

export interface ClonacionMasivaConfiguracionesDto {
  configuracionesIds: number[]
  fechaInicio: string
  fechaFin: string | null
  copiarConceptos: boolean
  copiarValoresFijos: boolean
  copiarValoresCategoria: boolean
}

export interface ClonacionMasivaConfiguracionesResultDto {
  clones: ConfiguracionNomencladorDetailDto[]
  configuracionesCerradas: number
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface ConfigurationFilters {
  nomencladorId?: number
  escalaSalarialId?: number
  zonaId?: number
  conceptoId?: number
  valorFijoId?: number
  valorCategoriaId?: number
  vigenteEn?: string
  estado?: string
  page?: number
  pageSize?: number
}

export interface AsociacionMasivaValoresFijosDto {
  valoresFijosIds: number[]
  configuracionesIds: number[]
}

export interface AsociacionMasivaValoresCategoriasDto {
  valoresCategoriasIds: number[]
  configuracionesIds: number[]
}

export interface AsociacionMasivaConceptosDto {
  conceptosIds: number[]
  configuracionesIds: number[]
}

export interface AsociacionMasivaResultDto {
  asociacionesCreadas: number
  asociacionesExistentes: number
}

export interface DesasociacionMasivaResultDto {
  asociacionesEliminadas: number
  asociacionesInexistentes: number
}

export interface CatalogsState {
  nomencladores: CatalogItem[]
  escalas: CatalogItem[]
  zonas: CatalogItem[]
  categorias: CategoriaCatalogItem[]
  valoresFijos: ValorFijoCatalogItem[]
  valoresCategorias: ValorCategoriaCatalogItem[]
  periodoActivo: string | null
}
