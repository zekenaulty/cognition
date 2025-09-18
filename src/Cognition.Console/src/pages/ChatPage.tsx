
import { useEffect, useRef, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { fetchPersonas } from '../api/client';
import { ChatLayout } from '../components/chat/ChatLayout';
import { useAuth } from '../auth/AuthContext';
import { useChatHub } from '../hooks/useChatHub';
import { useConversationsMessages } from '../hooks/useConversationsMessages';
import { useImageStyles } from '../hooks/useImageStyles';
import { useImageGenerator } from '../hooks/useImageGenerator';
import { useChatBus } from '../hooks/useChatBus';
import { useProvidersModels } from '../hooks/useProvidersModels';
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
  const route = useParams<{ personaId?: string; conversationId?: string }>();
  const navigate = useNavigate();
  const [personaId, setPersonaId] = useState<string>(route.personaId || getLS(LS.persona));
  const {
    providers,
    providerId,
    setProviderId,
    models,
    modelId,
    setModelId,
  } = useProvidersModels(accessToken || '', getLS(LS.provider), getLS(LS.model));
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

  // Image styles and generator hook
  const {
    imgStyles,
    imgStyleId,
    setImgStyleId,
  } = useImageStyles(accessToken || '');
  const assistantName = personas.find(p => p.id === personaId)?.name || 'Assistant';
  const [assistantGender, setAssistantGender] = useState<string | undefined>(undefined);
  const [assistantVoiceName, setAssistantVoiceName] = useState<string | undefined>(undefined);
  const { generateFromChat, pending: imgPending } = useImageGenerator({
    accessToken,
    conversationId,
    personaId,
    providerId,
    modelId,
    imgStyleId,
    imgStyles,
    messages,
    setMessages,
    assistantName,
  });

  // ChatHub connection (events handled via bus)
  const chatHub = useChatHub({ conversationId: conversationId ?? '', accessToken });

  // Bus subscriptions: final assistant message
  useChatBus('assistant-message', (msg) => {
    if (!msg || msg.conversationId !== (conversationId ?? '')) return;
    setMessages(prev => {
      const updated = prev.map(m => ({ ...m, role: normalizeRole(m.role) }));
      const idx = updated.findIndex(m => m.role === 'assistant' && m.pending);
      if (idx !== -1) {
        const copy = [...updated];
        const cur = copy[idx] as any;
        const baseVersions: string[] = Array.isArray(cur.versions) && cur.versions.length ? cur.versions : (cur.content ? [cur.content] : []);
        const nextVersions = baseVersions.length ? [...baseVersions.slice(0, baseVersions.length - 1), msg.content] : [msg.content];
        copy[idx] = { ...cur, id: (msg as any).messageId, pending: false, content: msg.content, timestamp: msg.timestamp, versions: nextVersions, versionIndex: Math.max(0, nextVersions.length - 1) } as MessageItemProps;
        return copy;
      }
      const assistantMsg: MessageItemProps = {
        role: 'assistant', id: (msg as any).messageId,
        content: msg.content,
        fromName: personas.find(p => p.id === personaId)?.name || 'Assistant',
        timestamp: msg.timestamp,
        versions: [msg.content],
        versionIndex: 0,
      };
      return [...updated, assistantMsg];
    });
    if (pendingAssistantTimerRef.current) {
      clearTimeout(pendingAssistantTimerRef.current);
      pendingAssistantTimerRef.current = null;
    }
  });

  // Bus: plan/tool events
  useChatBus('plan-ready', (evt) => {
    if (!evt || evt.conversationId !== (conversationId ?? '')) return;
    const steps = Array.isArray(evt.plan) ? evt.plan : [evt.plan];
    setPlanSteps(steps);
  });
  useChatBus('tool-requested', (evt) => {
    if (!evt || evt.conversationId !== (conversationId ?? '')) return;
    setToolActions(prev => [...prev, `Tool requested: ${evt.toolId}`]);
  });
  useChatBus('tool-completed', (evt) => {
    if (!evt || evt.conversationId !== (conversationId ?? '')) return;
    setToolActions(prev => [...prev, `Tool completed: ${evt.toolId} (${evt.success ? 'success' : 'error'})`]);
  });

  // Streaming deltas with rAF coalescing
  const deltaBufferRef = useRef<string>('');
  const deltaRafRef = useRef<number | null>(null);
  const flushDelta = () => {
    const delta = deltaBufferRef.current;
    if (!delta) return;
    deltaBufferRef.current = '';
    setMessages(prev => {
      const updated = prev.map(m => ({ ...m, role: normalizeRole(m.role) }));
      const idx = updated.findIndex(m => m.role === 'assistant' && m.pending);
      if (idx !== -1) {
        const copy = [...updated];
        const cur = copy[idx];
        copy[idx] = { ...cur, content: (cur.content || '') + delta } as MessageItemProps;
        return copy;
      }
      const assistantMsg: MessageItemProps = {
        role: 'assistant',
        content: delta,
        fromName: personas.find(p => p.id === personaId)?.name || 'Assistant',
        timestamp: new Date().toISOString(),
        pending: true,
      } as any;
      return [...updated, assistantMsg];
    });
  };
  useChatBus('assistant-delta', (evt) => {
    if (!evt || evt.conversationId !== (conversationId ?? '')) return;
    deltaBufferRef.current += evt.text || '';
    if (deltaRafRef.current == null) {
      deltaRafRef.current = requestAnimationFrame(() => {
        deltaRafRef.current = null;
        flushDelta();
      });
    }
  });

  // Hub broadcasts for versions (multi-client sync)
  useChatBus('assistant-version-appended', (evt: any) => {
    if (!evt || evt.conversationId !== (conversationId ?? '')) return;
    setMessages(prev => {
      const copy = [...prev];
      const idx = copy.findIndex(m => (m as any).id === evt.messageId);
      if (idx === -1) return prev;
      const m: any = copy[idx];
      const base = Array.isArray(m.versions) ? m.versions : (m.content ? [m.content] : []);
      const next = [...base];
      const vi = typeof evt.versionIndex === 'number' ? evt.versionIndex : next.length;
      next[vi] = evt.content;
      copy[idx] = { ...m, versions: next, versionIndex: vi, content: evt.content };
      return copy;
    });
  });
  useChatBus('assistant-version-activated', (evt: any) => {
    if (!evt || evt.conversationId !== (conversationId ?? '')) return;
    setMessages(prev => {
      const copy = [...prev];
      const idx = copy.findIndex(m => (m as any).id === evt.messageId);
      if (idx === -1) return prev;
      const m: any = copy[idx];
      const versions: string[] = Array.isArray(m.versions) ? m.versions : (m.content ? [m.content] : []);
      const vi = Math.max(0, Math.min(versions.length - 1, evt.versionIndex ?? 0));
      copy[idx] = { ...m, versionIndex: vi, content: versions[vi] };
      return copy;
    });
  });

  // Connection status indicator
  const [hubState, setHubState] = useState<'connecting' | 'connected' | 'reconnecting' | 'disconnected'>('disconnected');
  useChatBus('connection-state', s => setHubState(s.state));

  // Conversation lifecycle
  useChatBus('conversation-created', evt => {
    if (!evt) return;
    setConversationId(evt.conversationId);
    try { localStorage.setItem('cognition.chat.conversationId', evt.conversationId); } catch {}
  });
  useChatBus('conversation-joined', evt => {
    if (!evt) return;
    setConversationId(evt.conversationId);
    try { localStorage.setItem('cognition.chat.conversationId', evt.conversationId); } catch {}
  });
  useChatBus('conversation-left', evt => {
    if (!evt) return;
    if (evt.conversationId === (conversationId ?? '')) {
      setConversationId(null);
      try { localStorage.removeItem('cognition.chat.conversationId'); } catch {}
    }
  });

  // Image styles hook initialized above with generator
  const [busy, setBusy] = useState(false);
  const [planSteps, setPlanSteps] = useState<string[]>([]);
  const [toolActions, setToolActions] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState<boolean>(false);

  // Clear plan/tool state on persona or conversation change to avoid stale info
  useEffect(() => {
    setPlanSteps([]);
    setToolActions([]);
  }, [personaId, conversationId]);

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

  // Sync personaId from route if present
  useEffect(() => {
    if (route.personaId && route.personaId !== personaId) {
      setPersonaId(route.personaId);
    }
  }, [route.personaId]);

  // Sync conversationId from route if present
  useEffect(() => {
    if (route.conversationId && route.conversationId !== (conversationId ?? '')) {
      setMessages([]);
      setConversationId(route.conversationId);
    }
  }, [route.conversationId]);

  // Load selected persona details (for gender/voice)
  useEffect(() => {
    (async () => {
      try {
        if (!personaId || !accessToken) { setAssistantGender(undefined); setAssistantVoiceName(undefined); return; }
        const res = await fetch(`/api/personas/${personaId}`, { headers: { Authorization: `Bearer ${accessToken}` } });
        if (!res.ok) { setAssistantGender(undefined); setAssistantVoiceName(undefined); return; }
        const p = await res.json();
        const g = p.gender ?? p.Gender;
        const v = p.voice ?? p.Voice;
        const gl = typeof g === 'string' ? g.toLowerCase() : '';
        const gnorm = gl.startsWith('f') ? 'female' : (gl.startsWith('m') ? 'male' : undefined);
        setAssistantGender(gnorm);
        setAssistantVoiceName(typeof v === 'string' ? v : undefined);
      } catch {
        setAssistantGender(undefined);
        setAssistantVoiceName(undefined);
      }
    })();
  }, [personaId, accessToken]);

  // Providers/models handled by useProvidersModels

  // Persist selections to localStorage on change
  useEffect(() => { if (personaId) setLS(LS.persona, personaId); }, [personaId]);
  useEffect(() => { if (providerId) setLS(LS.provider, providerId); }, [providerId]);
  useEffect(() => { if (modelId) setLS(LS.model, modelId); }, [modelId]);
  useEffect(() => {
    if (conversationId) {
      localStorage.setItem('cognition.chat.conversationId', conversationId);
      if (personaId && (route.personaId !== personaId || route.conversationId !== conversationId)) {
        navigate(`/chat/${personaId}/${conversationId}`, { replace: true });
      }
    }
  }, [conversationId, personaId]);

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
        try { localStorage.setItem('cognition.chat.conversationId', convId ?? ''); } catch {}
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

  // Generate image using reusable hook
  const handleGenerateImage = async () => { await generateFromChat(); };

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
      onGenerateImage={(model, count) => { generateFromChat(model, count); }}
      onNewConversation={() => { setMessages([]); setConversationId(null); try { localStorage.removeItem('cognition.chat.conversationId'); } catch {} }}
      connectionState={hubState}
      assistantVoiceName={assistantVoiceName}
      assistantGender={assistantGender}
      onRegenerate={(idx) => {
        // Find previous user message
        const prevUserIdx = (() => { for (let i = idx - 1; i >= 0; i--) { if (normalizeRole(messages[i].role) === 'user') return i; } return -1; })();
        const prevContent = prevUserIdx >= 0 ? messages[prevUserIdx].content : '';
        (async () => {
          try {
            if (!accessToken) return;
            const resp = await fetch('/api/chat/ask', {
              method: 'POST',
              headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` },
              body: JSON.stringify({ PersonaId: personaId, ProviderId: providerId, ModelId: modelId || null, Input: prevContent || 'Regenerate the previous assistant response with a different approach.', RolePlay: false })
            });
            if (!resp.ok) return;
            const data = await resp.json();
            const text = String(data.reply || data.content || '').trim();
            if (!text) return;
            // Persist as server version if message id is known
            const msgId = (messages[idx] as any).id as string | undefined;
            if (conversationId && msgId) {
              try {
                await fetch(`/api/conversations/${conversationId}/messages/${msgId}/versions`, {
                  method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` }, body: JSON.stringify({ Content: text })
                });
              } catch {}
            }
            // UI will update on hub 'assistant-version-appended' event
          } catch {}
        })();
      }}
      onPrevVersion={(idx) => {
        const msgId = (messages[idx] as any).id as string | undefined;
        if (conversationId && msgId && accessToken) {
          const current = typeof (messages[idx] as any).versionIndex === 'number' ? (messages[idx] as any).versionIndex : 0;
          const target = Math.max(0, current - 1);
          fetch(`/api/conversations/${conversationId}/messages/${msgId}/active-version`, {
            method: 'PATCH', headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` }, body: JSON.stringify({ Index: target })
          }).catch(() => {});
        }
      }}
      onNextVersion={(idx) => {
        const msgId = (messages[idx] as any).id as string | undefined;
        if (conversationId && msgId && accessToken) {
          const current = typeof (messages[idx] as any).versionIndex === 'number' ? (messages[idx] as any).versionIndex : 0;
          const target = current + 1;
          fetch(`/api/conversations/${conversationId}/messages/${msgId}/active-version`, {
            method: 'PATCH', headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` }, body: JSON.stringify({ Index: target })
          }).catch(() => {});
        }
      }}
    />
  );
}
