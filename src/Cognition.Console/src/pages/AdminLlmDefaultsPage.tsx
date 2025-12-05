import React from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  Divider,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Typography,
  Alert,
} from '@mui/material';
import { useAuth } from '../auth/AuthContext';
import { useSecurity } from '../hooks/useSecurity';
import { useLlmDefaults } from '../hooks/useLlmDefaults';

export default function AdminLlmDefaultsPage() {
  const { auth } = useAuth();
  const security = useSecurity();
  const token = auth?.accessToken || '';
  const {
    providers,
    providerId,
    setProviderId,
    models,
    modelId,
    setModelId,
    loading,
    saving,
    error,
    success,
    save,
    reload,
    selectedProvider,
    selectedModel,
  } = useLlmDefaults(token);

  if (!security.isAdmin) {
    return (
      <Box>
        <Typography variant="h5" gutterBottom>Admin Access Required</Typography>
        <Typography color="text.secondary">You must be an administrator to manage LLM defaults.</Typography>
      </Box>
    );
  }

  return (
    <Card>
      <CardHeader title="LLM Default Settings" subheader="Select the default provider and model used for new conversations." />
      <Divider />
      <CardContent>
        <Stack spacing={2}>
          {error ? <Alert severity="error">{error}</Alert> : null}
          {success ? <Alert severity="success">{success}</Alert> : null}
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <FormControl fullWidth size="small">
              <InputLabel id="llm-provider-label">Provider</InputLabel>
              <Select
                labelId="llm-provider-label"
                label="Provider"
                value={providerId || ''}
                onChange={e => setProviderId(String(e.target.value))}
                disabled={loading || saving || providers.length === 0}
              >
                {providers.map(p => (
                  <MenuItem key={p.id} value={p.id}>{p.displayName || p.name || p.id}</MenuItem>
                ))}
              </Select>
            </FormControl>
            <FormControl fullWidth size="small">
              <InputLabel id="llm-model-label">Model</InputLabel>
              <Select
                labelId="llm-model-label"
                label="Model"
                value={modelId || ''}
                onChange={e => setModelId(String(e.target.value))}
                disabled={loading || saving || models.length === 0}
              >
                {models.map(m => (
                  <MenuItem key={m.id} value={m.id}>{m.displayName || m.name || m.id}</MenuItem>
                ))}
              </Select>
            </FormControl>
          </Stack>
          <Stack direction="row" spacing={1}>
            <Button variant="contained" onClick={save} disabled={saving || loading || !modelId}>
              {saving ? 'Saving...' : 'Save Defaults'}
            </Button>
            <Button variant="outlined" onClick={reload} disabled={saving || loading}>
              Reload
            </Button>
          </Stack>
          <Box>
            <Typography variant="body2" color="text.secondary">
              Active default: {selectedProvider ? (selectedProvider.displayName || selectedProvider.name) : '—'} / {selectedModel ? (selectedModel.displayName || selectedModel.name) : '—'}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Changes apply to new conversations when no conversation-specific setting exists. Validation ensures the model belongs to the selected provider.
            </Typography>
          </Box>
        </Stack>
      </CardContent>
    </Card>
  );
}
