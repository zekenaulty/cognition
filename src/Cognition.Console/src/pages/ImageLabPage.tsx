import { useEffect, useMemo, useState } from 'react'
import { Card, CardContent, Stack, Tab, Tabs, Typography, FormControl, InputLabel, Select, MenuItem } from '@mui/material'
import { useAuth } from '../auth/AuthContext'
import ImageViewer from '../components/ImageViewer'

import GenerateTab from './image-lab/GenerateTab'
import StylesTab from './image-lab/StylesTab'
import GalleryTab from './image-lab/GalleryTab'

export type Persona = { id: string; name: string }
export type Provider = { id: string; name: string; displayName?: string }
export type Model    = { id: string; name: string; displayName?: string }
export type Style    = { id: string; name: string; description?: string; promptPrefix?: string; negativePrompt?: string; isActive?: boolean }

export default function ImageLabPage() {
  const { auth } = useAuth()
  const accessToken = auth?.accessToken
  const primaryPersonaId = auth?.primaryPersonaId || ''

  const [tab, setTab] = useState(0)

  // Personas
  const [personas, setPersonas] = useState<Persona[]>([])
  const [personaId, setPersonaId] = useState<string>('')

  // Providers / models
  const [providers, setProviders] = useState<Provider[]>([])
  const [providerId, setProviderId] = useState<string>('')
  const [models, setModels] = useState<Model[]>([])
  const [imageModel, setImageModel] = useState<string>('')

  // Styles (shared)
  const [styles, setStyles] = useState<Style[]>([])

  // Image viewer (shared)
  const [viewer, setViewer] = useState<{ open: boolean; id?: string; title?: string }>({ open: false })

  const authHeaders = useMemo(() => {
    const h: Record<string, string> = { 'Content-Type': 'application/json' }
    if (accessToken) h['Authorization'] = `Bearer ${accessToken}`
    return h
  }, [accessToken])

  const fetchJson = async <T,>(url: string, init?: RequestInit): Promise<T | null> => {
    const res = await fetch(url, init)
    if (!res.ok) return null
    try { return await res.json() as T } catch { return null }
  }

  // ---- load personas (user assistants first; fallback to global) ----
  useEffect(() => {
    const run = async () => {
      const headers = accessToken ? { Authorization: `Bearer ${accessToken}` } as any : undefined
      // try user-scoped
      if (auth?.userId) {
        const r = await fetch(`/api/users/${auth.userId}/personas`, { headers })
        if (r.ok) {
          const js = await r.json()
          const assistants = (js as any[]).filter(p => {
            const t = p.type ?? p.Type
            return typeof t === 'number' ? t === 1 : (typeof t === 'string' ? t.toLowerCase() === 'assistant' : false)
          })
          const items: Persona[] = assistants.map(p => ({ id: p.id ?? p.Id, name: p.name ?? p.Name }))
          if (items.length) {
            setPersonas(items)
            setPersonaId(primaryPersonaId || items[0].id)
            return
          }
        }
      }
      // fallback global
      const r2 = await fetch('/api/personas', { headers })
      if (r2.ok) {
        const js = await r2.json()
        const assistants = (js as any[]).filter((p: any) => {
          const t = p.type ?? p.Type ?? p.persona_type ?? p.PersonaType
          return typeof t === 'number' ? t === 1 : (typeof t === 'string' ? t.toLowerCase() === 'assistant' : false)
        })
        const items: Persona[] = assistants.map((p: any) => ({ id: p.id ?? p.Id, name: p.name ?? p.Name }))
        setPersonas(items)
        setPersonaId(primaryPersonaId || items[0]?.id || '')
      }
    }
    run()
  }, [accessToken, auth?.userId, primaryPersonaId])

  // ---- providers (flexible fallbacks + auth) ----
  useEffect(() => {
    const loadProvidersFlex = async () => {
      // try /api/llm/providers
      const a = await fetchJson<any[]>(`/api/llm/providers`, { headers: authHeaders })
      if (a && a.length) {
        const mapped: Provider[] = a.map(x => ({
          id: x.id ?? x.Id,
          name: x.name ?? x.Name,
          displayName: x.displayName ?? x.DisplayName
        }))
        setProviders(mapped)
        if (!providerId && mapped.length) setProviderId(mapped[0].id)
        return
      }
      // try /api/images/providers
      const b = await fetchJson<any[]>(`/api/images/providers`, { headers: authHeaders })
      if (b && b.length) {
        const mapped: Provider[] = b.map(x => ({
          id: x.id ?? x.Id ?? (x.name ?? x.Name),
          name: x.name ?? x.Name,
          displayName: x.displayName ?? x.DisplayName ?? (x.name ?? x.Name)
        }))
        setProviders(mapped)
        if (!providerId && mapped.length) setProviderId(mapped[0].id)
        return
      }
      // generic fallback
      const c = await fetchJson<any[]>(`/api/providers`, { headers: authHeaders })
      if (c && c.length) {
        const mapped: Provider[] = c.map(x => ({
          id: x.id ?? x.Id,
          name: x.name ?? x.Name,
          displayName: x.displayName ?? x.DisplayName
        }))
        setProviders(mapped)
        if (!providerId && mapped.length) setProviderId(mapped[0].id)
      }
    }
    loadProvidersFlex()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [accessToken])

  // ---- models (flexible fallbacks + auth) ----
  useEffect(() => {
    const loadModelsFlex = async () => {
      if (!providerId) { setModels([]); setImageModel(''); return }

      const providerName = providers.find(p => p.id === providerId)?.name || providerId

      // 1) /api/llm/providers/{id}/models
      const a = await fetchJson<any[]>(`/api/llm/providers/${encodeURIComponent(providerId)}/models`, { headers: authHeaders })
      if (a && a.length) {
        const mapped: Model[] = a.map(x => ({ id: x.id ?? x.Id, name: x.name ?? x.Name, displayName: x.displayName ?? x.DisplayName }))
        setModels(mapped)
        if (!imageModel && mapped.length) setImageModel(mapped[0].name)
        return
      }

      // 2) /api/llm/models?providerId=... OR ?provider=...
      const b = await fetchJson<any[]>(`/api/llm/models?providerId=${encodeURIComponent(providerId)}`, { headers: authHeaders })
      if (b && b.length) {
        const mapped: Model[] = b.map(x => ({ id: x.id ?? x.Id, name: x.name ?? x.Name, displayName: x.displayName ?? x.DisplayName }))
        setModels(mapped)
        if (!imageModel && mapped.length) setImageModel(mapped[0].name)
        return
      }
      const c = await fetchJson<any[]>(`/api/llm/models?provider=${encodeURIComponent(providerName)}`, { headers: authHeaders })
      if (c && c.length) {
        const mapped: Model[] = c.map(x => ({ id: x.id ?? x.Id, name: x.name ?? x.Name, displayName: x.displayName ?? x.DisplayName }))
        setModels(mapped)
        if (!imageModel && mapped.length) setImageModel(mapped[0].name)
        return
      }

      // 3) /api/images/models?providerId=... OR ?provider=...
      const d = await fetchJson<any[]>(`/api/images/models?providerId=${encodeURIComponent(providerId)}`, { headers: authHeaders })
      if (d && d.length) {
        const mapped: Model[] = d.map(x => ({ id: x.id ?? x.Id, name: x.name ?? x.Name, displayName: x.displayName ?? x.DisplayName }))
        setModels(mapped)
        if (!imageModel && mapped.length) setImageModel(mapped[0].name)
        return
      }
      const e = await fetchJson<any[]>(`/api/images/models?provider=${encodeURIComponent(providerName)}`, { headers: authHeaders })
      if (e && e.length) {
        const mapped: Model[] = e.map(x => ({ id: x.id ?? x.Id, name: x.name ?? x.Name, displayName: x.displayName ?? x.DisplayName }))
        setModels(mapped)
        if (!imageModel && mapped.length) setImageModel(mapped[0].name)
        return
      }

      // nothing found
      setModels([])
      setImageModel('')
    }
    loadModelsFlex()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [providerId, providers, accessToken])

  // ---- styles (add auth) ----
  useEffect(() => {
    const loadStyles = async () => {
      const js = await fetchJson<any[]>(`/api/image-styles`, { headers: authHeaders })
      if (!js) { setStyles([]); return }
      setStyles(js.map(s => ({
        id: s.id ?? s.Id,
        name: s.name ?? s.Name,
        description: s.description ?? s.Description,
        promptPrefix: s.promptPrefix ?? s.PromptPrefix,
        negativePrompt: s.negativePrompt ?? s.NegativePrompt,
        isActive: s.isActive ?? s.IsActive ?? true
      })))
    }
    loadStyles()
  }, [accessToken, authHeaders])

  const currentProviderName = useMemo(() => {
    const p = providers.find(x => x.id === providerId)
    return p?.displayName || p?.name || ''
  }, [providers, providerId])

  return (
    <Stack spacing={2} sx={{ height: 'calc(100vh - 160px)' }}>
      <Typography variant="h5">Image Lab</Typography>
      <Card variant="outlined" sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
        <CardContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, height: '100%' }}>
          {/* top bar: persona/provider/model shared across tabs */}
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <FormControl size="small" sx={{ minWidth: 200 }}>
              <InputLabel id="persona">Persona</InputLabel>
              <Select labelId="persona" label="Persona" value={personaId} onChange={e => setPersonaId(String(e.target.value))}>
                {personas.map(p => <MenuItem key={p.id} value={p.id}>{p.name}</MenuItem>)}
              </Select>
            </FormControl>

            <FormControl size="small" sx={{ minWidth: 200 }}>
              <InputLabel id="prov">Provider</InputLabel>
              <Select labelId="prov" label="Provider" value={providerId} onChange={e => setProviderId(String(e.target.value))}>
                {providers.map(p => <MenuItem key={p.id} value={p.id}>{p.displayName || p.name}</MenuItem>)}
              </Select>
            </FormControl>

            <FormControl size="small" sx={{ minWidth: 240 }}>
              <InputLabel id="mod">Model</InputLabel>
              <Select labelId="mod" label="Model" value={imageModel} onChange={e => setImageModel(String(e.target.value))}>
                {models.map(m => <MenuItem key={m.id} value={m.name}>{m.displayName || m.name}</MenuItem>)}
              </Select>
            </FormControl>
          </Stack>

          <Tabs value={tab} onChange={(_, v) => setTab(v)}>
            <Tab label="Generate" />
            <Tab label="Styles" />
            <Tab label="Gallery" />
          </Tabs>

          {tab === 0 && (
            <GenerateTab
              accessToken={accessToken || ''}
              personaId={personaId}
              providerId={providerId}
              providerName={currentProviderName}
              imageModel={imageModel}
              styles={styles}
              onOpenViewer={(id, title) => setViewer({ open: true, id, title })}
            />
          )}

          {tab === 1 && (
            <StylesTab
              accessToken={accessToken || ''}
              styles={styles}
              onUpdate={(updated) => setStyles(prev => prev.map(s => s.id === updated.id ? updated : s))}
            />
          )}

          {tab === 2 && (
            <GalleryTab
              accessToken={accessToken || ''}
              personaId={personaId}
              styles={styles}
              onOpenViewer={(id, title) => setViewer({ open: true, id, title })}
            />
          )}
        </CardContent>
      </Card>

      <ImageViewer open={viewer.open} onClose={() => setViewer({ open: false })} imageId={viewer.id} title={viewer.title} />
    </Stack>
  )
}
