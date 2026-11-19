import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import './index.css'
import App from './App'
import RealtimeProvider from './realtime/RealtimeProvider'
import ToastProvider from './components/toasts/ToastProvider'

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <BrowserRouter>
      <ToastProvider>
        <RealtimeProvider>
          <App />
        </RealtimeProvider>
      </ToastProvider>
    </BrowserRouter>
  </StrictMode>,
)
