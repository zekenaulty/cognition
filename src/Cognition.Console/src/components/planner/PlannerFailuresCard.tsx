import React from 'react';
import { Card, CardContent, CardHeader, Chip, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material';
import { PlannerHealthReport } from '../../types/diagnostics';
import { formatRelativeTime } from './timeFormatters';
import { formatIdFragment } from './idFormatters';

type Props = {
  plannerReport: PlannerHealthReport;
};

export function PlannerFailuresCard({ plannerReport }: Props) {
  if (plannerReport.telemetry.recentFailures.length === 0) {
    return null;
  }

  return (
    <Card sx={{ mb: 3 }}>
      <CardHeader title="Recent Planner Failures" subheader="Last 24h" />
      <CardContent>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Planner</TableCell>
              <TableCell>Outcome</TableCell>
              <TableCell>Snippet</TableCell>
              <TableCell>Conversation</TableCell>
              <TableCell>When</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {plannerReport.telemetry.recentFailures.map(failure => (
              <TableRow key={failure.executionId}>
                <TableCell>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    {failure.plannerName}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Chip size="small" color="error" label={failure.outcome} />
                </TableCell>
                <TableCell>
                  <Typography variant="caption" color="text.secondary">
                    {failure.transcriptSnippet || 'No snippet'}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Typography variant="caption" color="text.secondary">
                    {failure.conversationId ? `Conv ${formatIdFragment(failure.conversationId)}` : '-'}
                    {failure.conversationMessageId ? ` · Msg ${formatIdFragment(failure.conversationMessageId)}` : ''}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Typography variant="body2">{formatRelativeTime(failure.createdAtUtc)}</Typography>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  );
}
