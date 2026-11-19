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
      // Destaca pedidos cujo status mudou desde a última carga (útil no modo polling).
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
          <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Pedidos</h1>
          <p className="mt-1 text-sm text-slate-500">
            {loading ? 'Carregando…' : `${orders.length} pedido${orders.length === 1 ? '' : 's'}`}
          </p>
        </div>
        <Link
          to="/orders/new"
          className="rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-brand-700"
        >
          Novo pedido
        </Link>
      </div>

      {error && <ErrorState message={error} onRetry={() => load()} />}

      {!error && loading && <ListSkeleton />}

      {!error && !loading && orders.length === 0 && (
        <Card className="p-10 text-center">
          <p className="font-medium text-slate-900">Nenhum pedido ainda</p>
          <p className="mt-1 text-sm text-slate-500">Crie o primeiro pedido para ver o processamento assíncrono em ação.</p>
          <Link
            to="/orders/new"
            className="mt-5 inline-block rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700"
          >
            Criar primeiro pedido
          </Link>
        </Card>
      )}

      {!error && !loading && orders.length > 0 && (
        <>
          {/* Telas médias e grandes: tabela */}
          <Card className="hidden overflow-hidden md:block">
            <table className="min-w-full divide-y divide-slate-200 text-sm">
              <thead className="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                <tr>
                  <th scope="col" className="px-4 py-3">Pedido</th>
                  <th scope="col" className="px-4 py-3">Cliente</th>
                  <th scope="col" className="px-4 py-3">Produto</th>
                  <th scope="col" className="px-4 py-3 text-right">Valor</th>
                  <th scope="col" className="px-4 py-3">Status</th>
                  <th scope="col" className="px-4 py-3">Criado em</th>
                  <th scope="col" className="px-4 py-3"><span className="sr-only">Ações</span></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {orders.map((order) => (
                  <tr key={order.id} className={`hover:bg-slate-50 ${highlighted[order.id] ? 'animate-flash' : ''}`}>
                    <td className="px-4 py-3 font-mono text-xs text-slate-500">#{shortId(order.id)}</td>
                    <td className="max-w-48 truncate px-4 py-3 font-medium text-slate-900">{order.cliente}</td>
                    <td className="max-w-56 truncate px-4 py-3 text-slate-600">{order.produto}</td>
                    <td className="px-4 py-3 text-right tabular-nums">{formatCurrency(order.valor)}</td>
                    <td className="px-4 py-3"><StatusBadge status={order.status} /></td>
                    <td className="whitespace-nowrap px-4 py-3 text-slate-500">{formatDateTime(order.dataCriacao)}</td>
                    <td className="px-4 py-3 text-right">
                      <Link to={`/orders/${order.id}`} className="font-medium text-brand-600 hover:text-brand-700">
                        Detalhes<span className="sr-only"> do pedido de {order.cliente}</span>
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Card>

          {/* Telas pequenas: cards */}
          <ul className="space-y-3 md:hidden">
            {orders.map((order) => (
              <li key={order.id}>
                <Link
                  to={`/orders/${order.id}`}
                  className={`block rounded-xl border border-slate-200 bg-white p-4 shadow-sm ${highlighted[order.id] ? 'animate-flash' : ''}`}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0">
                      <p className="truncate font-medium text-slate-900">{order.cliente}</p>
                      <p className="truncate text-sm text-slate-600">{order.produto}</p>
                    </div>
                    <StatusBadge status={order.status} />
                  </div>
                  <div className="mt-3 flex items-center justify-between text-sm">
                    <span className="font-semibold tabular-nums text-slate-900">{formatCurrency(order.valor)}</span>
                    <span className="text-slate-500">{formatDateTime(order.dataCriacao)}</span>
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
