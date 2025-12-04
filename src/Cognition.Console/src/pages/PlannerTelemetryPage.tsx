import React from 'react';
import { Alert, AlertTitle, Box, Button, Chip, CircularProgress, Grid, Stack, Tooltip, Typography } from '@mui/material';
import RefreshIcon from '@mui/icons-material/Refresh';
import { ApiError, fictionApi } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { useSecurity } from '../hooks/useSecurity';
import {
  OpenSearchDiagnosticsReport,
  PlannerHealthAlert,
} from '../types/diagnostics';
import {
  FictionBacklogItem,
  FictionLoreRequirementItem,
  PersonaObligation,
  ResumeBacklogPayload
} from '../types/fiction';
import { FictionResumeBacklogDialog } from '../components/fiction/FictionResumeBacklogDialog';
import { PersonaObligationActionDialog } from '../components/fiction/PersonaObligationActionDialog';
import { usePlannerTelemetryData } from '../hooks/usePlannerTelemetryData';
import { usePlannerFictionContext } from '../hooks/usePlannerFictionContext';
import { usePlannerFilters } from '../hooks/usePlannerFilters';
import { BacklogAlertsCard } from '../components/planner/BacklogAlertsCard';
import { BacklogTableCard } from '../components/planner/BacklogTableCard';
import { PlannerHealthCard } from '../components/planner/PlannerHealthCard';
import { OpenSearchCard } from '../components/planner/OpenSearchCard';
import { ActionLogCard } from '../components/planner/ActionLogCard';
import { PlannerFailuresCard } from '../components/planner/PlannerFailuresCard';
import { PlannerTemplatesCard } from '../components/planner/PlannerTemplatesCard';
import { TransitionsCard } from '../components/planner/TransitionsCard';
import { WorldBibleCard } from '../components/planner/WorldBibleCard';
import { BacklogAnomaliesCard } from '../components/planner/BacklogAnomaliesCard';
import { ExecutionTelemetryCard } from '../components/planner/ExecutionTelemetryCard';
import { LoreSummaryCard } from '../components/planner/LoreSummaryCard';
import { PersonaCard } from '../components/planner/PersonaCard';
import { TelemetryFeedCard } from '../components/planner/TelemetryFeedCard';
import { RosterCard } from '../components/planner/RosterCard';
import { formatRelativeTime, formatTimestamp } from '../components/planner/timeFormatters';
type AlertDescriptor = {
  id: string;
  severity: 'error' | 'warning' | 'info' | 'success';
  title: string;
  description?: string;
};

