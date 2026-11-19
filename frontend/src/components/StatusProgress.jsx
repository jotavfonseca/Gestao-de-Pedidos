import { STATUS_SEQUENCE } from './status'

/** Barra Pendente → Processando → Finalizado com a etapa atual destacada. */
export default function StatusProgress({ status }) {
  const current = STATUS_SEQUENCE.indexOf(status)

  return (
    <ol className="grid grid-cols-3 gap-2" aria-label="Progresso do pedido">
      {STATUS_SEQUENCE.map((step, index) => {
        const done = index < current || status === 'Finalizado'
        const active = index === current && status !== 'Finalizado'
        return (
          <li key={step} className="flex flex-col gap-2" aria-current={index === current ? 'step' : undefined}>
            <span
              className={`h-1.5 overflow-hidden rounded-full transition-colors duration-700 ${
                done ? 'bg-emerald-500' : active ? 'bg-sky-200' : 'bg-slate-200'
              }`}
            >
              {active && <span className="block h-full w-1/2 animate-pulse rounded-full bg-sky-500" />}
            </span>
            <span
              className={`text-xs font-medium ${done ? 'text-emerald-700' : active ? 'text-sky-700' : 'text-slate-400'}`}
            >
              {step}
            </span>
          </li>
        )
      })}
    </ol>
  )
}
