import { createContext, useContext } from 'react'

/**
 * Estado da conexão SignalR:
 * - 'connecting'  : primeira tentativa
 * - 'connected'   : tempo real ativo
 * - 'reconnecting': caiu e está tentando voltar (fallback de polling ativo)
 * - 'disconnected': sem conexão (fallback de polling ativo)
 */
export const RealtimeContext = createContext({
  status: 'disconnected',
  subscribe: () => () => {},
})

export const useRealtime = () => useContext(RealtimeContext)
