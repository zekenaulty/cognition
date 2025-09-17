import { useEffect, useState } from 'react';
import { fetchImageStyles } from '../api/client';

type ImageStyle = { id: string; name: string; description?: string; promptPrefix?: string; negativePrompt?: string };

export function useImageStyles(accessToken: string) {
  const [imgStyles, setImgStyles] = useState<ImageStyle[]>([]);
  const [imgStyleId, setImgStyleId] = useState<string>('');

  useEffect(() => {
    const loadStyles = async () => {
      const list = await fetchImageStyles(accessToken);
      const items: ImageStyle[] = (list as any[]).map((s: any) => ({
        id: s.id ?? s.Id,
        name: s.name ?? s.Name,
        description: s.description ?? s.Description,
        promptPrefix: s.promptPrefix ?? s.PromptPrefix,
        negativePrompt: s.negativePrompt ?? s.NegativePrompt,
      }));
      setImgStyles(items);
      if (!imgStyleId && items.length > 0) setImgStyleId(items[0].id);
    };
    loadStyles();
  }, [accessToken]);

  return {
    imgStyles,
    imgStyleId,
    setImgStyleId,
  };
}
