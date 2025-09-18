import { useEffect, useState } from 'react';
import { fetchConversations, fetchMessages } from '../api/client';
import { normalizeRole } from '../utils/chat';

type Conv = { id: string; title?: string | null };
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

export function useConversationsMessages(accessToken: string, personaId: string): {
  conversations: Conv[];
  conversationId: string | null;
  setConversationId: (id: string | null) => void;
  messages: Message[];
  setMessages: React.Dispatch<React.SetStateAction<Message[]>>;
} {
  const [conversations, setConversations] = useState<Conv[]>([]);
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);

  useEffect(() => {
    const loadConvs = async () => {
      if (!accessToken || !personaId) return;
      const list = await fetchConversations(accessToken, personaId);
      const items: Conv[] = (list as any[]).map((c: any) => ({ id: c.id ?? c.Id, title: c.title ?? c.Title }));
      setConversations(items);
      // Auto-select saved if valid, else first
      let saved: string | null = null;
      try { saved = localStorage.getItem('cognition.chat.conversationId'); } catch {}
      const pick = (saved && items.find(x => x.id === saved)) ? saved : (items[0]?.id || null);
      setConversationId(pick);
    };
    loadConvs();
  }, [accessToken, personaId]);

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
        fromId: m.fromPersonaId ?? m.FromPersonaId,
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
      setMessages(combined);
    };
    loadMsgs();
  }, [accessToken, conversationId]);

  // Clear messages when persona changes (new convo context)
  useEffect(() => {
    setMessages([]);
  }, [personaId]);

  return {
    conversations,
    conversationId,
    setConversationId,
    messages,
    setMessages,
  };
}
