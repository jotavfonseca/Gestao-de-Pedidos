import { useEffect, useRef, useState } from 'react'
import { askQuestion, fetchAskStatus } from '../services/api'
import { Card } from '../components/Feedback'

const EXAMPLES = [
  'How many orders do we have today?',
  'What is the average time to approve an order?',
  'How many orders are pending?',
  'What is the total amount of completed orders this month?',
]

const TOOL_LABELS = {
  count_orders: 'Order count',
  sum_order_amounts: 'Amount totals',
  avg_processing_time: 'Processing time',
  orders_by_status: 'Orders by status',
  list_recent_orders: 'Recent orders',
}

export default function AskOrders() {
  const [enabled, setEnabled] = useState(null)
  const [question, setQuestion] = useState('')
  const [conversation, setConversation] = useState([])
  const [asking, setAsking] = useState(false)
  const endRef = useRef(null)

  useEffect(() => {
    const controller = new AbortController()
    fetchAskStatus(controller.signal)
      .then((status) => setEnabled(status.enabled))
      .catch(() => setEnabled(false))
    return () => controller.abort()
  }, [])

  useEffect(() => {
    endRef.current?.scrollIntoView({ behavior: 'smooth', block: 'end' })
  }, [conversation])

  async function ask(text) {
    const value = text.trim()
    if (!value || asking) return

    setQuestion('')
    setAsking(true)
    setConversation((current) => [...current, { role: 'user', text: value }])
    try {
      const result = await askQuestion(value)
      setConversation((current) => [...current, { role: 'assistant', text: result.answer, toolCalls: result.toolCalls }])
    } catch (e) {
      if (e.status === 503 && e.title?.toLowerCase().includes('disabled')) setEnabled(false)
      setConversation((current) => [...current, { role: 'error', text: e.message }])
    } finally {
      setAsking(false)
    }
  }

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Ask about your orders</h1>
        <p className="mt-1 text-sm text-slate-500">
          Ask questions in plain English. Claude queries the database through read-only tools and answers with real
          data.
        </p>
      </div>

      {enabled === false && (
        <div role="status" className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900">
          <p className="font-semibold">AI module disabled</p>
          <p className="mt-1">
            Set <code className="rounded bg-amber-100 px-1">ANTHROPIC_API_KEY</code> in the{' '}
            <code className="rounded bg-amber-100 px-1">.env</code> file and restart the API to enable it.
          </p>
        </div>
      )}

      <Card className="p-4 sm:p-6">
        {conversation.length === 0 ? (
          <div>
            <p className="text-sm font-medium text-slate-700">Try:</p>
            <div className="mt-3 flex flex-wrap gap-2">
              {EXAMPLES.map((example) => (
                <button
                  key={example}
                  type="button"
                  disabled={!enabled || asking}
                  onClick={() => ask(example)}
                  className="rounded-full border border-brand-100 bg-brand-50 px-3 py-1.5 text-sm text-brand-700 hover:bg-brand-100 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {example}
                </button>
              ))}
            </div>
          </div>
        ) : (
          <ol className="space-y-4" aria-live="polite">
            {conversation.map((message, index) => (
              <Message key={index} message={message} />
            ))}
            {asking && (
              <li className="flex items-center gap-2 text-sm text-slate-500">
                <span className="size-2 animate-pulse rounded-full bg-brand-500" aria-hidden="true" />
                Querying the orders…
              </li>
            )}
            <li ref={endRef} aria-hidden="true" />
          </ol>
        )}

        <form
          className="mt-6 flex flex-col gap-3 sm:flex-row"
          onSubmit={(event) => {
            event.preventDefault()
            ask(question)
          }}
        >
          <label htmlFor="question" className="sr-only">Question</label>
          <input
            id="question"
            value={question}
            onChange={(event) => setQuestion(event.target.value)}
            maxLength={500}
            disabled={!enabled}
            placeholder={enabled ? 'e.g. What is the average order value this week?' : 'AI module disabled'}
            className="flex-1 rounded-lg border border-slate-300 px-3 py-2 shadow-sm placeholder:text-slate-400 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-100 disabled:bg-slate-50"
          />
          <button
            type="submit"
            disabled={!enabled || asking || !question.trim()}
            className="rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-brand-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            Ask
          </button>
        </form>
      </Card>
    </div>
  )
}

function Message({ message }) {
  if (message.role === 'user') {
    return (
      <li className="flex justify-end">
        <p className="max-w-[85%] rounded-2xl rounded-br-sm bg-brand-600 px-4 py-2 text-sm text-white">{message.text}</p>
      </li>
    )
  }

  if (message.role === 'error') {
    return (
      <li role="alert" className="rounded-lg bg-rose-50 px-4 py-2 text-sm text-rose-700">{message.text}</li>
    )
  }

  const count = message.toolCalls?.length ?? 0
  return (
    <li className="max-w-[90%] space-y-2">
      <p className="whitespace-pre-line rounded-2xl rounded-bl-sm bg-slate-100 px-4 py-3 text-sm text-slate-800">
        {message.text}
      </p>
      {count > 0 && (
        <details className="text-xs text-slate-500">
          <summary className="cursor-pointer select-none">
            {count} {count > 1 ? 'queries' : 'query'} run
          </summary>
          <ul className="mt-2 space-y-1">
            {message.toolCalls.map((call, index) => (
              <li key={index} className="font-mono">
                {TOOL_LABELS[call.name] ?? call.name}
                {call.isError && ' (invalid parameters)'}: {JSON.stringify(call.input)}
              </li>
            ))}
          </ul>
        </details>
      )}
    </li>
  )
}
