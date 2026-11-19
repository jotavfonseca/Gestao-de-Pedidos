export const STATUS_SEQUENCE = ['Pending', 'Processing', 'Completed']

export const STATUS_STYLES = {
  Pending: { badge: 'bg-amber-50 text-amber-800 ring-amber-200', dot: 'bg-amber-500' },
  Processing: { badge: 'bg-sky-50 text-sky-800 ring-sky-200', dot: 'bg-sky-500' },
  Completed: { badge: 'bg-emerald-50 text-emerald-800 ring-emerald-200', dot: 'bg-emerald-500' },
}

export const STATUS_DESCRIPTIONS = {
  Pending: 'Order received and waiting to be processed.',
  Processing: 'The worker picked up the message and is processing the order.',
  Completed: 'Processing finished.',
}
