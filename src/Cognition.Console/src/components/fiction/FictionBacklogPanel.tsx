import React from 'react';
import {
  Alert,
  Box,
  Button,
  ButtonGroup,
  Chip,
  LinearProgress,
  List,
  ListItem,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tooltip,
  Typography
} from '@mui/material';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import CheckIcon from '@mui/icons-material/Check';
import HighlightOffIcon from '@mui/icons-material/HighlightOff';
import {
  AuthorPersonaContext,
  BacklogActionLog,
  FictionBacklogItem,
  PersonaObligation
} from '../../types/fiction';
import {
  buildActionContextLine,
  buildActionMetadataLine,
  formatObligationStatus,
  formatResolutionNoteText,
  isObligationOpen,
  summarizeObligationMetadata
} from './backlogUtils';

type Props = {
  items?: FictionBacklogItem[] | null;
  loading: boolean;
  error?: string | null;
  placeholder?: string;
  onResume?: (item: FictionBacklogItem) => void;
  resumingId?: string | null;
  isAdmin?: boolean;
  actionLogs?: BacklogActionLog[] | null;
  actionLoading?: boolean;
  actionError?: string | null;
  obligations?: PersonaObligation[] | null;
  obligationsLoading?: boolean;
  obligationsError?: string | null;
  onResolveObligation?: (obligation: PersonaObligation, action: 'resolve' | 'dismiss') => void;
  obligationActionId?: string | null;
  obligationActionError?: string | null;
  personaContext?: AuthorPersonaContext | null;
};

