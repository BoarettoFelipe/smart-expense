const storageKey = 'smartexpense.auth'

export interface AuthSession {
  accessToken: string
  expiresAt: string
}

export function getStoredAuthSession(): AuthSession | null {
  try {
    const storedValue = window.localStorage.getItem(storageKey)

    if (!storedValue) {
      return null
    }

    const session = JSON.parse(storedValue) as unknown

    if (!isValidSession(session) || isExpired(session.expiresAt)) {
      clearStoredAuthSession()
      return null
    }

    return session
  } catch {
    clearStoredAuthSession()
    return null
  }
}

export function storeAuthSession(session: AuthSession): void {
  if (!isValidSession(session) || isExpired(session.expiresAt)) {
    throw new Error('Authentication session is invalid.')
  }

  window.localStorage.setItem(storageKey, JSON.stringify(session))
}

export function clearStoredAuthSession(): void {
  try {
    window.localStorage.removeItem(storageKey)
  } catch {
    // Storage may be unavailable; in-memory authentication is still cleared.
  }
}

function isValidSession(value: unknown): value is AuthSession {
  if (typeof value !== 'object' || value === null) {
    return false
  }

  const session = value as Partial<AuthSession>

  return (
    typeof session.accessToken === 'string' &&
    session.accessToken.length > 0 &&
    typeof session.expiresAt === 'string' &&
    Number.isFinite(Date.parse(session.expiresAt))
  )
}

function isExpired(expiresAt: string): boolean {
  return Date.parse(expiresAt) <= Date.now()
}
