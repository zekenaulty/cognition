import React from 'react';
import {
  Alert,
  AlertTitle,
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  Chip,
  CircularProgress,
  Divider,
  FormControl,
  Grid,
  IconButton,
  InputLabel,
  LinearProgress,
  List,
  ListItem,
  ListItemText,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tooltip,
  Typography
} from '@mui/material';
import RefreshIcon from '@mui/icons-material/Refresh';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import TimelineIcon from '@mui/icons-material/Timeline';
import InsightsIcon from '@mui/icons-material/Insights';
import { ApiError, diagnosticsApi, fictionApi } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { useSecurity } from '../hooks/useSecurity';
import {
  OpenSearchDiagnosticsReport,
  PlannerHealthAlert,
  PlannerHealthBacklogItem,
  PlannerHealthReport,
  PlannerHealthStatus
} from '../types/diagnostics';
import {
  AuthorPersonaContext,
  BacklogActionLog,
  FictionBacklogItem,
  FictionLoreRequirementItem,
  FictionPlanRoster,
  LoreBranchSummary,
  LoreFulfillmentLog,
  PersonaObligation,
  ResumeBacklogPayload
} from '../types/fiction';
import { FictionRosterPanel } from '../components/fiction/FictionRosterPanel';
import { FictionBacklogPanel } from '../components/fiction/FictionBacklogPanel';
import { FictionResumeBacklogDialog } from '../components/fiction/FictionResumeBacklogDialog';
import { PersonaObligationActionDialog } from '../components/fiction/PersonaObligationActionDialog';
import { buildActionContextLine, buildActionMetadataLine, isObligationOpen, normalizeObligationStatus } from '../components/fiction/backlogUtils';

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

  const [plannerReport, setPlannerReport] = React.useState<PlannerHealthReport | null>(null);
  const [openSearchReport, setOpenSearchReport] = React.useState<OpenSearchDiagnosticsReport | null>(null);
  const [loading, setLoading] = React.useState(true);
  const [refreshing, setRefreshing] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [rosterPlanId, setRosterPlanId] = React.useState<string | null>(null);
  const [planRoster, setPlanRoster] = React.useState<FictionPlanRoster | null>(null);
  const [rosterLoading, setRosterLoading] = React.useState(false);
  const [rosterError, setRosterError] = React.useState<string | null>(null);
  const [planRosterPersona, setPlanRosterPersona] = React.useState<AuthorPersonaContext | null>(null);
  const [planRosterPersonaLoading, setPlanRosterPersonaLoading] = React.useState(false);
  const [planRosterPersonaError, setPlanRosterPersonaError] = React.useState<string | null>(null);
  const [loreSummary, setLoreSummary] = React.useState<LoreBranchSummary[]>([]);
  const [loreSummaryLoading, setLoreSummaryLoading] = React.useState(false);
  const [loreSummaryError, setLoreSummaryError] = React.useState<string | null>(null);
  const [planLoreHistory, setPlanLoreHistory] = React.useState<Record<string, LoreFulfillmentLog[]>>({});
  const [planLoreHistoryLoading, setPlanLoreHistoryLoading] = React.useState(false);
  const [planLoreHistoryError, setPlanLoreHistoryError] = React.useState<string | null>(null);
  const [planObligations, setPlanObligations] = React.useState<PersonaObligation[]>([]);
  const [planObligationsLoading, setPlanObligationsLoading] = React.useState(false);
  const [planObligationsError, setPlanObligationsError] = React.useState<string | null>(null);
  const [planActionLogs, setPlanActionLogs] = React.useState<BacklogActionLog[]>([]);
  const [planActionLoading, setPlanActionLoading] = React.useState(false);
  const [planActionError, setPlanActionError] = React.useState<string | null>(null);
  const [planBacklogItems, setPlanBacklogItems] = React.useState<FictionBacklogItem[]>([]);
  const [planBacklogLoading, setPlanBacklogLoading] = React.useState(false);
  const [planBacklogError, setPlanBacklogError] = React.useState<string | null>(null);
  const [resumeTarget, setResumeTarget] = React.useState<FictionBacklogItem | null>(null);
  const [resuming, setResuming] = React.useState(false);
  const [resumeError, setResumeError] = React.useState<string | null>(null);
  const [resumeDefaults, setResumeDefaults] = React.useState<{ providerId?: string; modelId?: string | null }>({});
  const [obligationDialog, setObligationDialog] = React.useState<{
    obligation: PersonaObligation;
    action: 'resolve' | 'dismiss';
  } | null>(null);
  const [obligationActionId, setObligationActionId] = React.useState<string | null>(null);
  const [obligationActionError, setObligationActionError] = React.useState<string | null>(null);
  const rosterCardRef = React.useRef<HTMLDivElement | null>(null);

  const loadReports = React.useCallback(
    async (isRefresh = false) => {
      if (!token) {
        return;
      }
      if (isRefresh) {
        setRefreshing(true);
      } else {
        setLoading(true);
      }
      setError(null);
      try {
        const [planner, search] = await Promise.all([
          diagnosticsApi.plannerHealth(token),
          diagnosticsApi.openSearch(token)
        ]);
        setPlannerReport(planner);
        setOpenSearchReport(search);
      } catch (err) {
        const message = err instanceof ApiError ? err.message : 'Unable to load diagnostics.';
        setError(message);
      } finally {
        if (isRefresh) {
          setRefreshing(false);
        } else {
          setLoading(false);
        }
      }
    },
    [token]
  );

  React.useEffect(() => {
    loadReports(false);
  }, [loadReports]);

  React.useEffect(() => {
    if (!plannerReport || plannerReport.backlog.plans.length === 0) {
      setRosterPlanId(null);
      setPlanRoster(null);
      return;
    }
    setRosterPlanId(prev => {
      if (prev && plannerReport.backlog.plans.some(plan => plan.planId === prev)) {
        return prev;
      }
      return plannerReport.backlog.plans[0].planId;
    });
  }, [plannerReport]);

  React.useEffect(() => {
    if (!token || !rosterPlanId) {
      setPlanRoster(null);
      setPlanRosterPersona(null);
      setPlanRosterPersonaError(null);
      setLoreSummary([]);
      setLoreSummaryError(null);
      setPlanLoreHistory({});
      setPlanLoreHistoryError(null);
      setPlanLoreHistoryLoading(false);
      setPlanObligations([]);
      setPlanObligationsError(null);
      setPlanObligationsLoading(false);
      setPlanActionLogs([]);
      setPlanActionError(null);
      setPlanActionLoading(false);
      setPlanBacklogItems([]);
      setPlanBacklogError(null);
      setPlanBacklogLoading(false);
      setResumeTarget(null);
      setResumeError(null);
      setObligationDialog(null);
      setObligationActionError(null);
      setObligationActionId(null);
      setResumeDefaults({});
      return;
    }
    let cancelled = false;
    setRosterLoading(true);
    setRosterError(null);
    setPlanRosterPersonaLoading(true);
    setPlanRosterPersonaError(null);
    setLoreSummaryLoading(true);
    setLoreSummaryError(null);
    setPlanLoreHistoryLoading(true);
    setPlanLoreHistoryError(null);
    setPlanObligationsLoading(true);
    setPlanObligationsError(null);
    setPlanActionLoading(true);
    setPlanActionError(null);
    setPlanBacklogLoading(true);
    setPlanBacklogError(null);
    fictionApi
      .getPlanRoster(rosterPlanId, token)
      .then(data => {
        if (!cancelled) {
          setPlanRoster(data);
        }
      })
      .catch(err => {
        if (!cancelled) {
          const message = err instanceof ApiError ? err.message : 'Unable to load roster.';
          setRosterError(message);
          setPlanRoster(null);
        }
      })
      .finally(() => {
        if (!cancelled) {
          setRosterLoading(false);
        }
      });
    fictionApi
      .getAuthorPersonaContext(rosterPlanId, token)
      .then(data => {
        if (!cancelled) {
          setPlanRosterPersona(data);
        }
      })
      .catch(err => {
        if (!cancelled) {
          if (err instanceof ApiError && err.status === 404) {
            setPlanRosterPersona(null);
            setPlanRosterPersonaError(null);
          } else {
            const message = err instanceof ApiError ? err.message : 'Unable to load author persona context.';
            setPlanRosterPersonaError(message);
            setPlanRosterPersona(null);
          }
        }
      })
      .finally(() => {
        if (!cancelled) {
          setPlanRosterPersonaLoading(false);
        }
      });
    fictionApi
      .getLoreSummary(rosterPlanId, token)
      .then(data => {
        if (!cancelled) {
          setLoreSummary(data);
        }
      })
      .catch(err => {
        if (!cancelled) {
          const message = err instanceof ApiError ? err.message : 'Unable to load lore fulfillment summary.';
          setLoreSummaryError(message);
          setLoreSummary([]);
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoreSummaryLoading(false);
        }
      });
    fictionApi
      .getLoreHistory(rosterPlanId, token)
      .then(data => {
        if (cancelled) {
          return;
        }
        const grouped: Record<string, LoreFulfillmentLog[]> = {};
        data.forEach(entry => {
          const key = entry.requirementId.toLowerCase();
          if (!grouped[key]) {
            grouped[key] = [];
          }
          grouped[key].push(entry);
        });
        setPlanLoreHistory(grouped);
      })
      .catch(err => {
        if (!cancelled) {
          const message = err instanceof ApiError ? err.message : 'Unable to load lore history.';
          setPlanLoreHistoryError(message);
          setPlanLoreHistory({});
        }
      })
      .finally(() => {
        if (!cancelled) {
          setPlanLoreHistoryLoading(false);
        }
      });
    fictionApi
      .getPersonaObligations(rosterPlanId, token)
      .then(data => {
        if (!cancelled) {
          setPlanObligations(data.items ?? []);
        }
      })
      .catch(err => {
        if (!cancelled) {
          const message = err instanceof ApiError ? err.message : 'Unable to load persona obligations.';
          setPlanObligationsError(message);
          setPlanObligations([]);
        }
      })
      .finally(() => {
        if (!cancelled) {
          setPlanObligationsLoading(false);
        }
      });
    fictionApi
      .getBacklogActions(rosterPlanId, token)
      .then(data => {
        if (!cancelled) {
          setPlanActionLogs(data ?? []);
        }
      })
      .catch(err => {
        if (!cancelled) {
          const message = err instanceof ApiError ? err.message : 'Unable to load backlog action logs.';
          setPlanActionError(message);
          setPlanActionLogs([]);
        }
      })
      .finally(() => {
        if (!cancelled) {
          setPlanActionLoading(false);
        }
      });
    fictionApi
      .getPlanBacklog(rosterPlanId, token)
      .then(items => {
        if (!cancelled) {
          setPlanBacklogItems(items);
        }
      })
      .catch(err => {
        if (!cancelled) {
          const message = err instanceof ApiError ? err.message : 'Unable to load plan backlog.';
          setPlanBacklogError(message);
          setPlanBacklogItems([]);
        }
      })
      .finally(() => {
        if (!cancelled) {
          setPlanBacklogLoading(false);
        }
      });
    return () => {
      cancelled = true;
    };
  }, [token, rosterPlanId]);

  React.useEffect(() => {
    setResumeDefaults({});
  }, [rosterPlanId]);

  const refreshPlanBacklog = React.useCallback(
    async (planId: string) => {
      if (!token) {
        setPlanBacklogItems([]);
        return;
      }
      setPlanBacklogLoading(true);
      setPlanBacklogError(null);
      try {
        const items = await fictionApi.getPlanBacklog(planId, token);
        setPlanBacklogItems(items);
      } catch (err) {
        const message = err instanceof ApiError ? err.message : 'Unable to load plan backlog.';
        setPlanBacklogError(message);
        setPlanBacklogItems([]);
      } finally {
        setPlanBacklogLoading(false);
      }
    },
    [token]
  );

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
        const [refreshedRoster, summary] = await Promise.all([
          fictionApi.getPlanRoster(rosterPlanId, token),
          fictionApi.getLoreSummary(rosterPlanId, token)
        ]);
        setPlanRoster(refreshedRoster);
        setLoreSummary(summary);
      } catch (err) {
        const message = err instanceof ApiError ? err.message : 'Unable to fulfill lore requirement.';
        setRosterError(message);
      }
    },
    [token, rosterPlanId, planRoster?.branchSlug]
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
        await fictionApi.resumeBacklog(rosterPlanId, resumeTarget.backlogId, payload, token);
        setResumeDefaults({ providerId: payload.providerId, modelId: payload.modelId ?? null });
        setResumeTarget(null);
        await refreshPlanBacklog(rosterPlanId);
        try {
          const logs = await fictionApi.getBacklogActions(rosterPlanId, token);
          setPlanActionLogs(logs ?? []);
          setPlanActionError(null);
        } catch (err) {
          const message = err instanceof ApiError ? err.message : 'Unable to refresh backlog action logs.';
          setPlanActionError(message);
        }
      } catch (err) {
        const message = err instanceof ApiError ? err.message : 'Unable to resume backlog item.';
        setResumeError(message);
      } finally {
        setResuming(false);
      }
    },
    [token, rosterPlanId, resumeTarget, refreshPlanBacklog]
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
        const payload = {
          notes: payload?.notes ?? undefined,
          voiceDrift: payload?.voiceDrift,
          source: 'console',
          action: action ?? obligationDialog.action,
          backlogId: target.sourceBacklogId ?? null
        };
        const updated = await fictionApi.resolvePersonaObligation(rosterPlanId, target.id, payload, token);
        setPlanObligations(items => items.map(item => (item.id === updated.id ? updated : item)));
        setObligationDialog(null);
      } catch (err) {
        const message = err instanceof ApiError ? err.message : 'Unable to update persona obligation.';
        setObligationActionError(message);
      } finally {
        setObligationActionId(null);
      }
    },
    [token, rosterPlanId, obligationDialog]
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

  const contractTelemetryEvents = React.useMemo(() => {
    if (!plannerReport) {
      return [];
    }
    return (plannerReport.backlog.telemetryEvents ?? []).filter(
      evt => (evt.status ?? '').toString().toLowerCase() === 'contract'
    );
  }, [plannerReport]);

  const planStaleBacklogItems = React.useMemo(() => {
    if (!planBacklogItems || planBacklogItems.length === 0) {
      return [];
    }
    const now = Date.now();
    const thresholdMs = 60 * 60 * 1000;
    return planBacklogItems.filter(item => {
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
  }, [planBacklogItems]);

  const planMissingLoreRequirements = React.useMemo(
    () => planRoster?.loreRequirements?.filter(req => (req.status ?? '').toLowerCase() !== 'ready') ?? [],
    [planRoster]
  );

  const planDriftObligations = React.useMemo(
    () => planObligations.filter(obligation => summarizeObligationMetadata(obligation.metadata).voiceDrift),
    [planObligations]
  );

  const planAgingObligations = React.useMemo(() => {
    const thresholdMs = AGING_THRESHOLD_HOURS * 60 * 60 * 1000;
    return planObligations.filter(obligation => {
      if (!obligation.createdAtUtc) return false;
      const created = new Date(obligation.createdAtUtc);
      if (Number.isNaN(created.getTime())) return false;
      return Date.now() - created.getTime() > thresholdMs;
    });
  }, [planObligations]);

  const planOpenObligations = React.useMemo(
    () => planObligations.filter(obligation => isObligationOpen(obligation.status)),
    [planObligations]
  );

  const resolvedObligationCount = React.useMemo(
    () => planObligations.filter(obligation => normalizeObligationStatus(obligation.status) === 'resolved').length,
    [planObligations]
  );

  const dismissedObligationCount = React.useMemo(
    () => planObligations.filter(obligation => normalizeObligationStatus(obligation.status) === 'dismissed').length,
    [planObligations]
  );

  const planContractEvents = React.useMemo(() => {
    if (!rosterPlanId) {
      return [];
    }
    return contractTelemetryEvents.filter(evt => evt.planId === rosterPlanId);
  }, [rosterPlanId, contractTelemetryEvents]);

  const planAlertCount =
    planStaleBacklogItems.length +
    planMissingLoreRequirements.length +
    planOpenObligations.length +
    planDriftObligations.length +
    planAgingObligations.length +
    planContractEvents.length;

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
            <Button color="inherit" size="small" onClick={() => loadReports(false)}>
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

  const backlogTotals = [
    { label: 'Pending', value: plannerReport.backlog.pending, color: 'warning' as const },
    { label: 'In Progress', value: plannerReport.backlog.inProgress, color: 'info' as const },
    { label: 'Complete', value: plannerReport.backlog.complete, color: 'success' as const }
  ];
  const worldBibleReport = plannerReport.worldBible;

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
                onClick={() => loadReports(true)}
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

      <Card ref={rosterCardRef}>
        <CardHeader
          title="Backlog Alerts"
          subheader={
            !rosterPlanId
              ? 'Select a plan to inspect backlog alerts.'
              : planAlertCount === 0
                ? 'No stale backlog, lore, or obligation alerts detected.'
                : `${planAlertCount} active alert${planAlertCount === 1 ? '' : 's'} for the selected plan.`
          }
        />
        <CardContent>
          {!rosterPlanId ? (
            <Typography variant="body2" color="text.secondary">
              Choose a plan from the roster switcher below to review backlog alerts.
            </Typography>
          ) : planAlertCount === 0 ? (
            <Alert severity="success" variant="outlined">
              Planner backlog, lore fulfillment, and persona obligations are healthy.
            </Alert>
          ) : (
            <Stack spacing={1}>
              {planContractEvents.length > 0 && (
                <Alert severity="error">
                  {planContractEvents.length} contract drift event{planContractEvents.length === 1 ? '' : 's'} detected for this plan.
                  Inspect backlog telemetry below to resolve provider/model/agent mismatches before resuming.
                </Alert>
              )}
              {planStaleBacklogItems.length > 0 && (
                <Alert
                  severity="warning"
                  action={
                    <Button color="inherit" size="small" onClick={handlePlanResumeAlertClick}>
                      Resume
                    </Button>
                  }
                >
                  {planStaleBacklogItems.length} backlog item
                  {planStaleBacklogItems.length === 1 ? '' : 's'} are stuck in progress. Launch the resume dialog to refill metadata
                  and nudge the scheduler.
                </Alert>
              )}
              {planMissingLoreRequirements.length > 0 && (
                <Alert
                  severity="info"
                  action={
                    <Button color="inherit" size="small" onClick={handlePlanLoreAlertClick}>
                      Review lore
                    </Button>
                  }
                >
              {planMissingLoreRequirements.length} lore requirement
              {planMissingLoreRequirements.length === 1 ? '' : 's'} remain blocked. Jump to the roster panel to fulfill the gaps.
            </Alert>
          )}
          {planOpenObligations.length > 0 && (
                <Alert
                  severity="warning"
                  action={
                    <Button color="inherit" size="small" onClick={handlePlanObligationAlertClick}>
                      Resolve
                    </Button>
                  }
                >
              {planOpenObligations.length} persona obligation
              {planOpenObligations.length === 1 ? '' : 's'} require attention. Use the modal to resolve or dismiss them.
            </Alert>
          )}
          {planDriftObligations.length > 0 && (
            <Alert severity="info">
              {planDriftObligations.length} persona obligation
              {planDriftObligations.length === 1 ? '' : 's'} flagged for voice drift. Review resolutions to reset tone.
            </Alert>
          )}
          {planAgingObligations.length > 0 && (
            <Alert severity="warning">
              {planAgingObligations.length} persona obligation
              {planAgingObligations.length === 1 ? '' : 's'} aging without resolution ({AGING_THRESHOLD_HOURS}h).
            </Alert>
          )}
        </Stack>
      )}
    </CardContent>
  </Card>

      <Grid container spacing={3}>
        <Grid item xs={12} md={8}>
          <Card>
            <CardHeader
              avatar={<InsightsIcon color="primary" />}
              title="Planner Health"
              subheader={`Status: ${plannerReport.status}`}
              action={<StatusChip status={plannerReport.status} />}
            />
            <CardContent>
              <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
                <Stack spacing={1} flex={1}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Backlog Snapshot
                  </Typography>
                  <Stack direction="row" spacing={1}>
                    {backlogTotals.map(metric => (
                      <Chip
                        key={metric.label}
                        color={metric.color}
                        label={`${metric.label}: ${formatNumber(metric.value)}`}
                        variant="filled"
                      />
                    ))}
                  </Stack>
                </Stack>
                <Stack spacing={1} flex={1}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Planner Registrations
                  </Typography>
                  <Typography variant="h6">
                    {plannerReport.planners.length} Planners •{' '}
                    {plannerReport.planners.reduce((acc, planner) => acc + planner.steps.length, 0)} Steps
                  </Typography>
                </Stack>
              </Stack>
              <Divider sx={{ my: 2 }} />
              <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 1 }}>
                Backlog Coverage by Plan
              </Typography>
              {plannerReport.backlog.plans.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  No backlog coverage data available.
                </Typography>
              ) : (
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Plan</TableCell>
                      <TableCell>Pending</TableCell>
                      <TableCell>In Progress</TableCell>
                      <TableCell>Complete</TableCell>
                      <TableCell>Last Updated</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {plannerReport.backlog.plans.map(plan => (
                      <TableRow key={plan.planId}>
                        <TableCell>
                          <Typography variant="body2" sx={{ fontWeight: 600 }}>
                            {plan.planName}
                          </Typography>
                        </TableCell>
                        <TableCell>{formatNumber(plan.pending)}</TableCell>
                        <TableCell>{formatNumber(plan.inProgress)}</TableCell>
                        <TableCell>{formatNumber(plan.complete)}</TableCell>
                        <TableCell>
                          {plan.lastUpdatedUtc ? formatRelativeTime(plan.lastUpdatedUtc) : '—'}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
              {plannerReport.backlog.staleItems.length > 0 && (
                <Alert severity="warning" sx={{ mt: 2 }}>
                  {plannerReport.backlog.staleItems.length} backlog item
                  {plannerReport.backlog.staleItems.length === 1 ? '' : 's'} exceeded freshness SLO and remain stuck.
                </Alert>
              )}
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={4}>
          <Card>
            <CardHeader title="OpenSearch Diagnostics" subheader={formatTimestamp(openSearchReport.checkedAtUtc)} />
            <CardContent>
              <Stack spacing={1}>
                <StatusLine
                  label="Cluster"
                  healthy={openSearchReport.clusterAvailable}
                  detail={openSearchReport.clusterStatus ?? 'unknown'}
                />
                <StatusLine label="Index" healthy={openSearchReport.indexExists} detail={openSearchReport.defaultIndex} />
                <StatusLine label="Pipeline" healthy={openSearchReport.pipelineExists} detail={openSearchReport.pipelineId ?? 'n/a'} />
                <StatusLine
                  label="Model"
                  healthy={Boolean(openSearchReport.modelState && openSearchReport.modelState !== 'failed')}
                  detail={openSearchReport.modelState ?? 'n/a'}
                />
              </Stack>
              {openSearchReport.notes.length > 0 && (
                <>
                  <Divider sx={{ my: 2 }} />
                  <Typography variant="subtitle2" color="text.secondary">
                    Notes
                  </Typography>
                  <List dense>
                    {openSearchReport.notes.map((note, idx) => (
                      <ListItem key={`${note}-${idx}`} sx={{ py: 0.5 }}>
                        <ListItemText primary={note} />
                      </ListItem>
                    ))}
                  </List>
                </>
              )}
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12}>
          {plannerReport.telemetry.recentFailures.length > 0 && (
            <Card sx={{ mb: 3 }}>
              <CardHeader title="Recent Planner Failures" subheader="Last 24h" />
              <CardContent>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Planner</TableCell>
                      <TableCell>Outcome</TableCell>
                      <TableCell>Snippet</TableCell>
                      <TableCell>Conversation</TableCell>
                      <TableCell>When</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {plannerReport.telemetry.recentFailures.map(failure => (
                      <TableRow key={failure.executionId}>
                        <TableCell>
                          <Typography variant="body2" sx={{ fontWeight: 600 }}>
                            {failure.plannerName}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Chip size="small" color="error" label={failure.outcome} />
                        </TableCell>
                        <TableCell>
                          <Typography variant="caption" color="text.secondary">
                            {failure.transcriptSnippet || 'No snippet'}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="caption" color="text.secondary">
                            {failure.conversationId ? `Conv ${formatIdFragment(failure.conversationId)}` : '—'}
                            {failure.conversationMessageId
                              ? ` • Msg ${formatIdFragment(failure.conversationMessageId)}`
                              : ''}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2">{formatRelativeTime(failure.createdAtUtc)}</Typography>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </CardContent>
            </Card>
          )}
          <Card>
            <CardHeader
              title="World Bible Snapshots"
              subheader="Active entries recorded by WorldBibleManager"
            />
            <CardContent>
              {worldBibleReport.plans.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  No world bible entries have been captured yet. Run the WorldBibleManager phase to populate lore snapshots.
                </Typography>
              ) : (
                <Stack spacing={3}>
                  {worldBibleReport.plans.map(plan => (
                    <Box key={plan.worldBibleId}>
                      <Stack
                        direction={{ xs: 'column', md: 'row' }}
                        spacing={1}
                        justifyContent="space-between"
                        alignItems={{ xs: 'flex-start', md: 'center' }}
                      >
                        <Box>
                          <Typography variant="h6" sx={{ fontWeight: 600 }}>
                            {plan.planName}
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            Domain {plan.domain} • Branch {plan.branchSlug ?? 'main'}
                          </Typography>
                        </Box>
                        <Typography variant="body2" color="text.secondary">
                          Last updated{' '}
                          {plan.lastUpdatedUtc
                            ? `${formatRelativeTime(plan.lastUpdatedUtc)} (${formatTimestamp(plan.lastUpdatedUtc)})`
                            : '—'}
                        </Typography>
                      </Stack>
                      {plan.activeEntries.length === 0 ? (
                        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                          No active entries for this plan.
                        </Typography>
                      ) : (
                        <Table size="small" sx={{ mt: 2 }}>
                          <TableHead>
                            <TableRow>
                              <TableCell>Category</TableCell>
                              <TableCell>Entry</TableCell>
                              <TableCell>Status</TableCell>
                              <TableCell>Iteration</TableCell>
                              <TableCell>Backlog Item</TableCell>
                              <TableCell>Provenance</TableCell>
                              <TableCell>Continuity Notes</TableCell>
                              <TableCell align="right">Last Updated</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            {plan.activeEntries.map(entry => (
                              <TableRow key={`${entry.entrySlug}-v${entry.version}`}>
                                <TableCell sx={{ textTransform: 'capitalize', whiteSpace: 'nowrap' }}>{entry.category}</TableCell>
                                <TableCell>
                                  <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                                    {entry.entryName}
                                  </Typography>
                                  <Typography variant="body2" color="text.secondary">
                                    {entry.summary || 'No summary provided.'}
                                  </Typography>
                                  <Typography variant="caption" color="text.secondary">
                                    {entry.entrySlug}
                                  </Typography>
                                </TableCell>
                                <TableCell>
                                  <Chip label={entry.status || 'Unknown'} size="small" color="info" variant="outlined" />
                                </TableCell>
                                <TableCell>{entry.iterationIndex ?? '—'}</TableCell>
                                <TableCell>{entry.backlogItemId ?? '—'}</TableCell>
                                <TableCell>
                                  <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap>
                                    {entry.agentId && (
                                      <Chip size="small" label={`Agent ${formatIdFragment(entry.agentId)}`} variant="outlined" />
                                    )}
                                    {entry.personaId && (
                                      <Chip size="small" label={`Persona ${formatIdFragment(entry.personaId)}`} variant="outlined" />
                                    )}
                                    {entry.sourcePlanPassId && (
                                      <Chip size="small" label={`Pass ${formatIdFragment(entry.sourcePlanPassId)}`} variant="outlined" />
                                    )}
                                    {entry.sourceConversationId && (
                                      <Chip size="small" label={`Convo ${formatIdFragment(entry.sourceConversationId)}`} variant="outlined" />
                                    )}
                                    {entry.sourceBacklogId && <Chip size="small" label={`Backlog ${entry.sourceBacklogId}`} variant="outlined" />}
                                    {entry.branchSlug && <Chip size="small" label={`Branch ${entry.branchSlug}`} variant="outlined" />}
                                    {!entry.agentId &&
                                      !entry.personaId &&
                                      !entry.sourcePlanPassId &&
                                      !entry.sourceConversationId &&
                                      !entry.sourceBacklogId &&
                                      !entry.branchSlug && <Typography variant="caption">—</Typography>}
                                  </Stack>
                                </TableCell>
                                <TableCell>
                                  {entry.continuityNotes.length === 0 ? (
                                    '—'
                                  ) : (
                                    <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap>
                                      {entry.continuityNotes.map(note => (
                                        <Chip key={note} size="small" label={note} variant="outlined" />
                                      ))}
                                    </Stack>
                                  )}
                                </TableCell>
                                <TableCell align="right">
                                  {formatTimestamp(entry.updatedAtUtc)}
                                </TableCell>
                              </TableRow>
                            ))}
                          </TableBody>
                        </Table>
                      )}
                    </Box>
                  ))}
                </Stack>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Grid container spacing={3}>
        <Grid item xs={12} md={8}>
          <Card>
            <CardHeader title="Backlog Coverage by Plan" />
            <CardContent sx={{ px: 0 }}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Plan</TableCell>
                    <TableCell align="right">Pending</TableCell>
                    <TableCell align="right">In Progress</TableCell>
                    <TableCell align="right">Complete</TableCell>
                    <TableCell>Coverage</TableCell>
                    <TableCell>Last Activity</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {plannerReport.backlog.plans.map(plan => {
                    const total = plan.pending + plan.inProgress + plan.complete;
                    const completePct = total === 0 ? 0 : Math.round((plan.complete / total) * 100);
                    return (
                      <TableRow key={plan.planId} hover>
                        <TableCell>
                          <Typography variant="body2" sx={{ fontWeight: 600 }}>
                            {plan.planName}
                          </Typography>
                        </TableCell>
                        <TableCell align="right">{plan.pending}</TableCell>
                        <TableCell align="right">{plan.inProgress}</TableCell>
                        <TableCell align="right">{plan.complete}</TableCell>
                        <TableCell sx={{ minWidth: 160 }}>
                          <LinearProgress variant="determinate" value={completePct} sx={{ height: 8, borderRadius: 4, mb: 0.5 }} />
                          <Typography variant="caption" color="text.secondary">
                            {completePct}% complete
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2">{formatRelativeTime(plan.lastUpdatedUtc)}</Typography>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                  {plannerReport.backlog.plans.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={6} align="center">
                        <Typography variant="body2" color="text.secondary">
                          No backlog data found.
                        </Typography>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={4}>
          <Card>
            <CardHeader title="Stale & Orphaned Items" />
            <CardContent>
              <BacklogAnomalyList
                title="Stale Items"
                items={plannerReport.backlog.staleItems}
                emptyMessage="Backlog SLOs look good."
              />
              <Divider sx={{ my: 2 }} />
              <BacklogAnomalyList
                title="Orphaned Items"
                items={plannerReport.backlog.orphanedItems}
                emptyMessage="No orphaned backlog items detected."
              />
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Grid container spacing={3}>
        <Grid item xs={12} md={6}>
          <Card>
            <CardHeader title="Execution Telemetry" />
            <CardContent>
              <Stack spacing={1}>
                <Typography variant="h6">{formatNumber(plannerReport.telemetry.totalExecutions)} executions</Typography>
                <Typography variant="body2" color="text.secondary">
                  Last execution {formatRelativeTime(plannerReport.telemetry.lastExecutionUtc)}
                </Typography>
                <Stack direction="row" spacing={1} flexWrap="wrap">
                  {Object.entries(plannerReport.telemetry.outcomeCounts).map(([outcome, count]) => (
                    <Chip key={outcome} label={`${outcome}: ${count}`} size="small" />
                  ))}
                </Stack>
                {Object.keys(plannerReport.telemetry.critiqueStatusCounts).length > 0 && (
                  <>
                    <Typography variant="subtitle2" color="text.secondary" sx={{ mt: 1 }}>
                      Critique Budgets
                    </Typography>
                    <Stack direction="row" spacing={1} flexWrap="wrap">
                      {Object.entries(plannerReport.telemetry.critiqueStatusCounts).map(([status, count]) => (
                        <Chip
                          key={status}
                          size="small"
                          color={status.includes("exhausted") ? 'warning' : 'default'}
                          label={`${status}: ${count}`}
                        />
                      ))}
                    </Stack>
                  </>
                )}
              </Stack>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={6}>
          <Card>
            <CardHeader title="Planner Templates" />
            <CardContent>
              <Stack spacing={1}>
                {plannerReport.planners.map(planner => (
                  <Box key={planner.name} sx={{ border: '1px solid rgba(255,255,255,0.08)', borderRadius: 1, p: 1.5 }}>
                    <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                      {planner.name}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {planner.description}
                    </Typography>
                    <Stack spacing={0.5} sx={{ mt: 1 }}>
                      {planner.steps.map(step => (
                        <Stack key={step.stepId} direction="row" spacing={1} alignItems="center">
                          <Chip
                            size="small"
                            color={step.templateFound && step.templateActive ? 'success' : 'warning'}
                            icon={step.templateFound && step.templateActive ? <CheckCircleIcon fontSize="inherit" /> : <WarningAmberIcon fontSize="inherit" />}
                            label={step.displayName}
                          />
                          <Typography variant="caption" color="text.secondary">
                            {step.templateId ?? 'No template id'}
                            {!step.templateFound && ' • missing'}
                            {step.issue && ` • ${step.issue}`}
                          </Typography>
                        </Stack>
                      ))}
                    </Stack>
                  </Box>
                ))}
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Grid container spacing={3}>
        <Grid item xs={12} md={6}>
          <Card>
            <CardHeader
              title="Recent Transitions"
              action={
                <Tooltip title="Transitions from the past 24h">
                  <IconButton size="small">
                    <TimelineIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              }
            />
            <CardContent sx={{ px: 0 }}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Plan</TableCell>
                    <TableCell>Backlog Item</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Occurred</TableCell>
                    <TableCell>Age</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {plannerReport.backlog.recentTransitions.map(transition => (
                    <TableRow key={`${transition.planId}-${transition.backlogId}-${transition.occurredAtUtc}`} hover>
                      <TableCell>{transition.planName}</TableCell>
                      <TableCell>
                        <Typography variant="body2" sx={{ fontWeight: 500 }}>
                          {transition.backlogId}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {transition.description}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip size="small" label={describeBacklogStatus(transition.status)} />
                      </TableCell>
                      <TableCell>{formatRelativeTime(transition.occurredAtUtc)}</TableCell>
                      <TableCell>{formatDuration(transition.age)}</TableCell>
                    </TableRow>
                  ))}
                  {plannerReport.backlog.recentTransitions.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={5} align="center">
                        <Typography variant="body2" color="text.secondary">
                          No backlog movements captured in the last window.
                        </Typography>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={6}>
          <Card>
            <CardHeader title="Recent Failures" />
            <CardContent>
              <Stack spacing={1.5}>
                {plannerReport.telemetry.recentFailures.map(failure => (
                  <Box key={failure.executionId} sx={{ border: '1px solid rgba(255,255,255,0.08)', borderRadius: 1, p: 1.5 }}>
                    <Stack direction="row" spacing={1} alignItems="center">
                      <WarningAmberIcon color="warning" fontSize="small" />
                      <Typography variant="subtitle2">{failure.plannerName}</Typography>
                      <Chip size="small" label={failure.outcome} />
                    </Stack>
                    <Typography variant="caption" color="text.secondary">
                      {formatRelativeTime(failure.createdAtUtc)}
                    </Typography>
                    {failure.transcriptSnippet && (
                      <Typography variant="body2" sx={{ mt: 1 }}>
                        {failure.transcriptSnippet}
                      </Typography>
                    )}
                    {failure.diagnostics && Object.keys(failure.diagnostics).length > 0 && (
                      <Stack direction="row" spacing={1} flexWrap="wrap" sx={{ mt: 1 }}>
                        {Object.entries(failure.diagnostics).map(([key, value]) => (
                          <Chip key={`${failure.executionId}-${key}`} size="small" label={`${key}: ${value}`} />
                        ))}
                      </Stack>
                    )}
                  </Box>
                ))}
                {plannerReport.telemetry.recentFailures.length === 0 && (
                  <Typography variant="body2" color="text.secondary">
                    No planner failures within the configured window.
                  </Typography>
                )}
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
      <Card>
        <CardHeader title="Lore Fulfillment by Branch" />
        <CardContent>
          {!rosterPlanId ? (
            <Typography variant="body2" color="text.secondary">
              Select a plan to inspect lore fulfillment progress.
            </Typography>
          ) : loreSummaryLoading ? (
            <LinearProgress />
          ) : loreSummaryError ? (
            <Alert severity="warning">{loreSummaryError}</Alert>
          ) : loreSummary.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              No lore requirements have been recorded for this plan yet.
            </Typography>
          ) : (
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Branch</TableCell>
                  <TableCell align="right">Ready</TableCell>
                  <TableCell align="right">Blocked</TableCell>
                  <TableCell align="right">Planned</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {loreSummary.map(summary => (
                  <TableRow key={summary.branchSlug}>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>
                        {summary.branchSlug}
                      </Typography>
                      {summary.branchLineage && summary.branchLineage.length > 1 && (
                        <Typography variant="caption" color="text.secondary">
                          {summary.branchLineage.join(' â†’ ')}
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell align="right">{summary.ready}</TableCell>
                    <TableCell align="right">{summary.blocked}</TableCell>
                    <TableCell align="right">{summary.planned}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
      <Card>
        <CardHeader
          title="Plan Backlog"
          subheader={
            rosterPlanId
              ? `${plannerReport.backlog.plans.find(plan => plan.planId === rosterPlanId)?.planName ?? 'Backlog items'} • ${planBacklogItems.length} items • ${planOpenObligations.length} open obligations`
              : 'Select a plan to inspect backlog status and persona obligations.'
          }
          action={
            <FormControl size="small" sx={{ minWidth: 220 }} disabled={(plannerReport.backlog.plans ?? []).length === 0}>
              <InputLabel id="backlog-plan-label">Plan</InputLabel>
              <Select
                labelId="backlog-plan-label"
                label="Plan"
                value={rosterPlanId ?? ''}
                onChange={event => setRosterPlanId(event.target.value ? (event.target.value as string) : null)}
              >
                {(plannerReport.backlog.plans ?? []).map(plan => (
                  <MenuItem key={plan.planId} value={plan.planId}>
                    {plan.planName}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          }
        />
        <CardContent>
          {(plannerReport.backlog.plans ?? []).length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              No backlog plans detected yet.
            </Typography>
          ) : (
            <>
              {planObligations.length > 0 && (
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} sx={{ mb: 2 }}>
                  <Chip size="small" color="warning" label={`${planOpenObligations.length} open obligations`} />
                  <Chip size="small" color="success" variant="outlined" label={`${resolvedObligationCount} resolved`} />
                  <Chip size="small" color="default" variant="outlined" label={`${dismissedObligationCount} dismissed`} />
                </Stack>
              )}
              <FictionBacklogPanel
                items={planBacklogItems}
                loading={planBacklogLoading}
                error={planBacklogError}
                placeholder="Select a plan to inspect its backlog."
                onResume={handleResumeBacklog}
                resumingId={resuming && resumeTarget ? resumeTarget.id : null}
                isAdmin={isAdmin}
                actionLogs={planActionLogs}
                actionLoading={planActionLoading}
                actionError={planActionError}
                obligations={planObligations}
                obligationsLoading={planObligationsLoading}
                obligationsError={planObligationsError}
                onResolveObligation={handleObligationActionRequest}
                obligationActionId={obligationActionId}
                obligationActionError={obligationActionError}
                personaContext={planRosterPersona}
              />
            </>
          )}
        </CardContent>
      </Card>
      <Card>
        <CardHeader
          title="Character & Lore Roster"
          subheader={
            planRoster
              ? `${planRoster.planName}${planRoster.projectTitle ? ` • ${planRoster.projectTitle}` : ''}`
              : 'Select a plan to inspect tracked characters and lore.'
          }
          action={
            <FormControl size="small" sx={{ minWidth: 220 }} disabled={(plannerReport.backlog.plans ?? []).length === 0}>
              <InputLabel id="roster-plan-label">Plan</InputLabel>
              <Select
                labelId="roster-plan-label"
                label="Plan"
                value={rosterPlanId ?? ''}
                onChange={event => setRosterPlanId(event.target.value ? (event.target.value as string) : null)}
              >
                {(plannerReport.backlog.plans ?? []).map(plan => (
                  <MenuItem key={plan.planId} value={plan.planId}>
                    {plan.planName}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          }
        />
        <CardContent>
          {(plannerReport.backlog.plans ?? []).length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              No backlog plans detected yet.
            </Typography>
          ) : (
            <FictionRosterPanel
              roster={planRoster}
              loading={rosterLoading}
              error={rosterError}
              onFulfillLore={handleFulfillLore}
              loreHistory={planLoreHistory}
              loreHistoryLoading={planLoreHistoryLoading}
              loreHistoryError={planLoreHistoryError}
            />
          )}
        </CardContent>
      </Card>
      <Card>
        <CardHeader
          title="Author Persona"
          subheader={planRosterPersona ? planRosterPersona.personaName : undefined}
        />
        <CardContent>
          {!rosterPlanId ? (
            <Typography variant="body2" color="text.secondary">
              Select a plan with an author persona to inspect its memories and world notes.
            </Typography>
          ) : planRosterPersonaLoading ? (
            <LinearProgress />
          ) : planRosterPersonaError ? (
            <Alert severity="warning">{planRosterPersonaError}</Alert>
          ) : planRosterPersona ? (
            <Stack spacing={2}>
              <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-line' }}>
                {planRosterPersona.summary}
              </Typography>
              <Box>
                <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                  Recent Memories
                </Typography>
                {planRosterPersona.memories.length === 0 ? (
                  <Typography variant="body2" color="text.secondary">
                    No memories recorded for this persona.
                  </Typography>
                ) : (
                  <List dense>
                    {planRosterPersona.memories.map((memory, idx) => (
                      <ListItem key={`persona-memory-${idx}`} sx={{ py: 0 }}>
                        <ListItemText primary={memory} />
                      </ListItem>
                    ))}
                  </List>
                )}
              </Box>
              <Box>
                <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                  World Notes
                </Typography>
                {planRosterPersona.worldNotes.length === 0 ? (
                  <Typography variant="body2" color="text.secondary">
                    No world notes captured yet.
                  </Typography>
                ) : (
                  <List dense>
                    {planRosterPersona.worldNotes.map((note, idx) => (
                      <ListItem key={`persona-note-${idx}`} sx={{ py: 0 }}>
                        <ListItemText primary={note} />
                      </ListItem>
                    ))}
                  </List>
                )}
              </Box>
            </Stack>
          ) : (
            <Typography variant="body2" color="text.secondary">
              Author persona context is unavailable for the selected plan.
            </Typography>
          )}
        </CardContent>
      </Card>
      <Card>
        <CardHeader
          title="Backlog Telemetry Feed"
          subheader="Latest workflow events emitted from backlog transitions"
        />
        <CardContent sx={{ px: 0 }}>
          {plannerReport.backlog.telemetryEvents.length === 0 ? (
            <Box sx={{ px: 2, pb: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Telemetry events will appear once backlog runs emit workflow logs.
              </Typography>
            </Box>
          ) : (
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Plan</TableCell>
                  <TableCell>Phase / Status</TableCell>
                  <TableCell>Characters</TableCell>
                  <TableCell>Lore</TableCell>
                  <TableCell>Updated</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {plannerReport.backlog.telemetryEvents.map(event => {
                  const metadataLine = formatTelemetryMetadata(event.metadata);
                  const contextLine = formatTelemetryContext(event.metadata);
                  return (
                    <TableRow key={`${event.planId}-${event.backlogId}-${event.timestampUtc}`}>
                      <TableCell>
                        <Typography variant="body2" sx={{ fontWeight: 600 }}>
                          {event.planName}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {event.backlogId} on branch {event.branch}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Stack direction="row" spacing={0.5} alignItems="center" flexWrap="wrap">
                          <Chip size="small" label={event.phase} />
                          <Chip
                            size="small"
                            label={event.status}
                            color={
                              event.status.toLowerCase() === 'complete'
                                ? 'success'
                                : event.status.toLowerCase() === 'pending'
                                  ? 'warning'
                                  : event.status.toLowerCase() === 'contract' || event.status.toLowerCase().includes('mismatch')
                                    ? 'error'
                                    : 'default'
                            }
                          />
                        </Stack>
                        {event.reason && (
                          <Typography variant="caption" color={event.status.toLowerCase() === 'contract' ? 'error' : 'text.secondary'}>
                            {event.reason}
                          </Typography>
                        )}
                        {metadataLine && (
                          <Typography variant="caption" color="text.secondary" display="block">
                            {metadataLine}
                          </Typography>
                        )}
                        {contextLine && (
                          <Typography variant="caption" color="text.secondary" display="block">
                            {contextLine}
                          </Typography>
                        )}
                      </TableCell>
                      <TableCell>
                        {event.characterMetrics ? (
                          <Typography variant="body2">
                            {event.characterMetrics.personaLinked}/{event.characterMetrics.total} persona-linked
                          </Typography>
                        ) : (
                          <Typography variant="body2" color="text.secondary">—</Typography>
                        )}
                      </TableCell>
                      <TableCell>
                        {event.loreMetrics ? (
                          <Typography variant="body2">
                            {event.loreMetrics.ready}/{event.loreMetrics.total} ready
                          </Typography>
                        ) : (
                          <Typography variant="body2" color="text.secondary">—</Typography>
                        )}
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">{formatRelativeTime(event.timestampUtc)}</Typography>
                        {event.iteration && (
                          <Typography variant="caption" color="text.secondary">
                            Iteration {event.iteration}
                          </Typography>
                        )}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
      <Card>
        <CardHeader
          title="Console Action Log"
          subheader="Recent backlog resumes and API actions"
        />
        <CardContent sx={{ px: 0 }}>
          {plannerReport.backlog.actionLogs.length === 0 ? (
            <Box sx={{ px: 2, pb: 2 }}>
              <Typography variant="body2" color="text.secondary">
                No API or console backlog actions recorded.
              </Typography>
            </Box>
          ) : (
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Plan</TableCell>
                  <TableCell>Action</TableCell>
                  <TableCell>Actor</TableCell>
                  <TableCell>Timestamp</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {plannerReport.backlog.actionLogs.map(log => {
                  const metadataLine = buildActionMetadataLine(log as any);
                  const contextLine = buildActionContextLine(log as any);
                  return (
                    <TableRow key={`${log.planId}-${log.backlogId}-${log.timestampUtc}`}>
                      <TableCell>
                        <Typography variant="body2" sx={{ fontWeight: 600 }}>
                          {log.planName}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {log.backlogId} on branch {log.branch}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Stack direction="row" spacing={0.5} alignItems="center" flexWrap="wrap">
                          <Chip size="small" label={log.action} />
                          {log.status && <Chip size="small" variant="outlined" label={log.status} />}
                        </Stack>
                        <Typography variant="caption" color="text.secondary">
                          {log.description || 'No description'}
                        </Typography>
                        {metadataLine && (
                          <Typography variant="caption" color="text.secondary" display="block">
                            {metadataLine}
                          </Typography>
                        )}
                        {contextLine && (
                          <Typography variant="caption" color="text.secondary" display="block">
                            {contextLine}
                          </Typography>
                        )}
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">{log.actor ?? 'Unknown'}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          Source: {log.source}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">{formatRelativeTime(log.timestampUtc)}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {formatTimestamp(log.timestampUtc)}
                        </Typography>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
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

function StatusChip({ status }: { status: PlannerHealthStatus }) {
  const color = statusColor(status);
  return <Chip label={status} color={color} />;
}

function statusColor(status: PlannerHealthStatus) {
  switch ((status || '').toString().toLowerCase()) {
    case 'healthy':
      return 'success';
    case 'degraded':
      return 'warning';
    case 'critical':
      return 'error';
    default:
      return 'default';
  }
}

function StatusLine({ label, healthy, detail }: { label: string; healthy: boolean; detail?: string | null }) {
  return (
    <Stack direction="row" spacing={1} alignItems="center">
      {healthy ? <CheckCircleIcon color="success" fontSize="small" /> : <WarningAmberIcon color="warning" fontSize="small" />}
      <Typography variant="body2" sx={{ fontWeight: 600 }}>
        {label}
      </Typography>
      <Typography variant="body2" color="text.secondary">
        {detail}
      </Typography>
    </Stack>
  );
}

function BacklogAnomalyList({
  title,
  items,
  emptyMessage
}: {
  title: string;
  items: PlannerHealthBacklogItem[];
  emptyMessage: string;
}) {
  return (
    <Box>
      <Typography variant="subtitle2" color="text.secondary">
        {title}
      </Typography>
      {items.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          {emptyMessage}
        </Typography>
      ) : (
        <List dense>
          {items.map(item => {
            const freshness = item.staleDuration
              ? formatDuration(item.staleDuration)
              : formatRelativeTime(item.updatedAtUtc);
            return (
              <ListItem key={`${item.planId}-${item.backlogId}`} alignItems="flex-start">
                <ListItemText
                  primary={
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {item.planName} • {item.backlogId}
                    </Typography>
                  }
                  secondary={
                    <Typography variant="caption" color="text.secondary" component="span">
                      {item.description}
                      <br />
                      {describeBacklogStatus(item.status)} • {freshness}
                    </Typography>
                  }
                />
              </ListItem>
            );
          })}
        </List>
      )}
    </Box>
  );
}

function mapServerAlert(alert: PlannerHealthAlert): AlertDescriptor {
  const normalized = (alert.severity || 'info').toString().toLowerCase();
  const severity: AlertDescriptor['severity'] =
    normalized === 'error'
      ? 'error'
      : normalized === 'warning'
        ? 'warning'
        : normalized === 'success'
          ? 'success'
          : 'info';
  return {
    id: `planner-alert-${alert.id}`,
    severity,
    title: alert.title,
    description: alert.description
  };
}

function buildOpenSearchAlerts(openSearch: OpenSearchDiagnosticsReport): AlertDescriptor[] {
  const entries: AlertDescriptor[] = [];

  if (!openSearch.clusterAvailable) {
    entries.push({
      id: 'opensearch-cluster',
      severity: 'error',
      title: 'OpenSearch unavailable',
      description: 'Cluster health check failed; embeddings and retrieval may be degraded.'
    });
  }

  if (!openSearch.indexExists) {
    entries.push({
      id: 'opensearch-index',
      severity: 'warning',
      title: 'Default index missing',
      description: `Index ${openSearch.defaultIndex} is not reachable.`
    });
  }

  if (!openSearch.pipelineExists) {
    entries.push({
      id: 'opensearch-pipeline',
      severity: 'warning',
      title: 'Ingest pipeline missing',
      description: `Pipeline ${openSearch.pipelineId ?? '(not configured)'} is unavailable.`
    });
  }

  return entries;
}

function describeBacklogStatus(status: string | number | undefined) {
  const normalized = typeof status === 'number' ? status.toString() : (status ?? '').toString();
  switch (normalized.toLowerCase()) {
    case '0':
    case 'pending':
      return 'Pending';
    case '1':
    case 'inprogress':
    case 'in_progress':
      return 'In Progress';
    case '2':
    case 'complete':
      return 'Complete';
    default:
      return normalized || 'Unknown';
  }
}

function formatTelemetryMetadata(metadata?: Record<string, string | null> | null) {
  if (!metadata) {
    return null;
  }
  const segments: string[] = [];
  if (metadata.providerId) segments.push(`Provider ${formatIdFragment(metadata.providerId)}`);
  if (metadata.modelId) segments.push(`Model ${formatIdFragment(metadata.modelId)}`);
  if (metadata.agentId) segments.push(`Agent ${formatIdFragment(metadata.agentId)}`);
  return segments.length > 0 ? segments.join(' • ') : null;
}

function formatTelemetryContext(metadata?: Record<string, string | null> | null) {
  if (!metadata) {
    return null;
  }
  const segments: string[] = [];
  if (metadata.conversationPlanId) segments.push(`Plan ${formatIdFragment(metadata.conversationPlanId)}`);
  if (metadata.conversationId) segments.push(`Conversation ${formatIdFragment(metadata.conversationId)}`);
  if (metadata.taskId) segments.push(`Task ${formatIdFragment(metadata.taskId)}`);
  return segments.length > 0 ? segments.join(' • ') : null;
}

function formatTimestamp(value?: string | null) {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(date);
}

function formatRelativeTime(value?: string | null) {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  const diffMs = Date.now() - date.getTime();
  const future = diffMs < 0;
  const minutes = Math.floor(Math.abs(diffMs) / 60000);
  if (minutes < 1) return future ? 'in moments' : 'just now';
  if (minutes < 60) return `${minutes}m ${future ? 'from now' : 'ago'}`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ${minutes % 60}m ${future ? 'from now' : 'ago'}`;
  const days = Math.floor(hours / 24);
  return `${days}d ${hours % 24}h ${future ? 'from now' : 'ago'}`;
}

function formatDuration(value?: string | number | null) {
  if (value === null || value === undefined) return '—';
  if (typeof value === 'number') {
    const seconds = value % 60;
    const minutes = Math.floor(value / 60) % 60;
    const hours = Math.floor(value / 3600);
    const segments = [];
    if (hours) segments.push(`${hours}h`);
    if (minutes) segments.push(`${minutes}m`);
    if (seconds) segments.push(`${seconds}s`);
    return segments.join(' ') || `${value}s`;
  }
  const raw = value.toString();
  const [dayPortion, timePortion] = raw.includes('.') ? raw.split('.') : [undefined, raw];
  const [hoursStr, minutesStr, secondsStr] = (timePortion ?? '').split(':');
  const segments: string[] = [];
  if (dayPortion && dayPortion !== '0') {
    segments.push(`${dayPortion}d`);
  }
  if (hoursStr && hoursStr !== '00') {
    segments.push(`${parseInt(hoursStr, 10)}h`);
  }
  if (minutesStr && minutesStr !== '00') {
    segments.push(`${parseInt(minutesStr, 10)}m`);
  }
  if (secondsStr && secondsStr !== '00') {
    const secondsValue = parseInt(secondsStr, 10);
    if (!Number.isNaN(secondsValue)) {
      segments.push(`${secondsValue}s`);
    }
  }
  return segments.length > 0 ? segments.join(' ') : raw;
}

function formatNumber(value: number) {
  return new Intl.NumberFormat().format(value);
}

function formatIdFragment(value: string) {
  const trimmed = value.replace(/-/g, '');
  return trimmed.length <= 8 ? trimmed : trimmed.slice(0, 8);
}
