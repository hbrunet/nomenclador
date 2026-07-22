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

export interface ValorCategoriaCatalogItem extends CatalogItem {
  tipo: string
}

export interface ConceptoCatalogItem {
  id: number
  codigo: string
  subcodigo: number
  descripcionBreve: string
  descripcion: string
}

export interface ConceptoConfiguradoViewModel {
  idConcepto: number
  codigo: string
  subcodigo: number
  descripcion: string
  orden: number
}

export interface ValorFijoConfiguradoViewModel {
  idValorFijo: number
  descripcion: string
  tipo: string
  valor: number
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
  idZona: number
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
  vigenteEn?: string
  estado?: string
  page?: number
  pageSize?: number
}

export interface CatalogsState {
  nomencladores: CatalogItem[]
  escalas: CatalogItem[]
  zonas: CatalogItem[]
  categorias: CategoriaCatalogItem[]
  valoresFijos: ValorFijoCatalogItem[]
  valoresCategorias: ValorCategoriaCatalogItem[]
}
