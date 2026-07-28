<script setup lang="ts">

</script>

<template>
  <div class="app-shell">
    <header class="app-header">
      <div>
        <p class="eyebrow">Nomenclador salarial</p>
        <h1>Sistema de configuración</h1>
      </div>
      <nav class="app-nav">
        <RouterLink to="/configuraciones">Configuraciones</RouterLink>
        <RouterLink to="/escalas">Escalas</RouterLink>
        <RouterLink to="/valores-categoria">Valores por categoría</RouterLink>
        <RouterLink to="/valores-fijos">Valores fijos</RouterLink>
      </nav>
    </header>

    <main class="app-content">
      <RouterView />
    </main>
  </div>
</template>

