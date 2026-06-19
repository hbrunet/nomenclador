import axios from 'axios'
import type {
  CatalogItem,
  CategoriaCatalogItem,
  ClonarConfiguracionDto,
  ConfigurationFilters,
  ConfiguracionNomencladorCreateUpdateDto,
  ConfiguracionNomencladorDetailDto,
  ConfiguracionNomencladorListItemDto,
  ValidacionConfiguracionResponse,
  ValorFijoCatalogItem,
} from '../types/configuration'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5297/api',
  headers: {
    'Content-Type': 'application/json',
  },
})

export const configurationService = {
  async list(filters: ConfigurationFilters = {}) {
    const { data } = await apiClient.get<ConfiguracionNomencladorListItemDto[]>(
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
      payload,
    )
    return data
  },

  async update(id: number, payload: ConfiguracionNomencladorCreateUpdateDto) {
    const { data } = await apiClient.put<ConfiguracionNomencladorDetailDto>(
      `/configuraciones-nomenclador/${id}`,
      payload,
    )
    return data
  },

  async validate(payload: ConfiguracionNomencladorCreateUpdateDto) {
    const { data } = await apiClient.post<ValidacionConfiguracionResponse>(
      '/configuraciones-nomenclador/validar',
      payload,
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

  async getValoresFijos() {
    const { data } = await apiClient.get<ValorFijoCatalogItem[]>('/catalogs/valores-fijos')
    return data
  },
}
