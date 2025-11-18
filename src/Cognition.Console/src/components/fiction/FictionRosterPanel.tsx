import React from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  Divider,
  LinearProgress,
  List,
  ListItem,
  ListItemText,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography
} from '@mui/material';
import { FictionLoreRequirementItem, FictionPlanRoster, LoreFulfillmentLog } from '../../types/fiction';

const EM_DASH = '\u2014';

type Props = {
  roster?: FictionPlanRoster | null;
  loading: boolean;
  error?: string | null;
  placeholder?: string;
  onFulfillLore?: (requirement: FictionLoreRequirementItem) => Promise<void>;
  loreHistory?: Record<string, LoreFulfillmentLog[]>;
  loreHistoryLoading?: boolean;
  loreHistoryError?: string | null;
};

export function FictionRosterPanel({
  roster,
  loading,
  error,
  placeholder = 'Select a plan to load its roster.',
  onFulfillLore,
  loreHistory,
  loreHistoryLoading = false,
  loreHistoryError = null
}: Props) {
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

  const [fulfillingId, setFulfillingId] = React.useState<string | null>(null);
  const handleFulfillClick = React.useCallback(
    async (requirement: FictionLoreRequirementItem) => {
      if (!onFulfillLore) {
        return;
      }
      setFulfillingId(requirement.id);
      try {
        await onFulfillLore(requirement);
      } finally {
        setFulfillingId(null);
      }
    },
    [onFulfillLore]
  );
  const showFulfillActions = Boolean(onFulfillLore);
  const blockedByBranch = groupBlockedLore(roster);
  const flattenedHistory = React.useMemo(() => {
    if (!loreHistory) {
      return [];
    }
    const all = Object.values(loreHistory).flat();
    return all.sort((a, b) => new Date(b.timestampUtc).getTime() - new Date(a.timestampUtc).getTime()).slice(0, 25);
  }, [loreHistory]);

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
                <TableCell>Branch</TableCell>
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
                      {[character.role, character.importance].filter(Boolean).join(' • ') || EM_DASH}
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
                        {EM_DASH}
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
                        {EM_DASH}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>
                    {renderBranchDetails(character.branchSlug, character.branchLineage, roster.branchSlug)}
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
          <>
            <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Title</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Lore Entry</TableCell>
                <TableCell>Branch</TableCell>
                <TableCell>Updated</TableCell>
                {showFulfillActions && <TableCell align="right">Actions</TableCell>}
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
                      <Chip
                        size="small"
                        label={lore.status}
                        color={lore.status === 'Ready' ? 'success' : lore.status === 'Blocked' ? 'error' : 'default'}
                      />
                    </TableCell>
                    <TableCell>{lore.worldBible ? lore.worldBible.entryName : EM_DASH}</TableCell>
                    <TableCell>
                      {renderBranchDetails(lore.branchSlug, lore.branchLineage, roster.branchSlug)}
                    </TableCell>
                  <TableCell>{formatRelative(lore.updatedAtUtc ?? lore.createdAtUtc)}</TableCell>
                  {showFulfillActions && (
                    <TableCell align="right">
                      {lore.status === 'Blocked' ? (
                        <Button
                          size="small"
                          variant="outlined"
                          onClick={() => handleFulfillClick(lore)}
                          disabled={fulfillingId === lore.id}
                        >
                          {fulfillingId === lore.id ? 'Marking...' : 'Mark Ready'}
                        </Button>
                      ) : (
                        <Typography variant="caption" color="text.secondary">
                          {EM_DASH}
                        </Typography>
                      )}
                    </TableCell>
                  )}
                </TableRow>
              ))}
            </TableBody>
            </Table>
            {blockedByBranch.length > 0 && (
              <Box sx={{ mt: 2 }}>
                <Divider sx={{ mb: 2 }} />
                <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                  Blocked Lore by Branch
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Use this list to target lore fulfillment work per branch before rerunning scrolls/scenes.
                </Typography>
                <Stack spacing={1.5} sx={{ mt: 1.5 }}>
                  {blockedByBranch.map(group => (
                    <Box key={group.key}>
                      <Stack direction="row" spacing={1} alignItems="center">
                        {renderBranchDetails(group.slug, group.lineage, roster.branchSlug)}
                        <Typography variant="caption" color="text.secondary">
                          {group.items.length === 1
                            ? '1 blocked requirement'
                            : `${group.items.length} blocked requirements`}
                        </Typography>
                      </Stack>
                      <List dense disablePadding>
                        {group.items.map(item => (
                          <ListItem key={item.id} sx={{ py: 0 }}>
                            <ListItemText
                              primary={item.title}
                              secondary={item.description || item.notes || item.requirementSlug}
                            />
                          </ListItem>
                        ))}
                      </List>
                    </Box>
                  ))}
                </Stack>
              </Box>
            )}
            <Box sx={{ mt: 3 }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                Fulfillment Activity
              </Typography>
              {loreHistoryLoading ? (
                <LinearProgress sx={{ mt: 1 }} />
              ) : loreHistoryError ? (
                <Alert severity="warning" sx={{ mt: 1 }}>
                  {loreHistoryError}
                </Alert>
              ) : flattenedHistory.length === 0 ? (
                <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                  No recorded fulfillment events yet.
                </Typography>
              ) : (
                <List dense sx={{ mt: 1 }}>
                  {flattenedHistory.map(event => {
                    const badges = getFulfillmentBadges(event);
                    return (
                      <ListItem key={`${event.requirementId}-${event.timestampUtc}-${event.action}`} sx={{ py: 0.5 }}>
                        <ListItemText
                          primary={
                            <Stack direction="row" spacing={1} alignItems="center">
                              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                                {event.requirementSlug} &mdash; {event.action}
                              </Typography>
                              {badges.isAutomation && <Chip size="small" color="primary" label="Auto-Run" />}
                              {badges.slaBreach && <Chip size="small" color="error" label="SLA Breach" />}
                            </Stack>
                          }
                          secondary={
                            <Stack spacing={0.25}>
                              <Typography variant="caption" color="text.secondary">
                                {event.actor ?? 'Unknown actor'} via {event.source} on branch {event.branch} &middot; {formatRelative(event.timestampUtc)}
                              </Typography>
                              {event.status && (
                                <Typography variant="caption" color="text.secondary">
                                  Status: {event.status}
                                </Typography>
                              )}
                              {event.notes && (
                                <Typography variant="caption" color="text.secondary">
                                  {event.notes}
                                </Typography>
                              )}
                            </Stack>
                          }
                        />
                      </ListItem>
                    );
                  })}
                </List>
              )}
            </Box>
          </>
        )}
      </Box>
    </Stack>
  );
}

