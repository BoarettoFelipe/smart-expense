import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { register } from '../api/authApi.ts'
import { getErrorMessages } from '../api/apiClient.ts'

const basicEmailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function RegisterPage() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [errors, setErrors] = useState<string[]>([])
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (isSubmitting) {
      return
    }

    const validationErrors = validateRegistration(
      email,
      password,
      confirmPassword,
    )

    if (validationErrors.length > 0) {
      setErrors(validationErrors)
      return
    }

    setErrors([])
    setIsSubmitting(true)

    try {
      await register({ email: email.trim(), password })
      navigate('/login', {
        replace: true,
        state: { registrationSucceeded: true },
      })
    } catch (error) {
      setErrors(
        getErrorMessages(
          error,
          'Unable to create your account right now. Please try again.',
        ),
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="auth-page">
      <section className="auth-introduction" aria-labelledby="register-welcome-heading">
        <div className="auth-brand">
          <span className="brand-mark" aria-hidden="true">
            S
          </span>
          <span>SmartExpense</span>
        </div>
        <div>
          <p className="eyebrow">Start with a clear picture</p>
          <h1 id="register-welcome-heading">Make every month more intentional.</h1>
          <p>
            Create your private workspace and begin organizing your financial
            life with confidence.
          </p>
        </div>
        <p className="auth-introduction__note">
          One account for transactions, categories, budgets, and insights.
        </p>
      </section>

      <section className="auth-panel" aria-labelledby="register-heading">
        <div className="auth-form-container">
          <p className="eyebrow">Create your account</p>
          <h2 id="register-heading">Get started with SmartExpense</h2>
          <p className="form-introduction">
            Use an email address and a secure password to begin.
          </p>

          <ErrorSummary errors={errors} />

          <form className="auth-form" onSubmit={handleSubmit} noValidate>
            <div className="form-field">
              <label htmlFor="register-email">Email address</label>
              <input
                id="register-email"
                name="email"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                aria-describedby={errors.length > 0 ? 'register-errors' : undefined}
                disabled={isSubmitting}
                required
              />
            </div>

            <div className="form-field">
              <label htmlFor="register-password">Password</label>
              <input
                id="register-password"
                name="password"
                type="password"
                autoComplete="new-password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                aria-describedby={
                  errors.length > 0
                    ? 'password-guidance register-errors'
                    : 'password-guidance'
                }
                disabled={isSubmitting}
                required
              />
              <small id="password-guidance">
                Password requirements will be checked securely when you submit.
              </small>
            </div>

            <div className="form-field">
              <label htmlFor="confirm-password">Confirm password</label>
              <input
                id="confirm-password"
                name="confirmPassword"
                type="password"
                autoComplete="new-password"
                value={confirmPassword}
                onChange={(event) => setConfirmPassword(event.target.value)}
                aria-describedby={errors.length > 0 ? 'register-errors' : undefined}
                disabled={isSubmitting}
                required
              />
            </div>

            <button
              className="button button--primary"
              type="submit"
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Creating account…' : 'Create account'}
            </button>
          </form>

          <p className="auth-alternative">
            Already have an account? <Link to="/login">Sign in</Link>
          </p>
        </div>
      </section>
    </main>
  )
}

function validateRegistration(
  email: string,
  password: string,
  confirmPassword: string,
): string[] {
  const errors: string[] = []

  if (!email.trim()) {
    errors.push('Email is required.')
  } else if (!basicEmailPattern.test(email.trim())) {
    errors.push('Enter a valid email address.')
  }

  if (!password) {
    errors.push('Password is required.')
  }

  if (!confirmPassword) {
    errors.push('Password confirmation is required.')
  } else if (password !== confirmPassword) {
    errors.push('Passwords do not match.')
  }

  return errors
}

function ErrorSummary({ errors }: { errors: string[] }) {
  if (errors.length === 0) {
    return null
  }

  return (
    <div
      className="status-message status-message--error"
      id="register-errors"
      role="alert"
    >
      <ul>
        {errors.map((error) => (
          <li key={error}>{error}</li>
        ))}
      </ul>
    </div>
  )
}
