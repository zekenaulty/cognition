import React from 'react';
import { Alert, Card, CardContent, CardHeader, LinearProgress, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material';
import { LoreBranchSummary } from '../../types/fiction';

type Props = {
  rosterPlanId: string | null;
  loreSummary: LoreBranchSummary[];
  loading: boolean;
  error: string | null;
};

export function LoreSummaryCard({ rosterPlanId, loreSummary, loading, error }: Props) {
  return (
    <Card>
      <CardHeader title="Lore Fulfillment by Branch" />
      <CardContent>
        {!rosterPlanId ? (
          <Typography variant="body2" color="text.secondary">
            Select a plan to inspect lore fulfillment progress.
          </Typography>
        ) : loading ? (
          <LinearProgress />
        ) : error ? (
          <Alert severity="warning">{error}</Alert>
        ) : loreSummary.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            No lore requirements have been recorded for this plan yet.
          </Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Branch</TableCell>
                <TableCell align="right">Ready</TableCell>
                <TableCell align="right">Blocked</TableCell>
                <TableCell align="right">Planned</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {loreSummary.map(summary => (
                <TableRow key={summary.branchSlug}>
                  <TableCell>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {summary.branchSlug}
                    </Typography>
                    {summary.branchLineage && summary.branchLineage.length > 1 && (
                      <Typography variant="caption" color="text.secondary">
                        {summary.branchLineage.join(' / ')}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell align="right">{summary.ready}</TableCell>
                  <TableCell align="right">{summary.blocked}</TableCell>
                  <TableCell align="right">{summary.planned}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </CardContent>
    </Card>
  );
}
