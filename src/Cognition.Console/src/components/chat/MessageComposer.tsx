import React, { useState, useRef } from 'react';
import { Box, TextField, Button, IconButton, Tooltip } from '@mui/material';
import MicIcon from '@mui/icons-material/Mic';

export type MessageComposerProps = {
  onSend: (text: string) => void;
  busy?: boolean;
};

export function MessageComposer({ onSend, busy }: MessageComposerProps) {
  const [input, setInput] = useState('');
  const inputRef = useRef<HTMLInputElement | null>(null);

  const handleSend = () => {
    if (input.trim().length > 0 && !busy) {
      onSend(input.trim());
      setInput('');
    }
  };

  return (
    <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', mt: 2 }}>
      <TextField
        inputRef={inputRef}
        fullWidth
        size="small"
        placeholder="Type a message."
        value={input}
        onChange={e => setInput(e.target.value)}
        onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSend(); } }}
        autoComplete="off"
        inputProps={{ autoCorrect: 'off', autoCapitalize: 'off', spellCheck: false }}
      />
      <Tooltip title="Hold to record">
        <span>
          <IconButton aria-label="Record" size="medium" disabled={busy}>
            <MicIcon />
          </IconButton>
        </span>
      </Tooltip>
      <Button variant="contained" disabled={busy || input.trim().length === 0} onClick={handleSend}>Send</Button>
    </Box>
  );
}
