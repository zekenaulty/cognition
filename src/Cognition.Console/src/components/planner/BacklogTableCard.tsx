import React from 'react';
import { Card, CardContent, CardHeader, Chip, FormControl, InputLabel, MenuItem, Select, Stack, Typography } from '@mui/material';
import { FictionBacklogPanel } from '../fiction/FictionBacklogPanel';
import { PlannerHealthReport } from '../../types/diagnostics';
import { BacklogActionLog, FictionBacklogItem, PersonaObligation } from '../../types/fiction';

type Props = {
  plannerReport: PlannerHealthReport;
  rosterPlanId: string | null;
  setRosterPlanId: (id: string | null) => void;
  planBacklogItems: FictionBacklogItem[];
  planOpenObligationsCount: number;
  resolvedObligationCount: number;
  dismissedObligationCount: number;
  planActionLogs: BacklogActionLog[];
  planObligations: PersonaObligation[];
  fictionLoading: boolean;
  fictionError: string | null;
  resuming: boolean;
  resumeTargetId: string | null;
  isAdmin: boolean;
  onResumeBacklog: (item: FictionBacklogItem) => void;
  onResolveObligation: (obligation: PersonaObligation, action: 'resolve' | 'dismiss') => void;
  obligationActionId: string | null;
  obligationActionError: string | null;
};

export function BacklogTableCard({
  plannerReport,
  rosterPlanId,
  setRosterPlanId,
  planBacklogItems,
  planOpenObligationsCount,
  resolvedObligationCount,
  dismissedObligationCount,
  planActionLogs,
  planObligations,
  fictionLoading,
  fictionError,
  resuming,
  resumeTargetId,
  isAdmin,
  onResumeBacklog,
  onResolveObligation,
  obligationActionId,
  obligationActionError,
}: Props) {
  return (
    <Card>
      <CardHeader
        title="Plan Backlog"
        subheader={
          rosterPlanId
            ? `${plannerReport.backlog.plans.find(plan => plan.planId === rosterPlanId)?.planName ?? 'Backlog items'}  · ${planBacklogItems.length} items  · ${planOpenObligationsCount} open obligations`
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
                <Chip size="small" color="warning" label={`${planOpenObligationsCount} open obligations`} />
                <Chip size="small" color="success" variant="outlined" label={`${resolvedObligationCount} resolved`} />
                <Chip size="small" color="default" variant="outlined" label={`${dismissedObligationCount} dismissed`} />
              </Stack>
            )}
            <FictionBacklogPanel
              items={planBacklogItems}
              loading={fictionLoading}
              error={fictionError}
              placeholder="Select a plan to inspect its backlog."
              onResume={onResumeBacklog}
              resumingId={resuming && resumeTargetId ? resumeTargetId : null}
              isAdmin={isAdmin}
              actionLogs={planActionLogs}
              actionLoading={fictionLoading}
              actionError={fictionError}
              obligations={planObligations}
              obligationsLoading={fictionLoading}
              obligationsError={fictionError}
              onResolveObligation={onResolveObligation}
              obligationActionId={obligationActionId}
              obligationActionError={obligationActionError}
              personaContext={null}
            />
          </>
        )}
      </CardContent>
    </Card>
  );
}
