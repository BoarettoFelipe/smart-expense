import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/useAuth.ts'

const navigation = [
  { label: 'Dashboard', to: '/', icon: 'dashboard', end: true },
  { label: 'Transactions', to: '/transactions', icon: 'transactions', end: false },
  { label: 'Categories', to: '/categories', icon: 'categories', end: false },
  { label: 'Budgets', to: '/budgets', icon: 'budgets', end: false },
] as const

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
          {navigation.map((item) => (
            <NavLink
              className={({ isActive }) =>
                `navigation-item${isActive ? ' navigation-item--active' : ''}`
              }
              to={item.to}
              end={item.end}
              key={item.to}
            >
              <NavigationIcon name={item.icon} />
              <span>{item.label}</span>
            </NavLink>
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

      <nav className="mobile-navigation" aria-label="Mobile navigation">
        {navigation.map((item) => (
          <NavLink
            className={({ isActive }) =>
              `mobile-navigation__item${isActive ? ' mobile-navigation__item--active' : ''}`
            }
            to={item.to}
            end={item.end}
            key={item.to}
          >
            <NavigationIcon name={item.icon} />
            <span>{item.label}</span>
          </NavLink>
        ))}
      </nav>
    </div>
  )
}

function NavigationIcon({ name }: { name: (typeof navigation)[number]['icon'] }) {
  const paths = {
    dashboard: 'M4 13h6V4H4v9Zm0 7h6v-4H4v4Zm10 0h6v-9h-6v9Zm0-16v4h6V4h-6Z',
    transactions: 'M5 4h14v3H5V4Zm0 5h14v11H5V9Zm3 3v2h4v-2H8Zm0 4v2h8v-2H8Z',
    categories: 'M4 5h7v7H4V5Zm9 0h7v7h-7V5ZM4 14h7v6H4v-6Zm9 0h7v6h-7v-6Z',
    budgets: 'M4 6h16v14H4V6Zm2 3v9h12V9H6Zm5-7h2v3h-2V2Zm-3 9h8v2H8v-2Zm0 4h5v2H8v-2Z',
  } as const

  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d={paths[name]} />
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
