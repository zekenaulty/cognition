import { useEffect, useState } from 'react';
import { fetchProviders, fetchModels } from '../api/client';

type Provider = { id: string; name: string; displayName?: string };
type Model = { id: string; name: string; displayName?: string };

export function useProvidersModels(accessToken: string, initialProviderId: string, initialModelId: string) {
  const [providers, setProviders] = useState<Provider[]>([]);
  const [providerId, setProviderId] = useState<string>(initialProviderId);
  const [models, setModels] = useState<Model[]>([]);
  const [modelId, setModelId] = useState<string>(initialModelId);

  useEffect(() => {
    const loadProviders = async () => {
      if (!accessToken) return;
      const pList = await fetchProviders(accessToken);
      const normProviders: Provider[] = (pList as any[]).map((p: any) => ({ id: p.id ?? p.Id, name: p.name ?? p.Name, displayName: p.displayName ?? p.DisplayName }));
      setProviders(normProviders);
      let chosenProviderId = providerId;
      if (!chosenProviderId && normProviders.length > 0) {
        const openai = normProviders.find(p => (p.name || '').toLowerCase() === 'openai');
        chosenProviderId = (openai ?? normProviders[0]).id;
        setProviderId(chosenProviderId);
      }
    };
    loadProviders();
  }, [accessToken]);

  useEffect(() => {
    const loadModels = async () => {
      if (!accessToken || !providerId) return;
      const mList = await fetchModels(accessToken, providerId);
      const normModels: Model[] = (mList as any[]).map((m: any) => ({ id: m.id ?? m.Id, name: m.name ?? m.Name, displayName: m.displayName ?? m.DisplayName }));
      setModels(normModels);
      if (!modelId && normModels.length > 0) {
        setModelId(normModels[0].id);
      }
    };
    loadModels();
  }, [accessToken, providerId]);

  return {
    providers,
    providerId,
    setProviderId,
    models,
    modelId,
    setModelId,
  };
}
