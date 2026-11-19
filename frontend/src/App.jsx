import { useEffect } from 'react'
import { NavLink, Route, Routes } from 'react-router-dom'
import OrdersList from './pages/OrdersList'
import CreateOrder from './pages/CreateOrder'
import OrderDetails from './pages/OrderDetails'
import AskOrders from './pages/AskOrders'
import NotFound from './pages/NotFound'
import ConnectionIndicator from './components/ConnectionIndicator'
import { useRealtime } from './realtime/context'
import { useToasts } from './components/toasts/context'
import { shortId } from './utils/format'

const navLinks = [
  { to: '/', label: 'Pedidos', end: true },
  { to: '/orders/new', label: 'Novo pedido' },
  { to: '/ask', label: 'Pergunte' },
]

export default function App() {
  useStatusToasts()

  return (
    <div className="flex min-h-screen flex-col">
      <header className="sticky top-0 z-40 border-b border-slate-200 bg-white/90 backdrop-blur">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3 px-4 py-3">
          <NavLink to="/" className="flex items-center gap-2 font-semibold text-slate-900">
            <img src="/favicon.svg" alt="" className="size-7" />
            Gestão de Pedidos
          </NavLink>
          <nav className="flex items-center gap-1 text-sm" aria-label="Principal">
            {navLinks.map((link) => (
              <NavLink
                key={link.to}
                to={link.to}
                end={link.end}
                className={({ isActive }) =>
                  `rounded-lg px-3 py-1.5 font-medium ${
                    isActive ? 'bg-brand-50 text-brand-700' : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                  }`
                }
              >
                {link.label}
              </NavLink>
            ))}
          </nav>
          <ConnectionIndicator />
        </div>
      </header>

      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-8">
        <Routes>
          <Route path="/" element={<OrdersList />} />
          <Route path="/orders/new" element={<CreateOrder />} />
          <Route path="/orders/:id" element={<OrderDetails />} />
          <Route path="/ask" element={<AskOrders />} />
          <Route path="*" element={<NotFound />} />
        </Routes>
      </main>
    </div>
  )
}

/** Toast global a cada mudança de status recebida em tempo real. */
function useStatusToasts() {
  const { subscribe } = useRealtime()
  const { push } = useToasts()

  useEffect(
    () =>
      subscribe((event) => {
        if (event.status === 'Pendente') return
        push({
          title: `Pedido #${shortId(event.orderId)}`,
          description: `Status: ${event.status}`,
          tone: event.status === 'Finalizado' ? 'success' : 'info',
        })
      }),
    [subscribe, push],
  )
}
