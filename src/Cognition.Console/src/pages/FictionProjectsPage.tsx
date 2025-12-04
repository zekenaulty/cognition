import React from 'react';
import {
  Alert,
  Box,
  Button,
  Grid,
  Stack,
  Typography
} from '@mui/material';
import { fictionApi } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { useSecurity } from '../hooks/useSecurity';
import {
  FictionBacklogItem,
  FictionLoreRequirementItem,
  FictionPlanSummary,
  ResumeBacklogPayload,
  PersonaObligation
} from '../types/fiction';
import { FictionResumeBacklogDialog } from '../components/fiction/FictionResumeBacklogDialog';
import { PersonaObligationActionDialog } from '../components/fiction/PersonaObligationActionDialog';
import { FictionPlanWizardDialog } from '../components/fiction/FictionPlanWizardDialog';
import { useFictionProjectsData } from '../hooks/useFictionProjectsData';
import { FictionPlanListCard } from '../components/fiction/FictionPlanListCard';
import { FictionBacklogCard } from '../components/fiction/FictionBacklogCard';
import { FictionRosterPersonaSection } from '../components/fiction/FictionRosterPersonaSection';

export default function FictionProjectsPage() {
  const { auth } = useAuth();
  const { isAdmin } = useSecurity();
  const token = auth?.accessToken;

  const {
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
  } = useFictionProjectsData(token);
  const [obligationActionId, setObligationActionId] = React.useState<string | null>(null);
  const [obligationActionError, setObligationActionError] = React.useState<string | null>(null);
  const [obligationDialog, setObligationDialog] = React.useState<{
    obligation: PersonaObligation;
    action: 'resolve' | 'dismiss';
  } | null>(null);
  const [resumeTarget, setResumeTarget] = React.useState<FictionBacklogItem | null>(null);
  const [resuming, setResuming] = React.useState(false);
  const [resumeError, setResumeError] = React.useState<string | null>(null);
  const [resumeDefaults, setResumeDefaults] = React.useState<{ providerId?: string; modelId?: string | null; branchSlug?: string }>({});
  const [planDialogOpen, setPlanDialogOpen] = React.useState(false);
  const rosterCardRef = React.useRef<HTMLDivElement | null>(null);

  const handleResume = React.useCallback((item: FictionBacklogItem) => {
    setResumeError(null);
    setResumeTarget(item);
  }, []);

  const persistResumeDefaults = React.useCallback((defaults: { providerId?: string; modelId?: string | null; branchSlug?: string }) => {
    setResumeDefaults(defaults);
    try {
      localStorage.setItem('fiction.resumeDefaults', JSON.stringify(defaults));
    } catch {
      // ignore storage errors
    }
  }, []);

  React.useEffect(() => {
    try {
      const raw = localStorage.getItem('fiction.resumeDefaults');
      if (raw) {
        const parsed = JSON.parse(raw);
        if (parsed && typeof parsed === 'object') {
          setResumeDefaults(parsed);
        }
      }
    } catch {
      // ignore storage errors
    }
  }, []);

  const handleResumeSubmit = React.useCallback(
    async (payload: ResumeBacklogPayload) => {
      if (!token || !selectedPlanId || !resumeTarget) {
        return;
      }
      setResuming(true);
      setResumeError(null);
      try {
        await resumeBacklog(selectedPlanId, resumeTarget.backlogId, payload);
        persistResumeDefaults({ providerId: payload.providerId, modelId: payload.modelId ?? null, branchSlug: payload.branchSlug });
        setResumeTarget(null);
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Backlog resume failed.';
        setResumeError(message);
      } finally {
        setResuming(false);
      }
    },
    [token, selectedPlanId, resumeTarget, resumeBacklog, persistResumeDefaults]
  );

  const handleResumeDialogClose = React.useCallback(() => {
    if (resuming) {
      return;
    }
    setResumeTarget(null);
    setResumeError(null);
  }, [resuming]);

  React.useEffect(() => {
    setResumeTarget(null);
    setResumeError(null);
    setObligationActionError(null);
    setObligationDialog(null);
    setResumeDefaults({});
  }, [selectedPlanId, token]);

  const handleFulfillLore = React.useCallback(
    async (requirement: FictionLoreRequirementItem) => {
      if (!token || !selectedPlanId) {
        return;
      }
      try {
        await fictionApi.fulfillLoreRequirement(
          selectedPlanId,
          requirement.id,
          {
            branchSlug: requirement.branchSlug ?? roster?.branchSlug,
            branchLineage: requirement.branchLineage ?? null,
            source: 'console'
          },
          token
        );
        await refreshPlanContext(selectedPlanId);
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Unable to fulfill lore requirement.';
        setResumeError(message);
      }
    },
    [token, selectedPlanId, roster?.branchSlug, refreshPlanContext]
  );

  const handleObligationActionRequest = React.useCallback((obligation: PersonaObligation, action: 'resolve' | 'dismiss') => {
    setObligationActionError(null);
    setObligationDialog({ obligation, action });
  }, []);

  const handleObligationDialogSubmit = React.useCallback(
    async (_payload: { notes: string; voiceDrift?: boolean | null }) => {
      if (!token || !selectedPlanId || !obligationDialog) {
        return;
      }
      setObligationActionId(obligationDialog.obligation.id);
      setObligationActionError(null);
      try {
        await resolveObligation(selectedPlanId, obligationDialog.obligation, obligationDialog.action);
        setObligationDialog(null);
        await refreshPlanContext(selectedPlanId);
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Unable to update persona obligation.';
        setObligationActionError(message);
      } finally {
        setObligationActionId(null);
      }
    },
    [token, selectedPlanId, obligationDialog, resolveObligation, refreshPlanContext]
  );

  const handleObligationDialogClose = React.useCallback(() => {
    if (obligationActionId) {
      return;
    }
    setObligationDialog(null);
  }, [obligationActionId]);

  const handlePlanCreated = React.useCallback(
    (plan: FictionPlanSummary) => {
      refreshPlans(plan.id);
      setPlanDialogOpen(false);
    },
    [refreshPlans]
  );

  const scrollToRoster = React.useCallback(() => {
    rosterCardRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }, []);

  const staleBacklogItems = React.useMemo(() => {
    if (!backlogItems || backlogItems.length === 0) {
      return [];
    }
    const now = Date.now();
    const thresholdMs = 60 * 60 * 1000;
    return backlogItems.filter(item => {
      const status = (item.status ?? '').toString().toLowerCase();
      if (status !== 'in_progress') {
        return false;
      }
      const stamp = item.updatedAtUtc ?? item.createdAtUtc;
      if (!stamp) {
        return false;
      }
      const updated = new Date(stamp);
      if (Number.isNaN(updated.getTime())) {
        return false;
      }
      return now - updated.getTime() > thresholdMs;
    });
  }, [backlogItems]);

  const missingLoreRequirements = React.useMemo(
    () =>
      roster?.loreRequirements?.filter(req => (req.status ?? '').toLowerCase() !== 'ready') ?? [],
    [roster]
  );

  const openPersonaObligations = React.useMemo(
    () =>
      personaObligations.filter(obligation => {
        const normalized = (obligation.status ?? '').toLowerCase();
        return normalized !== 'resolved' && normalized !== 'dismissed';
      }),
    [personaObligations]
  );

  const handleResumeAlertClick = React.useCallback(
    (item: FictionBacklogItem) => {
      setResumeError(null);
      setResumeTarget(item);
    },
    []
  );

  const handleMissingLoreAlertClick = React.useCallback(() => {
    scrollToRoster();
  }, [scrollToRoster]);

  const handleObligationAlertClick = React.useCallback(() => {
    if (openPersonaObligations.length === 0 || obligationActionId) {
      return;
    }
    setObligationDialog({ obligation: openPersonaObligations[0], action: 'resolve' });
  }, [openPersonaObligations, obligationActionId]);

  const showPlanAlerts =
    Boolean(selectedPlanId) &&
    (staleBacklogItems.length > 0 ||
      missingLoreRequirements.length > 0 ||
      openPersonaObligations.length > 0);

  if (!isAdmin) {
    return (
      <Alert severity="info">
        Admin privileges are required to inspect fiction plan rosters.
      </Alert>
    );
  }

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="h4" sx={{ fontWeight: 600 }}>
          Fiction Projects
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Inspect backlog readiness, lore fulfillment, and persona context for each fiction plan.
        </Typography>
      </Box>
      {showPlanAlerts && (
        <Stack spacing={1}>
          {staleBacklogItems.length > 0 && (
            <Alert
              severity="warning"
              action={
                <Button
                  color="inherit"
                  size="small"
                  onClick={() => handleResumeAlertClick(staleBacklogItems[0])}
                >
                  Resume
                </Button>
              }
            >
              {staleBacklogItems.length} backlog item{staleBacklogItems.length === 1 ? '' : 's'} have been
              in progress for over an hour.
            </Alert>
          )}
          {missingLoreRequirements.length > 0 && (
            <Alert
              severity="info"
              action={
                <Button color="inherit" size="small" onClick={handleMissingLoreAlertClick}>
                  Review lore
                </Button>
              }
            >
              {missingLoreRequirements.length} lore requirement{missingLoreRequirements.length === 1 ? '' : 's'}{' '}
              still block downstream planners.
            </Alert>
          )}
          {openPersonaObligations.length > 0 && (
            <Alert
              severity="warning"
              action={
                <Button color="inherit" size="small" onClick={handleObligationAlertClick}>
                  Resolve
                </Button>
              }
            >
              {openPersonaObligations.length} persona obligation
              {openPersonaObligations.length === 1 ? '' : 's'} awaiting follow-up.
            </Alert>
          )}
        </Stack>
      )}
      <Grid container spacing={3}>
        <Grid item xs={12} md={4}>
            <FictionPlanListCard
              plans={plans}
              selectedPlanId={selectedPlanId}
              plansLoading={plansLoading}
              plansError={plansError}
              onSelectPlan={setSelectedPlanId}
              onCreatePlan={() => setPlanDialogOpen(true)}
            />
        </Grid>
        <Grid item xs={12} md={8}>
          <Stack spacing={3}>
            <FictionBacklogCard
              rosterPresent={Boolean(roster)}
              backlogItems={backlogItems}
              backlogLoading={backlogLoading}
              backlogError={backlogError}
              placeholder={plans.length === 0 ? 'Create a plan to populate backlog data.' : 'Select a plan to inspect its backlog.'}
              isAdmin={isAdmin}
              resumingId={resuming && resumeTarget ? resumeTarget.id : null}
              actionLogs={actionLogs}
              actionLoading={actionLoading}
              actionError={actionError}
              obligations={personaObligations}
              obligationsLoading={obligationsLoading}
              obligationsError={obligationsError}
              onResume={handleResume}
              onResolveObligation={handleObligationActionRequest}
              obligationActionId={obligationActionId}
              obligationActionError={obligationActionError}
              personaContext={personaContext}
            />
            <FictionRosterPersonaSection
              rosterCardRef={rosterCardRef}
              rosterProps={{
                roster,
                loading: rosterLoading,
                error: rosterError,
                placeholder: plans.length === 0 ? 'No fiction plans available.' : 'Select a plan to load its roster.',
                onFulfillLore: handleFulfillLore,
                loreHistory,
                loreHistoryLoading,
                loreHistoryError,
              }}
              personaContext={personaContext}
              personaLoading={personaLoading}
              personaError={personaError}
            />
          </Stack>
        </Grid>
      </Grid>
      <FictionResumeBacklogDialog
        open={Boolean(resumeTarget)}
        item={resumeTarget}
        defaultBranch={resumeDefaults.branchSlug ?? roster?.branchSlug ?? undefined}
        defaultProviderId={resumeDefaults.providerId}
        defaultModelId={resumeDefaults.modelId ?? undefined}
        submitting={resuming}
        error={resumeError}
        onClose={handleResumeDialogClose}
        onSubmit={handleResumeSubmit}
        accessToken={token}
      />
      <PersonaObligationActionDialog
        open={Boolean(obligationDialog)}
        obligation={obligationDialog?.obligation ?? null}
        action={obligationDialog?.action ?? 'resolve'}
        submitting={Boolean(obligationDialog && obligationActionId === obligationDialog.obligation.id)}
        error={obligationActionError}
        onSubmit={handleObligationDialogSubmit}
        onClose={handleObligationDialogClose}
      />
      <FictionPlanWizardDialog
        open={planDialogOpen}
        accessToken={token}
        onClose={() => setPlanDialogOpen(false)}
        onCreated={handlePlanCreated}
        onPrefillResume={setResumeDefaults}
      />
    </Stack>
  );
}
