import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from './useAuth.ts'

export function ProtectedRoute() {
  const { isAuthenticated, isInitializing } = useAuth()
  const location = useLocation()

  if (isInitializing) {
    return <RouteLoadingState />
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  return <Outlet />
}

function RouteLoadingState() {
  return (
    <main className="route-loading" aria-live="polite" aria-busy="true">
      <div className="brand-mark" aria-hidden="true">
        S
      </div>
      <p>Preparing SmartExpense…</p>
    </main>
  )
}
