<script setup lang="ts">
import { ref } from 'vue'
import Dialog from 'primevue/dialog'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import { conceptosService } from '../services/conceptosService'
import { formulasService } from '../services/formulasService'
import FormulaDetailDialog from './FormulaDetailDialog.vue'
import type { FormulaItem } from '../types/configuration'


const confirm = useConfirm()
const toast = useToast()

const isVisible = ref(false)
const formulaDialogRef = ref<InstanceType<typeof FormulaDetailDialog> | null>(null)
const editingId = ref<number | null>(null)
const deletingIds = ref<Set<number>>(new Set())

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

function deleteFormula(formulaId: number) {
    confirm.require({
        message: `¿Eliminar la fórmula #${formulaId}? También se eliminará el procedure asociado en la base de datos.`,
        header: 'Confirmar eliminación',
        icon: 'pi pi-exclamation-triangle',
        acceptLabel: 'Eliminar',
        acceptProps: { severity: 'danger' },
        rejectLabel: 'Cancelar',
        rejectProps: { severity: 'secondary', outlined: true },
        accept: () => removeFormula(formulaId),
    })
}

async function removeFormula(formulaId: number) {
    deletingIds.value = new Set(deletingIds.value).add(formulaId)
    try {
        await formulasService.remove(formulaId)
        await handleFormulaSaved()
        toast.add({ severity: 'success', summary: 'Fórmula eliminada', detail: `Se eliminó la fórmula #${formulaId}.`, life: 2500 })
    } catch (e: any) {
        toast.add({
            severity: 'error',
            summary: 'No se pudo eliminar',
            detail: e.response?.data?.message ?? 'Ocurrió un error al eliminar la fórmula.',
            life: 4000,
        })
    } finally {
        const pending = new Set(deletingIds.value)
        pending.delete(formulaId)
        deletingIds.value = pending
    }
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
                            <Button aria-label="Editar fórmula" icon="pi pi-pencil" size="small" severity="secondary" rounded
                                :disabled="deletingIds.has(data.id)"
                                @click="formulaDialogRef?.open({ formulaId: data.id })" />
                            <Button aria-label="Eliminar fórmula" icon="pi pi-trash" size="small" severity="danger" rounded text
                                :loading="deletingIds.has(data.id)" @click="deleteFormula(data.id)" />
                        </div>
                    </template>
                </Column>
            </DataTable>
        </div>

    </Dialog>

    <FormulaDetailDialog ref="formulaDialogRef" @saved="handleFormulaSaved" />
</template>
