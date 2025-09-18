import { useEffect, useMemo, useState } from 'react'
import { Card, CardContent, Stack, Tab, Tabs, Typography } from '@mui/material'
import { useAuth } from '../auth/AuthContext'
import ImageViewer from '../components/ImageViewer'

import GenerateTab from '../components/image-lab/GenerateTab'
import StylesTab from '../components/image-lab/StylesTab'
import GalleryTab from '../components/image-lab/GalleryTab'
import { useSecurity } from '../hooks/useSecurity'

export type Persona = { id: string; name: string }
export type Style    = { id: string; name: string; description?: string; promptPrefix?: string; negativePrompt?: string; isActive?: boolean }

export default function ImageLabPage() {
  const { auth } = useAuth()
  const accessToken = auth?.accessToken
  const primaryPersonaId = auth?.primaryPersonaId || ''

  const [tab, setTab] = useState(0)
  const security = useSecurity()

  // Personas
  const [personas, setPersonas] = useState<Persona[]>([])
  const [personaId, setPersonaId] = useState<string>('')

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
          const items: Persona[] = (js as any[]).map(p => ({ id: p.id ?? p.Id, name: p.name ?? p.Name }))
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
        const items: Persona[] = (js as any[]).map((p: any) => ({ id: p.id ?? p.Id, name: p.name ?? p.Name }))
        setPersonas(items)
        setPersonaId(primaryPersonaId || items[0]?.id || '')
      }
    }
    run()
  }, [accessToken, auth?.userId, primaryPersonaId])

  // (provider/model fetching removed; using default OpenAI DALL·E 3)

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

  // Provider/model no longer selected in UI; generation defaults to OpenAI + DALL·E 3

  return (
    <Stack spacing={2} sx={{ height: 'calc(100vh - 160px)' }}>
      <Typography variant="h5">Image Lab</Typography>
      <Card variant="outlined" sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
        <CardContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, height: '100%' }}>
          {/* Top-level dropdowns removed; controls moved inside Generate/Gallery tabs */}

          <Tabs value={tab} onChange={(_, v) => setTab(v)}>
            <Tab label="Generate" />
            {security.isAdmin && <Tab label="Styles" />}
            <Tab label="Gallery" />
          </Tabs>

          {tab === 0 && (
            <GenerateTab
              accessToken={accessToken || ''}
              personaId={primaryPersonaId}
              styles={styles}
              onOpenViewer={(id, title) => setViewer({ open: true, id, title })}
            />
          )}

          {security.isAdmin && tab === 1 && (
            <StylesTab
              accessToken={accessToken || ''}
              styles={styles}
              onUpdate={(updated) => setStyles(prev => prev.map(s => s.id === updated.id ? updated : s))}
            />
          )}

          {(security.isAdmin ? tab === 2 : tab === 1) && (
            <GalleryTab
              accessToken={accessToken || ''}
              personas={personas}
              personaId={personaId}
              onChangePersona={setPersonaId}
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