export function FictionBacklogPanel({
  items,
  loading,
  error,
  placeholder = 'Select a plan to inspect its backlog.',
  onResume,
  resumingId,
  isAdmin = false,
  actionLogs = [],
  actionLoading = false,
  actionError = null,
  obligations = [],
  obligationsLoading = false,
  obligationsError = null,
  onResolveObligation,
  obligationActionId = null,
  obligationActionError = null,
  personaContext = null
}: Props) {
  const backlogItems = items ?? [];
  const contractItems = React.useMemo(
    () => backlogItems.filter(item => (item.status ?? '').toString().toLowerCase() === 'contract'),
    [backlogItems]
  );

  const obligationsByBacklogId = React.useMemo(() => {
    if (!obligations || obligations.length === 0) {
      return new Map<string, PersonaObligation[]>();
    }
    const map = new Map<string, PersonaObligation[]>();
    obligations.forEach(entry => {
      if (!entry.sourceBacklogId) {
        return;
      }
      const key = entry.sourceBacklogId.toLowerCase();
      const bucket = map.get(key);
      if (bucket) {
        bucket.push(entry);
      } else {
        map.set(key, [entry]);
      }
    });
    return map;
  }, [obligations]);

  if (loading) {
    return <LinearProgress />;
  }

  if (error) {
    return <Alert severity="warning">{error}</Alert>;
  }

  const renderTable = () => {
    if (backlogItems.length === 0) {
      return (
        <Typography variant="body2" color="text.secondary">
          {placeholder}
        </Typography>
      );
    }

    return (
      <Stack spacing={1.5}>
        {contractItems.length > 0 && (
          <Alert severity="error">
            {contractItems.length} backlog item{contractItems.length === 1 ? '' : 's'} flagged for contract drift. Fix provider/model/agent/task metadata before resuming.
          </Alert>
        )}
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Backlog Item</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Task</TableCell>
              <TableCell>Updated</TableCell>
              {isAdmin && onResume ? <TableCell align="right">Actions</TableCell> : null}
            </TableRow>
          </TableHead>
          <TableBody>
          {backlogItems.map(item => {
            const resumeBlockedReason = getResumeBlockedReason(item, {
              isAdmin,
              hasResumeHandler: Boolean(onResume)
            });
            const isBusy = resumingId === item.id;
            const resumeDisabled = Boolean(resumeBlockedReason) || isBusy;
            const backlogKey = (item.backlogId ?? '').toLowerCase();
            const linkedObligations = backlogKey ? obligationsByBacklogId.get(backlogKey) ?? [] : [];
            const openLinkedObligations = linkedObligations.filter(entry => isObligationOpen(entry.status));
            const metadataChips = buildBacklogMetadataChips(item);
            const resumeButton = (
              <Tooltip
                title={
                  resumeBlockedReason
                    ? resumeBlockedReason
                    : isBusy
                      ? 'Resume request in flight...'
                      : 'Resume backlog item'
                }
                placement="left"
                disableHoverListener={!resumeBlockedReason && !isBusy}
              >
                <span>
                  <Button
                    variant={resumeBlockedReason ? 'outlined' : 'contained'}
                    size="small"
                    startIcon={<PlayArrowIcon />}
                    disabled={resumeDisabled}
                    onClick={() => onResume?.(item)}
                  >
                    {isBusy ? 'Resuming...' : 'Resume'}
                  </Button>
                </span>
              </Tooltip>
            );

            return (
              <TableRow key={item.id} hover>
                <TableCell>
                  <Stack spacing={0.5}>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {item.description || item.backlogId}
                    </Typography>
                    {typeof item.status === 'string' && item.status.toLowerCase() === 'contract' && (
                      <Alert severity="error" variant="outlined" icon={false} sx={{ py: 0.25, px: 1 }}>
                        Contract drift detected; resume blocked until metadata is corrected.
                      </Alert>
                    )}
                    <Typography variant="caption" color="text.secondary">
                      {item.backlogId}
                    </Typography>
                    {item.branchSlug && (
                      <Typography variant="caption" color="text.secondary">
                        Branch: {item.branchSlug}
                      </Typography>
                    )}
                    {metadataChips.length > 0 && (
                      <Stack direction="row" spacing={0.5} flexWrap="wrap">
                        {metadataChips.map((chip, index) => (
                          <Chip key={`${item.id}-meta-${index}`} size="small" variant="outlined" label={chip} />
                        ))}
                      </Stack>
                    )}
                    {linkedObligations.length > 0 && (
                      <Stack spacing={0.25}>
                        <Chip
                          size="small"
                          color={openLinkedObligations.length > 0 ? 'warning' : 'default'}
                          label={`${linkedObligations.length} linked obligation${linkedObligations.length === 1 ? '' : 's'}`}
                        />
                        {openLinkedObligations.length > 0 && (
                          <Typography variant="caption" sx={{ color: 'warning.main' }}>
                            {openLinkedObligations.length} open obligation
                            {openLinkedObligations.length === 1 ? '' : 's'} tied to this backlog item.
                          </Typography>
                        )}
                        {openLinkedObligations.length > 0 && onResolveObligation && (
                          <Button
                            size="small"
                            variant="text"
                            sx={{ alignSelf: 'flex-start', px: 0 }}
                            onClick={() => onResolveObligation(openLinkedObligations[0], 'resolve')}
                          >
                            Review obligation
                          </Button>
                        )}
                      </Stack>
                    )}
                  </Stack>
                </TableCell>
                <TableCell>
                  <StatusChip status={item.status} />
                </TableCell>
                <TableCell>
                  {item.taskId ? (
                    <Stack spacing={0.5}>
                      <Typography variant="body2">
                        {item.toolName || 'Planner'} - Step {item.stepNumber ?? '--'}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Task {item.taskId}
                      </Typography>
                      {item.taskStatus && (
                        <Typography variant="caption" color="text.secondary">
                          Last run: {item.taskStatus}
                        </Typography>
                      )}
                    </Stack>
                  ) : (
                    <Typography variant="body2" color="text.secondary">
                      —
                    </Typography>
                  )}
                </TableCell>
                <TableCell>
                  <Typography variant="body2">{formatRelative(item.updatedAtUtc ?? item.createdAtUtc)}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    Created {formatRelative(item.createdAtUtc)}
                  </Typography>
                </TableCell>
                {isAdmin && onResume ? (
                  <TableCell align="right">
                    {resumeButton}
                  </TableCell>
                ) : null}
              </TableRow>
            );
          })}
        </TableBody>
        </Table>
      </Stack>
    );
  };

  const renderActionLog = () => {
    if (actionLoading) {
      return <LinearProgress sx={{ mt: 1 }} />;
    }

    if (actionError) {
      return (
        <Alert severity="warning" sx={{ mt: 1 }}>
          {actionError}
        </Alert>
      );
    }

    if (!actionLogs || actionLogs.length === 0) {
      return (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
          No recent backlog actions recorded.
        </Typography>
      );
    }

    return (
      <List dense sx={{ mt: 1 }}>
        {actionLogs.map(log => {
          const metadataLine = buildActionMetadataLine(log);
          const contextLine = buildActionContextLine(log);
          return (
            <ListItem key={`${log.backlogId}-${log.timestampUtc}-${log.action}`}>
            <Stack spacing={0.25}>
              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                {log.action} - {log.backlogId}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {log.actor ?? 'Unknown actor'} via {log.source} - {formatRelative(log.timestampUtc)}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                Branch {log.branch}
                {log.status ? ` • Status ${formatStatus(log.status)}` : ''}
              </Typography>
              {log.description && (
                <Typography variant="caption" color="text.secondary">
                  {log.description}
                </Typography>
              )}
              {metadataLine && (
                <Typography variant="caption" color="text.secondary">
                  {metadataLine}
                </Typography>
              )}
              {contextLine && (
                <Typography variant="caption" color="text.secondary">
                  {contextLine}
                </Typography>
              )}
            </Stack>
            </ListItem>
          );
        })}
      </List>
    );
  };

  const renderObligations = () => {
    if (obligationsLoading) {
      return <LinearProgress sx={{ mt: 1 }} />;
    }

    if (obligationsError) {
      return (
        <Alert severity="warning" sx={{ mt: 1 }}>
          {obligationsError}
        </Alert>
      );
    }

    const obligationList = obligations ?? [];
    if (obligationList.length === 0) {
      return (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
          No persona obligations recorded for this plan.
        </Typography>
      );
    }

    const openObligations = obligationList.filter(obligation => {
      const status = (obligation.status ?? '').toLowerCase();
      return status !== 'resolved' && status !== 'dismissed';
    });
    const displayedObligations = obligationList.slice(0, 6);
    const driftedObligations = obligationList.filter(obligation => summarizeObligationMetadata(obligation.metadata).voiceDrift);
    const agingObligations = obligationList.filter(obligation => isObligationAging(obligation.createdAtUtc));

    return (
      <>
        {openObligations.length > 0 && (
          <Alert severity="warning" sx={{ mt: 1 }}>
            {openObligations.length} persona obligation{openObligations.length === 1 ? '' : 's'} awaiting attention.
          </Alert>
        )}
        {driftedObligations.length > 0 && (
          <Alert severity="info" sx={{ mt: 1 }}>
            {driftedObligations.length} obligation{driftedObligations.length === 1 ? '' : 's'} flagged for voice drift.
          </Alert>
        )}
        {agingObligations.length > 0 && (
          <Alert severity="warning" sx={{ mt: 1 }}>
            {agingObligations.length} obligation{agingObligations.length === 1 ? '' : 's'} aging without resolution ({AGING_THRESHOLD_HOURS}h).
          </Alert>
        )}
        <List dense sx={{ mt: 1 }}>
          {displayedObligations.map(obligation => {
            const actionable = typeof onResolveObligation === 'function' && isObligationOpen(obligation.status);
            const busy = obligationActionId === obligation.id;
            const metadataSummary = summarizeObligationMetadata(obligation.metadata);
            const personaHighlight =
              personaContext && personaContext.personaId === obligation.personaId ? personaContext : null;
            const memoryHighlights = personaHighlight?.memories?.slice(0, 2) ?? [];
            const worldNoteHighlights = personaHighlight?.worldNotes?.slice(0, 2) ?? [];
            return (
              <ListItem
                key={obligation.id}
                secondaryAction={
                  actionable ? (
                    <ButtonGroup size="small" orientation="vertical">
                      <Button
                        variant="contained"
                        color="success"
                        startIcon={<CheckIcon />}
                        disabled={busy}
                        onClick={() => onResolveObligation?.(obligation, 'resolve')}
                      >
                        Resolve
                      </Button>
                      <Button
                        variant="outlined"
                        color="warning"
                        startIcon={<HighlightOffIcon />}
                        disabled={busy}
                        onClick={() => onResolveObligation?.(obligation, 'dismiss')}
                      >
                        Dismiss
                      </Button>
                    </ButtonGroup>
                  ) : null
                }
              >
                <Stack spacing={0.25}>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    {obligation.title}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    Persona {obligation.personaName}
                    {obligation.branchSlug ? ` • Branch ${obligation.branchSlug}` : ''}
                    {obligation.sourcePhase ? ` • Source ${obligation.sourcePhase}` : ''}
                    {isObligationAging(obligation.createdAtUtc) ? ' • Aging' : ''}
                    {metadataSummary.voiceDrift ? ' • Voice drift flagged' : ''}
                  </Typography>
                  {obligation.branchLineage && obligation.branchLineage.length > 1 && (
                    <Typography variant="caption" color="text.secondary">
                      {obligation.branchLineage.join(' → ')}
                    </Typography>
                  )}
                  <Typography variant="caption" color="text.secondary">
                    Status {formatObligationStatus(obligation.status)} • Created {formatRelative(obligation.createdAtUtc)}
                    {obligation.resolvedAtUtc ? ` • Resolved ${formatRelative(obligation.resolvedAtUtc)}` : ''}
                  </Typography>
                  {obligation.sourceBacklogId && (
                    <Typography variant="caption" color="text.secondary">
                      Source Backlog: {obligation.sourceBacklogId}
                    </Typography>
                  )}
                  {obligation.description && (
                    <Typography variant="body2" color="text.secondary">
                      {obligation.description}
                    </Typography>
                  )}
                  {metadataSummary.otherEntries.length > 0 && (
                    <Typography variant="caption" color="text.secondary">
                      {metadataSummary.otherEntries.join(' â€¢ ')}
                    </Typography>
                  )}
                  {metadataSummary.resolutionNotes.length > 0 && (
                    <Stack spacing={0.25}>
                      <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600 }}>
                        Resolution history
                      </Typography>
                      {metadataSummary.resolutionNotes.map(note => (
                        <Typography key={note.id} variant="caption" color="text.secondary">
                          {formatResolutionNote(note)}
                        </Typography>
                      ))}
                    </Stack>
                  )}
                  {personaHighlight && (memoryHighlights.length > 0 || worldNoteHighlights.length > 0) && (
                    <Stack spacing={0.25}>
                      {memoryHighlights.map((entry, index) => (
                        <Typography key={`memory-${obligation.id}-${index}`} variant="caption" color="primary">
                          Memory{memoryHighlights.length > 1 ? ` ${index + 1}` : ''}: {entry}
                        </Typography>
                      ))}
                      {worldNoteHighlights.map((entry, index) => (
                        <Typography key={`world-${obligation.id}-${index}`} variant="caption" color="primary">
                          World Note{worldNoteHighlights.length > 1 ? ` ${index + 1}` : ''}: {entry}
                        </Typography>
                      ))}
                    </Stack>
                  )}
                </Stack>
              </ListItem>
            );
          })}
        </List>
        {obligationActionError && (
          <Alert severity="error" sx={{ mt: 1 }}>
            {obligationActionError}
          </Alert>
        )}
        {obligationList.length > displayedObligations.length && (
          <Typography variant="caption" color="text.secondary">
            Showing {displayedObligations.length} of {obligationList.length} obligations.
          </Typography>
        )}
      </>
    );
  };

  return (
    <Stack spacing={3}>
      <Box sx={{ overflowX: 'auto' }}>{renderTable()}</Box>
      <Box>
        <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
          Persona Obligations
        </Typography>
        {renderObligations()}
      </Box>
      <Box>
        <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
          Action Log
        </Typography>
        {renderActionLog()}
      </Box>
    </Stack>
  );
}