type BranchGroup = {
  key: string;
  slug: string;
  lineage?: string[];
  items: FictionLoreRequirementItem[];
};

function groupBlockedLore(roster: FictionPlanRoster): BranchGroup[] {
  const groups = new Map<string, BranchGroup>();
  roster.loreRequirements.forEach(item => {
    if (item.status !== 'Blocked') {
      return;
    }
    const context = resolveBranchContext(item.branchSlug, item.branchLineage, roster.branchSlug);
    const key = context.slug.toLowerCase();
    const existing = groups.get(key);
    if (existing) {
      existing.items.push(item);
      return;
    }
    groups.set(key, {
      key,
      slug: context.slug,
      lineage: context.lineage,
      items: [item]
    });
  });
  return Array.from(groups.values()).sort((a, b) => a.slug.localeCompare(b.slug));
}

function resolveBranchContext(
  slug?: string | null,
  lineage?: string[] | null,
  fallback?: string | null
): { slug: string; lineage?: string[] } {
  const effectiveSlug =
    slug ||
    (lineage && lineage.length > 0 ? lineage[lineage.length - 1] : undefined) ||
    fallback ||
    'main';

  const normalizedLineage =
    lineage && lineage.length > 0
      ? lineage
      : fallback && fallback !== effectiveSlug
        ? [fallback, effectiveSlug]
        : undefined;

  return { slug: effectiveSlug, lineage: normalizedLineage };
}

function renderBranchDetails(slug?: string | null, lineage?: string[] | null, fallback?: string | null) {
  const context = resolveBranchContext(slug, lineage, fallback);
  return (
    <Stack spacing={0.25}>
      <Chip size="small" label={context.slug} />
      {context.lineage && context.lineage.length > 1 && (
        <Typography variant="caption" color="text.secondary">
          {context.lineage.join(' → ')}
        </Typography>
      )}
    </Stack>
  );
}

function formatRelative(value?: string | null) {
  if (!value) return EM_DASH;
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

function getFulfillmentBadges(event: LoreFulfillmentLog) {
  const normalizedAction = (event.action ?? '').toLowerCase();
  const normalizedSource = (event.source ?? '').toLowerCase();
  const normalizedStatus = (event.status ?? '').toLowerCase();
  const isAutomation = normalizedSource.includes('auto') || normalizedAction.includes('auto');
  const slaBreach = normalizedAction.includes('sla') || normalizedStatus.includes('sla');
  return { isAutomation, slaBreach };
}
