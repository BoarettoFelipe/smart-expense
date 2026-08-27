import type { ReactNode } from 'react'

interface EmptyStateProps {
  title: string
  description: string
  action?: ReactNode
  compact?: boolean
}

export function EmptyState({
  title,
  description,
  action,
  compact = false,
}: EmptyStateProps) {
  return (
    <section className={`empty-state${compact ? ' empty-state--compact' : ''}`}>
      <div className="empty-state__icon" aria-hidden="true">
        <svg viewBox="0 0 24 24">
          <path d="M5 4h14a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2Zm0 2v12h14V6H5Zm3 3h8v2H8V9Zm0 4h5v2H8v-2Z" />
        </svg>
      </div>
      <div>
        <h2>{title}</h2>
        <p>{description}</p>
      </div>
      {action && <div className="empty-state__action">{action}</div>}
    </section>
  )
}

interface ErrorStateProps {
  title?: string
  messages: string[]
  onRetry?: () => void
}

export function ErrorState({
  title = 'Something went wrong',
  messages,
  onRetry,
}: ErrorStateProps) {
  return (
    <section className="error-state" role="alert">
      <div>
        <h2>{title}</h2>
        {messages.map((message) => (
          <p key={message}>{message}</p>
        ))}
      </div>
      {onRetry && (
        <button className="button button--secondary" type="button" onClick={onRetry}>
          Try again
        </button>
      )}
    </section>
  )
}

export function FormErrors({ errors, id }: { errors: string[]; id: string }) {
  if (errors.length === 0) {
    return null
  }

  return (
    <div className="status-message status-message--error" id={id} role="alert">
      <ul>
        {errors.map((error) => (
          <li key={error}>{error}</li>
        ))}
      </ul>
    </div>
  )
}

export function SuccessFeedback({
  message,
  onDismiss,
}: {
  message: string | null
  onDismiss: () => void
}) {
  if (!message) {
    return null
  }

  return (
    <div className="success-feedback" role="status">
      <span>{message}</span>
      <button type="button" onClick={onDismiss} aria-label="Dismiss notification">
        ×
      </button>
    </div>
  )
}
