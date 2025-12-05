import { useCallback, useEffect, useState } from 'react';
import { fetchConversationSettings, fetchLlmDefaults, patchConversationSettings } from '../api/client';
import { useProvidersModels } from './useProvidersModels';

const isGeminiLike = (name?: string, displayName?: string) => {
  const n = (name || displayName || '').toLowerCase();
  return n.includes('gemini') || n.includes('google');
};

const isFlashLike = (name?: string, displayName?: string) => {
  const n = (name || displayName || '').toLowerCase();
  return n.includes('flash') || n.includes('2.5') || n.includes('2.0') || n.includes('gemini');
};

export function useChatProviderModel(
  accessToken: string,
  conversationId: string | null,
  conversations: any[],
  setConversations: (fn: (prev: any[]) => any[]) => void
) {
  const [globalDefault, setGlobalDefault] = useState<{ providerId?: string; modelId?: string } | null>(null);
  const {
    providers,
    providerId,
    setProviderId,
    models,
    modelId,
    setModelId,
  } = useProvidersModels(accessToken, '', '');

  const pickPreferredProvider = useCallback(() => {
    if (!providers.length) return undefined;
    const preferred = providers.find(p => isGeminiLike(p.name, p.displayName));
    const nonOpenAi = providers.find(p => !(p.name || '').toLowerCase().includes('openai'));
    return (preferred ?? nonOpenAi ?? providers[0]).id;
  }, [providers]);

  const pickPreferredModel = useCallback(() => {
    if (!models.length) return undefined;
    const preferred = models.find(m => isFlashLike(m.name, m.displayName));
    return (preferred ?? models[0]).id;
  }, [models]);

  useEffect(() => {
    if (!accessToken) return;
    let cancelled = false;
    (async () => {
      try {
        const resp = await fetchLlmDefaults(accessToken);
        if (cancelled) return;
        setGlobalDefault({
          providerId: resp?.providerId ?? undefined,
          modelId: resp?.modelId ?? undefined,
        });
      } catch {
        if (!cancelled) setGlobalDefault({});
      }
    })();
    return () => { cancelled = true; };
  }, [accessToken]);

  // Force preferred defaults when no conversation is selected (new chat shell)
  useEffect(() => {
    if (conversationId) return;
    const defaultProvider = globalDefault?.providerId;
    if (defaultProvider && providers.some(p => p.id === defaultProvider)) {
      if (providerId !== defaultProvider) setProviderId(defaultProvider);
      return;
    }
    if (!providerId) {
      const nextProvider = pickPreferredProvider();
      if (nextProvider) setProviderId(nextProvider);
    }
  }, [conversationId, globalDefault, providers, providerId, pickPreferredProvider, setProviderId]);

  useEffect(() => {
    if (conversationId) return;
    const defaultModel = globalDefault?.modelId;
    if (defaultModel && models.some(m => m.id === defaultModel)) {
      if (modelId !== defaultModel) setModelId(defaultModel);
      return;
    }
    if (!modelId) {
      const nextModel = pickPreferredModel();
      if (nextModel) setModelId(nextModel);
    }
  }, [conversationId, globalDefault, modelId, models, pickPreferredModel, setModelId]);

  // Hydrate provider/model when entering a conversation
  useEffect(() => {
    let cancelled = false;
    if (conversationId && conversations.length) {
      const conv = conversations.find(c => c.id === conversationId);
      if (conv) {
        if (conv.providerId) setProviderId(conv.providerId);
        if (conv.modelId) setModelId(conv.modelId);
      }
    }
    const hydrate = async () => {
      if (!accessToken || !conversationId) return;
      try {
        const settingsResp = await fetchConversationSettings(accessToken, conversationId);
        if (cancelled) return;
        if (settingsResp?.providerId) {
          setProviderId(settingsResp.providerId);
        } else {
          const defaultProvider = globalDefault?.providerId && providers.some(p => p.id === globalDefault.providerId)
            ? globalDefault.providerId
            : pickPreferredProvider();
          if (defaultProvider) setProviderId(defaultProvider);
        }
        if (settingsResp?.modelId) {
          setModelId(settingsResp.modelId);
        } else {
          const defaultModel = globalDefault?.modelId && models.some(m => m.id === globalDefault.modelId)
            ? globalDefault.modelId
            : pickPreferredModel();
          if (defaultModel) setModelId(defaultModel);
        }
      } catch {
        // ignore
      }
    };
    hydrate();
    return () => { cancelled = true; };
  }, [accessToken, conversationId, conversations, globalDefault, models, pickPreferredModel, pickPreferredProvider, providers, setModelId, setProviderId]);

  // Persist provider/model to conversation and conversation list
  useEffect(() => {
    if (!accessToken || !conversationId || !providerId) return;
    patchConversationSettings(accessToken, conversationId, { providerId, modelId: modelId || null }).catch(() => {});
    setConversations(prev => prev.map(c => c.id === conversationId ? { ...c, providerId, modelId } : c));
  }, [accessToken, conversationId, providerId, modelId, setConversations]);

  return {
    providers,
    providerId,
    setProviderId,
    models,
    modelId,
    setModelId,
    pickPreferredProvider,
    pickPreferredModel,
  };
}
