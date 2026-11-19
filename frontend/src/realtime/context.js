import { createContext, useContext } from 'react'

/**
 * SignalR connection state:
 * - 'connecting'  : first attempt
 * - 'connected'   : real time active
 * - 'reconnecting': dropped and trying to come back (polling fallback active)
 * - 'disconnected': no connection (polling fallback active)
 */
export const RealtimeContext = createContext({
  status: 'disconnected',
  subscribe: () => () => {},
})

export const useRealtime = () => useContext(RealtimeContext)
