import React from 'react';
import { Box, Card, CardContent, CardHeader, Chip, Stack, Typography } from '@mui/material';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import { PlannerHealthReport } from '../../types/diagnostics';

type Props = {
  plannerReport: PlannerHealthReport;
};

export function PlannerTemplatesCard({ plannerReport }: Props) {
  return (
    <Card>
      <CardHeader title="Planner Templates" />
      <CardContent>
        <Stack spacing={1}>
          {plannerReport.planners.map(planner => (
            <Box key={planner.name} sx={{ border: '1px solid rgba(255,255,255,0.08)', borderRadius: 1, p: 1.5 }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                {planner.name}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {planner.description}
              </Typography>
              <Stack spacing={0.5} sx={{ mt: 1 }}>
                {planner.steps.map(step => (
                  <Stack key={step.stepId} direction="row" spacing={1} alignItems="center">
                    <Chip
                      size="small"
                      color={step.templateFound && step.templateActive ? 'success' : 'warning'}
                      icon={step.templateFound && step.templateActive ? <CheckCircleIcon fontSize="inherit" /> : <WarningAmberIcon fontSize="inherit" />}
                      label={step.displayName}
                    />
                    <Typography variant="caption" color="text.secondary">
                      {step.templateId ?? 'No template id'}
                      {!step.templateFound && ' · missing'}
                      {step.issue && ` · ${step.issue}`}
                    </Typography>
                  </Stack>
                ))}
              </Stack>
            </Box>
          ))}
        </Stack>
      </CardContent>
    </Card>
  );
}
