import { useEffect, useState } from 'react';
import { fetchConversations, fetchMessages } from '../api/client';
import { normalizeRole } from '../utils/chat';

type Conv = { id: string; title?: string | null; providerId?: string | null; modelId?: string | null };
type Message = {
  id?: string;
  role: 'system' | 'user' | 'assistant';
  content: string;
  fromId?: string;
  fromName?: string;
  timestamp?: string;
  imageId?: string;
  pending?: boolean;
  localId?: string;
  imgPrompt?: string;
  imgStyleName?: string;
  metatype?: string;
  versions?: string[];
  versionIndex?: number;
};

export function useConversationsMessages(accessToken: string, agentId: string | undefined): {
  conversations: Conv[];
  conversationId: string | null;
  setConversationId: (id: string | null) => void;
  messages: Message[];
  setMessages: React.Dispatch<React.SetStateAction<Message[]>>;
  setConversations: React.Dispatch<React.SetStateAction<Conv[]>>;
} {
  const [conversations, setConversations] = useState<Conv[]>([]);
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);

  useEffect(() => {
    const loadConvs = async () => {
      if (!accessToken || !agentId) return;
      const list = await fetchConversations(accessToken, { agentId });
      const items: Conv[] = (list as any[]).map((c: any) => ({
        id: c.id ?? c.Id,
        title: c.title ?? c.Title,
        providerId: c.providerId ?? c.ProviderId ?? null,
        modelId: c.modelId ?? c.ModelId ?? null
      }));
      setConversations(items);
      // Do NOT auto-select a conversation. Routing/new-convo flow controls selection now.
    };
    loadConvs();
  }, [accessToken, agentId]);

  useEffect(() => {
    const loadMsgs = async () => {
      if (!accessToken || !conversationId) return;
      // Fetch chat messages and images in parallel
      const [msgList, imgList] = await Promise.all([
        fetchMessages(accessToken, conversationId),
        fetch(`/api/images/by-conversation/${conversationId}`, { headers: { Authorization: `Bearer ${accessToken}` } })
          .then(res => res.ok ? res.json() : [])
      ]);

      // Normalize chat messages
      const baseMsgs: Message[] = (msgList as any[]).map((m: any) => ({
        id: m.id ?? m.Id,
        role: normalizeRole(m.role ?? m.Role),
        content: m.content ?? m.Content,
        fromId: m.fromAgentId ?? m.FromAgentId ?? m.fromPersonaId ?? m.FromPersonaId,
        fromName: m.fromName ?? m.FromName,
        timestamp: m.timestamp ?? m.Timestamp,
        pending: m.pending,
        imageId: m.imageId,
        imgPrompt: m.imgPrompt,
        imgStyleName: m.imgStyleName,
        metatype: m.metatype,
        versions: m.versions ?? m.Versions,
        versionIndex: m.versionIndex ?? m.VersionIndex,
      }));

      // Normalize image messages
      const imageMsgs: Message[] = (imgList as any[]).map((i: any) => ({
        role: 'assistant',
        content: '',
        imageId: String(i.id ?? i.Id),
        fromName: i.fromName ?? i.FromName,
        timestamp: i.createdAtUtc ?? i.CreatedAtUtc,
        imgPrompt: i.prompt ?? i.Prompt,
        imgStyleName: i.styleName ?? i.StyleName,
        metatype: 'Image',
      }));

      // Merge and sort
      const combined = [...baseMsgs, ...imageMsgs];
      combined.sort((a, b) => {
        const ta = a.timestamp ? Date.parse(a.timestamp) : 0;
        const tb = b.timestamp ? Date.parse(b.timestamp) : 0;
        return ta - tb;
      });
      setMessages(prev => {
        if (combined.length === 0) return prev; // avoid dropping pending UI messages when fetch returns empty
        return combined;
      });
    };
    loadMsgs();
  }, [accessToken, conversationId]);

  // Clear messages when agent context changes (new convo scope)
  useEffect(() => {
    setMessages([]);
  }, [agentId]);

  return {
    conversations,
    conversationId,
    setConversationId,
    messages,
    setMessages,
    setConversations,
  };
}
