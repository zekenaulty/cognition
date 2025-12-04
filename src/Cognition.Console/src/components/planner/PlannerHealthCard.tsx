import React from 'react';
import { Alert, Card, CardContent, CardHeader, Chip, Divider, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material';
import InsightsIcon from '@mui/icons-material/Insights';
import { PlannerHealthReport, PlannerHealthStatus } from '../../types/diagnostics';
import { formatNumber } from './numberFormatters';
import { formatRelativeTime } from './timeFormatters';

type Props = {
  plannerReport: PlannerHealthReport;
};

export function PlannerHealthCard({ plannerReport }: Props) {
  const backlogTotals = [
    { label: 'Pending', value: plannerReport.backlog.pending, color: 'warning' as const },
    { label: 'In Progress', value: plannerReport.backlog.inProgress, color: 'info' as const },
    { label: 'Complete', value: plannerReport.backlog.complete, color: 'success' as const }
  ];

  return (
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
              {plannerReport.planners.length} Planners ·{' '}
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
                    {plan.lastUpdatedUtc ? formatRelativeTime(plan.lastUpdatedUtc) : '-'}
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