function StatusChip({ status }: { status: string }) {
  const normalized = (status || '').toLowerCase();
  let color: 'default' | 'primary' | 'success' | 'warning' | 'error' = 'default';
  if (normalized === 'pending' || normalized === '0') {
    color = 'warning';
  } else if (normalized === 'inprogress' || normalized === 'in_progress' || normalized === '1') {
    color = 'primary';
  } else if (normalized === 'complete' || normalized === '2') {
    color = 'success';
  } else if (normalized === 'failed' || normalized === 'error') {
    color = 'error';
  }

  return <Chip size="small" color={color} label={formatStatus(status)} />;
}

function formatStatus(value?: string | number | null) {
  if (value === null || value === undefined) return 'Unknown';
  const str = value.toString();
  if (!str) return 'Unknown';
  switch (str.toLowerCase()) {
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
      return str.charAt(0).toUpperCase() + str.slice(1);
  }
}

function formatRelative(value?: string | null) {
  if (!value) return '--';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  const diff = Date.now() - date.getTime();
  const ahead = diff < 0;
  const minutes = Math.floor(Math.abs(diff) / 60000);
  if (minutes < 1) return ahead ? 'in moments' : 'just now';
  if (minutes < 60) return `${minutes}m ${ahead ? 'from now' : 'ago'}`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ${minutes % 60}m ${ahead ? 'from now' : 'ago'}`;
  const days = Math.floor(hours / 24);
  return `${days}d ${hours % 24}h ${ahead ? 'from now' : 'ago'}`;
}

function formatResolutionNote(note: ReturnType<typeof summarizeObligationMetadata>['resolutionNotes'][number]) {
  return formatResolutionNoteText(note, formatRelative);
}

function isObligationAging(createdAt?: string | null, thresholdHours = AGING_THRESHOLD_HOURS) {
  if (!createdAt) return false;
  const created = new Date(createdAt);
  if (Number.isNaN(created.getTime())) return false;
  const ageMs = Date.now() - created.getTime();
  return ageMs > thresholdHours * 60 * 60 * 1000;
}

function getResumeBlockedReason(
  item: FictionBacklogItem,
  options: { isAdmin: boolean; hasResumeHandler: boolean }
): string | null {
  if (!options.hasResumeHandler) {
    return 'Resume action is not available for this plan.';
  }

  if (!options.isAdmin) {
    return 'Administrator access required to resume backlog items.';
  }

  if (isBacklogCompleteStatus(item.status)) {
    return 'Backlog item already complete.';
  }

  const missingFields: string[] = [];
  if (!item.conversationPlanId) {
    missingFields.push('conversation plan');
  }
  if (!item.conversationId) {
    missingFields.push('conversation');
  }
  if (!item.taskId) {
    missingFields.push('task');
  }
  if (!item.agentId) {
    missingFields.push('agent');
  }

  if (missingFields.length > 0) {
    return `Missing ${missingFields.join(', ')} metadata`;
  }

  return null;
}

function buildBacklogMetadataChips(item: FictionBacklogItem) {
  const chips: string[] = [];
  if (item.providerId) {
    chips.push(`Provider ${formatIdFragment(item.providerId)}`);
  }
  if (item.modelId) {
    chips.push(`Model ${formatIdFragment(item.modelId)}`);
  }
  if (item.agentId) {
    chips.push(`Agent ${formatIdFragment(item.agentId)}`);
  }
  if (item.conversationPlanId) {
    chips.push(`Plan ${formatIdFragment(item.conversationPlanId)}`);
  }
  if (item.taskId) {
    chips.push(`Task ${formatIdFragment(item.taskId)}`);
  }
  return chips;
}

function isBacklogCompleteStatus(status?: string | number | null) {
  if (status === null || status === undefined) {
    return false;
  }

  const normalized = status.toString().trim().toLowerCase();
  return normalized === 'complete' || normalized === 'completed' || normalized === 'done' || normalized === '2';
}

function formatIdFragment(value?: string | null) {
  if (!value) {
    return '';
  }
  const trimmed = value.replace(/-/g, '');
  return trimmed.length <= 8 ? trimmed : trimmed.slice(0, 8);
}
