const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

function pad(value: number): string {
  return String(value).padStart(2, '0')
}

function toDate(value: string): Date | null {
  // Plain date (YYYY-MM-DD) parsed as local to avoid a UTC day shift.
  const dateOnly = value.match(/^(\d{4})-(\d{2})-(\d{2})$/)
  if (dateOnly) {
    return new Date(Number(dateOnly[1]), Number(dateOnly[2]) - 1, Number(dateOnly[3]))
  }

  // "YYYY-MM-DD HH:MM:SS" -> make it ISO-parseable.
  const normalized = value.includes(' ') && !value.includes('T') ? value.replace(' ', 'T') : value
  const parsed = new Date(normalized)
  return Number.isNaN(parsed.getTime()) ? null : parsed
}

function toDateValue(value: string | Date | null | undefined): Date | null {
  if (!value) return null
  if (value instanceof Date) return Number.isNaN(value.getTime()) ? null : value
  return toDate(value)
}

// Formats a date as dd-mmm-yyyy (e.g. 01-Dec-2005).
export function formatDate(value: string | Date | null | undefined): string {
  const date = toDateValue(value)
  if (!date) return '—'
  return `${pad(date.getDate())}-${MONTHS[date.getMonth()]}-${date.getFullYear()}`
}

// Formats a date and time as dd-mmm-yyyy HH:MM (no seconds).
export function formatDateTime(value: string | Date | null | undefined): string {
  const date = toDateValue(value)
  if (!date) return '—'
  return `${pad(date.getDate())}-${MONTHS[date.getMonth()]}-${date.getFullYear()} ${pad(date.getHours())}:${pad(date.getMinutes())}`
}
