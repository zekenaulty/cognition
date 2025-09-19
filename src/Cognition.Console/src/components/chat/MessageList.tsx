import React from 'react';
import { Box } from '@mui/material';
import { MessageItem, MessageItemProps } from './MessageItem';
import { useAutoScroll } from '../../hooks/useAutoScroll';
import { normalizeRole } from '../../utils/chat';

export type MessageListProps = {
  messages: MessageItemProps[];
  // Second param used to pass prompt string to viewer
  onImageClick?: (id: string, titleOrPrompt?: string) => void;
  ttsVoiceName?: string;
  assistantGender?: string;
  onRegenerate?: (index: number) => void;
  onPrevVersion?: (index: number) => void;
  onNextVersion?: (index: number) => void;
};

export function MessageList({ messages, onImageClick, ttsVoiceName, assistantGender, onRegenerate, onPrevVersion, onNextVersion }: MessageListProps) {
  const listRef = useAutoScroll(messages);
  return (
    <div ref={listRef}>
      {messages.map((msg, idx) => {
        const normRole = normalizeRole(msg.role);
        return (
          <React.Fragment key={(msg as any).localId || (msg as any).id || idx}>
            {/* Image message rendering: always display, show style/prompt as title, click-to-zoom */}
            {msg.imageId ? (
              <Box sx={{ mt: 0.5, mb: 2, display: 'flex', flexDirection: 'column', alignItems: normRole === 'user' ? 'flex-end' : 'flex-start' }}>
                <Box
                  component="img"
                  alt={msg.imgPrompt || 'generated'}
                  title={msg.imgPrompt || 'generated'}
                  src={`/api/images/content?id=${msg.imageId}`}
                  sx={{ height: 240, width: 'auto', borderRadius: 1, cursor: 'zoom-in', maxWidth: '100%' }}
                  onClick={() => { if (typeof onImageClick === 'function') onImageClick(msg.imageId!, msg.imgPrompt || ''); }}
                />
              </Box>
            ) : (
              <MessageItem
                {...msg}
                role={normRole}
                ttsVoiceName={ttsVoiceName}
                assistantGender={assistantGender}
                onRegenerate={onRegenerate ? () => onRegenerate(idx) : undefined}
                onPrevVersion={onPrevVersion ? () => onPrevVersion(idx) : undefined}
                onNextVersion={onNextVersion ? () => onNextVersion(idx) : undefined}
              />
            )}
          </React.Fragment>
        );
      })}
    </div>
  );
}
