import { createRouter, createWebHistory } from 'vue-router'
import ConfiguracionesView from '../views/ConfiguracionesView.vue'
import ConfiguracionDetailView from '../views/ConfiguracionDetailView.vue'
import EscalasView from '../views/EscalasView.vue'
import EscalaDetailView from '../views/EscalaDetailView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      redirect: '/configuraciones',
    },
    {
      path: '/configuraciones',
      name: 'configuraciones',
      component: ConfiguracionesView,
    },
    {
      path: '/configuraciones/nueva',
      name: 'configuracion-nueva',
      component: ConfiguracionDetailView,
    },
    {
      path: '/configuraciones/:id',
      name: 'configuracion-detalle',
      component: ConfiguracionDetailView,
      props: true,
    },
    {
      path: '/escalas',
      name: 'escalas',
      component: EscalasView,
    },
    {
      path: '/escalas/nueva',
      name: 'escala-nueva',
      component: EscalaDetailView,
    },
    {
      path: '/escalas/:id',
      name: 'escala-detalle',
      component: EscalaDetailView,
    },
  ],
})

export default router
