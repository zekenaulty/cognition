import React from 'react';
import { Box } from '@mui/material';
import { MessageItem, MessageItemProps } from './MessageItem';
import { useAutoScroll } from '../../hooks/useAutoScroll';

export type MessageListProps = {
  messages: MessageItemProps[];
  onImageClick?: (id: string, title?: string) => void;
};

export function MessageList({ messages, onImageClick }: MessageListProps) {
  const listRef = useAutoScroll(messages);
  // Normalize role for all messages (string only)
  const normalizeRole = (r: any): 'system' | 'user' | 'assistant' => {
    if (r === 1 || r === '1' || r === 'user') return 'user';
    if (r === 2 || r === '2' || r === 'assistant') return 'assistant';
    if (r === 0 || r === '0' || r === 'system') return 'system';
    return 'user';
  };
  return (
    <div ref={listRef}>
      {messages.map((msg, idx) => {
        const normRole = normalizeRole(msg.role);
        return (
          <React.Fragment key={idx}>
            {/* Image message rendering: always display, show style/prompt as title, click-to-zoom */}
            {msg.imageId ? (
              <Box sx={{ mt: 0.5, mb: 2, display: 'flex', flexDirection: 'column', alignItems: normRole === 'user' ? 'flex-end' : 'flex-start' }}>
                <Box
                  component="img"
                  alt={msg.imgStyleName ? `[${msg.imgStyleName}] ${msg.imgPrompt || ''}` : (msg.imgPrompt || 'generated')}
                  title={msg.imgStyleName ? `[${msg.imgStyleName}] ${msg.imgPrompt || ''}` : (msg.imgPrompt || 'generated')}
                  src={`/api/images/content?id=${msg.imageId}`}
                  sx={{ height: 240, width: 'auto', borderRadius: 1, cursor: 'zoom-in', maxWidth: '100%' }}
                  onClick={() => { if (typeof onImageClick === 'function') onImageClick(msg.imageId!, (msg.imgStyleName ? `[${msg.imgStyleName}] ` : '') + (msg.imgPrompt || '')); }}
                />
                {/* Show style/prompt as caption below image */}
                  {(msg.imgStyleName || msg.imgPrompt) && (
                    <Box sx={{ mt: 0.5 }}>
                      <span className={"image-caption"}>
                        {msg.imgStyleName ? `[${msg.imgStyleName}] ` : ''}{msg.imgPrompt || ''}
                      </span>
                    </Box>
                  )}
              </Box>
            ) : (
              <MessageItem {...msg} role={normRole} />
            )}
          </React.Fragment>
        );
      })}
    </div>
  );
}
