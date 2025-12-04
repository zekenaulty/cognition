import React from 'react';
import { Box, Card, CardContent, CardHeader, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material';
import { PlannerHealthReport } from '../../types/diagnostics';
import { formatTelemetryContext, formatTelemetryMetadata } from './telemetryFormatters';
import { formatRelativeTime } from './timeFormatters';

type Props = {
  plannerReport: PlannerHealthReport;
};

export function TelemetryFeedCard({ plannerReport }: Props) {
  return (
    <Card>
      <CardHeader
        title="Backlog Telemetry Feed"
        subheader="Latest workflow events emitted from backlog transitions"
      />
      <CardContent sx={{ px: 0 }}>
        {plannerReport.backlog.telemetryEvents.length === 0 ? (
          <Box sx={{ px: 2, pb: 2 }}>
            <Typography variant="body2" color="text.secondary">
              Telemetry events will appear once backlog runs emit workflow logs.
            </Typography>
          </Box>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Plan</TableCell>
                <TableCell>Phase / Status</TableCell>
                <TableCell>Characters</TableCell>
                <TableCell>Lore</TableCell>
                <TableCell>Updated</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {plannerReport.backlog.telemetryEvents.map(event => {
                const metadataLine = formatTelemetryMetadata(event.metadata);
                const contextLine = formatTelemetryContext(event.metadata);
                return (
                  <TableRow key={`${event.planId}-${event.backlogId}-${event.timestampUtc}`}>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>
                        {event.planName}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {event.backlogId} on branch {event.branch}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{event.phase}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        {event.status}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="caption" color="text.secondary">
                        {metadataLine}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="caption" color="text.secondary">
                        {contextLine}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{formatRelativeTime(event.timestampUtc)}</Typography>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        )}
      </CardContent>
    </Card>
  );
}
