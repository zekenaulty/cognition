import { useEffect, useState } from 'react';
import { fetchProviders, fetchModels } from '../api/client';

type Provider = { id: string; name: string; displayName?: string };
type Model = { id: string; name: string; displayName?: string };

const GEMINI_PREFERRED = (name?: string, displayName?: string) => {
  const n = (name || displayName || '').toLowerCase();
  return n.includes('gemini') || n.includes('google');
};

const FLASH_PREFERRED = (name?: string, displayName?: string) => {
  const n = (name || displayName || '').toLowerCase();
  return n.includes('flash') || n.includes('2.5') || n.includes('2.0');
};

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
      if (normProviders.length === 0) return;

      const preferred = normProviders.find(p => GEMINI_PREFERRED(p.name, p.displayName));

      const hasCurrent = providerId && normProviders.some(p => p.id === providerId);
      const isCurrentOpenAI = providerId && normProviders.some(p => p.id === providerId && (p.name || '').toLowerCase().includes('openai'));

      let next = providerId;
      if (!hasCurrent || (isCurrentOpenAI && preferred)) {
        next = (preferred ?? normProviders[0]).id;
        setProviderId(next);
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
      if (normModels.length === 0) return;

      const preferred = normModels.find(m => FLASH_PREFERRED(m.name, m.displayName) || GEMINI_PREFERRED(m.name, m.displayName));
      const hasCurrent = modelId && normModels.some(m => m.id === modelId);
      const isPreferredCurrent = modelId && preferred && preferred.id === modelId;

      if (!hasCurrent || !isPreferredCurrent) {
        setModelId((preferred ?? normModels[0]).id);
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
