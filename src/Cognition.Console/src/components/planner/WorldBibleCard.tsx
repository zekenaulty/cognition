import React from 'react';
import { Box, Card, CardContent, CardHeader, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material';
import { PlannerHealthReport } from '../../types/diagnostics';
import { formatRelativeTime, formatTimestamp } from './timeFormatters';

type Props = {
  plannerReport: PlannerHealthReport;
};

export function WorldBibleCard({ plannerReport }: Props) {
  const worldBibleReport = plannerReport.worldBible;

  return (
    <Card>
      <CardHeader
        title="World Bible Snapshots"
        subheader="Active entries recorded by WorldBibleManager"
      />
      <CardContent>
        {worldBibleReport.plans.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            No world bible entries have been captured yet. Run the WorldBibleManager phase to populate lore snapshots.
          </Typography>
        ) : (
          <Stack spacing={3}>
            {worldBibleReport.plans.map(plan => (
              <Box key={plan.worldBibleId}>
                <Stack
                  direction={{ xs: 'column', md: 'row' }}
                  spacing={1}
                  justifyContent="space-between"
                  alignItems={{ xs: 'flex-start', md: 'center' }}
                >
                  <Box>
                    <Typography variant="h6" sx={{ fontWeight: 600 }}>
                      {plan.planName}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Domain {plan.domain} · Branch {plan.branchSlug ?? 'main'}
                    </Typography>
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    Last updated{' '}
                    {plan.lastUpdatedUtc
                      ? `${formatRelativeTime(plan.lastUpdatedUtc)} (${formatTimestamp(plan.lastUpdatedUtc)})`
                      : '-'}
                  </Typography>
                </Stack>
                {plan.activeEntries.length === 0 ? (
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                    No active entries for this plan.
                  </Typography>
                ) : (
                  <Table size="small" sx={{ mt: 2 }}>
                    <TableHead>
                      <TableRow>
                        <TableCell>Entry</TableCell>
                        <TableCell>Type</TableCell>
                        <TableCell>Context</TableCell>
                        <TableCell>Updated</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {plan.activeEntries.map(entry => (
                        <TableRow key={entry.entryId}>
                          <TableCell>
                            <Typography variant="body2" sx={{ fontWeight: 600 }}>
                              {entry.entryId}
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                              {entry.title ?? 'Untitled'}
                            </Typography>
                          </TableCell>
                          <TableCell>{entry.entryType ?? 'n/a'}</TableCell>
                          <TableCell>
                            <Typography variant="caption" color="text.secondary">
                              {entry.context ?? 'n/a'}
                            </Typography>
                          </TableCell>
                          <TableCell>
                            <Typography variant="body2">
                              {entry.updatedAtUtc ? formatRelativeTime(entry.updatedAtUtc) : '-'}
                            </Typography>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                )}
              </Box>
            ))}
          </Stack>
        )}
      </CardContent>
    </Card>
  );
}
