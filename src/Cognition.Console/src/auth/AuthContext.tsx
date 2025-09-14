import { createContext, useContext, useEffect, useMemo, useState } from 'react'
import { api, LoginResponse } from '../api/client'
import { jwtDecode } from 'jwt-decode'

type JwtPayload = { sub: string; unique_name?: string; primary_persona?: string; exp?: number }

export type AuthState = {
  userId: string
  username?: string
  primaryPersonaId?: string | null
  accessToken: string
  refreshToken: string
  expiresAt: string
}

type AuthContextValue = {
  auth: AuthState | null
  isAuthenticated: boolean
  login: (username: string, password: string) => Promise<void>
  logout: () => void
  refresh: () => Promise<void>
  setPrimaryPersona: (personaId: string) => Promise<void>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

const STORAGE_KEY = 'cognition.auth.v1'

function loadStored(): AuthState | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const parsed = JSON.parse(raw) as AuthState
    return parsed
  } catch {
    return null
  }
}

function saveStored(state: AuthState | null) {
  if (!state) localStorage.removeItem(STORAGE_KEY)
  else localStorage.setItem(STORAGE_KEY, JSON.stringify(state))
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [auth, setAuth] = useState<AuthState | null>(loadStored())

  const isAuthenticated = !!auth?.accessToken

  useEffect(() => {
    saveStored(auth)
  }, [auth])

  // Auto refresh if token is near expiry
  useEffect(() => {
    if (!auth) return
    const expMs = new Date(auth.expiresAt).getTime()
    const now = Date.now()
    const millisToRefresh = expMs - now - 60_000 // refresh 1 min before expiry
    const inMs = Math.max(5000, millisToRefresh)
    // If already expired or about to expire soon, refresh immediately
    if (millisToRefresh <= 0) {
      refresh().catch(() => logout())
      return
    }
    const t = setTimeout(() => { refresh().catch(() => logout()) }, inMs)
    return () => clearTimeout(t)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [auth?.accessToken, auth?.expiresAt])

  async function login(username: string, password: string) {
    const res = await api.login(username, password)
    const payload = jwtDecode<JwtPayload>(res.accessToken)
    setAuth({
      userId: payload.sub,
      username: payload.unique_name || res.username,
      primaryPersonaId: res.primaryPersonaId ?? payload.primary_persona ?? null,
      accessToken: res.accessToken,
      refreshToken: res.refreshToken,
      expiresAt: res.expiresAt
    })
  }

  function logout() {
    setAuth(null)
  }

  async function refresh() {
    if (!auth) return
    const res = await api.refresh(auth.refreshToken)
    const payload = jwtDecode<JwtPayload>(res.accessToken)
    setAuth({
      userId: payload.sub,
      username: payload.unique_name || auth.username,
      primaryPersonaId: payload.primary_persona ?? auth.primaryPersonaId ?? null,
      accessToken: res.accessToken,
      refreshToken: res.refreshToken,
      expiresAt: res.expiresAt
    })
  }

  async function setPrimaryPersona(personaId: string) {
    if (!auth) return
    await api.setPrimaryPersona(auth.userId, personaId, auth.accessToken)
    setAuth({ ...auth, primaryPersonaId: personaId })
  }

  const value = useMemo<AuthContextValue>(() => ({ auth, isAuthenticated, login, logout, refresh, setPrimaryPersona }), [auth])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
