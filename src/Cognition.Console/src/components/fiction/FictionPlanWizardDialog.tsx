import React from 'react';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  FormControl,
  FormControlLabel,
  InputLabel,
  LinearProgress,
  MenuItem,
  Select,
  Stack,
  Switch,
  TextField,
  Typography
} from '@mui/material';
import { ApiError, api, fictionApi, fetchModels, fetchProviders } from '../../api/client';
import {
  AgentSummary,
  CreateFictionPlanPayload,
  FictionPlanSummary,
  FictionProjectSummary
} from '../../types/fiction';

type PersonaOption = {
  id: string;
  name: string;
  type?: number | string;
};

type Props = {
  open: boolean;
  accessToken?: string | null;
  onClose: () => void;
  onCreated: (plan: FictionPlanSummary) => void;
  onPrefillResume?: (defaults: { providerId?: string; modelId?: string | null; branchSlug?: string }) => void;
};

const STALE_PLAN_NAME_SUFFIX = 'Plan';

export function FictionPlanWizardDialog({ open, accessToken, onClose, onCreated, onPrefillResume }: Props) {
  const [projects, setProjects] = React.useState<FictionProjectSummary[]>([]);
  const [projectsLoading, setProjectsLoading] = React.useState(false);
  const [projectsError, setProjectsError] = React.useState<string | null>(null);

  const [personas, setPersonas] = React.useState<PersonaOption[]>([]);
  const [personasLoading, setPersonasLoading] = React.useState(false);
  const [personasError, setPersonasError] = React.useState<string | null>(null);

  const [agents, setAgents] = React.useState<AgentSummary[]>([]);
  const [agentsLoading, setAgentsLoading] = React.useState(false);
  const [providers, setProviders] = React.useState<ProviderOption[]>([]);
  const [providersLoading, setProvidersLoading] = React.useState(false);
  const [providerLoadError, setProviderLoadError] = React.useState<string | null>(null);
  const [models, setModels] = React.useState<ModelOption[]>([]);
  const [modelsLoading, setModelsLoading] = React.useState(false);
  const [modelLoadError, setModelLoadError] = React.useState<string | null>(null);

  const [createProject, setCreateProject] = React.useState(false);
  const [projectId, setProjectId] = React.useState('');
  const [projectTitle, setProjectTitle] = React.useState('');
  const [projectLogline, setProjectLogline] = React.useState('');

  const [planName, setPlanName] = React.useState('');
  const [planDescription, setPlanDescription] = React.useState('');
  const [branchSlug, setBranchSlug] = React.useState('main');
  const [personaId, setPersonaId] = React.useState('');
  const [agentId, setAgentId] = React.useState('');
  const [providerId, setProviderId] = React.useState('');
  const [modelId, setModelId] = React.useState('');

  const [submitting, setSubmitting] = React.useState(false);
  const [submitError, setSubmitError] = React.useState<string | null>(null);

  const resetState = React.useCallback(() => {
    setCreateProject(false);
    setProjectId('');
    setProjectTitle('');
    setProjectLogline('');
    setPlanName('');
    setPlanDescription('');
    setBranchSlug('main');
    setPersonaId('');
    setAgentId('');
    setSubmitError(null);
    setProviders([]);
    setModels([]);
    setProviderId('');
    setModelId('');
    setProviderLoadError(null);
    setModelLoadError(null);
  }, []);

  const loadProjects = React.useCallback(async () => {
    if (!accessToken) {
      setProjects([]);
      return;
    }
    setProjectsLoading(true);
    setProjectsError(null);
    try {
      const list = await fictionApi.listProjects(accessToken);
      setProjects(list);
      if (list.length > 0) {
        setProjectId(prev => (prev ? prev : list[0].id));
      }
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Unable to load projects.';
      setProjectsError(message);
      setProjects([]);
    } finally {
      setProjectsLoading(false);
    }
  }, [accessToken]);

  const loadPersonas = React.useCallback(async () => {
    if (!accessToken) {
      setPersonas([]);
      return;
    }
    setPersonasLoading(true);
    setPersonasError(null);
    try {
      const list = await api.listPersonas(accessToken);
      const options = list
        .filter(p => Boolean(p.id))
        .map(p => ({
          id: p.id,
          name: p.name,
          type: p.type
        }));
      setPersonas(options);
      if (options.length > 0) {
        setPersonaId(prev => (prev ? prev : options[0].id));
      }
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Unable to load personas.';
      setPersonasError(message);
      setPersonas([]);
    } finally {
      setPersonasLoading(false);
    }
  }, [accessToken]);

  const loadAgents = React.useCallback(async () => {
    if (!accessToken) {
      setAgents([]);
      return;
    }
    setAgentsLoading(true);
    try {
      const list = await api.listAgents(accessToken);
      setAgents(list ?? []);
    } catch {
      setAgents([]);
    } finally {
      setAgentsLoading(false);
    }
  }, [accessToken]);

  const loadProviders = React.useCallback(() => {
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
          if (current && normalized.some(p => p.id === current)) {
            return current;
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
  }, [open, accessToken]);

  const loadModels = React.useCallback(
    (provider: string) => {
      if (!open || !accessToken || !provider) {
        setModels([]);
        return;
      }

      let cancelled = false;
      setModelsLoading(true);
      setModelLoadError(null);

      fetchModels(accessToken, provider)
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
            if (current && normalized.some(m => m.id === current)) {
              return current;
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
    },
    [open, accessToken]
  );

React.useEffect(() => {
  if (!open) {
    resetState();
    return;
  }
  loadProjects();
  loadPersonas();
  loadAgents();
  loadProviders();
}, [open, loadProjects, loadPersonas, loadAgents, loadProviders, resetState]);

React.useEffect(() => {
  if (open && !createProject && projects.length === 0 && !projectsLoading) {
    setCreateProject(true);
  }
}, [open, createProject, projects.length, projectsLoading]);

React.useEffect(() => {
  if (!open) {
    return;
  }
  // Auto-populate plan name when selecting a project if user hasn't typed anything.
  if (!planName.trim()) {
    if (createProject && projectTitle.trim()) {
      setPlanName(`${projectTitle.trim()} ${STALE_PLAN_NAME_SUFFIX}`.trim());
    } else if (!createProject && projectId) {
      const project = projects.find(p => p.id === projectId);
      if (project?.title) {
        setPlanName(`${project.title} ${STALE_PLAN_NAME_SUFFIX}`);
      }
    }
  }
}, [createProject, projectId, projectTitle, projects, planName, open]);

React.useEffect(() => {
  if (!open || !providerId) {
    setModels([]);
    return;
  }
  const dispose = loadModels(providerId);
  return () => {
    if (dispose) dispose();
  };
}, [open, providerId, loadModels]);

  const personaAgents = React.useMemo(
    () => agents.filter(agent => agent.personaId === personaId),
    [agents, personaId]
  );

const projectValid = createProject ? Boolean(projectTitle.trim()) : Boolean(projectId);
const canSubmit =
  !submitting &&
  Boolean(planName.trim()) &&
  Boolean(personaId) &&
  Boolean(providerId) &&
  projectValid;

  const handleSubmit = async () => {
    if (!accessToken || !canSubmit) {
      return;
    }
    setSubmitting(true);
    setSubmitError(null);
    const payload: CreateFictionPlanPayload = {
      name: planName.trim(),
      description: planDescription.trim() ? planDescription.trim() : undefined,
      branchSlug: branchSlug.trim() || undefined,
      personaId,
      agentId: agentId || undefined
    };
    if (createProject) {
      payload.projectTitle = projectTitle.trim();
      payload.projectLogline = projectLogline.trim() ? projectLogline.trim() : undefined;
    } else if (projectId) {
      payload.projectId = projectId;
    }

    try {
      const plan = await fictionApi.createPlan(payload, accessToken);
      onPrefillResume?.({
        providerId: providerId || undefined,
        modelId: modelId || null,
        branchSlug: branchSlug.trim() || undefined
      });
      onCreated(plan);
      resetState();
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Unable to create plan.';
      setSubmitError(message);
      return;
    } finally {
      setSubmitting(false);
    }
    onClose();
  };

  const renderProjectField = () => {
    if (createProject) {
      return (
        <Stack spacing={2}>
          <TextField
            label="Project Title"
            value={projectTitle}
            onChange={event => setProjectTitle(event.target.value)}
            required
            fullWidth
          />
          <TextField
            label="Project Logline"
            value={projectLogline}
            onChange={event => setProjectLogline(event.target.value)}
            placeholder="High-level summary or logline"
            fullWidth
            multiline
            minRows={2}
          />
        </Stack>
      );
    }

    if (projectsLoading) {
      return <LinearProgress />;
    }

    if (projectsError) {
      return <Alert severity="warning">{projectsError}</Alert>;
    }

    if (projects.length === 0) {
      return (
        <Alert severity="info">
          No projects exist yet. Toggle “Create new project” to define one before creating a plan.
        </Alert>
      );
    }

    return (
      <FormControl fullWidth size="small">
        <InputLabel id="fiction-plan-project">Project</InputLabel>
        <Select
          labelId="fiction-plan-project"
          label="Project"
          value={projectId}
          onChange={event => setProjectId(event.target.value)}
        >
          {projects.map(project => (
            <MenuItem key={project.id} value={project.id}>
              {project.title}
            </MenuItem>
          ))}
        </Select>
      </FormControl>
    );
  };

  return (
    <Dialog open={open} onClose={submitting ? undefined : onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Create Fiction Plan</DialogTitle>
      <DialogContent sx={{ pt: 1 }}>
        <DialogContentText sx={{ mb: 2 }}>
          Pick or create a project, choose an author persona, and we’ll seed the backlog with the initial
          planning jobs. Provider/model/branch selections will prefill future resume dialogs for this user.
        </DialogContentText>
        <Stack spacing={2}>
          <FormControlLabel
            control={
              <Switch
                checked={createProject}
                onChange={event => setCreateProject(event.target.checked)}
                disabled={submitting}
              />
            }
            label="Create new project"
          />
          {renderProjectField()}
          <TextField
            label="Plan Name"
            value={planName}
            onChange={event => setPlanName(event.target.value)}
            required
            fullWidth
          />
          <TextField
            label="Plan Description"
            value={planDescription}
            onChange={event => setPlanDescription(event.target.value)}
            fullWidth
            multiline
            minRows={2}
          />
          <TextField
            label="Branch Slug"
            value={branchSlug}
            onChange={event => setBranchSlug(event.target.value)}
            helperText="Defaults to main; adjust if this plan should run on a feature branch."
            fullWidth
          />
          {providersLoading ? (
            <LinearProgress />
          ) : providerLoadError ? (
            <Alert severity="warning">{providerLoadError}</Alert>
          ) : (
            <FormControl fullWidth size="small">
              <InputLabel id="fiction-plan-provider">Provider</InputLabel>
              <Select
                labelId="fiction-plan-provider"
                label="Provider"
                value={providerId}
                onChange={event => setProviderId(event.target.value)}
                required
              >
                {providers.map(provider => (
                  <MenuItem key={provider.id} value={provider.id}>
                    {provider.displayName ?? provider.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          )}
          {modelsLoading ? (
            <LinearProgress />
          ) : modelLoadError ? (
            <Alert severity="warning">{modelLoadError}</Alert>
          ) : (
            <FormControl fullWidth size="small" disabled={!providerId || models.length === 0}>
              <InputLabel id="fiction-plan-model">Model (optional)</InputLabel>
              <Select
                labelId="fiction-plan-model"
                label="Model"
                value={modelId}
                onChange={event => setModelId(event.target.value)}
              >
                <MenuItem value="">
                  {providerId ? (models.length === 0 ? 'No models found' : 'Auto-select default model') : 'Select provider first'}
                </MenuItem>
                {models.map(model => (
                  <MenuItem key={model.id} value={model.id}>
                    {model.displayName ?? model.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          )}
          {personasLoading ? (
            <LinearProgress />
          ) : personasError ? (
            <Alert severity="warning">{personasError}</Alert>
          ) : (
            <FormControl fullWidth size="small">
              <InputLabel id="fiction-plan-persona">Author Persona</InputLabel>
              <Select
                labelId="fiction-plan-persona"
                label="Author Persona"
                value={personaId}
                onChange={event => {
                  setPersonaId(event.target.value);
                  setAgentId('');
                }}
                required
              >
                {personas.map(persona => (
                  <MenuItem key={persona.id} value={persona.id}>
                    {persona.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          )}
          <FormControl size="small" fullWidth disabled={!personaId || agentsLoading}>
            <InputLabel id="fiction-plan-agent">Agent (optional)</InputLabel>
            <Select
              labelId="fiction-plan-agent"
              label="Agent"
              value={agentId}
              onChange={event => setAgentId(event.target.value)}
            >
              <MenuItem value="">
                {personaId
                  ? personaAgents.length === 0
                    ? 'No linked agents'
                    : 'Auto-select linked agent'
                  : 'Select persona first'}
              </MenuItem>
              {personaAgents.map(agent => (
                <MenuItem key={agent.id} value={agent.id}>
                  Agent {agent.id.slice(0, 8).toUpperCase()}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          {submitError && (
            <Alert severity="error">
              {submitError}
            </Alert>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={submitting}>
          Cancel
        </Button>
        <Button variant="contained" onClick={handleSubmit} disabled={!canSubmit}>
          {submitting ? 'Creating…' : 'Create Plan'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
