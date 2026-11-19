import { useCallback, useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { fetchOrders } from '../services/api'
import { useOrderUpdates } from '../realtime/useOrderUpdates'
import StatusBadge from '../components/StatusBadge'
import { Card, ErrorState, Skeleton } from '../components/Feedback'
import { formatCurrency, formatDateTime, shortId } from '../utils/format'

const HIGHLIGHT_MS = 1600

export default function OrdersList() {
  const [orders, setOrders] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [highlighted, setHighlighted] = useState({})
  const ordersRef = useRef(orders)
  ordersRef.current = orders

  const load = useCallback(async ({ silent = false } = {}) => {
    if (!silent) setLoading(true)
    try {
      const data = await fetchOrders()
      // Highlight orders whose status changed since the last load (useful while polling).
      const previous = new Map(ordersRef.current.map((o) => [o.id, o.status]))
      const changed = data.filter((o) => previous.size && previous.get(o.id) !== o.status).map((o) => o.id)
      setOrders(data)
      flash(changed)
      setError(null)
    } catch (e) {
      if (!silent) setError(e.message)
    } finally {
      if (!silent) setLoading(false)
    }
  }, [])

  function flash(ids) {
    if (!ids.length) return
    const stamp = Date.now()
    setHighlighted((current) => ({ ...current, ...Object.fromEntries(ids.map((id) => [id, stamp])) }))
    setTimeout(() => {
      setHighlighted((current) => Object.fromEntries(Object.entries(current).filter(([, t]) => t !== stamp)))
    }, HIGHLIGHT_MS)
  }

  useEffect(() => {
    load()
  }, [load])

  useOrderUpdates(
    (event) => {
      const known = ordersRef.current.some((o) => o.id === event.orderId)
      if (!known) {
        load({ silent: true })
        return
      }
      setOrders((current) => current.map((o) => (o.id === event.orderId ? { ...o, status: event.status } : o)))
      flash([event.orderId])
    },
    () => load({ silent: true }),
  )

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Orders</h1>
          <p className="mt-1 text-sm text-slate-500">
            {loading ? 'Loading…' : `${orders.length} order${orders.length === 1 ? '' : 's'}`}
          </p>
        </div>
        <Link
          to="/orders/new"
          className="rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-brand-700"
        >
          New order
        </Link>
      </div>

      {error && <ErrorState message={error} onRetry={() => load()} />}

      {!error && loading && <ListSkeleton />}

      {!error && !loading && orders.length === 0 && (
        <Card className="p-10 text-center">
          <p className="font-medium text-slate-900">No orders yet</p>
          <p className="mt-1 text-sm text-slate-500">Create the first order to see asynchronous processing in action.</p>
          <Link
            to="/orders/new"
            className="mt-5 inline-block rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700"
          >
            Create the first order
          </Link>
        </Card>
      )}

      {!error && !loading && orders.length > 0 && (
        <>
          {/* Medium and large screens: table */}
          <Card className="hidden overflow-hidden md:block">
            <table className="min-w-full divide-y divide-slate-200 text-sm">
              <thead className="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                <tr>
                  <th scope="col" className="px-4 py-3">Order</th>
                  <th scope="col" className="px-4 py-3">Customer</th>
                  <th scope="col" className="px-4 py-3">Product</th>
                  <th scope="col" className="px-4 py-3 text-right">Amount</th>
                  <th scope="col" className="px-4 py-3">Status</th>
                  <th scope="col" className="px-4 py-3">Created</th>
                  <th scope="col" className="px-4 py-3"><span className="sr-only">Actions</span></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {orders.map((order) => (
                  <tr key={order.id} className={`hover:bg-slate-50 ${highlighted[order.id] ? 'animate-flash' : ''}`}>
                    <td className="px-4 py-3 font-mono text-xs text-slate-500">#{shortId(order.id)}</td>
                    <td className="max-w-48 truncate px-4 py-3 font-medium text-slate-900">{order.customer}</td>
                    <td className="max-w-56 truncate px-4 py-3 text-slate-600">{order.product}</td>
                    <td className="px-4 py-3 text-right tabular-nums">{formatCurrency(order.amount)}</td>
                    <td className="px-4 py-3"><StatusBadge status={order.status} /></td>
                    <td className="whitespace-nowrap px-4 py-3 text-slate-500">{formatDateTime(order.createdAt)}</td>
                    <td className="px-4 py-3 text-right">
                      <Link to={`/orders/${order.id}`} className="font-medium text-brand-600 hover:text-brand-700">
                        Details<span className="sr-only"> for {order.customer}'s order</span>
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Card>

          {/* Small screens: cards */}
          <ul className="space-y-3 md:hidden">
            {orders.map((order) => (
              <li key={order.id}>
                <Link
                  to={`/orders/${order.id}`}
                  className={`block rounded-xl border border-slate-200 bg-white p-4 shadow-sm ${highlighted[order.id] ? 'animate-flash' : ''}`}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0">
                      <p className="truncate font-medium text-slate-900">{order.customer}</p>
                      <p className="truncate text-sm text-slate-600">{order.product}</p>
                    </div>
                    <StatusBadge status={order.status} />
                  </div>
                  <div className="mt-3 flex items-center justify-between text-sm">
                    <span className="font-semibold tabular-nums text-slate-900">{formatCurrency(order.amount)}</span>
                    <span className="text-slate-500">{formatDateTime(order.createdAt)}</span>
                  </div>
                </Link>
              </li>
            ))}
          </ul>
        </>
      )}
    </div>
  )
}

function ListSkeleton() {
  return (
    <Card className="space-y-4 p-6">
      {Array.from({ length: 4 }, (_, i) => (
        <div key={i} className="flex items-center gap-4">
          <Skeleton className="h-4 w-20" />
          <Skeleton className="h-4 flex-1" />
          <Skeleton className="h-6 w-24 rounded-full" />
        </div>
      ))}
    </Card>
  )
}
