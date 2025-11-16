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
import { FictionPlanRoster, FictionPlanSummary } from '../types/fiction';
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
              />
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Stack>
  );
}
