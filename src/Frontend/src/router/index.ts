import { createRouter, createWebHistory } from 'vue-router'
import ConfiguracionesView from '../views/ConfiguracionesView.vue'
import ConfiguracionDetailView from '../views/ConfiguracionDetailView.vue'

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
  ],
})

export default router
