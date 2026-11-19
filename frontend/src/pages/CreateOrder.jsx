import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { createOrder } from '../services/api'
import { useToasts } from '../components/toasts/context'
import { Card } from '../components/Feedback'

const MAX_LENGTH = 100

function validate({ customer, product, amount }) {
  const errors = {}
  if (!customer.trim()) errors.customer = 'Customer is required.'
  else if (customer.trim().length > MAX_LENGTH) errors.customer = `At most ${MAX_LENGTH} characters.`
  if (!product.trim()) errors.product = 'Product is required.'
  else if (product.trim().length > MAX_LENGTH) errors.product = `At most ${MAX_LENGTH} characters.`
  const number = Number(amount)
  if (amount === '' || !Number.isFinite(number)) errors.amount = 'Amount is required.'
  else if (number <= 0) errors.amount = 'Amount must be greater than zero.'
  return errors
}

export default function CreateOrder() {
  const [form, setForm] = useState({ customer: '', product: '', amount: '' })
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
        customer: form.customer.trim(),
        product: form.product.trim(),
        amount: Number(form.amount),
      })
      push({ title: 'Order created', description: 'Follow its processing in real time.', tone: 'success' })
      navigate(`/orders/${created.id}`)
    } catch (e) {
      // Server validation errors (400) come per field.
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
        <Link to="/" className="text-sm font-medium text-brand-600 hover:text-brand-700">← Orders</Link>
        <h1 className="mt-2 text-2xl font-semibold tracking-tight text-slate-900">New order</h1>
        <p className="mt-1 text-sm text-slate-500">
          The order starts as <strong>Pending</strong> and is processed asynchronously by the worker.
        </p>
      </div>

      <Card className="p-6">
        {submitError && (
          <p role="alert" className="mb-4 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{submitError}</p>
        )}
        <form onSubmit={submit} noValidate className="space-y-5">
          <Field id="customer" label="Customer" error={errors.customer}>
            <input
              id="customer"
              value={form.customer}
              onChange={update('customer')}
              maxLength={MAX_LENGTH}
              autoComplete="name"
              placeholder="e.g. Jane Doe"
              aria-invalid={Boolean(errors.customer)}
              aria-describedby={errors.customer ? 'customer-error' : undefined}
              className={inputClass(errors.customer)}
            />
          </Field>
          <Field id="product" label="Product" error={errors.product}>
            <input
              id="product"
              value={form.product}
              onChange={update('product')}
              maxLength={MAX_LENGTH}
              placeholder="e.g. Mechanical keyboard"
              aria-invalid={Boolean(errors.product)}
              aria-describedby={errors.product ? 'product-error' : undefined}
              className={inputClass(errors.product)}
            />
          </Field>
          <Field id="amount" label="Amount (USD)" error={errors.amount}>
            <input
              id="amount"
              type="number"
              inputMode="decimal"
              min="0.01"
              step="0.01"
              value={form.amount}
              onChange={update('amount')}
              placeholder="0.00"
              aria-invalid={Boolean(errors.amount)}
              aria-describedby={errors.amount ? 'amount-error' : undefined}
              className={inputClass(errors.amount)}
            />
          </Field>

          <div className="flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:justify-end">
            <Link
              to="/"
              className="rounded-lg border border-slate-300 px-4 py-2 text-center text-sm font-medium text-slate-700 hover:bg-slate-50"
            >
              Cancel
            </Link>
            <button
              type="submit"
              disabled={submitting}
              className="rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-brand-700 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {submitting ? 'Saving…' : 'Create order'}
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