export default function PlannerTelemetryPage() {
  const { auth } = useAuth();
  const { isAdmin } = useSecurity();
  const token = auth?.accessToken;

  const {
    plannerReport,
    openSearchReport,
    loading,
    refreshing,
    error,
    refresh,
  } = usePlannerTelemetryData(token);
  const {
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
    loading: fictionLoading,
    error: fictionError,
    setResumeTarget,
    setResumeDefaults,
    refreshPlanData,
    resumeBacklog,
    resolveObligation,
  } = usePlannerFictionContext(token);
  const [resuming, setResuming] = React.useState(false);
  const [resumeError, setResumeError] = React.useState<string | null>(null);
  const [obligationDialog, setObligationDialog] = React.useState<{
    obligation: PersonaObligation;
    action: 'resolve' | 'dismiss';
  } | null>(null);
  const [obligationActionId, setObligationActionId] = React.useState<string | null>(null);
  const [obligationActionError, setObligationActionError] = React.useState<string | null>(null);
  const rosterCardRef = React.useRef<HTMLDivElement | null>(null);

  React.useEffect(() => {
    if (!plannerReport || plannerReport.backlog.plans.length === 0) {
      setRosterPlanId(null);
      return;
    }
    setRosterPlanId((prev: string): string => {
      if (prev !== null && plannerReport.backlog.plans.some(plan => plan.planId === prev)) {
        return prev;
      }
      return plannerReport.backlog.plans[0]?.planId;
    });
  }, [plannerReport]);

  React.useEffect(() => {
    if (!token || !rosterPlanId) {
      setResumeTarget(null);
      setResumeError(null);
      setObligationDialog(null);
      setObligationActionError(null);
      setObligationActionId(null);
      setResumeDefaults({});
      return;
    }
    refreshPlanData(rosterPlanId);
  }, [token, rosterPlanId, refreshPlanData]);

  React.useEffect(() => {
    setResumeDefaults({});
  }, [rosterPlanId]);

  const handleFulfillLore = React.useCallback(
    async (requirement: FictionLoreRequirementItem) => {
      if (!token || !rosterPlanId) {
        return;
      }
      try {
        await fictionApi.fulfillLoreRequirement(
          rosterPlanId,
          requirement.id,
          {
            branchSlug: requirement.branchSlug ?? planRoster?.branchSlug,
            branchLineage: requirement.branchLineage ?? null,
            source: 'console'
          },
          token
        );
        await refreshPlanData(rosterPlanId);
      } catch (err) {
        const message = err instanceof ApiError ? err.message : 'Unable to fulfill lore requirement.';
        setResumeError(message);
      }
    },
    [token, rosterPlanId, planRoster?.branchSlug, refreshPlanData]
  );

  const serverAlerts = React.useMemo(() => {
    if (!plannerReport) {
      return [] as AlertDescriptor[];
    }
    return (plannerReport.alerts ?? []).map(mapServerAlert);
  }, [plannerReport]);

  const handleResumeBacklog = React.useCallback((item: FictionBacklogItem) => {
    setResumeTarget(item);
    setResumeError(null);
  }, []);

  const handleResumeDialogClose = React.useCallback(() => {
    if (resuming) {
      return;
    }
    setResumeTarget(null);
    setResumeError(null);
  }, [resuming]);

  const handleResumeSubmit = React.useCallback(
    async (payload: ResumeBacklogPayload) => {
      if (!token || !rosterPlanId || !resumeTarget) {
        setResumeError('Missing backlog metadata for resume request.');
        return;
      }
      setResuming(true);
      setResumeError(null);
      try {
        await resumeBacklog(rosterPlanId, resumeTarget.backlogId, payload);
        setResumeDefaults({ providerId: payload.providerId, modelId: payload.modelId ?? null });
        setResumeTarget(null);
        await refreshPlanData(rosterPlanId);
      } catch (err) {
        const message = err instanceof ApiError ? err.message : 'Unable to resume backlog item.';
        setResumeError(message);
      } finally {
        setResuming(false);
      }
    },
    [token, rosterPlanId, resumeTarget, refreshPlanData]
  );

  const handleObligationActionRequest = React.useCallback(
    (obligation: PersonaObligation, action: 'resolve' | 'dismiss') => {
      if (obligationActionId) {
        return;
      }
      setObligationDialog({ obligation, action });
      setObligationActionError(null);
    },
    [obligationActionId]
  );

  const handleObligationDialogClose = React.useCallback(() => {
    if (obligationActionId) {
      return;
    }
    setObligationDialog(null);
  }, [obligationActionId]);

  const handleObligationDialogSubmit = React.useCallback(
    async (payload?: { notes: string; voiceDrift?: boolean | null }, action?: 'resolve' | 'dismiss') => {
      if (!token || !rosterPlanId || !obligationDialog) {
        setObligationActionError('Missing obligation context.');
        return;
      }
      const target = obligationDialog.obligation;
      setObligationActionId(target.id);
      setObligationActionError(null);
      try {
        const payloadBody = {
          notes: payload?.notes ?? undefined,
          voiceDrift: payload?.voiceDrift,
          source: 'console',
          action: action ?? obligationDialog.action,
          backlogId: target.sourceBacklogId ?? null
        };
        await resolveObligation(rosterPlanId, target, payloadBody.action);
        await refreshPlanData(rosterPlanId);
        setObligationDialog(null);
      } catch (err) {
        const message = err instanceof ApiError ? err.message : 'Unable to update persona obligation.';
        setObligationActionError(message);
      } finally {
        setObligationActionId(null);
      }
    },
    [token, rosterPlanId, obligationDialog, resolveObligation, refreshPlanData]
  );

  const alerts = React.useMemo(() => {
    if (!openSearchReport) {
      return serverAlerts;
    }
    return [...serverAlerts, ...buildOpenSearchAlerts(openSearchReport)];
  }, [serverAlerts, openSearchReport]);

  const fictionAlerts = React.useMemo(() => {
    return serverAlerts.filter(alert =>
      alert.id.startsWith('backlog:') ||
      alert.id.startsWith('lore:') ||
      alert.id.startsWith('obligation:') ||
      alert.id.startsWith('worldbible:')
    );
  }, [serverAlerts]);

  const {
    planStaleBacklogItems,
    planMissingLoreRequirements,
    planDriftObligations,
    planAgingObligations,
    planOpenObligations,
    resolvedObligationCount,
    dismissedObligationCount,
    planContractEvents,
    planAlertCount,
  } = usePlannerFilters({
    rosterPlanId,
    plannerReport,
    planBacklogItems,
    planObligations,
    planRoster,
  });

  const scrollToRoster = React.useCallback(() => {
    rosterCardRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }, []);

  const handlePlanResumeAlertClick = React.useCallback(() => {
    if (planStaleBacklogItems.length === 0) {
      return;
    }
    handleResumeBacklog(planStaleBacklogItems[0]);
  }, [planStaleBacklogItems, handleResumeBacklog]);

  const handlePlanLoreAlertClick = React.useCallback(() => {
    scrollToRoster();
  }, [scrollToRoster]);

  const handlePlanObligationAlertClick = React.useCallback(() => {
    if (planOpenObligations.length === 0) {
      return;
    }
    handleObligationActionRequest(planOpenObligations[0], 'resolve');
  }, [planOpenObligations, handleObligationActionRequest]);

  if (!isAdmin) {
    return (
      <Box sx={{ mt: 4 }}>
        <Alert severity="info">
          <AlertTitle>Administrator access required</AlertTitle>
          Backlog telemetry, planner health, and OpenSearch diagnostics are restricted to administrators.
        </Alert>
      </Box>
    );
  }

  if (loading) {
    return (
      <Box sx={{ mt: 6, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2 }}>
        <CircularProgress />
        <Typography variant="body2" color="text.secondary">
          Loading backlog telemetry…
        </Typography>
      </Box>
    );
  }

  if (error) {
    return (
      <Box sx={{ mt: 4 }}>
        <Alert
          severity="error"
          action={
            <Button color="inherit" size="small" onClick={() => refresh()}>
              Retry
            </Button>
          }
        >
          <AlertTitle>Unable to load diagnostics</AlertTitle>
          {error}
        </Alert>
      </Box>
    );
  }

  if (!plannerReport || !openSearchReport) {
    return null;
  }

  return (
    <Stack spacing={3}>
      <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'flex-start', sm: 'center' }} spacing={1}>
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 600 }}>
            Backlog Telemetry
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Planner health snapshot generated {formatRelativeTime(plannerReport.generatedAtUtc)} ({formatTimestamp(plannerReport.generatedAtUtc)})
          </Typography>
        </Box>
        <Stack direction="row" spacing={1}>
          <Tooltip title="Refresh diagnostics">
            <span>
              <Button
                variant="outlined"
                startIcon={<RefreshIcon />}
              onClick={() => refresh()}
                disabled={refreshing}
              >
                Refresh
              </Button>
            </span>
          </Tooltip>
        </Stack>
      </Stack>

      {alerts.length > 0 && (
        <Stack spacing={1}>
          {alerts.map(alert => (
            <Alert key={alert.id} severity={alert.severity}>
              <AlertTitle>{alert.title}</AlertTitle>
              {alert.description}
            </Alert>
          ))}
        </Stack>
      )}

      {fictionAlerts.length > 0 && (
        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap sx={{ my: 1 }}>
          {fictionAlerts.map(alert => {
            const lower = alert.id.toLowerCase();
            const linkText =
              lower.startsWith('backlog:') ? 'View backlog' :
              lower.startsWith('lore:') ? 'Review lore' :
              lower.startsWith('obligation:') ? 'Resolve obligations' :
              lower.startsWith('worldbible:') ? 'Open roster' :
              null;
            const onClick = () => {
              if (lower.startsWith('backlog:') && rosterCardRef.current) {
                rosterCardRef.current.scrollIntoView({ behavior: 'smooth', block: 'center' });
              } else if (lower.startsWith('lore:') && rosterCardRef.current) {
                rosterCardRef.current.scrollIntoView({ behavior: 'smooth', block: 'center' });
              } else if (lower.startsWith('obligation:') && rosterCardRef.current) {
                rosterCardRef.current.scrollIntoView({ behavior: 'smooth', block: 'center' });
              } else if (lower.startsWith('worldbible:') && rosterCardRef.current) {
                rosterCardRef.current.scrollIntoView({ behavior: 'smooth', block: 'center' });
              }
            };
            return (
              <Chip
                key={`fx-${alert.id}`}
                color={alert.severity === 'error' ? 'error' : alert.severity === 'warning' ? 'warning' : 'info'}
                variant="outlined"
                label={linkText ? `${alert.title} • ${linkText}` : alert.title}
                onClick={linkText ? onClick : undefined}
              />
            );
          })}
        </Stack>
      )}

      <Box ref={rosterCardRef}>
        <BacklogAlertsCard
          rosterPlanId={rosterPlanId}
          alerts={alerts as unknown as PlannerHealthAlert[]}
          planAlertCount={planAlertCount}
          planContractEvents={planContractEvents}
          planStaleBacklogItems={planStaleBacklogItems}
          planMissingLoreRequirements={planMissingLoreRequirements}
          planOpenObligations={planOpenObligations}
          planDriftObligations={planDriftObligations}
          planAgingObligations={planAgingObligations}
          onResumeClick={handlePlanResumeAlertClick}
          onLoreClick={handlePlanLoreAlertClick}
          onObligationClick={handlePlanObligationAlertClick}
        />
      </Box>
      <Grid container spacing={3}>
        <Grid item xs={12} md={8}>
          <PlannerHealthCard plannerReport={plannerReport} />
        </Grid>
        <Grid item xs={12} md={4}>
          <OpenSearchCard report={openSearchReport} />
        </Grid>
      </Grid>

      <PlannerFailuresCard plannerReport={plannerReport} />
      <PlannerTemplatesCard plannerReport={plannerReport} />
      <TransitionsCard plannerReport={plannerReport} />
      <WorldBibleCard plannerReport={plannerReport} />
      <ExecutionTelemetryCard plannerReport={plannerReport} />
      <BacklogAnomaliesCard plannerReport={plannerReport} />

      <LoreSummaryCard
        rosterPlanId={rosterPlanId}
        loreSummary={loreSummary}
        loading={fictionLoading}
        error={fictionError}
      />
      <BacklogTableCard
        plannerReport={plannerReport}
        rosterPlanId={rosterPlanId}
        setRosterPlanId={setRosterPlanId}
        planBacklogItems={planBacklogItems}
        planOpenObligationsCount={planOpenObligations.length}
        resolvedObligationCount={resolvedObligationCount}
        dismissedObligationCount={dismissedObligationCount}
        planActionLogs={planActionLogs}
        planObligations={planObligations}
        fictionLoading={fictionLoading}
        fictionError={fictionError}
        resuming={resuming}
        resumeTargetId={resumeTarget ? resumeTarget.id : null}
        isAdmin={isAdmin}
        onResumeBacklog={handleResumeBacklog}
        onResolveObligation={handleObligationActionRequest}
        obligationActionId={obligationActionId}
        obligationActionError={obligationActionError}
      />
      <RosterCard
        plannerReport={plannerReport}
        rosterPlanId={rosterPlanId}
        setRosterPlanId={setRosterPlanId}
        planRoster={planRoster}
        planLoreHistory={planLoreHistory}
        loading={fictionLoading}
        error={fictionError}
        onFulfillLore={handleFulfillLore}
      />
      <PersonaCard
        rosterPlanId={rosterPlanId}
        persona={planRosterPersona}
        loading={fictionLoading}
        error={fictionError}
      />
      <TelemetryFeedCard plannerReport={plannerReport} />
      <ActionLogCard plannerReport={plannerReport} />
      <FictionResumeBacklogDialog
        open={Boolean(resumeTarget)}
        item={resumeTarget}
        defaultBranch={planRoster?.branchSlug ?? undefined}
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
        onSubmit={payload => handleObligationDialogSubmit(payload)}
        onClose={handleObligationDialogClose}
      />
    </Stack>
  );
}

