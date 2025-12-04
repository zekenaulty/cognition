import { useCallback, useEffect, useState, type Dispatch, type SetStateAction } from 'react';
import { ApiError, fictionApi } from '../api/client';
import {
  AuthorPersonaContext,
  BacklogActionLog,
  FictionBacklogItem,
  FictionPlanRoster,
  FictionPlanSummary,
  LoreFulfillmentLog,
  PersonaObligation,
} from '../types/fiction';
import { ResumeBacklogPayload } from '../types/fiction';
import { ResolvePersonaObligationPayload } from '../types/fiction';

type PlanState = {
  plans: FictionPlanSummary[];
  plansLoading: boolean;
  plansError: string | null;
  selectedPlanId: string | null;
  setSelectedPlanId: Dispatch<SetStateAction<string | null>>;
  roster: FictionPlanRoster | null;
  rosterLoading: boolean;
  rosterError: string | null;
  personaContext: AuthorPersonaContext | null;
  personaLoading: boolean;
  personaError: string | null;
  backlogItems: FictionBacklogItem[];
  backlogLoading: boolean;
  backlogError: string | null;
  actionLogs: BacklogActionLog[];
  actionLoading: boolean;
  actionError: string | null;
  personaObligations: PersonaObligation[];
  obligationsLoading: boolean;
  obligationsError: string | null;
  loreHistory: Record<string, LoreFulfillmentLog[]>;
  loreHistoryLoading: boolean;
  loreHistoryError: string | null;
  refreshPlans: (nextSelectedId?: string | null) => Promise<void>;
  refreshPlanContext: (planId: string) => Promise<void>;
  resumeBacklog: (planId: string, backlogId: string, payload: ResumeBacklogPayload) => Promise<void>;
  resolveObligation: (planId: string, target: PersonaObligation, action: ResolvePersonaObligationPayload['action']) => Promise<PersonaObligation | null>;
};

