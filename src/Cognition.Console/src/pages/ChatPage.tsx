import { useEffect, useMemo, useRef, useState } from 'react'
import { Box, Button, Card, CardContent, Divider, Stack, TextField, Typography, FormControl, InputLabel, Select, MenuItem, Tooltip, IconButton, Chip } from '@mui/material'
import RefreshIcon from '@mui/icons-material/Refresh'
import { useAuth } from '../auth/AuthContext'
import MarkdownView from '../components/MarkdownView'
import EmojiButton from '../components/EmojiButton'

type Message = { role: 'system' | 'user' | 'assistant'; content: string; fromId?: string; fromName?: string; timestamp?: string; imageId?: string; pending?: boolean; localId?: string }
type Provider = { id: string; name: string; displayName?: string }
type Model = { id: string; name: string; displayName?: string }
type Persona = { id: string; name: string }
type ImageStyle = { id: string; name: string; description?: string; promptPrefix?: string; negativePrompt?: string }

export default function ChatPage() {
  const { auth } = useAuth()
  const accessToken = auth?.accessToken
  const userId = auth?.userId

  const [conversationId, setConversationId] = useState<string | null>(null)
  const [messages, setMessages] = useState<Message[]>([])
  const [input, setInput] = useState('')
  const inputRef = useRef<HTMLInputElement | null>(null)
  const creatingRef = useRef(false)
  const fetchSeqRef = useRef(0)

  const [providers, setProviders] = useState<Provider[]>([])
  const [models, setModels] = useState<Model[]>([])
  const [providerId, setProviderId] = useState<string>('')
  const [modelId, setModelId] = useState<string>('')

  const [personas, setPersonas] = useState<Persona[]>([])
  const [personaId, setPersonaId] = useState<string>('')

  type Conv = { id: string; title?: string | null }
  const [conversations, setConversations] = useState<Conv[]>([])

  // Image tools state
  const [imgStyles, setImgStyles] = useState<ImageStyle[]>([])
  const [imgStyleId, setImgStyleId] = useState<string>('')
  const [imgModel, setImgModel] = useState<string>('dall-e-3')
  const [imgMsgCount, setImgMsgCount] = useState<number>(6)
  const [imgPending, setImgPending] = useState<boolean>(false)
  const scrollRef = useRef<HTMLDivElement | null>(null)
  const stickToBottomRef = useRef(true)
  const forceScrollRef = useRef(false)

  const canSend = useMemo(
    () => !!accessToken && !!personaId && !!providerId && input.trim().length > 0,
    [accessToken, personaId, providerId, input]
  )

  async function loadConversations(pid?: string, autoSelectFirst: boolean = false) {
    if (!accessToken) return
    const url = pid ? `/api/conversations?participantId=${pid}` : `/api/conversations`
    const res = await fetch(url, { headers: { Authorization: `Bearer ${accessToken}` } })
    if (!res.ok) return
    const list = await res.json()
    const items: Conv[] = (list as any[]).map((c: any) => ({ id: c.id ?? c.Id, title: c.title ?? c.Title }))
    setConversations(items)
    if (autoSelectFirst) setConversationId(items.length > 0 ? items[0].id : null)
    return items
  }

  // Personas: show assistants only
  useEffect(() => {
    const loadPersonas = async () => {
      if (!accessToken || !userId) return
      try {
        const headers = { Authorization: `Bearer ${accessToken}` }
        const res = await fetch(`/api/users/${userId}/personas`, { headers })
        if (res.ok) {
          const list = await res.json()
          const assistants = (list as any[]).filter((p: any) => {
            const t = (p.type ?? p.Type)
            if (typeof t === 'number') return t === 1
            if (typeof t === 'string') return t.toLowerCase() === 'assistant'
            return false
          })
          const items: Persona[] = assistants.map((p: any) => ({ id: p.id ?? p.Id, name: p.name ?? p.Name }))
          setPersonas(items)
          if (!personaId) {
            if (items.length > 0) setPersonaId(items[0].id)
            else if (auth?.primaryPersonaId) setPersonaId(auth.primaryPersonaId)
          }
          if (items.length > 0) return
        }
      } catch {}
      // Fallback: global assistants
      try {
        const res = await fetch('/api/personas', { headers: accessToken ? { Authorization: `Bearer ${accessToken}` } as any : undefined })
        if (res.ok) {
          const list = await res.json()
          const assistants = (list as any[]).filter((p: any) => {
            const t = (p.type ?? p.Type ?? p.persona_type ?? p.PersonaType)
            if (typeof t === 'number') return t === 1
            if (typeof t === 'string') return t.toLowerCase() === 'assistant'
            return false
          })
          const items: Persona[] = assistants.map((p: any) => ({ id: p.id ?? p.Id, name: p.name ?? p.Name }))
          setPersonas(items)
          if (!personaId && items.length > 0) setPersonaId(items[0].id)
        }
      } catch {}
    }
    loadPersonas()
  }, [accessToken, userId, personaId, auth?.primaryPersonaId])

  // Providers/models
  useEffect(() => {
    const load = async () => {
      if (!accessToken) return
      const headers = { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` }
      const pRes = await fetch('/api/llm/providers', { headers })
      if (!pRes.ok) return
      const pList = await pRes.json()
      const normProviders: Provider[] = (pList as any[]).map((p: any) => ({ id: p.id ?? p.Id, name: p.name ?? p.Name, displayName: p.displayName ?? p.DisplayName }))
      setProviders(normProviders)
      let chosenProviderId = providerId
      if (!chosenProviderId && normProviders.length > 0) {
        const openai = normProviders.find(p => (p.name || '').toLowerCase() === 'openai')
        chosenProviderId = (openai ?? normProviders[0]).id
        setProviderId(chosenProviderId)
      }
      if (!chosenProviderId) return
      const mRes = await fetch(`/api/llm/providers/${chosenProviderId}/models`, { headers })
      if (!mRes.ok) return
      const mList = await mRes.json()
      const normModels: Model[] = (mList as any[]).map((m: any) => ({ id: m.id ?? m.Id, name: m.name ?? m.Name, displayName: m.displayName ?? m.DisplayName }))
      setModels(normModels)
      if (!modelId && normModels.length > 0) setModelId(normModels[0].id)
    }
    load()
  }, [accessToken, providerId])

  useEffect(() => {
    // On persona change: clear and select latest conversation for that persona
    setMessages([])
    setConversationId(null)
    setConversations([])
    if (!personaId) return
    loadConversations(personaId, true)
  }, [accessToken, personaId])

  // Image styles
  useEffect(() => {
    const loadStyles = async () => {
      try {
        const r = await fetch('/api/image-styles', { headers: accessToken ? { Authorization: `Bearer ${accessToken}` } as any : undefined })
        if (r.ok) {
          const list = await r.json()
          const items: ImageStyle[] = (list as any[]).map((s: any) => ({ id: s.id ?? s.Id, name: s.name ?? s.Name, description: s.description ?? s.Description, promptPrefix: s.promptPrefix ?? s.PromptPrefix, negativePrompt: s.negativePrompt ?? s.NegativePrompt }))
          setImgStyles(items)
          if (!imgStyleId && items.length > 0) setImgStyleId(items[0].id)
        }
      } catch {}
    }
    loadStyles()
  }, [accessToken])

  // Load messages (and images) for selected conversation
  useEffect(() => {
    const load = async () => {
      if (!accessToken || !conversationId) return
      const seq = ++fetchSeqRef.current
      // Clear immediately to avoid showing stale messages from previous selection
      setMessages([])
      // Ensure we scroll to bottom after this conversation loads
      forceScrollRef.current = true
      const headers = { Authorization: `Bearer ${accessToken}` }
      // Fetch chat messages and images in parallel
      const [resMsgs, resImgs] = await Promise.all([
        fetch(`/api/conversations/${conversationId}/messages`, { headers }),
        fetch(`/api/images/by-conversation/${conversationId}`, { headers })
      ])

      let baseMsgs: Message[] = []
      if (resMsgs.ok) {
        const list = await resMsgs.json()
        // Optionally gather labels if needed in future; we now derive names by role for clarity

        const normalizeRole = (r: any): 'system' | 'user' | 'assistant' => {
          if (r == null) return 'user'
          if (typeof r === 'string') {
            const t = r.toLowerCase()
            if (t === 'system' || t === 'user' || t === 'assistant') return t as any
            // sometimes numeric comes as string
            const n = Number(t)
            if (!Number.isNaN(n)) r = n
          }
          if (typeof r === 'number') {
            if (r === 0) return 'system'
            if (r === 2) return 'assistant'
            return 'user'
          }
          return 'user'
        }

        baseMsgs = (list as any[]).map((m: any) => {
          const role = normalizeRole(m.role ?? m.Role)
          const fromId = String(m.fromPersonaId ?? m.FromPersonaId ?? '')
          // Display name strictly by role to avoid mislabeling
          const fromName = role === 'user'
            ? 'You'
            : role === 'assistant'
            ? (personas.find(p => p.id === personaId)?.name || 'Assistant')
            : 'System'
          return {
            role,
            content: m.content ?? m.Content,
            fromId,
            fromName,
            timestamp: m.timestamp ?? m.Timestamp
          } as Message
        })
      }

      let imageMsgs: Message[] = []
      if (resImgs.ok) {
        const imgs = await resImgs.json()
        imageMsgs = (imgs as any[]).map((i: any) => ({
          role: 'assistant',
          content: '',
          imageId: String(i.id ?? i.Id),
          fromName: personas.find(p => p.id === personaId)?.name || 'Assistant',
          timestamp: i.createdAtUtc ?? i.CreatedAtUtc
        }))
      }

      const combined = [...baseMsgs, ...imageMsgs]
      combined.sort((a, b) => {
        const ta = a.timestamp ? Date.parse(a.timestamp) : 0
        const tb = b.timestamp ? Date.parse(b.timestamp) : 0
        return ta - tb
      })
      if (seq === fetchSeqRef.current) setMessages(combined)
    }
    load()
  }, [accessToken, conversationId, personaId, personas])

  const createConversation = () => {
    // Soft reset: don't persist yet. First send() will create and then auto-title.
    setMessages([])
    setConversationId(null)
  }

  const send = async () => {
    if (!canSend || !personaId) return
    // Lazily create a conversation on first send if needed
    let convId = conversationId
    if (!convId) {
      try {
        const res = await fetch('/api/conversations', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}) },
          body: JSON.stringify({ Title: null, ParticipantIds: personaId ? [personaId] : [] })
        })
        if (res.ok) {
          const body = await res.json()
          convId = body.id || body.Id
          setConversationId(convId)
        } else {
          const err = await res.text()
          setMessages(prev => [...prev, { role: 'assistant', content: `Error creating conversation: ${err}` }])
          return
        }
      } catch (e: any) {
        setMessages(prev => [...prev, { role: 'assistant', content: `Error creating conversation: ${String(e)}` }])
        return
      }
    }
    const text = input.trim()
    setMessages(prev => [...prev, { role: 'user', content: text, fromName: 'You' }])
    if (stickToBottomRef.current) {
      forceScrollRef.current = true
    }
    setInput('')
    try {
      const placeholderId = `pending-${Date.now()}-${Math.random().toString(36).slice(2)}`
      const assistantName = personas.find(p => p.id === personaId)?.name || 'Assistant'
      setMessages(prev => [...prev, { role: 'assistant', content: '', fromName: assistantName, pending: true, localId: placeholderId }])
      if (stickToBottomRef.current) forceScrollRef.current = true
      const res = await fetch('/api/chat/ask-chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}) },
        body: JSON.stringify({ ConversationId: convId, PersonaId: personaId, ProviderId: providerId, ModelId: modelId || null, Input: text, RolePlay: false })
      })
      if (!res.ok) {
        const err = await res.text()
        setMessages(prev => prev.map(m => m.localId === placeholderId ? { ...m, pending: false, content: `Error: ${err}` } : m))
        return
      }
      const body = await res.json()
      setMessages(prev => prev.map(m => m.localId === placeholderId ? { ...m, pending: false, content: body.reply } : m))
      if (stickToBottomRef.current) {
        forceScrollRef.current = true
      }
      await loadConversations(personaId)
    } catch (e: any) {
      setMessages(prev => prev.map(m => m.pending ? { ...m, pending: false, content: `Network error` } : m))
    }
  }

  const generateImageFromChat = async () => {
    if (!accessToken || !conversationId || !personaId) return
    const style = imgStyles.find(s => s.id === imgStyleId)
    const recent = messages.slice(-imgMsgCount)
    const lines = recent.map(m => `${m.fromName || m.role}: ${m.content}`).join('\n')
    const fullPrompt = `${style?.promptPrefix ? style.promptPrefix + '\n' : ''}${lines}`
    const payload = { ConversationId: conversationId, PersonaId: personaId, Prompt: fullPrompt, Width: 1024, Height: 1024, StyleId: imgStyleId || undefined, NegativePrompt: style?.negativePrompt || undefined, Provider: 'OpenAI', Model: imgModel }
    try {
      setImgPending(true)
      // Add placeholder assistant message for image generation
      const placeholderId = `img-${Date.now()}-${Math.random().toString(36).slice(2)}`
      const assistantName = personas.find(p => p.id === personaId)?.name || 'Assistant'
      setMessages(prev => [...prev, { role: 'assistant', content: 'Generating image', fromName: assistantName, pending: true, localId: placeholderId }])
      if (stickToBottomRef.current) forceScrollRef.current = true
      const res = await fetch('/api/images/generate', { method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` }, body: JSON.stringify(payload) })
      if (!res.ok) {
        const txt = await res.text()
        setMessages(prev => prev.map(m => m.localId === placeholderId ? { ...m, pending: false, content: `Image error: ${txt}` } : m))
        return
      }
      const data = await res.json()
      const id = data.id || data.Id
      setMessages(prev => prev.map(m => m.localId === placeholderId ? { ...m, pending: false, content: '', imageId: String(id) } : m))
      if (stickToBottomRef.current) {
        forceScrollRef.current = true
      }
    } catch (e: any) {
      setMessages(prev => prev.map(m => m.pending ? { ...m, pending: false, content: `Image error: ${String(e)}` } : m))
    } finally { setImgPending(false) }
  }

  // Auto-scroll to bottom when new messages arrive if user was already at bottom
  // Also scroll after a conversation is freshly loaded (forceScrollRef)
  useEffect(() => {
    const el = scrollRef.current
    if (!el) return
    if (forceScrollRef.current || stickToBottomRef.current) {
      el.scrollTop = el.scrollHeight
      forceScrollRef.current = false
    }
  }, [messages])

  return (
    <Stack spacing={2} sx={{ height: 'calc(100vh - 210px)' }}>
      <Typography variant="h5">Chat</Typography>
      <Card variant="outlined" sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
        <CardContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, height: '100%' }}>
          {/* lightweight animation for pending messages */}
          <style>{`
            .loading-dots span { display: inline-block; animation: blink 1.2s infinite; }
            .loading-dots span:nth-child(2) { animation-delay: 0.2s; }
            .loading-dots span:nth-child(3) { animation-delay: 0.4s; }
            @keyframes blink { 0% { opacity: 0.2 } 20% { opacity: 1 } 100% { opacity: 0.2 } }
          `}</style>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ sm: 'center' }}>
            <FormControl size="small" sx={{ minWidth: 200 }}>
              <InputLabel id="persona-label">Persona</InputLabel>
              <Select labelId="persona-label" label="Persona" value={personaId} onChange={e => setPersonaId(e.target.value)}>
                {personas.length === 0 ? (
                  <MenuItem value="" disabled>Loading...</MenuItem>
                ) : (
                  personas.map(p => <MenuItem key={p.id} value={p.id}>{p.name}</MenuItem>)
                )}
              </Select>
            </FormControl>
            <FormControl size="small" sx={{ minWidth: 260 }}>
              <InputLabel id="conversation-label">Conversation</InputLabel>
              <Select labelId="conversation-label" label="Conversation" value={conversationId || ''} onChange={e => { setConversationId(e.target.value); setMessages([]) }} renderValue={(v) => { const c = conversations.find(x => x.id === v); return c ? (c.title || (c.id.slice(0,8) + '...')) : ''; }}>
                {conversations.length === 0 && <MenuItem value="" disabled>No saved conversations</MenuItem>}
                {conversations.map(c => (
                  <MenuItem key={c.id} value={c.id}>
                    <Stack direction="row" spacing={1} alignItems="center">
                      <Chip size="small" label={c.id.slice(0,8)} />
                      <Typography variant="body2" color="text.secondary">{c.title || c.id}</Typography>
                    </Stack>
                  </MenuItem>
                ))}
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
                      if (res.ok) setProviderId('')
                    } catch {}
                  }}>
                    <RefreshIcon />
                  </IconButton>
                </span>
              </Tooltip>
            )}
          </Stack>

          {/* Image tools toolbar */}
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ sm: 'center' }}>
            <FormControl size="small" sx={{ minWidth: 160 }}>
              <InputLabel id="img-model-label">Image Model</InputLabel>
              <Select labelId="img-model-label" label="Image Model" value={imgModel} onChange={e => setImgModel(e.target.value)}>
                <MenuItem value="dall-e-3">DALLE-3</MenuItem>
                <MenuItem value="gpt-image-1">gpt-image-1</MenuItem>
              </Select>
            </FormControl>
            <FormControl size="small" sx={{ minWidth: 220 }}>
              <InputLabel id="img-style-label">Image Style</InputLabel>
              <Select labelId="img-style-label" label="Image Style" value={imgStyleId} onChange={e => setImgStyleId(e.target.value)}>
                {imgStyles.map(s => (
                  <MenuItem key={s.id} value={s.id}>{s.name}</MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField size="small" label="#Msgs" type="number" sx={{ width: 110 }} value={imgMsgCount} onChange={e => setImgMsgCount(Math.max(1, parseInt(e.target.value || '1') || 1))} />
            <Button variant="outlined" disabled={imgPending} onClick={generateImageFromChat}>{imgPending ? 'Generating.' : 'Generate Image'}</Button>
          </Stack>

          {/* Messages */}
          <Box
            ref={scrollRef}
            onScroll={(e) => {
              const el = e.currentTarget
              const threshold = 24
              const atBottom = (el.scrollHeight - el.scrollTop - el.clientHeight) <= threshold
              stickToBottomRef.current = atBottom
            }}
            sx={{ flex: 1, minHeight: 0, overflowY: 'auto', pr: 1 }}
          >
            {messages.length === 0 ? (
              <Typography color="text.secondary">Start the conversation.</Typography>
            ) : (
              <Stack spacing={1}>
                {messages.map((m, i) => (
                  <Box key={i} sx={{ p: 1, borderRadius: 1, bgcolor: m.role === 'user' ? 'action.selected' : 'background.paper' }}>
                    <Typography variant="caption" color="text.secondary">{m.fromName || m.role}</Typography>
                    {m.imageId ? (
                      <Box sx={{ mt: 0.5 }}>
                        <img alt="generated" src={`/api/images/content?id=${m.imageId}`} style={{ maxWidth: '100%', borderRadius: 6 }} />
                      </Box>
                    ) : m.pending ? (
                      <Typography variant="body1">
                        {m.content ? m.content + ' ' : ''}
                        <span className="loading-dots"><span>.</span><span>.</span><span>.</span></span>
                      </Typography>
                    ) : (
                      <MarkdownView content={m.content} />
                    )}
                  </Box>
                ))}
              </Stack>
            )}
          </Box>

          {/* Input */}
          <Divider />
          <Stack direction="row" spacing={1} alignItems="center">
            <TextField inputRef={inputRef} fullWidth size="small" placeholder="Type a message." value={input} onChange={(e) => setInput(e.target.value)} onKeyDown={(e) => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send() } }} />
            <EmojiButton
              onInsert={(text) => {
                const el = inputRef.current
                if (!el) { setInput(prev => prev + text); return }
                const start = el.selectionStart ?? el.value.length
                const end = el.selectionEnd ?? el.value.length
                const before = input.slice(0, start)
                const after = input.slice(end)
                const next = before + text + after
                setInput(next)
                // Restore caret after inserted text
                setTimeout(() => { try { el.focus(); const pos = start + text.length; el.setSelectionRange(pos, pos) } catch {} }, 0)
              }}
              onCloseFocus={() => { try { inputRef.current?.focus() } catch {} }}
            />
            <Button variant="contained" disabled={!canSend} onClick={send}>Send</Button>
          </Stack>
        </CardContent>
      </Card>
    </Stack>
  )
}
