import { useEffect, useRef } from 'react';

function findScrollContainer(el: HTMLElement | null): HTMLElement | null {
  let node: HTMLElement | null = el;
  while (node) {
    const style = window.getComputedStyle(node);
    const overflowY = style.overflowY;
    if ((overflowY === 'auto' || overflowY === 'scroll') && node.scrollHeight > node.clientHeight) return node;
    node = node.parentElement;
  }
  return null;
}

export function useAutoScroll(messages: any[], threshold = 24) {
  const listRef = useRef<HTMLDivElement>(null);
  const stickRef = useRef(true);
  const containerRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    if (!listRef.current) return;
    containerRef.current = findScrollContainer(listRef.current) || listRef.current.parentElement;
    const scroller = containerRef.current;
    if (!scroller) return;
    const onScroll = () => {
      const atBottom = (scroller.scrollHeight - scroller.scrollTop - scroller.clientHeight) <= threshold;
      stickRef.current = atBottom;
    };
    scroller.addEventListener('scroll', onScroll, { passive: true });
    // initialize stick state
    onScroll();
    return () => { scroller.removeEventListener('scroll', onScroll); };
  }, []);

  useEffect(() => {
    const scroller = containerRef.current;
    if (!scroller) return;
    if (stickRef.current) {
      scroller.scrollTop = scroller.scrollHeight;
    }
  }, [messages]);

  return listRef;
}
