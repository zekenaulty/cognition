import React from 'react';
import {
  Alert,
  Box,
  Chip,
  LinearProgress,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography
} from '@mui/material';
import { FictionPlanRoster } from '../../types/fiction';

type Props = {
  roster?: FictionPlanRoster | null;
  loading: boolean;
  error?: string | null;
  placeholder?: string;
};

export function FictionRosterPanel({ roster, loading, error, placeholder = 'Select a plan to load its roster.' }: Props) {
  if (loading) {
    return <LinearProgress />;
  }

  if (error) {
    return (
      <Alert severity="warning">
        {error}
      </Alert>
    );
  }

  if (!roster) {
    return (
      <Typography variant="body2" color="text.secondary">
        {placeholder}
      </Typography>
    );
  }

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="subtitle2" sx={{ mb: 1, fontWeight: 600 }}>
          Tracked Characters ({roster.characters.length})
        </Typography>
        {roster.characters.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            No tracked characters have been promoted for this plan yet.
          </Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Persona</TableCell>
                <TableCell>Lore Entry</TableCell>
                <TableCell>Updated</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {roster.characters.map(character => (
                <TableRow key={character.id} hover>
                  <TableCell>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {character.displayName}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {[character.role, character.importance].filter(Boolean).join(' • ') || '—'}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    {character.persona ? (
                      <Stack spacing={0.5}>
                        <Typography variant="body2">{character.persona.name}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {character.persona.voice || character.persona.role || 'Persona linked'}
                        </Typography>
                      </Stack>
                    ) : (
                      <Typography variant="body2" color="text.secondary">
                        —
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>
                    {character.worldBible ? (
                      <Stack spacing={0.5}>
                        <Typography variant="body2">{character.worldBible.entryName}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {character.worldBible.category}
                        </Typography>
                      </Stack>
                    ) : (
                      <Typography variant="body2" color="text.secondary">
                        —
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>{formatRelative(character.updatedAtUtc ?? character.createdAtUtc)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Box>
      <Box>
        <Typography variant="subtitle2" sx={{ mb: 1, fontWeight: 600 }}>
          Lore Requirements ({roster.loreRequirements.length})
        </Typography>
        {roster.loreRequirements.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            No lore prerequisites have been captured for this plan yet.
          </Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Title</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Lore Entry</TableCell>
                <TableCell>Updated</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {roster.loreRequirements.map(lore => (
                <TableRow key={lore.id}>
                  <TableCell>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {lore.title}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {lore.description || lore.notes || lore.requirementSlug}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Chip size="small" label={lore.status} color={lore.status === 'Ready' ? 'success' : lore.status === 'Blocked' ? 'error' : 'default'} />
                  </TableCell>
                  <TableCell>{lore.worldBible ? lore.worldBible.entryName : '—'}</TableCell>
                  <TableCell>{formatRelative(lore.updatedAtUtc ?? lore.createdAtUtc)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Box>
    </Stack>
  );
}

function formatRelative(value?: string | null) {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  const diffMs = Date.now() - date.getTime();
  const future = diffMs < 0;
  const minutes = Math.floor(Math.abs(diffMs) / 60000);
  if (minutes < 1) return future ? 'in moments' : 'just now';
  if (minutes < 60) return `${minutes}m ${future ? 'from now' : 'ago'}`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ${minutes % 60}m ${future ? 'from now' : 'ago'}`;
  const days = Math.floor(hours / 24);
  return `${days}d ${hours % 24}h ${future ? 'from now' : 'ago'}`;
}
