import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { ChatPageView } from '../components/chat/ChatPageView';
import { useAuth } from '../auth/AuthContext';
import { useChatHub } from '../hooks/useChatHub';
import { useImageStyles } from '../hooks/useImageStyles';
import { useImageGenerator } from '../hooks/useImageGenerator';
import { useChatProviderModel } from '../hooks/useChatProviderModel';
import { MessageItemProps } from '../components/chat/MessageItem';
import { normalizeRole } from '../utils/chat';
import { useActiveAgent } from '../hooks/useActiveAgent';
import { useConversationManager } from '../hooks/useConversationManager';
import { useChatHubEvents } from '../hooks/useChatHubEvents';


export default function ChatPage() {
  const { auth } = useAuth();
  const accessToken = auth?.accessToken || '';
  const { agentId: routeAgentId, conversationId: routeConversationId } = useParams<{ agentId?: string; conversationId?: string }>();

  const [planSteps, setPlanSteps] = useState<string[]>([]);
  const [toolActions, setToolActions] = useState<string[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hubState, setHubState] = useState<'connecting' | 'connected' | 'reconnecting' | 'disconnected'>('disconnected');

  const {
    agents,
    loadingAgents,
    agentId,
    setAgentId,
    activePersonaId,
    assistantName,
    assistantGender,
    assistantVoiceName,
    resetConversationToken,
  } = useActiveAgent({
    accessToken,
    routeAgentId,
    routeConversationId,
  });

  const {
    conversations,
    conversationId,
    setConversationId,
    setConversations,
    messages,
    setMessages,
  } = useConversationManager({
    accessToken,
    agentId,
    resetConversationToken,
    routeConversationId: routeConversationId || undefined,
  });

  const {
    providers,
    providerId,
    setProviderId,
    models,
    modelId,
    setModelId,
    pickPreferredProvider,
    pickPreferredModel,
  } = useChatProviderModel(accessToken, conversationId, conversations, setConversations);

  const chatHub = useChatHub({ conversationId: conversationId ?? '', accessToken });

  useChatHubEvents({
    conversationId,
    accessToken,
    agents,
    assistantName,
    setMessages,
    conversations,
    setConversations,
    setPlanSteps,
    setToolActions,
    setHubState,
  });

  const { imgStyles, imgStyleId, setImgStyleId } = useImageStyles(accessToken);

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

  useEffect(() => {
    setPlanSteps([]);
    setToolActions([]);
  }, [agentId, conversationId]);

  // If the route agent changes, forcibly align agent and clear the in-memory conversation/messages
  useEffect(() => {
    if (routeAgentId && routeAgentId !== agentId) {
      setAgentId(routeAgentId);
      setMessages([]);
      setConversationId(null);
      setPlanSteps([]);
      setToolActions([]);
    }
  }, [routeAgentId, agentId, setAgentId, setMessages, setConversationId]);

  // Normalize any existing messages once on mount
  useEffect(() => {
    setMessages(prev => prev.map(m => ({ ...m, role: normalizeRole(m.role) })));
  }, [setMessages]);

  const handleSendMessage = async (text: string) => {
    if (!accessToken || !text.trim() || !agentId) return;
    const personaForAgent = activePersonaId;
    setBusy(true);
    setError(null);
    try {
      // Ensure provider/model are chosen before first send
      if (!providerId) {
        const nextProvider = pickPreferredProvider();
        if (nextProvider) setProviderId(nextProvider);
      }
      if (!modelId) {
        const nextModel = pickPreferredModel();
        if (nextModel) setModelId(nextModel);
      }

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
        const nextProviderId = providerId || pickPreferredProvider();
        const nextModelId = modelId || pickPreferredModel();
        if (nextProviderId) setProviderId(nextProviderId);
        if (nextModelId) setModelId(nextModelId);
        setConversationId(convId);
        setConversations(prev => {
          const exists = prev.some(c => c.id === convId);
          const entry = { id: convId, title: null as string | null, providerId: nextProviderId ?? undefined, modelId: nextModelId ?? undefined } as any;
          return exists ? prev : [entry, ...prev];
        });
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
            await chatHub.connectionRef.current?.invoke('AppendUserMessage', convId, text, agentId, personaForAgent, providerId, modelId);
            return true;
          } catch {}
        }
        return await chatHub.sendUserMessage(text, agentId, personaForAgent, providerId, modelId);
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
    } catch (e: any) {
      setError(e?.message || 'Failed to send message');
    } finally {
      setBusy(false);
    }
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
    <ChatPageView
      layout={{
        agents,
        agentId,
        showAgentSelector: false,
        providers,
        models,
        providerId,
        modelId,
        onProviderChange: setProviderId,
        onModelChange: setModelId,
        messages,
        onSend: handleSendMessage,
        busy,
        error: error || undefined,
        loading: loadingAgents,
        planSteps,
        toolActions,
        conversations,
        conversationId: conversationId ?? null,
        onConversationChange: id => {
          setMessages([]);
          setConversationId(id);
        },
        imgStyles,
        imgStyleId,
        onImgStyleChange: setImgStyleId,
        imgPending,
        onGenerateImage: (model, count) => { generateFromChat(model, count); },
        onRememberLast: handleRememberLast,
        onNewConversation: () => {
          setMessages([]);
          setConversationId(null);
          const nextProvider = pickPreferredProvider();
          const nextModel = pickPreferredModel();
          if (nextProvider) setProviderId(nextProvider);
          if (nextModel) setModelId(nextModel);
        },
        connectionState: hubState,
        assistantVoiceName,
        assistantGender,
        onRegenerate: idx => {
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
        },
        onPrevVersion: idx => {
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
        },
        onNextVersion: idx => {
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
        },
      }}
    />
  );
}
