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
  FictionLoreRequirementItem,
  FictionPlanRoster,
  FictionPlanSummary
} from '../types/fiction';
import { FictionRosterPanel } from '../components/fiction/FictionRosterPanel';

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
      } catch (err) {
        const message = err instanceof ApiError ? err.message : 'Unable to fulfill lore requirement.';
        setRosterError(message);
      }
    },
    [token, selectedPlanId, roster?.branchSlug]
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
          Inspect tracked characters and lore for each fiction plan.
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
    </Stack>
  );
}
