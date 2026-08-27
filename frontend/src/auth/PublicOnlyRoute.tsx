import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './useAuth.ts'

export function PublicOnlyRoute() {
  const { isAuthenticated, isInitializing } = useAuth()

  if (isInitializing) {
    return (
      <main className="route-loading" aria-live="polite" aria-busy="true">
        <div className="brand-mark" aria-hidden="true">
          S
        </div>
        <p>Preparing SmartExpense…</p>
      </main>
    )
  }

  return isAuthenticated ? <Navigate to="/" replace /> : <Outlet />
}
