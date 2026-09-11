import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/authStore'
import { tokenStorage } from '../utils/tokenStorage'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', name: 'login', component: () => import('../views/LoginView.vue') },
    { path: '/', redirect: '/configuraciones' },
    {
      path: '/configuraciones',
      name: 'configuraciones',
      component: () => import('../views/ConfiguracionesView.vue'),
    },
    {
      path: '/configuraciones/nueva',
      name: 'configuracion-nueva',
      component: () => import('../views/ConfiguracionDetailView.vue'),
    },
    {
      path: '/configuraciones/:id',
      name: 'configuracion-detalle',
      component: () => import('../views/ConfiguracionDetailView.vue'),
      props: true,
    },
    { path: '/escalas', name: 'escalas', component: () => import('../views/EscalasView.vue') },
    {
      path: '/escalas/nueva',
      name: 'escala-nueva',
      component: () => import('../views/EscalaDetailView.vue'),
    },
    {
      path: '/escalas/:id',
      name: 'escala-detalle',
      component: () => import('../views/EscalaDetailView.vue'),
    },
    {
      path: '/valores-categoria',
      name: 'valores-categoria',
      component: () => import('../views/ValoresCategoriaView.vue'),
    },
    {
      path: '/valores-categoria/nuevo',
      name: 'valor-categoria-nuevo',
      component: () => import('../views/ValorCategoriaDetailView.vue'),
    },
    {
      path: '/valores-categoria/:id',
      name: 'valor-categoria-detalle',
      component: () => import('../views/ValorCategoriaDetailView.vue'),
    },
    {
      path: '/valores-fijos',
      name: 'valores-fijos',
      component: () => import('../views/ValoresFijosView.vue'),
    },
    { path: '/asociacion-masiva', redirect: '/asociacion-masiva/valores-fijos' },
    {
      path: '/asociacion-masiva/valores-fijos',
      name: 'asociacion-masiva-valores-fijos',
      component: () => import('../views/AsociacionMasivaValoresFijosView.vue'),
    },
    {
      path: '/asociacion-masiva/valores-categoria',
      name: 'asociacion-masiva-valores-categoria',
      component: () => import('../views/AsociacionMasivaValoresCategoriasView.vue'),
    },
    {
      path: '/asociacion-masiva/conceptos',
      name: 'asociacion-masiva-conceptos',
      component: () => import('../views/AsociacionMasivaConceptosView.vue'),
    },
    {
      path: '/clonacion-masiva/valores-fijos',
      name: 'clonacion-masiva-valores-fijos',
      component: () => import('../views/ClonacionMasivaValoresFijosView.vue'),
    },
    {
      path: '/clonacion-masiva/valores-categoria',
      name: 'clonacion-masiva-valores-categoria',
      component: () => import('../views/ClonacionMasivaValoresCategoriaView.vue'),
    },
    {
      path: '/clonacion-masiva/escala-salarial',
      name: 'actualizacion-masiva-escala-salarial',
      component: () => import('../views/ActualizacionMasivaEscalaSalarialView.vue'),
    },
    {
      path: '/grupos-valor-fijo',
      name: 'grupos-valor-fijo',
      component: () => import('../views/GruposValorFijoView.vue'),
    },
    {
      path: '/grupos-valor-categoria',
      name: 'grupos-valor-categoria',
      component: () => import('../views/GruposValorCategoriaView.vue'),
    },
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
