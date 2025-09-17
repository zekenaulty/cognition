
import { useEffect, useMemo, useRef, useState } from 'react';
import { fetchPersonas } from '../api/client';
import { request } from '../api/client';
import { ChatLayout } from '../components/chat/ChatLayout';
import { FeedbackBar } from '../components/chat/FeedbackBar';
import { useAuth } from '../auth/AuthContext';
import { useChatHub } from '../hooks/useChatHub';
import { useConversationsMessages } from '../hooks/useConversationsMessages';
import { useImageStyles } from '../hooks/useImageStyles';
import { useImageActions } from '../hooks/useImageActions';
import { MessageItemProps } from '../components/chat/MessageItem';

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
  // Normalize all loaded messages from backend to MessageItemProps
  const normalizeRole = (r: any): 'system' | 'user' | 'assistant' => {
    if (r === 1 || r === '1' || r === 'user') return 'user';
    if (r === 2 || r === '2' || r === 'assistant') return 'assistant';
    if (r === 0 || r === '0' || r === 'system') return 'system';
    return 'user';
  };
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
  const { createImageMessage, pendingImageId } = useImageActions(setMessages);

  // ChatHub event bus
  const chatHub = useChatHub({
    conversationId: conversationId ?? '',
    accessToken,
    onAssistantMessage: (msg) => {
      console.log('AssistantMessageAppended event:', msg);
      // Remove pending flag from matching user message if present
      setMessages(prev => {
        // Normalize all roles to MessageItemProps type
        const normalizeRole = (r: any): 'system' | 'user' | 'assistant' => {
          if (r === 1 || r === '1' || r === 'user') return 'user';
          if (r === 2 || r === '2' || r === 'assistant') return 'assistant';
          if (r === 0 || r === '0' || r === 'system') return 'system';
          return 'user';
        };
        // Fix all previous messages
        const updated = prev.map(m => ({
          ...m,
          role: normalizeRole(m.role)
        })).map(m =>
          m.role === 'user' && m.pending && m.content === msg.content
            ? { ...m, pending: false }
            : m
        );
        // Append assistant message, strictly typed
        const assistantMsg: MessageItemProps = {
          role: 'assistant',
          content: msg.content,
          fromName: personas.find(p => p.id === personaId)?.name || 'Assistant',
          timestamp: msg.timestamp,
        };
        return [
          ...updated,
          assistantMsg,
        ];
      });
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
      // If no conversationId, let server create it
      let convId = conversationId;
      if (!convId) {
        // Server will create a new conversation and push the id back via event
        // Optionally, you could set a temporary id or wait for the event
      }
      const localId = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
      await chatHub.sendUserMessage(text, personaId, providerId, modelId);
      setMessages(prev => [
        ...prev,
        {
          role: normalizeRole(1),
          content: text,
          fromId: userId,
          fromName: personas.find(p => p.id === personaId)?.name || 'User',
          timestamp: new Date().toISOString(),
          pending: true,
          localId,
        } as MessageItemProps,
      ]);
    } catch (e: any) {
      setError(e.message || 'Failed to send message');
    } finally {
      setBusy(false);
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
      onConversationChange={setConversationId}
      imgStyles={imgStyles}
      imgStyleId={imgStyleId}
      onImgStyleChange={setImgStyleId}
    />
  );
}