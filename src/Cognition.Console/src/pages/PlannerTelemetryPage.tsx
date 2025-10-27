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
  Grid,
  IconButton,
  LinearProgress,
  List,
  ListItem,
  ListItemText,
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
import { ApiError, diagnosticsApi } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { useSecurity } from '../hooks/useSecurity';
import {
  OpenSearchDiagnosticsReport,
  PlannerHealthAlert,
  PlannerHealthBacklogItem,
  PlannerHealthReport,
  PlannerHealthStatus
} from '../types/diagnostics';

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

  const serverAlerts = React.useMemo(() => {
    if (!plannerReport) {
      return [] as AlertDescriptor[];
    }
    return (plannerReport.alerts ?? []).map(mapServerAlert);
  }, [plannerReport]);

  const alerts = React.useMemo(() => {
    if (!openSearchReport) {
      return serverAlerts;
    }
    return [...serverAlerts, ...buildOpenSearchAlerts(openSearchReport)];
  }, [serverAlerts, openSearchReport]);

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
