import React from 'react';
import { Card, CardContent, CardHeader, Stack, Typography } from '@mui/material';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import { OpenSearchDiagnosticsReport } from '../../types/diagnostics';
import { formatTimestamp } from './timeFormatters';

type Props = {
  report: OpenSearchDiagnosticsReport;
};

export function OpenSearchCard({ report }: Props) {
  return (
    <Card>
      <CardHeader title="OpenSearch Diagnostics" subheader={formatTimestamp(report.checkedAtUtc)} />
      <CardContent>
        <Stack spacing={1}>
          <StatusLine
            label="Cluster"
            healthy={report.clusterAvailable}
            detail={report.clusterStatus ?? 'unknown'}
          />
          <StatusLine label="Index" healthy={report.indexExists} detail={report.defaultIndex} />
          <StatusLine label="Pipeline" healthy={report.pipelineExists} detail={report.pipelineId ?? 'n/a'} />
          <StatusLine
            label="Model"
            healthy={Boolean(report.modelState && report.modelState !== 'failed')}
            detail={report.modelState ?? 'n/a'}
          />
        </Stack>
        {report.notes.length > 0 && (
          <Stack spacing={0.5} sx={{ mt: 2 }}>
            <Typography variant="subtitle2" color="text.secondary">
              Notes
            </Typography>
            {report.notes.map((note, idx) => (
              <Typography key={`${note}-${idx}`} variant="body2" color="text.secondary">
                {note}
              </Typography>
            ))}
          </Stack>
        )}
      </CardContent>
    </Card>
  );
}

function StatusLine({ label, healthy, detail }: { label: string; healthy: boolean; detail?: string | null }) {
  return (
    <Stack direction="row" spacing={1} alignItems="center">
      {healthy ? <CheckCircleIcon color="success" fontSize="small" /> : <WarningAmberIcon color="warning" fontSize="small" />}
      <Typography variant="body2" sx={{ fontWeight: 600 }}>
        {label}
      </Typography>
      <Typography variant="body2" color="text.secondary">
        {detail}
      </Typography>
    </Stack>
  );
}
