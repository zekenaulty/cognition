import React, { useRef, useState } from 'react';
import { Box, TextField, Button, Tooltip, IconButton } from '@mui/material';
import MicIcon from '@mui/icons-material/Mic';
import EmojiButton from '../EmojiButton';

export type MessageInputProps = {
  value: string;
  onChange: (v: string) => void;
  onSend: () => void;
  busy?: boolean;
  onSTT?: (text: string) => void;
};

export function MessageInput({ value, onChange, onSend, busy, onSTT }: MessageInputProps) {
  const inputRef = useRef<HTMLInputElement | null>(null);
  const [recognizing, setRecognizing] = useState(false);

  // Emoji insert handler
  const handleEmojiInsert = (emoji: string) => {
    const el = inputRef.current;
    if (!el) { onChange(value + emoji); return; }
    const start = el.selectionStart ?? value.length;
    const end = el.selectionEnd ?? value.length;
    const before = value.slice(0, start);
    const after = value.slice(end);
    const next = before + emoji + after;
    onChange(next);
    // Restore caret after inserted emoji
    setTimeout(() => { try { el.focus(); const pos = start + emoji.length; el.setSelectionRange(pos, pos); } catch {} }, 0);
  };

  // STT logic (browser SpeechRecognition)
  const startRecognition = () => {
    const SpeechRecognition = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;
    if (!SpeechRecognition) {
      alert('Speech recognition not supported in this browser.');
      return;
    }
    const recognition = new SpeechRecognition();
    recognition.lang = 'en-US';
    recognition.interimResults = false;
    recognition.maxAlternatives = 1;
    recognition.onresult = (event: any) => {
      const transcript = event.results[0][0].transcript;
      if (onSTT) onSTT(transcript);
      setRecognizing(false);
    };
    recognition.onerror = () => setRecognizing(false);
    recognition.onend = () => setRecognizing(false);
    recognition.start();
    setRecognizing(true);
  };
  const stopRecognition = () => setRecognizing(false);

  return (
    <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', mt: 2 }}>
      <TextField
        inputRef={inputRef}
        fullWidth
        size="small"
        placeholder="Type a message."
        value={value}
        onChange={e => onChange(e.target.value)}
        onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); onSend(); } }}
        autoComplete="off"
        inputProps={{ autoCorrect: 'off', autoCapitalize: 'off', spellCheck: false }}
      />
      <EmojiButton
        onInsert={handleEmojiInsert}
        onCloseFocus={() => { try { inputRef.current?.focus(); } catch {} }}
      />
      <Tooltip title="Hold to record">
        <span>
          <IconButton
            aria-label="Record"
            size="medium"
            disabled={busy || recognizing}
            onMouseDown={startRecognition}
            onMouseUp={stopRecognition}
            onTouchStart={startRecognition}
            onTouchEnd={stopRecognition}
          >
            <MicIcon />
          </IconButton>
        </span>
      </Tooltip>
      <Button variant="contained" disabled={busy || !value.trim()} onClick={onSend}>Send</Button>
    </Box>
  );
}
