import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/useAuth.ts'

const upcomingNavigation = ['Transactions', 'Categories', 'Budgets']

export function AppLayout() {
  const { logout } = useAuth()

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <NavLink className="app-brand" to="/" aria-label="SmartExpense home">
          <span className="brand-mark" aria-hidden="true">
            S
          </span>
          <span>
            <strong>SmartExpense</strong>
            <small>Personal finance</small>
          </span>
        </NavLink>

        <nav className="primary-navigation" aria-label="Primary navigation">
          <NavLink className="navigation-item" to="/" end>
            <DashboardIcon />
            <span>Dashboard</span>
          </NavLink>

          {upcomingNavigation.map((item) => (
            <span
              className="navigation-item navigation-item--disabled"
              aria-disabled="true"
              key={item}
            >
              <PlaceholderIcon />
              <span>{item}</span>
              <small>Soon</small>
            </span>
          ))}
        </nav>

        <div className="sidebar-footer">
          <p>Your finances, clearly organized.</p>
          <button
            className="button button--secondary logout-button"
            type="button"
            onClick={logout}
          >
            <LogoutIcon />
            Log out
          </button>
        </div>
      </aside>

      <div className="app-content">
        <header className="mobile-header">
          <NavLink className="app-brand" to="/" aria-label="SmartExpense home">
            <span className="brand-mark" aria-hidden="true">
              S
            </span>
            <strong>SmartExpense</strong>
          </NavLink>
          <button
            className="button button--secondary"
            type="button"
            onClick={logout}
          >
            Log out
          </button>
        </header>
        <Outlet />
      </div>
    </div>
  )
}

function DashboardIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M4 13h6V4H4v9Zm0 7h6v-4H4v4Zm10 0h6v-9h-6v9Zm0-16v4h6V4h-6Z" />
    </svg>
  )
}

function PlaceholderIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M5 5h14v14H5V5Zm2 2v10h10V7H7Z" />
    </svg>
  )
}

function LogoutIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M10 4H5v16h5v-2H7V6h3V4Zm5.6 3.6-1.4 1.4 2 2H9v2h7.2l-2 2 1.4 1.4L20 12l-4.4-4.4Z" />
    </svg>
  )
}
