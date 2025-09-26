import { useCallback, useEffect, useMemo, useState } from 'react';

export type AgentPersonaOption = {
  id: string;
  personaId?: string;
  label: string;
};

export function useAgentPersonaIndex(accessToken?: string) {
  const [agents, setAgents] = useState<AgentPersonaOption[]>([]);
  const [agentPersonaMap, setAgentPersonaMap] = useState<Record<string, string>>({});
  const [personaNameMap, setPersonaNameMap] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async (signal?: AbortSignal) => {
    if (!accessToken) {
      if (!signal?.aborted) {
        setAgents([]);
        setAgentPersonaMap({});
        setPersonaNameMap({});
        setError(null);
        setLoading(false);
      }
      return;
    }

    setLoading(true);
    try {
      const headers: HeadersInit = { Authorization: `Bearer ${accessToken}` };
      const [agentsResult, personasResult] = await Promise.allSettled([
        fetch('/api/agents', { headers, signal }),
        fetch('/api/personas', { headers, signal }),
      ]);
      if (signal?.aborted) return;

      let agentItems: any[] = [];
      if (agentsResult.status === 'fulfilled') {
        const res = agentsResult.value;
        if (res.ok) {
          agentItems = await res.json();
        }
      }
      let personaItems: any[] = [];
      if (personasResult.status === 'fulfilled') {
        const res = personasResult.value;
        if (res.ok) {
          personaItems = await res.json();
        }
      }

      if (signal?.aborted) return;

      const personaNames: Record<string, string> = {};
      for (const p of personaItems || []) {
        const id = String(p?.id ?? p?.Id ?? '');
        if (!id) continue;
        const name = String(p?.name ?? p?.Name ?? id.slice(0, 8));
        personaNames[id] = name;
      }

      const normalizedAgents: AgentPersonaOption[] = [];
      const accessiblePersonaIds = new Set(Object.keys(personaNames));
      for (const item of agentItems || []) {
        const id = String(item?.id ?? item?.Id ?? '');
        if (!id) continue;
        const personaId = item?.personaId
          ? String(item.personaId)
          : item?.PersonaId
          ? String(item.PersonaId)
          : undefined;
        if (personaId && !accessiblePersonaIds.has(personaId)) {
          continue;
        }
        const explicitLabel = item?.name ?? item?.Name;
        const label = explicitLabel
          ? String(explicitLabel)
          : personaId
          ? personaNames[personaId] || id.slice(0, 8)
          : id.slice(0, 8);
        normalizedAgents.push({ id, personaId, label });
      }

      const map: Record<string, string> = {};
      normalizedAgents.forEach(agent => {
        if (agent.personaId) map[agent.id] = agent.personaId;
      });

      if (signal?.aborted) return;
      setAgents(normalizedAgents);
      setAgentPersonaMap(map);
      setPersonaNameMap(personaNames);
      setError(null);
    } catch (err: any) {
      if (signal?.aborted) return;
      setAgents([]);
      setAgentPersonaMap({});
      setPersonaNameMap({});
      setError(typeof err?.message === 'string' ? err.message : 'Failed to load agents');
    } finally {
      if (!signal?.aborted) {
        setLoading(false);
      }
    }
  }, [accessToken]);

  useEffect(() => {
    const controller = new AbortController();
    refresh(controller.signal).catch(() => {});
    return () => {
      controller.abort();
    };
  }, [refresh]);

  const resolvePersonaId = useCallback((id: string) => agentPersonaMap[id], [agentPersonaMap]);

  const resolveAgentLabel = useCallback(
    (id: string) => agents.find(agent => agent.id === id)?.label,
    [agents]
  );

  const hasAgents = useMemo(() => agents.length > 0, [agents]);

  return {
    agents,
    agentPersonaMap,
    personaNameMap,
    loading,
    error,
    refresh,
    resolvePersonaId,
    resolveAgentLabel,
    hasAgents,
  };
}
