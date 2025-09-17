import { useEffect } from 'react';
import { chatBus, ChatBusEvents } from '../bus/chatBus';

export function useChatBus<K extends keyof ChatBusEvents>(
  event: K,
  handler: (payload: ChatBusEvents[K]) => void
) {
  useEffect(() => {
    const off = chatBus.on(event, handler as any);
    return () => { off(); };
  }, [event, handler]);
}

