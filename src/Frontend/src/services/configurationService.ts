import axios from 'axios'
import { formatLocalDate } from '../utils/date'
import type {
  CatalogItem,
  CategoriaCatalogItem,
  CategoriaMontoUpdateItem,
  ClonarConfiguracionDto,
  ConfigurationFilters,
  ConceptoConfiguradoInputDto,
  ConfiguracionNomencladorCreateUpdateDto,
  ConfiguracionNomencladorDetailDto,
  ConfiguracionNomencladorListItemDto,
  PagedResult,
  ValidacionConfiguracionResponse,
  ValorCategoriaCatalogItem,
  ValorCategoriaConfiguradoInputDto,
  ValorCategoriaItemInputDto,
  ValorFijoCatalogItem,
  ValorFijoConfiguradoInputDto,
} from '../types/configuration'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5297/api',
  headers: {
    'Content-Type': 'application/json',
  },
})

// El draft usa Date (para que los DatePicker funcionen en horario local); acá se
// serializa a "YYYY-MM-DD" en horario local antes de mandarlo al backend. Usar
// JSON.stringify por defecto (Date.toISOString(), en UTC) puede correr la fecha
// un día para atrás/adelante según la zona horaria del navegador.
function toApiPayload(payload: ConfiguracionNomencladorCreateUpdateDto) {
  return {
    ...payload,
    fechaInicio: formatLocalDate(payload.fechaInicio),
    fechaFin: payload.fechaFin ? formatLocalDate(payload.fechaFin) : null,
  }
}

export const configurationService = {
  async list(filters: ConfigurationFilters = {}) {
    const { data } = await apiClient.get<PagedResult<ConfiguracionNomencladorListItemDto>>(
      '/configuraciones-nomenclador',
      { params: filters },
    )
    return data
  },

  async getById(id: number) {
    const { data } = await apiClient.get<ConfiguracionNomencladorDetailDto>(
      `/configuraciones-nomenclador/${id}`,
    )
    return data
  },

  async create(payload: ConfiguracionNomencladorCreateUpdateDto) {
    const { data } = await apiClient.post<ConfiguracionNomencladorDetailDto>(
      '/configuraciones-nomenclador',
      toApiPayload(payload),
    )
    return data
  },

  async update(id: number, payload: ConfiguracionNomencladorCreateUpdateDto) {
    const { data } = await apiClient.put<ConfiguracionNomencladorDetailDto>(
      `/configuraciones-nomenclador/${id}`,
      toApiPayload(payload),
    )
    return data
  },

  async validate(payload: ConfiguracionNomencladorCreateUpdateDto, excludedId?: number) {
    const { data } = await apiClient.post<ValidacionConfiguracionResponse>(
      '/configuraciones-nomenclador/validar',
      toApiPayload(payload),
      { params: excludedId ? { excludedId } : undefined },
    )
    return data
  },

  async clone(id: number, payload: ClonarConfiguracionDto) {
    const { data } = await apiClient.post<ConfiguracionNomencladorDetailDto>(
      `/configuraciones-nomenclador/${id}/clonar`,
      payload,
    )
    return data
  },

  async addConcepto(id: number, concepto: ConceptoConfiguradoInputDto) {
    const { data } = await apiClient.post<ConfiguracionNomencladorDetailDto>(
      `/configuraciones-nomenclador/${id}/concepto`,
      concepto,
    )
    return data
  },

  async removeConcepto(id: number, conceptoId: number) {
    const { data } = await apiClient.delete<ConfiguracionNomencladorDetailDto>(
      `/configuraciones-nomenclador/${id}/concepto/${conceptoId}`,
    )
    return data
  },

  async addValorFijo(id: number, valorFijo: ValorFijoConfiguradoInputDto) {
    const { data } = await apiClient.post<ConfiguracionNomencladorDetailDto>(
      `/configuraciones-nomenclador/${id}/valor-fijo`,
      { idValorFijo: valorFijo.idValorFijo },
    )
    return data
  },

  async removeValorFijo(id: number, valorFijoId: number) {
    const { data } = await apiClient.delete<ConfiguracionNomencladorDetailDto>(
      `/configuraciones-nomenclador/${id}/valor-fijo/${valorFijoId}`,
    )
    return data
  },

  async addValorPorCategoria(id: number, valorCategoria: ValorCategoriaConfiguradoInputDto) {
    const { data } = await apiClient.post<ConfiguracionNomencladorDetailDto>(
      `/configuraciones-nomenclador/${id}/valor-categoria`,
      { idValorCategoria: valorCategoria.idValorCategoria },
    )
    return data
  },

  async removeValorPorCategoria(id: number, valorCategoriaId: number) {
    const { data } = await apiClient.delete<ConfiguracionNomencladorDetailDto>(
      `/configuraciones-nomenclador/${id}/valor-categoria/${valorCategoriaId}`,
    )
    return data
  },

  async getNomencladores() {
    const { data } = await apiClient.get<CatalogItem[]>('/catalogs/nomencladores')
    return data
  },

  async getEscalas() {
    const { data } = await apiClient.get<CatalogItem[]>('/catalogs/escalas')
    return data
  },

  async getZonas() {
    const { data } = await apiClient.get<CatalogItem[]>('/catalogs/zonas')
    return data
  },

  async getCategorias(escalaId?: number) {
    const { data } = await apiClient.get<CategoriaCatalogItem[]>('/catalogs/categorias', {
      params: escalaId ? { escalaId } : {},
    })
    return data
  },

  async updateCategoriaMontos(items: CategoriaMontoUpdateItem[]) {
    await apiClient.put('/catalogs/categorias/montos', items)
  },

  async getValoresFijos() {
    const { data } = await apiClient.get<ValorFijoCatalogItem[]>('/catalogs/valores-fijos')
    return data
  },

  async getValorFijoUsages(id: number) {
    const { data } = await apiClient.get<{ count: number }>(`/catalogs/valores-fijos/${id}/usages`)
    return data
  },

  async updateValorFijo(id: number, valor: number) {
    const { data } = await apiClient.put<ValorFijoCatalogItem>(`/catalogs/valores-fijos/${id}`, { valor })
    return data
  },

  async createValorFijo(payload: { descripcion: string; idTipo: number; valor: number; configuracionId?: number }) {
    const { data } = await apiClient.post<ValorFijoCatalogItem>('/catalogs/valores-fijos', payload)
    return data
  },

  async getValoresCategorias() {
    const { data } = await apiClient.get<ValorCategoriaCatalogItem[]>('/catalogs/valores-categorias')
    return data
  },

  async getValorCategoriaConfiguradoItems(id: number) {
    const { data } = await apiClient.get<ValorCategoriaItemInputDto[]>(`/catalogs/valor-categoria-configurado-items/${id}`)
    return data
  },

  async updateValorCategoriaItems(valorCategoriaId: number, items: ValorCategoriaItemInputDto[]) {
    await apiClient.put(
      `/catalogs/valor-categoria-configurado-items/${valorCategoriaId}`,
      items,
    )
  },
}