function mapServerAlert(alert: PlannerHealthAlert): AlertDescriptor {
  const severity = (alert.severity || '').toLowerCase();
  const normalized: AlertDescriptor['severity'] =
    severity === 'error' ? 'error' :
    severity === 'warning' ? 'warning' :
    severity === 'success' ? 'success' :
    'info';
  return {
    id: alert.id,
    severity: normalized,
    title: alert.title,
    description: alert.description
  };
}

function buildOpenSearchAlerts(report: OpenSearchDiagnosticsReport): AlertDescriptor[] {
  const alerts: AlertDescriptor[] = [];
  if (!report.clusterAvailable) {
    alerts.push({
      id: 'opensearch:cluster',
      severity: 'error',
      title: 'OpenSearch cluster unavailable',
      description: `Endpoint ${report.endpoint} is not reachable or reports status ${report.clusterStatus ?? 'unknown'}.`
    });
  }
  if (!report.indexExists) {
    alerts.push({
      id: 'opensearch:index',
      severity: 'warning',
      title: 'Missing OpenSearch index',
      description: `Default index ${report.defaultIndex} is missing.`
    });
  }
  if (!report.pipelineExists) {
    alerts.push({
      id: 'opensearch:pipeline',
      severity: 'warning',
      title: 'Missing OpenSearch pipeline',
      description: report.pipelineId ? `Ingest pipeline ${report.pipelineId} not found.` : 'Expected ingest pipeline missing.'
    });
  }
  if (report.modelState && report.modelState.toLowerCase() !== 'loaded') {
    alerts.push({
      id: 'opensearch:model',
      severity: 'warning',
      title: 'Model not loaded',
      description: `Model ${report.modelId ?? 'unknown'} is in state ${report.modelState}.`
    });
  }
  if (report.modelDeployState && report.modelDeployState.toLowerCase() !== 'started') {
    alerts.push({
      id: 'opensearch:model-deploy',
      severity: 'warning',
      title: 'Model deploy not started',
      description: `Model deployment state: ${report.modelDeployState}.`
    });
  }
  (report.notes || []).forEach((note, idx) => {
    alerts.push({
      id: `opensearch:note:${idx}`,
      severity: 'info',
      title: 'OpenSearch note',
      description: note
    });
  });
  return alerts;
}




















