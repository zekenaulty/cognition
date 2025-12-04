import React from 'react';
import { Card, CardContent, CardHeader, Chip, Stack, Typography } from '@mui/material';
import { PlannerHealthReport } from '../../types/diagnostics';
import { formatRelativeTime } from './timeFormatters';
import { formatNumber } from './numberFormatters';

type Props = {
  plannerReport: PlannerHealthReport;
};

export function ExecutionTelemetryCard({ plannerReport }: Props) {
  const telemetry = plannerReport.telemetry;
  return (
    <Card>
      <CardHeader title="Execution Telemetry" />
      <CardContent>
        <Stack spacing={1}>
          <Typography variant="h6">{formatNumber(telemetry.totalExecutions)} executions</Typography>
          <Typography variant="body2" color="text.secondary">
            Last execution {formatRelativeTime(telemetry.lastExecutionUtc)}
          </Typography>
          <Stack direction="row" spacing={1} flexWrap="wrap">
            {Object.entries(telemetry.outcomeCounts).map(([outcome, count]) => (
              <Chip key={outcome} label={`${outcome}: ${count}`} size="small" />
            ))}
          </Stack>
          {Object.keys(telemetry.critiqueStatusCounts).length > 0 && (
            <>
              <Typography variant="subtitle2" color="text.secondary" sx={{ mt: 1 }}>
                Critique Budgets
              </Typography>
              <Stack direction="row" spacing={1} flexWrap="wrap">
                {Object.entries(telemetry.critiqueStatusCounts).map(([status, count]) => (
                  <Chip
                    key={status}
                    size="small"
                    color={status.includes('exhausted') ? 'warning' : 'default'}
                    label={`${status}: ${count}`}
                  />
                ))}
              </Stack>
            </>
          )}
        </Stack>
      </CardContent>
    </Card>
  );
}
