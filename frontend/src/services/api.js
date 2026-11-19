// Base relativa: o proxy (Vite em dev, nginx no Docker) encaminha /api para a API.
const BASE = '/api'

/** Erro de API com os dados do ProblemDetails retornado pelo backend. */
export class ApiError extends Error {
  constructor(status, problem) {
    super(problem?.detail || problem?.title || `Erro ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.title = problem?.title
    this.detail = problem?.detail
    this.fieldErrors = problem?.errors ?? {}
  }
}

async function request(path, options = {}) {
  let response
  try {
    response = await fetch(`${BASE}${path}`, {
      ...options,
      headers: { Accept: 'application/json', ...options.headers },
    })
  } catch {
    throw new ApiError(0, { title: 'Não foi possível conectar à API. Verifique se ela está em execução.' })
  }

  const isJson = response.headers.get('content-type')?.includes('json')
  const body = isJson ? await response.json() : null

  if (!response.ok) {
    throw new ApiError(response.status, body)
  }

  return body
}

export const fetchOrders = (signal) => request('/orders', { signal })

export const fetchOrder = (id, signal) => request(`/orders/${encodeURIComponent(id)}`, { signal })

export const createOrder = (order) =>
  request('/orders', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(order),
  })

export const fetchAskStatus = (signal) => request('/orders/ask/status', { signal })

export const askQuestion = (question) =>
  request('/orders/ask', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ question }),
  })
