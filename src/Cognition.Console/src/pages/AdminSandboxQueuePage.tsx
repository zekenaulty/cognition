import React from 'react';
import { Alert, Box, Button, Card, CardContent, Stack, Typography } from '@mui/material';
import { api } from '../api/client';

type SandboxWorkRequest = {
  toolId: string;
  classPath: string;
  args: Record<string, unknown>;
};

export const AdminSandboxQueuePage: React.FC = () => {
  const [items, setItems] = React.useState<SandboxWorkRequest[]>([]);
  const [error, setError] = React.useState<string | null>(null);
  const [loading, setLoading] = React.useState(false);

  const fetchQueue = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await api.get<SandboxWorkRequest[]>('/api/sandbox/queue');
      setItems(res.data ?? []);
    } catch (err: any) {
      setError(err?.message ?? 'Failed to load sandbox queue.');
    } finally {
      setLoading(false);
    }
  };

  const approveNext = async () => {
    setError(null);
    try {
      await api.post('/api/sandbox/approve');
      await fetchQueue();
    } catch (err: any) {
      setError(err?.message ?? 'Failed to approve sandbox request.');
    }
  };

  React.useEffect(() => {
    fetchQueue();
  }, []);

  return (
    <Box p={3}>
      <Stack direction="row" spacing={2} alignItems="center" mb={2}>
        <Typography variant="h5">Sandbox Approval Queue</Typography>
        <Button variant="contained" onClick={fetchQueue} disabled={loading}>
          Refresh
        </Button>
        <Button variant="outlined" onClick={approveNext} disabled={loading || items.length === 0}>
          Approve Next
        </Button>
      </Stack>
      {error && <Alert severity="error">{error}</Alert>}
      {items.length === 0 && !loading && <Alert severity="info">No pending sandbox requests.</Alert>}
      <Stack spacing={2}>
        {items.map((item, idx) => (
          <Card key={`${item.toolId}-${idx}`}>
            <CardContent>
              <Typography variant="subtitle1">Tool: {item.toolId}</Typography>
              <Typography variant="body2">ClassPath: {item.classPath}</Typography>
            </CardContent>
          </Card>
        ))}
      </Stack>
    </Box>
  );
};
