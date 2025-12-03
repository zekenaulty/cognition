import type React from 'react';
import { useChatBus } from './useChatBus';
import { MessageItemProps } from '../components/chat/MessageItem';
import { normalizeRole } from '../utils/chat';

type Params = {
  conversationId: string | null;
  accessToken: string;
  agents: any[];
  assistantName: string;
  setMessages: React.Dispatch<React.SetStateAction<MessageItemProps[]>>;
  conversations: any[];
  setConversations: React.Dispatch<React.SetStateAction<any[]>>;
  setPlanSteps: React.Dispatch<React.SetStateAction<string[]>>;
  setToolActions: React.Dispatch<React.SetStateAction<string[]>>;
  setHubState?: React.Dispatch<React.SetStateAction<'connecting' | 'connected' | 'reconnecting' | 'disconnected'>>;
};

export function useChatHubEvents({
  conversationId,
  accessToken,
  agents,
  assistantName,
  setMessages,
  conversations,
  setConversations,
  setPlanSteps,
  setToolActions,
  setHubState,
}: Params) {
  useChatBus('connection-state', evt => {
    if (!evt || !setHubState) return;
    setHubState(evt.state);
  });

  useChatBus('assistant-delta', evt => {
    if (!evt || evt.conversationId !== (conversationId ?? '')) return;
    const delta = evt.text || '';
    if (!delta) return;
    setMessages(prev => {
      const updated = prev.map(m => ({ ...m, role: normalizeRole(m.role) }));
      const idx = updated.findIndex(m => m.role === 'assistant' && (m as any).pending);
      if (idx !== -1) {
        const copy = [...updated];
        const current = copy[idx];
        copy[idx] = { ...current, content: (current.content || '') + delta } as MessageItemProps;
        return copy;
      }
      const assistantMsg: MessageItemProps = {
        role: 'assistant',
        content: delta,
        fromName: assistantName,
        timestamp: new Date().toISOString(),
        pending: true,
      } as any;
      return [...updated, assistantMsg];
    });
  });

  useChatBus('assistant-message', msg => {
    if (!msg || msg.conversationId !== (conversationId ?? '')) return;
    const displayFrom = msg.agentId
      ? (agents.find(a => a.id === msg.agentId)?.label ?? assistantName)
      : assistantName;
    setMessages(prev => {
      const updated = prev.map(m => ({ ...m, role: normalizeRole(m.role) }));
      const pendingIdx = updated.findIndex(m => m.role === 'assistant' && (m as any).pending);
      if (pendingIdx !== -1) {
        const copy = [...updated];
        const current = copy[pendingIdx] as MessageItemProps;
        const baseVersions: string[] = Array.isArray(current.versions) && current.versions.length ? current.versions : (current.content ? [current.content] : []);
        const nextVersions = baseVersions.length ? [...baseVersions.slice(0, baseVersions.length - 1), msg.content] : [msg.content];
        copy[pendingIdx] = {
          ...current,
          id: (msg as any).messageId,
          pending: false,
          content: msg.content,
          fromName: displayFrom,
          timestamp: msg.timestamp,
          versions: nextVersions,
          versionIndex: Math.max(0, nextVersions.length - 1),
        };
        return copy;
      }
      const assistantMsg: MessageItemProps = {
        role: 'assistant',
        id: (msg as any).messageId,
        content: msg.content,
        fromName: displayFrom,
        timestamp: msg.timestamp,
        versions: [msg.content],
        versionIndex: 0,
      };
      return [...updated, assistantMsg];
    });
    const convId = conversationId ?? '';
    if (!convId || !accessToken) return;
    const existing = conversations.find(c => c.id === convId);
    if (existing && existing.title) return;
    (async () => {
      try {
        const res = await fetch(`/api/conversations/${convId}`, { headers: { Authorization: `Bearer ${accessToken}` } });
        if (res.ok) {
          const data = await res.json();
          const title = data?.title ?? data?.Title;
          if (title) {
            setConversations(prev => prev.map(c => (c.id === convId ? { ...c, title } : c)));
          }
        }
      } catch {}
    })();
  });

  useChatBus('conversation-updated', evt => {
    if (!evt) return;
    const cid = String(evt.conversationId || '');
    if (!cid) return;
    const title = (evt.title ?? '').toString();
    setConversations(prev => prev.map(c => (c.id === cid ? { ...c, title } : c)));
  });

  useChatBus('plan-ready', evt => {
    if (!evt || evt.conversationId !== (conversationId ?? '')) return;
    const label = evt.agentId ? (agents.find(a => a.id === evt.agentId)?.label ?? 'Agent') : 'Agent';
    const steps = Array.isArray(evt.plan) ? evt.plan : [evt.plan];
    setPlanSteps(steps.filter(Boolean).map(s => `${label}: ${s}`));
  });

  useChatBus('tool-requested', evt => {
    if (!evt || evt.conversationId !== (conversationId ?? '')) return;
    const label = evt.agentId ? (agents.find(a => a.id === evt.agentId)?.label ?? 'Agent') : 'Agent';
    setToolActions(prev => [...prev, `${label} requested tool: ${evt.toolId}`]);
  });

  useChatBus('tool-completed', evt => {
    if (!evt || evt.conversationId !== (conversationId ?? '')) return;
    const label = evt.agentId ? (agents.find(a => a.id === evt.agentId)?.label ?? 'Agent') : 'Agent';
    setToolActions(prev => [...prev, `${label} completed tool: ${evt.toolId} (${evt.success ? 'success' : 'error'})`]);
  });

  useChatBus('assistant-version-appended', evt => {
    if (!evt || evt.conversationId !== (conversationId ?? '')) return;
    setMessages(prev => {
      const copy = [...prev];
      const idx = copy.findIndex(m => (m as any).id === evt.messageId);
      if (idx === -1) return prev;
      const current: any = copy[idx];
      const base = Array.isArray(current.versions) ? current.versions : (current.content ? [current.content] : []);
      const next = [...base];
      const targetIndex = typeof evt.versionIndex === 'number' ? evt.versionIndex : next.length;
      next[targetIndex] = evt.content;
      copy[idx] = { ...current, versions: next, versionIndex: targetIndex, content: evt.content };
      return copy;
    });
  });

  useChatBus('assistant-version-activated', evt => {
    if (!evt || evt.conversationId !== (conversationId ?? '')) return;
    setMessages(prev => {
      const copy = [...prev];
      const idx = copy.findIndex(m => (m as any).id === evt.messageId);
      if (idx === -1) return prev;
      const current: any = copy[idx];
      const versions: string[] = Array.isArray(current.versions) ? current.versions : (current.content ? [current.content] : []);
      const targetIndex = Math.max(0, Math.min(versions.length - 1, evt.versionIndex ?? 0));
      copy[idx] = { ...current, versionIndex: targetIndex, content: versions[targetIndex] };
      return copy;
    });
  });

  useChatBus('conversation-created', evt => {
    if (!evt) return;
    // handled in ChatPage send flow
  });

  useChatBus('conversation-joined', evt => {
    if (!evt) return;
    // handled in ChatPage send flow
  });

  useChatBus('conversation-left', evt => {
    if (!evt) return;
    // handled in ChatPage for state clearing
  });
}
