
import { useEffect, useRef, useState } from 'react';
import { fetchPersonas } from '../api/client';
import { ChatLayout } from '../components/chat/ChatLayout';
import { FeedbackBar } from '../components/chat/FeedbackBar';
import { useAuth } from '../auth/AuthContext';
import { useChatHub } from '../hooks/useChatHub';
import { useConversationsMessages } from '../hooks/useConversationsMessages';
import { useImageStyles } from '../hooks/useImageStyles';
import { MessageItemProps } from '../components/chat/MessageItem';
import { normalizeRole } from '../utils/chat';

// Types
type Provider = { id: string; name: string; displayName?: string };
type Model = { id: string; name: string; displayName?: string };
type Persona = { id: string; name: string; gender?: string };
type Message = MessageItemProps;

export default function ChatPage() {
  // Auth
  const { auth } = useAuth();
  const accessToken = auth?.accessToken;
  const userId = auth?.userId;

  // LocalStorage keys
  const LS = {
    persona: 'cognition.chat.personaId',
    provider: 'cognition.chat.providerId',
    model: 'cognition.chat.modelId',
  } as const;
  const getLS = (k: string) => {
    try { return localStorage.getItem(k) || ''; } catch { return ''; }
  };
  const setLS = (k: string, v: string | null) => {
    try { if (v) localStorage.setItem(k, v); else localStorage.removeItem(k); } catch { }
  };

  // State
  const [personas, setPersonas] = useState<Persona[]>([]);
  const [personaId, setPersonaId] = useState<string>(getLS(LS.persona));
  const [providers, setProviders] = useState<Provider[]>([]);
  const [providerId, setProviderId] = useState<string>(getLS(LS.provider));
  const [models, setModels] = useState<Model[]>([]);
  const [modelId, setModelId] = useState<string>(getLS(LS.model));
  // Conversations/messages hook
  const {
    conversations,
    conversationId,
    setConversationId,
    messages,
    setMessages,
  } = useConversationsMessages(accessToken || '', personaId);

  // Ensure all messages are normalized on initial load
  useEffect(() => {
    setMessages(prev => prev.map(m => ({ ...m, role: normalizeRole(m.role) })));
  }, []);

  // Image actions hook
  const [imgPending, setImgPending] = useState(false);

  // ChatHub event bus
  const chatHub = useChatHub({
    conversationId: conversationId ?? '',
    accessToken,
    onAssistantMessage: (msg) => {
      console.log('AssistantMessageAppended event:', msg);
      // Resolve the first pending assistant bubble, or append new one
      setMessages(prev => {
        const updated = prev.map(m => ({ ...m, role: normalizeRole(m.role) }));
        const idx = updated.findIndex(m => m.role === 'assistant' && m.pending);
        if (idx !== -1) {
          const copy = [...updated];
          copy[idx] = { ...copy[idx], pending: false, content: msg.content, timestamp: msg.timestamp } as MessageItemProps;
          return copy;
        }
        const assistantMsg: MessageItemProps = {
          role: 'assistant',
          content: msg.content,
          fromName: personas.find(p => p.id === personaId)?.name || 'Assistant',
          timestamp: msg.timestamp,
        };
        return [...updated, assistantMsg];
      });
      // Clear any pending send timeout
      if (pendingAssistantTimerRef.current) {
        clearTimeout(pendingAssistantTimerRef.current);
        pendingAssistantTimerRef.current = null;
      }
    },
    onPlanReady: (evt) => {
      // If plan is an array of steps, use it; otherwise, wrap in array
      const steps = Array.isArray(evt.plan) ? evt.plan : [evt.plan];
      setPlanSteps(steps);
    },
    onToolExecutionRequested: (evt) => {
      setToolActions(prev => [...prev, `Tool requested: ${evt.toolId}`]);
    },
    onToolExecutionCompleted: (evt) => {
      setToolActions(prev => [...prev, `Tool completed: ${evt.toolId} (${evt.success ? 'success' : 'error'})`]);
    },
  });
  // Image styles hook
  const {
    imgStyles,
    imgStyleId,
    setImgStyleId,
  } = useImageStyles(accessToken || '');
  const [busy, setBusy] = useState(false);
  const [planSteps, setPlanSteps] = useState<string[]>([]);
  const [toolActions, setToolActions] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState<boolean>(false);

  // Load personas
  useEffect(() => {
    const loadPersonas = async () => {
      setLoading(true);
      setError(null);
      try {
        if (!accessToken || !userId) return;
        const list = await fetchPersonas(accessToken, userId);
        const assistants = (list as any[]).filter((p: any) => {
          const t = (p.type ?? p.Type ?? p.persona_type ?? p.PersonaType);
          if (typeof t === 'number') return t === 1;
          if (typeof t === 'string') return t.toLowerCase() === 'assistant';
          return false;
        });
        const items: Persona[] = assistants.map((p: any) => ({ id: p.id ?? p.Id, name: p.name ?? p.Name }));
        setPersonas(items);
        if (!personaId) {
          const saved = getLS(LS.persona);
          const pick = (saved && items.find(x => x.id === saved)) ? saved : (items[0]?.id || auth?.primaryPersonaId || '');
          if (pick) setPersonaId(pick);
        }
      } catch (e: any) {
        setError('Failed to load personas');
      } finally {
        setLoading(false);
      }
    };
    loadPersonas();
  }, [accessToken, userId, personaId, auth?.primaryPersonaId]);

  // Load providers/models
  useEffect(() => {
    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        if (!accessToken) return;
        const headers = { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` };
        const pRes = await fetch('/api/llm/providers', { headers });
        if (!pRes.ok) throw new Error('Failed to load providers');
        const pList = await pRes.json();
        const normProviders: Provider[] = (pList as any[]).map((p: any) => ({ id: p.id ?? p.Id, name: p.name ?? p.Name, displayName: p.displayName ?? p.DisplayName }));
        setProviders(normProviders);
        let chosenProviderId = providerId || getLS(LS.provider);
        if (!chosenProviderId && normProviders.length > 0) {
          const openai = normProviders.find(p => (p.name || '').toLowerCase() === 'openai');
          chosenProviderId = (openai ?? normProviders[0]).id;
          setProviderId(chosenProviderId);
        }
        if (!chosenProviderId) return;
        const mRes = await fetch(`/api/llm/providers/${chosenProviderId}/models`, { headers });
        if (!mRes.ok) throw new Error('Failed to load models');
        const mList = await mRes.json();
        const normModels: Model[] = (mList as any[]).map((m: any) => ({ id: m.id ?? m.Id, name: m.name ?? m.Name, displayName: m.displayName ?? m.DisplayName }));
        setModels(normModels);
        if (!modelId && normModels.length > 0) {
          const savedModel = getLS(LS.model);
          const pick = (savedModel && normModels.find(x => x.id === savedModel || x.name === savedModel)) ? savedModel : normModels[0].id;
          setModelId(pick);
        }
      } catch (e: any) {
        setError(e.message || 'Failed to load providers/models');
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [accessToken, providerId]);

  // Persist selections to localStorage on change
  useEffect(() => { if (personaId) setLS(LS.persona, personaId); }, [personaId]);
  useEffect(() => { if (providerId) setLS(LS.provider, providerId); }, [providerId]);
  useEffect(() => { if (modelId) setLS(LS.model, modelId); }, [modelId]);
  useEffect(() => { if (conversationId) localStorage.setItem('cognition.chat.conversationId', conversationId); }, [conversationId]);

  // Message sending logic
  const handleSendMessage = async (text: string) => {
    if (!accessToken || !text.trim()) return;
    setBusy(true);
    try {
      // Ensure a conversation exists
      let convId = conversationId;
      if (!convId) {
        const res = await fetch('/api/conversations', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}) },
          body: JSON.stringify({ Title: null, ParticipantIds: personaId ? [personaId] : [] })
        });
        if (!res.ok) {
          const err = await res.text();
          throw new Error(err || 'Failed to create conversation');
        }
        const body = await res.json();
        convId = body.id || body.Id;
        setConversationId(convId);
        try { localStorage.setItem('cognition.chat.conversationId', convId); } catch {}
        // Make sure hub has joined this conversation
        try { await chatHub.connectionRef.current?.invoke('JoinConversation', convId); } catch {}
      }

      // Append user message immediately (no pending)
      const placeholderId = `pending-${Date.now()}-${Math.random().toString(36).slice(2)}`;
      setMessages(prev => ([
        ...prev,
        { role: 'user', content: text, fromId: userId, fromName: 'You', timestamp: new Date().toISOString() } as MessageItemProps,
        // Add a pending assistant placeholder with localId
        { role: 'assistant', content: '', fromName: personas.find(p => p.id === personaId)?.name || 'Assistant', pending: true, localId: placeholderId } as any,
      ]));

      // Send via hub
      const ok = await (async () => {
        if (convId) {
          try { await chatHub.connectionRef.current?.invoke('AppendUserMessage', convId, text, personaId, providerId, modelId); return true; } catch {}
        }
        return await chatHub.sendUserMessage(text, personaId, providerId, modelId);
      })();
      // Start a timeout to convert pending assistant to error only if no reply arrives
      if (!ok) {
        // Let the timeout handle marking error to avoid false negatives
      }
      if (pendingAssistantTimerRef.current) clearTimeout(pendingAssistantTimerRef.current);
      pendingAssistantTimerRef.current = window.setTimeout(() => {
        setMessages(prev => prev.map(m => (m.role === 'assistant' && (m as any).localId === placeholderId && m.pending)
          ? { ...m, pending: false, content: 'Error sending message.' }
          : m
        ));
        pendingAssistantTimerRef.current = null;
      }, 8000) as any;
    } catch (e: any) {
      setError(e.message || 'Failed to send message');
      // Do not immediately mark pending assistant as error; allow timeout to handle to avoid dupes
    } finally {
      setBusy(false);
    }
  };

  // Track pending assistant timeout
  const pendingAssistantTimerRef = useRef<number | null>(null);

  // Generate image from recent chat using style
  const handleGenerateImage = async () => {
    if (!accessToken || !conversationId || !personaId || !providerId) return;
    setImgPending(true);
    try {
      const style = imgStyles.find(s => s.id === imgStyleId);
      const recent = messages.slice(-6);
      const lines = recent.map(m => `${m.fromName || m.role}: ${m.content}`).join('\n');
      const styleRecipe = [`Style: ${style?.name || ''}`, style?.description || '', style?.promptPrefix || ''].filter(Boolean).join('\n');
      const sysInstr = `You are an expert prompt-writer for image models.\nGiven a conversation transcript and a style recipe, produce ONE concise, vivid, concrete image prompt.\nRules:\n- 1-4 sentences. <= 2500 characters total.\n- Describe subject, setting, background, foreground, lighting, mood, and camera.\n- Avoid copyrighted characters/logos and explicit sexual content.\n- Do NOT include disclaimers or the transcript itself. Output only the prompt.`;
      const userInstr = `Style recipe:\n${styleRecipe}\n\nConversation (recent):\n${lines}\n\nWrite the single best image prompt now.`;
      const promptBuildInput = `${sysInstr}\n\n${userInstr}`;

      // Add placeholder assistant message for image generation
      const assistantName = personas.find(p => p.id === personaId)?.name || 'Assistant';
      const placeholderId = `img-${Date.now()}-${Math.random().toString(36).slice(2)}`;
      setMessages(prev => [...prev, { role: 'assistant', content: 'Generating image', fromName: assistantName, pending: true, localId: placeholderId, imgPrompt: '', imgStyleName: style?.name, metatype: 'Image' } as any]);

      // Step 1: ask LLM to synthesize an image prompt
      let finalPrompt = '';
      try {
        const resp = await fetch('/api/chat/ask', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` },
          body: JSON.stringify({ PersonaId: personaId, ProviderId: providerId, ModelId: modelId || null, Input: promptBuildInput, RolePlay: false })
        });
        if (resp.ok) {
          const rj = await resp.json();
          finalPrompt = String(rj.reply || '').trim();
        }
      } catch {}
      if (!finalPrompt) {
        finalPrompt = `${style?.promptPrefix ? style.promptPrefix + '\n' : ''}${lines}`.slice(0, 2500);
      }
      setMessages(prev => prev.map(m => (m as any).localId === placeholderId ? { ...m, imgPrompt: finalPrompt, metatype: 'Image' } : m));

      // Step 2: request image generation
      const payload = { ConversationId: conversationId, PersonaId: personaId, Prompt: finalPrompt, Width: 1024, Height: 1024, StyleId: imgStyleId || undefined, NegativePrompt: style?.negativePrompt || undefined, Provider: 'OpenAI', Model: 'dall-e-3' };
      const res = await fetch('/api/images/generate', { method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` }, body: JSON.stringify(payload) });
      if (!res.ok) {
        const txt = await res.text();
        setMessages(prev => prev.map(m => (m as any).localId === placeholderId ? { ...m, pending: false, content: `Image error: ${txt}` } : m));
        return;
      }
      const data = await res.json();
      const id = data.id || data.Id;
      setMessages(prev => prev.map(m => (m as any).localId === placeholderId ? { ...m, pending: false, content: '', imageId: String(id), imgPrompt: finalPrompt, imgStyleName: style?.name, metatype: 'Image' } : m));
    } catch (e: any) {
      setMessages(prev => prev.map(m => (m as any).pending ? { ...m, pending: false, content: `Image error: ${String(e?.message || e)}` } : m));
    } finally {
      setImgPending(false);
    }
  };

  // Wire up modular layout with real state and handlers
  return (
    <ChatLayout
      personas={personas}
      personaId={personaId}
      onPersonaChange={setPersonaId}
      providers={providers}
      models={models}
      providerId={providerId}
      modelId={modelId}
      onProviderChange={setProviderId}
      onModelChange={setModelId}
      messages={messages}
      onSend={handleSendMessage}
      busy={busy}
      error={error || undefined}
      loading={loading}
      planSteps={planSteps}
      toolActions={toolActions}
      conversations={conversations}
      conversationId={conversationId ?? ''}
      onConversationChange={(id) => { setMessages([]); setConversationId(id); }}
      imgStyles={imgStyles}
      imgStyleId={imgStyleId}
      onImgStyleChange={setImgStyleId}
      imgPending={imgPending}
      onGenerateImage={handleGenerateImage}
      onNewConversation={() => { setMessages([]); setConversationId(null); try { localStorage.removeItem('cognition.chat.conversationId'); } catch {} }}
    />
  );
}
