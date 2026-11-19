import { STATUS_STYLES } from './status'

export default function StatusBadge({ status }) {
  const style = STATUS_STYLES[status] ?? { badge: 'bg-slate-100 text-slate-700 ring-slate-200', dot: 'bg-slate-400' }
  const processing = status === 'Processando'

  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset transition-colors duration-500 ${style.badge}`}
    >
      <span className="relative flex size-2" aria-hidden="true">
        {processing && <span className={`absolute inline-flex size-full animate-ping rounded-full opacity-75 ${style.dot}`} />}
        <span className={`relative inline-flex size-2 rounded-full ${style.dot}`} />
      </span>
      {status}
    </span>
  )
}
