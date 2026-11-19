import { useEffect, useRef } from 'react'
import { useRealtime } from './context'

export const POLLING_INTERVAL_MS = 5000

/**
 * Recebe mudanças de status em tempo real e, enquanto o SignalR estiver
 * desconectado, chama `onPoll` a cada 5 s (fallback de polling).
 *
 * @param {(event: {orderId: string, status: string, changedAt: string}) => void} onEvent
 * @param {() => void} onPoll
 */
export function useOrderUpdates(onEvent, onPoll) {
  const { status, subscribe } = useRealtime()
  const handlers = useRef({ onEvent, onPoll })

  useEffect(() => {
    handlers.current = { onEvent, onPoll }
  })

  useEffect(() => subscribe((event) => handlers.current.onEvent?.(event)), [subscribe])

  const live = status === 'connected'

  useEffect(() => {
    if (live) return undefined
    const timer = setInterval(() => handlers.current.onPoll?.(), POLLING_INTERVAL_MS)
    return () => clearInterval(timer)
  }, [live])

  // Ao reconectar, sincroniza uma vez: eventos podem ter sido perdidos.
  const wasLive = useRef(live)
  useEffect(() => {
    if (live && !wasLive.current) handlers.current.onPoll?.()
    wasLive.current = live
  }, [live])

  return { live, status }
}
