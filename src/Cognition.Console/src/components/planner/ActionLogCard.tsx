import React from 'react';
import { Box, Card, CardContent, CardHeader, Chip, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material';
import { PlannerHealthReport } from '../../types/diagnostics';
import { buildActionContextLine, buildActionMetadataLine } from '../fiction/backlogUtils';
import { formatRelativeTime, formatTimestamp } from './timeFormatters';

type Props = {
  plannerReport: PlannerHealthReport;
};

export function ActionLogCard({ plannerReport }: Props) {
  return (
    <Card>
      <CardHeader
        title="Console Action Log"
        subheader="Recent backlog resumes and API actions"
      />
      <CardContent sx={{ px: 0 }}>
        {plannerReport.backlog.actionLogs.length === 0 ? (
          <Box sx={{ px: 2, pb: 2 }}>
            <Typography variant="body2" color="text.secondary">
              No API or console backlog actions recorded.
            </Typography>
          </Box>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Plan</TableCell>
                <TableCell>Action</TableCell>
                <TableCell>Actor</TableCell>
                <TableCell>Timestamp</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {plannerReport.backlog.actionLogs.map(log => {
                const metadataLine = buildActionMetadataLine(log as any);
                const contextLine = buildActionContextLine(log as any);
                return (
                  <TableRow key={`${log.planId}-${log.backlogId}-${log.timestampUtc}`}>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>
                        {log.planName}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {log.backlogId} on branch {log.branch}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Stack direction="row" spacing={0.5} alignItems="center" flexWrap="wrap">
                        <Chip size="small" label={log.action} />
                        {log.status && <Chip size="small" variant="outlined" label={log.status} />}
                      </Stack>
                      <Typography variant="caption" color="text.secondary">
                        {log.description || 'No description'}
                      </Typography>
                      {metadataLine && (
                        <Typography variant="caption" color="text.secondary" display="block">
                          {metadataLine}
                        </Typography>
                      )}
                      {contextLine && (
                        <Typography variant="caption" color="text.secondary" display="block">
                          {contextLine}
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{log.actor ?? 'Unknown'}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        Source: {log.source}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{formatRelativeTime(log.timestampUtc)}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        {formatTimestamp(log.timestampUtc)}
                      </Typography>
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
