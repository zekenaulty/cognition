import React from 'react';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography
} from '@mui/material';
import { PersonaObligation } from '../../types/fiction';
import { formatObligationStatus, summarizeObligationMetadata } from './backlogUtils';

type Props = {
  open: boolean;
  obligation: PersonaObligation | null;
  action: 'resolve' | 'dismiss';
  submitting: boolean;
  error?: string | null;
  onSubmit: (notes: string) => void;
  onClose: () => void;
};

export function PersonaObligationActionDialog({
  open,
  obligation,
  action,
  submitting,
  error,
  onSubmit,
  onClose
}: Props) {
  const [notes, setNotes] = React.useState('');

  React.useEffect(() => {
    if (open) {
      setNotes('');
    }
  }, [open, obligation?.id, action]);

  if (!obligation) {
    return null;
  }

  const metadata = summarizeObligationMetadata(obligation.metadata);
  const disabled = submitting || notes.trim().length === 0;

  const handleSubmit = (evt: React.FormEvent) => {
    evt.preventDefault();
    if (!disabled) {
      onSubmit(notes.trim());
    }
  };

  return (
    <Dialog open={open} onClose={submitting ? undefined : onClose} fullWidth maxWidth="sm">
      <DialogTitle>{action === 'dismiss' ? 'Dismiss obligation' : 'Resolve obligation'}</DialogTitle>
      <form onSubmit={handleSubmit}>
        <DialogContent dividers>
          <Stack spacing={2}>
            <Stack spacing={0.25}>
              <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                {obligation.title}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {formatObligationStatus(obligation.status)} • Persona {obligation.personaName}
                {obligation.branchSlug ? ` • Branch ${obligation.branchSlug}` : ''}
              </Typography>
              {obligation.description && (
                <Typography variant="body2" color="text.secondary">
                  {obligation.description}
                </Typography>
              )}
            </Stack>
            {metadata.otherEntries.length > 0 && (
              <Stack spacing={0.25}>
                {metadata.otherEntries.map(entry => (
                  <Typography key={entry} variant="caption" color="text.secondary">
                    {entry}
                  </Typography>
                ))}
              </Stack>
            )}
            <TextField
              label="Resolution notes"
              value={notes}
              onChange={evt => setNotes(evt.target.value)}
              required
              multiline
              minRows={3}
              disabled={submitting}
              helperText="Explain what changed or why this obligation is being closed."
              autoFocus
            />
            {error && <Alert severity="error">{error}</Alert>}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose} disabled={submitting}>
            Cancel
          </Button>
          <Button type="submit" variant="contained" disabled={disabled}>
            {action === 'dismiss' ? 'Dismiss' : 'Resolve'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
