import { useCallback } from 'react';

export function useTts() {
  // Optionally expose available voices, etc.
  const speak = useCallback((text: string, voice?: SpeechSynthesisVoice) => {
    if (!text) return;
    const utter = new window.SpeechSynthesisUtterance(text);
    if (voice) utter.voice = voice;
    window.speechSynthesis?.speak(utter);
  }, []);

  return { speak };
}
