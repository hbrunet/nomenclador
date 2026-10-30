# Configuraciones

## Buscar configuraciones
En el menú Configuraciones podés filtrar por nomenclador, escala salarial, zona, mes/año de vigencia y estado (Activa, Futura o Vencida). Combiná varios filtros y seleccioná “Aplicar filtros”. La tabla permite recorrer las páginas y abrir una configuración con “Editar”.

Cada fila muestra la combinación y el período de vigencia. Si el período no tiene fecha fin, se muestra como vigente. Para buscar un detalle desde el asistente, podés usar el Id de la configuración o filtros que la identifiquen.

## Crear o editar una configuración
Seleccioná “Nueva configuración” desde el listado, o abrí una fila para editarla. En “Datos generales” elegí un nomenclador y una escala salarial; la zona es opcional. Seleccioná fecha de inicio y, si corresponde, fecha fin. Guardá los datos generales antes de cargar los elementos asociados.

Una configuración necesita nomenclador y escala. La fecha fin no puede ser anterior a la fecha de inicio. No puede superponerse con otra configuración de la misma combinación de nomenclador, escala y zona. Si aparece un error de superposición, revisá el rango y los registros existentes antes de volver a intentar.

Al crear una configuración, primero se guardan los datos generales. Luego quedan disponibles las pestañas de Conceptos, Valores fijos, Valores por categoría y Categorías de la escala. Usá las acciones de cada pestaña para agregar, editar o quitar sus elementos; algunas operaciones guardan directamente y otras muestran su propio botón Guardar.

## Pestañas de una configuración
En Conceptos se administran los conceptos asociados y su orden. En Valores fijos se asocian valores fijos, con un máximo de uno por tipo. En Valores por categoría se administran los valores asociados para cada categoría. La pestaña Categorías permite consultar o actualizar los montos de las categorías de la escala elegida.

Los cambios de conceptos y valores pueden afectar la liquidación de las configuraciones relacionadas. Revisá el Id y la descripción del registro seleccionado antes de quitarlo o reemplazarlo.

## Clonar una configuración
Desde una fila del listado, usá “Clonar” para crear una configuración basada en la seleccionada. Definí el inicio y, opcionalmente, el fin del nuevo período. La clonación individual copia los conceptos y valores asociados; el sistema valida el nuevo rango antes de guardarlo.

Para clonar varias a la vez, seleccioná únicamente configuraciones Activas en el listado y elegí “Clonar seleccionadas”. Indicá el inicio y el fin opcional del nuevo período. Al continuar, las configuraciones originales se cierran el mes anterior al nuevo inicio y se crean los clones para el nuevo período. Si hay errores de validación o superposición, corregí las fechas o la selección.
