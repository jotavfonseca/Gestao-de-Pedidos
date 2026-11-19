import { createContext, useContext } from 'react'

export const ToastContext = createContext({ push: () => {} })

/** `push({ title, description?, tone?: 'info' | 'success' | 'error' })` */
export const useToasts = () => useContext(ToastContext)
