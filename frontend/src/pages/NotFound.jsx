import { Link } from 'react-router-dom'
import { Card } from '../components/Feedback'

export default function NotFound() {
  return (
    <Card className="mx-auto max-w-lg p-10 text-center">
      <p className="text-sm font-semibold text-brand-600">404</p>
      <h1 className="mt-1 text-xl font-semibold text-slate-900">Page not found</h1>
      <Link to="/" className="mt-5 inline-block font-medium text-brand-600 hover:text-brand-700">← View orders</Link>
    </Card>
  )
}
