import React from 'react';
import {
  Alert,
  Box,
  Card,
  CardContent,
  CardHeader,
  Grid,
  LinearProgress,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  Stack,
  Typography
} from '@mui/material';
import { ApiError, fictionApi } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { useSecurity } from '../hooks/useSecurity';
import {
  AuthorPersonaContext,
  FictionBacklogItem,
  BacklogActionLog,
  FictionLoreRequirementItem,
  FictionPlanRoster,
  FictionPlanSummary,
  ResumeBacklogPayload,
  LoreFulfillmentLog
} from '../types/fiction';
import { FictionRosterPanel } from '../components/fiction/FictionRosterPanel';
import { FictionBacklogPanel } from '../components/fiction/FictionBacklogPanel';
import { FictionResumeBacklogDialog } from '../components/fiction/FictionResumeBacklogDialog';

export default function FictionProjectsPage() {
  const { auth } = useAuth();
  const { isAdmin } = useSecurity();
  const token = auth?.accessToken;

  const [plans, setPlans] = React.useState<FictionPlanSummary[]>([]);
  const [plansLoading, setPlansLoading] = React.useState(true);
  const [plansError, setPlansError] = React.useState<string | null>(null);
  const [selectedPlanId, setSelectedPlanId] = React.useState<string | null>(null);
  const [roster, setRoster] = React.useState<FictionPlanRoster | null>(null);
  const [rosterLoading, setRosterLoading] = React.useState(false);
  const [rosterError, setRosterError] = React.useState<string | null>(null);
  const [personaContext, setPersonaContext] = React.useState<AuthorPersonaContext | null>(null);
  const [personaLoading, setPersonaLoading] = React.useState(false);
  const [personaError, setPersonaError] = React.useState<string | null>(null);
  const [backlogItems, setBacklogItems] = React.useState<FictionBacklogItem[]>([]);
  const [backlogLoading, setBacklogLoading] = React.useState(false);
  const [backlogError, setBacklogError] = React.useState<string | null>(null);
  const [actionLogs, setActionLogs] = React.useState<BacklogActionLog[]>([]);
  const [actionLoading, setActionLoading] = React.useState(false);
  const [actionError, setActionError] = React.useState<string | null>(null);
  const [resumeTarget, setResumeTarget] = React.useState<FictionBacklogItem | null>(null);
  const [resuming, setResuming] = React.useState(false);
  const [resumeError, setResumeError] = React.useState<string | null>(null);
  const [loreHistory, setLoreHistory] = React.useState<Record<string, LoreFulfillmentLog[]>>({});
  const [loreHistoryLoading, setLoreHistoryLoading] = React.useState(false);
  const [loreHistoryError, setLoreHistoryError] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (!token) {
      setPlans([]);
      setSelectedPlanId(null);
      return;
    }
    let cancelled = false;
    setPlansLoading(true);
    setPlansError(null);
    fictionApi
      .listPlans(token)
      .then(data => {
        if (!cancelled) {
          setPlans(data);
          setSelectedPlanId(prev => {
            if (prev && data.some(plan => plan.id === prev)) {
              return prev;
            }
            return data.length > 0 ? data[0].id : null;
          });
        }
      })
      .catch(err => {
        if (!cancelled) {
          const message = err instanceof ApiError ? err.message : 'Unable to load fiction plans.';
          setPlansError(message);
          setPlans([]);
          setSelectedPlanId(null);
        }
      })
      .finally(() => {
        if (!cancelled) {
          setPlansLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [token]);

  React.useEffect(() => {
    if (!token || !selectedPlanId) {
      setRoster(null);
      return;
    }
    let cancelled = false;
    setRosterLoading(true);
    setRosterError(null);
    fictionApi
      .getPlanRoster(selectedPlanId, token)
      .then(data => {
        if (!cancelled) {
          setRoster(data);
        }
      })
      .catch(err => {
        if (!cancelled) {
          const message = err instanceof ApiError ? err.message : 'Unable to load roster.';
          setRosterError(message);
          setRoster(null);
        }
      })
      .finally(() => {
        if (!cancelled) {
          setRosterLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [token, selectedPlanId]);

  React.useEffect(() => {
    if (!token || !selectedPlanId) {
      setPersonaContext(null);
      setPersonaLoading(false);
      setPersonaError(null);
      return;
    }
    let cancelled = false;
    setPersonaLoading(true);
    setPersonaError(null);
    fictionApi
      .getAuthorPersonaContext(selectedPlanId, token)
      .then(data => {
        if (!cancelled) {
          setPersonaContext(data);
        }
      })
      .catch(err => {
        if (!cancelled) {
          const message = err instanceof ApiError ? err.message : 'Unable to load author persona context.';
          setPersonaError(message);
          setPersonaContext(null);
        }
      })
      .finally(() => {
        if (!cancelled) {
          setPersonaLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [token, selectedPlanId]);

  const fetchBacklog = React.useCallback(
    async (isCancelled?: () => boolean) => {
      if (!token || !selectedPlanId) {
        setBacklogItems([]);
        setBacklogError(null);
        setBacklogLoading(false);
        return;
      }

      setBacklogLoading(true);
      setBacklogError(null);
      try {
        const data = await fictionApi.getPlanBacklog(selectedPlanId, token);
        if (isCancelled?.()) return;
        setBacklogItems(data);
      } catch (err) {
        if (isCancelled?.()) return;
        const message = err instanceof ApiError ? err.message : 'Unable to load backlog.';
        setBacklogError(message);
        setBacklogItems([]);
      } finally {
        if (isCancelled?.()) return;
        setBacklogLoading(false);
      }
    },
    [token, selectedPlanId]
  );
  const fetchLoreHistory = React.useCallback(
    async (isCancelled?: () => boolean) => {
      if (!token || !selectedPlanId) {
        setLoreHistory({});
        setLoreHistoryError(null);
        setLoreHistoryLoading(false);
        return;
      }

      setLoreHistoryLoading(true);
      setLoreHistoryError(null);
      try {
        const data = await fictionApi.getLoreHistory(selectedPlanId, token);
        if (isCancelled?.()) return;
        const grouped: Record<string, LoreFulfillmentLog[]> = {};
        data.forEach(entry => {
          const key = entry.requirementId.toLowerCase();
          if (!grouped[key]) {
            grouped[key] = [];
          }
          grouped[key].push(entry);
        });
        setLoreHistory(grouped);
      } catch (err) {
        if (isCancelled?.()) return;
        const message = err instanceof ApiError ? err.message : 'Unable to load lore history.';
        setLoreHistoryError(message);
        setLoreHistory({});
      } finally {
        if (isCancelled?.()) return;
        setLoreHistoryLoading(false);
      }
    },
    [token, selectedPlanId]
  );

  const fetchActionLogs = React.useCallback(
    async (isCancelled?: () => boolean) => {
      if (!token || !selectedPlanId) {
        setActionLogs([]);
        setActionError(null);
        setActionLoading(false);
        return;
      }

      setActionLoading(true);
      setActionError(null);
      try {
        const data = await fictionApi.getBacklogActions(selectedPlanId, token);
        if (isCancelled?.()) return;
        setActionLogs(data);
      } catch (err) {
        if (isCancelled?.()) return;
        const message = err instanceof ApiError ? err.message : 'Unable to load backlog action logs.';
        setActionError(message);
        setActionLogs([]);
      } finally {
        if (isCancelled?.()) return;
        setActionLoading(false);
      }
    },
    [token, selectedPlanId]
  );

  React.useEffect(() => {
    let cancelled = false;
    fetchBacklog(() => cancelled);
    return () => {
      cancelled = true;
    };
  }, [fetchBacklog]);

  React.useEffect(() => {
    let cancelled = false;
    fetchLoreHistory(() => cancelled);
    return () => {
      cancelled = true;
    };
  }, [fetchLoreHistory]);

  React.useEffect(() => {
    let cancelled = false;
    fetchActionLogs(() => cancelled);
    return () => {
      cancelled = true;
    };
  }, [fetchActionLogs]);

  const handleResume = React.useCallback((item: FictionBacklogItem) => {
    setResumeError(null);
    setResumeTarget(item);
  }, []);

  const handleResumeSubmit = React.useCallback(
    async (payload: ResumeBacklogPayload) => {
      if (!token || !selectedPlanId || !resumeTarget) {
        return;
      }
      setResuming(true);
      setResumeError(null);
      try {
        await fictionApi.resumeBacklog(selectedPlanId, resumeTarget.backlogId, payload, token);
        setResumeTarget(null);
        await fetchBacklog();
        await fetchActionLogs();
      } catch (err) {
        const message = err instanceof ApiError ? err.message : 'Backlog resume failed.';
        setResumeError(message);
      } finally {
        setResuming(false);
      }
    },
    [token, selectedPlanId, resumeTarget, fetchBacklog, fetchActionLogs]
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
        const refreshed = await fictionApi.getPlanRoster(selectedPlanId, token);
        setRoster(refreshed);
        await fetchLoreHistory();
      } catch (err) {
        const message = err instanceof ApiError ? err.message : 'Unable to fulfill lore requirement.';
        setRosterError(message);
      }
    },
    [token, selectedPlanId, roster?.branchSlug, fetchLoreHistory]
  );

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
      <Grid container spacing={3}>
        <Grid item xs={12} md={4}>
          <Card>
            <CardHeader title="Plans" />
            <CardContent sx={{ px: 0 }}>
              {plansLoading && <LinearProgress />}
              {plansError && (
                <Box sx={{ px: 2, pb: 2 }}>
                  <Alert severity="warning">{plansError}</Alert>
                </Box>
              )}
              {!plansLoading && !plansError && (
                <List dense disablePadding>
                  {plans.map(plan => (
                    <ListItem disablePadding key={plan.id}>
                      <ListItemButton
                        selected={plan.id === selectedPlanId}
                        onClick={() => setSelectedPlanId(plan.id)}
                      >
                        <ListItemText
                          primary={plan.name}
                          secondary={plan.projectTitle || plan.status}
                        />
                      </ListItemButton>
                    </ListItem>
                  ))}
                  {plans.length === 0 && (
                    <Box sx={{ px: 2, py: 2 }}>
                      <Typography variant="body2" color="text.secondary">
                        No fiction plans found.
                      </Typography>
                    </Box>
                  )}
                </List>
              )}
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={8}>
          <Stack spacing={3}>
            <Card>
              <CardHeader
                title="Backlog"
                subheader={
                  roster
                    ? `${backlogItems.length} tracked items`
                    : 'View pending planner work and resume blocked steps.'
                }
              />
              <CardContent>
                <FictionBacklogPanel
                  items={backlogItems}
                  loading={backlogLoading}
                  error={backlogError}
                  placeholder={plans.length === 0 ? 'Create a plan to populate backlog data.' : 'Select a plan to inspect its backlog.'}
                  onResume={isAdmin ? handleResume : undefined}
                  resumingId={resuming && resumeTarget ? resumeTarget.id : null}
                  isAdmin={isAdmin}
                  actionLogs={actionLogs}
                  actionLoading={actionLoading}
                  actionError={actionError}
                />
              </CardContent>
            </Card>
            <Card>
              <CardHeader
                title={roster ? roster.planName : 'Roster'}
                subheader={roster?.projectTitle}
              />
              <CardContent>
                <FictionRosterPanel
                  roster={roster}
                  loading={rosterLoading}
                  error={rosterError}
                  placeholder={plans.length === 0 ? 'No fiction plans available.' : 'Select a plan to load its roster.'}
                  onFulfillLore={handleFulfillLore}
                  loreHistory={loreHistory}
                  loreHistoryLoading={loreHistoryLoading}
                  loreHistoryError={loreHistoryError}
                />
              </CardContent>
            </Card>
            <Card>
              <CardHeader
                title="Author Persona"
                subheader={personaContext ? personaContext.personaName : undefined}
              />
              <CardContent>
                {personaLoading ? (
                  <LinearProgress />
                ) : personaError ? (
                  <Alert severity="warning">{personaError}</Alert>
                ) : personaContext ? (
                  <Stack spacing={2}>
                    <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-line' }}>
                      {personaContext.summary}
                    </Typography>
                    <Box>
                      <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                        Recent Memories
                      </Typography>
                      {personaContext.memories.length === 0 ? (
                        <Typography variant="body2" color="text.secondary">
                          No memories recorded yet.
                        </Typography>
                      ) : (
                        <List dense>
                          {personaContext.memories.map((memory, idx) => (
                            <ListItem key={`memory-${idx}`} sx={{ py: 0 }}>
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
                      {personaContext.worldNotes.length === 0 ? (
                        <Typography variant="body2" color="text.secondary">
                          No world notes captured.
                        </Typography>
                      ) : (
                        <List dense>
                          {personaContext.worldNotes.map((note, idx) => (
                            <ListItem key={`note-${idx}`} sx={{ py: 0 }}>
                              <ListItemText primary={note} />
                            </ListItem>
                          ))}
                        </List>
                      )}
                    </Box>
                  </Stack>
                ) : (
                  <Typography variant="body2" color="text.secondary">
                    Select a plan with an author persona to review recent memories and notes.
                  </Typography>
                )}
              </CardContent>
            </Card>
          </Stack>
        </Grid>
      </Grid>
      <FictionResumeBacklogDialog
        open={Boolean(resumeTarget)}
        item={resumeTarget}
        defaultBranch={roster?.branchSlug ?? undefined}
        submitting={resuming}
        error={resumeError}
        onClose={handleResumeDialogClose}
        onSubmit={handleResumeSubmit}
        accessToken={token}
      />
    </Stack>
  );
}
