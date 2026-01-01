import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useConversationsMessages } from './useConversationsMessages';

type Params = {
  accessToken: string;
  agentId: string;
  resetConversationToken: number;
  routeConversationId?: string;
};

export function useConversationManager({ accessToken, agentId, resetConversationToken, routeConversationId }: Params) {
  const navigate = useNavigate();

  const {
    conversations,
    conversationId,
    setConversationId,
    messages,
    setMessages,
    setConversations,
  } = useConversationsMessages(accessToken, agentId);

  // When route conversation id changes, hydrate it
  useEffect(() => {
    if (routeConversationId && routeConversationId !== conversationId) {
      setMessages([]);
      setConversationId(routeConversationId);
    }
  }, [routeConversationId, conversationId, setConversationId, setMessages]);

  // Clear state when agent change requested a reset
  useEffect(() => {
    if (resetConversationToken > 0 && !routeConversationId) {
      setMessages([]);
      setConversationId(null);
    }
  }, [resetConversationToken, routeConversationId, setConversationId, setMessages]);

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

  return {
    conversations,
    conversationId,
    setConversationId,
    setConversations,
    messages,
    setMessages,
  };
}
