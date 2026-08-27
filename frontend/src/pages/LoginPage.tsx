import { useState, type FormEvent } from 'react'
import {
  Link,
  useLocation,
  useNavigate,
  type Location,
} from 'react-router-dom'
import { getErrorMessages } from '../api/apiClient.ts'
import { useAuth } from '../auth/useAuth.ts'

interface LoginLocationState {
  from?: Location
  registrationSucceeded?: boolean
}

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const locationState = location.state as LoginLocationState | null
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [errors, setErrors] = useState<string[]>([])
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (isSubmitting) {
      return
    }

    const validationErrors = validateLogin(email, password)

    if (validationErrors.length > 0) {
      setErrors(validationErrors)
      return
    }

    setErrors([])
    setIsSubmitting(true)

    try {
      await login(email.trim(), password)
      navigate(locationState?.from?.pathname ?? '/', { replace: true })
    } catch (error) {
      setErrors(
        getErrorMessages(
          error,
          'Unable to sign in right now. Please try again.',
        ),
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="auth-page">
      <section className="auth-introduction" aria-labelledby="welcome-heading">
        <div className="auth-brand">
          <span className="brand-mark" aria-hidden="true">
            S
          </span>
          <span>SmartExpense</span>
        </div>
        <div>
          <p className="eyebrow">Clarity for your money</p>
          <h1 id="welcome-heading">Build better financial habits.</h1>
          <p>
            Keep your income, spending, categories, and monthly goals together
            in one calm workspace.
          </p>
        </div>
        <p className="auth-introduction__note">
          Simple tools. Useful insight. Your data stays yours.
        </p>
      </section>

      <section className="auth-panel" aria-labelledby="login-heading">
        <div className="auth-form-container">
          <p className="eyebrow">Welcome back</p>
          <h2 id="login-heading">Sign in to your account</h2>
          <p className="form-introduction">
            Enter your details to continue to SmartExpense.
          </p>

          {locationState?.registrationSucceeded && (
            <div className="status-message status-message--success" role="status">
              Your account is ready. Sign in to continue.
            </div>
          )}

          <ErrorSummary errors={errors} />

          <form className="auth-form" onSubmit={handleSubmit} noValidate>
            <div className="form-field">
              <label htmlFor="login-email">Email address</label>
              <input
                id="login-email"
                name="email"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                aria-describedby={errors.length > 0 ? 'login-errors' : undefined}
                disabled={isSubmitting}
                required
              />
            </div>

            <div className="form-field">
              <label htmlFor="login-password">Password</label>
              <input
                id="login-password"
                name="password"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                aria-describedby={errors.length > 0 ? 'login-errors' : undefined}
                disabled={isSubmitting}
                required
              />
            </div>

            <button
              className="button button--primary"
              type="submit"
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Signing in…' : 'Sign in'}
            </button>
          </form>

          <p className="auth-alternative">
            New to SmartExpense? <Link to="/register">Create an account</Link>
          </p>
        </div>
      </section>
    </main>
  )
}

function validateLogin(email: string, password: string): string[] {
  const errors: string[] = []

  if (!email.trim()) {
    errors.push('Email is required.')
  }

  if (!password) {
    errors.push('Password is required.')
  }

  return errors
}

function ErrorSummary({ errors }: { errors: string[] }) {
  if (errors.length === 0) {
    return null
  }

  return (
    <div className="status-message status-message--error" id="login-errors" role="alert">
      <ul>
        {errors.map((error) => (
          <li key={error}>{error}</li>
        ))}
      </ul>
    </div>
  )
}
