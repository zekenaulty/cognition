import React from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  LinearProgress,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  Typography
} from '@mui/material';
import { FictionPlanSummary } from '../../types/fiction';

type Props = {
  plans: FictionPlanSummary[];
  selectedPlanId: string | null;
  plansLoading: boolean;
  plansError: string | null;
  onSelectPlan: (planId: string) => void;
  onCreatePlan: () => void;
};

export function FictionPlanListCard({
  plans,
  selectedPlanId,
  plansLoading,
  plansError,
  onSelectPlan,
  onCreatePlan,
}: Props) {
  return (
    <Card>
      <CardHeader
        title="Plans"
        action={
          <Button
            variant="contained"
            size="small"
            onClick={onCreatePlan}
            disabled={plansLoading}
          >
            New Plan
          </Button>
        }
      />
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
                  onClick={() => onSelectPlan(plan.id)}
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
  );
}
