import { useCallback, useEffect, useMemo, useState } from 'react';
import { fetchLlmDefaults, fetchModels, fetchProviders, updateLlmDefaults } from '../api/client';

type Provider = { id: string; name: string; displayName?: string };
type Model = { id: string; name: string; displayName?: string };

export function useLlmDefaults(accessToken: string) {
  const [providers, setProviders] = useState<Provider[]>([]);
  const [providerId, setProviderId] = useState<string>('');
  const [models, setModels] = useState<Model[]>([]);
  const [modelId, setModelId] = useState<string>('');
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const loadModels = useCallback(async (pid: string, desiredModelId?: string) => {
    if (!accessToken || !pid) {
      setModels([]);
      setModelId('');
      return;
    }
    const mList = await fetchModels(accessToken, pid);
    const norm: Model[] = (mList as any[]).map((m: any) => ({ id: m.id ?? m.Id, name: m.name ?? m.Name, displayName: m.displayName ?? m.DisplayName }));
    setModels(norm);
    if (norm.length === 0) {
      setModelId('');
      return;
    }
    if (desiredModelId && norm.some(m => m.id === desiredModelId)) {
      setModelId(desiredModelId);
      return;
    }
    setModelId(norm[0].id);
  }, [accessToken]);

  const load = useCallback(async () => {
    if (!accessToken) return;
    setLoading(true);
    setError(null);
    setSuccess(null);
    try {
      const [provResp, defaults] = await Promise.all([fetchProviders(accessToken), fetchLlmDefaults(accessToken)]);
      const normProviders: Provider[] = (provResp as any[]).map((p: any) => ({ id: p.id ?? p.Id, name: p.name ?? p.Name, displayName: p.displayName ?? p.DisplayName }));
      setProviders(normProviders);
      const defaultProviderId = defaults?.providerId ?? undefined;
      const resolvedProviderId = (defaultProviderId && normProviders.some(p => p.id === defaultProviderId))
        ? defaultProviderId
        : (normProviders[0]?.id ?? '');
      if (resolvedProviderId) {
        setProviderId(resolvedProviderId);
        await loadModels(resolvedProviderId, defaults?.modelId ?? undefined);
      } else {
        setProviderId('');
        setModels([]);
        setModelId('');
      }
    } catch (e: any) {
      setError(e?.message || 'Failed to load defaults');
    } finally {
      setLoading(false);
    }
  }, [accessToken, loadModels]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    if (!providerId) return;
    loadModels(providerId);
  }, [providerId, loadModels]);

  const save = useCallback(async () => {
    if (!accessToken || !modelId) return;
    setSaving(true);
    setError(null);
    setSuccess(null);
    try {
      await updateLlmDefaults(accessToken, { modelId, isActive: true, priority: 0 });
      setSuccess('Defaults updated');
    } catch (e: any) {
      setError(e?.message || 'Failed to update defaults');
    } finally {
      setSaving(false);
    }
  }, [accessToken, modelId]);

  const selectedProvider = useMemo(() => providers.find(p => p.id === providerId), [providers, providerId]);
  const selectedModel = useMemo(() => models.find(m => m.id === modelId), [models, modelId]);

  return {
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
    reload: load,
    selectedProvider,
    selectedModel,
  };
}
