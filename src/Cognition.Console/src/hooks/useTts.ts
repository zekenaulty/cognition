import { useCallback } from 'react';

type Gender = 'male' | 'female' | undefined;

export function useTts() {
  const getVoices = () => (typeof window !== 'undefined' ? (window.speechSynthesis?.getVoices?.() || []) : []);

  const resolveVoice = (preferred?: string, gender?: Gender): SpeechSynthesisVoice | undefined => {
    const voices = getVoices();
    if (voices.length === 0) return undefined;
    const match = (v: SpeechSynthesisVoice, s: string) => v.name.toLowerCase() === s.toLowerCase() || v.voiceURI.toLowerCase() === s.toLowerCase();
    if (preferred) {
      const v = voices.find(v => match(v, preferred));
      if (v) return v;
    }
    if (gender) {
      const gn = gender.toLowerCase();
      // Prefer Google UK English Female/Male if present
      if (gn === 'female') {
        const v = voices.find(v => v.name === 'Google UK English Female' && v.lang === 'en-GB');
        if (v) return v;
      } else if (gn === 'male') {
        const v = voices.find(v => v.name === 'Google UK English Male' && v.lang === 'en-GB');
        if (v) return v;
      }
      // Fallback regex on name/URI
      const rx = gn === 'female' ? /(female|woman|girl)/i : /(male|man|boy)/i;
      const v2 = voices.find(v => rx.test(v.name + ' ' + v.voiceURI));
      if (v2) return v2;
      // Named fallback
      if (gn === 'female') {
        const z = voices.find(v => /zira/i.test(v.name));
        if (z) return z;
      } else {
        const m = voices.find(v => /mark/i.test(v.name));
        if (m) return m;
      }
    }
    // Prefer default en voices if available
    const en = voices.find(v => /^en(-|_)/i.test(v.lang));
    return en || voices[0];
  };

  const speak = useCallback((text: string, opts?: { preferredVoice?: string; gender?: Gender }) => {
    if (!text) return;
    const utter = new window.SpeechSynthesisUtterance(text);
    const voice = resolveVoice(opts?.preferredVoice, opts?.gender);
    if (voice) utter.voice = voice;
    window.speechSynthesis?.speak(utter);
  }, []);

  return { speak, getVoices, resolveVoice };
}
