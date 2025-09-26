import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
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
import { useUserSettings } from '../hooks/useUserSettings';
import { useAgentPersonaIndex } from '../hooks/useAgentPersonaIndex';


export default function ChatPage() {
  const { auth } = useAuth();
  const accessToken = auth?.accessToken || '';
  const navigate = useNavigate();
  const settings = useUserSettings();
  const { agentId: routeAgentId, conversationId: routeConversationId } = useParams<{ agentId?: string; conversationId?: string }>();

  const { agents, loading: loadingAgents, resolvePersonaId } = useAgentPersonaIndex(auth?.accessToken);
  const [agentId, setAgentId] = useState<string>('');
  const [assistantGender, setAssistantGender] = useState<string | undefined>();
  const [assistantVoiceName, setAssistantVoiceName] = useState<string | undefined>();
  const [planSteps, setPlanSteps] = useState<string[]>([]);
  const [toolActions, setToolActions] = useState<string[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hubState, setHubState] = useState<'connecting' | 'connected' | 'reconnecting' | 'disconnected'>('disconnected');

  const activePersonaId = agentId ? resolvePersonaId(agentId) : undefined;

  const {
    providers,
    providerId,
    setProviderId,
    models,
    modelId,
    setModelId,
  } = useProvidersModels(accessToken, settings.get<string>('chat.providerId') || '', settings.get<string>('chat.modelId') || '');

  const {
    conversations,
    conversationId,
    setConversationId,
    messages,
    setMessages,
    setConversations,
  } = useConversationsMessages(accessToken, agentId);

  const {
    imgStyles,
    imgStyleId,
    setImgStyleId,
  } = useImageStyles(accessToken);

  const assistantName = useMemo(() => {
    if (agentId) {
      const agent = agents.find(a => a.id === agentId);
      if (agent) return agent.label;
    }
    return 'Assistant';
  }, [agents, agentId]);

  const { generateFromChat, pending: imgPending } = useImageGenerator({
    accessToken,
    conversationId,
    agentId,
    personaId: activePersonaId,
    providerId,
    modelId,
    imgStyleId,
    imgStyles,
    messages,
    setMessages,
    assistantName,
  });

  const chatHub = useChatHub({ conversationId: conversationId ?? '', accessToken });

  // Normalize any existing messages once on mount
  useEffect(() => {
    setMessages(prev => prev.map(m => ({ ...m, role: normalizeRole(m.role) })));
  }, [setMessages]);
// Ensure an agent is selected when list or route changes
  useEffect(() => {
    if (!agents.length) {
      setAgentId('');
      return;
    }
    const routeCandidate = routeAgentId && agents.some(a => a.id === routeAgentId) ? routeAgentId : undefined;
    const savedCandidate = (() => {
      const saved = settings.get<string>('chat.agentId');
      if (saved && agents.some(a => a.id === saved)) return saved;
      return undefined;
    })();
    const next = routeCandidate ?? savedCandidate ?? agents[0].id;
    if (next && next !== agentId) {
      setAgentId(next);
    }
  }, [agents, routeAgentId]);

  // Persist agent selection and synchronize route
  useEffect(() => {
    if (!agentId) return;
    settings.set('chat.agentId', agentId);
    if (conversationId) {
      if (routeAgentId !== agentId || routeConversationId !== conversationId) {
        navigate(`/chat/${agentId}/${conversationId}`, { replace: true });
      }
    } else {
      if (routeAgentId !== agentId || routeConversationId) {
        navigate(`/chat/${agentId}`, { replace: true });
      }
    }
  }, [agentId, conversationId, navigate, routeAgentId, routeConversationId, settings]);

  // When conversation id is provided in the route, load it
  useEffect(() => {
    if (routeConversationId && routeConversationId !== conversationId) {
      setMessages([]);
      setConversationId(routeConversationId);
    }
  }, [routeConversationId, conversationId, setConversationId, setMessages]);

  // Clear conversation state when agent changes without explicit conversation in route
  const previousAgentRef = useRef<string | undefined>();
  useEffect(() => {
    if (!agentId) return;
    const previous = previousAgentRef.current;
    previousAgentRef.current = agentId;
    if (previous && previous !== agentId && !routeConversationId) {
      setMessages([]);
      setConversationId(null);
      try { settings.remove('chat.conversationId'); } catch {}
    }
  }, [agentId, routeConversationId, setConversationId, setMessages, settings]);

  // Reset plan/tool state when agent or conversation changes
  useEffect(() => {
    setPlanSteps([]);
    setToolActions([]);
  }, [agentId, conversationId]);

  // Persist conversation id locally for quick resume
  useEffect(() => {
    if (!agentId) return;
    if (conversationId) {
      settings.set('chat.conversationId', conversationId);
    } else {
      try { settings.remove('chat.conversationId'); } catch {}
    }
  }, [agentId, conversationId, settings]);

  // Guard against invalid conversation access
  useEffect(() => {
    if (!accessToken || !conversationId) return;
    let cancelled = false;
    (async () => {
      try {
        const res = await fetch(`/api/conversations/${conversationId}`, { headers: { Authorization: `Bearer ${accessToken}` } });
        if (!res.ok && !cancelled) {
          navigate('/', { replace: true });
        }
      } catch {
        if (!cancelled) navigate('/', { replace: true });
      }
    })();
    return () => { cancelled = true; };
  }, [accessToken, conversationId, navigate]);

  // Load persona details (voice/gender) for the active agent persona
  useEffect(() => {
    if (!accessToken || !activePersonaId) {
      setAssistantGender(undefined);
      setAssistantVoiceName(undefined);
      return;
    }
    let cancelled = false;
    (async () => {
      try {
        const res = await fetch(`/api/personas/${activePersonaId}`, { headers: { Authorization: `Bearer ${accessToken}` } });
        if (!res.ok || cancelled) {
          setAssistantGender(undefined);
          setAssistantVoiceName(undefined);
          return;
        }
        const data = await res.json();
        const genderRaw = data?.gender ?? data?.Gender;
        const voiceRaw = data?.voice ?? data?.Voice;
        const gender = typeof genderRaw === 'string' ? genderRaw.toLowerCase() : '';
        const normalizedGender = gender.startsWith('f') ? 'female' : (gender.startsWith('m') ? 'male' : undefined);
        setAssistantGender(normalizedGender);
        setAssistantVoiceName(typeof voiceRaw === 'string' ? voiceRaw : undefined);
      } catch {
        if (!cancelled) {
          setAssistantGender(undefined);
          setAssistantVoiceName(undefined);
        }
      }
    })();
    return () => { cancelled = true; };
  }, [accessToken, activePersonaId]);

  // Hub connection state forwarding
  useChatBus('connection-state', evt => setHubState(evt.state));

  const pendingAssistantTimerRef = useRef<number | null>(null);
  const deltaBufferRef = useRef<string>('');
  const deltaRafRef = useRef<number | null>(null);

  const flushDelta = () => {
    const delta = deltaBufferRef.current;
    if (!delta) return;
    deltaBufferRef.current = '';
    setMessages(prev => {
      const updated = prev.map(m => ({ ...m, role: normalizeRole(m.role) }));
      const idx = updated.findIndex(m => m.role === 'assistant' && (m as any).pending);
      if (idx !== -1) {
        const copy = [...updated];
        const current = copy[idx];
        copy[idx] = { ...current, content: (current.content || '') + delta } as MessageItemProps;
        return copy;
      }
      const assistantMsg: MessageItemProps = {
        role: 'assistant',
        content: delta,
        fromName: assistantName,
        timestamp: new Date().toISOString(),
        pending: true,
      } as any;
      return [...updated, assistantMsg];
    });
  };

  useChatBus('assistant-delta', evt => {
    if (!evt || evt.conversationId !== (conversationId ?? '')) return;
    deltaBufferRef.current += evt.text || '';
    if (deltaRafRef.current == null) {
      deltaRafRef.current = requestAnimationFrame(() => {
        deltaRafRef.current = null;
        flushDelta();
      });
    }
  });

  useChatBus('assistant-message', msg => {
    if (!msg || msg.conversationId !== (conversationId ?? '')) return;
    setMessages(prev => {
      const updated = prev.map(m => ({ ...m, role: normalizeRole(m.role) }));
      const pendingIdx = updated.findIndex(m => m.role === 'assistant' && (m as any).pending);
      if (pendingIdx !== -1) {
        const copy = [...updated];
        const current = copy[pendingIdx] as MessageItemProps;
        const baseVersions: string[] = Array.isArray(current.versions) && current.versions.length ? current.versions : (current.content ? [current.content] : []);
        const nextVersions = baseVersions.length ? [...baseVersions.slice(0, baseVersions.length - 1), msg.content] : [msg.content];
        copy[pendingIdx] = {
          ...current,
          id: (msg as any).messageId,
          pending: false,
          content: msg.content,
          timestamp: msg.timestamp,
          versions: nextVersions,
          versionIndex: Math.max(0, nextVersions.length - 1),
        };
        return copy;
      }
      const assistantMsg: MessageItemProps = {
        role: 'assistant',
        id: (msg as any).messageId,
        content: msg.content,
        fromName: assistantName,
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
    // Refresh title if server auto-titled
    const convId = conversationId ?? '';
    if (!convId || !accessToken) return;
    const existing = conversations.find(c => c.id === convId);
    if (existing && existing.title) return;
    setTimeout(async () => {
      try {
        const res = await fetch(`/api/conversations/${convId}`, { headers: { Authorization: `Bearer ${accessToken}` } });
        if (res.ok) {
          const data = await res.json();
          const title = data?.title ?? data?.Title;
          if (title) {
            setConversations(prev => prev.map(c => (c.id === convId ? { ...c, title } : c)));
          }
        }
      } catch {}
    }, 300);
  });

  useChatBus('conversation-updated', evt => {
    if (!evt) return;
    const cid = String(evt.conversationId || '');
    if (!cid) return;
    const title = (evt.title ?? '').toString();
    setConversations(prev => prev.map(c => (c.id === cid ? { ...c, title } : c)));
  });

  useChatBus('plan-ready', evt => {
    if (!evt || evt.conversationId !== (conversationId ?? '')) return;
    const steps = Array.isArray(evt.plan) ? evt.plan : [evt.plan];
    setPlanSteps(steps.filter(Boolean));
  });

  useChatBus('tool-requested', evt => {
    if (!evt || evt.conversationId !== (conversationId ?? '')) return;
    setToolActions(prev => [...prev, `Tool requested: ${evt.toolId}`]);
  });

  useChatBus('tool-completed', evt => {
    if (!evt || evt.conversationId !== (conversationId ?? '')) return;
    setToolActions(prev => [...prev, `Tool completed: ${evt.toolId} (${evt.success ? 'success' : 'error'})`]);
  });

  useChatBus('assistant-version-appended', evt => {
    if (!evt || evt.conversationId !== (conversationId ?? '')) return;
    setMessages(prev => {
      const copy = [...prev];
      const idx = copy.findIndex(m => (m as any).id === evt.messageId);
      if (idx === -1) return prev;
      const current: any = copy[idx];
      const base = Array.isArray(current.versions) ? current.versions : (current.content ? [current.content] : []);
      const next = [...base];
      const targetIndex = typeof evt.versionIndex === 'number' ? evt.versionIndex : next.length;
      next[targetIndex] = evt.content;
      copy[idx] = { ...current, versions: next, versionIndex: targetIndex, content: evt.content };
      return copy;
    });
  });

  useChatBus('assistant-version-activated', evt => {
    if (!evt || evt.conversationId !== (conversationId ?? '')) return;
    setMessages(prev => {
      const copy = [...prev];
      const idx = copy.findIndex(m => (m as any).id === evt.messageId);
      if (idx === -1) return prev;
      const current: any = copy[idx];
      const versions: string[] = Array.isArray(current.versions) ? current.versions : (current.content ? [current.content] : []);
      const targetIndex = Math.max(0, Math.min(versions.length - 1, evt.versionIndex ?? 0));
      copy[idx] = { ...current, versionIndex: targetIndex, content: versions[targetIndex] };
      return copy;
    });
  });

  useChatBus('conversation-created', evt => {
    if (!evt) return;
    setConversationId(evt.conversationId);
    try { settings.set('chat.conversationId', evt.conversationId); } catch {}
  });

  useChatBus('conversation-joined', evt => {
    if (!evt) return;
    setConversationId(evt.conversationId);
    try { settings.set('chat.conversationId', evt.conversationId); } catch {}
  });

  useChatBus('conversation-left', evt => {
    if (!evt) return;
    if (evt.conversationId === (conversationId ?? '')) {
      setConversationId(null);
      try { settings.remove('chat.conversationId'); } catch {}
    }
  });

  const handleSendMessage = async (text: string) => {
    if (!accessToken || !text.trim() || !agentId) return;
    const personaForAgent = activePersonaId;
    if (!personaForAgent) {
      setError('Selected agent is missing a persona mapping.');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      let convId = conversationId;
      if (!convId) {
        const res = await fetch('/api/conversations', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` },
          body: JSON.stringify({ AgentId: agentId, Title: null, ParticipantIds: personaForAgent ? [personaForAgent] : [] }),
        });
        if (!res.ok) {
          const err = await res.text();
          throw new Error(err || 'Failed to create conversation');
        }
        const body = await res.json();
        const createdId = String(body?.id ?? body?.Id ?? '').trim();
        if (!createdId) throw new Error('Conversation id missing from response');
        convId = createdId;
        setConversationId(convId);
        setConversations(prev => (convId && prev.every(c => c.id !== convId) ? [{ id: convId, title: null }, ...prev] : prev));
        try { settings.set('chat.conversationId', convId); } catch {}
        try { await chatHub.connectionRef.current?.invoke('JoinConversation', convId); } catch {}
      }

      const placeholderId = `pending-${Date.now()}-${Math.random().toString(36).slice(2)}`;
      setMessages(prev => ([
        ...prev,
        { role: 'user', content: text, fromId: auth?.userId, fromName: 'You', timestamp: new Date().toISOString() } as MessageItemProps,
        { role: 'assistant', content: '', fromName: assistantName, pending: true, localId: placeholderId } as any,
      ]));

      const sendSucceeded = await (async () => {
        if (convId) {
          try {
            await chatHub.connectionRef.current?.invoke('AppendUserMessage', convId, text, personaForAgent, providerId, modelId);
            return true;
          } catch {}
        }
        return await chatHub.sendUserMessage(text, personaForAgent, providerId, modelId);
      })();

      if (!sendSucceeded && convId) {
        try {
          const resp = await fetch('/api/chat/ask-chat', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` },
            body: JSON.stringify({ ConversationId: convId, ProviderId: providerId, ModelId: modelId || null, Input: text }),
          });
          if (resp.ok) {
            const data = await resp.json();
            const reply = String(data?.content?.Reply ?? data?.content?.reply ?? '').trim();
            if (reply) {
              setMessages(prev => prev.map(m => ((m as any).localId === placeholderId && (m as any).pending)
                ? ({ ...m, pending: false, content: reply } as any)
                : m));
            }
          }
        } catch {}
      }

      if (pendingAssistantTimerRef.current) clearTimeout(pendingAssistantTimerRef.current);
      pendingAssistantTimerRef.current = window.setTimeout(() => {
        setMessages(prev => prev.map(m => (m.role === 'assistant' && (m as any).localId === placeholderId && (m as any).pending)
          ? { ...m, pending: false, content: 'Error sending message.' }
          : m));
        pendingAssistantTimerRef.current = null;
      }, 8000) as any;
    } catch (e: any) {
      setError(e?.message || 'Failed to send message');
    } finally {
      setBusy(false);
    }
  };

  const handleGenerateImage = async () => {
    await generateFromChat();
  };

  const handleRememberLast = async () => {
    try {
      if (!accessToken || !conversationId) return;
      const lastAssistant = [...messages].reverse().find(m => normalizeRole(m.role) === 'assistant');
      const content = lastAssistant?.content?.trim();
      if (!content) return;
      await fetch('/api/chat/remember', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` },
        body: JSON.stringify({ ConversationId: conversationId, AgentId: agentId, Content: content, Metadata: { Source: 'ui_remember' } }),
      });
    } catch {}
  };

  return (
    <ChatLayout
      agents={agents}
      agentId={agentId}
      onAgentChange={nextId => {
        if (nextId && nextId !== agentId) {
          setAgentId(nextId);
        }
      }}
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
      loading={loadingAgents}
      planSteps={planSteps}
      toolActions={toolActions}
      conversations={conversations}
      conversationId={conversationId ?? null}
      onConversationChange={id => {
        setMessages([]);
        setConversationId(id);
      }}
      imgStyles={imgStyles}
      imgStyleId={imgStyleId}
      onImgStyleChange={setImgStyleId}
      imgPending={imgPending}
      onGenerateImage={(model, count) => { generateFromChat(model, count); }}
      onRememberLast={handleRememberLast}
      onNewConversation={() => {
        setMessages([]);
        setConversationId(null);
        try { settings.remove('chat.conversationId'); } catch {}
      }}
      connectionState={hubState}
      assistantVoiceName={assistantVoiceName}
      assistantGender={assistantGender}
      onRegenerate={idx => {
        const personaForAgent = activePersonaId;
        if (!accessToken || !agentId || !personaForAgent) return;
        const previousUserIdx = (() => {
          for (let i = idx - 1; i >= 0; i -= 1) {
            if (normalizeRole(messages[i].role) === 'user') return i;
          }
          return -1;
        })();
        const prevContent = previousUserIdx >= 0 ? messages[previousUserIdx].content : '';
        (async () => {
          try {
            const resp = await fetch('/api/chat/ask', {
              method: 'POST',
              headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` },
              body: JSON.stringify({ AgentId: agentId, PersonaId: personaForAgent, ProviderId: providerId, ModelId: modelId || null, Input: prevContent || 'Regenerate the previous assistant response with a different approach.' }),
            });
            if (!resp.ok) return;
            const data = await resp.json();
            const text = String(data?.reply ?? data?.content ?? '').trim();
            if (!text) return;
            const msgId = (messages[idx] as any).id as string | undefined;
            if (conversationId && msgId) {
              try {
                await fetch(`/api/conversations/${conversationId}/messages/${msgId}/versions`, {
                  method: 'POST',
                  headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` },
                  body: JSON.stringify({ Content: text }),
                });
              } catch {}
            }
          } catch {}
        })();
      }}
      onPrevVersion={idx => {
        const msgId = (messages[idx] as any).id as string | undefined;
        if (conversationId && msgId && accessToken) {
          const current = typeof (messages[idx] as any).versionIndex === 'number' ? (messages[idx] as any).versionIndex : 0;
          const target = Math.max(0, current - 1);
          fetch(`/api/conversations/${conversationId}/messages/${msgId}/active-version`, {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` },
            body: JSON.stringify({ Index: target }),
          }).catch(() => {});
        }
      }}
      onNextVersion={idx => {
        const msgId = (messages[idx] as any).id as string | undefined;
        if (conversationId && msgId && accessToken) {
          const current = typeof (messages[idx] as any).versionIndex === 'number' ? (messages[idx] as any).versionIndex : 0;
          const target = current + 1;
          fetch(`/api/conversations/${conversationId}/messages/${msgId}/active-version`, {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` },
            body: JSON.stringify({ Index: target }),
          }).catch(() => {});
        }
      }}
    />
  );
}
