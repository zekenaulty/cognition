import { useState } from 'react';
import { MessageItemProps } from '../components/chat/MessageItem';

type ImageStyle = { id: string; name: string; description?: string; promptPrefix?: string; negativePrompt?: string };

export function useImageGenerator(params: {
  accessToken?: string;
  conversationId?: string | null;
  agentId?: string;
  personaId?: string;
  providerId?: string;
  modelId?: string | null;
  imgStyleId?: string;
  imgStyles: ImageStyle[];
  messages: MessageItemProps[];
  setMessages: React.Dispatch<React.SetStateAction<MessageItemProps[]>>;
  assistantName: string;
}) {
  const {
    accessToken,
    conversationId,
    agentId,
    personaId,
    providerId,
    modelId,
    imgStyleId,
    imgStyles,
    messages,
    setMessages,
    assistantName,
  } = params;

  const [pending, setPending] = useState(false);

  async function generateFromChat(imgModel: string = 'dall-e-3', msgCount: number = 6) {
    if (!accessToken || !conversationId || !agentId || !personaId || !providerId) return;
    const style = imgStyles.find(s => s.id === imgStyleId);
    const recent = messages.slice(-msgCount);
    const lines = recent.map(m => `${m.fromName || m.role}: ${m.content}`).join('\n');
    const styleRecipe = [`Style: ${style?.name || ''}`, style?.description || '', style?.promptPrefix || ''].filter(Boolean).join('\n');
    const sysInstr = `You are an expert prompt-writer for image models.\nGiven a conversation transcript and a style recipe, produce ONE concise, vivid, concrete image prompt.\nRules:\n- 1-4 sentences. <= 2500 characters total.\n- Describe subject, setting, background, foreground, lighting, mood, and camera.\n- Avoid copyrighted characters/logos and explicit sexual content.\n- Do NOT include disclaimers or the transcript itself. Output only the prompt.`;
    const userInstr = `Style recipe:\n${styleRecipe}\n\nConversation (recent):\n${lines}\n\nWrite the single best image prompt now.`;
    const promptBuildInput = `${sysInstr}\n\n${userInstr}`;

    setPending(true);
    const placeholderId = `img-${Date.now()}-${Math.random().toString(36).slice(2)}`;
    // Insert placeholder assistant image message
    setMessages(prev => [...prev, { role: 'assistant', content: 'Generating image', fromName: assistantName, pending: true, localId: placeholderId, imgPrompt: '', imgStyleName: style?.name, metatype: 'Image' } as any]);

    try {
      // Step 1: synthesize image prompt
      let finalPrompt = '';
      try {
        const resp = await fetch('/api/chat/ask', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` },
          body: JSON.stringify({ AgentId: agentId, PersonaId: personaId, ProviderId: providerId, ModelId: modelId || null, Input: promptBuildInput, RolePlay: false })
        });
        if (resp.ok) {
          const rj = await resp.json();
          finalPrompt = String(rj.reply || '').trim();
        }
      } catch {}
      if (!finalPrompt) {
        finalPrompt = `${style?.promptPrefix ? style.promptPrefix + '\n' : ''}${lines}`.slice(0, 2500);
      }
      setMessages(prev => prev.map(m => (m as any).localId === placeholderId ? { ...m, imgPrompt: finalPrompt, metatype: 'Image' } : m));

      // Step 2: request image generation
      const payload = { ConversationId: conversationId, AgentId: agentId, PersonaId: personaId, Prompt: finalPrompt, Width: 1024, Height: 1024, StyleId: imgStyleId || undefined, NegativePrompt: style?.negativePrompt || undefined, Provider: 'OpenAI', Model: imgModel };
      const res = await fetch('/api/images/generate', { method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` }, body: JSON.stringify(payload) });
      if (!res.ok) {
        const txt = await res.text();
        setMessages(prev => prev.map(m => (m as any).localId === placeholderId ? { ...m, pending: false, content: `Image error: ${txt}` } : m));
        return;
      }
      const data = await res.json();
      const id = data.id || data.Id;
      setMessages(prev => prev.map(m => (m as any).localId === placeholderId ? { ...m, pending: false, content: '', imageId: String(id), imgPrompt: finalPrompt, imgStyleName: style?.name, metatype: 'Image' } : m));
    } catch (e: any) {
      setMessages(prev => prev.map(m => (m as any).localId === placeholderId ? { ...m, pending: false, content: `Image error: ${String(e?.message || e)}` } : m));
    } finally {
      setPending(false);
    }
  }

  return { generateFromChat, pending };
}

