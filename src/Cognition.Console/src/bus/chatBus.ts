type Handler<T> = (payload: T) => void;

export type ChatBusEvents = {
  'assistant-message': { conversationId: string; personaId: string; content: string; timestamp: string };
  'plan-ready': { conversationId: string; personaId: string; plan: any; timestamp: string };
  'tool-requested': { conversationId: string; personaId: string; toolId: string; args: Record<string, any>; timestamp: string };
  'tool-completed': { conversationId: string; personaId: string; toolId: string; result: any; success: boolean; error?: string; timestamp: string };
  'connection-state': { state: 'connecting' | 'connected' | 'reconnecting' | 'disconnected' };
};

class ChatBus {
  private listeners: Map<keyof ChatBusEvents, Set<Handler<any>>> = new Map();

  on<K extends keyof ChatBusEvents>(event: K, handler: Handler<ChatBusEvents[K]>) {
    if (!this.listeners.has(event)) this.listeners.set(event, new Set());
    this.listeners.get(event)!.add(handler as Handler<any>);
    return () => this.off(event, handler);
  }

  off<K extends keyof ChatBusEvents>(event: K, handler: Handler<ChatBusEvents[K]>) {
    this.listeners.get(event)?.delete(handler as Handler<any>);
  }

  emit<K extends keyof ChatBusEvents>(event: K, payload: ChatBusEvents[K]) {
    const set = this.listeners.get(event);
    if (!set) return;
    for (const h of set) {
      try { h(payload); } catch {}
    }
  }
}

export const chatBus = new ChatBus();

