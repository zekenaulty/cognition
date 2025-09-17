import { useEffect, useState } from 'react';
import { fetchImageMessages } from '../api/client';

type ImageMessage = {
  id: string;
  imageUrl: string;
  prompt?: string;
  styleName?: string;
  timestamp?: string;
  fromId?: string;
  fromName?: string;
};

export function useImageMessages(accessToken: string, conversationId: string) {
  const [imageMessages, setImageMessages] = useState<ImageMessage[]>([]);

  useEffect(() => {
    const loadImageMessages = async () => {
      if (!accessToken || !conversationId) return;
      const list = await fetchImageMessages(accessToken, conversationId);
      setImageMessages(list as ImageMessage[]);
    };
    loadImageMessages();
  }, [accessToken, conversationId]);

  return {
    imageMessages,
    setImageMessages,
  };
}
