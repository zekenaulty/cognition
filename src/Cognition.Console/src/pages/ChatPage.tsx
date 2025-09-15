import { useEffect, useMemo, useRef, useState } from 'react'
import { Box, Button, Card, CardContent, Divider, Stack, TextField, Typography, FormControl, InputLabel, Select, MenuItem, Tooltip, IconButton, Chip } from '@mui/material'
import RefreshIcon from '@mui/icons-material/Refresh'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'

type Message = { role: 'system' | 'user' | 'assistant'; content: string }
type Provider = { id: string; name: string; displayName?: string }
type Model = { id: string; name: string; displayName?: string }
type Persona = { id: string; name: string }

const LS_CONVOS_KEY = 'cognition.chat.conversations'

export default function ChatPage() {
  const { auth } = useAuth()
  const accessToken = auth?.accessToken
  const userId = auth?.userId
  const [conversationId, setConversationId] = useState<string | null>(null)
  const [messages, setMessages] = useState<Message[]>([])
  const [input, setInput] = useState('')
  const creatingRef = useRef(false)
  const [providers, setProviders] = useState<Array<{ id: string; name: string; displayName?: string }>>([])
  const [models, setModels] = useState<Array<{ id: string; name: string; displayName?: string }>>([])
  const [providerId, setProviderId] = useState<string>('')
  const [modelId, setModelId] = useState<string>('')
  const [personas, setPersonas] = useState<Persona[]>([])
  const [personaId, setPersonaId] = useState<string>('')
  const [conversations, setConversations] = useState<string[]>([])

  const canSend = useMemo(
    () => !!accessToken && !!personaId && !!conversationId && !!providerId && input.trim().length > 0,
    [accessToken, personaId, conversationId, providerId, input]
  )

  useEffect(() => {
    // Load saved conversations from localStorage
    try {
      const raw = localStorage.getItem(LS_CONVOS_KEY)
      if (raw) {
        const list = JSON.parse(raw) as string[]
        setConversations(Array.from(new Set(list)))
      }
    } catch {}
  }, [])

  useEffect(() => {
    // Load user's accessible personas; show Assistants (type = 1 or 'Assistant') only.
    const loadPersonas = async () => {
      if (!accessToken || !userId) return
      try {
        const headers = { Authorization: `Bearer ${accessToken}` }
        const res = await fetch(`/api/users/${userId}/personas`, { headers })
        if (res.ok) {
          const list = await res.json()
          const assistants = (list as any[]).filter(p => {
            const t = (p.type ?? p.Type)
            if (typeof t === 'number') return t === 1
            if (typeof t === 'string') return t.toLowerCase() === 'assistant'
            return false
          })
          const items: Persona[] = assistants.map(p => ({ id: p.id ?? p.Id, name: p.name ?? p.Name }))
          setPersonas(items)
          if (!personaId) {
            if (items.length > 0) setPersonaId(items[0].id)
            else if (auth?.primaryPersonaId) setPersonaId(auth.primaryPersonaId)
          }
          if (items.length > 0) return
        }
      } catch {}
      // Fallback: load global assistant personas from /api/personas (unfiltered) and filter client-side
      try {
        const res3 = await fetch('/api/personas', { headers: accessToken ? { Authorization: `Bearer ${accessToken}` } as any : undefined })
        if (res3.ok) {
          const list = await res3.json()
          const assistants = (list as any[]).filter(p => {
            const t = (p.type ?? p.Type ?? p.persona_type ?? p.PersonaType)
            if (typeof t === 'number') return t === 1
            if (typeof t === 'string') return t.toLowerCase() === 'assistant'
            return false
          })
          const items: Persona[] = assistants.map(p => ({ id: p.id ?? p.Id, name: p.name ?? p.Name }))
          setPersonas(items)
          if (!personaId && items.length > 0) setPersonaId(items[0].id)
          return
        }
      } catch {}
      // Last fallback: use primary persona only
      const primary = auth?.primaryPersonaId
      if (primary) setPersonaId(primary)
    }
    loadPersonas()
  }, [accessToken, userId, personaId, auth?.primaryPersonaId])

  const createConversation = async () => {
    if (creatingRef.current) return
    creatingRef.current = true
    try {
      const res = await fetch('/api/conversations', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}) },
        body: JSON.stringify({ Title: null, ParticipantIds: personaId ? [personaId] : [] })
      })
      if (res.ok) {
        const body = await res.json()
        const id = body.id || body.Id
        setConversationId(id)
        const next = Array.from(new Set([id, ...conversations]))
        setConversations(next)
        try { localStorage.setItem(LS_CONVOS_KEY, JSON.stringify(next)) } catch {}
      }
    } finally {
      creatingRef.current = false
    }
  }

  useEffect(() => {
    // Load providers then models for selected provider
    const load = async () => {
      if (!accessToken) return
      const headers = { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` }
      // Providers
      const pRes = await fetch('/api/llm/providers', { headers })
      if (!pRes.ok) return
      const pList = await pRes.json()
      const normProviders = (pList as any[]).map(p => ({ id: p.id ?? p.Id, name: p.name ?? p.Name, displayName: p.displayName ?? p.DisplayName }))
      setProviders(normProviders)
      let chosenProviderId = providerId
      if (!chosenProviderId) {
        const openai = normProviders.find(p => (p.name || '').toLowerCase() === 'openai')
        chosenProviderId = (openai ?? normProviders[0])?.id || ''
        setProviderId(chosenProviderId)
      }
      if (!chosenProviderId) return
      // Models for provider
      const mRes = await fetch(`/api/llm/providers/${chosenProviderId}/models`, { headers })
      if (!mRes.ok) return
      const mList = await mRes.json()
      const normModels = (mList as any[]).map(m => ({ id: m.id ?? m.Id, name: m.name ?? m.Name, displayName: m.displayName ?? m.DisplayName }))
      setModels(normModels)
      if (!modelId && normModels.length > 0) setModelId(normModels[0].id)
    }
    load()
  }, [accessToken, providerId])

  const send = async () => {
    if (!canSend || !conversationId || !personaId) return
    const text = input.trim()
    setMessages((prev) => [...prev, { role: 'user', content: text }])
    setInput('')
    try {
      const res = await fetch('/api/chat/ask-chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}) },
        body: JSON.stringify({ ConversationId: conversationId, PersonaId: personaId, ProviderId: providerId, ModelId: modelId || null, Input: text, RolePlay: false })
      })
      if (!res.ok) {
        const err = await res.text()
        setMessages((prev) => [...prev, { role: 'assistant', content: `Error: ${err}` }])
        return
      }
      const body = await res.json()
      setMessages((prev) => [...prev, { role: 'assistant', content: body.reply }])
    } catch (e: any) {
      setMessages((prev) => [...prev, { role: 'assistant', content: `Network error` }])
    }
  }

  return (
    <Stack spacing={2}>
      <Typography variant="h5">Chat</Typography>
      <Card variant="outlined">
        <CardContent>
          <Stack spacing={2}>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ sm: 'center' }}>
              <FormControl size="small" sx={{ minWidth: 200 }}>
                <InputLabel id="persona-label">Persona</InputLabel>
                <Select labelId="persona-label" label="Persona" value={personaId} onChange={e => setPersonaId(e.target.value)}>
                  {personas.length === 0 ? (
                    <MenuItem value="" disabled>Loading…</MenuItem>
                  ) : (
                    personas.map(p => <MenuItem key={p.id} value={p.id}>{p.name}</MenuItem>)
                  )}
                </Select>
              </FormControl>
              <FormControl size="small" sx={{ minWidth: 260 }}>
                <InputLabel id="conversation-label">Conversation</InputLabel>
                <Select labelId="conversation-label" label="Conversation" value={conversationId || ''} onChange={e => setConversationId(e.target.value)} renderValue={(v) => v ? String(v).slice(0,8)+'…' : ''}>
                  {conversations.length === 0 && <MenuItem value="" disabled>No saved conversations</MenuItem>}
                  {conversations.map(id => <MenuItem key={id} value={id}><Stack direction="row" spacing={1} alignItems="center"><Chip size="small" label={id.slice(0,8)} /> <Typography variant="body2" color="text.secondary">{id}</Typography></Stack></MenuItem>)}
                </Select>
              </FormControl>
              <Button variant="outlined" onClick={createConversation}>New Conversation</Button>
              <FormControl size="small" sx={{ minWidth: 180 }}>
                <InputLabel id="provider-label">Provider</InputLabel>
                <Select labelId="provider-label" label="Provider" value={providerId} onChange={e => setProviderId(e.target.value)}>
                  {providers.length === 0 ? (
                    <MenuItem value="" disabled>No providers</MenuItem>
                  ) : (
                    providers.map(p => (
                      <MenuItem key={p.id} value={p.id}>{p.displayName || p.name}</MenuItem>
                    ))
                  )}
                </Select>
              </FormControl>
              <FormControl size="small" sx={{ minWidth: 200 }}>
                <InputLabel id="model-label">Model</InputLabel>
                <Select labelId="model-label" label="Model" value={modelId} onChange={e => setModelId(e.target.value)}>
                  {models.length === 0 ? (
                    <MenuItem value="">Default</MenuItem>
                  ) : (
                    models.map(m => (
                      <MenuItem key={m.id} value={m.id}>{m.displayName || m.name}</MenuItem>
                    ))
                  )}
                </Select>
              </FormControl>
              {providers.length === 0 && (
                <Tooltip title="Try to sync providers (admin only)">
                  <span>
                    <IconButton onClick={async () => {
                      try {
                        const res = await fetch('/api/admin/sync-models', { method: 'POST', headers: { 'Content-Type': 'application/json', ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}) } })
                        if (res.ok) {
                          // re-trigger providers load
                          setProviderId('')
                        }
                      } catch {}
                    }}>
                      <RefreshIcon />
                    </IconButton>
                  </span>
                </Tooltip>
              )}
            </Stack>
            <Box sx={{ minHeight: 240 }}>
              {messages.length === 0 ? (
                <Typography color="text.secondary">Start the conversation…</Typography>
              ) : (
                <Stack spacing={1}>
                  {messages.map((m, i) => (
                    <Box key={i} sx={{ p: 1, borderRadius: 1, bgcolor: m.role === 'user' ? 'action.selected' : 'background.paper' }}>
                      <Typography variant="caption" color="text.secondary">{m.role}</Typography>
                      <Typography variant="body1" whiteSpace="pre-wrap">{m.content}</Typography>
                    </Box>
                  ))}
                </Stack>
              )}
            </Box>
            <Divider />
            <Stack direction="row" spacing={1}>
              <TextField fullWidth size="small" placeholder="Type a message…" value={input} onChange={(e) => setInput(e.target.value)} onKeyDown={(e) => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send() } }} />
              <Button variant="contained" disabled={!canSend} onClick={send}>Send</Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </Stack>
  )
}
