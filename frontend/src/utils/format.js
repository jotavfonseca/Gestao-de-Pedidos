const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
const dateTime = new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeStyle: 'medium' })

export const formatCurrency = (value) => currency.format(Number(value ?? 0))

export const formatDateTime = (value) => (value ? dateTime.format(new Date(value)) : '—')

export const shortId = (id) => (id ? id.slice(0, 8) : '')

/** Readable duration between two ISO dates (e.g. "5.2 s", "3 min 10 s"). */
export function formatDuration(from, to) {
  const ms = new Date(to) - new Date(from)
  if (!Number.isFinite(ms) || ms < 0) return ''
  const seconds = ms / 1000
  if (seconds < 60) return `${seconds.toLocaleString('en-US', { maximumFractionDigits: 1 })} s`
  const minutes = Math.floor(seconds / 60)
  const rest = Math.round(seconds % 60)
  if (minutes < 60) return rest ? `${minutes} min ${rest} s` : `${minutes} min`
  const hours = Math.floor(minutes / 60)
  return `${hours} h ${minutes % 60} min`
}
