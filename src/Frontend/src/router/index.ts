import { createRouter, createWebHistory } from 'vue-router'
import ConfiguracionesView from '../views/ConfiguracionesView.vue'
import ConfiguracionDetailView from '../views/ConfiguracionDetailView.vue'
import EscalasView from '../views/EscalasView.vue'
import EscalaDetailView from '../views/EscalaDetailView.vue'
import ValoresCategoriaView from '../views/ValoresCategoriaView.vue'
import ValorCategoriaDetailView from '../views/ValorCategoriaDetailView.vue'
import ValoresFijosView from '../views/ValoresFijosView.vue'
import AsociacionMasivaValoresFijosView from '../views/AsociacionMasivaValoresFijosView.vue'
import AsociacionMasivaValoresCategoriasView from '../views/AsociacionMasivaValoresCategoriasView.vue'
const router = createRouter({
  history: createWebHistory(),
  routes: [
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
    }
  ],
})

export default router
