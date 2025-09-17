import { useEffect, useRef } from 'react';

export function useAutoScroll(messages: any[]) {
  const listRef = useRef<HTMLDivElement>(null);
  const triggerScroll = () => {
    if (!listRef.current) return;
    listRef.current.scrollTop = listRef.current.scrollHeight;
  };
  useEffect(() => {
    triggerScroll();
  }, [messages]);
  return listRef;
}
