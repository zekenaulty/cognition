import React from 'react';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  MenuItem,
  Stack,
  TextField
} from '@mui/material';
import { fetchModels, fetchProviders } from '../../api/client';
import { FictionBacklogItem, ResumeBacklogPayload } from '../../types/fiction';

type ProviderOption = { id: string; name: string; displayName?: string | null };
type ModelOption = { id: string; name: string; displayName?: string | null };

type Props = {
  open: boolean;
  item: FictionBacklogItem | null;
  defaultBranch?: string;
  defaultProviderId?: string | null;
  defaultModelId?: string | null;
  submitting?: boolean;
  error?: string | null;
  accessToken?: string | null;
  onClose: () => void;
  onSubmit: (payload: ResumeBacklogPayload) => void | Promise<void>;
};

export function FictionResumeBacklogDialog({
  open,
  item,
  defaultBranch,
  defaultProviderId,
  defaultModelId,
  submitting = false,
  error,
  onClose,
  onSubmit,
  accessToken
}: Props) {
  const [providerId, setProviderId] = React.useState('');
  const [modelId, setModelId] = React.useState('');
  const [branchSlug, setBranchSlug] = React.useState('');
  const [providers, setProviders] = React.useState<ProviderOption[]>([]);
  const [models, setModels] = React.useState<ModelOption[]>([]);
  const [providersLoading, setProvidersLoading] = React.useState(false);
  const [modelsLoading, setModelsLoading] = React.useState(false);
  const [providerLoadError, setProviderLoadError] = React.useState<string | null>(null);
  const [modelLoadError, setModelLoadError] = React.useState<string | null>(null);

  React.useEffect(() => {
    setProviderId(item?.providerId ?? '');
    setModelId(item?.modelId ?? '');
    setBranchSlug(item?.branchSlug ?? defaultBranch ?? 'main');
  }, [item, defaultBranch]);

  React.useEffect(() => {
    if (!open || !accessToken) {
      setProviders([]);
      return;
    }

    let cancelled = false;
    setProvidersLoading(true);
    setProviderLoadError(null);

    fetchProviders(accessToken)
      .then(list => {
        if (cancelled) return;
        const normalized = (list as any[])
          .map(p => ({
            id: p.id ?? p.Id ?? '',
            name: p.name ?? p.Name ?? 'provider',
            displayName: p.displayName ?? p.DisplayName ?? null
          }))
          .filter(p => p.id);
        setProviders(normalized);
        setProviderId(current => {
          if (current) {
            return current;
          }
          if (item?.providerId && normalized.some(p => p.id === item.providerId)) {
            return item.providerId;
          }
          if (defaultProviderId && normalized.some(p => p.id === defaultProviderId)) {
            return defaultProviderId;
          }
          return normalized[0]?.id ?? '';
        });
      })
      .catch(err => {
        if (cancelled) return;
        const message = err instanceof Error ? err.message : 'Unable to load providers.';
        setProviderLoadError(message);
        setProviders([]);
      })
      .finally(() => {
        if (!cancelled) {
          setProvidersLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [open, accessToken, item?.providerId, defaultProviderId]);

  React.useEffect(() => {
    if (!open || !accessToken || !providerId) {
      setModels([]);
      return;
    }

    let cancelled = false;
    setModelsLoading(true);
    setModelLoadError(null);

    fetchModels(accessToken, providerId)
      .then(list => {
        if (cancelled) return;
        const normalized = (list as any[])
          .map(m => ({
            id: m.id ?? m.Id ?? '',
            name: m.name ?? m.Name ?? 'model',
            displayName: m.displayName ?? m.DisplayName ?? null
          }))
          .filter(m => m.id);
        setModels(normalized);
        setModelId(current => {
          if (current) {
            return current;
          }
          if (item?.modelId && normalized.some(m => m.id === item.modelId)) {
            return item.modelId;
          }
          if (defaultModelId && normalized.some(m => m.id === defaultModelId)) {
            return defaultModelId;
          }
          return normalized[0]?.id ?? '';
        });
      })
      .catch(err => {
        if (cancelled) return;
        const message = err instanceof Error ? err.message : 'Unable to load models.';
        setModelLoadError(message);
        setModels([]);
      })
      .finally(() => {
        if (!cancelled) {
          setModelsLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [open, accessToken, providerId, item?.modelId, defaultModelId]);

  const canSubmit =
    Boolean(item) &&
    Boolean(item?.conversationId) &&
    Boolean(item?.conversationPlanId) &&
    Boolean(item?.taskId) &&
    Boolean(item?.agentId) &&
    Boolean(providerId);

  const handleSubmit = () => {
    if (!item || !canSubmit) {
      return;
    }
    const payload: ResumeBacklogPayload = {
      conversationId: item.conversationId!,
      conversationPlanId: item.conversationPlanId!,
      agentId: item.agentId!,
      providerId,
      modelId: modelId || null,
      taskId: item.taskId!,
      branchSlug: branchSlug || undefined
    };
    onSubmit(payload);
  };

  return (
    <Dialog open={open} onClose={submitting ? undefined : onClose} fullWidth maxWidth="sm">
      <DialogTitle>Resume {item?.backlogId ?? 'backlog task'}</DialogTitle>
      <DialogContent sx={{ pt: 1 }}>
        <DialogContentText sx={{ mb: 2 }}>
          Confirm the metadata to feed back into the backlog scheduler. Update the provider/model or branch slug if this resume should run with different resources.
        </DialogContentText>
        {(!item?.agentId || !item?.conversationId || !item?.taskId) && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            This backlog item is missing required metadata. Please verify the conversation plan has tasks with backlog metadata before resuming.
          </Alert>
        )}
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        {providerLoadError && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            {providerLoadError}
          </Alert>
        )}
        {modelLoadError && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            {modelLoadError}
          </Alert>
        )}
        <Stack spacing={2}>
          <TextField
            label="Conversation ID"
            value={item?.conversationId ?? ''}
            InputProps={{ readOnly: true }}
            size="small"
            fullWidth
          />
          <TextField
            label="Conversation Plan ID"
            value={item?.conversationPlanId ?? ''}
            InputProps={{ readOnly: true }}
            size="small"
            fullWidth
          />
          <TextField
            label="Task ID"
            value={item?.taskId ?? ''}
            InputProps={{ readOnly: true }}
            size="small"
            fullWidth
          />
          <TextField
            label="Agent ID"
            value={item?.agentId ?? ''}
            InputProps={{ readOnly: true }}
            size="small"
            fullWidth
          />
          <TextField
            label="Provider"
            select
            value={providerId}
            onChange={event => {
              setProviderId(event.target.value);
              setModelId('');
            }}
            required
            size="small"
            fullWidth
            helperText={providersLoading ? 'Loading providers…' : undefined}
            SelectProps={{ displayEmpty: true }}
            disabled={providers.length === 0 && providersLoading}
          >
            {providers.length === 0 && !providersLoading && (
              <MenuItem value="">
                <em>No providers available</em>
              </MenuItem>
            )}
            {providers.map(provider => (
              <MenuItem key={provider.id} value={provider.id}>
                {provider.displayName || provider.name}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            label="Model (optional)"
            select
            value={modelId}
            onChange={event => setModelId(event.target.value)}
            size="small"
            fullWidth
            helperText={modelsLoading ? 'Loading models…' : undefined}
            SelectProps={{ displayEmpty: true }}
            disabled={!providerId || (models.length === 0 && modelsLoading)}
          >
            <MenuItem value="">
              <em>Use provider default</em>
            </MenuItem>
            {models.map(model => (
              <MenuItem key={model.id} value={model.id}>
                {model.displayName || model.name}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            label="Branch Slug"
            value={branchSlug}
            onChange={event => setBranchSlug(event.target.value)}
            size="small"
            fullWidth
          />
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} disabled={submitting}>
          Cancel
        </Button>
        <Button variant="contained" onClick={handleSubmit} disabled={!canSubmit || submitting}>
          {submitting ? 'Resuming…' : 'Resume'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
