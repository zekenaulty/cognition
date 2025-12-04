import { useEffect, useState, type Dispatch, type SetStateAction } from 'react';
import { ApiError, fictionApi } from '../api/client';
import {
  AuthorPersonaContext,
  BacklogActionLog,
  FictionBacklogItem,
  FictionPlanRoster,
  LoreBranchSummary,
  LoreFulfillmentLog,
  PersonaObligation,
  ResumeBacklogPayload,
} from '../types/fiction';

type FictionContextState = {
  rosterPlanId: string | null;
  setRosterPlanId: Dispatch<SetStateAction<string | null>>;
  planRoster: FictionPlanRoster | null;
  planRosterPersona: AuthorPersonaContext | null;
  loreSummary: LoreBranchSummary[];
  planLoreHistory: Record<string, LoreFulfillmentLog[]>;
  planObligations: PersonaObligation[];
  planActionLogs: BacklogActionLog[];
  planBacklogItems: FictionBacklogItem[];
  resumeTarget: FictionBacklogItem | null;
  resumeDefaults: { providerId?: string; modelId?: string | null };
  loading: boolean;
  error: string | null;
  setResumeTarget: Dispatch<SetStateAction<FictionBacklogItem | null>>;
  setResumeDefaults: Dispatch<SetStateAction<{ providerId?: string; modelId?: string | null }>>;
  refreshPlanData: (planId: string) => Promise<void>;
  resumeBacklog: (planId: string, backlogId: string, payload: ResumeBacklogPayload) => Promise<void>;
  resolveObligation: (planId: string, target: PersonaObligation, action: 'resolve' | 'dismiss') => Promise<PersonaObligation | null>;
};

export function usePlannerFictionContext(token?: string | null): FictionContextState {
  const [rosterPlanId, setRosterPlanId] = useState<string | null>(null);
  const [planRoster, setPlanRoster] = useState<FictionPlanRoster | null>(null);
  const [planRosterPersona, setPlanRosterPersona] = useState<AuthorPersonaContext | null>(null);
  const [loreSummary, setLoreSummary] = useState<LoreBranchSummary[]>([]);
  const [planLoreHistory, setPlanLoreHistory] = useState<Record<string, LoreFulfillmentLog[]>>({});
  const [planObligations, setPlanObligations] = useState<PersonaObligation[]>([]);
  const [planActionLogs, setPlanActionLogs] = useState<BacklogActionLog[]>([]);
  const [planBacklogItems, setPlanBacklogItems] = useState<FictionBacklogItem[]>([]);
  const [resumeTarget, setResumeTarget] = useState<FictionBacklogItem | null>(null);
  const [resumeDefaults, setResumeDefaults] = useState<{ providerId?: string; modelId?: string | null }>({});
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const refreshPlanData = async (planId: string) => {
    if (!token || !planId) return;
    setLoading(true);
    setError(null);
    try {
      const [
        roster,
        persona,
        summary,
        loreHistory,
        obligations,
        actionLogs,
        backlog,
      ] = await Promise.all([
        fictionApi.getPlanRoster(planId, token),
        fictionApi.getAuthorPersonaContext(planId, token),
        fictionApi.getLoreSummary(planId, token),
        fictionApi.getLoreHistory(planId, token),
        fictionApi.getPersonaObligations(planId, token),
        fictionApi.getBacklogActions(planId, token),
        fictionApi.getPlanBacklog(planId, token),
      ]);
      setPlanRoster(roster);
      setPlanRosterPersona(persona);
      setLoreSummary(summary);
      setPlanLoreHistory(loreHistory);
      setPlanObligations(obligations);
      setPlanActionLogs(actionLogs);
      setPlanBacklogItems(backlog);
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to load plan data.';
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (token && rosterPlanId) {
      refreshPlanData(rosterPlanId);
    } else {
      setPlanRoster(null);
      setPlanRosterPersona(null);
      setLoreSummary([]);
      setPlanLoreHistory({});
      setPlanObligations([]);
      setPlanActionLogs([]);
      setPlanBacklogItems([]);
      setResumeTarget(null);
      setResumeDefaults({});
      setError(null);
    }
  }, [token, rosterPlanId]);

  const resumeBacklog = async (planId: string, backlogId: string, payload: ResumeBacklogPayload) => {
    if (!token) return;
    await fictionApi.resumeBacklog(planId, backlogId, payload, token);
    await refreshPlanData(planId);
  };

  const resolveObligation = async (planId: string, target: PersonaObligation, action: 'resolve' | 'dismiss') => {
    if (!token) return null;
    const payload = { action, metadata: { Source: 'planner_telemetry' } };
    const updated = await fictionApi.resolvePersonaObligation(planId, target.id, payload, token);
    setPlanObligations(prev => prev.map(o => (o.id === updated.id ? updated : o)));
    return updated;
  };

  return {
    rosterPlanId,
    setRosterPlanId,
    planRoster,
    planRosterPersona,
    loreSummary,
    planLoreHistory,
    planObligations,
    planActionLogs,
    planBacklogItems,
    resumeTarget,
    resumeDefaults,
    loading,
    error,
    setResumeTarget,
    setResumeDefaults,
    refreshPlanData,
    resumeBacklog,
    resolveObligation,
  };
}
