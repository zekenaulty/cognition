import React from 'react';
import { Alert, Box, Card, CardContent, CardHeader, LinearProgress, Stack, Typography } from '@mui/material';
import { FictionRosterPanel } from './FictionRosterPanel';
import { FictionRosterPanelProps } from './FictionRosterPanel';
import { AuthorPersonaContext } from '../../types/fiction';

type Props = {
  rosterCardRef?: React.RefObject<HTMLDivElement>;
  rosterProps: Pick<FictionRosterPanelProps, 'roster' | 'loading' | 'error' | 'placeholder' | 'onFulfillLore' | 'loreHistory' | 'loreHistoryLoading' | 'loreHistoryError'>;
  personaContext: AuthorPersonaContext | null;
  personaLoading: boolean;
  personaError: string | null;
};

export function FictionRosterPersonaSection({
  rosterCardRef,
  rosterProps,
  personaContext,
  personaLoading,
  personaError,
}: Props) {
  return (
    <>
      <Card ref={rosterCardRef}>
        <CardHeader
          title={rosterProps.roster ? rosterProps.roster.planName : 'Roster'}
          subheader={rosterProps.roster?.projectTitle}
        />
        <CardContent>
          <FictionRosterPanel {...rosterProps} />
        </CardContent>
      </Card>
      <Card>
        <CardHeader
          title="Author Persona"
          subheader={personaContext ? personaContext.personaName : undefined}
        />
        <CardContent>
          {personaLoading ? (
            <LinearProgress />
          ) : personaError ? (
            <Alert severity="warning">{personaError}</Alert>
          ) : personaContext ? (
            <Stack spacing={2}>
              <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-line' }}>
                {personaContext.summary}
              </Typography>
              <Box>
                <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                  Recent Memories
                </Typography>
                {personaContext.memories.length === 0 ? (
                  <Typography variant="body2" color="text.secondary">
                    No memories recorded yet.
                  </Typography>
                ) : (
                  <Stack spacing={0.5}>
                    {personaContext.memories.map((memory, idx) => (
                      <Typography key={`memory-${idx}`} variant="body2">
                        {memory}
                      </Typography>
                    ))}
                  </Stack>
                )}
              </Box>
              <Box>
                <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                  World Notes
                </Typography>
                {personaContext.worldNotes.length === 0 ? (
                  <Typography variant="body2" color="text.secondary">
                    No world notes captured.
                  </Typography>
                ) : (
                  <Stack spacing={0.5}>
                    {personaContext.worldNotes.map((note, idx) => (
                      <Typography key={`note-${idx}`} variant="body2">
                        {note}
                      </Typography>
                    ))}
                  </Stack>
                )}
              </Box>
            </Stack>
          ) : (
            <Typography variant="body2" color="text.secondary">
              Select a plan with an author persona to review recent memories and notes.
            </Typography>
          )}
        </CardContent>
      </Card>
    </>
  );
}
