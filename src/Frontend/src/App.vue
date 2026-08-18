<script setup lang="ts">
import { computed } from 'vue'
import Menubar from 'primevue/menubar'
import Toast from 'primevue/toast'
import ConfirmDialog from 'primevue/confirmdialog'
import type { MenuItem } from 'primevue/menuitem'

const menuItems = computed<MenuItem[]>(() => [
  { label: 'Configuraciones', route: '/configuraciones' },
  { label: 'Escalas', route: '/escalas' },

  {
    label: 'Valores fijos',
    items: [
      { label: 'Valores y Tipos', route: '/valores-fijos' },
      { label: 'Grupos', route: '/grupos-valor-fijo' },
    ],
  },

   {
    label: 'Valores por categoría',
    items: [
      { label: 'Valores y Tipos', route: '/valores-categoria' },
      { label: 'Grupos', route: '/grupos-valor-categoria' },
    ],
  },

  {
    label: 'Asociación masiva',
    // Agrupa las variantes de asociación masiva; agregar futuras opciones acá.
    items: [
      { label: 'Valores fijos', route: '/asociacion-masiva/valores-fijos' },
      { label: 'Valores por categoría', route: '/asociacion-masiva/valores-categoria' },
      { label: 'Conceptos', route: '/asociacion-masiva/conceptos' },
    ],
  },
  {
    label: 'Clonación masiva',
    // Agrupa las variantes de clonación masiva; agregar futuras opciones acá.
    items: [
      { label: 'Valores fijos', route: '/clonacion-masiva/valores-fijos' },
      { label: 'Valores por categoría', route: '/clonacion-masiva/valores-categoria' },
    ],
  },
])
</script>

<template>
  <div class="app-shell">
    <header class="app-header">
      <div>
        <p class="eyebrow">Nomenclador salarial</p>
        <h1>Sistema de configuración</h1>
      </div>
      <nav class="app-nav">
        <Menubar :model="menuItems">
          <template #item="{ item, props, hasSubmenu, root }">
            <router-link v-if="item.route" v-slot="{ href, navigate }" :to="item.route" custom>
              <a :href="href" v-bind="props.action" @click="navigate">
                <span>{{ item.label }}</span>
              </a>
            </router-link>
            <a v-else v-bind="props.action">
              <span>{{ item.label }}</span>
              <i v-if="hasSubmenu" :class="['pi', root ? 'pi-angle-down' : 'pi-angle-right']" aria-hidden="true"></i>
            </a>
          </template>
        </Menubar>
      </nav>
    </header>

    <main class="app-content">
      <RouterView />
    </main>

    <Toast />
    <ConfirmDialog />
  </div>
</template>

