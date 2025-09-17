import React from 'react';
import { Box, Typography } from '@mui/material';

export type ToolTraceProps = {
  actions: string[];
};

export function ToolTrace({ actions }: ToolTraceProps) {
  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="subtitle2">Tool Trace</Typography>
      <ul>
        {actions.map((action, idx) => (
          <li key={idx}>{action}</li>
        ))}
      </ul>
    </Box>
  );
}
