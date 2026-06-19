import { defineStore } from 'pinia'
import { conceptosService } from '../services/conceptosService'
import type { ConceptoCatalogItem } from '../types/configuration'

interface ConceptosState {
  items: ConceptoCatalogItem[]
  loading: boolean
}

export const useConceptosStore = defineStore('conceptos', {
  state: (): ConceptosState => ({
    items: [],
    loading: false,
  }),
  actions: {
    async fetchConceptos(query = '') {
      this.loading = true

      try {
        this.items = await conceptosService.list(query)
      } finally {
        this.loading = false
      }
    },
  },
})
