import { useEffect, useState } from 'react';

type Provider = { id: string; name: string; displayName?: string };
type Model = { id: string; name: string; displayName?: string };

export function useImageProvidersModels(accessToken: string) {
  const [providers, setProviders] = useState<Provider[]>([]);
  const [providerId, setProviderId] = useState<string>('');
  const [models, setModels] = useState<Model[]>([]);
  const [imageModel, setImageModel] = useState<string>('');

  const headers = accessToken ? { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` } : { 'Content-Type': 'application/json' };

  useEffect(() => {
    async function loadProviders() {
      // Prefer /api/images/providers; fallback to /api/llm/providers
      const r1 = await fetch('/api/images/providers', { headers });
      if (r1.ok) {
        const js = await r1.json();
        const mapped: Provider[] = (js as any[]).map(x => ({ id: x.id ?? x.Id ?? (x.name ?? x.Name), name: x.name ?? x.Name, displayName: x.displayName ?? x.DisplayName ?? (x.name ?? x.Name) }));
        setProviders(mapped);
        if (!providerId && mapped.length) setProviderId(mapped[0].id);
        return;
      }
      const r2 = await fetch('/api/llm/providers', { headers });
      if (r2.ok) {
        const js = await r2.json();
        const mapped: Provider[] = (js as any[]).map(x => ({ id: x.id ?? x.Id, name: x.name ?? x.Name, displayName: x.displayName ?? x.DisplayName }));
        setProviders(mapped);
        if (!providerId && mapped.length) setProviderId(mapped[0].id);
      }
    }
    loadProviders();
  }, [accessToken]);

  useEffect(() => {
    async function loadModels() {
      if (!providerId) { setModels([]); setImageModel(''); return; }
      // Try /api/images/models first
      const r1 = await fetch(`/api/images/models?providerId=${encodeURIComponent(providerId)}`, { headers });
      if (r1.ok) {
        const js = await r1.json();
        const mapped: Model[] = (js as any[]).map(x => ({ id: x.id ?? x.Id, name: x.name ?? x.Name, displayName: x.displayName ?? x.DisplayName }));
        setModels(mapped);
        if (!imageModel && mapped.length) setImageModel(mapped[0].name);
        return;
      }
      // Fallback to /api/llm/providers/:id/models
      const r2 = await fetch(`/api/llm/providers/${providerId}/models`, { headers });
      if (r2.ok) {
        const js = await r2.json();
        const mapped: Model[] = (js as any[]).map(x => ({ id: x.id ?? x.Id, name: x.name ?? x.Name, displayName: x.displayName ?? x.DisplayName }));
        setModels(mapped);
        if (!imageModel && mapped.length) setImageModel(mapped[0].name);
        return;
      }
      setModels([]);
      setImageModel('');
    }
    loadModels();
  }, [providerId, accessToken]);

  return { providers, providerId, setProviderId, models, imageModel, setImageModel };
}

