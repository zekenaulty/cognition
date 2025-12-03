import { useCallback, useEffect } from 'react';
import { fetchConversationSettings, patchConversationSettings } from '../api/client';
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

  // Force preferred defaults when no conversation is selected (new chat shell)
  useEffect(() => {
    if (!conversationId) {
      const nextProvider = pickPreferredProvider();
      if (nextProvider) setProviderId(nextProvider);
      const nextModel = pickPreferredModel();
      if (nextModel) setModelId(nextModel);
    }
  }, [conversationId, pickPreferredProvider, pickPreferredModel, setModelId, setProviderId]);

  // If a better preferred exists and current points to OpenAI, override for new chat shell
  useEffect(() => {
    if (conversationId) return;
    const preferred = pickPreferredProvider();
    if (preferred && providerId && preferred !== providerId) {
      const current = providers.find(p => p.id === providerId);
      const currentIsOpenAi = current && (current.name || '').toLowerCase().includes('openai');
      if (currentIsOpenAi) {
        setProviderId(preferred);
      }
    }
  }, [conversationId, pickPreferredProvider, providerId, providers, setProviderId]);

  useEffect(() => {
    if (conversationId) return;
    const preferred = pickPreferredModel();
    if (preferred && modelId && preferred !== modelId) {
      const current = models.find(m => m.id === modelId);
      const currentIsOpenAi = current && (current.name || '').toLowerCase().includes('gpt');
      if (currentIsOpenAi) {
        setModelId(preferred);
      }
    }
  }, [conversationId, pickPreferredModel, modelId, models, setModelId]);

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
        if (settingsResp?.providerId) setProviderId(settingsResp.providerId);
        if (settingsResp?.modelId) setModelId(settingsResp.modelId);
        if (!settingsResp?.providerId) {
          const next = pickPreferredProvider();
          if (next) setProviderId(next);
        }
        if (!settingsResp?.modelId) {
          const nextModel = pickPreferredModel();
          if (nextModel) setModelId(nextModel);
        }
      } catch {
        // ignore
      }
    };
    hydrate();
    return () => { cancelled = true; };
  }, [accessToken, conversationId, conversations, pickPreferredModel, pickPreferredProvider, setModelId, setProviderId]);

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
