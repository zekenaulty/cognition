import { useState } from 'react';

export function useImageActions(setMessages: (fn: (prev: any[]) => any[]) => void) {
  const [pendingImageId, setPendingImageId] = useState<string | null>(null);

  async function createImageMessage(style: string, prompt: string) {
    // Add a pending placeholder message
    const localId = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
    setPendingImageId(localId);
    setMessages(prev => [
      ...prev,
      {
        role: 'assistant',
        content: '',
        imageId: localId,
        imgPrompt: prompt,
        imgStyleName: style,
        pending: true,
        localId,
      },
    ]);
    try {
      // Simulate image creation API call
      const res = await fetch('/api/images/generate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ style, prompt }),
      });
      if (!res.ok) throw new Error('Image generation failed');
      const data = await res.json();
      // Replace placeholder with final image message
      setMessages(prev => prev.map(m =>
        m.imageId === localId
          ? { ...m, imageId: data.id, pending: false, imgPrompt: prompt, imgStyleName: style }
          : m
      ));
      setPendingImageId(null);
    } catch (err) {
      // Remove pending placeholder on error
      setMessages(prev => prev.filter(m => m.imageId !== localId));
      setPendingImageId(null);
    }
  }

  return { createImageMessage, pendingImageId };
}
