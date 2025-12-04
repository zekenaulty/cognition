import React from 'react';
import { Alert, Box, Card, CardContent, CardHeader, LinearProgress, List, ListItem, ListItemText, Stack, Typography } from '@mui/material';
import { AuthorPersonaContext } from '../../types/fiction';

type Props = {
  rosterPlanId: string | null;
  persona: AuthorPersonaContext | null;
  loading: boolean;
  error: string | null;
};

export function PersonaCard({ rosterPlanId, persona, loading, error }: Props) {
  return (
    <Card>
      <CardHeader
        title="Author Persona"
        subheader={persona ? persona.personaName : undefined}
      />
      <CardContent>
        {!rosterPlanId ? (
          <Typography variant="body2" color="text.secondary">
            Select a plan with an author persona to inspect its memories and world notes.
          </Typography>
        ) : loading ? (
          <LinearProgress />
        ) : error ? (
          <Alert severity="warning">{error}</Alert>
        ) : persona ? (
          <Stack spacing={2}>
            <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-line' }}>
              {persona.summary}
            </Typography>
            <Box>
              <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                Recent Memories
              </Typography>
              {persona.memories.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  No memories recorded for this persona.
                </Typography>
              ) : (
                <List dense>
                  {persona.memories.map((memory, idx) => (
                    <ListItem key={`persona-memory-${idx}`} sx={{ py: 0 }}>
                      <ListItemText primary={memory} />
                    </ListItem>
                  ))}
                </List>
              )}
            </Box>
            <Box>
              <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                World Notes
              </Typography>
              {persona.worldNotes.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  No world notes captured yet.
                </Typography>
              ) : (
                <List dense>
                  {persona.worldNotes.map((note, idx) => (
                    <ListItem key={`persona-note-${idx}`} sx={{ py: 0 }}>
                      <ListItemText primary={note} />
                    </ListItem>
                  ))}
                </List>
              )}
            </Box>
          </Stack>
        ) : (
          <Typography variant="body2" color="text.secondary">
            Author persona context is unavailable for the selected plan.
          </Typography>
        )}
      </CardContent>
    </Card>
  );
}
