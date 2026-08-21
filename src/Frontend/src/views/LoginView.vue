<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'
import { useAuthStore } from '../stores/authStore'

const router = useRouter()
const authStore = useAuthStore()

const username = ref('')
const password = ref('')
const errorMessage = ref('')

async function handleSubmit() {
  errorMessage.value = ''
  try {
    await authStore.login({ username: username.value, password: password.value })
    await router.push('/configuraciones')
  } catch (e: any) {
    errorMessage.value =
      e.response?.data?.mensaje ??
      'Usuario o contraseña incorrectos.'
  }
}
</script>

<template>
  <div class="login-shell">
    <form class="panel login-card flex flex-column gap-4 p-5" @submit.prevent="handleSubmit">
      <div>
        <p class="eyebrow">Nomenclador salarial</p>
        <h1 class="text-xl mt-1 mb-0 font-semibold">Iniciar sesión</h1>
      </div>

      <div class="flex flex-column gap-1">
        <label class="field-label" for="username">Usuario</label>
        <InputText id="username" v-model="username" class="w-full" autofocus required />
      </div>

      <div class="flex flex-column gap-1">
        <label class="field-label" for="password">Contraseña</label>
        <Password
          id="password"
          v-model="password"
          class="w-full"
          input-class="w-full"
          :feedback="false"
          toggle-mask
          required
        />
      </div>

      <Message v-if="errorMessage" severity="error" :closable="false">{{ errorMessage }}</Message>

      <Button
        type="submit"
        label="Ingresar"
        class="w-full"
        :loading="authStore.loggingIn"
      />
    </form>
  </div>
</template>

<style scoped>
.login-shell {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f3f7fb;
}

.login-card {
  width: 100%;
  max-width: 360px;
}
</style>
