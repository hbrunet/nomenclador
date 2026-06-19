export interface CatalogItem {
  id: number
  descripcion: string
}

export interface CategoriaCatalogItem extends CatalogItem {
  escalaSalarialId: number
  numero: number
}

export interface ValorFijoCatalogItem extends CatalogItem {
  tipo: string
}

export interface ConceptoCatalogItem {
  id: number
  codigo: string
  subcodigo: number
  descripcionBreve: string
  descripcion: string
  clasificacion: string
}

export interface ConceptoConfiguradoViewModel {
  idRelacion: number
  idConcepto: number
  codigo: string
  subcodigo: number
  descripcion: string
  clasificacion: string
  orden: number
  activo: boolean
}

export interface ValorFijoConfiguradoViewModel {
  idRelacion: number
  idValorFijo: number
  descripcion: string
  tipo: string
  importe: number
}

export interface ValorCategoriaConfiguradoViewModel {
  idRelacion: number
  idCategoria: number
  categoriaDescripcion: string
  numeroCategoria: number
  importe: number
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
  idZona: number
  zonaDescripcion: string
  fechaInicio: string
  fechaFin: string | null
  estado: string
  conceptos: ConceptoConfiguradoViewModel[]
  valoresFijos: ValorFijoConfiguradoViewModel[]
  valoresCategorias: ValorCategoriaConfiguradoViewModel[]
}

export interface ConceptoConfiguradoInputDto {
  idConcepto: number
  orden: number
  activo: boolean
}

export interface ValorFijoConfiguradoInputDto {
  idValorFijo: number
  importe: number
}

export interface ValorCategoriaConfiguradoInputDto {
  idCategoria: number
  importe: number
}

export interface ConfiguracionNomencladorCreateUpdateDto {
  idNomenclador: number
  idEscalaSalarial: number
  idZona: number
  fechaInicio: string
  fechaFin: string | null
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

export interface ConfigurationFilters {
  nomencladorId?: number
  escalaSalarialId?: number
  zonaId?: number
  vigenteEn?: string
  estado?: string
}

export interface CatalogsState {
  nomencladores: CatalogItem[]
  escalas: CatalogItem[]
  zonas: CatalogItem[]
  categorias: CategoriaCatalogItem[]
  valoresFijos: ValorFijoCatalogItem[]
}
