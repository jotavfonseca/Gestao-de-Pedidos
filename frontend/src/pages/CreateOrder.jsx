import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { createOrder } from '../services/api'
import { useToasts } from '../components/toasts/context'
import { Card } from '../components/Feedback'

const MAX_LENGTH = 100

function validate({ cliente, produto, valor }) {
  const errors = {}
  if (!cliente.trim()) errors.cliente = 'Informe o cliente.'
  else if (cliente.trim().length > MAX_LENGTH) errors.cliente = `Máximo de ${MAX_LENGTH} caracteres.`
  if (!produto.trim()) errors.produto = 'Informe o produto.'
  else if (produto.trim().length > MAX_LENGTH) errors.produto = `Máximo de ${MAX_LENGTH} caracteres.`
  const number = Number(valor)
  if (valor === '' || !Number.isFinite(number)) errors.valor = 'Informe o valor.'
  else if (number <= 0) errors.valor = 'O valor deve ser maior que zero.'
  return errors
}

export default function CreateOrder() {
  const [form, setForm] = useState({ cliente: '', produto: '', valor: '' })
  const [errors, setErrors] = useState({})
  const [submitError, setSubmitError] = useState(null)
  const [submitting, setSubmitting] = useState(false)
  const navigate = useNavigate()
  const { push } = useToasts()

  const update = (field) => (event) => {
    setForm((current) => ({ ...current, [field]: event.target.value }))
    setErrors((current) => ({ ...current, [field]: undefined }))
  }

  async function submit(event) {
    event.preventDefault()
    setSubmitError(null)

    const clientErrors = validate(form)
    setErrors(clientErrors)
    if (Object.keys(clientErrors).length) return

    setSubmitting(true)
    try {
      const created = await createOrder({
        cliente: form.cliente.trim(),
        produto: form.produto.trim(),
        valor: Number(form.valor),
      })
      push({ title: 'Pedido criado', description: 'Acompanhe o processamento em tempo real.', tone: 'success' })
      navigate(`/orders/${created.id}`)
    } catch (e) {
      // Erros de validação do servidor (400) vêm por campo.
      const serverErrors = Object.fromEntries(
        Object.entries(e.fieldErrors ?? {}).map(([field, messages]) => [field, messages[0]]),
      )
      setErrors(serverErrors)
      if (!Object.keys(serverErrors).length) setSubmitError(e.message)
      setSubmitting(false)
    }
  }

  return (
    <div className="mx-auto max-w-xl space-y-6">
      <div>
        <Link to="/" className="text-sm font-medium text-brand-600 hover:text-brand-700">← Pedidos</Link>
        <h1 className="mt-2 text-2xl font-semibold tracking-tight text-slate-900">Novo pedido</h1>
        <p className="mt-1 text-sm text-slate-500">
          O pedido nasce <strong>Pendente</strong> e é processado de forma assíncrona pelo worker.
        </p>
      </div>

      <Card className="p-6">
        {submitError && (
          <p role="alert" className="mb-4 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{submitError}</p>
        )}
        <form onSubmit={submit} noValidate className="space-y-5">
          <Field id="cliente" label="Cliente" error={errors.cliente}>
            <input
              id="cliente"
              value={form.cliente}
              onChange={update('cliente')}
              maxLength={MAX_LENGTH}
              autoComplete="name"
              placeholder="Ex.: Ana Souza"
              aria-invalid={Boolean(errors.cliente)}
              aria-describedby={errors.cliente ? 'cliente-error' : undefined}
              className={inputClass(errors.cliente)}
            />
          </Field>
          <Field id="produto" label="Produto" error={errors.produto}>
            <input
              id="produto"
              value={form.produto}
              onChange={update('produto')}
              maxLength={MAX_LENGTH}
              placeholder="Ex.: Teclado mecânico"
              aria-invalid={Boolean(errors.produto)}
              aria-describedby={errors.produto ? 'produto-error' : undefined}
              className={inputClass(errors.produto)}
            />
          </Field>
          <Field id="valor" label="Valor (R$)" error={errors.valor}>
            <input
              id="valor"
              type="number"
              inputMode="decimal"
              min="0.01"
              step="0.01"
              value={form.valor}
              onChange={update('valor')}
              placeholder="0,00"
              aria-invalid={Boolean(errors.valor)}
              aria-describedby={errors.valor ? 'valor-error' : undefined}
              className={inputClass(errors.valor)}
            />
          </Field>

          <div className="flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:justify-end">
            <Link
              to="/"
              className="rounded-lg border border-slate-300 px-4 py-2 text-center text-sm font-medium text-slate-700 hover:bg-slate-50"
            >
              Cancelar
            </Link>
            <button
              type="submit"
              disabled={submitting}
              className="rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-brand-700 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {submitting ? 'Salvando…' : 'Criar pedido'}
            </button>
          </div>
        </form>
      </Card>
    </div>
  )
}

function Field({ id, label, error, children }) {
  return (
    <div>
      <label htmlFor={id} className="block text-sm font-medium text-slate-700">{label}</label>
      <div className="mt-1.5">{children}</div>
      {error && <p id={`${id}-error`} className="mt-1.5 text-sm text-rose-600">{error}</p>}
    </div>
  )
}

const inputClass = (error) =>
  `block w-full rounded-lg border px-3 py-2 text-slate-900 shadow-sm placeholder:text-slate-400 focus:outline-none focus:ring-2 ${
    error ? 'border-rose-300 focus:border-rose-500 focus:ring-rose-200' : 'border-slate-300 focus:border-brand-500 focus:ring-brand-100'
  }`
