import React from 'react';
import { Card, CardContent, CardHeader, FormControl, InputLabel, MenuItem, Select, Typography } from '@mui/material';
import { PlannerHealthReport } from '../../types/diagnostics';
import { FictionPlanRoster, LoreFulfillmentLog } from '../../types/fiction';
import { FictionRosterPanel } from '../fiction/FictionRosterPanel';

type Props = {
  plannerReport: PlannerHealthReport;
  rosterPlanId: string | null;
  setRosterPlanId: (id: string | null) => void;
  planRoster: FictionPlanRoster | null;
  planLoreHistory: Record<string, LoreFulfillmentLog[]>;
  loading: boolean;
  error: string | null;
  onFulfillLore: (req: any) => Promise<void>;
};

export function RosterCard({
  plannerReport,
  rosterPlanId,
  setRosterPlanId,
  planRoster,
  planLoreHistory,
  loading,
  error,
  onFulfillLore
}: Props) {
  return (
    <Card>
      <CardHeader
        title="Character & Lore Roster"
        subheader={
          planRoster
            ? `${planRoster.planName}${planRoster.projectTitle ? ` · ${planRoster.projectTitle}` : ''}`
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
            loading={loading}
            error={error}
            onFulfillLore={onFulfillLore}
            loreHistory={planLoreHistory}
            loreHistoryLoading={loading}
            loreHistoryError={error}
          />
        )}
      </CardContent>
    </Card>
  );
}
