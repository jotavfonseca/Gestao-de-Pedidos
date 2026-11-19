import { useEffect, useMemo, useRef, useState } from 'react'
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { RealtimeContext } from './context'

const HUB_URL = '/hubs/orders'
const RETRY_DELAY_MS = 5000

/**
 * Keeps a SignalR connection to /hubs/orders and forwards the
 * `orderStatusChanged` event to subscribers. SignalR negotiates the
 * transport (WebSocket → Server-Sent Events → long polling) on its own.
 */
export default function RealtimeProvider({ children }) {
  const [status, setStatus] = useState('connecting')
  const listeners = useRef(new Set())

  useEffect(() => {
    let disposed = false
    let retryTimer

    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL)
      // Never gives up: retries every 5 s while the page is open.
      .withAutomaticReconnect({ nextRetryDelayInMilliseconds: () => RETRY_DELAY_MS })
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('orderStatusChanged', (event) => {
      listeners.current.forEach((listener) => listener(event))
    })
    connection.onreconnecting(() => setStatus('reconnecting'))
    connection.onreconnected(() => setStatus('connected'))
    connection.onclose(() => {
      if (!disposed) {
        setStatus('disconnected')
        scheduleStart()
      }
    })

    // Automatic reconnect only covers drops after a successful connection;
    // the first connection is retried here until it succeeds.
    async function start() {
      if (disposed || connection.state !== HubConnectionState.Disconnected) return
      try {
        await connection.start()
        if (!disposed) setStatus('connected')
      } catch {
        if (!disposed) {
          setStatus('disconnected')
          scheduleStart()
        }
      }
    }

    function scheduleStart() {
      clearTimeout(retryTimer)
      retryTimer = setTimeout(start, RETRY_DELAY_MS)
    }

    start()

    return () => {
      disposed = true
      clearTimeout(retryTimer)
      connection.stop()
    }
  }, [])

  const value = useMemo(
    () => ({
      status,
      subscribe(listener) {
        listeners.current.add(listener)
        return () => listeners.current.delete(listener)
      },
    }),
    [status],
  )

  return <RealtimeContext.Provider value={value}>{children}</RealtimeContext.Provider>
}
