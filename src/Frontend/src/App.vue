<template>
  <div class="app-shell">
    <header class="app-header">
      <div>
        <p class="eyebrow">Nomenclador salarial</p>
        <h1>Sistema de configuración</h1>
      </div>
      <nav class="app-nav">
        <RouterLink to="/configuraciones">Configuraciones</RouterLink>
        <RouterLink to="/configuraciones/nueva">Nueva configuración</RouterLink>
      </nav>
    </header>

    <main class="app-content">
      <RouterView />
    </main>
  </div>
</template>
