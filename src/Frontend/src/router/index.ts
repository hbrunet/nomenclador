import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/authStore'
import { tokenStorage } from '../utils/tokenStorage'
import LoginView from '../views/LoginView.vue'
import ConfiguracionesView from '../views/ConfiguracionesView.vue'
import EscalasView from '../views/EscalasView.vue'
import EscalaDetailView from '../views/EscalaDetailView.vue'
import ValoresCategoriaView from '../views/ValoresCategoriaView.vue'
import ValorCategoriaDetailView from '../views/ValorCategoriaDetailView.vue'
import ValoresFijosView from '../views/ValoresFijosView.vue'
import AsociacionMasivaValoresFijosView from '../views/AsociacionMasivaValoresFijosView.vue'
import AsociacionMasivaValoresCategoriasView from '../views/AsociacionMasivaValoresCategoriasView.vue'
import AsociacionMasivaConceptosView from '../views/AsociacionMasivaConceptosView.vue'
import ClonacionMasivaValoresFijosView from '../views/ClonacionMasivaValoresFijosView.vue'
import ClonacionMasivaValoresCategoriaView from '../views/ClonacionMasivaValoresCategoriaView.vue'
import ActualizacionMasivaEscalaSalarialView from '../views/ActualizacionMasivaEscalaSalarialView.vue'
import GruposValorFijoView from '../views/GruposValorFijoView.vue'
import GruposValorCategoriaView from '../views/GruposValorCategoriaView.vue'
const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', name: 'login', component: LoginView },
    { path: '/', redirect: '/configuraciones' },
    { path: '/configuraciones', name: 'configuraciones', component: ConfiguracionesView },
    { path: '/configuraciones/nueva', name: 'configuracion-nueva', component: ConfiguracionDetailView },
    { path: '/configuraciones/:id', name: 'configuracion-detalle', component: ConfiguracionDetailView, props: true },
    { path: '/escalas', name: 'escalas', component: EscalasView },
    { path: '/escalas/nueva', name: 'escala-nueva', component: EscalaDetailView },
    { path: '/escalas/:id', name: 'escala-detalle', component: EscalaDetailView },
    { path: '/valores-categoria', name: 'valores-categoria', component: ValoresCategoriaView },
    { path: '/valores-categoria/nuevo', name: 'valor-categoria-nuevo', component: ValorCategoriaDetailView },
    { path: '/valores-categoria/:id', name: 'valor-categoria-detalle', component: ValorCategoriaDetailView },
    { path: '/valores-fijos', name: 'valores-fijos', component: ValoresFijosView },
    { path: '/asociacion-masiva', redirect: '/asociacion-masiva/valores-fijos' },
    {
      path: '/asociacion-masiva/valores-fijos',
      name: 'asociacion-masiva-valores-fijos',
      component: AsociacionMasivaValoresFijosView,
    },
    {
      path: '/asociacion-masiva/valores-categoria',
      name: 'asociacion-masiva-valores-categoria',
      component: AsociacionMasivaValoresCategoriasView,
    },
    {
      path: '/asociacion-masiva/conceptos',
      name: 'asociacion-masiva-conceptos',
      component: AsociacionMasivaConceptosView,
    },
    {
      path: '/clonacion-masiva/valores-fijos',
      name: 'clonacion-masiva-valores-fijos',
      component: ClonacionMasivaValoresFijosView,
    },
    {
      path: '/clonacion-masiva/valores-categoria',
      name: 'clonacion-masiva-valores-categoria',
      component: ClonacionMasivaValoresCategoriaView,
    },
    {
      path: '/clonacion-masiva/escala-salarial',
      name: 'actualizacion-masiva-escala-salarial',
      component: ActualizacionMasivaEscalaSalarialView,
    },
    { path: '/grupos-valor-fijo', name: 'grupos-valor-fijo', component: GruposValorFijoView },
    { path: '/grupos-valor-categoria', name: 'grupos-valor-categoria', component: GruposValorCategoriaView },
  ],
})

router.beforeEach((to) => {
  if (to.name === 'login') return true
  const authStore = useAuthStore()
  if (!tokenStorage.getDisplayName()) {
    authStore.clearSession()
    return { name: 'login' }
  }
  return true
})

export default router
