import { weekDays } from '@/types/models'

export function formatSendDays(mask: number): string {
  if (mask === 0) return 'No days selected'
  if (mask === 127) return 'Every day'
  if (mask === 62) return 'Weekdays'
  if (mask === 65) return 'Weekends'
  return weekDays
    .filter((day) => (mask & day.bit) !== 0)
    .map((day) => day.label)
    .join(', ')
}

export function formatTimeWindow(start: string, end: string): string {
  return `${start.slice(0, 5)} - ${end.slice(0, 5)}`
}
