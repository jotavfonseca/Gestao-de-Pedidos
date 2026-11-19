import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { fetchOrder } from '../services/api'
import { useOrderUpdates } from '../realtime/useOrderUpdates'
import StatusBadge from '../components/StatusBadge'
import StatusProgress from '../components/StatusProgress'
import StatusTimeline from '../components/StatusTimeline'
import { Card, ErrorState, Skeleton } from '../components/Feedback'
import { formatCurrency, formatDateTime } from '../utils/format'

export default function OrderDetails() {
  const { id } = useParams()
  const [order, setOrder] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [notFound, setNotFound] = useState(false)
  const [flash, setFlash] = useState(false)
  const statusRef = useRef(null)

  const load = useCallback(
    async ({ silent = false } = {}) => {
      if (!silent) setLoading(true)
      try {
        const data = await fetchOrder(id)
        if (statusRef.current && statusRef.current !== data.status) {
          setFlash(true)
          setTimeout(() => setFlash(false), 1600)
        }
        statusRef.current = data.status
        setOrder(data)
        setError(null)
        setNotFound(false)
      } catch (e) {
        if (e.status === 404) setNotFound(true)
        else if (!silent) setError(e.message)
      } finally {
        if (!silent) setLoading(false)
      }
    },
    [id],
  )

  useEffect(() => {
    load()
  }, [load])

  useOrderUpdates(
    (event) => {
      // Reload to get the full history, including the new entry.
      if (event.orderId === id) load({ silent: true })
    },
    () => {
      if (order?.status !== 'Completed') load({ silent: true })
    },
  )

  if (notFound) {
    return (
      <Card className="mx-auto max-w-lg p-10 text-center">
        <p className="text-lg font-semibold text-slate-900">Order not found</p>
        <p className="mt-1 text-sm text-slate-500">Check the address or go back to the list.</p>
        <Link to="/" className="mt-5 inline-block font-medium text-brand-600 hover:text-brand-700">← View orders</Link>
      </Card>
    )
  }

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <Link to="/" className="text-sm font-medium text-brand-600 hover:text-brand-700">← Orders</Link>

      {error && <ErrorState message={error} onRetry={() => load()} />}

      {!error && loading && (
        <Card className="space-y-4 p-6">
          <Skeleton className="h-6 w-1/3" />
          <Skeleton className="h-4 w-2/3" />
          <Skeleton className="h-24 w-full" />
        </Card>
      )}

      {!error && !loading && order && (
        <>
          <Card className={`p-6 ${flash ? 'animate-flash' : ''}`}>
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div className="min-w-0">
                <p className="font-mono text-xs text-slate-500">Order {order.id}</p>
                <h1 className="mt-1 truncate text-2xl font-semibold tracking-tight text-slate-900">{order.product}</h1>
                <p className="mt-1 text-slate-600">{order.customer}</p>
              </div>
              <StatusBadge status={order.status} />
            </div>

            <dl className="mt-6 grid gap-4 sm:grid-cols-2">
              <div className="rounded-lg bg-slate-50 p-4">
                <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">Amount</dt>
                <dd className="mt-1 text-xl font-semibold tabular-nums text-slate-900">{formatCurrency(order.amount)}</dd>
              </div>
              <div className="rounded-lg bg-slate-50 p-4">
                <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">Created</dt>
                <dd className="mt-1 text-xl font-semibold text-slate-900">{formatDateTime(order.createdAt)}</dd>
              </div>
            </dl>

            <div className="mt-6">
              <StatusProgress status={order.status} />
            </div>
          </Card>

          <Card className="p-6">
            <h2 className="mb-5 text-base font-semibold text-slate-900">Status history</h2>
            <StatusTimeline history={order.statusHistory} />
          </Card>
        </>
      )}
    </div>
  )
}
