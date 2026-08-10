// El backend maneja fechas "solo calendario" (DateOnly, sin hora/zona horaria),
// serializadas como "YYYY-MM-DD". Parsearlas con `new Date("YYYY-MM-DD")` las
// interpreta como medianoche UTC (comportamiento del spec de JS para fechas ISO
// sin hora); al leerlas luego con getters locales (getMonth/getDate/getFullYear,
// como hace PrimeVue DatePicker) en una zona horaria detrás de UTC (ej. Argentina
// UTC-3), el resultado se corre un día/mes hacia atrás. Estas utilidades evitan
// ese salto construyendo/formateando siempre en horario local, sin pasar por UTC.

export function parseLocalDate(value: string): Date {
  const [year, month, day] = value.split('-').map(Number)
  return new Date(year, month - 1, day)
}

export function formatLocalDate(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function formatPeriodo(fecha: string): string {
  const date = parseLocalDate(fecha)
  return `${(date.getMonth() + 1).toString().padStart(2, '0')}/${date.getFullYear()}`
}
