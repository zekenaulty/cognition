import React from 'react';
import { Alert, Box, Button, Card, CardContent, CardHeader, Chip, Stack, Typography } from '@mui/material';
import { PlannerHealthAlert } from '../../types/diagnostics';
import { FictionBacklogItem, PersonaObligation } from '../../types/fiction';

type Props = {
  rosterPlanId: string | null;
  alerts: PlannerHealthAlert[];
  planAlertCount: number;
  planContractEvents: any[];
  planStaleBacklogItems: FictionBacklogItem[];
  planMissingLoreRequirements: any[];
  planOpenObligations: PersonaObligation[];
  planDriftObligations: PersonaObligation[];
  planAgingObligations: PersonaObligation[];
  onResumeClick: () => void;
  onLoreClick: () => void;
  onObligationClick: () => void;
};

export function BacklogAlertsCard({
  rosterPlanId,
  alerts,
  planAlertCount,
  planContractEvents,
  planStaleBacklogItems,
  planMissingLoreRequirements,
  planOpenObligations,
  planDriftObligations,
  planAgingObligations,
  onResumeClick,
  onLoreClick,
  onObligationClick,
}: Props) {
  return (
    <Card>
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
                  <Button color="inherit" size="small" onClick={onResumeClick}>
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
                  <Button color="inherit" size="small" onClick={onLoreClick}>
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
                  <Button color="inherit" size="small" onClick={onObligationClick}>
                    Resolve
                  </Button>
                }
              >
                {planOpenObligations.length} persona obligation
                {planOpenObligations.length === 1 ? '' : 's'} require attention. Use the modal to resolve or dismiss them.
              </Alert>
            )}
            {planDriftObligations.length > 0 && (
              <Alert severity="warning">
                {planDriftObligations.length} obligation{planDriftObligations.length === 1 ? '' : 's'} flagged with voice drift.
                Review and resolve to restore narrative alignment.
              </Alert>
            )}
            {planAgingObligations.length > 0 && (
              <Alert severity="info">
                {planAgingObligations.length} obligation{planAgingObligations.length === 1 ? '' : 's'} aging beyond the freshness window.
                Consider resolving or dismissing stale obligations.
              </Alert>
            )}
            {alerts.length > 0 && (
              <Box sx={{ pt: 1 }}>
                <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                  {alerts.map(alert => (
                    <Chip
                      key={alert.id}
                      color={alert.severity === 'error' ? 'error' : alert.severity === 'warning' ? 'warning' : 'info'}
                      variant="outlined"
                      label={alert.title}
                    />
                  ))}
                </Stack>
              </Box>
            )}
          </Stack>
        )}
      </CardContent>
    </Card>
  );
}
