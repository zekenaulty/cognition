import { useEffect, useMemo, useRef, useState } from 'react'
import React from 'react'
// FeedbackBar component for emoji rating
// ...existing code...
import HelpOutlineIcon from '@mui/icons-material/HelpOutline'
import SentimentSatisfiedAltIcon from '@mui/icons-material/SentimentSatisfiedAlt'
import SentimentVerySatisfiedIcon from '@mui/icons-material/SentimentVerySatisfied'
import SentimentSatisfiedIcon from '@mui/icons-material/SentimentSatisfied'
import FavoriteIcon from '@mui/icons-material/Favorite'
import FavoriteBorderIcon from '@mui/icons-material/FavoriteBorder'
import EmojiObjectsIcon from '@mui/icons-material/EmojiObjects'
import VisibilityIcon from '@mui/icons-material/Visibility'
import WhatshotIcon from '@mui/icons-material/Whatshot'
import EmojiEmotionsIcon from '@mui/icons-material/EmojiEmotions'
import SentimentVeryDissatisfiedIcon from '@mui/icons-material/SentimentVeryDissatisfied'
import SentimentDissatisfiedIcon from '@mui/icons-material/SentimentDissatisfied'

const FEEDBACK_ICONS = [
  { key: 'satisfied', icon: <SentimentSatisfiedAltIcon fontSize="medium" />, label: 'Satisfied' },
  { key: 'very_satisfied', icon: <SentimentVerySatisfiedIcon fontSize="medium" />, label: 'Very Satisfied' },
  { key: 'neutral', icon: <SentimentSatisfiedIcon fontSize="medium" />, label: 'Neutral' },
  { key: 'love', icon: <FavoriteIcon fontSize="medium" />, label: 'Love' },
  { key: 'like', icon: <FavoriteBorderIcon fontSize="medium" />, label: 'Like' },
  { key: 'idea', icon: <EmojiObjectsIcon fontSize="medium" />, label: 'Interesting' },
  { key: 'see', icon: <VisibilityIcon fontSize="medium" />, label: 'See' },
  { key: 'hot', icon: <WhatshotIcon fontSize="medium" />, label: 'Hot' },
  { key: 'funny', icon: <EmojiEmotionsIcon fontSize="medium" />, label: 'Funny' },
  { key: 'very_funny', icon: <SentimentVerySatisfiedIcon fontSize="medium" />, label: 'Very Funny' },
  { key: 'dissatisfied', icon: <SentimentDissatisfiedIcon fontSize="medium" />, label: 'Dissatisfied' },
  { key: 'very_dissatisfied', icon: <SentimentVeryDissatisfiedIcon fontSize="medium" />, label: 'Very Dissatisfied' },
]

type FeedbackBarProps = {
  onRate?: (key: string) => void
  selected?: string
}
function FeedbackBar({ onRate, selected }: FeedbackBarProps) {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)
  const open = Boolean(anchorEl)
  const handleOpen = (e: React.MouseEvent<HTMLElement>) => setAnchorEl(e.currentTarget)
  const handleClose = () => setAnchorEl(null)
  const handleSelect = (key: string) => {
    if (onRate) onRate(key)
    handleClose()
  }
  const selectedIcon = FEEDBACK_ICONS.find(f => f.key === selected)?.icon
  return (
    <>
      <IconButton size="small" onClick={handleOpen} sx={{ p: 0.5 }}>
        {selectedIcon || <HelpOutlineIcon fontSize="medium" />}
      </IconButton>
      <Popover
        open={open}
        anchorEl={anchorEl}
        onClose={handleClose}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        transformOrigin={{ vertical: 'top', horizontal: 'left' }}
      >
        <Box sx={{ display: 'flex', gap: 0.5, p: 1 }}>
          {FEEDBACK_ICONS.map(f => (
            <Tooltip key={f.key} title={f.label}>
              <IconButton
                size="small"
                sx={{ p: 0.5, background: selected === f.key ? 'rgba(255,200,0,0.15)' : undefined }}
                onClick={() => handleSelect(f.key)}
              >
                {f.icon}
              </IconButton>
            </Tooltip>
          ))}
        </Box>
      </Popover>
    </>
  )
}
import { Box, Button, Card, CardContent, Divider, Stack, TextField, Typography, FormControl, InputLabel, Select, MenuItem, Tooltip, IconButton, Chip, Popover, Menu, MenuList, MenuItem as MUIMenuItem, ListItemIcon, ListItemText } from '@mui/material'
import SettingsIcon from '@mui/icons-material/Settings'
import VolumeUpIcon from '@mui/icons-material/VolumeUp'
import MicIcon from '@mui/icons-material/Mic'
import { alpha } from '@mui/material/styles'
import RefreshIcon from '@mui/icons-material/Refresh'
import { useAuth } from '../auth/AuthContext'
import MarkdownView from '../components/MarkdownView'
import EmojiButton from '../components/EmojiButton'
import ImageViewer from '../components/ImageViewer'

