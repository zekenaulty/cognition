import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAgentPersonaIndex } from './useAgentPersonaIndex';

type Params = {
  accessToken: string;
  routeAgentId?: string;
  routeConversationId?: string;
};

export function useActiveAgent({ accessToken, routeAgentId, routeConversationId }: Params) {
  const navigate = useNavigate();
  const { agents, loading: loadingAgents, resolvePersonaId } = useAgentPersonaIndex(accessToken);
  const [agentId, setAgentId] = useState<string>('');
  const [assistantGender, setAssistantGender] = useState<string | undefined>();
  const [assistantVoiceName, setAssistantVoiceName] = useState<string | undefined>();
  const previousAgentRef = useRef<string | undefined>();
  const previousRouteAgentRef = useRef<string | undefined>();

  const activePersonaId = agentId ? resolvePersonaId(agentId) : undefined;

  const assistantName = useMemo(() => {
    if (agentId) {
      const agent = agents.find(a => a.id === agentId);
      if (agent) return agent.label;
    }
    return 'Assistant';
  }, [agents, agentId]);

  // Choose an agent based on route (authoritative) or first available when no route is provided
  useEffect(() => {
    if (!agents.length) {
      setAgentId('');
      return;
    }
    const routeCandidate = routeAgentId && agents.some(a => a.id === routeAgentId) ? routeAgentId : undefined;
    const next = routeCandidate ?? agents[0].id;
    if (next && next !== agentId) {
      setAgentId(next);
      // Only push a route when no route agent is provided (fresh entry)
      if (!routeAgentId) {
        if (routeConversationId) {
          navigate(`/chat/${next}/${routeConversationId}`, { replace: true });
        } else {
          navigate(`/chat/${next}`, { replace: true });
        }
      }
    }
  }, [agents, routeAgentId, routeConversationId, agentId, navigate]);

  // Clear conversation when agent changes without explicit route conversation
  const [resetConversationToken, setResetConversationToken] = useState(0);
  useEffect(() => {
    if (!agentId) return;
    const previous = previousAgentRef.current;
    previousAgentRef.current = agentId;
    const routeChanged = routeAgentId && previousRouteAgentRef.current && previousRouteAgentRef.current !== routeAgentId;
    if (routeAgentId) {
      previousRouteAgentRef.current = routeAgentId;
    }
    const shouldReset = (previous && previous !== agentId) || (routeChanged && !routeConversationId);
    if (shouldReset) {
      setResetConversationToken(t => t + 1);
    }
  }, [agentId, routeConversationId, routeAgentId]);

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

  return {
    agents,
    loadingAgents,
    agentId,
    setAgentId,
    activePersonaId,
    assistantName,
    assistantGender,
    assistantVoiceName,
    resetConversationToken,
  };
}
