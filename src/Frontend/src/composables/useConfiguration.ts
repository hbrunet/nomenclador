import { storeToRefs } from 'pinia'
import { useConfigurationStore } from '../stores/configurationStore'
import { useConceptosStore } from '../stores/conceptosStore'

export function useConfiguration() {
  const configurationStore = useConfigurationStore()
  const conceptosStore = useConceptosStore()

  const {
    items: configuraciones,
    current,
    draft,
    catalogs,
    pagination,
    validation,
    loadingList,
    loadingDetail,
    saving,
  } = storeToRefs(configurationStore)

  const {
    items: conceptosDisponibles,
    loading: loadingConceptos,
    pagedItems: conceptosPaginados,
    loadingPaged: loadingConceptosPaginados,
    pagination: paginacionConceptos,
    tipos: tiposConcepto,
    tiposLiquidacion,
  } = storeToRefs(conceptosStore)

  return {
    configuraciones,
    current,
    draft,
    catalogs,
    pagination,
    validation,
    loadingList,
    loadingDetail,
    saving,
    conceptosDisponibles,
    loadingConceptos,
    conceptosPaginados,
    loadingConceptosPaginados,
    paginacionConceptos,
    tiposConcepto,
    tiposLiquidacion,
    fetchCatalogs: configurationStore.fetchCatalogs,
    fetchCategorias: configurationStore.fetchCategorias,
    fetchList: configurationStore.fetchList,
    fetchDetail: configurationStore.fetchDetail,
    initializeDraft: configurationStore.initializeDraft,
    saveCurrent: configurationStore.saveCurrent,
    validateCurrent: configurationStore.validateCurrent,
    cloneCurrent: configurationStore.cloneCurrent,
    fetchConceptos: conceptosStore.fetchConceptos,
    fetchConceptosPaginados: conceptosStore.fetchConceptosPaginados,
    fetchTiposConcepto: conceptosStore.fetchTiposConcepto,
    fetchTiposLiquidacion: conceptosStore.fetchTiposLiquidacion,
    fetchConcepto: conceptosStore.fetchConcepto,
    saveConcepto: conceptosStore.saveConcepto,
    deleteFormula: conceptosStore.deleteFormula,
  }
}
