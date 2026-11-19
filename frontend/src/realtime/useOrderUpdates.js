import { useEffect, useRef } from 'react'
import { useRealtime } from './context'

export const POLLING_INTERVAL_MS = 5000

/**
 * Receives status changes in real time and, while SignalR is disconnected,
 * calls `onPoll` every 5 s (polling fallback).
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

  // On reconnect, sync once: events may have been missed.
  const wasLive = useRef(live)
  useEffect(() => {
    if (live && !wasLive.current) handlers.current.onPoll?.()
    wasLive.current = live
  }, [live])

  return { live, status }
}
