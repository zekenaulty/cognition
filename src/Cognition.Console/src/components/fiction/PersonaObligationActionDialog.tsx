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
  Typography,
  Checkbox,
  FormControlLabel,
  Divider
} from '@mui/material';
import { PersonaObligation } from '../../types/fiction';
import { formatObligationStatus, summarizeObligationMetadata, formatResolutionNoteText } from './backlogUtils';

type Props = {
  open: boolean;
  obligation: PersonaObligation | null;
  action: 'resolve' | 'dismiss';
  submitting: boolean;
  error?: string | null;
  onSubmit: (payload: { notes: string; voiceDrift?: boolean | null }) => void;
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
  const [voiceDrift, setVoiceDrift] = React.useState(false);

  React.useEffect(() => {
    if (open) {
      setNotes('');
      setVoiceDrift(false);
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
      onSubmit({ notes: notes.trim(), voiceDrift });
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
                {obligation.sourcePhase ? ` • Source ${obligation.sourcePhase}` : ''}
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
            {metadata.resolutionNotes.length > 0 && (
              <Stack spacing={0.5}>
                <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                  Resolution history
                </Typography>
                <Stack spacing={0.25}>
                  {metadata.resolutionNotes.map(note => (
                    <Typography key={note.id} variant="caption" color="text.secondary">
                      {formatResolutionNoteText(note)}
                    </Typography>
                  ))}
                </Stack>
                <Divider flexItem />
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
            <FormControlLabel
              control={
                <Checkbox
                  id="voiceDrift"
                  checked={voiceDrift}
                  onChange={evt => setVoiceDrift(evt.target.checked)}
                  disabled={submitting}
                  size="small"
                />
              }
              label={
                <Typography variant="caption" color="text.secondary">
                  Flag voice drift noted in this resolution
                </Typography>
              }
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
