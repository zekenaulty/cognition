import React from 'react';
import { Box, Typography } from '@mui/material';

export type PlanTimelineProps = {
  steps: string[];
};

export function PlanTimeline({ steps }: PlanTimelineProps) {
  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="subtitle2">Plan Timeline</Typography>
      <ul>
        {steps.map((step, idx) => (
          <li key={idx}>{step}</li>
        ))}
      </ul>
    </Box>
  );
}
