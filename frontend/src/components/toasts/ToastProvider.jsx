import { useCallback, useMemo, useRef, useState } from 'react'
import { ToastContext } from './context'

const DURATION_MS = 4000
const MAX_VISIBLE = 4

const tones = {
  info: 'border-sky-200 bg-white',
  success: 'border-emerald-200 bg-white',
  error: 'border-rose-200 bg-white',
}

const dots = {
  info: 'bg-sky-500',
  success: 'bg-emerald-500',
  error: 'bg-rose-500',
}

export default function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([])
  const nextId = useRef(0)

  const dismiss = useCallback((id) => setToasts((current) => current.filter((t) => t.id !== id)), [])

  const push = useCallback(
    ({ title, description, tone = 'info' }) => {
      const id = ++nextId.current
      setToasts((current) => [...current.slice(-(MAX_VISIBLE - 1)), { id, title, description, tone }])
      setTimeout(() => dismiss(id), DURATION_MS)
    },
    [dismiss],
  )

  const value = useMemo(() => ({ push }), [push])

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div
        aria-live="polite"
        className="pointer-events-none fixed inset-x-4 bottom-4 z-50 flex flex-col items-end gap-2 sm:inset-x-auto sm:right-6"
      >
        {toasts.map((toast) => (
          <div
            key={toast.id}
            role="status"
            className={`pointer-events-auto flex w-full max-w-sm animate-toast-in items-start gap-3 rounded-xl border p-4 shadow-lg ${tones[toast.tone]}`}
          >
            <span className={`mt-1.5 size-2 shrink-0 rounded-full ${dots[toast.tone]}`} aria-hidden="true" />
            <div className="min-w-0 flex-1">
              <p className="text-sm font-semibold text-slate-900">{toast.title}</p>
              {toast.description && <p className="mt-0.5 text-sm text-slate-600">{toast.description}</p>}
            </div>
            <button
              type="button"
              onClick={() => dismiss(toast.id)}
              className="rounded text-slate-400 hover:text-slate-600"
              aria-label="Dismiss notification"
            >
              ×
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}
