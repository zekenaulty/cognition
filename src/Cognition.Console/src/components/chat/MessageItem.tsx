import React from 'react';
import { useTts } from '../../hooks/useTts';
import styles from './chat.module.css';
import { Box, IconButton, Tooltip } from '@mui/material';
import { FeedbackBar } from './FeedbackBar';
import VolumeUpIcon from '@mui/icons-material/VolumeUp';
import RefreshIcon from '@mui/icons-material/Refresh';
import ChevronLeftIcon from '@mui/icons-material/ChevronLeft';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import MarkdownView from '../MarkdownView';
import { normalizeRole } from '../../utils/chat';

export type MessageItemProps = {
    id?: string;
    role: 'system' | 'user' | 'assistant';
    content: string;
    fromName?: string;
    timestamp?: string;
    pending?: boolean;
    imageId?: string;
    imgPrompt?: string;
    imgStyleName?: string;
    metatype?: string;
    versions?: string[];
    versionIndex?: number;
};

export function MessageItem({ role, content, fromName, timestamp, pending, versions, versionIndex, ttsVoiceName, assistantGender, onRegenerate, onPrevVersion, onNextVersion }: MessageItemProps & {
  ttsVoiceName?: string;
  assistantGender?: string;
  onRegenerate?: () => void;
  onPrevVersion?: () => void;
  onNextVersion?: () => void;
}) {
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
                    {/* removed explicit pending label */}
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
                {/* Feedback + version controls + TTS under assistant messages */}
                <div className={`${styles['bubble-footer']} ${isUser ? styles['user'] : styles['assistant']}`}>
                    {normRole === 'assistant' && <FeedbackBar />}
                    {normRole === 'assistant' && (typeof versions !== 'undefined') && (
                      <Box sx={{ display: 'inline-flex', alignItems: 'center', ml: 1 }}>
                        <Tooltip title="Previous version">
                          <span>
                            <IconButton size="small" onClick={onPrevVersion} disabled={!versions || versions.length <= 1 || (versionIndex ?? 0) <= 0}>
                              <ChevronLeftIcon fontSize="small" />
                            </IconButton>
                          </span>
                        </Tooltip>
                        <Tooltip title="Next version">
                          <span>
                            <IconButton size="small" onClick={onNextVersion} disabled={!versions || versions.length <= 1 || (versionIndex ?? 0) >= (versions.length - 1)}>
                              <ChevronRightIcon fontSize="small" />
                            </IconButton>
                          </span>
                        </Tooltip>
                        <Tooltip title="Regenerate">
                          <span>
                            <IconButton size="small" onClick={onRegenerate}>
                              <RefreshIcon fontSize="small" />
                            </IconButton>
                          </span>
                        </Tooltip>
                        {typeof versionIndex === 'number' && versions && versions.length > 0 && (
                          <span className={styles['bubble-version']}>v{versionIndex! + 1}/{versions.length}</span>
                        )}
                      </Box>
                    )}
                    {normRole === 'assistant' && content && (
                        <IconButton
                            aria-label="Read aloud"
                            size="small"
                            className={styles['bubble-tts']}
                            onClick={() => speak(content, { preferredVoice: ttsVoiceName, gender: (assistantGender as any) })}
                        >
                            <VolumeUpIcon fontSize="small" />
                        </IconButton>
                    )}
                </div>
            </Box>
        </Box>
    );

}