export function useFictionProjectsData(token?: string | null): PlanState {
  const [plans, setPlans] = useState<FictionPlanSummary[]>([]);
  const [plansLoading, setPlansLoading] = useState(true);
  const [plansError, setPlansError] = useState<string | null>(null);
  const [selectedPlanId, setSelectedPlanId] = useState<string | null>(null);

  const [roster, setRoster] = useState<FictionPlanRoster | null>(null);
  const [rosterLoading, setRosterLoading] = useState(false);
  const [rosterError, setRosterError] = useState<string | null>(null);

  const [personaContext, setPersonaContext] = useState<AuthorPersonaContext | null>(null);
  const [personaLoading, setPersonaLoading] = useState(false);
  const [personaError, setPersonaError] = useState<string | null>(null);

  const [backlogItems, setBacklogItems] = useState<FictionBacklogItem[]>([]);
  const [backlogLoading, setBacklogLoading] = useState(false);
  const [backlogError, setBacklogError] = useState<string | null>(null);

  const [actionLogs, setActionLogs] = useState<BacklogActionLog[]>([]);
  const [actionLoading, setActionLoading] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const [personaObligations, setPersonaObligations] = useState<PersonaObligation[]>([]);
  const [obligationsLoading, setObligationsLoading] = useState(false);
  const [obligationsError, setObligationsError] = useState<string | null>(null);

  const [loreHistory, setLoreHistory] = useState<Record<string, LoreFulfillmentLog[]>>({});
  const [loreHistoryLoading, setLoreHistoryLoading] = useState(false);
  const [loreHistoryError, setLoreHistoryError] = useState<string | null>(null);

  const resetPlanContext = useCallback(() => {
    setRoster(null);
    setRosterError(null);
    setRosterLoading(false);
    setPersonaContext(null);
    setPersonaError(null);
    setPersonaLoading(false);
    setBacklogItems([]);
    setBacklogError(null);
    setBacklogLoading(false);
    setActionLogs([]);
    setActionError(null);
    setActionLoading(false);
    setPersonaObligations([]);
    setObligationsError(null);
    setObligationsLoading(false);
    setLoreHistory({});
    setLoreHistoryError(null);
    setLoreHistoryLoading(false);
  }, []);

  const refreshPlans = useCallback(async (nextSelectedId?: string | null) => {
    if (!token) {
      setPlans([]);
      setSelectedPlanId(null);
      return;
    }
    setPlansLoading(true);
    setPlansError(null);
    try {
      const data = await fictionApi.listPlans(token);
      setPlans(data);
      setSelectedPlanId(prev => {
        if (nextSelectedId && data.some(plan => plan.id === nextSelectedId)) {
          return nextSelectedId;
        }
        if (prev && data.some(plan => plan.id === prev)) {
          return prev;
        }
        return data.length > 0 ? data[0].id : null;
      });
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Unable to load fiction plans.';
      setPlansError(message);
      setPlans([]);
      setSelectedPlanId(null);
    } finally {
      setPlansLoading(false);
    }
  }, [token]);

  useEffect(() => {
    refreshPlans();
  }, [refreshPlans]);

  const refreshPlanContext = useCallback(async (planId: string) => {
    if (!token || !planId) {
      resetPlanContext();
      return;
    }

    setRosterLoading(true);
    setPersonaLoading(true);
    setBacklogLoading(true);
    setActionLoading(true);
    setObligationsLoading(true);
    setLoreHistoryLoading(true);

    setRosterError(null);
    setPersonaError(null);
    setBacklogError(null);
    setActionError(null);
    setObligationsError(null);
    setLoreHistoryError(null);

    const currentPlan = planId;
    try {
      const [
        rosterData,
        personaData,
        backlogData,
        actionLogData,
        obligationData,
        loreHistoryData,
      ] = await Promise.all([
        fictionApi.getPlanRoster(currentPlan, token),
        fictionApi.getAuthorPersonaContext(currentPlan, token),
        fictionApi.getPlanBacklog(currentPlan, token),
        fictionApi.getBacklogActions(currentPlan, token),
        fictionApi.getPersonaObligations(currentPlan, token),
        fictionApi.getLoreHistory(currentPlan, token),
      ]);

      setRoster(rosterData);
      setPersonaContext(personaData);
      setBacklogItems(backlogData);
      setActionLogs(actionLogData);
      setPersonaObligations(obligationData);
      setLoreHistory(loreHistoryData);
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Unable to load plan context.';
      setRosterError(message);
      setPersonaError(message);
      setBacklogError(message);
      setActionError(message);
      setObligationsError(message);
      setLoreHistoryError(message);
    } finally {
      setRosterLoading(false);
      setPersonaLoading(false);
      setBacklogLoading(false);
      setActionLoading(false);
      setObligationsLoading(false);
      setLoreHistoryLoading(false);
    }
  }, [resetPlanContext, token]);

  useEffect(() => {
    if (!token || !selectedPlanId) {
      resetPlanContext();
      return;
    }
    refreshPlanContext(selectedPlanId);
  }, [token, selectedPlanId, refreshPlanContext, resetPlanContext]);

  const resumeBacklog = useCallback(async (planId: string, backlogId: string, payload: ResumeBacklogPayload) => {
    if (!token) return;
    await fictionApi.resumeBacklog(planId, backlogId, payload, token);
    await refreshPlanContext(planId);
  }, [token, refreshPlanContext]);

  const resolveObligation = useCallback(async (planId: string, target: PersonaObligation, action: ResolvePersonaObligationPayload['action']) => {
    if (!token) return null;
    const payload: ResolvePersonaObligationPayload = { action, metadata: { Source: 'fiction_projects' } };
    const updated = await fictionApi.resolvePersonaObligation(planId, target.id, payload, token);
    setPersonaObligations(prev => prev.map(o => (o.id === updated.id ? updated : o)));
    return updated;
  }, [token]);

  return {
    plans,
    plansLoading,
    plansError,
    selectedPlanId,
    setSelectedPlanId,
    roster,
    rosterLoading,
    rosterError,
    personaContext,
    personaLoading,
    personaError,
    backlogItems,
    backlogLoading,
    backlogError,
    actionLogs,
    actionLoading,
    actionError,
    personaObligations,
    obligationsLoading,
    obligationsError,
    loreHistory,
    loreHistoryLoading,
    loreHistoryError,
    refreshPlans,
    refreshPlanContext,
    resumeBacklog,
    resolveObligation,
  };
}
