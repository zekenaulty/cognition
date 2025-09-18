import { useCallback, useMemo } from 'react'
import { useAuth } from '../auth/AuthContext'

type SettingsBag = Record<string, any>

function getByPath(obj: any, path: string): any {
  if (!path) return obj
  return path.split('.').reduce((acc, key) => (acc && typeof acc === 'object') ? acc[key] : undefined, obj)
}

function setByPath(obj: any, path: string, value: any): any {
  const parts = path.split('.')
  const root = Array.isArray(obj) || typeof obj === 'object' ? { ...obj } : {}
  let cur: any = root
  for (let i = 0; i < parts.length - 1; i++) {
    const k = parts[i]
    const next = cur[k]
    cur[k] = (next && typeof next === 'object') ? { ...next } : {}
    cur = cur[k]
  }
  cur[parts[parts.length - 1]] = value
  return root
}

function deleteByPath(obj: any, path: string): any {
  const parts = path.split('.')
  const root = Array.isArray(obj) || typeof obj === 'object' ? { ...obj } : {}
  let cur: any = root
  for (let i = 0; i < parts.length - 1; i++) {
    const k = parts[i]
    const next = cur[k]
    cur[k] = (next && typeof next === 'object') ? { ...next } : {}
    cur = cur[k]
  }
  delete cur[parts[parts.length - 1]]
  return root
}

export function useUserSettings() {
  const { auth } = useAuth()
  const userId = auth?.userId || 'anon'
  const key = useMemo(() => `cognition.user.${userId}.settings`, [userId])

  const read = useCallback((): SettingsBag => {
    try {
      const raw = localStorage.getItem(key)
      if (!raw) return {}
      const parsed = JSON.parse(raw)
      return typeof parsed === 'object' && parsed ? parsed as SettingsBag : {}
    } catch { return {} }
  }, [key])

  const write = useCallback((bag: SettingsBag) => {
    try { localStorage.setItem(key, JSON.stringify(bag)) } catch {}
  }, [key])

  const get = useCallback(<T,>(path: string, fallback?: T): T | undefined => {
    const bag = read()
    const val = getByPath(bag, path)
    return (val === undefined ? fallback : val) as T | undefined
  }, [read])

  const set = useCallback((path: string, value: any) => {
    const bag = read()
    const next = setByPath(bag, path, value)
    write(next)
  }, [read, write])

  const remove = useCallback((path: string) => {
    const bag = read()
    const next = deleteByPath(bag, path)
    write(next)
  }, [read, write])

  const clearAll = useCallback(() => {
    try { localStorage.removeItem(key) } catch {}
  }, [key])

  return { get, set, remove, clearAll, read }
}

