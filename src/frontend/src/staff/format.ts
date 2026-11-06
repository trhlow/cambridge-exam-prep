export function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : 'Something went wrong. Please try again.'
}

export function formatDate(iso: string): string {
  const [y, m, d] = iso.split('-')
  return `${d}/${m}/${y}`
}

export function formatTime(iso: string | null): string {
  return iso ? new Date(iso).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' }) : '–'
}

export function formatDateTime(iso: string | null): string {
  return iso ? new Date(iso).toLocaleString('en-GB', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }) : '–'
}

/** "ABBAABA" → "Part 1 A · Part 2 B · …": which source paper each part of a mixed paper comes from (VE-65). */
export function patternText(pattern: string) {
  return [...pattern].map((source, index) => `Part ${index + 1} ${source}`).join(' · ')
}

/** Today's date as yyyy-mm-dd on the computer's clock; `toISOString()` is UTC, a day behind before 07:00 in Vietnam. */
export function localDate(date = new Date()): string {
  const two = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${two(date.getMonth() + 1)}-${two(date.getDate())}`
}
