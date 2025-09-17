import React from 'react';
import { useTts } from '../../hooks/useTts';
import styles from './chat.module.css';
import { Box, Typography, IconButton } from '@mui/material';
import { FeedbackBar } from './FeedbackBar';
import VolumeUpIcon from '@mui/icons-material/VolumeUp';
import MarkdownView from '../MarkdownView';
import { normalizeRole } from '../../utils/chat';

export type MessageItemProps = {
    role: 'system' | 'user' | 'assistant';
    content: string;
    fromName?: string;
    timestamp?: string;
    pending?: boolean;
    imageId?: string;
    imgPrompt?: string;
    imgStyleName?: string;
    metatype?: string;
};

export function MessageItem({ role, content, fromName, timestamp, pending }: MessageItemProps) {
    const { speak } = useTts();
    const normRole = normalizeRole(role);
    const isUser = normRole === 'user';
    const isAssistantPending = normRole === 'assistant' && pending;
    let displayName = 'User';
    if (normRole === 'user') displayName = 'You';
    else if (normRole === 'assistant') displayName = fromName || 'Assistant';
    else if (normRole === 'system') displayName = 'System';

    return (
        <Box sx={{ mb: 2, display: 'flex', flexDirection: 'column', alignItems: isUser ? 'flex-end' : 'flex-start' }}>
            <Box className={`${styles['chat-bubble']} ${isUser ? styles['user'] : styles['assistant']}`}>
                <div className={`${styles['bubble-header']} ${isUser ? styles['user'] : styles['assistant']}`}>
                    <span className={styles['bubble-name']}>{displayName}</span>
                    {timestamp && (
                        <span className={styles['bubble-timestamp']}>{new Date(timestamp).toLocaleTimeString()}</span>
                    )}
                    {isAssistantPending && (
                        <span className={styles['pending-state']}>(thinking...)</span>
                    )}
                </div>
                {isAssistantPending ? (
                    <div className={isUser ? styles['bubble-right'] : styles['bubble-left']}>
                        {content ? content + ' ' : ''}
                        <span className={styles['loading-dots']}><span>.</span><span>.</span><span>.</span></span>
                    </div>
                ) : (
                    <div className={isUser ? styles['bubble-right'] : styles['bubble-left']}>
                        <MarkdownView content={content} />
                    </div>
                )}
                {/* FeedbackBar and TTS button under every message */}
                <div className={`${styles['bubble-footer']} ${isUser ? styles['user'] : styles['assistant']}`}>
                    <FeedbackBar />
                    {normRole === 'assistant' && content && (
                        <IconButton
                            aria-label="Read aloud"
                            size="small"
                            className={styles['bubble-tts']}
                            onClick={() => speak(content)}
                        >
                            <VolumeUpIcon fontSize="small" />
                        </IconButton>
                    )}
                </div>
            </Box>
        </Box>
    );

}