type Message = { role: 'system' | 'user' | 'assistant'; content: string; fromId?: string; fromName?: string; timestamp?: string; imageId?: string; pending?: boolean; localId?: string; imgPrompt?: string; imgStyleName?: string }
type Provider = { id: string; name: string; displayName?: string }
type Model = { id: string; name: string; displayName?: string }
type Persona = { id: string; name: string; gender?: string }
type ImageStyle = { id: string; name: string; description?: string; promptPrefix?: string; negativePrompt?: string }

export default function ChatPage() {
  // Responsive gear/settings menu state
  const [settingsAnchor, setSettingsAnchor] = useState<null | HTMLElement>(null)
  const [settingsMenu, setSettingsMenu] = useState<string | null>(null)
  const [subMenuAnchor, setSubMenuAnchor] = useState<null | HTMLElement>(null)
  const [subMenuType, setSubMenuType] = useState<string | null>(null)

  // Responsive breakpoint
  const isMobile = typeof window !== 'undefined' && window.matchMedia('(max-width: 600px)').matches

  // Handlers for gear/settings menu
  const handleSettingsOpen = (e: React.MouseEvent<HTMLElement>) => setSettingsAnchor(e.currentTarget)
  const handleSettingsClose = () => { setSettingsAnchor(null); setSettingsMenu(null); setSubMenuAnchor(null); setSubMenuType(null) }
  const handleMenuOpen = (menu: string, e: React.MouseEvent<HTMLElement>) => { setSettingsMenu(menu); setSubMenuAnchor(e.currentTarget); setSubMenuType(menu) }

  // When a provider is selected in mobile menu, update providerId, reload models, and keep menu open for model selection
  const handleProviderSelect = async (prId: string) => {
    setProviderId(prId)
    setSubMenuType('provider') // keep provider/model menu open
    // Wait for models to load (models are loaded via useEffect on providerId)
    // No need to close menu yet; user should select model next
  }

  // ...existing code...

  // Place this effect after providerId, modelId, models are declared
  const handleMenuClose = () => { setSubMenuAnchor(null); setSubMenuType(null) }
  // TTS: Speak text aloud
  function speakText(text: string, gender: 'male' | 'female' = 'female') {
    if ('speechSynthesis' in window) {
      const synth = window.speechSynthesis
      const voices = synth.getVoices()
      let selectedVoice: SpeechSynthesisVoice | undefined
      // Prefer Google UK English Female/Male for en-GB
      if (gender === 'female') {
        selectedVoice = voices.find(v => v.name === 'Google UK English Female' && v.lang === 'en-GB')
      } else {
        selectedVoice = voices.find(v => v.name === 'Google UK English Male' && v.lang === 'en-GB')
      }
      // Fallback to gender-based name/voiceURI matching
      if (!selectedVoice) {
        selectedVoice = voices.find(v => gender === 'female' ? /female|woman|girl/i.test(v.name + v.voiceURI) : /male|man|boy/i.test(v.name + v.voiceURI))
      }
      // Fuzzy fallback for 'Zira' (female) and 'Mark' (male)
      if (!selectedVoice) {
        if (gender === 'female') {
          selectedVoice = voices.find(v => /zira/i.test(v.name))
        } else {
          selectedVoice = voices.find(v => /mark/i.test(v.name))
        }
      }
      // Fallback to first available
      if (!selectedVoice) selectedVoice = voices[0]
      const utter = new window.SpeechSynthesisUtterance(text)
      if (selectedVoice) utter.voice = selectedVoice
      synth.speak(utter)
    }
  }

  // STT: Speech to text for input
  const recognitionRef = useRef<any>(null)
  function startRecognition() {
    const SpeechRecognition = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition
    if (!SpeechRecognition) {
      alert('Speech recognition not supported in this browser.')
      return
    }
    const recognition = new SpeechRecognition()
    recognition.lang = 'en-US'
    recognition.interimResults = false
    recognition.maxAlternatives = 1
    recognition.onresult = (event: any) => {
      const transcript = event.results[0][0].transcript
      setInput(prev => prev + (prev ? ' ' : '') + transcript)
    }
    recognition.onerror = () => {}
    recognitionRef.current = recognition
    recognition.start()
  }
  function stopRecognition() {
    if (recognitionRef.current) {
      recognitionRef.current.stop()
      recognitionRef.current = null
    }
  }
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
  const LS = {
    persona: 'cognition.chat.personaId',
    conversation: 'cognition.chat.conversationId',
    provider: 'cognition.chat.providerId',
    model: 'cognition.chat.modelId'
  } as const
  const getLS = (k: string) => {
    try { return localStorage.getItem(k) || '' } catch { return '' }
  }
  const setLS = (k: string, v: string | null) => { try { if (v) localStorage.setItem(k, v); else localStorage.removeItem(k) } catch {} }

  const [providerId, setProviderId] = useState<string>(getLS(LS.provider))
  const [modelId, setModelId] = useState<string>(getLS(LS.model))

  const [personas, setPersonas] = useState<Persona[]>([])
  const [personaId, setPersonaId] = useState<string>(getLS(LS.persona))

  type Conv = { id: string; title?: string | null }
  const [conversations, setConversations] = useState<Conv[]>([])

  // Image tools state
  const [imgStyles, setImgStyles] = useState<ImageStyle[]>([])
  const [imgStyleId, setImgStyleId] = useState<string>('')
  const [imgModel, setImgModel] = useState<string>('dall-e-3')
  const [imgMsgCount, setImgMsgCount] = useState<number>(6)
  const [imgPending, setImgPending] = useState<boolean>(false)
  // When the menu closes, if provider is selected but model is empty, set default/first model
  useEffect(() => {
    if (!settingsAnchor && subMenuType === 'provider' && providerId && !modelId && models.length > 0) {
      // Set default model (first in list)
      setModelId(models[0].id)
    }
  }, [settingsAnchor, subMenuType, providerId, modelId, models])
  const scrollRef = useRef<HTMLDivElement | null>(null)
  const stickToBottomRef = useRef(true)
  const forceScrollRef = useRef(false)
  const [viewer, setViewer] = useState<{ open: boolean, id?: string, title?: string }>({ open: false })

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
    if (autoSelectFirst) {
      const saved = getLS(LS.conversation)
      const pick = (saved && items.find(x => x.id === saved)) ? saved : (items[0]?.id || null)
      setConversationId(pick)
    }
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
            const saved = getLS(LS.persona)
            const pick = (saved && items.find(x => x.id === saved)) ? saved : (items[0]?.id || auth?.primaryPersonaId || '')
            if (pick) setPersonaId(pick)
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
          if (!personaId && items.length > 0) {
            const saved = getLS(LS.persona)
            const pick = (saved && items.find(x => x.id === saved)) ? saved : items[0].id
            setPersonaId(pick)
          }
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
      let chosenProviderId = providerId || getLS(LS.provider)
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
      if (!modelId && normModels.length > 0) {
        const savedModel = getLS(LS.model)
        const pick = (savedModel && normModels.find(x => x.id === savedModel || x.name === savedModel)) ? savedModel : normModels[0].id
        setModelId(pick)
      }
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

  // Persist selections to localStorage on change
  useEffect(() => { if (personaId) setLS(LS.persona, personaId) }, [personaId])
  useEffect(() => { setLS(LS.conversation, conversationId || '') }, [conversationId])
  useEffect(() => { if (providerId) setLS(LS.provider, providerId) }, [providerId])
  useEffect(() => { if (modelId) setLS(LS.model, modelId) }, [modelId])

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
          timestamp: i.createdAtUtc ?? i.CreatedAtUtc,
          imgPrompt: i.prompt ?? i.Prompt,
          imgStyleName: i.styleName ?? i.StyleName
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
    if (!accessToken || !conversationId || !personaId || !providerId) return
    const style = imgStyles.find(s => s.id === imgStyleId)
    const recent = messages.slice(-imgMsgCount)
    const lines = recent.map(m => `${m.fromName || m.role}: ${m.content}`).join('\n')
    // Build a style recipe string for the LLM
    const styleRecipe = [`Style: ${style?.name || ''}`, style?.description || '', style?.promptPrefix || ''].filter(Boolean).join('\n')
    // System + user instruction to synthesize a single image prompt from chat + style
    const sysInstr = `You are an expert prompt-writer for image models.
Given a conversation transcript and a style recipe, produce ONE concise, vivid, concrete image prompt.
Rules:
- 1-4 sentences. <= 2500 characters total.
- Describe subject, setting, background, foreground, lighting, mood, and camera.
- Avoid copyrighted characters/logos and explicit sexual content.
- Do NOT include disclaimers or the transcript itself. Output only the prompt.`
    const userInstr = `Style recipe:\n${styleRecipe}\n\nConversation (recent):\n${lines}\n\nWrite the single best image prompt now.`
    const promptBuildInput = `${sysInstr}\n\n${userInstr}`
    // Placeholder assistant message while we build + generate
    try {
      setImgPending(true)
      // Add placeholder assistant message for image generation
      const placeholderId = `img-${Date.now()}-${Math.random().toString(36).slice(2)}`
      const assistantName = personas.find(p => p.id === personaId)?.name || 'Assistant'
      setMessages(prev => [...prev, { role: 'assistant', content: 'Generating image', fromName: assistantName, pending: true, localId: placeholderId, imgPrompt: '', imgStyleName: style?.name }])
      if (stickToBottomRef.current) forceScrollRef.current = true
      // Step 1: ask LLM to synthesize an image prompt from recent chat + style
      let finalPrompt = ''
      try {
        const resp = await fetch('/api/chat/ask', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` },
          body: JSON.stringify({ PersonaId: personaId, ProviderId: providerId, ModelId: modelId || null, Input: promptBuildInput, RolePlay: false })
        })
        if (resp.ok) {
          const rj = await resp.json()
          finalPrompt = String(rj.reply || '').trim()
        }
      } catch {}
      if (!finalPrompt) {
        // Fallback: simple concatenation if LLM failed
        finalPrompt = `${style?.promptPrefix ? style.promptPrefix + '\n' : ''}${lines}`.slice(0, 2500)
      }
      // Update placeholder with the prompt we will use (for thumbnail titles)
      setMessages(prev => prev.map(m => m.localId === placeholderId ? { ...m, imgPrompt: finalPrompt } : m))
      // Step 2: request image generation
      const payload = { ConversationId: conversationId, PersonaId: personaId, Prompt: finalPrompt, Width: 1024, Height: 1024, StyleId: imgStyleId || undefined, NegativePrompt: style?.negativePrompt || undefined, Provider: 'OpenAI', Model: imgModel }
      const res = await fetch('/api/images/generate', { method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` }, body: JSON.stringify(payload) })
      if (!res.ok) {
        const txt = await res.text()
        setMessages(prev => prev.map(m => m.localId === placeholderId ? { ...m, pending: false, content: `Image error: ${txt}` } : m))
        return
      }
      const data = await res.json()
      const id = data.id || data.Id
      setMessages(prev => prev.map(m => m.localId === placeholderId ? { ...m, pending: false, content: '', imageId: String(id), imgPrompt: finalPrompt, imgStyleName: style?.name } : m))
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
          {/* Always show gear/settings icon and conversation title, always hide toolbars */}
          <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
            <IconButton aria-label="Settings" onClick={handleSettingsOpen} size="large">
              <SettingsIcon />
            </IconButton>
            {/* Conversation title next to gear */}
            <Typography variant="subtitle1" sx={{ ml: 1, fontWeight: 500 }}>
              {conversations.find(c => c.id === conversationId)?.title || (conversationId ? conversationId.slice(0,8) + '...' : 'New Conversation')}
            </Typography>
            <Popover
              open={Boolean(settingsAnchor)}
              anchorEl={settingsAnchor}
              onClose={handleSettingsClose}
              anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
              transformOrigin={{ vertical: 'top', horizontal: 'left' }}
            >
              <MenuList>
                <MUIMenuItem onClick={e => handleMenuOpen('persona', e)}>
                  <ListItemIcon><SettingsIcon /></ListItemIcon>
                  <ListItemText>Persona</ListItemText>
                </MUIMenuItem>
                <MUIMenuItem onClick={e => handleMenuOpen('provider', e)}>
                  <ListItemIcon><SettingsIcon /></ListItemIcon>
                  <ListItemText>Provider & Model</ListItemText>
                </MUIMenuItem>
                <MUIMenuItem onClick={e => handleMenuOpen('images', e)}>
                  <ListItemIcon><SettingsIcon /></ListItemIcon>
                  <ListItemText>Images</ListItemText>
                </MUIMenuItem>
                <MUIMenuItem onClick={e => handleMenuOpen('conversation', e)}>
                  <ListItemIcon><SettingsIcon /></ListItemIcon>
                  <ListItemText>Conversation</ListItemText>
                </MUIMenuItem>
              </MenuList>
            </Popover>
            {/* Cascading submenus */}
            <Popover
              open={Boolean(subMenuAnchor)}
              anchorEl={subMenuAnchor}
              onClose={handleMenuClose}
              anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
              transformOrigin={{ vertical: 'top', horizontal: 'left' }}
            >
              <MenuList>
                {subMenuType === 'persona' && personas.map(p => (
                  <MUIMenuItem key={p.id} selected={personaId === p.id} onClick={() => { setPersonaId(p.id); handleSettingsClose() }}>{p.name}</MUIMenuItem>
                ))}
                {subMenuType === 'provider' && [
                  ...providers.map(pr => (
                    <MUIMenuItem key={pr.id} selected={providerId === pr.id} onClick={async () => { await handleProviderSelect(pr.id) }}>{pr.displayName || pr.name}</MUIMenuItem>
                  )),
                  ...models.length > 0 ? [<Divider key="provider-divider" />] : [],
                  ...models.map(m => (
                    <MUIMenuItem key={m.id} selected={modelId === m.id} onClick={() => { setModelId(m.id); handleSettingsClose() }}>{m.displayName || m.name}</MUIMenuItem>
                  ))
                ]}
                {subMenuType === 'images' && [
                  <MUIMenuItem key="img-model-label" disabled>Image Model</MUIMenuItem>,
                  ...['dall-e-3', 'gpt-image-1'].map(im => (
                    <MUIMenuItem key={im} selected={imgModel === im} onClick={() => { setImgModel(im); handleMenuClose() }}>{im}</MUIMenuItem>
                  )),
                  <Divider key="img-divider" />,
                  <MUIMenuItem key="img-style-label" disabled>Image Style</MUIMenuItem>,
                  ...imgStyles.map(s => (
                    <MUIMenuItem key={s.id} selected={imgStyleId === s.id} onClick={() => { setImgStyleId(s.id); handleMenuClose() }}>{s.name}</MUIMenuItem>
                  )),
                  <Divider key="img-divider2" />,
                  <MUIMenuItem key="img-msg-label" disabled>#Msgs</MUIMenuItem>,
                  ...[6, 12, 24].map(n => (
                    <MUIMenuItem key={n} selected={imgMsgCount === n} onClick={() => { setImgMsgCount(n); handleMenuClose() }}>{n}</MUIMenuItem>
                  )),
                  <Divider key="img-divider3" />,
                  <MUIMenuItem key="img-generate" disabled={imgPending} onClick={() => { generateImageFromChat(); handleSettingsClose() }}>{imgPending ? 'Generating...' : 'Generate Image'}</MUIMenuItem>
                ]}
                {subMenuType === 'conversation' && [
                  <MUIMenuItem key="new-conv" onClick={() => { createConversation(); handleSettingsClose() }}>New Conversation</MUIMenuItem>,
                  <Divider key="conv-divider" />,
                  ...conversations.map(c => (
                    <MUIMenuItem key={c.id} selected={conversationId === c.id} onClick={() => { setConversationId(c.id); setMessages([]); handleSettingsClose() }}>{c.title || (c.id.slice(0,8) + '...')}</MUIMenuItem>
                  ))
                ]}
              </MenuList>
            </Popover>
          </Box>

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
                {messages.map((m, i) => {
                  const isUser = m.role === 'user'
                  return (
                    <Box
                      key={i}
                      sx={{
                        p: 1.25,
                        maxWidth: { xs: '92%', sm: '78%' },
                        backgroundColor: (theme) => isUser
                          ? alpha(theme.palette.action.selected, 0.45)
                          : theme.palette.background.paper,
                        alignSelf: isUser ? 'flex-end' : 'flex-start',
                        ml: isUser ? 2 : 0,
                        mr: isUser ? 0 : 2,
                        borderTopLeftRadius: isUser ? 12 : 10,
                        borderTopRightRadius: isUser ? 12 : 10,
                        borderBottomLeftRadius: isUser ? 12 : 8,
                        borderBottomRightRadius: isUser ? 8 : 12,
                      }}
                    >
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', textAlign: isUser ? 'right' : 'left', mb: 0.25 }}>
                        {m.fromName || m.role}
                      </Typography>
                      {m.imageId ? (
                        <Box sx={{ mt: 0.5 }}>
                          <Box
                            component="img"
                            alt={m.imgStyleName ? `[${m.imgStyleName}] ${m.imgPrompt || ''}` : (m.imgPrompt || 'generated')}
                            title={m.imgStyleName ? `[${m.imgStyleName}] ${m.imgPrompt || ''}` : (m.imgPrompt || 'generated')}
                            src={`/api/images/content?id=${m.imageId}`}
                            onClick={() => setViewer({ open: true, id: m.imageId, title: (m.imgStyleName ? `[${m.imgStyleName}] ` : '') + (m.imgPrompt || '') })}
                            sx={{ height: 240, width: 'auto', borderRadius: 1, cursor: 'zoom-in', maxWidth: '100%' }}
                          />
                        </Box>
                      ) : m.pending ? (
                        <Typography variant="body1" sx={{ textAlign: isUser ? 'right' : 'left' }}>
                          {m.content ? m.content + ' ' : ''}
                          <span className="loading-dots"><span>.</span><span>.</span><span>.</span></span>
                        </Typography>
                      ) : (
                        <Box sx={{ textAlign: isUser ? 'right' : 'left', display: 'flex', flexDirection: 'column', alignItems: isUser ? 'flex-end' : 'flex-start' }}>
                          <MarkdownView content={m.content} />
                          {/* Feedback bar and TTS button under every message */}
                          <Box sx={{ width: '100%', display: 'flex', alignItems: 'center', justifyContent: isUser ? 'flex-end' : 'flex-start', mt: 1 }}>
                            <FeedbackBar />
                            {m.role === 'assistant' && m.content && (
                              <IconButton
                                aria-label="Read aloud"
                                size="small"
                                sx={{ ml: 1 }}
                                onClick={() => {
                                  let gender: 'male' | 'female' = 'female'
                                  const persona = personas.find(p => p.id === personaId)
                                  if (persona && persona.gender) {
                                    const g = String(persona.gender).toLowerCase()
                                    if (g === 'male' || g === 'm') gender = 'male'
                                    if (g === 'female' || g === 'f') gender = 'female'
                                  }
                                  speakText(m.content, gender)
                                }}
                              >
                                <VolumeUpIcon fontSize="small" />
                              </IconButton>
                            )}
                          </Box>
                        </Box>
                      )}
                    </Box>
                  )
                })}
              </Stack>
            )}
          </Box>

          {/* Input */}
          <Divider />
          <Stack direction="row" spacing={1} alignItems="center">
            <TextField inputRef={inputRef} fullWidth size="small" placeholder="Type a message." value={input} onChange={(e) => setInput(e.target.value)} onKeyDown={(e) => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send() } }} />
            {/* STT record button */}
            <Tooltip title="Hold to record">
              <span>
                <IconButton
                  aria-label="Record"
                  size="medium"
                  onMouseDown={startRecognition}
                  onMouseUp={stopRecognition}
                  onTouchStart={startRecognition}
                  onTouchEnd={stopRecognition}
                >
                  <MicIcon />
                </IconButton>
              </span>
            </Tooltip>
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
            <Tooltip title={!personaId ? 'Select a persona' : (!providerId ? 'Select a provider' : (input.trim().length === 0 ? 'Type a message' : ''))}>
              <span>
                <Button variant="contained" disabled={!canSend} onClick={send}>Send</Button>
              </span>
            </Tooltip>
          </Stack>
          {!personaId || !providerId ? (
            <Typography variant="caption" color="text.secondary">
              {!personaId ? 'Choose a persona to chat as. ' : ''}{!providerId ? 'Choose a provider and model.' : ''}
            </Typography>
          ) : (
            <Typography variant="caption" color="text.secondary">
              {`Chatting as ${personas.find(p => p.id === personaId)?.name || 'Assistant'} via ${providers.find(p => p.id === providerId)?.displayName || providers.find(p => p.id === providerId)?.name}${modelId ? ` · ${models.find(m => m.id === modelId)?.displayName || models.find(m => m.id === modelId)?.name}` : ''}${conversationId ? '' : ' · New conversation on first send'}`}
            </Typography>
          )}
        </CardContent>
      </Card>
      <ImageViewer open={viewer.open} onClose={() => setViewer({ open: false })} imageId={viewer.id} title={viewer.title} />
    </Stack>
  )
}
