<script setup lang="ts">
import { ref } from 'vue'
import Dialog from 'primevue/dialog'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import { conceptosService } from '../services/conceptosService'
import FormulaDetailDialog from './FormulaDetailDialog.vue'
import type { FormulaItem } from '../types/configuration'


const isVisible = ref(false)
const formulaDialogRef = ref<InstanceType<typeof FormulaDetailDialog> | null>(null)
const editingId = ref<number | null>(null)

const formulas = ref<FormulaItem[]>([])

async function open({ conceptoId }: { conceptoId: number }) {
    editingId.value = conceptoId
    const data = await conceptosService.getById(editingId.value)

    formulas.value = data.formulas
    isVisible.value = true
}

async function handleFormulaSaved() {
    if (!editingId.value) return
    const data = await conceptosService.getById(editingId.value)
    formulas.value = data.formulas
}

defineExpose({ open })
</script>

<template>
    <Dialog v-model:visible="isVisible" :header="'Formulas del concepto - ' + editingId" :modal="true"
        :style="{ width: '52rem' }">


        <div class="pt-3 flex flex-column gap-3">
            <div class="flex justify-content-end">
                <Button label="Nueva fórmula" icon="pi pi-plus" size="small" :disabled="!editingId"
                    @click="formulaDialogRef?.open({ conceptoId: editingId! })" />
            </div>
            <DataTable :value="formulas" data-key="id" striped-rows>
                <template #empty>
                    <span class="muted">Este concepto no tiene fórmulas asociadas.</span>
                </template>
                <Column field="id" header="ID" />
                <Column field="condicion" header="Condición" />
                <Column field="accion" header="Acción" />
                <Column>
                    <template #body="{ data }">
                        <div class="flex gap-1 align-items-center">
                            <Button icon="pi pi-pencil" size="small" severity="secondary" outlined
                                @click="formulaDialogRef?.open({ formulaId: data.id })" />
                        </div>
                    </template>
                </Column>
            </DataTable>
        </div>

    </Dialog>

    <FormulaDetailDialog ref="formulaDialogRef" @saved="handleFormulaSaved" />
</template>
