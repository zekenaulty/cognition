import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAgentPersonaIndex } from './useAgentPersonaIndex';
import { useUserSettings } from './useUserSettings';

type Params = {
  accessToken: string;
  routeAgentId?: string;
  routeConversationId?: string;
};

export function useActiveAgent({ accessToken, routeAgentId, routeConversationId }: Params) {
  const navigate = useNavigate();
  const settings = useUserSettings();
  const { agents, loading: loadingAgents, resolvePersonaId } = useAgentPersonaIndex(accessToken);
  const [agentId, setAgentId] = useState<string>('');
  const [assistantGender, setAssistantGender] = useState<string | undefined>();
  const [assistantVoiceName, setAssistantVoiceName] = useState<string | undefined>();
  const previousAgentRef = useRef<string | undefined>();

  const activePersonaId = agentId ? resolvePersonaId(agentId) : undefined;

  const assistantName = useMemo(() => {
    if (agentId) {
      const agent = agents.find(a => a.id === agentId);
      if (agent) return agent.label;
    }
    return 'Assistant';
  }, [agents, agentId]);

  // Choose an agent based on route, saved setting, or first available
  useEffect(() => {
    if (!agents.length) {
      setAgentId('');
      return;
    }
    const routeCandidate = routeAgentId && agents.some(a => a.id === routeAgentId) ? routeAgentId : undefined;
    const saved = settings.get<string>('chat.agentId');
    const savedCandidate = saved && agents.some(a => a.id === saved) ? saved : undefined;
    const next = routeCandidate ?? savedCandidate ?? agents[0].id;
    if (next && next !== agentId) setAgentId(next);
  }, [agents, routeAgentId, agentId, settings]);

  // Persist agent selection and synchronize route
  useEffect(() => {
    if (!agentId) return;
    settings.set('chat.agentId', agentId);
    if (routeConversationId) {
      if (routeAgentId !== agentId) {
        navigate(`/chat/${agentId}/${routeConversationId}`, { replace: true });
      }
    } else if (routeAgentId !== agentId) {
      navigate(`/chat/${agentId}`, { replace: true });
    }
  }, [agentId, navigate, routeAgentId, routeConversationId, settings]);

  // Clear conversation when agent changes without explicit route conversation
  const [resetConversationToken, setResetConversationToken] = useState(0);
  useEffect(() => {
    if (!agentId) return;
    const previous = previousAgentRef.current;
    previousAgentRef.current = agentId;
    if (previous && previous !== agentId && !routeConversationId) {
      setResetConversationToken(t => t + 1);
    }
  }, [agentId, routeConversationId]);

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
