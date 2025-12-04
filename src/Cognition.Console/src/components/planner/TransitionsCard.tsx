import React from 'react';
import { Card, CardContent, CardHeader, Chip, IconButton, Table, TableBody, TableCell, TableHead, TableRow, Tooltip, Typography } from '@mui/material';
import TimelineIcon from '@mui/icons-material/Timeline';
import { PlannerHealthReport } from '../../types/diagnostics';
import { formatRelativeTime, formatDuration } from './timeFormatters';
import { describeBacklogStatus } from './backlogFormatters';

type Props = {
  plannerReport: PlannerHealthReport;
};

export function TransitionsCard({ plannerReport }: Props) {
  return (
    <Card>
      <CardHeader
        title="Recent Transitions"
        action={
          <Tooltip title="Transitions from the past 24h">
            <IconButton size="small">
              <TimelineIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        }
      />
      <CardContent sx={{ px: 0 }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Plan</TableCell>
              <TableCell>Backlog Item</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Occurred</TableCell>
              <TableCell>Age</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {plannerReport.backlog.recentTransitions.map(transition => (
              <TableRow key={`${transition.planId}-${transition.backlogId}-${transition.occurredAtUtc}`} hover>
                <TableCell>{transition.planName}</TableCell>
                <TableCell>
                  <Typography variant="body2" sx={{ fontWeight: 500 }}>
                    {transition.backlogId}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {transition.description}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Chip size="small" label={describeBacklogStatus(transition.status)} />
                </TableCell>
                <TableCell>{formatRelativeTime(transition.occurredAtUtc)}</TableCell>
                <TableCell>{formatDuration(transition.age)}</TableCell>
              </TableRow>
            ))}
            {plannerReport.backlog.recentTransitions.length === 0 && (
              <TableRow>
                <TableCell colSpan={5} align="center">
                  <Typography variant="body2" color="text.secondary">
                    No backlog movements captured in the last window.
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  );
}
