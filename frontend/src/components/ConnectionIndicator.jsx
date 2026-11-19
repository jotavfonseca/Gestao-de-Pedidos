import { useRealtime } from '../realtime/context'

const states = {
  connected: { label: 'Live', dot: 'bg-emerald-500', hint: 'Updates arrive over SignalR.' },
  connecting: { label: 'Connecting…', dot: 'bg-slate-400', hint: 'Opening the real-time connection.' },
  reconnecting: { label: 'Polling', dot: 'bg-amber-500', hint: 'Real-time connection dropped; refreshing every 5 s.' },
  disconnected: { label: 'Polling', dot: 'bg-amber-500', hint: 'No real-time connection; refreshing every 5 s.' },
}

export default function ConnectionIndicator() {
  const { status } = useRealtime()
  const state = states[status] ?? states.disconnected

  return (
    <span
      className="inline-flex items-center gap-2 rounded-full border border-slate-200 bg-white px-3 py-1 text-xs font-medium text-slate-600"
      title={state.hint}
      role="status"
    >
      <span className={`size-2 rounded-full ${state.dot}`} aria-hidden="true" />
      {state.label}
    </span>
  )
}
