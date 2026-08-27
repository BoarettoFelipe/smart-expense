import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { login as loginRequest } from '../api/authApi.ts'
import { setUnauthorizedHandler } from '../api/apiClient.ts'
import {
  clearStoredAuthSession,
  getStoredAuthSession,
  storeAuthSession,
  type AuthSession,
} from './authStorage.ts'
import {
  AuthContext,
  type AuthContextValue,
} from './authContextDefinition.ts'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(() =>
    getStoredAuthSession(),
  )
  const isInitializing = false

  const logout = useCallback(() => {
    clearStoredAuthSession()
    setSession(null)
  }, [])

  useEffect(() => {
    setUnauthorizedHandler(logout)
    return () => setUnauthorizedHandler(null)
  }, [logout])

  useEffect(() => {
    if (!session) {
      return undefined
    }

    const millisecondsUntilExpiration =
      Date.parse(session.expiresAt) - Date.now()

    const timeout = window.setTimeout(
      logout,
      Math.max(0, millisecondsUntilExpiration),
    )
    return () => window.clearTimeout(timeout)
  }, [logout, session])

  const login = useCallback(async (email: string, password: string) => {
    const response = await loginRequest({ email, password })
    const authenticatedSession: AuthSession = {
      accessToken: response.accessToken,
      expiresAt: response.expiresAt,
    }

    storeAuthSession(authenticatedSession)
    setSession(authenticatedSession)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      isAuthenticated: session !== null,
      isInitializing,
      expiresAt: session?.expiresAt ?? null,
      login,
      logout,
    }),
    [isInitializing, login, logout, session],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
