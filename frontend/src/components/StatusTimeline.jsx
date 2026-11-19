import { formatDateTime, formatDuration } from '../utils/format'
import { STATUS_DESCRIPTIONS, STATUS_STYLES } from './status'

/** Timeline of the status history, with the time between steps. */
export default function StatusTimeline({ history }) {
  if (!history?.length) {
    return <p className="text-sm text-slate-500">No history yet.</p>
  }

  return (
    <ol className="relative space-y-6 border-l border-slate-200 pl-6">
      {history.map((entry, index) => {
        const previous = history[index - 1]
        return (
          <li key={`${entry.status}-${entry.changedAt}`} className="relative animate-toast-in">
            <span
              className={`absolute -left-[31px] top-1 size-3.5 rounded-full ring-4 ring-white ${STATUS_STYLES[entry.status]?.dot ?? 'bg-slate-400'}`}
              aria-hidden="true"
            />
            <div className="flex flex-wrap items-baseline gap-x-3 gap-y-1">
              <p className="font-medium text-slate-900">{entry.status}</p>
              <time dateTime={entry.changedAt} className="text-sm text-slate-500">
                {formatDateTime(entry.changedAt)}
              </time>
              {previous && (
                <span className="rounded bg-slate-100 px-1.5 py-0.5 text-xs text-slate-600">
                  +{formatDuration(previous.changedAt, entry.changedAt)}
                </span>
              )}
            </div>
            <p className="mt-1 text-sm text-slate-600">{STATUS_DESCRIPTIONS[entry.status]}</p>
          </li>
        )
      })}
    </ol>
  )
}
